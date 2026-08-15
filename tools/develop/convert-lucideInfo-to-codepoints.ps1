param (
    [Parameter(Mandatory=$true)]
    [string]$SourcePath
)

if (-not (Test-Path $SourcePath)) {
    Write-Error "Source file not found at: $SourcePath"
    exit
}

$FullSourcePath = (Get-Item $SourcePath).FullName
$ParentDir = Split-Path -Parent $FullSourcePath
$OutputPath = Join-Path -Path $ParentDir -ChildPath "lucide.json"

Write-Host "[info.json to codepoints Converter]-----"
Write-Host "Source: $FullSourcePath"
Write-Host "Output: $OutputPath"

$sourceData = Get-Content -Path $FullSourcePath -Raw | ConvertFrom-Json
$resultDict = [ordered]@{}

foreach ($prop in $sourceData.psobject.Properties) {
    $iconName = $prop.Name
    $unicodeString = $prop.Value.unicode
    
    if ($unicodeString -match '&#(\d+);') {
        $resultDict[$iconName] = [int]$matches[1]
    }
}

# Export
$resultDict | ConvertTo-Json -Depth 2 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "--------------------------------FINISHED"