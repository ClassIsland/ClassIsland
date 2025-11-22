$ErrorActionPreference = "Stop"

$appPath = "./out_pack/pack/app-${env:version}-0/"
$rootPath = "./out_pack/pack/"

if ($(Test-Path ./out_pack/) -eq $false) {
    mkdir -p $appPath
}

Get-ChildItem -Path ./out

# Install PDCC

./tools/release-gen/install-pdcc.ps1 ${env:osName}

$appBaseName = "out_appBase_${env:osName}_${env:arch}_${env:buildType}_folder"
$launcherName = "out_launcher_${env:osName}_${env:arch}_aot_singleFile"

Expand-Archive "./out/${appBaseName}.zip" -DestinationPath $appPath -Force
Expand-Archive "./out/${launcherName}.zip" -DestinationPath $rootPath -Force

Remove-Item $rootPath/*.pdb -Force

$env:PDCC_version = ${env:version}
./pdcc/PhainonDistributionCenter.Client ./phainon.yml GenerateFileMap $rootPath
Copy-Item $rootPath/files.json -Destination $appPath -Force
