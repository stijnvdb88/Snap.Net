using CommandLine;

namespace Snap.Net.Broadcast
{
    class Options
    {
        [Option('l', "list", HelpText = "Prints a list of all available audio devices and quits.")]
        public bool ListDevices { get; set; }

        [Option('h', "host", HelpText = "Server hostname or ip address")]
        public string HostName { get; set; }

        [Option('p', "port", Default = 4953, HelpText = "Server port")]
        public int Port { get; set; }

        [Option('s', "soundcard", Default = 0, HelpText = "Index of the audio device to use")]
        public int SoundCard { get; set; }
    }
}
