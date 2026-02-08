using Avalonia.Styling;
using Snap.Net.ControlClient.JsonRpcData;
using SnapDotNet.ControlClient.JsonRpcData;
namespace Snap.Net.Avalonia.Helpers;
#if DEBUG
public static class XamlPreviewHelpers
{
    public static ThemeVariant ThemeVariant => ThemeVariant.Dark;
    
    public static Stream GetStream(int idx)
    {
        return new Stream()
        {
            id = $"test {idx}",
            status =  idx % 2 == 0 ? "playing" : "idle",
            meta = new Meta()
            {
                STREAM = $"stream_id_{idx}"
            },
            properties = new Properties()
            {
                canControl = idx % 2 == 0,
                canGoNext = idx % 2 == 0,
                canGoPrevious = idx % 2 == 0,
                canPlay = idx % 2 == 0,
                canPause = idx % 2 == 0,
                canSeek = idx % 2 == 0,
                metadata = new  Metadata()
                {
                    album = $"some album {idx}",
                    artist = [$"some artist {idx}"],
                    albumArtist =  [$"some album artist {idx}"],
                    title = $"some song title {idx}",
                    duration = 5 * 60
                }
            },
            uri = new Uri()
            {
                scheme = "librespot",
                path = "//usr/bin/librespot",
                query = new Query()
                {
                    chunk_ms = "20",
                    codec = "flac",
                    name = "SpotifyStijn",
                    sampleformat = "44100:16:2"
                }
            }
        };
    }
    public static Group GetGroup(int idx, int numClients, int streamIdx)
    {
        Client[] clients = new  Client[numClients];
        for (int i = 0; i < numClients; i++)
        {
            clients[i] = GetClient(i);
        }

        return new Group()
        {
            id = $"group {idx} name",
            clients = clients,
            muted = idx % 2 == 0,
            name = $"Group {idx} name",
            stream_id = $"stream_id_{streamIdx}"
        };
    }
    public static Client GetClient(int idx)
    {
        return new Client()
        {
            id = $"client_id_{idx}",
            config = new Config()
            {
                instance = 0,
                latency = 0,
                name = $"Client {idx} name",
                volume = new Volume()
                {
                    muted = idx % 2 == 0,
                    percent = 10 * (idx + 1),
                }
            },
            connected = true,
            host = new Host()
            {
                arch = "linux-x64",
                ip = $"192.168.1.{111+idx}",
                mac = $"00:00:00:00:0{idx}",
                name = $"host {idx} name",
                os = "Linux Mint 22.3"
            },
            lastSeen = new Lastseen()
            {
                sec = 0,
                usec = 0
            },
            Name = $"Client {idx} name",
            snapclient = new Snapclient()
            {
                name = $"Snapclient {idx} name",
                protocolVersion = 1,
                version = "0.34.0",
            }
        };
    }
}
#endif