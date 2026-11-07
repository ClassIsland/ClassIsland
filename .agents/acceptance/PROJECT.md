# ClassIsland acceptance 项目适配

本文件说明本仓库如何运行；证据与报告要求遵循 [acceptance 技能](../skills/acceptance/SKILL.md)，执行顺序见 [PROCESS.md](PROCESS.md)。命令从仓库根目录运行，示例使用 PowerShell。本文覆盖 Windows 和 Android，源码核对日期为 2026-11-02；未进行应用启动或设备实测。

## 1. 项目概况

ClassIsland 是课表显示与教学辅助应用，使用共享的 Avalonia 界面和服务。

- Windows 桌面入口为 `ClassIsland.Desktop/Program.cs`，应用主体在 `ClassIsland/`，共享控件与服务在 `ClassIsland.Core/`，Windows 平台实现在 `platforms/ClassIsland.Platforms.Windows/`。
- Android 入口为 `ClassIsland.Android/Application.cs` 和 `MainActivity.cs`，目标框架为 `net10.0-android`，引用同一应用主体。
- SDK 以 `global.json` 为准；UI 版本以 `AvaloniaShared.props` 和项目引用为准。当前 Avalonia 为 `12.1.4`，不以旧说明中的版本号替代实际配置。

## 2. 运行环境

### 共同前提

- 使用 .NET 10 SDK；先执行 `dotnet --list-sdks` 检查。项目依赖 `vendors/EdgeTtsSharp` 子模块；缺失时按仓库指南初始化。
- 还原依赖需要可访问配置的 NuGet 源，包括 GitHub Package Registry；不要把源凭据写入适配文档或报告。
- 本地基础课表与设置功能不要求单独启动数据库、缓存、队列或 Web 服务。更新、插件市场、在线语音及集控等功能按本次需求检查相应网络与服务，不能用基础功能可运行推断它们可用。
- 验证使用 `dotnet build`，不运行 NUKE 发布任务或 `InitPluginDevEnv`。

### Windows

编译与启动：

```powershell
dotnet build ClassIsland.Desktop/ClassIsland.Desktop.csproj -c Debug
dotnet run --project ClassIsland.Desktop/ClassIsland.Desktop.csproj -c Debug --no-build --no-launch-profile
```

构建成功后再启动；`--no-launch-profile` 避免带入开发者自己的启动参数。普通 Windows 构建由 `CrossPlatformProps.props` 选择 Windows 平台及目标框架，不能证明其他平台代码可编译。

启动前检查进程与实际路径：

```powershell
Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -like 'ClassIsland*' -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'ClassIsland.Desktop')
    } |
    Select-Object ProcessId, CreationDate, ExecutablePath, CommandLine
```

已有实例时先确认它是否属于本轮、是否运行目标构建；无法确认时不得结束该实例或向它发送导航。查询失败或路径不可读也不能当作“没有实例”。桌面端使用 `Global\ClassIsland.Lock`，不同程序目录仍可能互相影响。

记录启动前后的 PID、启动时间和程序路径，区分 `dotnet run` 宿主与应用子进程。结束时从本轮应用界面执行退出；关闭设置窗口不代表应用退出。无法正常退出时，仅在再次核对身份后对本轮应用 PID 使用 `Stop-Process -Id <本轮应用PID>`，不要按名称批量结束进程。

### Android

需要 .NET Android 工作负载、匹配的 Android SDK/JDK，以及 API 24 或更新的真机或模拟器。先检查 `dotnet workload list`、`adb version` 和设备状态；缺少工具链时先记录缺项，不在普通验收中自行安装或升级。

```powershell
dotnet build ClassIsland.Android/ClassIsland.Android.csproj -c Debug
adb devices -l
```

显式选定状态为 `device` 的目标；多设备时禁止省略 `-s`。记录序列号、系统版本、分辨率、方向及初始运行状态。以下变量须替换为本轮目标：

```powershell
$deviceSerial = '<目标设备序列号>'
$appPackage = 'cn.classisland.android.dev'
adb -s $deviceSerial shell getprop ro.build.version.sdk
adb -s $deviceSerial shell pm path $appPackage
adb -s $deviceSerial shell pidof $appPackage
```

Debug 默认品牌为 Dev。以下 APK 路径对应默认构建；若构建设置了其他输出路径或运行时标识，以本次构建日志为准，不能安装旧目录中碰巧存在的 APK。

```powershell
$apkPath = (Resolve-Path 'ClassIsland.Android/bin/Debug/net10.0-android/cn.classisland.android.dev-Signed.apk').Path
adb -s $deviceSerial install -r $apkPath
adb -s $deviceSerial shell cmd package resolve-activity --brief -a android.intent.action.MAIN -c android.intent.category.LAUNCHER $appPackage
```

检查每条命令的退出码和输出。安装前确认目标为验收设备及 Dev 包，`install -r` 会更新既有安装并保留数据；签名冲突时停止，不以卸载或清数据绕过问题。从解析结果取得实际 `包名/Activity`，不要猜测 Java 类名：

```powershell
$launchComponent = '<解析结果中的包名/Activity>'
adb -s $deviceSerial shell am start -W -n $launchComponent
```

启动后检查实际界面，不以 `am start` 返回成功作为通过依据。收尾时，仅对本轮拥有的应用会话使用 `adb -s $deviceSerial shell am force-stop $appPackage`；不要关闭用户已有的模拟器或执行 `adb kill-server`。

### 数据目录与隔离

目录规则来自 `ClassIsland/App.axaml.cs` 的 `ActivatePackageType`、`ActivateAppDirectories`，以及 `ClassIsland.Core/CommonDirectories.cs`：

