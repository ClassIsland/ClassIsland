#!/bin/bash
# ClassIsland Flatpak构建脚本

set -e

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
FLATPAK_DOTNET_GENERATOR_URL="https://github.com/flatpak/flatpak-builder-tools/raw/refs/heads/master/dotnet/flatpak-dotnet-generator.py"
FLATPAK_MANIFEST="org.classisland.ClassIsland.json"

echo "ClassIsland Flatpak Build Script"

if [ "$(uname -s)" != "Linux" ]; then
    echo "Error: This script must be run on Linux."
    exit 1
fi
echo "   ✓ Running on Linux"

if ! command -v flatpak &>/dev/null; then
    echo "Error: flatpak is not installed. Please install it first."
    exit 1
fi
echo "   ✓ flatpak is installed"

if ! command -v flatpak-builder &>/dev/null; then
    echo "Error: flatpak-builder is not installed. Please install it first."
    exit 1
fi
echo "   ✓ flatpak-builder is installed"

if ! flatpak remotes | grep -q "^flathub"; then
    echo "   Flathub remote not found. Adding flathub..."
    flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
    echo "   ✓ Flathub remote added"
else
    echo "   ✓ Flathub remote is configured"
fi

echo "Downloading flatpak-dotnet-generator.py..."
cd "$SCRIPT_DIR"
if [ -f "flatpak-dotnet-generator.py" ]; then
    echo "   flatpak-dotnet-generator.py already exists, skipping download"
else
    if command -v curl &>/dev/null; then
        curl -LO "$FLATPAK_DOTNET_GENERATOR_URL"
    elif command -v wget &>/dev/null; then
        wget "$FLATPAK_DOTNET_GENERATOR_URL"
    else
        echo "Error: Neither curl nor wget is available to download the script."
        exit 1
    fi
    chmod +x flatpak-dotnet-generator.py
    echo "   ✓ flatpak-dotnet-generator.py downloaded"
fi

case $(uname -m) in
    x86_64)
    RUNTIME="linux-x64"
    ;;
    aarch64|arm64)
    RUNTIME="linux-arm64"
    ;;
    *)
    echo "Unsupported Runtime.Exiting..."
    exit 1
    ;;
esac

echo "Generating Nuget source file for $RUNTIME..."
python3 flatpak-dotnet-generator.py sources.json "$SCRIPT_DIR/../../ClassIsland.Desktop/ClassIsland.Desktop.csproj" -d 9 -r $RUNTIME
echo "   ✓ sources.json generated"

echo "Building Flatpak package..."
flatpak-builder --install-deps-from=flathub --force-clean --repo=repo build "$FLATPAK_MANIFEST"
echo "   ✓ Flatpak build completed"

echo "Exporting Flatpak bundle..."
flatpak build-bundle repo ClassIsland.flatpak org.classisland.ClassIsland
echo "   ✓ Flatpak bundle created: ClassIsland.flatpak"

echo ""
echo "Done!"
echo "Flatpak bundle location: $SCRIPT_DIR/ClassIsland.flatpak"
