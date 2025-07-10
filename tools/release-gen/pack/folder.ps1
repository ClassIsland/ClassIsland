if ($(Test-Path ./out_pack/) -eq $false) {
    New-Item ./out_pack
    New-Item ./out_pack/pack
}

$artifacts = Get-ChildItem ./out

$appBaseName = "out_appBase_${env:osName}_${env:arch}_${env:buildType}_folder"
$launcherName = "out_launcher_${env:osName}_${env:arch}_aot_singleFile"

Copy-Item ./out/$appBaseName/* -Destination ./out_pack/pack/app-${env:version}/ -Recurse -Force
Copy-Item ./out/$launcherName/* -Destination ./out_pack/pack/ -Recurse -Force
Remove-Item ./out_pack/pack/*.pdb -Force
