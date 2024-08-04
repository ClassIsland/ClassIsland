param($is_release)

$PUBLISH_TARGET = "..\out\ClassIsland"

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}
$tag = $(git describe --tags --abbrev=0)
$count = $(git rev-list --count HEAD)
$ver = [System.Version]::Parse($tag)

if ($is_release -eq "true") {
    $version = $ver
} else {
    $version = [System.Version]::new(1, 0, 0, $($count -as [int]))
}
echo $($version -as [string])
#dotnet clean

dotnet build -c Release -p:Platform="Any CPU" -p:Version=$($version -as [string])
cp ./**/bin/Release/*.nupkg ./out

Get-ChildItem ./out
