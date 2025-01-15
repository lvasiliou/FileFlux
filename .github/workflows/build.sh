#!/bin/bash
echo "Starting nightly build..."

# Build for Windows
dotnet publish -c Release -r win10-x64 --self-contained

# Build for macOS
#dotnet publish -c Release -r osx-x64 --self-contained

# Build for iOS
#dotnet publish -c Release -r ios-arm64 --self-contained

echo "Nightly build completed."
