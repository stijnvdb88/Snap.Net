## Snap.Net.SnapClient ##

This library is an an implementation of snapcast's [binary protocol](https://github.com/badaix/snapcast/blob/master/doc/binary_protocol.md).
It lets you easily connect to snapserver as a player, and handle incoming audio.

## Usage: ##

#### Using NAudio: ####

The easiest way to get started is if you already have an NAudio implementation for the player you want to use. In this case, just pass in a factory func to NAudioPlayer:
```csharp
static async Task<int> Main(string[] args)
{
  int dacLatency = 100;
  int bufferDurationMs = 80;
  int offsetToleranceMs = 5;
  HelloMessage helloMessage = new HelloMessage("unique_player_id_here", "OS version here");
  Player player = new NAudioPlayer(dacLatency, bufferDurationMs, offsetToleranceMs, _DeviceFactory);
  Controller controller = new Controller(player, helloMessage);
  await controller.StartAsync("hostname", 1704);
  return 0;
}

private IWavePlayer _DeviceFactory()
{
  return new WaveOutEvent(); // create your player instance here
}
 
```
See https://github.com/stijnvdb88/Snap.Net/tree/master/SnapClient.Net for a full example with NAudio, which implements most of the audio devices listed here: https://github.com/naudio/NAudio/blob/master/Docs/OutputDeviceTypes.md

#### Without NAudio ####

If the player you're targeting does not yet have an NAudio implementation, you'll have to inherit from the [Player](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Player/Player.cs) class directly.
Override the `_Start()` function to: 
* initialize the audio device
* start the audio loop: grab `m_BufferDurationMs` milliseconds worth of audio from the AudioStream, and feed it into your player. When your player has finished playing this data, repeat. You'll want to get this loop as tight as possible, making sure that exactly `m_BufferDurationMs` milliseconds has passed between each iteration. Deviations up to ~1ms are acceptable here, but any higher than that should be looked into. Usually either the audio device's callback is inaccurate, or the clock being used isn't precise enough. Use AudioStream.GetNextBuffer's `age` variable for measuring this.

See the [iOS player](https://github.com/stijnvdb88/Snap.Net/blob/master/Mobile/Mobile.iOS/Player/AudioQueuePlayer.cs) for a working example. 

## High-level summary ##

The names of most classes have been kept the same as in the native [snapclient](https://github.com/badaix/snapcast/tree/master/client) implementation.
This should make it a lot simpler to get started in this codebase if you're already familiar with snapclient.

### [ClientConnection](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/ClientConnection.cs) ###

Handles the TCP connection between us and the server. It's also responsible for sending and receiving messages. When messages are received, it just makes sure it reads in the correct amount of data and then passes it on to the [MessageRouter](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/MessageRouter.cs), which will parse the messages and forward them to where they need to go.

For more details on how messages are read, see the [binary protocol](https://github.com/badaix/snapcast/blob/master/doc/binary_protocol.md).

### [MessageRouter](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/MessageRouter.cs) ###

Parses incoming messages and forwards them to subscribers (those are registered in the [Controller](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Controller.cs) class). Subscribers need to implement the [IMessageListener](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/IMessageListener.cs) interface.

### [TimeProvider](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Time/TimeProvider.cs) ###

This class is responsible for handling the time difference between us and the server. Periodic time messages are sent by the [Controller](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Controller.cs) (about every second) with our own time included, to which the server replies with the difference between our clocks - that response gets routed to the TimeProvider. This lets us figure out the latency and use our own clock to calculate server time at any moment. We need this for when we start reading audio chunks, which contain timestamps indicating exactly when they need to start playing (in server time).

By default we use [System.Diagnostics.Stopwatch](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-5.0) for accurately telling time. On most platforms, this should be the most high resolution clock available. However if the audio device you're using has its own clock, that will be a better alternative, and you should create your own TimeProvider implementation to leverage it. See the iOS [AudioQueueTimeProvider](https://github.com/stijnvdb88/Snap.Net/blob/master/Mobile/Mobile.iOS/Player/AudioQueueTimeProvider.cs) for an example.

### [Player](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Player/Player.cs) ###

An abstract class which represents an audio player. It has an [AudioStream](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/AudioStream.cs), which it queries for new raw audio data whenever the audio device has finished playback of the previous data. Some buffering may also happen here, depending on the implementation. 

### [AudioStream](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/AudioStream.cs) ###

This is where the syncronized music comes from!
Whenever audio data has been received from the server and decoded, it gets queued up in AudioStream (in the order we received it).
When the Player asks for raw audio, we match up the current server time (adjusted for latency) with the correct audio chunk. A bit of seeking will need to happen at first: the queue might have filled up with some data already which we weren't ready to play yet, or we might be early and need to wait a bit to get in sync. Once we're in sync, the goal is obviously to stay in sync. Each time the audio device asks us again for raw audio data, the amount of time that has passed should be exactly the duration of the last chunk we gave it (this isn't always true, but usually accurate within ~1ms). When we detect inaccuracies here, we'll either skip a few audio frames (play faster) or copy a few (play slower) to make up for the difference without audible seeking.

### [Decoder](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Decoder/Decoder.cs) ###

Base class for the various decoders. The server will always first send us the codec header, so we can figure out the [SampleFormat](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/SampleFormat.cs) before we start receiving audio. Based on the codec being used, the decoder will be initialized with the header, and all audio data received after that will be fed through the decoder to be converted to raw PCM and sent to the [AudioStream](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/AudioStream.cs). See the [FlacDecoder](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Decoder/FlacDecoder.cs) for an example.

### [Controller](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Controller.cs) ###

This class glues all of the above together. It starts the TCP connection, registers all the message listeners with the [MessageRouter](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/MessageRouter.cs), initializes the decoders, and sends the periodic Time messages.

It also starts the [Player](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/Player/Player.cs) as soon as the codec header has been received, and forwards all the raw audio data to it (after first feeding it through the correct decoder).
