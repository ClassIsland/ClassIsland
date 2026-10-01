#!/bin/sh

export ClassIsland_PackageRoot="/app/bin"
export DOTNET_ROOT="/app/lib/dotnet"

exec /app/lib/dotnet/dotnet /app/bin/ClassIsland.Desktop.dll "$@"
