$AppId = $env:GHAPP_ID  # 使用GitHub App的ID
$PrivateKey = $env:GHAPP_PRIVATE_KEY  # 使用GitHub App的Private Key
$InstallationId = $env:GHAPP_INSTALLATION_ID  # 安装ID

# 生成 JWT Token
$jwtHeader = @{ 'alg' = 'RS256'; 'typ' = 'JWT' }
$jwtPayload = @{
    'iat' = [int](Get-Date -UFormat %s)
    'exp' = [int](Get-Date -UFormat %s) + 600  # 设置有效期为10分钟
    'iss' = $AppId
}
$jwtToken = [JWT]::Encode($jwtPayload, $PrivateKey, 'RS256', $jwtHeader)

# 使用JWT生成安装token
$uri = "https://api.github.com/app/installations/$InstallationId/access_tokens"
$headers = @{
    "Authorization" = "Bearer $jwtToken"
    "Accept" = "application/vnd.github.v3+json"
}

$response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Post
$accessToken = $response.token

gh auth login --with-token $accessToken
echo "##vso[task.setvariable variable=GHAPP_TOKEN]$accessToken"