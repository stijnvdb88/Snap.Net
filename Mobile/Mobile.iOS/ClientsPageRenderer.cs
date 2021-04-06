using AVFoundation;
using Foundation;
using SnapDotNet.Mobile.iOS;
using SnapDotNet.Mobile.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ClientsPage), typeof(ClientsPageRenderer))]

namespace SnapDotNet.Mobile.iOS
{
    class ClientsPageRenderer : PageRenderer
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();
            this.BecomeFirstResponder();
        }
        
        public override bool CanBecomeFirstResponder => true;

        public override void RemoteControlReceived(UIEvent uiEvent)
        {
            if (uiEvent.Type == UIEventType.RemoteControl)
            {
                switch (uiEvent.Subtype)
                {
                    case UIEventSubtype.RemoteControlPlay:
                        Player.Player.Instance.Play();
                        break;
                    case UIEventSubtype.RemoteControlStop:
                        Player.Player.Instance.Stop();
                        break;

                    case UIEventSubtype.RemoteControlTogglePlayPause:
                        if (Player.Player.Instance.IsPlaying())
                        {
                            Player.Player.Instance.Stop();
                        }
                        else
                        {
                            Player.Player.Instance.Play();
                        }
                        break;
                }
            }
            base.RemoteControlReceived(uiEvent);
        }
    }
}