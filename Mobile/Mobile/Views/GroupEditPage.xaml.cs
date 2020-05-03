using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SnapDotNet.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GroupEditPage : ContentPage
    {
        private SnapDotNet.ControlClient.JsonRpcData.Client[] m_Clients;

        private readonly SnapDotNet.ControlClient.JsonRpcData.Group m_Group;
        private readonly SnapDotNet.ControlClient.SnapcastClient m_SnapcastClient;

        private CheckBox[] m_ClientCheckBoxes;

        public GroupEditPage(SnapDotNet.ControlClient.SnapcastClient snapcastClient, SnapDotNet.ControlClient.JsonRpcData.Group group)
        {
            InitializeComponent();

            m_Group = group;
            m_SnapcastClient = snapcastClient;

            m_Group.SERVER_OnInvalidate += () =>
            {
                // server is sending us a full data refresh - close this window (this group might be getting deleted for all we know)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Navigation.PopAsync().ConfigureAwait(false);
                });
            };

            m_Group.CLIENT_OnNameModified += Group_Updated;
            m_Group.SERVER_OnGroupUpdated += Group_Updated;

            _UpdateData();
        }

        private void Group_Updated()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _UpdateData();
            });
        }

        private void _UpdateData()
        {
            //Title = m_Group.Name;
            //tbName.Text = m_Group.name;

            //cbStream.Items.Clear();
            //foreach (SnapDotNet.ControlClient.JsonRpcData.Stream stream in m_SnapcastClient.ServerData.streams)
            //{
            //    cbStream.Items.Add(stream.id);
            //}

            //m_Clients = m_SnapcastClient.ServerData.GetAllClients();
            //spClients.Children.Clear();
            //m_ClientCheckBoxes = new CheckBox[m_Clients.Length];
            //for (int i = 0; i < m_ClientCheckBoxes.Length; i++)
            //{
            //    CheckBox cb = new CheckBox();
            //    cb.Content = m_Clients[i].Name;
            //    //cb.FlowDirection = FlowDirection.RightToLeft; // (we want the text on the left side of the checkbox...)
            //    //TextBlock tb = new TextBlock();
            //    //tb.FlowDirection = FlowDirection.LeftToRight;
            //    //tb.Text = m_Clients[i].Name;
            //    //cb.Content = tb;
            //    cb.Margin = new Thickness(10, 0, 0, 5);
            //    cb.IsChecked = m_Group.HasClientWithId(m_Clients[i].id);
            //    m_ClientCheckBoxes[i] = cb;
            //    spClients.Children.Add(m_ClientCheckBoxes[i]);
            //}

            //cbStream.SelectedItem = m_Group.stream_id;
        }
    }
}