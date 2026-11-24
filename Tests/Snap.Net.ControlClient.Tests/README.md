# Classic control connection regression tests

## Acknowledgement ordering follow-up

Branch `fix/classic-control-ack-ordering` starts at `5130706` and preserves both
validated reconnect commits (`7bc6d9e` and `5130706`). Its output is isolated from
the installed sleep/resume test build. Do not deploy over `classic-resume`.

The older implementation could acknowledge volume 30 after the user had already
requested 40. A mute during the next RPC then captured 30 and sent 30 + muted.
The new deterministic reply-barrier test fails against the unmodified 5130706
DLL with 30 + muted and passes against the corrected production DLL with
40 + muted. The tests use production assemblies, not a replacement control client.

Volume intent and group mute intent now carry a per-model pending token. Older
acknowledgements do not update that optimistic state. Notifications observed
during a pending burst, or queued before a newer local revision, cannot overwrite
it later. After the last write, a targeted `Client.GetStatus` / `Group.GetStatus`
read reconciles the affected volume/mute only. This also includes external changes
made before the final acknowledgement; there is no permanent value blacklist or
notification suppression. A new local edit during the read supersedes that read.
If another notification arrives during the read, a fresh reconciliation is queued
at the tail instead of publishing that potentially older snapshot. Other queued
commands can continue; the connection gate is not held in a retry loop.
Reconnect still replaces all models with authoritative server status. Full
server/topology updates retain the existing model replacement behavior.

All queued command arguments are captured at the modification event, including
IDs, names, latency, streams, mute and a cloned membership array. Name/latency/
stream acknowledgements are applied only if the corresponding model value still
matches that request. Stream acknowledgements now update stream selection rather
than the group name; latency uses its actual acknowledged result field.

