$ErrorActionPreference = "Stop"

$env:version = $(git describe --abbrev=0 --tags)

if ($env:package -eq "folder") {
    ./tools/release-gen/pack/folder.ps1
    Compress-Archive -Path ./out_pack/pack/* -DestinationPath "./out_pack/${env:artifact_name}.zip" -Force
}