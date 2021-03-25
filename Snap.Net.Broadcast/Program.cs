using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Snap.Net.Broadcast
{
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
                Device.PrintDevices(options.DataFlow);
                return 0;
            }

            MMDevice device = Device.GetDevice(options.SoundCard, options.DataFlow);
            if (device == null)
            {
                Console.WriteLine($"Invalid sound device index: {options.SoundCard}");
                Console.WriteLine("Available options are: ");
                Device.PrintDevices();
                return -1;
            }

            BroadcastController broadcast = new BroadcastController(device);
            await broadcast.RunAsync(options.HostName, options.Port);

            return 0;
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
}
