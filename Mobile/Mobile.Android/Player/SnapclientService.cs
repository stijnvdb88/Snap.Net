
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Java.Lang;
using Application = Android.App.Application;
using Process = Java.Lang.Process;
using Android.Content.PM;
using System.Threading.Tasks;
using System.IO;
using System.Linq.Expressions;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.Support.V4.App;
using Java.IO;
using Java.Security;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;
using IOException = Java.IO.IOException;
using TaskStackBuilder = Android.App.TaskStackBuilder;

namespace SnapDotNet.Mobile.Droid.Player
{
    /// <summary>
    /// ported SnapclientService.java from badaix/snapdroid :-)
    /// </summary>
    ///
    [Service]
    public class SnapclientService : Service
    {
        public static string EXTRA_HOST = "EXTRA_HOST";
        public static string EXTRA_PORT = "EXTRA_PORT";
        public static string ACTION_START = "ACTION_START";
        public static string ACTION_STOP = "ACTION_STOP";
        private const string NOTIFICATION_CHANNEL_ID = "com.stijnvdb88.snapcast.snapclientservice.defaultchannel";
        private const int NOTIFICATION_CHANNEL = 666;

        private IBinder m_Binder;
        private Process m_Process = null;
        private PowerManager.WakeLock m_WakeLock = null;
        private WifiManager.WifiLock m_WifiLock = null;
        private Thread m_Reader = null;

        private bool m_Running = false;

        private ISnapclientListener m_SnapclientListener = null;
        private bool m_LogReceived = false;
        private Handler m_RestartHandler = new Handler();
        private Runnable m_RestartRunnable = null;
        private string m_Host = "";
        private int m_Port = 1704;

        public void Init()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            NotificationManager notificationManager =
                (NotificationManager) Application.Context.GetSystemService(Application.NotificationService);
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID,
                "Snapclient service", NotificationImportance.Low);
            channel.Description = "Snapcast player service";
            notificationManager.CreateNotificationChannel(channel);

