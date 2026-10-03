# iOS / iPadOS 开发与构建

ClassIsland 的 iPhone 与 iPad 主界面由 Avalonia 统一实现。Swift 代码只存在于 ActivityKit bridge 和 Widget Extension 中；业务代码通过 `ClassIsland.Platforms.Abstraction` 提供的纯 C# API 调用实时活动与灵动岛。

## 侧载版与 App Store 版

通过 MSBuild 属性 `ClassIslandIosDistribution` 选择发行版，默认值为 `Sideload`。发行版与 `Debug` / `Release`、`BrandType` 相互独立。

| 项目 | `Sideload` | `AppStore` |
| --- | --- | --- |
| 插件市场、安装、更新与执行 | 开启 | 禁用 |
| 插件设置及异常插件选项 | 显示 | 隐藏 |
| `.cipx` 文件类型声明 | 注册 | 不注册 |
| 正式 Bundle ID | `cn.classisland.ios.sideload` | `cn.classisland.ios` |
| 正式显示名称 | `ClassIsland` | `ClassIsland` |
| 托管代码裁剪 | 逐程序集 `copy`，保留插件可能调用的宿主 API | Release 使用 `partial` |

Beta 和 Dev 的 Bundle ID 同样在原有 ID 后附加 `.sideload`。两种发行版可以共存，各自使用独立沙盒；从旧版 `cn.classisland.ios` 切换到侧载版不会自动迁移数据，需要先导出再导入。Live Activity Extension 跟随宿主 Bundle ID 构建，签名时需要为实际的宿主及 Extension ID 配置描述文件。

侧载版使用 Mono 解释器执行托管插件，沿用现有 `.cipx` 格式与插件 API。安装或更新后，按应用提示手动结束并重新打开应用。插件应声明支持 `iOS`；Android 专用 API、运行时补丁、插件自带原生库和后台常驻能力不保证兼容，仍受 iOS 运行时及系统限制。不得将侧载版改为 Native AOT 或裁剪宿主 API。

侧载版使用 `TrimmerRootAssembly` 显式保留整个 iOS 入口程序集。仅设置 `TrimMode=copy` 不足以保护入口程序集：默认链接根只有 `Main`，可能删除由原生系统调用的 `AppDelegate` 构造函数和启动回调，导致系统转而调用泛型父类并在启动时崩溃。CI 会解包最终 IPA，检查入口构造函数、启动回调及其 IL；不能只凭 Debug 编译或注册器模式检查判断启动正常。

侧载版还在 `PrepareForILLink` 前逐项设置 `ManagedAssemblyToLink.TrimMode=copy`，保留共享配置模型和插件依赖的托管成员；全局设置不能替代每个程序集的链接操作。CI 使用链接前的五个核心程序集作基准，比较最终包中的公共类型、字段、方法重载数量和方法体，并拒绝 `Linked away` 占位实现；另外在宿主运行时加载包内共享库，测试集控配置的创建、保存和重新读取。运行这项侧载 IPA 校验需要当前构建的 `bin/Sideload/Release/net10.0/` 原始程序集。宿主检查不能替代 iOS 真机交互验证。

App Store 版在编译时移除插件加载器，并关闭安装处理、插件初始化、市场下载与刷新；复制插件到沙盒、恢复旧设置或打开插件链接都不会启用它。解释器本身可以用于应用内置代码，不代表开启外部插件功能。

普通编译验证（需要与 .NET iOS SDK 匹配的 Xcode）：

```sh
dotnet build ClassIsland.iOS/ClassIsland.iOS.csproj -c Debug -p:ClassIslandIosDistribution=Sideload -p:EnableCodeSigning=false -m:1
dotnet build ClassIsland.iOS/ClassIsland.iOS.csproj -c Release -p:ClassIslandIosDistribution=AppStore -p:EnableCodeSigning=false -m:1
```

NUKE 的 `PublishApp` 使用 `--IosDistribution Sideload|AppStore` 选择相同模式（默认 `Sideload`）。其它发布与签名参数不变，IPA 文件名附加发行版名称，避免混淆。托管构建产物分别存放在各项目的 `bin/<发行版>/` 与 `obj/<发行版>/` 下，Extension 的 Xcode 构建目录也按发行版隔离。不要仅通过修改 Bundle ID 把侧载包作为上架包。

无需 iOS 真机即可运行的插件回归测试：

```sh
dotnet test ClassIsland.Plugin.Tests/ClassIsland.Plugin.Tests.csproj -p:ClassIslandIosDistribution=Sideload -m:1
dotnet test ClassIsland.Plugin.Tests/ClassIsland.Plugin.Tests.csproj -p:ClassIslandIosDistribution=AppStore -m:1
```

这些测试覆盖待安装插件包的处理、上架版市场禁用、加载器是否包含在产物中，以及 Mono 路径下的托管插件加载与宿主 API 共享。它们不能替代 Release IPA 真机验证：还需要在侧载版安装带 Avalonia 设置页的插件，检查页面、依赖、重开生效和更新；在上架版确认入口隐藏、恢复插件目录及插件链接均无法启用插件。