| 情形 | 应用数据根目录 |
| --- | --- |
| Windows 未识别到打包类型的普通运行 | 默认 `./`；初始化时工作目录切换到程序所在目录 |
| `folder` 打包 | `AppPackageRoot/data` |
| `installer`、`msix`、`apk` 等安装类型 | `Environment.SpecialFolder.ApplicationData` 下的 `ClassIsland/Data` |

桌面端通过 `PackageType` 文件识别类型，`ClassIsland_PackageRoot` 影响打包根目录的查找；它不是通用的数据目录覆盖参数。启动前检查该变量指向的目录（未设置时为程序目录）及程序上级目录中的 `PackageType`，不要仅凭启动命令推断数据位置。`Settings.json`、`Config`、`Logs` 等均依赖实际数据根目录。

Windows 全局配置另存于 `%LOCALAPPDATA%/ClassIsland/Global/Global.json`，不随程序副本隔离。Android 路径由运行时的应用沙箱决定，不把 Windows 路径套用到设备。准备数据时使用本轮专用目录或测试设备，记录已有数据与修改范围；不默认导入用户配置、清空数据或恢复整份共享配置。

## 3. 身份与权限

- 基础本地功能没有统一的账号登录前置条件，也没有本适配提供的测试账号或账号播种命令；通过实际进入课表或设置界面检查可操作状态。
- 集控授权与产品账号不是同一概念。先检查目标数据中的集控状态；需要凭据时使用本次授权的测试配置，不记录明文凭据。Windows 的 `-dm` 会在本次会话禁用集控，仅用于明确不测试集控的场景，不能默认添加或用于证明受管场景通过。
- Android 按功能检查通知、定位、悬浮窗等权限，记录允许或拒绝的初始状态与本轮变更。权限清单见 `ClassIsland.Android/AndroidManifest.xml`，请求逻辑见 `MainActivity.cs` 及平台服务；清单声明不等于权限已授予。
- LobeHub 身份只影响验收报告去向。使用 `lh acceptance preflight --json` 检查模式与报告语言；不要为了应用验收触发账号登录。

## 4. 操作与取证界面

### Windows 原生桌面

使用当前环境可用的 Windows 原生桌面操作工具，操作前读取该工具所属技能。需要结构化查看 Avalonia 界面时，先发现实际可用的 DevTools 能力；源码中的 Debug `WithDeveloperTools()` 不代表自动化连接已经配置。应用是原生 Avalonia，不提供 Web 基址或 Electron CDP 端口，不能用浏览器截图替代应用界面证据。

界面操作后捕获目标窗口或所需系统界面，记录构建、窗口、显示缩放与场景。截图必须由指定检查者实际打开查看后才能支持通过结论。

### Android 设备界面

使用选定设备的真实界面与 ADB，状态和捕获命令见 [probe-mock-patterns.md](probe-mock-patterns.md)。普通 UI 自动化未必能读取 Avalonia 内部控件；空控件树不能证明界面为空，须结合截图与实际操作。

ADB 的安装、进程和日志输出只能证明相应状态；可见结果仍需界面证据。动画、交互顺序和时间行为使用相应录像，声音结果需要音频。所需驱动或捕获能力不可用时记录未完成项，不改用编译成功代替。

## 5. 检查与快速导航

- Windows 实例检查：使用 §2 的进程查询，核对路径与 PID；没有独立的当前路由或后台任务查询 CLI，需检查可见页面和相关日志。
- Windows 导航：`--uri` 支持 `classisland://app/settings/general`、`classisland://app/settings/appearance`、`classisland://app/settings/automation` 和 `classisland://app/profile`。参数示例与 IPC 注意事项见 [probe-mock-patterns.md](probe-mock-patterns.md)。
- Android 安装与进程检查：使用 §2 的 `pm path`、`pidof`；前台 Activity、设备日志与截图方法见 [probe-mock-patterns.md](probe-mock-patterns.md)。Activity 不等于应用内当前页面，页面状态须观察界面。
- Android 没有从源码确认的通用外部 URI 快速导航入口；通过应用内可见入口操作。不向 Activity 任意添加桌面 `--uri`、`--mobile` 或 `-dm` 参数。
- 身份、权限与运行中操作均通过对应界面及相关日志检查，不假设存在 HTTP 健康检查、鉴权探针或任务列表服务。

## 6. 已知限制

- 独立程序目录不能隔离 Windows 全局锁和全局配置；向已有实例发送 URI 也不能证明新构建已启动。
- Android 最低 API 24；Debug、Beta、Production 包名分别为 `cn.classisland.android.dev`、`cn.classisland.android.beta`、`cn.classisland.android`，不能混用证据或部署目标。
- Android Debug 使用 CoreCLR 并将程序集嵌入 APK，见项目文件；当前配置不应为加快部署而随意修改。模拟器结果不能自动代表真机表现。
- Linux/macOS 不在本适配的操作指引范围内；Windows 伪移动端模式不能替代 Android 验收。
- 2026-11-02 的本机只读探测发现 .NET SDK `10.0.301`、`lh 0.0.73`、ADB `1.0.41`（工具版本 `37.0.0-14910828`）。`lh` 未登录，模式为 `local`、语言为 `unset`，当时按对话语言使用中文。每轮重新核对，不能把这些机器状态当成团队默认值。
- 当时 `dotnet workload list` 因无法访问 Service Control Manager 失败，Android 工作负载完整性未确认；没有启动应用、连接设备验收或验证 UI 驱动。本文中的运行、安装与捕获步骤均为源码及工具帮助核对后的指引，尚未实测。