            m_RestartRunnable = new Runnable(() =>
            {
                _StopProcess();
                try
                {
                    _StartProcess();
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                }
            });
        }

        public void Restart()
        {
            if (m_RestartHandler != null)
            {
                m_RestartHandler.Post(m_RestartRunnable);
            }
        }

        public void SetListener(ISnapclientListener listener)
        {
            m_SnapclientListener = listener;
        }

        public bool IsPlaying()
        {
            return m_Running;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent == null)
                return StartCommandResult.NotSticky;

            if (intent.Action == ACTION_STOP)
            {
                _StopService();
                return StartCommandResult.NotSticky;
            }
            else if (intent.Action == ACTION_START)
            {
                string host = intent.GetStringExtra(EXTRA_HOST);
                int port = intent.GetIntExtra(EXTRA_PORT, 1704);

                Intent stopIntent = new Intent(this, typeof(SnapclientService));
                stopIntent.SetAction(ACTION_STOP);
                PendingIntent PiStop = PendingIntent.GetService(this, 0, stopIntent, 0);

                NotificationCompat.Builder builder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                    .SetSmallIcon(Resource.Drawable.ic_media_play)
                    .SetTicker("Snapclient started")
                    .SetContentTitle("Snapclient")
                    .SetContentText("Snapclient is running...")
                    .SetContentInfo(string.Format("{0}:{1}", host, port))
                    .SetStyle(new NotificationCompat.BigTextStyle().BigText("Snapclient is running..."))
                    .AddAction(Resource.Drawable.ic_media_stop, "Stop", PiStop); 

                Intent resultIntent = new Intent(this, typeof(MainActivity));

                TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
                stackBuilder.AddParentStack(Class.FromType(typeof(MainActivity)));

                stackBuilder.AddNextIntent(resultIntent);
                PendingIntent resultPendingIntent =
                    stackBuilder.GetPendingIntent(
                        0,
                        PendingIntentFlags.UpdateCurrent
                    );

                builder.SetContentIntent(resultPendingIntent);
                Notification notification = builder.Build();

                StartForeground(NOTIFICATION_CHANNEL, notification);

                _Start(host, port);

                return StartCommandResult.Sticky;
            }

            return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            _Stop();
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

        public void Stop()
        {
            _StopService();
        }

        private void _StopService()
        {
            _Stop();
            StopForeground(true);

            NotificationManager mNotificationManager =
                (NotificationManager) GetSystemService(Context.NotificationService);
            mNotificationManager.Cancel(NOTIFICATION_CHANNEL);
        }

        private void _StartProcess()
        {
            string sampleformat = "*:16:*";
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
                sampleformat = string.Format("{0}:16:*", rate);
            }

            ProcessBuilder pb = new ProcessBuilder()
                .Command(
                    Path.Combine(Android.App.Application.Context.ApplicationInfo.NativeLibraryDir, "libsnapclient.so"),
                    "-h", m_Host,
                    "-p", Integer.ToString(m_Port), "--player", player, "--sampleformat", sampleformat)
                .RedirectErrorStream(true);

            Dictionary<string, string> env = new Dictionary<string, string>();
            env.Add("SAMPLE_RATE", rate);
            env.Add("FRAMES_PER_BUFFER", fpb);

            m_Process = pb.Start();

            m_Reader = new Thread(new Runnable(() =>
            {
                BufferedReader bufferedReader = new BufferedReader(
                    new InputStreamReader(m_Process.InputStream));
                try
                {
                    string line;
                    while ((line = bufferedReader.ReadLine()) != null)
                    {
                        _Log(line);
                    }
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                }
            }));
            m_LogReceived = false;
            m_Reader.Start();
        }

        private void _Start(string host, int port)
        {
            try
            {
                if (m_Running)
                    return;

                Android.OS.Process.SetThreadPriority(ThreadPriority.UrgentAudio);

                PowerManager powerManager = (PowerManager) GetSystemService(PowerService);
                m_WakeLock =
                    powerManager.NewWakeLock(WakeLockFlags.Partial,
                        "snapdotnet:SnapcastPartialWakeLock");
                m_WakeLock.Acquire();

                WifiManager wifiManager = (WifiManager) GetSystemService(WifiService);
                m_WifiLock = wifiManager.CreateWifiLock(WifiMode.FullHighPerf, "snapdotnet:SnapcastWifiWakeLock");
                m_WifiLock.Acquire();

                m_Host = host;
                m_Port = port;

                _StartProcess();
            }
            catch (Exception e)
            {
                e.PrintStackTrace();
                if(m_SnapclientListener != null)
                    m_SnapclientListener.OnError(this, e.Message, e);
                _Stop();
            }
        }

        private void _Log(string msg)
        {
            if (m_LogReceived == false)
            {
                m_LogReceived = true;
                m_Running = true;
                if (m_SnapclientListener != null)
                    m_SnapclientListener.OnPlayStateChanged(this);
            }

            if (m_SnapclientListener != null)
            {
                //int idxSeverityOpen = msg.IndexOf('[');
                //int idxSeverityClose = msg.IndexOf(']', idxSeverityOpen);
                //int idxTagOpen = msg.IndexOf('(', idxSeverityClose);
                //int idxTagClose = msg.IndexOf(')', idxTagOpen);
                //if ((idxSeverityOpen > 0) && (idxSeverityClose > 0))
                //{
                //    string severity = msg.Substring(idxSeverityOpen + 1, idxSeverityClose);
                //    string tag = "";
                //    if ((idxTagOpen > 0) && (idxTagClose > 0))
                //        tag = msg.Substring(idxTagOpen + 1, idxTagClose);
                //    string timestamp = msg.Substring(0, idxSeverityOpen - 1);
                //    string message = msg.Substring(Math.Max(idxSeverityClose, idxTagClose) + 2);
                //    m_SnapclientListener.OnLog(this, timestamp, severity, tag, message);

                //    if ((message.Equals("Init start")) && (m_RestartRunnable == null))
                //    {
                //        m_RestartRunnable = new Runnable(() =>
                //        {
                //            _StopProcess();
                //            try
                //            {
                //                _StartProcess();
                //            }
                //            catch (IOException e)
                //            {
                //                e.PrintStackTrace();
                //            }
                //        });
                //        m_RestartHandler.PostDelayed(m_RestartRunnable, 3000);
                //    }
                //    else if ((message.Contains("Init failed")) && (m_RestartRunnable != null))
                //    {
                //        m_RestartHandler.RemoveCallbacks(m_RestartRunnable);
                //        m_RestartHandler.Post(m_RestartRunnable);
                //    }
                //    else if ((message.Equals("Init done")) && (m_RestartRunnable != null))
                //    {
                //        m_RestartHandler.RemoveCallbacks(m_RestartRunnable);
                //        m_RestartRunnable = null;
                //    }
                //}
            }
        }



        private void _StopProcess()
        {
            try
            {
                if (m_Reader != null)
                {
                    m_Reader.Interrupt();
                }

                if (m_Process != null)
                {
                    m_Process.Destroy();
                }
            }
            catch (Exception e)
            {
                e.PrintStackTrace();
            }
        }

        private void _Stop()
        {
            try
            {
                _StopProcess();
                if (m_WakeLock != null && m_WakeLock.IsHeld)
                {
                    m_WakeLock.Release();
                }

                if (m_WifiLock != null && m_WifiLock.IsHeld)
                {
                    m_WifiLock.Release();
                }

                Android.OS.Process.SetThreadPriority(ThreadPriority.Default);
                m_Running = false;
            }
            catch (Exception e)
            {
                e.PrintStackTrace();
            }

            if (m_SnapclientListener != null)
                m_SnapclientListener.OnPlayStateChanged(this);
        }
    }

    public class LocalBinder : Binder
    {
        private SnapclientService m_Instance = null;
        public LocalBinder(SnapclientService player)
        {
            m_Instance = player;
        }

        public SnapclientService GetService() 
        {
            return m_Instance;
        }
    }

    public interface ISnapclientListener
    {
        void OnPlayStateChanged(SnapclientService snapclientService);

        void OnLog(SnapclientService snapclientService, string timestamp, string logClass, string tag, string msg);

        void OnError(SnapclientService snapclientService, string msg, Exception exception);
    }

    public class SnapclientServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(SnapclientServiceConnection).FullName;

        MainActivity m_MainActivity;
        public bool IsConnected { get; set; }
        public LocalBinder Binder { get; private set; }
        public SnapclientService Player { get; private set; }

        public SnapclientServiceConnection(MainActivity activity)
        {
            IsConnected = false;
            Binder = null;
            m_MainActivity = activity;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = (LocalBinder) service;
            Player = Binder.GetService();
            Player.Init();
            IsConnected = true;
            Player.SetListener(m_MainActivity);

            // force play state update (UI sometimes tries to check it while service not bound - this make sure it gets updated once we are bound)
            m_MainActivity.OnPlayStateChanged(Player); 
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            IsConnected = false;
        }
    }
}