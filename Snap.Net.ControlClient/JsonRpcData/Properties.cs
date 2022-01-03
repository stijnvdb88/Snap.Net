using System;
using System.Collections.Generic;
using System.Text;

namespace Snap.Net.ControlClient.JsonRpcData
{
    public class Properties
    {
        public bool canControl { get; set; }
        public bool canGoNext { get; set; }
        public bool canGoPrevious { get; set; }
        public bool canPause { get; set; }
        public bool canPlay { get; set; }
        public bool canSeek { get; set; }
        public Metadata metadata { get; set; }
    }
}
