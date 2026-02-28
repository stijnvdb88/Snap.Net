#!/usr/bin/env bash
SCRIPT_PATH="$(readlink -f "$0")"
SCRIPT_DIR="$(dirname "$SCRIPT_PATH")"
readonly VERSION=$1

sed -i -e "s/0.34.0/$VERSION/g" "$SCRIPT_DIR/dpkg-deb/DEBIAN/control"
dotnet publish "$SCRIPT_DIR/../../Snap.Net.Avalonia.csproj" -r linux-x64 -p:PublishSingleFile=true  --self-contained true -c Release --nologo -p:Version="$VERSION" -p:InformationalVersion="$VERSION"
mkdir -p "$SCRIPT_DIR/dpkg-deb/usr/lib/snapdotnet/"
cp -f -a "$SCRIPT_DIR/../../bin/Release/net10.0/linux-x64/publish/." "$SCRIPT_DIR/dpkg-deb/usr/lib/snapdotnet/"
chmod -R a+rX "$SCRIPT_DIR/dpkg-deb/usr/lib/snapdotnet/"
chmod +x ."$SCRIPT_DIR/dpkg-deb/usr/lib/snapdotnet/Snap.Net.Avalonia"
declare -a arr=("16" "32" "48" "64" "128" "256" "512")
for i in "${arr[@]}"
do
    mkdir -p "$SCRIPT_DIR/dpkg-deb/usr/share/icons/hicolor/${i}x${i}/apps"
    inkscape -o "$SCRIPT_DIR/dpkg-deb/usr/share/icons/hicolor/${i}x${i}/apps/snapdotnet.png" -C -w $i -h $i "$SCRIPT_DIR/dpkg-deb/usr/share/icons/hicolor/scalable/apps/snapdotnet.svg"
done
dpkg-deb --root-owner-group --build "$SCRIPT_DIR/dpkg-deb" "${SCRIPT_DIR}/Snap.Net-${VERSION}-Linux-amd64.deb"
