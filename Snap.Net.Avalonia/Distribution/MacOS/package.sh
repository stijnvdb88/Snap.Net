#!/usr/bin/env bash
SCRIPT_PATH="$(readlink -f "$0")"
SCRIPT_DIR="$(dirname "$SCRIPT_PATH")"
readonly VERSION=$1

sed -i -e "s/0.34.0/$VERSION/g" "$SCRIPT_DIR/App/SnapDotNet.app/Contents/Info.plist"
dotnet publish "$SCRIPT_DIR/../../Snap.Net.Avalonia.csproj" -r osx-x64 -c Release -p:UseAppHost=true --self-contained true -p:PublishSingleFile=true -p:Version="$VERSION" -p:InformationalVersion="$VERSION"
mkdir -p "$SCRIPT_DIR/App/SnapDotNet.app/Contents/MacOS"
cp -f -a "$SCRIPT_DIR/../../bin/Release/net10.0/osx-x64/publish/." "$SCRIPT_DIR/App/SnapDotNet.app/Contents/MacOS"
chmod -R a+rX "$SCRIPT_DIR/App/SnapDotNet.app/Contents/MacOS"
chmod +x "$SCRIPT_DIR/App/SnapDotNet.app/Contents/MacOS/Snap.Net.Avalonia"

