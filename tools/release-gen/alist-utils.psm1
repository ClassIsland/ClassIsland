function UploadFile {
    param (
        $path, $relative_path
    )
    
    $targetPath = [System.IO.Path]::Combine($relative_path, [System.IO.Path]::GetFileName($path))
    $form = @{
        file = Get-Item $path
    }
    $headers = @{
        Authorization = $env:ALIST_KEY
        "File-Path" = $targetPath
    }

    Invoke-WebRequest -Uri "${env:ALIST_HOST}/api/fs/form" -Method Put -Headers $headers -Form $form -MaximumRetryCount 5 -RetryIntervalSec 15 > $null
    # echo "Uploading $path -> $targetPath"
}

function UploadFolder {
    param (
        $folder, $relative_path
    )
    $files = $(Get-ChildItem $folder)
    foreach ($i in $files) {
        Write-Output $i.FullName
        if ($(Test-Path $i -PathType Container)) {
            UploadFolder $i.FullName $([System.IO.Path]::Combine($relative_path, [System.IO.Path]::GetFileName($i)))
        } else {
            UploadFile $i.FullName $relative_path
        }
    }
}
