param($path)

function DisableWpfInDirectory{
    param(
        [string]$directory
    )
    $files = Get-ChildItem -Path $directory -Recurse -Include *.xaml.cs
    
    foreach ($i in $files) {
        $content = Get-Content -Raw $i.FullName
        if ($content -match "#if false") {
            Write-Output "Skipping $($i.FullName) as it already has a disabled section."
            continue
        }
        Write-Output "Processing $($i.FullName)"
        $newContent = "#if false" + "`n" + $content + "`n#endif"
        Set-Content -Path $i.FullName -Value $newContent
    }
}

DisableWpfInDirectory $path