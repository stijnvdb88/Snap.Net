using Newtonsoft.Json.Linq;
using SnapDotNet.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// A dependency-free integration runner on the same .NET Framework as Classic.
// Each test uses an ephemeral loopback port; it never contacts a real snapserver.
internal static class Program
{
    private static int s_UnobservedExceptions;

    private static int Main()
    {
        if (Environment.GetEnvironmentVariable("SNAP_CONTROL_TEST_TRACE") == "1")
            Trace.Listeners.Add(new ConsoleTraceListener());
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Interlocked.Increment(ref s_UnobservedExceptions);
            Console.Error.WriteLine(e.Exception);
            e.SetObserved();
        };
        try
        {
            RunAsync().GetAwaiter().GetResult();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert(s_UnobservedExceptions == 0, "Unobserved task exceptions");
            Console.WriteLine("All control-client regression tests passed.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        await Test("volume subscriptions survive server updates", () =>
        {
            var config = new Config { volume = new Volume { percent = 10 } };
            var original = config.volume;
            int commands = 0;
            int groupUpdates = 0;
            var group = new Group { clients = new[] { new Client { config = config } } };
            group.SubscribeToClientEvents();
            group.CLIENT_OnVolumeUpdated += () => groupUpdates++;
            original.CLIENT_OnModified += () => commands++;
            // Reproduces the old lost-subscription window synchronously.
            config.SERVER_OnVolumeUpdated += () => config.volume.CLIENT_ToggleMuted();
            config.SERVER_SetVolume(new Volume { percent = 20 });
            Assert(ReferenceEquals(original, config.volume) && commands == 1 && groupUpdates == 1,
                "A server update detached local volume handlers");
            return Task.CompletedTask;
        });

        SnapcastClient.AutoReconnect = true;
        await WithServer("normal volume, client/group mute and healthy idle", async (server, client) =>
        {
            var group = client.ServerData.groups[0];
            var volume = group.clients[0].config.volume;
            volume.CLIENT_SetPercent(42);
            await Until(() => server.Percent == 42, "normal volume");
            volume.CLIENT_ToggleMuted();
            await Until(() => server.Muted, "client mute");
            group.CLIENT_ToggleMuted();
            await Until(() => server.GroupMuted, "group mute");
            await Until(() => group.muted && volume.muted, "local mute acknowledgements");
            int statusCount = server.StatusRequests;
            int connections = server.Connections;
            await Task.Delay(1200);
            Assert(server.StatusRequests == statusCount && server.Connections == connections,
                "Healthy connection was polled or reconnected");
        });

        await WithServer("remote close, reconnect, fresh state and subscriptions", async (server, client) =>
        {
            var old = client.ServerData;
            int invalidations = 0;
            old.groups[0].SERVER_OnInvalidate += () => invalidations++;
            server.CloseConnections();
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "close recovery");
            Assert(invalidations == 1, "Old group was not invalidated exactly once");
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(90);
            client.ServerData.groups[0].clients[0].config.volume.CLIENT_SetPercent(37);
            await Until(() => server.Percent == 37, "volume after reconnect");
            await Task.Delay(150);
            Assert(!server.VolumeRequests.Contains(90), "An obsolete UI model sent a command");
        });

