$ErrorActionPreference = "Stop"
$artifacts = Get-ChildItem -Path ./out_artifacts -Directory

if ($(Test-Path ./out) -eq $false) {
    mkdir out
}

foreach ($artifact in $artifacts) {
    if ($artifact -eq "out_nupkg") {
        continue
    }
    if ($artifact -eq "signed_artifacts") {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./out/ -Recurse -Force
}

if ($(Test-Path ./out_artifacts/signed_artifacts) -eq $true) {
    Write-Host Combining signed artifacts.
    Copy-Item -Path ./out_artifacts/signed_artifacts/* -DestinationPath ./out/ -Force 
} else {
    Write-Host Skipping combine signed artifacts.
}