$ErrorActionPreference = "Stop"
$artifacts = Get-ChildItem -Path ./out_artifacts -Directory

mkdir -p ./ci_tmp/sign_bundle
if ($(Test-Path ./out) -eq $false) {
    mkdir out
}

foreach ($artifact in $artifacts) {
    if ($artifact -eq "out_nupkg") {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./out/ -Recurse -Force
    if ($artifact.Name.Contains('windows') -eq $false) {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./ci_tmp/sign_bundle/ -Recurse -Force
}

# Move-Item ./ci_tmp/sign_bundle/out_app_windows_x64_full_wap.appx -Destination ./ci_tmp/sign_bundle/out_app_windows_x64_full_wap.msix
# Remove-Item ./out/out_app_windows_x64_full_wap.appx