        await WithServer("established but unresponsive TCP, timeout and recovery", async (server, client) =>
        {
            var old = client.ServerData;
            server.SilenceConnections();
            old.groups[0].clients[0].config.volume.CLIENT_ToggleMuted();
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(24);
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(25);
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "silent TCP recovery");
            Assert(!client.ServerData.groups[0].clients[0].config.volume.muted,
                "Unacknowledged optimistic mute survived resynchronization");
            client.ServerData.groups[0].clients[0].config.volume.CLIENT_SetPercent(61);
            await Until(() => server.Percent == 61, "command after timeout recovery");
            Assert(server.VolumeRequests.SequenceEqual(new[] { 61 }), "Old queued commands were replayed after reconnect");
        });

        await WithServer("unresponsive group mute and explicit status request", async (server, client) =>
        {
            var old = client.ServerData;
            server.SilenceConnections();
            old.groups[0].CLIENT_ToggleMuted();
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "group mute timeout recovery");
            Assert(!client.ServerData.groups[0].muted, "Unacknowledged group mute survived recovery");
            old = client.ServerData;
            server.SilenceConnections();
            await client.GetServerStatusAsync();
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "explicit status timeout recovery");
        });

        await WithServer("rapid volume changes are ordered and not duplicated", async (server, client) =>
        {
            server.DelayVolumeReplies = true;
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            for (int i = 1; i <= 50; i++)
                volume.CLIENT_SetPercent(i);
            await Until(() => server.VolumeRequests.Count == 50 && volume.percent == 50, "rapid volume drain");
            Assert(server.VolumeRequests.SequenceEqual(Enumerable.Range(1, 50)), "Volume requests reordered or duplicated");
            Assert(server.MaxPendingVolume == 1 && server.Percent == 50, "Overlapping/stale volume responses");
        });

        await WithServer("status refresh preserves commands on the same connection", async (server, client) =>
        {
            server.DelayVolumeReplies = true;
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            for (int i = 1; i <= 10; i++) volume.CLIENT_SetPercent(i);
            await client.GetServerStatusAsync();
            await Until(() => server.VolumeRequests.Count == 10, "commands across status refresh");
            Assert(server.Percent == 10 && server.Connections == 1,
                "A status refresh dropped queued commands or reconnected");
        });

        await WithServer("repeated reconnects do not multiply requests or workers", async (server, client) =>
        {
            for (int i = 0; i < 5; i++)
            {
                var old = client.ServerData;
                server.CloseConnections();
                await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "reconnect cycle " + i);
                client.ServerData.groups[0].clients[0].config.volume.CLIENT_SetPercent(60 + i);
                await Until(() => server.Percent == 60 + i, "cycle volume");
            }
            Assert(server.VolumeRequests.Count == 5, "Duplicated subscriptions after reconnect");
            Assert(server.Connections == 6, "Unexpected extra reconnects");
        });

        await WithServer("subscriber failure does not terminate recovery", async (server, client) =>
        {
            var old = client.ServerData;
            old.groups[0].SERVER_OnInvalidate += () => throw new InvalidOperationException("Test subscriber failure");
            server.CloseConnections();
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "recovery after subscriber failure");
        });

        await WithServer("RPC error resynchronizes without reconnecting", async (server, client) =>
        {
            server.RejectNextVolume = true;
            var old = client.ServerData;
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(99);
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "RPC error resynchronization");
            Assert(client.IsConnected() && server.Connections == 1 && server.Percent == 20,
                "Valid RPC error unnecessarily reconnected or retained optimistic state");
        });

        await Test("initial status timeout and shutdown during reconnect", async () =>
        {
            using (var server = new FakeServer { SilenceNewConnections = true })
            {
                var client = new SnapcastClient();
                await client.ConnectAsync("127.0.0.1", server.Port, 300);
                Assert(client.ConnectionFailed && client.ServerData == null, "Initial status did not time out");
                await Until(() => server.Connections >= 2, "active reconnect attempt");
                client.Dispose();
                await BackgroundTasks(client);
                await Until(() => server.ActiveConnections == 0, "TCP resources released on shutdown");
                int connections = server.Connections;
                await Task.Delay(1200);
                Assert(!client.IsConnected() && client.ServerData == null && server.Connections == connections,
                    "Disposed client reconnected or published state");
                client.Dispose();
            }
            using (var client = new SnapcastClient()) { } // never connected
        });

        await WithServer("shutdown cancels in-flight and queued commands", async (server, client) =>
        {
            server.SilenceConnections();
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            for (int i = 1; i <= 20; i++) volume.CLIENT_SetPercent(i);
            await Task.Delay(100);
            client.Dispose();
            await BackgroundTasks(client);
            int connections = server.Connections;
            await Task.Delay(1100);
            Assert(server.Connections == connections && client.ServerData == null,
                "Shutdown allowed a queued command or reconnect to restore state");
        });

        await Test("initial refusal recovers when server appears", async () =>
        {
            int port;
            var reservation = new TcpListener(IPAddress.Loopback, 0);
            reservation.Start();
            port = ((IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            using (var client = new SnapcastClient())
            {
                await client.ConnectAsync("127.0.0.1", port, 500);
                Assert(client.ConnectionFailed, "Refused connection reported success");
                using (var server = new FakeServer(port))
                    await Until(client.IsConnected, "initial refusal recovery");
                client.Dispose();
                await BackgroundTasks(client);
            }
        });

        await WithServer("explicit connects are serialized with recovery", async (server, client) =>
        {
            server.CloseConnections();
            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.ConnectAsync("127.0.0.1", server.Port, 500)));
            await Until(client.IsConnected, "concurrent explicit connect completion");
            int count = server.Connections;
            await Task.Delay(1200);
            Assert(server.Connections == count, "A superseded listener disconnected the replacement connection");
        });

        await WithServer("auto reconnect disabled then re-enabled", async (server, client) =>
        {
            SnapcastClient.AutoReconnect = false;
            server.CloseConnections();
            await Until(() => client.ServerData == null, "disconnect with auto reconnect disabled");
            await Task.Delay(1200);
            Assert(server.Connections == 1, "AutoReconnect=false was ignored");
            SnapcastClient.AutoReconnect = true;
            await Until(client.IsConnected, "auto reconnect re-enabled");
        });

        await WithServer("resume refreshes silent connection without user action and coalesces requests", async (server, client) =>
        {
            var old = client.ServerData;
            server.SilenceConnections();
            server.Muted = true;
            server.GroupMuted = true;
            server.StatusDelayMs = 150;
            var attempts = Enumerable.Range(0, 8).Select(_ => client.ReconnectAsync()).ToArray();
            Assert(attempts.All(task => ReferenceEquals(task, attempts[0])), "Resume attempts did not coalesce");
            await Task.WhenAll(attempts);
            Assert(client.IsConnected() && !ReferenceEquals(old, client.ServerData), "Resume did not replace connection");
            Assert(client.ServerData.groups[0].muted && client.ServerData.groups[0].clients[0].config.volume.muted,
                "Resume did not refresh mute state without a click");
            await Task.Delay(1200);
            Assert(server.Connections == 2 && server.VolumeRequests.Count == 0, "Resume caused extra reconnects or replayed controls");
        });

        await Test("resume aborts silent in-flight request before its timeout", async () =>
        {
            using (var server = new FakeServer())
            using (var client = new SnapcastClient())
            {
                await client.ConnectAsync("127.0.0.1", server.Port, 5000);
                server.SilenceConnections();
                client.ServerData.groups[0].clients[0].config.volume.CLIENT_SetPercent(33);
                await Task.Delay(100);
                var clock = Stopwatch.StartNew();
                await client.ReconnectAsync();
                Assert(clock.ElapsedMilliseconds < 2000 && client.IsConnected(), "Resume waited for old request deadline");
                Assert(server.Percent == 20, "Resume replayed an unacknowledged control");
                client.Dispose();
                await BackgroundTasks(client);
            }
        });

        await WithServer("resume retries while network is not ready and stops on shutdown", async (server, client) =>
        {
            server.SilenceNewConnections = true;
            await client.ReconnectAsync();
            Assert(!client.IsConnected(), "Silent post-resume status unexpectedly succeeded");
            server.SilenceNewConnections = false;
            await Until(client.IsConnected, "resume network recovery");
            server.StatusDelayMs = 1000;
            var resume = client.ReconnectAsync();
            await Task.Delay(100);
            client.Dispose();
            await resume;
            await BackgroundTasks(client);
            Assert(client.ServerData == null, "Resume published state after shutdown");
        });

        await Test("control lifecycle diagnostics reach a trace listener", async () =>
        {
            var listener = new DiagnosticListener();
            Trace.Listeners.Add(listener);
            try
            {
                using (var server = new FakeServer())
                using (var client = new SnapcastClient())
                {
                    await client.ConnectAsync("127.0.0.1", server.Port, 500);
                    server.SilenceConnections();
                    await client.GetServerStatusAsync();
                    await Until(client.IsConnected, "diagnostic timeout recovery");
                    client.Dispose();
                    await BackgroundTasks(client);
                }
                Assert(listener.Messages.Any(message => message.Contains("Connecting control to 127.0.0.1:")) &&
                    listener.Messages.Any(message => message.Contains("timed out")) &&
                    listener.Messages.Any(message => message.Contains("Control state synchronized")),
                    "Missing lifecycle/timeout/synchronization diagnostics");
            }
            finally { Trace.Listeners.Remove(listener); listener.Dispose(); }
        });

        await AckOrderingTests();
    }

    private static async Task AckOrderingTests()
    {
        await WithServer("ack: volume 30 -> 40 -> mute between acknowledgements", async (server, client) =>
        {
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            var first = server.HoldNext("Client.SetVolume");
            var second = server.HoldNext("Client.SetVolume");
            volume.CLIENT_SetPercent(30);
            await BarrierReceived(first);
            volume.CLIENT_SetPercent(40);
            first.Release.TrySetResult(true);
            await BarrierReceived(second);
            volume.CLIENT_ToggleMuted();
            second.Release.TrySetResult(true);
            await DrainCommands(client);
            Console.WriteLine("REPRO volume/mute: server=" + server.Percent + ", muted=" + server.Muted);
            Assert(server.Percent == 40 && server.Muted && volume.percent == 40 && volume.muted,
                "Older acknowledgement overwrote newer volume intent");
        });

        await WithServer("ack: mute -> volume -> unmute between acknowledgements", async (server, client) =>
        {
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            var first = server.HoldNext("Client.SetVolume");
            var second = server.HoldNext("Client.SetVolume");
            volume.CLIENT_ToggleMuted();
            await BarrierReceived(first);
            volume.CLIENT_SetPercent(55);
            first.Release.TrySetResult(true);
            await BarrierReceived(second);
            volume.CLIENT_ToggleMuted();
            second.Release.TrySetResult(true);
            await DrainCommands(client);
            Assert(server.Percent == 55 && !server.Muted && volume.percent == 55 && !volume.muted,
                "Unmute used an older acknowledged volume");
        });

        await WithServer("ack: external changes before and after a local burst stay visible", async (server, client) =>
        {
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            await server.PublishVolumeAsync(63, true);
            await Until(() => volume.percent == 63 && volume.muted, "external idle volume");
            volume.CLIENT_SetPercent(45);
            await DrainCommands(client);
            await server.PublishVolumeAsync(20, false); // includes a previously acknowledged value
            await Until(() => volume.percent == 20 && !volume.muted, "external volume after completion");
            await server.PublishVolumeAsync(72, true);
            await Until(() => volume.percent == 72 && volume.muted, "external changes remain enabled");
        });

        await WithServer("ack: external change before final acknowledgement is reconciled", async (server, client) =>
        {
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            var last = server.HoldNext("Client.SetVolume");
            volume.CLIENT_SetPercent(45);
            await BarrierReceived(last); // server applied 45, but its reply is still blocked
            await server.PublishVolumeAsync(67, true);
            last.Release.TrySetResult(true);
            await DrainCommands(client);
            Assert(volume.percent == 67 && volume.muted, "External update during pending acknowledgement was lost");
        });

        await WithServer("ack: disconnect discards old intent and reconnect status is authoritative", async (server, client) =>
        {
            var old = client.ServerData;
            var pending = server.HoldNext("Client.SetVolume");
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(31);
            await BarrierReceived(pending);
            old.groups[0].clients[0].config.volume.CLIENT_SetPercent(49);
            server.Percent = 68;
            server.Muted = true;
            server.CloseConnections();
            pending.Release.TrySetResult(true);
            await Until(() => client.IsConnected() && !ReferenceEquals(old, client.ServerData), "pending reconnect");
            await DrainCommands(client);
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            Assert(volume.percent == 68 && volume.muted && !server.VolumeRequests.Contains(49),
                "Pending intent leaked onto a replacement connection");
            await server.PublishVolumeAsync(54, false);
            await Until(() => volume.percent == 54 && !volume.muted, "external update after reconnect");
        });

        await WithServer("ack: external notification during readback cannot be suppressed by its older snapshot", async (server, client) =>
        {
            var group = client.ServerData.groups[0];
            var volume = group.clients[0].config.volume;
            var read = server.HoldNext("Client.GetStatus");
            volume.CLIENT_SetPercent(45);
            await BarrierReceived(read); // snapshot already captured 45
            await server.PublishVolumeAsync(73, true);
            read.Release.TrySetResult(true);
            await DrainCommands(client);
            await Until(() => volume.percent == 73 && volume.muted, "external change during volume readback");

            var groupRead = server.HoldNext("Group.GetStatus");
            group.CLIENT_ToggleMuted();
            await BarrierReceived(groupRead); // snapshot already captured true
            await server.PublishGroupMuteAsync(false);
            groupRead.Release.TrySetResult(true);
            await DrainCommands(client);
            await Until(() => !group.muted, "external change during group readback");
        });

        await WithServer("ack: group mute preserves intent and external changes remain visible", async (server, client) =>
        {
            var group = client.ServerData.groups[0];
            var first = server.HoldNext("Group.SetMute");
            var second = server.HoldNext("Group.SetMute");
            group.CLIENT_ToggleMuted();
            await BarrierReceived(first);
            group.CLIENT_ToggleMuted();
            first.Release.TrySetResult(true);
            await BarrierReceived(second);
            Assert(!group.muted, "Older group mute acknowledgement overwrote the second toggle");
            await server.PublishGroupMuteAsync(true);
            second.Release.TrySetResult(true);
            await DrainCommands(client);
            Assert(group.muted, "External group mute during pending write was lost");
            await server.PublishGroupMuteAsync(false);
            await Until(() => !group.muted, "external group mute after completion");
        });

        await WithServer("ack: rapid mixed controls preserve latest values", async (server, client) =>
        {
            var volume = client.ServerData.groups[0].clients[0].config.volume;
            var pending = server.HoldNext("Client.SetVolume");
            volume.CLIENT_SetPercent(1);
            await BarrierReceived(pending);
            for (int i = 2; i <= 60; i++)
            {
                volume.CLIENT_SetPercent(i);
                volume.CLIENT_ToggleMuted();
            }
            pending.Release.TrySetResult(true);
            await DrainCommands(client);
            Assert(server.Percent == 60 && server.Muted && volume.percent == 60 && volume.muted,
                "Mixed control burst ended with stale values");
        });

        await WithServer("ack: queued names latency stream mute and membership use event snapshots", async (server, client) =>
        {
            var group = client.ServerData.groups[0];
            var model = group.clients[0];
            var blocker = server.HoldNext("Client.SetVolume");
            model.config.volume.CLIENT_SetPercent(32);
            await BarrierReceived(blocker);
            model.CLIENT_SetName("A");
            model.CLIENT_SetName("B");
            model.config.CLIENT_SetLatency(10);
            model.config.CLIENT_SetLatency(20);
            group.CLIENT_SetName("G1");
            group.CLIENT_SetName("G2");
            group.CLIENT_SetStream("stream1");
            group.CLIENT_SetStream("stream2");
            group.CLIENT_ToggleMuted();
            group.CLIENT_ToggleMuted();
            var ids = new[] { "other", "client" };
            group.CLIENT_SetClients(ids);
            ids[0] = "mutated-after-event";
            blocker.Release.TrySetResult(true);
            await DrainCommands(client);
            Func<string, string, string[]> values = (method, field) => server.Requests
                .Where(request => (string)request["method"] == method)
                .Select(request => (string)request["params"][field]).ToArray();
            Assert(values("Client.SetName", "name").SequenceEqual(new[] { "A", "B" }), "Client name snapshots lost");
            Assert(values("Client.SetLatency", "latency").SequenceEqual(new[] { "10", "20" }), "Latency snapshots lost");
            Assert(values("Group.SetName", "name").SequenceEqual(new[] { "G1", "G2" }), "Group name snapshots lost");
            Assert(values("Group.SetStream", "stream_id").SequenceEqual(new[] { "stream1", "stream2" }), "Stream snapshots lost");
            Assert(server.Requests.Single(request => (string)request["method"] == "Group.SetClients")["params"]["clients"][0].Value<string>() == "other",
                "Membership array was not cloned");
            Assert(server.ClientName == "B" && server.Latency == 20 && server.GroupName == "G2" && server.StreamId == "stream2" && !server.GroupMuted,
                "Queued edits changed unrelated fields or lost their final values");
            Assert(client.ServerData.groups[0].name == "G2" && client.ServerData.groups[0].stream_id == "stream2", "Stream acknowledgement changed the group name");
        });

        await WithServer("ack: older edit acknowledgements do not roll back newer model values", async (server, client) =>
        {
            var group = client.ServerData.groups[0];
            var model = group.clients[0];
            var cases = new[]
            {
                Tuple.Create("Client.SetName", (Action)(() => model.CLIENT_SetName("A")), (Action)(() => model.CLIENT_SetName("B")), (Func<bool>)(() => model.config.name == "B")),
                Tuple.Create("Client.SetLatency", (Action)(() => model.config.CLIENT_SetLatency(10)), (Action)(() => model.config.CLIENT_SetLatency(20)), (Func<bool>)(() => model.config.latency == 20)),
                Tuple.Create("Group.SetName", (Action)(() => group.CLIENT_SetName("G1")), (Action)(() => group.CLIENT_SetName("G2")), (Func<bool>)(() => group.name == "G2")),
                Tuple.Create("Group.SetStream", (Action)(() => group.CLIENT_SetStream("stream1")), (Action)(() => group.CLIENT_SetStream("stream2")), (Func<bool>)(() => group.stream_id == "stream2"))
            };
            foreach (var item in cases)
            {
                var first = server.HoldNext(item.Item1);
                var second = server.HoldNext(item.Item1);
                item.Item2();
                await BarrierReceived(first);
                item.Item3();
                first.Release.TrySetResult(true);
                await BarrierReceived(second);
                try { Assert(item.Item4(), "Stale acknowledgement changed latest model value: " + item.Item1); }
                finally { second.Release.TrySetResult(true); }
                await DrainCommands(client);
            }
        });
    }

    private static async Task BarrierReceived(FakeServer.ReplyBarrier barrier)
    {
        Assert(await Task.WhenAny(barrier.Received.Task, Task.Delay(3000)) == barrier.Received.Task, "Expected RPC did not reach reply barrier");
    }

    private static async Task DrainCommands(SnapcastClient client)
    {
        var field = typeof(SnapcastClient).GetField("m_CommandTask", BindingFlags.Instance | BindingFlags.NonPublic);
        while (true)
        {
            var task = (Task)field.GetValue(client);
            Assert(await Task.WhenAny(task, Task.Delay(8000)) == task, "Command queue did not drain");
            await task;
            if (ReferenceEquals(task, field.GetValue(client))) return;
        }
    }

    private static async Task WithServer(string name, Func<FakeServer, SnapcastClient, Task> test)
    {
        await Test(name, async () =>
        {
            using (var server = new FakeServer())
            using (var client = new SnapcastClient())
            {
                await client.ConnectAsync("127.0.0.1", server.Port, 500);
                Assert(client.IsConnected(), "Initial connection failed");
                await test(server, client);
                client.Dispose();
                await BackgroundTasks(client);
                await Until(() => server.ActiveConnections == 0, "TCP resources released on shutdown");
            }
        });
    }

    private static async Task BackgroundTasks(SnapcastClient client)
    {
        foreach (string field in new[] { "m_ConnectionCheckTask", "m_CommandTask", "m_ReconnectTask" })
        {
            var task = (Task)typeof(SnapcastClient).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            if (task == null) continue;
            Assert(await Task.WhenAny(task, Task.Delay(2000)) == task, field + " did not stop on shutdown");
            await task;
        }
    }

    private static async Task Test(string name, Func<Task> test)
    {
        string filter = Environment.GetEnvironmentVariable("SNAP_CONTROL_TEST_FILTER");
        if (!string.IsNullOrEmpty(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
            return;
        var clock = Stopwatch.StartNew();
        await test();
        Console.WriteLine("PASS " + name + " (" + clock.ElapsedMilliseconds + " ms)");
    }

    private static async Task Until(Func<bool> predicate, string description)
    {
        var clock = Stopwatch.StartNew();
        while (!predicate())
        {
            Assert(clock.ElapsedMilliseconds < 8000, "Timed out: " + description);
            await Task.Delay(20);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class DiagnosticListener : TraceListener
    {
        internal readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
        public override void WriteLine(string message, string category)
        {
            if (category == "Snapcast control") Messages.Enqueue(message);
        }
    }
}

internal sealed class FakeServer : IDisposable
{
    private sealed class Session
    {
        internal TcpClient Tcp;
        internal StreamWriter Writer;
        internal volatile bool Silent;
        internal readonly SemaphoreSlim WriteGate = new SemaphoreSlim(1, 1);
    }

    private readonly TcpListener m_Listener;
    private readonly ConcurrentDictionary<Session, byte> m_Sessions = new ConcurrentDictionary<Session, byte>();
    private readonly Task m_AcceptTask;
    private readonly ConcurrentBag<Task> m_SessionTasks = new ConcurrentBag<Task>();
    private volatile bool m_Disposed;
    private int m_Connections, m_StatusRequests, m_PendingVolume, m_MaxPendingVolume;
    internal volatile int Percent = 20;
    internal volatile bool Muted, GroupMuted;
    internal volatile bool DelayVolumeReplies, RejectNextVolume, SilenceNewConnections;
    internal volatile int StatusDelayMs;
    internal readonly ConcurrentQueue<int> VolumeRequests = new ConcurrentQueue<int>();
    internal readonly ConcurrentQueue<JObject> Requests = new ConcurrentQueue<JObject>();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ReplyBarrier>> m_Barriers = new ConcurrentDictionary<string, ConcurrentQueue<ReplyBarrier>>();
    internal string ClientName = "Test", GroupName = "Test", StreamId = "stream";
    internal int Latency;

    internal sealed class ReplyBarrier
    {
        internal readonly TaskCompletionSource<bool> Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<bool> Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal ReplyBarrier HoldNext(string method)
    {
        var barrier = new ReplyBarrier();
        m_Barriers.GetOrAdd(method, _ => new ConcurrentQueue<ReplyBarrier>()).Enqueue(barrier);
        return barrier;
    }

    private object ClientSnapshot() => new { id = "client", connected = true,
        config = new { name = ClientName, latency = Latency, volume = new { percent = Percent, muted = Muted } } };
    private object GroupSnapshot() => new { id = "group", name = GroupName, muted = GroupMuted,
        stream_id = StreamId, clients = new[] { ClientSnapshot() } };

    internal async Task PublishVolumeAsync(int percent, bool muted)
    {
        Percent = percent;
        Muted = muted;
        await PublishNotificationAsync("Client.OnVolumeChanged", new { id = "client", volume = new { percent, muted } });
    }

    internal Task PublishGroupMuteAsync(bool muted)
    {
        GroupMuted = muted;
        return PublishNotificationAsync("Group.OnMute", new { id = "group", mute = muted });
    }

    private async Task PublishNotificationAsync(string method, object args)
    {
        foreach (var session in m_Sessions.Keys)
        {
            await session.WriteGate.WaitAsync();
            try
            {
                await session.Writer.WriteLineAsync(JObject.FromObject(new { jsonrpc = "2.0", method,
                    @params = args }).ToString(Newtonsoft.Json.Formatting.None));
            }
            finally { session.WriteGate.Release(); }
        }
    }

    internal int Connections => Volatile.Read(ref m_Connections);
    internal int ActiveConnections => m_Sessions.Count;
    internal int StatusRequests => Volatile.Read(ref m_StatusRequests);
    internal int MaxPendingVolume => Volatile.Read(ref m_MaxPendingVolume);
    internal int Port => ((IPEndPoint)m_Listener.LocalEndpoint).Port;

    internal FakeServer(int port = 0)
    {
        m_Listener = new TcpListener(IPAddress.Loopback, port);
        m_Listener.Start();
        m_AcceptTask = AcceptAsync();
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (!m_Disposed)
            {
                var session = new Session { Tcp = await m_Listener.AcceptTcpClientAsync(), Silent = SilenceNewConnections };
                m_Sessions.TryAdd(session, 0);
                Interlocked.Increment(ref m_Connections);
                m_SessionTasks.Add(ServeAsync(session));
            }
        }
        catch (ObjectDisposedException) when (m_Disposed) { }
        catch (SocketException) when (m_Disposed) { }
    }

    private async Task ServeAsync(Session session)
    {
        var replies = new List<Task>();
        try
        {
            using (var stream = session.Tcp.GetStream())
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                session.Writer = writer;
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var request = JObject.Parse(line);
                    if (request["id"] == null || session.Silent) continue;
                    replies.Add(ReplyAsync(session, writer, request));
                }
                await Task.WhenAll(replies);
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            // Observe response tasks even when a socket is closed during a delayed reply.
            await Task.WhenAll(replies);
            session.Tcp.Dispose();
            m_Sessions.TryRemove(session, out _);
        }
    }

    private async Task ReplyAsync(Session session, StreamWriter writer, JObject request)
    {
        try
        {
            string method = (string)request["method"];
            var args = request["params"];
            Requests.Enqueue((JObject)request.DeepClone());
            ReplyBarrier barrier = null;
            if (m_Barriers.TryGetValue(method, out var barriers)) barriers.TryDequeue(out barrier);
            object result;
            object notification = null;
            bool rejected = false;
            switch (method)
            {
                case "Server.GetStatus":
                    if (StatusDelayMs > 0) await Task.Delay(StatusDelayMs);
                    Interlocked.Increment(ref m_StatusRequests);
                    result = new
                    {
                        server = new
                        {
                            groups = new[] { GroupSnapshot() },
                            streams = new object[0]
                        }
                    };
                    break;
                case "Client.GetStatus": result = new { client = ClientSnapshot() }; break;
                case "Group.GetStatus": result = new { group = GroupSnapshot() }; break;
                case "Client.SetName": ClientName = (string)args["name"]; result = new { name = ClientName }; break;
                case "Client.SetLatency": Latency = (int)args["latency"]; result = new { latency = Latency }; break;
                case "Group.SetName": GroupName = (string)args["name"]; result = new { name = GroupName }; break;
                case "Group.SetStream": StreamId = (string)args["stream_id"]; result = new { stream_id = StreamId }; break;
                case "Group.SetClients": result = new { server = new { groups = new[] { GroupSnapshot() }, streams = new object[0] } }; break;
                case "Client.SetVolume":
                    int pending = Interlocked.Increment(ref m_PendingVolume);
                    m_MaxPendingVolume = Math.Max(m_MaxPendingVolume, pending);
                    try
                    {
                        if (DelayVolumeReplies) await Task.Delay((int)args["volume"]["percent"] == 1 ? 100 : 10);
                        rejected = RejectNextVolume;
                        RejectNextVolume = false;
                        if (!rejected)
                        {
                            Percent = (int)args["volume"]["percent"];
                            Muted = (bool)args["volume"]["muted"];
                            VolumeRequests.Enqueue(Percent);
                        }
                        var volume = new { percent = Percent, muted = Muted };
                        result = new { volume };
                        notification = new { jsonrpc = "2.0", method = "Client.OnVolumeChanged", @params = new { id = "client", volume } };
                    }
                    finally { Interlocked.Decrement(ref m_PendingVolume); }
                    break;
                case "Group.SetMute":
                    GroupMuted = (bool)args["mute"];
                    result = new { mute = GroupMuted };
                    break;
                default: throw new Exception("Unexpected test RPC: " + method);
            }

            if (barrier != null)
            {
                barrier.Received.TrySetResult(true);
                if (await Task.WhenAny(barrier.Release.Task, Task.Delay(5000)) != barrier.Release.Task)
                    throw new TimeoutException("Test reply barrier was not released.");
            }

            await session.WriteGate.WaitAsync();
            try
            {
                if (notification != null && !rejected)
                    await writer.WriteLineAsync(JObject.FromObject(notification).ToString(Newtonsoft.Json.Formatting.None));
                var response = rejected
                    ? new JObject { ["jsonrpc"] = "2.0", ["id"] = request["id"], ["error"] = new JObject { ["code"] = -32602, ["message"] = "Test rejection" } }
                    : new JObject { ["jsonrpc"] = "2.0", ["id"] = request["id"], ["result"] = JObject.FromObject(result) };
                await writer.WriteLineAsync(response.ToString(Newtonsoft.Json.Formatting.None));
            }
            finally { session.WriteGate.Release(); }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    internal void CloseConnections()
    {
        foreach (var session in m_Sessions.Keys) session.Tcp.Dispose();
    }

    internal void SilenceConnections()
    {
        foreach (var session in m_Sessions.Keys) session.Silent = true;
    }

    public void Dispose()
    {
        m_Disposed = true;
        m_Listener.Stop();
        m_AcceptTask.GetAwaiter().GetResult();
        CloseConnections();
        Task.WhenAll(m_SessionTasks).GetAwaiter().GetResult();
    }
}
