param($token, $version)
$APP = "HelloWRC/ClassIsland"

echo "Uploading AppCenter..."
echo "Version is $version, APP is $APP"

Copy-Item ./out/ClassIsland_app_windows_x64_full_singleFile.zip -Destination ./out/ClassIsland.zip -Force
appcenter distribute release --group Collaborators --token $token -a $APP -f ./out/ClassIsland.zip -b $version
