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
            Title = m_Group.Name;
            eName.Text = m_Group.name;
            pStream.Items.Clear();
            foreach (SnapDotNet.ControlClient.JsonRpcData.Stream stream in m_SnapcastClient.ServerData.streams)
            {
                pStream.Items.Add(stream.id);
            }

            m_Clients = m_SnapcastClient.ServerData.GetAllClients();
            slClients.Children.Clear();
            m_ClientCheckBoxes = new CheckBox[m_Clients.Length];
            for (int i = 0; i < m_ClientCheckBoxes.Length; i++)
            {
                StackLayout sl = new StackLayout();
                sl.Orientation = StackOrientation.Horizontal;
                
                Label lb = new Label();
                TapGestureRecognizer tr = new TapGestureRecognizer();
                int idx = i;
                tr.Tapped += (sender, e) =>
                {
                    m_ClientCheckBoxes[idx].IsChecked = m_ClientCheckBoxes[idx].IsChecked == false;
                };
                sl.GestureRecognizers.Add(tr);
                lb.Text = m_Clients[i].Name;
                lb.HorizontalOptions = LayoutOptions.StartAndExpand;
                sl.Children.Add(lb);

                CheckBox cb = new CheckBox();
                cb.Margin = new Thickness(10, 0, 0, 5);
                cb.IsChecked = m_Group.HasClientWithId(m_Clients[i].id);

                m_ClientCheckBoxes[i] = cb;
                sl.Children.Add(cb);

                slClients.Children.Add(sl);
            }

            pStream.SelectedItem = m_Group.stream_id;
        }

        private string[] _GetSelectedClientIds()
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < m_ClientCheckBoxes.Length; i++)
            {
                if (m_ClientCheckBoxes[i].IsChecked == true)
                {
                    ids.Add(m_Clients[i].id);
                }
            }
            return ids.ToArray();
        }

        private void GroupEditPage_OnDisappearing(object sender, EventArgs e)
        {
            m_Group.CLIENT_SetName(eName.Text);
            m_Group.CLIENT_SetStream(pStream.SelectedItem.ToString());
            m_Group.CLIENT_SetClients(_GetSelectedClientIds());
        }
    }
}