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
    $version = $($ver -as [string]) + "-dev" + $count
}
echo PackageVersion:$($version -as [string])
echo Version:$($ver -as [string])
#dotnet clean

dotnet build -c Release -p:Platform="Any CPU" -p:Version=$($ver -as [string]) -p:PackageVersion=$($version -as [string])
cp ./**/bin/Release/*.nupkg ./out

Get-ChildItem ./out
