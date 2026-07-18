---
name: cipx-dev-env
description: 配置 ClassIsland 插件开发环境：检查操作系统、安装 .NET SDK/Git/PowerShell Core、配置环境变量、安装项目模板。
---

# 配置开发环境

## 系统要求

请保证你的操作系统为：`Windows 10 1803` 及以上的操作系统，`x86_64` 架构。如果不是，请拒绝用户的请求。

## 安装必要软件

检查你的系统是否安装了以下软件：

- .NET 8 & 9 SDK
- Git
- PowerShell Core（`pwsh.exe`）

> [!WARNING]
> 请不要使用 Windows PowerShell（`powershell.exe`）。使用 Windows PowerShell 运行构建脚本造成的后果自负。

如果没有安装，请使用以下命令安装：

```pwsh
# PowerShell Core
winget install Microsoft.PowerShell

# .NET SDK
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.DotNet.SDK.9

# Git
winget install --id Git.Git -e --source winget
```

## 配置环境变量

检查环境变量 `ClassIsland_DebugBinaryDirectory` 和 `ClassIsland_DebugBinaryFile` 是否存在。如果不存在，则说明你还没有配置 ClassIsland 的开发环境，请要求用户按照 https://docs.classisland.tech/dev/get-started/development-plugins.html 的步骤配置开发环境。

## 安装项目模板

安装项目模板：

````pwsh
dotnet new install ClassIsland.PluginTemplate.Packaging

## 更多资源与诊断

- 官方开发快速入门： https://docs.classisland.tech/dev/get-started/development-plugins.html
- 检查还原和构建问题：
```pwsh
dotnet --info
git submodule update --init --recursive
dotnet restore
dotnet build
````

- 在仓库根目录的 `global.json` 中查看目标 SDK 版本，确保本地 SDK 与之兼容：

```pwsh
Get-Content global.json
```

- 在 CI 中建议的步骤（快速示例，GitHub Actions）：

```yaml
name: Plugin CI
on: [push, pull_request]
jobs:
	build:
		runs-on: windows-latest
		steps:
			- uses: actions/checkout@v4
			- uses: actions/setup-dotnet@v4
				with:
					dotnet-version: '9.0.x'
			- run: dotnet restore
			- run: dotnet build --configuration Release
			- run: dotnet publish -p:CreateCipx=true
			- run: echo "Collect cipx artifacts from ./cipx"
```

```

```
