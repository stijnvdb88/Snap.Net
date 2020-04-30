using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SnapDotNet.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VolumeControl : ContentView
    {
        public event Action<int> OnVolumeChanged;
        public event Action OnMuteToggled;
        public event Action OnVolumeChangeStart;
        public event Action OnVolumeChangeEnd;

        private bool m_Muted;
        private bool m_Active;

        private double m_Percent;
        private bool m_UserManipulating = false; // true while user manipulation is ongoing - we ignore incoming progress sets while this is true so we the control doesn't get jittery

        public VolumeControl()
        {
            InitializeComponent();
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
                _OnDataUpdated();
            }
        }

        public bool Muted
        {
            get
            {
                return m_Muted;
            }
            set
            {
                m_Muted = value;
                _OnDataUpdated();
            }
        }

        public double Percent
        {
            get
            {
                return m_Percent;
            }
            set
            {
                if (m_UserManipulating == false)
                {
                    m_Percent = value;
                    slVolume.Value = m_Percent;
                    _OnDataUpdated();
                }
            }
        }

        private void _OnDataUpdated()
        {
            //imgSound.Kind = (MahApps.Metro.IconPacks.PackIconBoxIconsKind)VolumeLevel;
            imgMute.Source = m_Muted ? "ic_mute_icon.png" : "ic_speaker_icon.png";
            if (m_Muted)
            {
                imgMute.Opacity = 0.3f;
            }

            slVolume.Opacity = _GetOpacityForState();
            imgMute.Opacity = _GetOpacityForState();
        }

        private float _GetOpacityForState()
        {
            return m_Active ? 0.8f : 0.3f;
        }

        private void SlVolume_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                OnVolumeChanged?.Invoke((int)e.NewValue);
            }
            m_Percent = e.NewValue;
            slVolume.Value = m_Percent;
            _OnDataUpdated();
        }

        //private void imgSound_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    OnMuteToggled?.Invoke();
        //}


        //private void imgSound_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    imgSound.Opacity = 1.0f;
        //}

        //private void imgSound_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    imgSound.Opacity = _GetOpacityForState();
        //}


        private void SlVolume_OnDragStarted(object sender, EventArgs e)
        {
            m_UserManipulating = true;
            OnVolumeChangeStart?.Invoke();
        }

        private void SlVolume_OnDragCompleted(object sender, EventArgs e)
        {
            m_UserManipulating = false;
            OnVolumeChangeEnd?.Invoke();
        }

        private void Mute_OnTapped(object sender, EventArgs e)
        {
            OnMuteToggled?.Invoke();
        }

        private void TapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            
        }
    }
}