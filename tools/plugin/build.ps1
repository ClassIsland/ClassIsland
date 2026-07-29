param($quiet=$true)

$ErrorActionPreference = 'Stop'
$scriptPath =  $MyInvocation.MyCommand.Definition
$classIslandRoot = "$([System.IO.Path]::GetDirectoryName($scriptPath))\..\..\ClassIsland.Desktop" 


function SetEnvironmentVariable {
    param (
        $Name, $Value
    )
    $out = "$Name = $Value"
    Write-Host $out -ForegroundColor DarkGray
    [Environment]::SetEnvironmentVariable($Name, $Value, 1)
}

Set-Location $classIslandRoot


try {
    $quietparams = ""
    if ($quiet){
        $quietparams = ("-p:WarningLevel=0", "-p:NoWarn=NU1701")
    }
    dotnet --version
    Write-Host "🔧 正在清理…" -ForegroundColor Cyan

    dotnet clean ClassIsland.Desktop.csproj
    Write-Host "🔧 正在构建 ClassIsland，这可能需要 1-6 分钟。" -ForegroundColor Cyan
    dotnet build ClassIsland.Desktop.csproj -c Debug -p:Version=$(git describe --tags --abbrev=0) -p:NuGetVersion=$(git describe --tags --abbrev=0) $quietparams
}
catch {
    Write-Host "🔥 构建失败" -ForegroundColor Red
    return
}


Write-Host "🔧 正在设置开发环境变量…" -ForegroundColor Cyan

[Environment]::SetEnvironmentVariable("ClassIsland_DebugBinaryFile", [System.IO.Path]::GetFullPath("${classIslandRoot}\bin\Debug\net10.0-windows10.0.19041.0\ClassIsland.Desktop.exe"), 1)
[Environment]::SetEnvironmentVariable("ClassIsland_DebugBinaryDirectory", [System.IO.Path]::GetFullPath("${classIslandRoot}/bin\Debug\net10.0-windows10.0.19041.0\"), 1)

Write-Host "构建完成" -ForegroundColor Green
