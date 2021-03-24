/*
    This file is part of Snap.Net
    Copyright (C) 2020  Stijn Van der Borght
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Win32;
using NAudio.Wave.Compression;
using Snap.Net.SnapClient;
using Snap.Net.SnapClient.Player;

namespace SnapClient.Net
{
    class Program
    {
        private static System.Version s_Version = null;

        static async Task<int> Main(string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            s_Version = assembly.GetName().Version;

            Task<int> result = Parser.Default.ParseArguments<Options>(args)
                .MapResult(_RunAsync, _HandleParseErrorAsync);

            return result.Result;
        }

        static async Task<int> _RunAsync(Options options)
        {
            // https://github.com/naudio/NAudio/blob/master/Docs/OutputDeviceTypes.md
            Device device = _GetDevice(options);

            if (options.ListDevices)
            {
                string[] deviceList = device.List().ToArray();
                foreach (string d in deviceList)
                {
                    Console.WriteLine(d);
                }

                return 0;
            }

            HelloMessage helloMessage = new HelloMessage(_GetMacAddress(), _GetOS(), options.Instance);
            helloMessage.Version = string.Join(".", s_Version.Major, s_Version.Minor, s_Version.Build);
            Player player = new NAudioPlayer(options.DacLatency, options.BufferDurationMs, options.OffsetToleranceMs, device.DeviceFactory);
            Controller controller = new Controller(player, helloMessage);

            await controller.StartAsync(options.HostName, options.Port);
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

        private static Device _GetDevice(Options options)
        {
            // todo: use reflection for this? maybe a bit overkill...
            switch (options.Player)
            {
                case Options.EPlayer.DirectSound:
                    return new DirectSoundDevice(options);
                case Options.EPlayer.Wasapi:
                    return new WasapiDevice(options);
                case Options.EPlayer.WaveOut:
                    return new WaveOutDevice(options);
            }

            return null;
        }

        private static string _GetMacAddress()
        {
            byte[] macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().GetAddressBytes())
                .FirstOrDefault();
            return string.Join(":", (from z in macAddress select z.ToString("X2")).ToArray()).ToLower();
        }

        private static string _GetOS()
        {
            return (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion", "ProductName", null);
        }
    }
}
