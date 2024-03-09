param($token)
$APP = "HelloWRC/ClassIsland"

appcenter distribute release --group Collaborators --token $token -a $APP -f ./ClassIsland/ClassIsland/ClassIsland.zip
