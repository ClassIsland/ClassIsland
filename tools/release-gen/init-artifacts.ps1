$ErrorActionPreference = "Stop"

# Generate metadata & upload artifacts

if ($(Test-Path ./out) -eq $false) {
    mkdir out
}

$artifacts = Get-ChildItem -Path ./out_artifacts -Directory

foreach ($artifact in $artifacts) {
    if ($artifact.Name.Contains("out_app_") -ne $true) {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./out/ -Recurse -Force
}

foreach ($artifact in $(Get-ChildItem ./out)) {
    Move-Item $artifact.FullName -Destination $artifact.FullName.Replace("out_app_", "ClassIsland_app_") -Force
}
