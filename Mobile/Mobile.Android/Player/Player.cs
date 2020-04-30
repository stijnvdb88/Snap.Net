using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Application = Android.App.Application;
using Process = Java.Lang.Process;
using Android.Content.PM;
using System.Threading.Tasks;
using System.IO;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;

[assembly: Dependency(typeof(SnapDotNet.Mobile.Droid.Player.Player))]
namespace SnapDotNet.Mobile.Droid.Player
{
    public class Player : IPlayer
    {
        public bool SupportsSnapclient()
        {
            return true;
        }

        public async void PlayAsync(string host, int port)
        {
         
            await RunCommand(Path.Combine(Android.App.Application.Context.ApplicationInfo.NativeLibraryDir,
                "libsnapclient.so"), "-h", host, "-p", port.ToString(), "--player", "oboe");
        }

        async Task<(int exitCode, string result)> RunCommand(params string[] command)
        {
            string result = null;
            var exitCode = -1;

            try
            {
                Android.OS.Process.SetThreadPriority(ThreadPriority.Audio);
                var builder = new ProcessBuilder(command);
                var process = builder.Start();
                exitCode = await process.WaitForAsync();

                if (exitCode == 0)
                {
                    using (var inputStreamReader = new StreamReader(process.InputStream))
                    {
                        result = await inputStreamReader.ReadToEndAsync();
                    }
                }
                else if (process.ErrorStream != null)
                {
                    using (var errorStreamReader = new StreamReader(process.ErrorStream))
                    {
                        var error = await errorStreamReader.ReadToEndAsync();
                        result = $"Error {error}";
                    }
                }
            }
            catch (IOException ex)
            {
                result = $"Exception {ex.Message}";
            }

            return (exitCode, result);
        }

        public void Stop()
        {

        }
    }
}