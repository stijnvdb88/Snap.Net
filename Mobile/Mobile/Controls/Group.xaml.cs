using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnapDotNet.Mobile.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SnapDotNet.ControlClient.JsonRpcData;
using SnapDotNet.ControlClient;
using Xamarin.Essentials;

namespace SnapDotNet.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Group : ContentView
    {
        private SnapDotNet.ControlClient.JsonRpcData.Group m_Group;
        private SnapcastClient m_SnapcastClient;

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


        private void _OnGroupUpdated()
        {
            lbGroup.Text = m_Group.name;
            vcGroup.Active = true;
            vcGroup.Muted = m_Group.muted;
            vcGroup.OnVolumeChanged -= VcGroup_OnVolumeChanged;
            vcGroup.Percent = m_Group.VolumePercent;
            vcGroup.OnVolumeChanged += VcGroup_OnVolumeChanged;
            //if (m_Stream != null)
            //{
            //    m_Stream.SERVER_OnStreamUpdated -= _OnStreamUpdated; // unhook event from old m_Stream object
            //}
            //m_Stream = m_SnapcastClient.GetStream(m_Group.stream_id);
            //m_Stream.SERVER_OnStreamUpdated += _OnStreamUpdated; // hook up event to new m_Stream object
            //_OnStreamUpdated();
        }
    }
}