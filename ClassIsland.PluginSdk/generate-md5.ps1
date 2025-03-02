param($path)

$env:PSModulePath = "$PSHOME/Modules"

echo $path
cd $path
rm $path/*.md5sum

$files = Get-ChildItem $path
$hashes = [ordered]@{}
$summary = ""

foreach ($i in $files) {
    $name = $i.Name
    $hash = Get-FileHash $i -Algorithm MD5
    $hashString = $hash.Hash
    $hashes.Add($name, $hashString)
}

echo $hashes

$json = ConvertTo-Json $hashes -Compress

$summary +=  "`n<!-- CLASSISLAND_PKG_MD5 ${json} -->" 
echo $summary > "$path/checksums.md"

