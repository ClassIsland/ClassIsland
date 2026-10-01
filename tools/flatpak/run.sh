#!/bin/sh

export ClassIsland_PackageRoot="/app/bin"
export DOTNET_ROOT="/app/lib/dotnet"

cd /app/bin
exec /app/bin/ClassIsland.Desktop "$@"
