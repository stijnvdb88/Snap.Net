using System;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Media.Projection;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using SnapDotNet.Mobile.Droid.Player;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;
using Exception = Java.Lang.Exception;
using Xamarin.Essentials;

namespace SnapDotNet.Mobile.Droid
{
    [Activity(Label = "Snap.Net", Icon = "@drawable/ic_launcher", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(
        actions: new string[] { "android.intent.action.MAIN" },
        Categories = new string[] { "android.intent.category.LEANBACK_LAUNCHER" })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, ISnapclientListener
    {
        internal static MainActivity Instance { get; private set; }

        private SnapclientServiceConnection m_SnapclientServiceConnection = null;
        private BroadcastServiceConnection m_BroadcastServiceConnection = null;
        private Action m_OnPlayStateChangedCallback = null;
        private Action m_OnBroadcastStateChangedCallback = null;

        private SnapcastBroadcastReceiver m_AudioPlugChangedReceiver = new SnapcastBroadcastReceiver();

        private MediaProjectionManager m_MediaProjectionmanager = null;

        private const int RECORD_AUDIO_PERMISSION_REQUEST_CODE = 42;
        private const int MEDIA_PROJECTION_REQUEST_CODE = 13;

        private string m_BroadcastHost;
        private int m_BroadcastPort;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // sborght: workaround for "Unable to activate instance of type LabelRenderer from native handle" exceptions in GroupEditPage
            // see https://github.com/xamarin/Xamarin.Forms/issues/2444
            global::Xamarin.Forms.Forms.SetFlags("UseLegacyRenderers"); 


            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
            DependencyService.Register<IPlayer>();
            RegisterReceiver(m_AudioPlugChangedReceiver, new IntentFilter(AudioManager.ActionHeadsetPlug));
        }

        protected override void OnStart()
        {
            base.OnStart();
            m_SnapclientServiceConnection = new SnapclientServiceConnection(this);

            Intent playerIntent = new Intent(this, typeof(SnapclientService));
            BindService(playerIntent, m_SnapclientServiceConnection, Bind.AutoCreate);

            m_BroadcastServiceConnection = new BroadcastServiceConnection(this);
            Intent broadcastServiceIntent = new Intent(this, typeof(BroadcastService));
            BindService(broadcastServiceIntent, m_BroadcastServiceConnection, Bind.AutoCreate);
        }

        private bool _HasRecordAudioPermission()
        {
            return ContextCompat.CheckSelfPermission(this, Manifest.Permission.RecordAudio) == Permission.Granted;
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                UnbindService(m_SnapclientServiceConnection);
                m_SnapclientServiceConnection.IsConnected = false;
            }

            if (m_BroadcastServiceConnection != null && m_BroadcastServiceConnection.IsConnected)
            {
                UnbindService(m_BroadcastServiceConnection);
                m_BroadcastServiceConnection.IsConnected = false;
            }


            //UnregisterReceiver(m_AudioPlugChangedReceiver);
        }

        public void Play(string host, int port)
        {
            Intent i = new Intent(this, typeof(Player.SnapclientService));
            i.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            i.PutExtra(ServiceBase.EXTRA_HOST, host);
            i.PutExtra(ServiceBase.EXTRA_PORT, port);
            i.SetAction(ServiceBase.ACTION_START);

            StartService(i);
        }

        public void Broadcast(string host, int port)
        {
            m_BroadcastHost = host;
            m_BroadcastPort = port;

            if (_HasRecordAudioPermission() == false)
            {
                ActivityCompat.RequestPermissions(this, new [] { Manifest.Permission.RecordAudio}, RECORD_AUDIO_PERMISSION_REQUEST_CODE);
            }
            else
            {
                _StartMediaProjectionRequest();
            }

        }

        private void _StartMediaProjectionRequest()
        {
            m_MediaProjectionmanager = Android.App.Application.Context.GetSystemService(Context.MediaProjectionService) as MediaProjectionManager;
            StartActivityForResult(m_MediaProjectionmanager.CreateScreenCaptureIntent(), MEDIA_PROJECTION_REQUEST_CODE);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode != MEDIA_PROJECTION_REQUEST_CODE)
            {
                return;
            }

            if (resultCode == Result.Ok)
            {
                Toast.MakeText(this, "MediaProjection permission granted. Audio capture starting...", ToastLength.Short)?.Show();
            }

            Intent i = new Intent(this, typeof(BroadcastService));
            i.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            i.PutExtra(ServiceBase.EXTRA_HOST, m_BroadcastHost);
            i.PutExtra(ServiceBase.EXTRA_PORT, m_BroadcastPort);
            i.PutExtra(BroadcastService.EXTRA_RESULT_DATA, data);
            i.SetAction(ServiceBase.ACTION_START);
            StartForegroundService(i);
        }

        public void Restart()
        {
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                m_SnapclientServiceConnection.Player.Restart();
            }
        }

        public void Stop()
        {
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                m_SnapclientServiceConnection.Player.Stop();
            }
        }


        public void StopBroadcast()
        {
            if (m_BroadcastServiceConnection != null && m_BroadcastServiceConnection.IsConnected)
            {
                m_BroadcastServiceConnection.BroadcastService.Stop();
            }
        }

        public bool IsPlaying()
        {
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                return m_SnapclientServiceConnection.Player.IsPlaying();
            }

            return false;
        }

        public bool IsBroadcasting()
        {
            if (m_BroadcastServiceConnection != null && m_BroadcastServiceConnection.IsConnected)
            {
                return m_BroadcastServiceConnection.BroadcastService.IsBroadcasting();
            }

            return false;
        }

        public void OnPlayStateChangedCallback(Action callback)
        {
            m_OnPlayStateChangedCallback = callback;
        }

        public void OnBroadcastStateChangedCallback(Action callback)
        {
            m_OnBroadcastStateChangedCallback = callback;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RECORD_AUDIO_PERMISSION_REQUEST_CODE)
            {
                if (grantResults[0] == Permission.Granted)
                {
                    Toast.MakeText(this, "Permission to capture audio granted. Please tap the Broadcast button again to start broadcasting.", ToastLength.Long)?.Show();
                }
                else
                {
                    Toast.MakeText(this, "Permission to capture audio denied", ToastLength.Short)?.Show();
                }
            }
        }

        public void OnPlayStateChanged(SnapclientService service)
        {
            m_OnPlayStateChangedCallback?.Invoke();
        }

        public void OnBroadcastStateChanged(BroadcastService service)
        {
            m_OnBroadcastStateChangedCallback?.Invoke();
        }

        public void OnLog(SnapclientService snapclientService, string timestamp, string logClass, string tag,
            string msg)
        {
            if ("err".Equals(logClass) || "Emerg".Equals(logClass) || "Alert".Equals(logClass) || "Crit".Equals(logClass) || "Err".Equals(logClass) || "Error".Equals(logClass))
            {
                //if (warningSamplerateSnackbar != null)
                //    warningSamplerateSnackbar.dismiss();
                //warningSamplerateSnackbar = Snackbar.make(findViewById(R.id.myCoordinatorLayout),
                //    msg, Snackbar.LENGTH_LONG);
                //warningSamplerateSnackbar.show();
            }
        }

        public void OnError(SnapclientService snapclientService, string msg, Exception exception)
        {
            m_OnPlayStateChangedCallback?.Invoke();
        }

        public void OnError(BroadcastService broadcastService, string msg, Exception exception)
        {
            m_OnBroadcastStateChangedCallback?.Invoke();
        }
    }
}