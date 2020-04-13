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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs",
    Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)",
    Scope = "namespace",
    Target = "~M:SnapDotNet.SnapControl.SnapControl.#ctor(Hardcodet.Wpf.TaskbarNotification.Interop.Point,SnapDotNet.Client.SnapcastClient)")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.Snapcast._ConnectClientAndStartAutoPlayAsync~System.Threading.Tasks.Task")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.Snapcast.ShowNotification(System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.Snapcast.ShowSnapPane(System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.Client.#ctor(SnapDotNet.Client.JsonRpcData.Client,SnapDotNet.Client.JsonRpcData.Snapserver)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.EditClient.#ctor(SnapDotNet.Client.JsonRpcData.Client,SnapDotNet.Client.JsonRpcData.Snapserver)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.EditGroup.#ctor(SnapDotNet.Client.SnapcastClient,SnapDotNet.Client.JsonRpcData.Group)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.EditGroup.Group_Updated")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.Group.#ctor(SnapDotNet.Client.SnapcastClient,SnapDotNet.Client.JsonRpcData.Group)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.Group._OnStreamUpdated")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.SnapControl.ViewStream.#ctor(SnapDotNet.Client.JsonRpcData.Stream)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.Windows.Player.Player_DevicePlayStateChanged(System.String,SnapDotNet.Player.EState)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "This appears to apply to VisualStudio extension development only (StreamJsonRpc package adds Microsoft.VisualStudio.Threading.Analyzers package, which throws this warning)", Scope = "member", Target = "~M:SnapDotNet.Windows.Player.Player_OnSnapClientErrored")]