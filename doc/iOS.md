# iOS / iPadOS 开发与构建

ClassIsland 的 iPhone 与 iPad 主界面由 Avalonia 统一实现。Swift 代码只存在于 ActivityKit bridge 和 Widget Extension 中；业务代码通过 `ClassIsland.Platforms.Abstraction` 提供的纯 C# API 调用实时活动与灵动岛。

## 使用 GitHub Actions 构建 unsigned IPA

工作流位于 `.github/workflows/build_ios.yml`，不需要 Apple 证书、provisioning profile 或 GitHub Environment Secrets。

- Pull Request、`master` 与 `develop/v2/ios` 的相关提交会构建 `ios-arm64` 真机版本。
- 工作流运行平台抽象测试，并构建 Avalonia 主程序、Swift bridge 和 Live Activity Extension。
- 主程序使用正式 Bundle ID `cn.classisland.ios`，Extension 使用 `cn.classisland.ios.LiveActivityExtension`。
- 构建结果封装为标准 `Payload/ClassIsland.iOS.app` IPA，并生成 SHA-256 文件。
- 上传前会重新解包，检查 arm64、minimum OS、ActivityKit weak link、Extension 和 bridge，并确认没有签名与 provisioning profile。
- Artifact 保留 14 天，名称格式为 `ClassIsland-iOS-unsigned-<run number>-<run attempt>`。

推送代码后，进入 `Actions > Build iOS` 打开对应运行，从 Artifacts 下载 unsigned IPA。也可以在工作流进入默认分支后通过 `Run workflow` 手动构建。

unsigned IPA 不能直接安装到普通 iPhone 或 iPad；安装前需由使用者通过自己的证书或侧载工具重新签名。仓库与 Action 不处理签名。

Windows 可以编译和测试 C# 层，但无法执行 Xcode、构建 Widget Extension 或封装 iOS 真机应用。

## 通过 Files App 查看应用文件

iOS 与 iPadOS 版本已启用文件共享和原位打开，应用数据保存在可见的 `Documents/ClassIsland/Data` 目录。安装并至少启动一次 ClassIsland 后，可在 Files App 的“在我的 iPhone/iPad 上 > ClassIsland”中查看配置、课表、日志等文件。

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

`PublishAsync` 对同一个 `IntervalId` 执行更新；区间 ID 改变时会结束旧活动并创建新活动。非 iOS 平台、低于 iOS 16.1 的系统或用户关闭实时活动时，API 会安全返回 `Unsupported` 或 `Disabled`。

ActivityKit 单次内容数据不能超过 4 KB。前台运行时课程状态会自动同步；应用被系统挂起后若仍需实时切换课程，后续需接入 ActivityKit push 和 APNs。iPadOS 会显示系统支持的实时活动表面，但没有 iPhone 的 Dynamic Island 硬件区域。
