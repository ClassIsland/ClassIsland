# 在 Windows 7 上安装 ClassIsland

> [!warning]
> **不建议在 Windows 10 以下的操作系统上运行 ClassIsland。** 

如果要在 Windows 7 上正常运行 ClassIsland，请跟随本指引做以下准备工作。

## 1. 安装依赖项

您需要按照[此处](https://learn.microsoft.com/zh-cn/dotnet/core/install/windows?tabs=net80#additional-deps)的指引根据您的操作系统版本安装额外的依赖项。

对于 Windows 7 ，以下是您需要额外安装的依赖项：

- Microsoft Visual C++ 2015-2019 Redistributable [64 位](https://aka.ms/vs/16/release/vc_redist.x64.exe) / [32 位](https://aka.ms/vs/16/release/vc_redist.x86.exe)
- KB3063858 [64 位](https://www.microsoft.com/download/details.aspx?id=47442) / [32 位](https://www.microsoft.com/download/details.aspx?id=47409)

## 2. 处理内存泄漏问题

.NET 7 以及以上的运行时在 Windows 7 会产生严重的内存泄漏问题（[#91](https://github.com/HelloWRC/ClassIsland/issues/91)），您需要应用以下的环境变量来解决这个问题。

| 环境变量 | 值 |
| --- | --- |
| `DOTNET_GCName` | `clrgc.dll` |
| `DOTNET_EnableWriteXorExecute` | `0` |

**您可以下载我们的[修复工具](https://github.com/ClassIsland/ClassIsland.RepairToolForWindows7/releases/download/v1.0.0/RepairToolForWindows7.bat)进行一键修复**

您也可以在命令提示符中运行以下的命令快速完成环境变量的设置。

```
setx DOTNET_GCName clrgc.dll
setx DOTNET_EnableWriteXorExecute 0
```


完成以上准备工作后，跟随[README](../README.md#开始使用)剩余部分的指引即可完成 ClassIsland 的安装。
