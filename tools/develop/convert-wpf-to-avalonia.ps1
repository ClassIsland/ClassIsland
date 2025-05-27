param($path)

Write-Output "Start processing $path"
$files = Get-ChildItem -Path $path -Recurse -Include *.xaml
$fileCodes = Get-ChildItem -Path $path -Recurse -Include *.xaml.cs

foreach ($i in $files) {
    Write-Output "Processing $($i.FullName)"
    $content = Get-Content -Raw $i.FullName
    $newContent = $content.Replace("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "https://github.com/avaloniaui")
    Set-Content -Path $i.FullName -Value $newContent
    git mv $i.FullName $i.FullName.Replace('.xaml', '.axaml')
    
}
foreach ($i in $fileCodes) {
    Write-Output "Processing $($i.FullName)"
    $content = Get-Content -Raw $i.FullName
    $newContent = [regex]::Replace($content, "using System\.Windows.+;", "")
    Set-Content -Path $i.FullName -Value $newContent
    git mv $i.FullName $i.FullName.Replace('.xaml', '.axaml')
}