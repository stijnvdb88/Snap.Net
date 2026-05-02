using CommandLine;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Broadcast;

class Program
{
    
    static int Main(string[] args)
    {
        Task<int> result = Parser.Default.ParseArguments<Options>(args)
            .MapResult(_RunAsync, _HandleParseErrorAsync);

        return result.Result;
    }
    
    static async Task<int> _RunAsync(Options options)
    {
        if (options.ListDevices)
        {
            _PrintDevices(options.Type);
            return 0;
        }
        
        IAudioDevice? device = Device.GetDevice(options.SoundCard, options.Type);
        if (device == null)
        {
            Console.WriteLine($"Invalid sound device index: {options.SoundCard}");
            Console.WriteLine("Available options are: ");
            _PrintDevices(options.Type);
            return -1;
        }

        BroadcastController broadcast = new BroadcastController(device);
        await broadcast.RunAsync(options.HostName, options.Port);

        return 0;
    }

    private static void _PrintDevices(EDeviceType type)
    {
        IAudioDevice[] devices = Device.GetDevices(type).ToArray();
        for (int i = 0; i < devices.Length; i++)
        {
            Console.WriteLine($"{i}: {devices[i].FriendlyName}");
        }
    }


    private static async Task<int> _HandleParseErrorAsync(IEnumerable<Error> errs)
    {
        //handle command line option errors
        foreach (Error e in errs)
        {
            if (e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError)
            {
                Console.WriteLine(e.Tag);
            }
        }

        return -1;
    }
}