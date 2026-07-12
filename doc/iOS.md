# iOS / iPadOS 开发与构建

ClassIsland 的 iPhone 与 iPad 主界面由 Avalonia 统一实现。Swift 代码只存在于 ActivityKit bridge 和 Widget Extension 中；业务代码通过 `ClassIsland.Platforms.Abstraction` 提供的纯 C# API 调用实时活动与灵动岛。

## 使用 GitHub Actions 构建 IPA

工作流位于 `.github/workflows/build_ios.yml`：

- Pull Request 以及 `master`、`develop/v2/ios` 的相关提交会在 `macos-26` 上执行无签名的 iOS Simulator 构建。
- 手动运行 `Build iOS` 工作流会先通过 Simulator 构建，再生成已签名的 `ios-arm64` IPA。
- 推送形如 `ios-v2.0.0` 的 tag 也会使用 `ios-production` Environment 构建正式 IPA，因此 workflow 尚未合并到默认分支时仍可触发首次构建。
- IPA 上传前会验证主 App、Live Activity Extension、双 provisioning profile、代码签名，以及 ActivityKit 的 weak link（弱链接）。
- 构建结果和 SHA-256 文件会作为 GitHub Artifact 保留 14 天。

### 1. 创建 App ID 和 provisioning profile

每个品牌都需要两个显式 App ID，以及两个相同发布类型的 provisioning profile：

| GitHub Action 品牌 | 主 App Bundle ID | Extension Bundle ID | GitHub Environment |
| --- | --- | --- | --- |
| `production` | `cn.classisland.ios` | `cn.classisland.ios.LiveActivityExtension` | `ios-production` |
| `beta` | `cn.classisland.ios.beta` | `cn.classisland.ios.beta.LiveActivityExtension` | `ios-beta` |
| `dev` | `cn.classisland.ios.dev` | `cn.classisland.ios.dev.LiveActivityExtension` | `ios-dev` |

用于 TestFlight 或 App Store 时应创建 Apple Distribution 证书和 App Store provisioning profile。需要直接安装到已登记真机时，应为两个 App ID 创建 Ad Hoc profile。Development profile 只能配合 Apple Development 证书用于开发构建。

### 2. 配置 GitHub Environment Secrets

在仓库的 `Settings > Environments` 中创建所需的 `ios-production`、`ios-beta` 或 `ios-dev` Environment，并配置以下 Secrets：

| Secret | 内容 |
| --- | --- |
| `IOS_DISTRIBUTION_CERTIFICATE_BASE64` | 包含私钥的 `.p12` 证书 Base64 |
| `IOS_DISTRIBUTION_CERTIFICATE_PASSWORD` | `.p12` 的导出密码 |
| `IOS_APP_PROVISIONING_PROFILE_BASE64` | 主 App `.mobileprovision` 的 Base64 |
| `IOS_LIVE_ACTIVITY_PROVISIONING_PROFILE_BASE64` | Extension `.mobileprovision` 的 Base64 |

可在 macOS 上生成待粘贴的 Base64：

```bash
base64 -i ClassIsland.p12 | pbcopy
base64 -i ClassIsland.mobileprovision | pbcopy
base64 -i ClassIslandLiveActivityExtension.mobileprovision | pbcopy
```

不要提交 `.p12`、`.mobileprovision`、证书密码或 Apple Team 信息。工作流会从 profile 自动读取 Team ID 和 UUID，并检查 profile 的 Bundle ID，配置不匹配时会在签名前失败。

### 3. 运行工作流

进入仓库的 `Actions > Build iOS > Run workflow`，选择：

- `brand`：选择与 GitHub Environment 和 provisioning profile 一致的品牌；
- `version`：三个整数段组成的版本号，例如 `2.0.0`。

成功后，从该次运行的 Artifacts 中取得 `ClassIsland-iOS-<brand>-<version>-<run number>`。其中的 IPA 已包含 Avalonia 主应用、Swift bridge、实时活动 Widget Extension 和灵动岛布局。

如果 workflow 还没有进入仓库默认分支，可在目标提交上推送生产构建 tag：

```bash
git tag ios-v2.0.0
git push origin ios-v2.0.0
```

CI 会静态检查主程序和 bridge 的 iOS 13.0 minimum OS、ActivityKit weak load command，以及 `@rpath` Swift back-deployment 依赖。正式发布前仍需在 iOS 13 真机或对应旧 Simulator 上执行一次启动烟测。

## 本地构建已签名 IPA

本地发布必须在装有 Xcode 26.5 和 .NET 10 iOS workload 的 macOS 上执行。证书与两个 provisioning profile 需已安装到登录钥匙串和 provisioning profile 目录。

```bash
dotnet workload restore ClassIsland.iOS/ClassIsland.iOS.csproj

./build.sh PublishApp \
  --OsName ios \
  --Arch arm64 \
  --Package ipa \
  --BuildType selfContained \
  --BuildName app \
  --Configuration Release \
  --AppVersion 2.0.0 \
  --BuildNumber 1 \
  --BrandType Production \
  --CodesignKey "Apple Distribution: Example (TEAMID)" \
  --CodesignProvision "MAIN_PROFILE_UUID" \
  --ClassIslandLiveActivityCodesignProvision "EXTENSION_PROFILE_UUID" \
  --ClassIslandDevelopmentTeam "TEAMID"
```

产物位于 `out/out_app_ios_arm64_selfContained_ipa.ipa`。

Windows 可以编译和测试 C# 层，但无法执行 Xcode、签名 Widget Extension 或产出可安装的 IPA。

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
