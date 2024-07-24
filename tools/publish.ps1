$PUBLISH_TARGET = "..\out\ClassIsland"

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}

dotnet clean

dotnet build -c Release


$nuget_packs = @(
    "ClassIsland.Shared",
    "ClassIsland.Shared.IPC",
    "ClassIsland.Core",
    "ClassIsland.PluginSdk"
)
foreach ($i in $nuget_packs) {
    echo .\${i}\${i}.csproj
    dotnet pack .\${i}\${i}.csproj -o .\out -c Release -property:DebugType=full
}
    
dotnet publish .\ClassIsland\ClassIsland.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded

Write-Host "Successfully published to $PUBLISH_TARGET" -ForegroundColor Green

Write-Host "Packaging..." -ForegroundColor Cyan

rm ./out/ClassIsland/*.xml

7z a ./out/ClassIsland.zip ./out/ClassIsland/* -r -mx=9

rm -Recurse -Force ./out/ClassIsland
