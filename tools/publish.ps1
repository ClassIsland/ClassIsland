$PUBLISH_TARGET = ".\ClassIsland"

dotnet publish .\ClassIsland\ClassIsland.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded

Write-Host "Successfully published to $PUBLISH_TARGET" -ForegroundColor Green

Write-Host "Packaging..." -ForegroundColor Cyan
if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}
7z a ./out/ClassIsland.zip ./ClassIsland/ClassIsland/* -r -mx=9
