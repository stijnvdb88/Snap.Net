using System;
using System.Collections.Generic;
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

namespace SnapDotNet.Windows
{
    /// <summary>
    /// Interaction logic for DeviceSettings.xaml
    /// </summary>
    public partial class DeviceSettings : MetroWindow
    {
        private int[] kSampleRates = new int[] { 44100, 48000, 96000, 192000 };
        private int[] kBitDepths = new int[] { 16, 24 };

        private SnapDotNet.Player.Device m_Device;

        private SnapDotNet.Player.DeviceSettings m_Settings;

        public DeviceSettings(SnapDotNet.Player.Device device)
        {
            InitializeComponent();
            m_Device = device;
            this.Title = device.Name;
            m_Settings = SnapSettings.GetDeviceSettings(device.UniqueId);

            string[] sharemodes = Enum.GetNames(typeof(SnapDotNet.Player.EShareMode));
            foreach(string mode in sharemodes)
            {
                cbSharemode.Items.Add(mode);
            }

            cbSharemode.SelectedIndex = (int)m_Settings.ShareMode;

            cbSampleFormat.Items.Add("(no resample)");
            cbSampleFormat.SelectedIndex = 0;
            int idx = 1;
            foreach(int sr in kSampleRates)
            {
                foreach(int bp in kBitDepths)
                {
                    string sampleFormat = string.Format("{0}:{1}", sr, bp);
                    cbSampleFormat.Items.Add(sampleFormat);
                    if(m_Settings.ResampleFormat == sampleFormat)
                    {
                        cbSampleFormat.SelectedIndex = idx;
                    }
                    idx++;
                }
            }

            tbRestartTries.Text = m_Settings.RestartAttempts.ToString();
            cbAutoRestart.IsChecked = m_Settings.AutoRestartOnFailure;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            m_Settings.ResampleFormat = cbSampleFormat.SelectedIndex == 0 ? "" : cbSampleFormat.SelectedItem.ToString();
            m_Settings.ShareMode = (SnapDotNet.Player.EShareMode)cbSharemode.SelectedIndex;
            m_Settings.AutoRestartOnFailure = (bool) cbAutoRestart.IsChecked;
            if(m_Settings.AutoRestartOnFailure)
            {
                m_Settings.RestartAttempts = int.Parse(tbRestartTries.Text, System.Globalization.CultureInfo.CurrentCulture);
            }
            SnapSettings.SaveDeviceSettings(m_Device.UniqueId, m_Settings);
            this.Close();
        }

        private void cbAutoRestart_Toggled(object sender, RoutedEventArgs e)
        {
            tbRestartTries.IsEnabled = (bool)cbAutoRestart.IsChecked;
        }

        private void tbRestartTries_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Utils.IsNumbersOnly(e.Text) == false;
        }
    }
}
