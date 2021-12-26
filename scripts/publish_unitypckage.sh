#!/bin/bash

# folders for managed part
mkdir './unity_package'

./scripts/copy_dlls_to_unity.sh

# build local
# /Applications/Unity/Hub/Editor/2020.3.24f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -projectPath ./src/unity/UdpToolkit.Unity/ -exportPackage "Assets/Plugins/UdpToolkit" "Assets/Scripts/UdpToolkit" "../../../unity_package/UdpToolkit.dev.unitypackage"

# build
/opt/Unity/Editor/Unity -quit -batchmode -nographics -projectPath ./src/unity/UdpToolkit.Unity/ -exportPackage "Assets/Plugins/UdpToolkit" "Assets/Scripts/UdpToolkit" "../../../unity_package/UdpToolkit.$PACKAGE_VERSION.unitypackage"
