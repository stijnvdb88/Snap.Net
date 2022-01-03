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
using Android.Media.Projection;
using Android.Net;
using Android.Net.Wifi;
using Android.Support.V4.App;
using Java.IO;
using Java.Security;
using SnapDotNet.Mobile.Common;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;
using Console = System.Console;
using IOException = Java.IO.IOException;
using TaskStackBuilder = Android.App.TaskStackBuilder;

namespace SnapDotNet.Mobile.Droid.Player
{
    [Service(Exported = true, ForegroundServiceType = ForegroundService.TypeMediaProjection)]
    public class BroadcastService : ServiceBase
    {
        private const int NOTIFICATION_CHANNEL = 667;
        public const string EXTRA_RESULT_DATA = "BROADCAST_SERVICE_EXTRA_RESULT_DATA";
        public const string EXTRA_BROADCAST_MODE = "BROADCAST_SERVICE_EXTRA_BROADCAST_MODE";
        private IBinder m_Binder;
        private ISnapclientListener m_SnapclientListener = null;

        private bool m_Running = false;

        private MediaProjection m_MediaProjection = null;
        private MediaProjectionManager m_MediaProjectionManager = null;

        private AudioRecord m_AudioRecord = null;
        private ClientConnection m_ClientConnection = null;

        private Thread m_RecordThread = null;
        private Thread m_ConnectionThread = null;

        private EBroadcastMode m_BroadcastMode = EBroadcastMode.Media;

        public void Init()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Q) // broadcast is only supported on Android 10 and up
            {
                return;
            }

