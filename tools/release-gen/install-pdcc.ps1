$downloadUrl = "https://github.com/ClassIsland/PhainonDistributionCenter/releases/download/${env:PDC_CLIENT_VERSION}/out_app_linux_x64.zip"
$zipPath = "./out_app_linux_x64.zip"
$extractPath = "./pdcc"

if (!(Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath | Out-Null
}

Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

chmod +x "$extractPath/PhainonDistributionCenter.Client"
