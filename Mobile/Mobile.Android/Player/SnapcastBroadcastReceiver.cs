
using Android.App;
using Android.Content;
using Android.Media;

namespace SnapDotNet.Mobile.Droid.Player
{
    /// <summary>
    /// intent receiver for restarting snapclient service when
    /// something is plugged in/out of the audio jack
    /// </summary>
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { AudioManager.ActionHeadsetPlug })]
    public class SnapcastBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != AudioManager.ActionHeadsetPlug)
                return;

            if(MainActivity.Instance.IsPlaying())
                MainActivity.Instance.Restart();
        }
    }
}