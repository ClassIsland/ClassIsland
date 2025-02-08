param($is_trim, $isReleaseSigning)

$ErrorActionPreference = "Stop"

$env:Wap_Project_Directory = "./ClassIsland.Packaging"
$env:Wap_Project_Path = "./ClassIsland.Packaging/ClassIsland.Packaging.wapproj"
$env:Appx_Bundle = "Nerver"
$env:Appx_Bundle_Platforms = "AnyCPU"
$env:Appx_Package_Build_Mode = "SideloadOnly"

$tag = $(git describe --tags --abbrev=0)
$ver = [System.Version]::Parse($tag)

if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    Remove-Item out/* -Recurse -Force
}

if (!$isReleaseSigning) {
    $pfx_cert_byte = [System.Convert]::FromBase64String($env:test_signing_key)
    $certificatePath = Join-Path -Path $env:Wap_Project_Directory -ChildPath TestSigningKey.pfx
    [IO.File]::WriteAllBytes("$certificatePath", $pfx_cert_byte)
}

if ($isReleaseSigning) {
    $releasePublisher = 'Publisher="CN=SignPath Foundation, O=SignPath Foundation, L=Lewes, S=Delaware, C=US"'
    $ciTestPublisher = 'Publisher="' + "CN=Test certificate for 'ClassIsland [OSS]'" + '"'
    $manifestRaw = Get-Content ./ClassIsland.Packaging/Package.appxmanifest -Raw
    $manifestRaw.Replace('Publisher="CN=ClassIsland_TestSigning"', $env:isTestMode -eq "false" ? $releasePublisher : $ciTestPublisher) | Set-Content ./ClassIsland.Packaging/Package.appxmanifest
}

$manifestRaw = Get-Content ./ClassIsland.Packaging/Package.appxmanifest -Raw
$manifestRaw.Replace('Version="0.0.0.0"', 'Version="' + $ver + '"') | Set-Content ./ClassIsland.Packaging/Package.appxmanifest

# clean
msbuild ./ClassIsland.sln /t:Clean /p:Configuration="Release_MSIX" /p:RuntimeIdentifiers="win-x64" /p:Platform="x64" /p:PlatformTarget="x64"
# restore
msbuild ./ClassIsland.sln /t:Restore /p:Configuration="Release_MSIX" /p:RuntimeIdentifiers="win-x64" /p:Platform="x64" /p:PlatformTarget="x64"
# build
if ($isReleaseSigning) {
    msbuild .\ClassIsland.Packaging\ClassIsland.Packaging.wapproj /p:Configuration=Release_MSIX /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true /p:AppxPackageDir="Packages" /p:RuntimeIdentifiers="win-x64" /p:Platform="x64" /p:PlatformTarget="x64" /p:PublishBuilding=true /p:AppxPackageSigningEnabled=false
} else {
    msbuild .\ClassIsland.Packaging\ClassIsland.Packaging.wapproj /p:Configuration=Release_MSIX /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Never /p:PackageCertificateKeyFile=TestSigningKey.pfx /p:PackageCertificatePassword="" /p:GenerateAppxPackageOnBuild=true /p:AppxPackageDir="Packages" /p:RuntimeIdentifiers="win-x64" /p:Platform="x64" /p:PlatformTarget="x64" /p:PublishBuilding=true 
}

Copy-Item ClassIsland.Packaging\Packages\*\*.appx .\out\${env:artifact_name}.appx -Force

Write-Host "Successfully published!" -ForegroundColor Green

