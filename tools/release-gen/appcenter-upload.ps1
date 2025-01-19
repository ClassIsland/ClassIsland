param($token, $version)
$APP = "HelloWRC/ClassIsland"

echo "Uploading AppCenter..."
echo "Version is $version, APP is $APP"

appcenter distribute release --group Collaborators --token $token -a $APP -f ./out/ClassIsland.zip -b $version
