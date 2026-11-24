param(
    [Parameter(Mandatory = $true)][string]$ApplicationDirectory,
    [Parameter(Mandatory = $true)][string]$TestDirectory,
    [Parameter(Mandatory = $true)][string]$LogPath
)

# Run with Windows PowerShell (Full Framework), -NoProfile -STA. This exercises
# the actual WPF resume handler and NLog bridge without starting the application,
# touching user settings/logs, subscribing to system power events, or playing audio.
$ErrorActionPreference = 'Stop'
$ApplicationDirectory = (Resolve-Path -LiteralPath $ApplicationDirectory).Path
$TestDirectory = (Resolve-Path -LiteralPath $TestDirectory).Path
$LogPath = [IO.Path]::GetFullPath($LogPath)
# RPC continuations load dependencies on pool threads, which have no PowerShell
# runspace. Resolve there with managed code, not a PowerShell event scriptblock.
Add-Type @'
using System;
using System.IO;
using System.Reflection;
public static class SmokeAssemblyResolver {
    public static string[] Directories;
    public static Assembly Resolve(object sender, ResolveEventArgs args) {
        var name = new AssemblyName(args.Name).Name;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (assembly.GetName().Name == name) return assembly;
        foreach (var directory in Directories) {
            var path = Path.Combine(directory, name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
    public static void Install() { AppDomain.CurrentDomain.AssemblyResolve += Resolve; }
    public static void Remove() { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
}
'@
[SmokeAssemblyResolver]::Directories = @($ApplicationDirectory, $TestDirectory)
[SmokeAssemblyResolver]::Install()
Add-Type -AssemblyName PresentationFramework
$null = [Reflection.Assembly]::LoadFrom((Join-Path $ApplicationDirectory 'NLog.dll'))
$null = [Reflection.Assembly]::LoadFrom((Join-Path $ApplicationDirectory 'Snap.Net.ControlClient.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ApplicationDirectory 'Snap.Net.exe'))
$tests = [Reflection.Assembly]::LoadFrom((Join-Path $TestDirectory 'Snap.Net.ControlClient.Tests.exe'))
$flags = [Reflection.BindingFlags]'Instance, NonPublic'
$server = [Activator]::CreateInstance($tests.GetType('FakeServer'), $flags, $null, [object[]]@(0), $null)
$port = $server.GetType().GetProperty('Port', $flags).GetValue($server)
$client = New-Object SnapDotNet.ControlClient.SnapcastClient
$app = New-Object SnapDotNet.Snapcast
$bridge = $assembly.GetType('SnapDotNet.Snapcast').GetField('m_ControlTraceListener', $flags).GetValue($app)
$config = New-Object NLog.Config.LoggingConfiguration
$target = New-Object NLog.Targets.FileTarget('resume-smoke')
$target.FileName = $LogPath
$config.AddRule([NLog.LogLevel]::Info, [NLog.LogLevel]::Fatal, $target)
[NLog.LogManager]::Configuration = $config
[Diagnostics.Trace]::Listeners.Add($bridge) | Out-Null
try {
    $null = $client.ConnectAsync('127.0.0.1', $port, 500).GetAwaiter().GetResult()
    if (-not $client.IsConnected()) { throw 'Initial smoke connection failed.' }
    $assembly.GetType('SnapDotNet.Snapcast').GetField('m_SnapcastClient', $flags).SetValue($app, $client)
    # Populate only the in-memory setting inspected by the handler. Never Save().
    $settings = $assembly.GetType('SnapDotNet.Properties.Settings').GetProperty('Default').GetValue($null)
    $settings['Server'] = '127.0.0.1'
    $handler = $assembly.GetType('SnapDotNet.Snapcast').GetMethod('_OnPowerModeChanged', $flags)
    $resumeArgs = New-Object Microsoft.Win32.PowerModeChangedEventArgs([Microsoft.Win32.PowerModes]::Resume)
    $initial = $server.GetType().GetProperty('Connections', $flags).GetValue($server)
    [SnapDotNet.ControlClient.SnapcastClient]::AutoReconnect = $false
    $handler.Invoke($app, [object[]]@($null, $resumeArgs.PSObject.BaseObject))
    Start-Sleep -Milliseconds 150
    if ($server.GetType().GetProperty('Connections', $flags).GetValue($server) -ne $initial) {
        throw 'Resume ignored disabled AutoReconnect.'
    }
    [SnapDotNet.ControlClient.SnapcastClient]::AutoReconnect = $true
    $handler.Invoke($app, [object[]]@($null, $resumeArgs.PSObject.BaseObject))
    $recovery = $client.GetType().GetField('m_ReconnectTask', $flags).GetValue($client)
    if ($null -eq $recovery) { throw 'WPF Resume did not request control recovery.' }
    if (-not $recovery.Wait(5000)) { throw 'WPF Resume recovery timed out.' }
    if (-not $client.IsConnected() -or $server.GetType().GetProperty('Connections', $flags).GetValue($server) -ne ($initial + 1)) {
        throw 'WPF Resume did not create exactly one replacement connection.'
    }
    [Diagnostics.Trace]::WriteLine('SMOKE_DEBUG_SHOULD_NOT_REACH_FILE', 'Snapcast control debug')
    [NLog.LogManager]::Flush()
    $text = Get-Content -LiteralPath $LogPath -Raw
    foreach ($expected in @('Windows resumed', 'Connecting control', 'Control state synchronized', 'Control connection ready')) {
        if (-not $text.Contains($expected)) { throw "Missing NLog message: $expected" }
    }
    if ($text.Contains('SMOKE_DEBUG_SHOULD_NOT_REACH_FILE')) { throw 'Slider debug traffic reached the file log.' }
    Write-Output 'PASS actual WPF Resume handler, AutoReconnect setting and control-to-NLog file bridge'
}
finally {
    $app.Dispose()
    $server.Dispose()
    [NLog.LogManager]::Shutdown()
    [SmokeAssemblyResolver]::Remove()
}
