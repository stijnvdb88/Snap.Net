using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SnapDotNet.ControlClient.JsonRpcData;
using SnapDotNet.ControlClient;
using SnapDotNet.Mobile.Views;
using Xamarin.Essentials;

namespace SnapDotNet.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Group : ContentView
    {
        private SnapDotNet.ControlClient.JsonRpcData.Group m_Group;
        private SnapcastClient m_SnapcastClient;
        private SnapDotNet.ControlClient.JsonRpcData.Stream m_Stream;

        public Group(SnapcastClient snapcastClient, SnapDotNet.ControlClient.JsonRpcData.Group group)
        {
            InitializeComponent();
            m_Group = group;
            m_SnapcastClient = snapcastClient;

            foreach (SnapDotNet.ControlClient.JsonRpcData.Client client in m_Group.clients)
            {
                Client c = new Client(client, m_SnapcastClient.ServerData.server.snapserver);
                //c.HorizontalAlignment = HorizontalAlignment.Stretch;
                //c.HorizontalContentAlignment = HorizontalAlignment.Center;
                slClients.Children.Add(c);
            }

            m_Group.SERVER_OnGroupUpdated += () =>
            {
                MainThread.BeginInvokeOnMainThread(_OnGroupUpdated);
            };

            m_Group.CLIENT_OnVolumeUpdated += () =>
            {
                MainThread.BeginInvokeOnMainThread(_OnGroupUpdated);
            };

            vcGroup.OnMuteToggled += VcGroup_OnMuteToggled;
            vcGroup.OnVolumeChanged += VcGroup_OnVolumeChanged;
            vcGroup.OnVolumeChangeStart += VcGroup_OnVolumeChangeStart;
            vcGroup.OnSettingsTapped += VcGroup_OnSettingsTapped;
            _OnGroupUpdated();
        }

        private void VcGroup_OnVolumeChangeStart()
        {
            m_Group.CLIENT_StartGroupVolumeChange();
        }

        private void VcGroup_OnVolumeChanged(int newValue)
        {
            m_Group.CLIENT_SetVolume(newValue);
        }

        private void VcGroup_OnMuteToggled()
        {
            m_Group.CLIENT_ToggleMuted();
        }

        private void VcGroup_OnSettingsTapped()
        {
            Navigation.PushAsync(new GroupEditPage(m_SnapcastClient, m_Group)).ConfigureAwait(false);
        }

        private void _OnGroupUpdated()
        {
            vcGroup.Active = true;
            vcGroup.Muted = m_Group.muted;
            vcGroup.OnVolumeChanged -= VcGroup_OnVolumeChanged;
            vcGroup.Percent = m_Group.VolumePercent;
            vcGroup.OnVolumeChanged += VcGroup_OnVolumeChanged;
            if (m_Stream != null)
            {
                m_Stream.SERVER_OnStreamUpdated -= _OnStreamUpdated; // unhook event from old m_Stream object
            }
            m_Stream = m_SnapcastClient.GetStream(m_Group.stream_id);

            if (m_Stream != null)
            {
                m_Stream.SERVER_OnStreamUpdated += _OnStreamUpdated; // hook up event to new m_Stream object
                lbGroup.Text = string.Format("{0} - {1} ({2})", m_Group.Name, m_Stream.id, m_Stream.status);
            }
        }

        private void _OnStreamUpdated()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _OnGroupUpdated();
            });
        }
    }
}