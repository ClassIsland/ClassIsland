$ErrorActionPreference = "Stop"
$AppId = $env:GHAPP_ID  # 使用GitHub App的ID
$PrivateKey = $env:GHAPP_PRIVATE_KEY  # 使用GitHub App的Private Key
$InstallationId = $env:GHAPP_INSTALLATION_ID  # 安装ID

$header = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject @{
  alg = "RS256"
  typ = "JWT"
}))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

$payload = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject @{
  iat = [System.DateTimeOffset]::UtcNow.AddSeconds(-10).ToUnixTimeSeconds()
  exp = [System.DateTimeOffset]::UtcNow.AddMinutes(5).ToUnixTimeSeconds()
   iss = $AppId 
}))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.ImportFromPem($PrivateKey)

$signature = [Convert]::ToBase64String($rsa.SignData([System.Text.Encoding]::UTF8.GetBytes("$header.$payload"), [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$jwt = "$header.$payload.$signature"

# 使用JWT生成安装token
$uri = "https://api.github.com/app/installations/${InstallationId}/access_tokens"
$headers = @{
    "Authorization" = "Bearer $jwt"
    "Accept" = "application/vnd.github.v3+json"
}

$response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Post
$accessToken = $response.token

$accessToken | gh auth login --with-token
$env:GITHUB_TOKEN = $accessToken