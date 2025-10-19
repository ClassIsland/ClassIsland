param($platform)

$downloadUrl = "https://github.com/ClassIsland/PhainonDistributionCenter/releases/download/${env:PDC_CLIENT_VERSION}/out_app_${platform}_x64.zip"
$zipPath = "./out_app_${platform}_x64.zip"
$extractPath = "./pdcc"

if (!(Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath | Out-Null
}

Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

if ($platform -eq "linux")
{
    chmod +x "$extractPath/PhainonDistributionCenter.Client"
}
