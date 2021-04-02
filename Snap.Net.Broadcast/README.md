# Snap.Net.Broadcast #

## Overview ##

The purpose of this tool is to make it possible to stream audio from your Windows PC to snapserver. It uses [Wasapi loopback](https://docs.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording) to capture the audio coming through your audio device, and sends it to snapserver for syncronized playback. The delay between the audio being captured on your PC will more or less equal the `buffer` setting in snapserver which defaults to 1000ms (plus the network latency between your PC and snapserver, which should be negligible in most cases).  

This also works really well with meta streams, as we only send audio to the server when something is actually played (silence is not sent).

It's also possible to capture the audio from any *input* device (eg. a microphone) and send it to snapserver. I can't think of many practical applications for broadcasting your own voice around the house, other than pranking the wife and kids in a thousand different ways. 

## Usage ##

List all output devices:  
`Snap.Net.Broadcast.exe --list --type Output`

List all input devices:  
`Snap.Net.Broadcast.exe --list --type Input`

Broadcast:  
`Snap.Net.Broadcast.exe --host <snapserver-ip> --port 4953 --type <Input/Output> --soundcard x` (where x is the number of the soundcard identified via --list)

## Setup ##

To make this work, you first need to set up a stream on snapserver which listens for incoming audio data over TCP. See https://github.com/badaix/snapcast/blob/master/doc/configuration.md#tcp-server

`stream = tcp://<snapserver-ip>?port=4953&name=snapbroadcast`

Next, you need to choose an audio device from your PC to use for capturing loopback from. In most cases you won't want to use your main output device, because you can't use it for loopback and playback at the same time. E.g. audio being captured from your main speakers would just echo back ~1000ms after being broadcast. Volume and mute of your audio device is also applied to loopback, so you can't mute one without also muting the other.

There are 2 ways to work around this: either use an audio device which is not connected to any speakers (in my case I have a Digital Audio interface on my motherboard which isn't hooked up):

![image](https://user-images.githubusercontent.com/4498834/112459365-a979cb00-8d55-11eb-9d71-90c1e793817a.png)

Or you can install a virtual audio device and just use that. [Virtual Audio Cable](https://vb-audio.com/Cable/) works well for this:

![image](https://user-images.githubusercontent.com/4498834/112459588-e80f8580-8d55-11eb-94d6-b9f90daef574.png)

Finally, it's just a matter of configuring the Broadcast tool within Snap.Net:

![image](https://user-images.githubusercontent.com/4498834/112459773-1e4d0500-8d56-11eb-8956-e3783a841858.png)

Or starting it via command line (use this for troubleshooting if something doesn't work):   
`Snap.Net.Broadcast.exe -s 2 -h <snapserver-ip> -p 4953`

That's it! Now, any audio that is played to the device you've set up for loopback will get broadcast via snapserver.

## Applications ##

The easiest way to make use of this, is to just set the broadcast audio device as your default:
![image](https://user-images.githubusercontent.com/4498834/112467623-31180780-8d5f-11eb-8e88-fb7fb32ae827.png)

However, this will also include regular Windows alert sounds etc, which isn't great. A nicer way is to specify the output device in whatever media player you use to play music, so that only audio from that application gets broadcast.

You can even watch movies this way, because the delay is known we are able to compensate for it so that audio and video are perfectly in sync:

(example screenshots are from [Media Player Classic](https://mpc-hc.org/))

![image](https://user-images.githubusercontent.com/4498834/112467996-a552ab00-8d5f-11eb-9e18-2c9d93d7ba5f.png)

![image](https://user-images.githubusercontent.com/4498834/112468062-bdc2c580-8d5f-11eb-9fec-bf1f122d7f43.png)


