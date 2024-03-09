$PUBLISH_TARGET = ".\ClassIsland"

$env:git_latest_tag = git describe --abbrev=0 --tags
$env:git_commit_short = $(git rev-parse HEAD).substring(0, 7)
$env:git_current_branch = git branch --show-curren
echo "Git commit: ${env:git_commit_short} ${env:git_latest_tag} ${$env:git_current_branch}"
echo "APPVEYOR_REPO_TAG = ${APPVEYOR_REPO_TAG}"

dotnet publish .\ClassIsland\ClassIsland.csproj -c Release -p:PublishProfile=FolderProfile -p:PublishDir=$PUBLISH_TARGET -property:DebugType=embedded

echo "Successfully published to $PUBLISH_TARGET"

7z a ./ClassIsland/ClassIsland/ClassIsland.zip ./ClassIsland/ClassIsland/* -r -mx=9
