
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Java.Lang;
using Application = Android.App.Application;
using Process = Java.Lang.Process;
using Android.Content.PM;
using System.Threading.Tasks;
using System.IO;
using Android.Content;
using Android.Media;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;

[assembly: Dependency(typeof(SnapDotNet.Mobile.Droid.Player.Player))]
namespace SnapDotNet.Mobile.Droid.Player
{
    /// <summary>
    /// ported SnapclientService.java from badaix/snapdroid :-)
    /// </summary>
    ///
    [Service]
    public class Player : Service, IPlayer
    {
        private IBinder m_Binder;
        private const string NOTIFICATION_CHANNEL_ID = "com.stijnvdb88.snapcast.snapclientservice.defaultchannel";
        private Process m_Process = null;
        private bool m_Running = false;

        public void Init()
        {
            if (Build.VERSION.SdkInt < Build.VERSION_CODES.O)
            {
                return;
            }

            NotificationManager notificationManager =
                (NotificationManager) Application.Context.GetSystemService(Application.NotificationService);
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, 
                "Snapclient service", NotificationImportance.Low);
            channel.Description = "Snapcast player service";
            notificationManager.CreateNotificationChannel(channel);
        }

        public bool IsPlaying()
        {
            return m_Running;
        }

        public override IBinder OnBind(Intent intent)
        {
            m_Binder = new LocalBinder(this);
            return m_Binder;
        }


        public bool SupportsSnapclient()
        {
            return true;
        }


        public async void PlayAsync(string host, int port)
        {

            string sampleformat = "*:16:2";
            string player = "oboe";
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                player = "opensl";
            }

            AudioManager am = (AudioManager) Application.Context.GetSystemService(Application.AudioService);
            string rate = "";
            string fpb = "";
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
            {
                rate = am.GetProperty(AudioManager.PropertyOutputSampleRate);
                fpb = am.GetProperty(AudioManager.PropertyOutputFramesPerBuffer);
                sampleformat = string.Format("{0}:16:2", rate);
            }

            
            Dictionary<string, string> env = new Dictionary<string, string>();
            env.Add("SAMPLE_RATE", rate);
            env.Add("FRAMES_PER_BUFFER", fpb);

            m_Running = true; // FIX: only set this to true after receiving logs from process output! (see snapdroid)

            await RunService(env, Path.Combine(Android.App.Application.Context.ApplicationInfo.NativeLibraryDir,
                "libsnapclient.so"), "-h", host, "-p", port.ToString(), "--player", player);//, "--sampleformat", sampleformat);
        }

        async Task<(int exitCode, string result)> RunService(Dictionary<string, string> environment, params string[] command)
        {
            string result = null;
            var exitCode = -1;

            try
            {
                Android.OS.Process.SetThreadPriority(ThreadPriority.Audio);
                var builder = new ProcessBuilder(command);

                IDictionary<string, string> env = builder.Environment();
                foreach(KeyValuePair<string, string> kvp in environment)
                {
                    env.Add(kvp.Key, kvp.Value);
                }

                m_Process = builder.Start();
                exitCode = await m_Process.WaitForAsync();

                if (exitCode == 0)
                {
                    using (var inputStreamReader = new StreamReader(m_Process.InputStream))
                    {
                        result = await inputStreamReader.ReadToEndAsync();
                    }
                }
                else if (m_Process.ErrorStream != null)
                {
                    using (var errorStreamReader = new StreamReader(m_Process.ErrorStream))
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
            m_Process.Destroy();
            m_Running = false;
        }
    }

    public class LocalBinder : Binder
    {
        private Player m_Instance = null;
        public LocalBinder(Player player)
        {
            m_Instance = player;
        }

        Player getService() {
            return m_Instance;
        }
    }
}