These readback methods and result shapes are documented in the
[Snapcast control API](https://github.com/snapcast/snapcast/blob/develop/doc/json_rpc_api/control.md).
They add a targeted read after a completed local burst, not periodic polling.
The connection worker, Windows resume handler, logging bridge, dependencies,
playback and broadcast code remain unchanged. The new ack correction has not
replaced the validated installation or itself undergone prolonged workstation use.

Build and run this follow-up separately:

```powershell
msbuild Tests\Snap.Net.ControlClient.Tests\Snap.Net.ControlClient.Tests.csproj /restore /p:Configuration=Release /p:OutputPath="$PWD\artifacts\ack-tests\"
& .\artifacts\ack-tests\Snap.Net.ControlClient.Tests.exe
msbuild Snap.Net.csproj /restore /t:Rebuild /p:Configuration=Release /p:SolutionDir="$PWD\" /p:VersionAssembly=0.35.0.4 /p:OutputPath="$PWD\artifacts\classic-ack-ordering\"
powershell.exe -NoProfile -STA -File Tests\Snap.Net.ControlClient.Tests\ClassicResumeSmoke.ps1 -ApplicationDirectory artifacts\classic-ack-ordering -TestDirectory artifacts\ack-tests -LogPath artifacts\ack-smoke-nlog.log
```

The suite has 29 cases: the existing 19 plus 10 deterministic ack cases, including
interleaved controls, idle/post-burst external updates, an external update before
the final acknowledgement, notifications during the readback itself, disconnect with pending writes,
group mute, mixed bursts and queued edit payloads/acknowledgements. The WPF/NLog
smoke test separately exercises the unchanged resume handler and file logger.

To compare against an existing production DLL, the test project also accepts
`/p:ControlClientAssemblyPath=<absolute-DLL-path>` with a separate OutputPath.
Restore with the same properties, copy that build's runtime DLLs into the test
output and use its application binding redirects as the test executable config.
Set `SNAP_CONTROL_TEST_FILTER=ack: volume 30` to select the before/after case.
This does not change the source or files of the original production build.

The remaining sections document the original reconnect investigation.

This is a standalone, non-interactive .NET Framework 4.7.2 integration runner.
It uses the production netstandard2.0 control library and a newline-delimited
JSON-RPC server on ephemeral IPv4 loopback ports. No real Snapcast server,
audio device, WPF application settings, or additional test packages are needed.
Exit code 0 means success; failures return 1. It is intentionally separate from
the main solution, which also contains mobile and Avalonia projects.

From a Visual Studio Developer PowerShell, in the repository root:

```powershell
msbuild Tests\Snap.Net.ControlClient.Tests\Snap.Net.ControlClient.Tests.csproj /restore /p:Configuration=Release
& .\Tests\Snap.Net.ControlClient.Tests\bin\Release\net472\Snap.Net.ControlClient.Tests.exe

msbuild Snap.Net.csproj /restore /p:Configuration=Release /p:SolutionDir="$PWD\" /p:VersionAssembly=0.35.0.3 /p:OutputPath="$PWD\artifacts\classic-resume\"

# Run the actual WPF resume handler and NLog bridge without starting the app:
powershell.exe -NoProfile -STA -File Tests\Snap.Net.ControlClient.Tests\ClassicResumeSmoke.ps1 -ApplicationDirectory artifacts\classic-resume -TestDirectory Tests\Snap.Net.ControlClient.Tests\bin\Release\net472 -LogPath artifacts\classic-resume-smoke-nlog.log
```

Requires Visual Studio Build Tools with .NET desktop/WPF build support, the
.NET Framework 4.7.2 targeting pack, a .NET SDK for the SDK-style control/test
projects, and access to the existing NuGet dependencies. The validation machine
has Build Tools 18, SDK 10.0.301 and the 4.7.2 targeting pack. Build the WPF
project directly; building the entire solution also requires unrelated workloads.

The new application is `artifacts\classic-resume\Snap.Net.exe`; keep the full output directory,
including its configuration, DLLs and `SnapClient` directory. The test runner is
`Tests\Snap.Net.ControlClient.Tests\bin\Release\net472\Snap.Net.ControlClient.Tests.exe`.
Optional environment variables `SNAP_CONTROL_TEST_FILTER` (name substring) and
`SNAP_CONTROL_TEST_TRACE=1` select cases and enable control trace output.

## Investigation

Baseline: commit `30670e3e1c76fd4f15421b66c1840d9cb5c77244`, tag `v0.35.0.2`.
The following are source defects or reproducible failure modes, rather than
proof of the cause of a particular workstation incident:

| Hypothesis | Evidence in the original code | Conclusion |
| --- | --- | --- |
| A: stale `TcpClient.Connected` | `SnapcastClient.IsConnected` and `_CheckConnectionAsync` only inspect the last known socket state. No request deadline tests the peer. | A silently unresponsive peer is reproduced by keeping the socket open and ignoring requests. The baseline fails to recover. This does not prove that the reported TCP states represented a half-open socket. |
| B: overlapping/terminated reconnect tasks | Every successful `ConnectAsync` calls `_StartReconnectLoop`, including connections made by the existing worker. The worker also recursively calls itself. Its cancellation token is never used in the loops or delays; disabling AutoReconnect can leave a tight loop. | Demonstrated lifecycle defects. One worker now waits on JSON-RPC completion, retries only while disconnected, and observes cancellation and errors. Explicit connects and retries share one gate. |
| C: failed or hanging RPC | `GetServerStatusAsync` has no deadline and only catches `ConnectionLostException`; event handlers discard `Task.Run` tasks. A connect task won by `WhenAny` is never awaited to observe its failure. | Demonstrated missing timeout/error paths. A hard local deadline closes the RPC transport, including when cancellation alone does not complete the RPC. Failed operations are observed; a valid RPC error resynchronizes without reconnecting. |
| D: subscriptions | `Config.SERVER_SetVolume` replaces the Volume object and invokes `SERVER_OnVolumeUpdated` before `_ClientVolumeChanged` attaches local/group handlers to the replacement. | A synchronous subscriber modifying volume during that notification reproduces the lost-handler window on the baseline. Keeping the object preserves the subscriptions. Duplication after reconnect was not established as the workstation cause; repeated cycles test for duplicate outgoing calls. |
| E: divergent local state | `Volume.CLIENT_ToggleMuted`, `CLIENT_SetPercent` and `Group.CLIENT_ToggleMuted` optimistically change the model before RPC success. Failed requests do not reconcile it. | Reproduced with silent mute/group-mute and rejected RPC requests. Reconnect obtains a full status and rebuilds client/group subscriptions and WPF listeners. |
| F: concurrent/stale slider RPC | Each event starts an independently scheduled task reading a mutable `client.config.volume`; response handlers update local state independently. | The original code admits reordering and mutable payload races. The fix snapshots volume/mute at the event and serializes outgoing commands and notification updates. Fifty delayed volume requests are checked for order, final value, single in-flight RPC and duplicates. |

The WPF path remains `VolumeControl` -> `Client`/`Group` handlers ->
`Volume`/`Group.CLIENT_*` -> `SnapcastClient` RPC -> server response/notification
-> model events -> Dispatcher UI update. `SnapControl.OnServerUpdated` rebuilds
the group controls after full status replacement. Slider handlers already detach
`OnVolumeChanged` during programmatic refresh. No WPF appearance, playback,
broadcast, wire format, dependency version, or existing public signature has
been changed. A control-only `ReconnectAsync()` entry point supports WPF resume.

Classic now subscribes to `SystemEvents.PowerModeChanged` and replaces the
control connection on Resume when auto-retry is enabled. The subscription is
removed on exit/disposal. This bypasses `Connect()`, which also starts autoplay
and broadcast. Concurrent resume requests share one attempt; an old in-flight
RPC is aborted before waiting for the connection gate. If the network is not
ready, the single recovery worker continues retrying. A non-resume interruption
is detected by the listener or the next request's three-second deadline.
Healthy idle connections have no heartbeat or polling.
Notifications are gated until initial status is available and ignored if their
RPC instance has been replaced. Old UI models cannot submit new commands.
Commands already submitted survive status refresh on the same connection; they
are discarded on replacement, avoiding stale volume writes or replayed stream
commands. Superseded RPC, stream and TCP resources are closed before replacement.

## Automated coverage and results

The runner covers normal volume/client mute/group mute, healthy idle, remote
close, silent established TCP, group-mute and status timeouts, reconnect and
resynchronization, stable client/group subscriptions, rapid delayed volume
requests, status refresh with queued commands, five reconnect cycles, a failing
subscriber, RPC rejection, initial refusal, initial status timeout, shutdown
during reconnect and queued operations, explicit concurrent connects, and
AutoReconnect toggling, proactive resume synchronization, coalesced resume
attempts, resume during an in-flight RPC, delayed network readiness, shutdown
during resume, and lifecycle trace output. It also observes background task
completion, released TCP sessions and unobserved task exceptions. The separate
WPF smoke test invokes the actual Resume handler, checks AutoReconnect=false,
and verifies that the real trace-to-NLog bridge writes lifecycle messages to an
isolated file while filtering per-slider debug traffic. It does not emit a real
Windows power event, run the application, play audio, or save user settings.

All 19 runner cases and the WPF/NLog smoke test pass on the validation machine.
Two cases were run against an
isolated copy of the unmodified baseline:
`volume subscriptions` failed with a detached-handler assertion, and
`established but unresponsive` failed with a recovery timeout. They pass with
the fix. The Classic Release build succeeds. Existing assembly binding redirect
warnings and CS4014 warnings in Player/BroadcastSettings are outside this fix.
Local build and test logs are in the ignored `artifacts` directory.

## Workstation validation still required

The real intermittent failure has **not** been demonstrated fixed. Automated
tests do not simulate Windows power transitions, a real network blackhole,
snapserver load, or the full WPF event loop under prolonged use.

1. Exit the installed Snap.Net through its tray menu. The application is single
   instance, so ensure the process has exited before launching the test build.
2. Start `artifacts\classic-resume\Snap.Net.exe` (test version 0.35.0.3). Check server `<snapserver-hostname>` or `<snapserver-ip-address>`, control port
   `1705`, and enable auto-retry in Settings. Check the actual selected settings;
   the test build may use a different per-user settings location.
3. Verify client and group mute/unmute and both volume sliders, including rapid
   dragging. Compare the resulting state with Home Assistant/Music Assistant.
4. Keep it running for several hours, including idle periods and repeated Windows
   sleep/resume cycles. After each resume, allow the network to return and check
   all client/group state is refreshed before any control click. Then verify
   mute and volume respond normally.
5. At a suitable time, repeat with a short workstation network interruption and
   verify recovery without restarting the application. Close the application
   while disconnected and verify it exits normally.
6. Record build/commit, uptime, sleep/resume timestamps, the first failing action,
   recovery time, server/controller state, and TCP evidence if failure returns.
   The existing `%LOCALAPPDATA%\SnapDotNet\SnapDotNet.log` now records Windows
   resume, control connection attempts/endpoints/durations, failures/timeouts,
   full status synchronization and client/group mute states. Routine slider
   traffic remains debug-only. The previous-session log is preserved by the
   existing NLog setup.

An idle silent connection without a Windows Resume event is detected on the next
request, rather than through periodic traffic. The first timed-out action is not replayed: local
state is reconciled to the server and the user may need to repeat it. Very large
slider bursts are ordered rather than coalesced, so they can queue behind server
latency. Existing `SnapControl` pane event subscriptions are not explicitly
removed when the pane closes; potential long-runtime retention is a separate
investigation, not an established cause of the reported control failure.
