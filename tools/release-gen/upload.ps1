$ErrorActionPreference = "Stop"

Import-Module ./tools/release-gen/alist-utils.psm1

# Generate metadata & upload artifacts
Copy-Item ./out_signed/* -Destination ./out/ -Recurse -Force

$version = $(git describe --abbrev=0 --tags)
$gitCommitId = $(git rev-parse HEAD)
$tagInfo = @{
    PrimaryVersion = $env:primaryVersion
    Channels = ConvertFrom-Json $env:channels
}
$versionInfo = @{
    Version = $version
    Title = $version
    DownloadInfos = @{}
    ChangeLogs = $(Get-Content ./doc/ChangeLogs/$($tagInfo.PrimaryVersion)/$version/App.md -Raw)
    Channels = @()
}
$versionInfo.Channels = $tagInfo.Channels

foreach ($artifact in $(Get-ChildItem ./out)) {
    Move-Item $artifact.FullName -Destination $artifact.FullName.Replace("out_app_", "ClassIsland_app_") -Force
}
$artifacts = $(Get-ChildItem ./out)
$hashSummary = "

> [!important]
> 下载时请注意核对文件 SHA256 是否正确。

| 文件名 | SHA256 |
| --- | --- |
"
$legaceyMD5Hashes = [ordered]@{}

foreach ($artifact in $artifacts){
    if ($artifact.Name.StartsWith("ClassIsland_app") -ne $true) {
        continue
    }
    $artifactId = [System.IO.Path]::GetFileNameWithoutExtension($artifact.Name).Split("_")[2..-1] -join "_"
    # TODO: 根据 artifact name 生成对应的 DeployMethod
    $downloadInfo = @{
        "DeployMethod" = 1
        "ArchiveDownloadUrls" = @{
            "main" = "https://get.classisland.tech/p/ClassIsland-Ningbo-S3/classisland/disturb/${version}/$($artifact.Name)"
            "github-origin" = "https://github.com/ClassIsland/ClassIsland/releases/download/${version}/$($artifact.Name)"
        }
        "ArchiveSHA256"= $(Get-FileHash $artifact -Algorithm SHA256).Hash
    }
    UploadFile $artifact.FullName "ClassIsland-Ningbo-S3/classisland/disturb/${version}/"
    $versionInfo.DownloadInfos[$artifactId] = $downloadInfo
    $hashSummary +=  "| $($artifact.Name) | ``$($downloadInfo.ArchiveSHA256)`` |`n"
    $legaceyMD5Hashes.Add($artifact.Name, (Get-FileHash $artifact -Algorithm MD5).Hash)
}

# 兼容旧版更新系统
Copy-Item ./out/ClassIsland_app_windows_x64_full_singleFile.zip -Destination ./out/ClassIsland.zip -Force
Copy-Item ./out/ClassIsland_app_windows_x64_trimmed_singleFile.zip -Destination ./out/ClassIsland_AssetsTrimmed.zip -Force
$legaceyMD5Hashes."ClassIsland.zip" = (Get-FileHash ./out/ClassIsland.zip -Algorithm MD5).Hash
$legaceyMD5Hashes."ClassIsland_AssetsTrimmed.zip" = (Get-FileHash ./out/ClassIsland_AssetsTrimmed.zip -Algorithm MD5).Hash

Copy-Item ./doc/ChangeLogs/$($tagInfo.PrimaryVersion)/$version/App.md -Destination ./out/ChangeLogs.md -Force

$hashSummary | Add-Content ./out/ChangeLogs.md
"<!-- CLASSISLAND_PKG_MD5 $(ConvertTo-Json $legaceyMD5Hashes -Compress) -->" | Add-Content ./out/ChangeLogs.md 

ConvertTo-Json $versionInfo -Depth 99 | Out-File ./out/index.json

# Upload metadata
git config --global user.name 'classisland-bot'
git config --global user.email 'elf-elysia.noreply@classisland.tech'
git clone git@github.com:ClassIsland/metadata.git
cd metadata

git checkout -b metadata/disturb/$version
if ($(Test-Path ./metadata/disturb/$version) -eq $false) {
    mkdir ./metadata/disturb/$version
}
Copy-Item ../out/index.json -Destination ./metadata/disturb/$version/index.json -Force
$globalIndex = ConvertFrom-Json (Get-Content ./metadata/disturb/index.json -Raw)
$globalIndex.Versions = [System.Collections.ArrayList]@($globalIndex.Versions)
$globalIndex.Versions.Add(@{
    Version = $version
    Title = $version
    Channels = $tagInfo.Channels
    VersionInfoUrl = "https://get.classisland.tech/p/ClassIsland-Ningbo-S3/classisland/disturb/${version}/index.json"
})
ConvertTo-Json $globalIndex -Depth 99 | Out-File ./metadata/disturb/index.json
git add .
git commit -m "metadata(disturb): release $version at https://github.com/ClassIsland/ClassIsland/commit/$gitCommitId"
git push origin metadata/disturb/$version
gh pr create -R ClassIsland/metadata -t "Add metadata for $version" -b "Add metadata for $version at https://github.com/ClassIsland/ClassIsland/commit/$gitCommitId" -B main -a HelloWRC