            NotificationManager notificationManager =
                (NotificationManager)Application.Context.GetSystemService(Application.NotificationService);
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID,
                "Snapclient service", NotificationImportance.Low);
            channel.Description = "Snapcast broadcast service";
            notificationManager.CreateNotificationChannel(channel);

            m_MediaProjectionManager = (MediaProjectionManager)Application.Context.GetSystemService(MediaProjectionService);
        }


        public bool IsBroadcasting()
        {
            return m_Running;
        }

        public override IBinder OnBind(Intent intent)
        {
            m_Binder = new LocalBinder(this);
            return m_Binder;
        }

        public void SetListener(ISnapclientListener listener)
        {
            m_SnapclientListener = listener;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent == null)
                return StartCommandResult.NotSticky;

            if (intent.Action == ACTION_STOP)
            {
                Stop();
                return StartCommandResult.NotSticky;
            }
            else if (intent.Action == ACTION_START)
            {
                string host = intent.GetStringExtra(EXTRA_HOST);
                int port = intent.GetIntExtra(EXTRA_PORT, 1704);
                EBroadcastMode broadcastMode = (EBroadcastMode) intent.GetIntExtra(EXTRA_BROADCAST_MODE, (int)EBroadcastMode.Media);

                Intent stopIntent = new Intent(this, typeof(SnapclientService));
                stopIntent.SetAction(ACTION_STOP);
                PendingIntent PiStop = PendingIntent.GetService(this, 0, stopIntent, 0);

                NotificationCompat.Builder builder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                    .SetSmallIcon(Resource.Drawable.ic_media_play)
                    .SetTicker("Snap.Net.Broadcast started")
                    .SetContentTitle("Snap.Net Broadcast")
                    .SetContentText("Snap.Net.Broadcast is running...")
                    .SetContentInfo(string.Format("{0}:{1}", host, port))
                    .SetStyle(new NotificationCompat.BigTextStyle().BigText("Snap.Net.Broadcast is running..."))
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

                m_MediaProjection =
                    m_MediaProjectionManager.GetMediaProjection((int)Result.Ok,
                        intent.GetParcelableExtra(EXTRA_RESULT_DATA) as Intent);

                _Start(host, port, broadcastMode);

                return StartCommandResult.Sticky;
            }

            return StartCommandResult.NotSticky;
        }

        private void _Start(string host, int port, EBroadcastMode broadcastMode)
        {
            try
            {
                if (m_Running)
                    return;

                Android.OS.Process.SetThreadPriority(ThreadPriority.UrgentAudio);

                PowerManager powerManager = (PowerManager)GetSystemService(PowerService);
                m_WakeLock =
                    powerManager.NewWakeLock(WakeLockFlags.Partial,
                        "snapdotnet:SnapcastPartialWakeLock");
                m_WakeLock.Acquire();

                WifiManager wifiManager = (WifiManager)GetSystemService(WifiService);
                m_WifiLock = wifiManager.CreateWifiLock(WifiMode.FullHighPerf, "snapdotnet:SnapcastWifiWakeLock");
                m_WifiLock.Acquire();

                m_Host = host;
                m_Port = port;
                m_BroadcastMode = broadcastMode;

                // actually connect here and start audio loop
                AudioPlaybackCaptureConfiguration config = new AudioPlaybackCaptureConfiguration.Builder(m_MediaProjection)
                    .AddMatchingUsage(AudioUsageKind.Media)
                    .Build();

                AudioFormat audioFormat = new AudioFormat.Builder()
                    .SetEncoding(Encoding.Pcm16bit)
                    .SetSampleRate(48000)
                    .SetChannelMask(ChannelOut.Stereo).Build();

                AudioRecord.Builder builder = new AudioRecord.Builder()
                    .SetAudioFormat(audioFormat)
                    .SetBufferSizeInBytes(2048);

                if (m_BroadcastMode == EBroadcastMode.Media)
                {
                    builder.SetAudioPlaybackCaptureConfig(config);
                }

                if (m_BroadcastMode == EBroadcastMode.Microphone)
                {
                    builder.SetAudioSource(AudioSource.Mic);
                }

                m_AudioRecord = builder.Build();


                m_ConnectionThread = new Thread(() =>
                {
                    m_ClientConnection = new ClientConnection(m_Host, m_Port);
                    m_ClientConnection.OnConnected += _OnClientConnectionConnected;
                    m_ClientConnection.Connect();
                });

                m_ConnectionThread.Start();


                m_Running = true;
                if (m_SnapclientListener != null)
                    m_SnapclientListener.OnBroadcastStateChanged(this);
            }
            catch (Exception e)
            {
                e.PrintStackTrace();
                if (m_SnapclientListener != null)
                    m_SnapclientListener.OnError(this, e.Message, e);
                _Stop();
            }
        }

        private void _OnClientConnectionConnected(bool connected)
        {
            if (connected == false)
            {
                m_ClientConnection.Connect();
            }
            else
            {
                if (m_RecordThread == null)
                {
                    m_AudioRecord.StartRecording();
                    m_RecordThread = new Thread(() =>
                    {
                        byte[] data = new byte[2048];
                        
                        while (m_RecordThread.IsInterrupted == false)
                        {
                            m_AudioRecord.Read(data, 0, data.Length);
                            m_ClientConnection.Write(data, data.Length);
                        }

                    });
                    m_RecordThread.Start();
                }
            }
        }

        public void Stop()
        {
            _Stop();
            StopForeground(true);

            NotificationManager mNotificationManager =
                (NotificationManager)GetSystemService(Context.NotificationService);
            mNotificationManager.Cancel(NOTIFICATION_CHANNEL);
        }

        private void _Stop()
        {
            if (m_Running == false)
                return;

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


            m_RecordThread.Interrupt();
            m_RecordThread.Join();
            m_AudioRecord.Stop();
            m_AudioRecord.Release();
            m_AudioRecord = null;

            m_ConnectionThread.Interrupt();
            m_ConnectionThread.Join();

            m_ClientConnection.Dispose();
            m_ClientConnection = null;

            m_MediaProjection.Stop();
            m_RecordThread = null;
            m_ConnectionThread = null;

            if (m_SnapclientListener != null)
                m_SnapclientListener.OnBroadcastStateChanged(this);
        }

        public override void OnDestroy()
        {
            _Stop();
        }


        public class LocalBinder : Binder
        {
            private BroadcastService m_Instance = null;
            public LocalBinder(BroadcastService broadcastService)
            {
                m_Instance = broadcastService;
            }

            public BroadcastService GetService()
            {
                return m_Instance;
            }
        }
    }

    public class BroadcastServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(BroadcastServiceConnection).FullName;

        MainActivity m_MainActivity;
        public bool IsConnected { get; set; }
        public BroadcastService.LocalBinder Binder { get; private set; }
        public BroadcastService BroadcastService { get; private set; }

        public BroadcastServiceConnection(MainActivity activity)
        {
            IsConnected = false;
            Binder = null;
            m_MainActivity = activity;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = (BroadcastService.LocalBinder)service;
            BroadcastService = Binder.GetService();
            BroadcastService.Init();
            IsConnected = true;
            BroadcastService.SetListener(m_MainActivity);

            //// force broadcast state update (UI sometimes tries to check it while service not bound - this make sure it gets updated once we are bound)
            m_MainActivity.OnBroadcastStateChanged(BroadcastService);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            IsConnected = false;
        }
    }

}