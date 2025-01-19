$ErrorActionPreference = "Stop"

Install-Module powershell-yaml -Force
Import-Module ./tools/release-gen/alist-utils.psm1

function GenerateDownloadInfo {
    param (
        $artifact_name,
        $artifact_path
    )

}

# Generate metadata & upload artifacts
Copy-Item ./out_signed/* -Destination ./out/ -Recurse -Force

$version = $(git describe --abbrev=0 --tags)
$gitCommitId = $(git rev-parse HEAD)
$infoYaml = $(git tag -l --format='%(contents)' $version) | Join-String -Separator "`n"
$tagInfo = $(ConvertFrom-Yaml $infoYaml)
$versionInfo = @{
    Version = $version
    Title = $version
    DownloadInfos = @{}
    ChangeLogs = $(Get-Content ./doc/ChangeLogs/$($tagInfo.primaryVersion)/App.md -Raw)
    Channels = @()
}
$versionInfo.Channels = $tagInfo.Channels

$artifacts = $(Get-ChildItem ./out)
foreach ($artifact in $artifacts){
    if ($artifact.Name.StartsWith("out_app") -ne $true) {
        continue
    }
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
    $versionInfo.DownloadInfos[$([System.IO.Path]::GetFileNameWithoutExtension($artifact.Name))] = $downloadInfo
}

Copy-Item ./doc/ChangeLogs/$($tagInfo.primaryVersion)/App.md -Destination ./out/ChangeLogs.md -Force
ConvertTo-Json $versionInfo -Depth 99 | Out-File ./out/index.json

# Upload metadata
$token_base64 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($env:GITHUB_TOKEN))
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
gh pr create -R ClassIsland/metadata -t "Add metadata for $version" -b "Add metadata for $version at https://github.com/ClassIsland/ClassIsland/commit/$gitCommitId" -B main
