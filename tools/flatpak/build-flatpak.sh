#!/usr/bin/env bash
# ClassIsland Flatpak 构建脚本

set -Eeuo pipefail

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly REPO_DIR="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
readonly APP_ID="org.classisland.ClassIsland"
readonly RUNTIME_VERSION="24.08"
readonly MANIFEST="$SCRIPT_DIR/$APP_ID.json"
readonly GENERATOR="$SCRIPT_DIR/flatpak-dotnet-generator.py"
readonly GENERATOR_URL="https://github.com/flatpak/flatpak-builder-tools/raw/74697c75b630d7330e77250fc13cb5ea688d9479/dotnet/flatpak-dotnet-generator.py"
readonly SOURCES="$SCRIPT_DIR/sources.json"
readonly BUILD_DIR="$SCRIPT_DIR/build"
readonly REPO="$SCRIPT_DIR/repo"
readonly BUNDLE="$SCRIPT_DIR/ClassIsland.flatpak"

usage() {
    cat <<EOF_USAGE
Usage: $(basename "$0") [--help]

Build a framework-dependent ClassIsland Flatpak bundle in tools/flatpak.
EOF_USAGE
}

if (($# > 0)); then
    case "$1" in
        --help|-h)
            usage
            exit 0
            ;;
        *)
            echo "Error: unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
fi

echo "ClassIsland Flatpak Build Script"
echo "WARNING: EXPERIMENTAL: Flatpak support is still in early stages."
echo

if [[ "$(uname -s)" != "Linux" ]]; then
    echo "Error: this script must be run on Linux." >&2
    exit 1
fi

for command in flatpak flatpak-builder python3; do
    if ! command -v "$command" >/dev/null 2>&1; then
        echo "Error: $command is not installed. Install the Flatpak build prerequisites and run this script again." >&2
        exit 1
    fi
done
echo "   ✓ Required tools are installed"

case "$(uname -m)" in
    x86_64)
        runtime="linux-x64"
        flatpak_arch="x86_64"
        ;;
    aarch64|arm64)
        runtime="linux-arm64"
        flatpak_arch="aarch64"
        ;;
    *)
        echo "Error: unsupported architecture: $(uname -m)" >&2
        exit 1
        ;;
esac

if ! flatpak remotes --columns=name | grep -Fxq flathub; then
    echo "Error: the Flathub remote is not configured." >&2
    echo "Run: flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo" >&2
    exit 1
fi
echo "   ✓ Flathub remote is configured"

require_ref() {
    local ref="$1"
    if ! flatpak info "$ref" >/dev/null 2>&1; then
        echo "Error: required Flatpak SDK is not installed: $ref" >&2
        echo "Install it manually with: flatpak install flathub $ref" >&2
        exit 1
    fi
}

require_ref "org.freedesktop.Sdk//$RUNTIME_VERSION"
require_ref "org.freedesktop.Sdk.Extension.dotnet9//$RUNTIME_VERSION"
echo "   ✓ Required Flatpak SDKs are installed"

cd "$SCRIPT_DIR"
if [[ ! -f "$GENERATOR" ]]; then
    echo "Downloading flatpak-dotnet-generator.py..."
    if command -v curl >/dev/null 2>&1; then
        curl --fail --location --retry 3 --output "$GENERATOR" "$GENERATOR_URL"
    elif command -v wget >/dev/null 2>&1; then
        wget --quiet --output-document="$GENERATOR" "$GENERATOR_URL"
    else
        echo "Error: curl or wget is required to download the dependency generator." >&2
        exit 1
    fi
    chmod +x "$GENERATOR"
fi

echo "Generating NuGet sources for $runtime..."
temporary_sources="$(mktemp "$SCRIPT_DIR/sources.json.XXXXXX")"
trap 'rm -f "$temporary_sources"' EXIT
python3 "$GENERATOR" "$temporary_sources" \
    "$REPO_DIR/ClassIsland.Desktop/ClassIsland.Desktop.csproj" \
    --dotnet 9 --runtime "$runtime"
if ! python3 - "$temporary_sources" <<'PY'
import json
import sys
from pathlib import Path

sources = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
if not sources:
    raise SystemExit("the dependency generator produced an empty sources list")
PY
then
    echo "Error: failed to generate NuGet sources. Check the Flatpak SDK and network prerequisites." >&2
    exit 1
fi
mv -- "$temporary_sources" "$SOURCES"
echo "   ✓ sources.json generated"

echo "Building Flatpak package..."
flatpak-builder \
    --disable-rofiles-fuse \
    --force-clean \
    --repo="$REPO" \
    "$BUILD_DIR" "$MANIFEST"
echo "   ✓ Flatpak build completed"

echo "Exporting Flatpak bundle..."
flatpak build-bundle \
    --arch="$flatpak_arch" \
    --runtime-repo=https://flathub.org/repo/flathub.flatpakrepo \
    "$REPO" "$BUNDLE" "$APP_ID"
echo "   ✓ Flatpak bundle created: $BUNDLE"

echo
echo "Done!"
echo "Flatpak bundle location: $BUNDLE"
