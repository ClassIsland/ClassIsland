param($is_trim)

$PUBLISH_TARGET = "..\out\ClassIsland"

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}
#dotnet clean

dotnet publish .\ClassIsland\ClassIsland.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded -p:TrimAssets=$is_trim

Write-Host "Successfully published to $PUBLISH_TARGET" -ForegroundColor Green

Write-Host "Packaging..." -ForegroundColor Cyan

rm ./out/ClassIsland/*.xml

7z a ./out/ClassIsland.zip ./out/ClassIsland/* -r -mx=9

rm -Recurse -Force ./out/ClassIsland
