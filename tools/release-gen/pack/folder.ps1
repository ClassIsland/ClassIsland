$ErrorActionPreference = "Stop"

if ($(Test-Path ./out_pack/) -eq $false) {
    mkdir -p ./out_pack/pack/app-${env:version}/
}

Get-ChildItem -Path ./out

$appBaseName = "out_appBase_${env:osName}_${env:arch}_${env:buildType}_folder"
$launcherName = "out_launcher_${env:osName}_${env:arch}_aot_singleFile"

Expand-Archive "./out/${appBaseName}.zip" -DestinationPath ./out_pack/pack/app-${env:version}/ -Force
Expand-Archive "./out/${launcherName}.zip" -DestinationPath ./out_pack/pack/ -Force

Remove-Item ./out_pack/pack/*.pdb -Force
