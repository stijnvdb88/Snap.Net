using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapDotNet.Player
{
    public enum EShareMode
    {
        Shared,
        Exclusive
    }

    [System.Serializable]
    public struct DeviceSettings
    {
        public string ResampleFormat;
        public EShareMode ShareMode;
        public bool AutoRestartOnFailure;
        public int RestartAttempts;
    }
}
