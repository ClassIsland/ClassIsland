$PUBLISH_TARGET = ".\ClassIsland"

$ErrorActionPreference = "Stop"

Write-Host "Build starting..." -ForegroundColor Cyan
$env:git_latest_tag = git describe --abbrev=0 --tags
$env:git_commit_short = $(git rev-parse HEAD).substring(0, 7)
$env:git_current_branch = git branch --show-curren
echo "Git commit: ${env:git_commit_short} ${env:git_latest_tag} ${$env:git_current_branch}"
echo "APPVEYOR_REPO_TAG = ${env:APPVEYOR_REPO_TAG}"

pwsh -ep bypass -c .\tools\release-gen\publish.ps1 false

Write-Host "Generating MD5..." -ForegroundColor Cyan
pwsh -ep Bypass -c .\tools\generate-md5.ps1 ./out

Write-Host "Completed!" -ForegroundColor Green
