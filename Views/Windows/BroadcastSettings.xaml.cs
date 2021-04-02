using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using NAudio.CoreAudioApi;
using Snap.Net.Broadcast;

namespace SnapDotNet.Windows
{
    /// <summary>
    /// Interaction logic for BroadcastSettings.xaml
    /// </summary>
    public partial class BroadcastSettings : MetroWindow
    {
        private Broadcast.Broadcast m_Broadcast = null;

        private List<string> m_Devices = new List<string>();

        private Broadcast.Broadcast.BroadcastSettings m_Settings;
        private string m_DeviceName = "";

        public BroadcastSettings(Broadcast.Broadcast broadcast)
        {
            InitializeComponent();
            m_Broadcast = broadcast;
            m_Broadcast.BroadcastStateChanged += _OnBroadcastStateChanged;
        }

        private void _OnBroadcastStateChanged(Broadcast.Broadcast.EState state, string deviceName)
        {
            m_DeviceName = deviceName;
            Application.Current.Dispatcher.Invoke(() =>
            {
                imgIcon.Kind = MahApps.Metro.IconPacks.PackIconBoxIconsKind.RegularBroadcast;
                bool connected = state.HasFlag(Broadcast.Broadcast.EState.Connected);
                if (connected == false)
                {
                    imgIcon.Kind = PackIconBoxIconsKind.RegularBlock;
                }

                btRecord.IsChecked = connected;
            });
            Console.WriteLine(state);
        }

        private async void BtRecord_OnClick(object sender, RoutedEventArgs e)
        {
            _Save();
            if (m_Broadcast.State.HasFlag(Broadcast.Broadcast.EState.Connected) == false)
            {
                Task.Run(m_Broadcast.BroadcastAsync).ConfigureAwait(false);
            }
            else
            {
                m_Broadcast.Stop();
            }
            _OnBroadcastStateChanged(m_Broadcast.State, m_DeviceName);
        }

        private void BroadcastSettings_OnLoaded(object sender, RoutedEventArgs e)
        {
            m_Settings = SnapSettings.GetBroadcastSettings();
            tbPort.Text = m_Settings.Port.ToString();
            cbAutoStart.IsChecked = m_Settings.AutoBroadcast;
            _OnBroadcastStateChanged(m_Broadcast.State, m_DeviceName);
            _ListDevices();
        }

        private void _ListDevices()
        {
            cbDevices.Items.Clear();
            List<Broadcast.Broadcast.BroadcastDevice> devices = Broadcast.Broadcast.GetDevices();
            foreach (Broadcast.Broadcast.BroadcastDevice device in devices)
            {
                cbDevices.Items.Add(device);
                if (m_Settings.DeviceUniqueId == device.UniqueId)
                {
                    cbDevices.SelectedItem = device;
                }
            }
        }

        private void _Save()
        {
            m_Settings.Port = int.Parse(tbPort.Text);
            m_Settings.AutoBroadcast = (bool)cbAutoStart.IsChecked;
            m_Settings.DeviceUniqueId = ((Broadcast.Broadcast.BroadcastDevice)cbDevices.SelectedItem).UniqueId;
            SnapSettings.SaveBroadcastSettings(m_Settings);
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            _Save();
            Close();
        }
    }
}
