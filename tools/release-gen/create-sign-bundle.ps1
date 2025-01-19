$ErrorActionPreference = "Stop"
$artifacts = Get-ChildItem -Path ./out_artifacts -Directory

mkdir ./ci_tmp/sign_bundle

foreach ($artifact in $artifacts) {
    if ($artifact -eq "out_nupkg.zip") {
        continue
    }
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./out/ -Recurse -Force
    Copy-Item ./out_artifacts/$($artifact.Name)/* -Destination ./ci_tmp/sign_bundle/ -Recurse -Force
}
