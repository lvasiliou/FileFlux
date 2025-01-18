#!/bin/bash
echo "Starting nightly build..."

# Build for Windows
dotnet publish -f net9.0-windows10.0.26100.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None

# Build for macOS
#dotnet publish -c Release -r osx-x64 --self-contained

# Build for iOS
#dotnet publish -c Release -r ios-arm64 --self-contained

echo "Nightly build completed."
