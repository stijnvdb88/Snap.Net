using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SnapDotNet.Mobile.Droid.Player
{
    public abstract class ServiceBase : Service
    {
        public static string ACTION_START = "ACTION_START";
        public static string ACTION_STOP = "ACTION_STOP";

        public static string EXTRA_HOST = "EXTRA_HOST";
        public static string EXTRA_PORT = "EXTRA_PORT";
        protected const string NOTIFICATION_CHANNEL_ID = "com.stijnvdb88.snapdotnet.snapclientservice.defaultchannel";
        protected string m_Host = "";
        protected int m_Port = 1704;

        protected PowerManager.WakeLock m_WakeLock = null;
        protected WifiManager.WifiLock m_WifiLock = null;
    }
}