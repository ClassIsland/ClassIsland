# iOS / iPadOS 开发与构建

ClassIsland 的 iPhone 与 iPad 主界面由 Avalonia 统一实现。Swift 代码只存在于 ActivityKit bridge 和 Widget Extension 中；业务代码通过 `ClassIsland.Platforms.Abstraction` 提供的纯 C# API 调用实时活动与灵动岛。

## 使用 GitHub Actions 构建 unsigned IPA

工作流位于 `.github/workflows/build_ios.yml`，不需要 Apple 证书、provisioning profile 或 GitHub Environment Secrets。

- Pull Request、`master` 与 `develop/v2/ios` 的相关提交会通过仓库统一的 NUKE `PublishApp` 目标构建 Release `ios-arm64` 真机版本。
- 工作流运行平台抽象测试，并构建 Avalonia 主程序、Swift bridge 和 Live Activity Extension。
- 主程序使用正式 Bundle ID `cn.classisland.ios`，Extension 使用 `cn.classisland.ios.LiveActivityExtension`。
- 构建结果封装为标准 `Payload/ClassIsland.iOS.app` IPA，并生成 SHA-256 文件。
- 上传前会重新解包，检查 arm64、minimum OS、Swift back-deployment runtime、ActivityKit weak link、Extension 和 bridge，并确认没有签名与 provisioning profile。
- Artifact 保留 14 天，名称格式为 `ClassIsland-iOS-unsigned-<run number>-<run attempt>`。

当前只支持 iOS/iPadOS 真机的 `ios-arm64` RID。SoundFlow 1.2.1 没有提供 Simulator 原生 framework，因此 `iossimulator-*` 应用构建会在项目校验阶段给出明确错误。`.NET for iOS` 的 `XcodeProject` 集成会为纯 Swift bridge 生成包含 device 与 Simulator slice 的 XCFramework；该内部 slice 不代表应用支持 Simulator，也不会链接 SoundFlow。主程序和 Swift bridge 最低支持 iOS 15.0；Live Activity Extension 最低支持 iOS 16.1。

推送代码后，进入 `Actions > Build iOS` 打开对应运行，从 Artifacts 下载 unsigned IPA。也可以在工作流进入默认分支后通过 `Run workflow` 手动构建。

unsigned IPA 不能直接安装到普通 iPhone 或 iPad；安装前需由使用者通过自己的证书或侧载工具重新签名。仓库与 Action 不处理签名。

Windows 可以编译和测试 C# 层，但无法执行 Xcode、构建 Widget Extension 或封装 iOS 真机应用。

## 课程本地通知

iOS 最多保留 64 条 pending local notifications。ClassIsland 为其它系统通知预留 4 条，每次按时间顺序提交最近 60 条课程提醒，并向后扫描最多 60 天以填满这个窗口。应用进入前台、课表或提醒设置改变、NTP 同步结果改变时会立即重排；应用保持活跃时还会每 6 小时补齐一次。

`DispatcherTimer` 在应用被 iOS 挂起后不会继续执行，因此滚动窗口不会在无限期后台状态中自行补充。用户重新打开或切回 ClassIsland 后会自动补齐，无需手动操作。需要长期完全不启动应用仍持续更新计划时，必须增加服务端 push 或合适的 iOS BackgroundTasks 方案，但系统仍不保证后台任务准点执行。

## 通过 Files App 查看应用文件

iOS 与 iPadOS 版本已启用文件共享和原位打开，应用数据保存在可见的 `Documents/ClassIsland/Data` 目录。安装并至少启动一次 ClassIsland 后，可在 Files App 的“在我的 iPhone/iPad 上 > ClassIsland”中查看配置、课表、日志等文件。

从 Files App 选择的 security-scoped 文件会复制到 `Documents/ClassIsland/ImportedFiles`，避免选择器关闭后授权失效。这里也可能保存被自定义图片、音频或跨手动重开导入流程继续引用的文件，因此应用不会按时间自动删除；可在“设置 > 存储 > iOS 导入文件”中查看或在确认不再引用后手动清空。

## 从 C# 调用实时活动和灵动岛

业务代码不需要引用 Swift 类型：

```csharp
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;

var service = PlatformServices.LiveActivityService;
if (service.Availability == LiveActivityAvailability.Available)
{
    var result = await service.PublishAsync(new LessonLiveActivityContent(
        IntervalId: "lesson-2026-07-12-3",
        Phase: LessonLiveActivityPhase.OnClass,
        Title: "数学",
        Subtitle: "第 3 节",
        Detail: "高一（1）班 · 302 教室",
        CompactText: "数学",
        StartTime: DateTimeOffset.Now,
        EndTime: DateTimeOffset.Now.AddMinutes(40)));

    if (!result.IsSuccess)
    {
        // 根据 result.Code 和 result.ErrorMessage 记录或降级处理。
    }
}

await service.EndAsync(LiveActivityDismissalPolicy.Immediate);
```

`PublishAsync` 会复用当前由 ClassIsland 创建的 Activity，并平滑更新其 `ContentState`；`IntervalId` 仅用于业务日志和内容去重，不会强制删除并新建 Activity。非 iOS 平台、低于 iOS 16.1 的系统或用户关闭实时活动时，API 会安全返回 `Unsupported` 或 `Disabled`。

ActivityKit 单次内容数据不能超过 4 KB。应用在前台时，“下一节课”实时活动会使用与“准备上课”本地通知相同的课程 attached settings、室内/室外提前量和 channel 开关，并在计划提醒时间开始显示。若应用在该时刻已被系统挂起且此前没有活动，iOS 不允许本地代码在后台准点新建 Live Activity；要让本地通知与首次创建在后台也严格同步，必须由服务端通过 APNs Activity push 启动。已有活动的前台课程状态会平滑更新。iPadOS 会显示系统支持的实时活动表面，但没有 iPhone 的 Dynamic Island 硬件区域。
