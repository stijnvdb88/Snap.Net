using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using SnapDotNet.Mobile.Droid.Player;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;
using Exception = Java.Lang.Exception;

namespace SnapDotNet.Mobile.Droid
{
    [Activity(Label = "Snapcast", Icon = "@drawable/ic_launcher", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, ISnapclientListener
    {
        internal static MainActivity Instance { get; private set; }

        private SnapclientServiceConnection m_SnapclientServiceConnection = null;
        private Action m_OnPlayStateChangedCallback = null;

        private SnapcastBroadcastReceiver m_AudioPlugChangedReceiver = new SnapcastBroadcastReceiver();

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
        }

        protected override void OnStart()
        {
            base.OnStart();
            m_SnapclientServiceConnection = new SnapclientServiceConnection(this);
            Intent intent = new Intent(this, typeof(SnapclientService));
            BindService(intent, m_SnapclientServiceConnection, Bind.AutoCreate);
            RegisterReceiver(m_AudioPlugChangedReceiver, new IntentFilter(AudioManager.ActionHeadsetPlug));
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                UnbindService(m_SnapclientServiceConnection);
                m_SnapclientServiceConnection.IsConnected = false;
            }

            UnregisterReceiver(m_AudioPlugChangedReceiver);
        }

        public void Play(string host, int port)
        {
            Intent i = new Intent(this, typeof(Player.SnapclientService));
            i.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            i.PutExtra(SnapclientService.EXTRA_HOST, host);
            i.PutExtra(SnapclientService.EXTRA_PORT, port);
            i.SetAction(SnapclientService.ACTION_START);

            StartService(i);
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

        public bool IsPlaying()
        {
            if (m_SnapclientServiceConnection != null && m_SnapclientServiceConnection.IsConnected)
            {
                return m_SnapclientServiceConnection.Player.IsPlaying();
            }

            return false;
        }

        public void OnPlayStateChangedCallback(Action callback)
        {
            m_OnPlayStateChangedCallback = callback;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void OnPlayStateChanged(SnapclientService service)
        {
            m_OnPlayStateChangedCallback?.Invoke();
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
    }
}