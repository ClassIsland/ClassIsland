$ErrorActionPreference = "Stop"
$artifacts = Get-ChildItem -Path ./out_artifacts -Directory

if ($(Test-Path ./out) -eq $false) {
    mkdir out
}

if ($(Test-Path ./out_artifacts/signed_artifacts.zip) -eq $true) {
    Write-Host Combining signed artifacts.
    Expand-Archive -Path ./out_artifacts/signed_artifacts.zip -DestinationPath ./out_artifacts -Force 
} else {
    Write-Host Skipping combine signed artifacts.
}


foreach ($artifact in $artifacts) {
    if ($artifact -eq "out_nupkg.zip") {
        continue
    }
    if ($artifact -eq "signed_artifacts.zip") {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./out/ -Recurse -Force
}