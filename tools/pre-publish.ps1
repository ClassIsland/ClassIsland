if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}

cp ./out_artifacts/out_app_assetsTrimmed_false/ClassIsland.zip -Destination ./out/ClassIsland.zip
cp ./out_artifacts/out_app_assetsTrimmed_true/ClassIsland.zip -Destination ./out/ClassIsland_AssetsTrimmed.zip
cp ./out_artifacts/out_nupkgs/* -Destination ./out/

./tools/generate-md5.ps1 ./out/

echo "Pre publish success!"
