param($is_trim, $arch)

$ErrorActionPreference = "Stop"

$PUBLISH_TARGET = "..\out\ClassIsland"

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}
#dotnet clean

dotnet publish .\ClassIsland\ClassIsland.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded -p:TrimAssets=$is_trim -p:ClassIsland_PlatformTarget=$arch -p:RuntimeIdentifier="win-${arch}"

Write-Host "Packaging..." -ForegroundColor Cyan

rm ./out/ClassIsland/*.xml -ErrorAction Continue
7z a ./out/${env:artifact_name}.zip ./out/ClassIsland/* -r -mx=9

Write-Host "Successfully published to $PUBLISH_TARGET" -ForegroundColor Green