## 使用 GitHub Actions 构建 unsigned IPA

工作流位于 `.github/workflows/build_release.yml`，由一个 `build_ios` job 的 `distribution: [Sideload, AppStore]` matrix 构建两个发行版，不需要 Apple 证书、provisioning profile 或 GitHub Environment Secrets。两个 matrix 实例分别校验和上传自己的 IPA。

- Pull Request、`master` 与 `develop/v2/ios` 的相关提交会通过仓库统一的 NUKE `PublishApp` 目标构建 Release `ios-arm64` 真机版本。
- 工作流运行平台抽象测试，并构建 Avalonia 主程序、Swift bridge 和 Live Activity Extension。
- 主程序 Bundle ID 由发行版决定，Extension 在宿主 ID 后附加 `.LiveActivityExtension`。
- 构建结果封装为标准 `Payload/ClassIsland.iOS.app` IPA，并生成 SHA-256 文件。
- 上传前会重新解包，检查 arm64、minimum OS、Swift back-deployment runtime、ActivityKit weak link、Extension 和 bridge，并确认没有签名与 provisioning profile。
- Artifact 保留 14 天，名称格式为 `ClassIsland-iOS-unsigned-<run number>-<run attempt>`。

当前只支持 iOS/iPadOS 真机的 `ios-arm64` RID。SoundFlow 1.2.1 没有提供 Simulator 原生 framework，因此 `iossimulator-*` 应用构建会在项目校验阶段给出明确错误。`.NET for iOS` 的 `XcodeProject` 集成会为纯 Swift bridge 生成包含 device 与 Simulator slice 的 XCFramework；该内部 slice 不代表应用支持 Simulator，也不会链接 SoundFlow。主程序和 Swift bridge 最低支持 iOS 15.0；Live Activity Extension 最低支持 iOS 16.1。

推送代码后，进入 `Actions > Build iOS` 打开对应运行，从 Artifacts 下载 unsigned IPA。也可以在工作流进入默认分支后通过 `Run workflow` 手动构建。

unsigned IPA 不能直接安装到普通 iPhone 或 iPad；安装前需由使用者通过自己的证书或侧载工具重新签名。仓库与 Action 不处理签名。

Windows 可以编译和测试 C# 层，但无法执行 Xcode、构建 Widget Extension 或封装 iOS 真机应用。

## 课程本地通知

iOS 最多保留 64 条 pending local notifications。ClassIsland 为其它系统通知预留 4 条，每次按时间顺序提交最近 60 条课程提醒，并向后扫描最多 60 天以填满这个窗口。应用进入前台、课表或提醒设置改变、NTP 同步结果改变、系统时间或时区改变时会立即重排；应用保持活跃时还会每 6 小时补齐一次。进入后台时，应用会申请一次有时限的 iOS background task，以完成挂起前最后一次重排并立即释放执行租约。

`DispatcherTimer` 在应用被 iOS 挂起后不会继续执行，上述短期 background task 也不是周期任务，因此滚动窗口不会在无限期后台状态中自行补充。用户重新打开或切回 ClassIsland 后会自动补齐，无需手动操作。需要长期完全不启动应用仍持续更新计划时，必须增加服务端 push 或合适的 iOS BackgroundTasks 方案，但系统仍不保证后台任务准点执行。

## 自动化中的打开行动

iOS/iPadOS 的自动化“打开”菜单提供网页链接、文件夹、文件预览和 App 链接：

- 网页链接支持 HTTP(S)，省略协议时使用 HTTPS。
- 文件夹会在“文件”App 中打开 ClassIsland 内的目录。通过选择器选取外部文件夹时，会将其复制到应用内，之后打开的是副本。
- 文件选择使用持久导入，运行时通过系统 Quick Look 预览，也可使用预览中的分享功能。导入内容不会随临时缓存清理而删除；文件格式是否支持预览取决于系统。
- App 链接接受目标 App 提供的 URL Scheme 或 Universal Link，需安装对应 App；Universal Link 也可能由系统交给浏览器。打开失败会记录在行动的错误详情中。

旧配置中的程序路径和终端命令不会在 iOS 上执行，编辑器会提示改选支持的行动；新建 App 链接使用独立类型，保留原配置含义。退出和重启行动仍不开放。

这些行动在应用运行时执行。切到其他 App 或进入后台后，iOS 可能挂起 ClassIsland，后续行动不保证继续或准时执行；文件预览需要 ClassIsland 位于前台。

回归测试分别运行 `dotnet test ClassIsland.Platforms.Abstractions.Tests/ClassIsland.Platforms.Abstractions.Tests.csproj` 和 `dotnet test ClassIsland.Automation.Tests/ClassIsland.Automation.Tests.csproj`。真机验证应覆盖：重启后仍能预览已选文件、关闭预览后再次打开、从“文件”App 返回、目标 App 未安装时的错误提示，以及导入程序路径和终端命令后的不支持提示。

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
