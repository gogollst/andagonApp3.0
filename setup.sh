#!/bin/bash
set -euxo pipefail

# Install required packages for .NET 9 SDK installation
apt-get update
apt-get install -y wget apt-transport-https

# Add Microsoft package signing key and repository for .NET
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update

# Install .NET SDK 9.0 (preview) for building the project
apt-get install -y dotnet-sdk-9.0

# Clean up apt caches to reduce image size
apt-get clean
