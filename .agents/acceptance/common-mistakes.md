# 项目易错点

以下为 2026-11-02 从源码核实的预防性记录，尚无本适配执行的历史验收轮次。后续实测条目应注明轮次、观察与证据，不能把推测写成已复现故障。

## 检查清单

- [ ] Windows 启动前核对已有实例及所属路径；第二次启动不等于新构建已接管。
- [ ] 检查实际打包类型、数据目录与共享全局配置，不能只改变工作目录就宣称数据隔离。
- [ ] Android 使用明确的设备序列号、当前构建 APK 与正确品牌包名。
- [ ] 按目标平台选择操作方法，不把桌面参数当作 Android 导航入口。
- [ ] 查看真实界面与必需证据；编译、安装、进程状态不能替代行为验收。

## W1：程序副本仍共享单实例锁

`ClassIsland/Program.cs` 为非 Android 进程创建 `Global\ClassIsland.Lock`。已有实例时 `--uri` 可经 IPC 发送给它，随后新进程退出。因此 URI 导航成功不足以证明正在验收刚编译的版本。先记录 PID、启动时间和程序路径，不能批量结束用户进程。

## W2：启动目录不等于数据隔离

`ClassIsland/App.axaml.cs` 在初始化时将工作目录切换到程序目录，并依据 `PackageType` 选择数据位置。`ClassIsland_PackageRoot` 参与打包根目录定位，不是任意数据目录参数。`ClassIsland.Core/Services/GlobalStorageService.cs` 另将全局配置保存在 `LocalApplicationData/ClassIsland/Global/Global.json`；独立程序目录仍会共享它。

## A1：默认 Debug 包不是正式包

`ClassIsland.Android/ClassIsland.Android.csproj` 将 Debug 的默认 `BrandType` 设为 Dev，包名为 `cn.classisland.android.dev`。Beta 与 Production 使用其他包名。部署、进程查询和证据必须指向同一包与设备；不能用卸载或清数据自动处理签名冲突。

## A2：桌面参数不提供 Android 导航

`ClassIsland/Program.cs` 的命令行解析只在非 Android 分支执行。`Application.cs` 向 `Program.AppEntry` 传递参数不能证明 Android 支持相同命令行；`MainActivity.cs` 的 `OnNewIntent` 当前处理通知激活，而非通用 `classisland://` 路由。应使用实际应用界面，不猜测额外参数或 Activity 类名。

## U1：原生界面需要原生证据

桌面入口使用 Avalonia，Android 入口使用 `Avalonia.Android`，不是 Web/Electron 应用。Debug `WithDeveloperTools()` 仅表明代码包含开发工具入口；不代表 MCP 自动化已连接。没有可用操作或捕获能力时披露未完成，不把浏览器空页、空控件树或日志无异常当作界面通过。

## E1：工具可执行不等于环境就绪

本次发现 .NET 10、`lh` 和 ADB，但 `dotnet workload list` 因 Service Control Manager 访问失败，未确认 Android 工作负载。可执行文件存在不能证明 SDK/JDK、设备连接、权限或部署可用；应分别检查并保留原始错误。本条是环境探测记录，不是产品验收失败记录。
