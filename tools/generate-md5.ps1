param($path)

echo $path
rm $path/*.md5sum
rm "$path/checksums.md"
$files = Get-ChildItem $path
$hashes = [ordered]@{}
$summary = "
> [!info]
> 下载时请注意核对文件MD5是否正确。

| 文件名 | MD5 |
| --- | --- |
"

foreach ($i in $files) {
    $name = $i.Name
    $hash = Get-FileHash $i -Algorithm MD5
    $hashString = $hash.Hash
    $hashes.Add($name, $hashString)
    Write-Output $hash.Hash > "${i}.md5sum"
    $summary +=  "| $name | ``${hashString}`` |`n"
}

echo $hashes

$json = ConvertTo-Json $hashes -Compress

$summary +=  "`n<!-- CLASSISLAND_PKG_MD5 ${json} -->" 
echo $summary > "$path/checksums.md"