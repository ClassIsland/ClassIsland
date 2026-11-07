# 项目状态检查与导航

以下方法于 2026-11-02 从源码和本机工具帮助整理，尚未进行应用或设备实测。只记录已有能力，不引入测试接口或默认注入故障。运行和数据保护规则见 [PROJECT.md](PROJECT.md)，报告与证据规则见 [PROCESS.md](PROCESS.md)。

## Windows：实例与页面导航

启动前使用 `PROJECT.md` §2 的进程查询，确认目标实例归属。导航示例：

```powershell
dotnet run --project ClassIsland.Desktop/ClassIsland.Desktop.csproj -c Debug --no-build --no-launch-profile -- --uri classisland://app/settings/general
```

同样可导航到 `settings/appearance`、`settings/automation`、`profile`。依据为 `ClassIsland/Program.cs` 的参数与 IPC 处理、`ClassIsland/App.axaml.cs` 的路由注册，以及设置页的 `SettingsPageInfo`。没有实例时该命令启动应用；已有实例时可能向原实例转发，故必须在发送前核实 PID 和构建路径。

当前没有独立的页面状态或后台任务查询 CLI。导航后观察实际窗口及页面，结合该数据根目录的 `Logs` 排查；日志文本和进程存在不能替代界面检查。不要为方便导航去改系统 URI 协议注册。

## Android：安装、运行和前台状态

先按 `PROJECT.md` §2 选定 `$deviceSerial`，设置 `$appPackage = 'cn.classisland.android.dev'`，然后执行：

```powershell
adb -s $deviceSerial shell pm path $appPackage
adb -s $deviceSerial shell pidof $appPackage
adb -s $deviceSerial shell dumpsys activity activities
adb -s $deviceSerial shell dumpsys package $appPackage
```

分别核对安装位置、进程、前台 Activity 和包版本/权限。`dumpsys` 的字段随系统版本变化，不依赖某个固定字段名作为唯一判断；输出中可能包含其他应用信息，报告仅保留与目标相关的部分。系统 Activity 不等于 Avalonia 内部页面，内部页面仍需实际观察。

启动 Activity 从 `cmd package resolve-activity` 的结果取得，命令见 `PROJECT.md` §2。不硬编码生成的 Java 名称；不把桌面 `--uri`、`-dm` 参数当作 Android intent 能力。

## Android：日志与截图

以下示例在已经分配好的本轮目录中使用，`$roundDir` 须为实际轮次路径，`assets/` 已存在。日志 PID 使用刚查询到的目标进程值，进程重启后重新查询：

```powershell
$appProcessId = '<目标应用当前PID>'
adb -s $deviceSerial logcat -d --pid=$appProcessId |
    Set-Content -LiteralPath (Join-Path $roundDir 'assets/android-logcat.txt') -Encoding utf8
$deviceCapturePath = '/sdcard/classisland-acceptance-' + [guid]::NewGuid().ToString('N') + '.png'
adb -s $deviceSerial shell screencap -p $deviceCapturePath
adb -s $deviceSerial pull $deviceCapturePath (Join-Path $roundDir 'assets/android-screen.png')
```

分别检查日志、截图和拉取的退出码；截图后由指定检查者打开本地 PNG，确认目标应用、页面和可见结果。设备临时文件使用本轮唯一名称，记录路径，拉取成功后只删除该文件。避免在不确定 PowerShell 二进制重定向行为时将 PNG 直接经文本管道落盘。

`logcat -d` 不会清空日志；不要为了本轮执行 `logcat -c`。启动前崩溃或进程已消失时，不能用旧 PID 的空日志证明没有错误，应保留对应时间范围的相关系统日志并披露限制。时间行为、系统通知或声音另按要求捕获录像、相关系统界面或音频。

## 数据准备、权限与故障状态

当前没有经本适配实测的固定夹具、故障注入脚本或通用 mock 开关。需要特定配置或错误前提时，先沿源码确认真实数据入口，仅在本轮专用数据/设备中构造，并记录原始前提、变更和恢复方式。

- Windows 配置位置按 `PROJECT.md` §2 解析，不把仓库当前目录当作数据根目录；不默认修改共享 `Global.json`。
- Android 从设置界面观察通知、定位与悬浮窗授权；仅针对本轮需求改变测试设备权限，不默认授予所有权限。
- 不默认执行 `pm clear`、卸载、删除用户配置或替换全局配置；需要重置的场景必须以明确授权的测试数据为对象。
- Windows 伪移动端仅用于相应桌面场景，不能据此声明 Android 返回键、权限、后台服务或悬浮窗行为已通过。

后续新增模式应记录平台、适用前提、步骤、恢复方式和真实轮次来源；未经实测的推断必须保留标记。
