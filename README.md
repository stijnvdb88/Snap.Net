[![Builds](https://github.com/stijnvdb88/Snap.Net/actions/workflows/build-all.yml/badge.svg)](https://github.com/stijnvdb88/Snap.Net/actions/workflows/build-all.yml)
[![Github Releases](https://img.shields.io/github/release/stijnvdb88/Snap.Net.svg)](https://github.com/stijnvdb88/Snap.Net/releases)
[![GitHub Downloads](https://img.shields.io/github/downloads/stijnvdb88/Snap.Net/total)](https://github.com/stijnvdb88/Snap.Net/releases)
<br />
AppStore release: https://apps.apple.com/us/app/snapcast-control/id1552559653<br />
<p align="center">
  <a href="https://github.com/stijnvdb88/Snap.Net">
    <img src="Assets/snapcast.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">Snap.Net</h3>

  <p align="center">
    A control client and player for <a href="https://github.com/badaix/snapcast">Snapcast</a>
    <br /><b>Windows &middot; Linux &middot; macOS &middot; <a href="https://apps.apple.com/us/app/snapcast-control/id1552559653">iOS</a> &middot; Android</b>
    <br />
    <br />
    <img src="https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Control.png?raw=true">
  </p>
</p>

### SnapClient ###

This project comes with a .NET port of snapclient. That library can be used to easily port snapclient to all platforms that are able to run .NET code.
See the [documentation](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.SnapClient/README.md) for more information.

### Broadcast ###

A small [tool for broadcasting to snapserver](https://github.com/stijnvdb88/Snap.Net/blob/master/Snap.Net.Broadcast/README.md) is also included. This makes it easy to stream audio from your PC to all snapclients.

### Player + Broadcast ###

![Player](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Player.png?raw=true) ![Broadcast](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Broadcast.png?raw=true) 

### Control ###
The client/group name and client latency can be set in their menus. These menus are accessible by clicking on client/group name in the overview.

![Client](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Client.png?raw=true)
![Group](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Group.png?raw=true)

### Customizable ###
If you don't like the colors, change them!  

![Settings](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Settings.png?raw=true)

## Linux / macOS ##
![LinuxMacOS](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/LinuxMacOS.png?raw=true)

## iOS ##

![iOS](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/iOS.png?raw=true)
![iOS_Client](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/iOS_Client.png?raw=true)
![iOS_Group](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/iOS_Group.png?raw=true)

## Android ##

![Android](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Android.png?raw=true)
![Android_Client](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Android_Client.png?raw=true)
![Android_Group](https://github.com/stijnvdb88/Snap.Net/blob/master/Doc/Android_Group.png?raw=true)

## Build instructions ##

### Control + player + broadcast client (Windows only) ###
```
msbuild /t:restore
msbuild /p:Configuration=Release;VersionAssembly=0.34.0
```
### Control client ###

**Requirements**:  
- .NET 10: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
### Windows ###
```
dotnet publish Snap.Net.Avalonia/Snap.Net.Avalonia.csproj -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release --nologo
```
### Linux ###
```
dotnet publish Snap.Net.Avalonia/Snap.Net.Avalonia.csproj -r linux-x64 -p:PublishSingleFile=true --self-contained true -c Release --nologo
```
### macOS ###
```
dotnet publish Snap.Net.Avalonia//Snap.Net.Avalonia.csproj -r osx-x64 -p:PublishSingleFile=true --self-contained true  -c Release -p:UseAppHost=true
```

## Acknowledgements
* [Avalonia](https://avaloniaui.net/)
* [NAudio](https://github.com/naudio/NAudio)
* [StreamJsonRpc](https://github.com/microsoft/vs-streamjsonrpc)
* [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
* [WPF NotifyIcon](https://github.com/hardcodet/wpf-notifyicon)
* [Concentus](https://github.com/lostromb/concentus)
* [CliWrap](https://github.com/Tyrrrz/CliWrap)
* [NLog](https://nlog-project.org/)
* [MouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook)
