if ($(Test-Path ./out) -eq $false) {
    mkdir out
} else {
    rm out/* -Recurse -Force
}


7z a ./out/ClassIsland.zip ./out_artifacts/out_app_assetsTrimmed_false/* -r -mx=9
7z a ./out/ClassIsland_AssetsTrimmed.zip ./out_artifacts/out_app_assetsTrimmed_true/* -r -mx=9
cp ./out_artifacts/out_nupkgs/* -Destination ./out/

./tools/generate-md5.ps1 ./out/

echo "Pre publish success!"
