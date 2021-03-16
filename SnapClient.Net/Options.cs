/*
    This file is part of Snap.Net
    Copyright (C) 2020  Stijn Van der Borght
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using CommandLine;

namespace SnapClient.Net
{
    class Options
    {
        public enum EPlayer
        {
            Wasapi,
            DirectSound,
            WaveOut
        }

        [Option('l', "list", HelpText = "Prints a list of all available audio devices and quits.")]
        public bool ListDevices { get; set; }

        [Option('h', "host", HelpText = "Server hostname or ip address")]
        public string HostName { get; set; }

        [Option('p', "port", Default = 1704, HelpText = "Server port")]
        public int Port { get; set; }

        [Option('i', "instance", Default = 1, HelpText = "Instance id when running multiple instances on the same host")]
        public int Instance { get; set; }

        [Option('s', "soundcard", Default = 0, HelpText = "Index of the audio device to use")]
        public int SoundCard { get; set; }

        [Option("soundcard-latency", Default = 100, HelpText = "The latency to request for the sound card (not the same as client latency!)")]
        public int DacLatency { get; set; }

        [Option("player", Default = EPlayer.Wasapi, HelpText = "Wasapi|DirectSound|WaveOut")]
        public EPlayer Player { get; set; }

        [Option("buffer-duration", Default = 50, HelpText = "The duration of the audio device's buffer in milliseconds")]
        public int BufferDurationMs { get; set; }

        [Option("offset-tolerance", Default = 10, HelpText = "The tolerance for offset in milliseconds. This determines how quickly we'll enforce 'hard' syncs (cause audible pops).")]
        public int OffsetToleranceMs { get; set; }
    }
}
