# TrollStore 发行版与悬浮主界面（阶段 1）

> 本文档描述 ClassIsland iOS/iPadOS 的第三种发行版 `TrollStore`，以及阶段 1 的「系统级悬浮主界面」增强包。
> 该发行版只能通过 [TrollStore](https://github.com/opa334/TrollStore) 安装，不能上架 App Store，也不能作为普通侧载包直接安装。

## 目标与范围

阶段 1 希望在不越狱的 TrollStore 设备上，做出接近 Android `AndroidMainView` 的效果：主界面不只是一个应用内页面，而是常驻在其他 App 上方的只读悬浮岛。

本阶段包含：

- 新增发行版 `TrollStore`，插件、解释器与裁剪行为沿用 `Sideload`，并追加编译常量 `CLASSISLAND_TROLLSTORE`。
- 主 App 通过私有 API 以 **root HUD 子进程** 的方式重新拉起自身（`--hud` 模式），子进程运行同一套 Avalonia 应用。
- HUD 子进程创建透明全屏 Avalonia 窗口，并把窗口注册到 SpringBoard，使其显示在其他 App 之上。
- 打包后使用 `ldid` 预置 [Entitlements.TrollStore.plist](../ClassIsland.iOS/Entitlements.TrollStore.plist)，再由 TrollStore 安装。

本阶段不包含：

- 交互能力：阶段 1 的悬浮岛是只读的，点击会穿透到下层 App，不支持拖动、点击组件等操作。
- 开机自启与后台常驻：iOS 不允许第三方 App 开机自启，HUD 只能随主 App 启动，并随主 App 退出。
- App Store 上架：`platform-application`、`com.apple.springboard.accessibility-window-hosting` 等私有 entitlements 与私有 API 均无法通过审核。
- 插件组件的完整渲染：HUD 只加载主 App 生成的只读数据快照，不加载插件程序集，因此插件提供的组件在悬浮岛中暂时不显示。

## 发行版差异

`ClassIslandIosDistribution` 从 `Sideload` / `AppStore` 扩展为三个值；`Directory.Build.props` 会为 `Sideload` 与 `TrollStore` 派生 `ClassIslandIosSideloadMode=true`，项目条件统一使用该属性判断“是否保留插件能力”。

| 项目 | `Sideload` | `AppStore` | `TrollStore` |
| --- | --- | --- | --- |
| 外部插件市场、安装与执行 | 开启 | 禁用 | 开启（同 `Sideload`） |
| 托管代码执行方式 | Mono 解释器，逐程序集 `copy` | Release `partial` | Mono 解释器，逐程序集 `copy` |
| Bundle ID（正式版） | `cn.classisland.ios.sideload` | `cn.classisland.ios` | `cn.classisland.ios.sideload` |
| `CodesignEntitlements` | `Entitlements.plist`（空） | `Entitlements.plist`（空） | `Entitlements.TrollStore.plist` |
| 编译常量 | 无 | `CLASSISLAND_APP_STORE` | `CLASSISLAND_TROLLSTORE` |
| 托管构建目录 | `bin/Sideload/`、`obj/Sideload/` | `bin/AppStore/`、`obj/AppStore/` | `bin/TrollStore/`、`obj/TrollStore/` |
| 安装方式 | 重新签名后侧载 | App Store | TrollStore 安装 ldid 签名 IPA |

TrollStore 版与 Sideload 版使用同一个 Bundle ID，因此两者不能同时安装；在已安装 Sideload 版的设备上安装 TrollStore 版会被系统视为升级，应用数据（`Documents/ClassIsland/Data`）会保留。不要只通过修改 Bundle ID 把侧载包当作 TrollStore 包使用，三者的 entitlements 与签名流程不同。

## 架构

```text
主 App（TrollStore 安装，带 TrollStore entitlements）
  ├─ 设置 → 显示悬浮主界面（Settings.IsIosHudOverlayEnabled）
  ├─ IosHudOverlayService
  │    ├─ 生成只读数据快照：Documents/ClassIsland/HudData/
  │    │    ├─ Settings.json、ManagementPreset.json
  │    │    └─ Profiles/、Config/（排除 Cache/ 与 ExternalAudios/）
  │    ├─ ci_hud_start(spawn_as_root=1, data_dir)
  │    │    = posix_spawn(自身可执行文件, ["--hud"],
  │    │       env CLASSISLAND_HUD=1
  │    │           CLASSISLAND_HUD_DATA_DIR=<快照目录>
  │    │           CLASSISLAND_HUD_PARENT_PID=<主 App pid>)
  │    └─ 配置/课表变化时延迟 1 秒重启 HUD；主 App 退出时停止 HUD
  └─ HUD 子进程（root）
       ├─ Main.cs 识别 --hud / CLASSISLAND_HUD，诊断日志写入 HUD 数据目录
       ├─ AppDelegate 创建透明全屏 IosHudHost，共享 App 以 HUD 模式初始化
       ├─ OnAppStarted → IosHudHost.InitializeIsland()：创建悬浮岛并获取 Avalonia UIWindow
       ├─ ci_hud_register_window(UIWindow*)
       │    = SBSAccessibilityWindowHostingController
       │      registerWindowWithContextID:atLevel:（私有 SpringBoard 服务）
       ├─ 每 3 秒检查 getppid()，主 App 退出（被 launchd 收养，ppid==1）后自行退出
       └─ 只读快照 = 不加载插件、不写配置、不初始化通知/实时活动/定位服务
```

数据流是单向的：主 App 负责写入配置与课表，HUD 只读取启动时复制的快照。因此悬浮岛不会和主 App 抢写配置文件；代价是配置变化后需要重启 HUD 子进程才能刷新（由 `IosHudOverlayService` 自动处理）。

关键实现位置：

- `ClassIsland.iOS/Services/Overlay/`：`IosHudOverlayService`（开关、快照、拉起/停止/重启）、`IosHudNativeBridge`（C ABI 调用）、`IosHudMode`（识别 `--hud`）。
- `ClassIsland.iOS.Native/Bridge/`：Swift 侧 `posix_spawn`、root persona、SpringBoard 窗口注册与 HUD 进程管理。
- `ClassIsland.iOS/Controls/IosHudHost` 与 `IosMainWindowIslandView`：HUD 的 Avalonia 宿主与悬浮岛视图（结构参考 Android 的 `AndroidMainView`）。
- `ClassIsland/App.axaml.cs`、`ClassIsland/Program.cs`：共享应用新增 `--hud` 模式与只读数据目录。
- `ClassIsland.iOS/AppDelegate.cs` 中 `#if CLASSISLAND_TROLLSTORE` 分支：只在 TrollStore 发行版注册悬浮主界面服务。

## Entitlements

主可执行文件使用 [`ClassIsland.iOS/Entitlements.TrollStore.plist`](../ClassIsland.iOS/Entitlements.TrollStore.plist)，在打包后由 `tools/ci/sign-ios-trollstore-ipa.sh` 通过 `ldid -S` 写入。该文件参考 Letterpress、TrollSpeed、FloatingBall 等已验证组合：

| Key | 用途 |
| --- | --- |
| `platform-application` | 使 App 以平台二进制身份运行，解除平台签名限制，是 TrollStore 增强能力的核心。 |
| `com.apple.private.security.no-sandbox` | 关闭沙盒，允许访问主 App 数据目录、spawn 子进程等。 |
| `com.apple.private.security.storage.AppDataContainers` | 允许访问 App 数据容器（HUD 读取主 App 生成的快照需要）。 |
| `com.apple.private.security.storage.AppBundles` | 允许访问 App 包体目录（root HUD 子进程从同一 Bundle 重新拉起自身需要）。 |
| `com.apple.private.persona-mgmt` | 允许在 `posix_spawn` 时设置 root persona。 |
| `com.apple.springboard.accessibility-window-hosting` | 允许把窗口注册到 SpringBoard 的全局 accessibility 窗口层（悬浮在其他 App 之上）。 |
| `com.apple.springboard-ui.client`、`com.apple.springboard.appbackgroundstyle`、`com.apple.backboard.client` | 允许与 SpringBoard/BackBoard 交互，控制窗口层级与背景样式。 |
| `com.apple.QuartzCore.displayable-context`、`com.apple.QuartzCore.secure-mode` | 允许创建可用于跨进程窗口托管的 QuartzCore 上下文。 |
| `com.apple.private.memorystatus`、`com.apple.private.kernel.jetsam`、`com.apple.developer.kernel.increased-memory-limit` | 提高 HUD 子进程的内存上限，降低被 jetsam 回收的概率。 |
| `com.apple.security.iokit-user-client-class` | 允许访问 AGX/IOAccel/IOSurface 等 IOKit 用户客户端，root 子进程创建 Metal 渲染上下文需要。 |

设计决定：**只关闭沙盒（`no-sandbox`），保留数据容器**。不要同时声明 `com.apple.private.security.no-container` 或 `com.apple.private.security.container-required=false`：本地测试表明，这类声明会让 App 失去自己的数据容器，`NSHomeDirectory()` 可能变成 `/var/mobile`，`Documents/ClassIsland/Data` 等现有路径全部失效，升级安装后表现为“数据丢失”。

出于 iOS 15 / A12 及以上系统的 AMFI 限制，entitlements 中**不能**包含 `com.apple.private.cs.debugger`、`dynamic-codesigning`、`com.apple.private.skip-library-validation`；包含其中任何一项都会导致 App 无法启动，也不能靠这些 entitlement 在阶段 1 换取 JIT。产物仍使用 Mono 解释器。

这些 entitlement 只在 TrollStore 安装的 IPA 中有效；用 Xcode 正常签名、AltStore/Sideloadly 侧载或 App Store 提交都会失败。

## 构建

TrollStore 版与其它 iOS 发行版使用同一套固定工具链；**不要使用系统安装的 dotnet**（本机为 10.0.401，其 iOS workload 与 Xcode 26.3 不匹配），请使用 `$HOME/.cache/classisland/dotnet-10.0.202`。

```sh
classisland_sdk_dir="$HOME/.cache/classisland/dotnet-10.0.202"
export DOTNET_ROOT="$classisland_sdk_dir"
export PATH="$classisland_sdk_dir:$PATH"

dotnet --version        # 应为 10.0.202
xcodebuild -version     # 应为 Xcode 26.3
```

首次使用该工具链时，按 `doc/specs` 中的记录安装 SDK 与 iOS workload（本地知识，不随仓库提交）：

```sh
classisland_install_script="$(mktemp)"
curl --fail --location --retry 2 https://dot.net/v1/dotnet-install.sh --output "$classisland_install_script"
bash "$classisland_install_script" --version 10.0.202 --os osx \
  --install-dir "$classisland_sdk_dir" --no-path
"$classisland_sdk_dir/dotnet" workload install ios --version 10.0.202
```

通过 NUKE 生成 unsigned IPA（`EnableCodeSigning=false`，签名由后续 ldid 步骤完成）：

```sh
env osName=ios arch=arm64 package=ipa buildType=selfContained buildName=app \
    configuration=Release appVersion=2.2.0 brandType=Production \
    IosDistribution=TrollStore enableCodeSigning=false \
    ./build.sh PublishApp
```

产物为 `out/out_app_ios_arm64_selfContained_ipa_TrollStore.ipa`（未签名）。注意 `PublishApp` 会先清空 `out/` 下的旧产物（`ClassIsland_Dev` 除外），请先备份需要保留的文件。首次完整发布构建耗时可能接近一小时。

日常只做编译验证时，可以直接构建项目（不产出 IPA）：

```sh
dotnet build ClassIsland.iOS/ClassIsland.iOS.csproj -c Debug \
  -p:ClassIslandIosDistribution=TrollStore -p:EnableCodeSigning=false -m:1
```

`-m:1` 用于串行化重复引用的 Avalonia 项目构建，避免多个 MSBuild 节点同时写同一个产物文件；不要改用 `iossimulator-*` RID（SoundFlow 没有 Simulator slice）。

## 签名

IPA 产物未经过 Apple 签名；`tools/ci/sign-ios-trollstore-ipa.sh` 用 `ldid` 预先写入 entitlements，TrollStore 安装时即可直接使用。

安装 ldid（脚本不会自动下载）：

```sh
# macOS：从 ProcursusTeam/ldid releases 下载与主机架构匹配的 ldid_macosx_arm64 / ldid_macosx_x86_64
# 并放到 PATH 中；也可以使用 Homebrew 的 ldid-procursus（若可用）：
brew install ldid-procursus
```

注意不要使用 TrollStore 下载器里的 `ldid` 资产（`releases/latest/download/ldid`）：那是 **iphoneos** 二进制，macOS 无法执行。

对 IPA 签名：

```sh
bash tools/ci/sign-ios-trollstore-ipa.sh \
  out/out_app_ios_arm64_selfContained_ipa_TrollStore.ipa \
  ClassIsland.iOS/Entitlements.TrollStore.plist
```

脚本行为：

1. 解包 IPA，定位 `Payload/` 下唯一的 `.app`。
2. 用传入的 entitlements 为**主可执行文件**执行 `ldid -S<entitlements>`。
3. `PlugIns/*.appex` 的宿主可执行文件使用 appex 自带的 `Entitlements.plist`，没有则使用空 entitlements；appex 缺少宿主二进制时打印警告并跳过。
4. 其余嵌套 Mach-O（`Frameworks/` 下的 framework 与 dylib 等）统一用空 entitlements 签名，避免未签名嵌套二进制在安装后加载失败。
5. 重新打包为 `<原文件名>-TrollStore-signed.ipa`，并生成同名 `.sha256` 文件，同时打印 SHA-256。

示例输出：

```text
signing: ClassIsland.iOS
signing: PlugIns/ClassIslandLiveActivityExtension.appex/ClassIslandLiveActivityExtension
signing: Frameworks/libSkiaSharp.framework/libSkiaSharp
...
signed 7 binaries from ClassIsland.iOS.app (1 appex bundle(s))
signed IPA: out/out_app_ios_arm64_selfContained_ipa_TrollStore-TrollStore-signed.ipa
SHA-256: 34e0adc4...
```

- `ldid` 不在 `PATH` 中时脚本会报错，可用 `LDID=/opt/homebrew/bin/ldid bash tools/ci/sign-ios-trollstore-ipa.sh ...` 指定路径。
- 输入必须是未签名 IPA。如果 IPA 已经带有其它签名，先用 `tools/ci/normalize-ios-ipa.sh` 去除签名再签。
- 脚本只处理主可执行文件、Extension 与嵌套二进制，不修改 IPA 的输入文件，也不会联网。

## 安装与卸载

安装：

1. 把签名后的 IPA 传到设备（AirDrop、文件 App、Finder 文件共享均可）。
2. 在 TrollStore 的 Apps 页点击右上角 `+` 选择 IPA；或在文件 App/分享菜单里选择“用 TrollStore 打开”。
3. 首次启动 ClassIsland 后，进入主界面设置，打开“显示悬浮主界面”。悬浮岛需要几秒才会出现。
4. 之后每次启动主 App 时会自动恢复上次的悬浮主界面开关状态；修改课表/组件/主题后悬浮岛会在约 1 秒内自动重启刷新。

卸载与残留（trash）：

- TrollStore 版可以像普通 App 一样在主屏长按删除，也可以在 TrollStore 的 Apps 页卸载。
- iOS 没有系统级“最近删除/回收站”，卸载后的 App 与数据无法从系统恢复；TrollStore 卸载也不保证清理数据容器，长期反复安装/卸载会在 `/var/mobile/Containers/Data/Application` 留下残留（社区常称 trash/残留）。需要彻底清理时使用 TrollStore 生态的残留清理工具，或在卸载前先导出需要保留的数据。
- ClassIsland 的配置与课表位于 Files App 可见的 `我的 iPhone/iPad > ClassIsland > ClassIsland/Data`，卸载前可先导出。悬浮快照目录 `Documents/ClassIsland/HudData` 可随时手动删除，主 App 下次开启悬浮主界面时会重新生成。
- 覆盖安装（同一个 Bundle ID）不会删除数据；但 TrollStore 版与 Sideload 版共用 Bundle ID，不能同时安装。

## GitHub Actions 构建

`build_release.yml` 的 iOS 矩阵已包含 `Sideload` / `AppStore` / `TrollStore` 三档。TrollStore 档位在常规流程之外额外执行：

1. 与 Sideload 相同的 Release 解释器构建与 unsigned IPA 校验（`tools/ci/verify-ios-ipa.sh` 已支持 `TrollStore`，并使用 `bin/TrollStore/` 的宿主程序集做对比）；
2. 下载固定版本的 [ProcursusTeam/ldid](https://github.com/ProcursusTeam/ldid) **macOS arm64** 资产（`ldid_macosx_arm64`，`LDID_VERSION` / `LDID_SHA256` 固定），校验 SHA-256；`macos-26` runner 是 arm64，但注意不能下载 `releases/latest/download/ldid`（该资产是 iphoneos 二进制，会被 macOS 以 `Killed: 9` 终止）。下载后用 `codesign --force --sign -` 做 ad-hoc 重签再执行；
3. `tools/ci/sign-ios-trollstore-ipa.sh` 用 `ClassIsland.iOS/Entitlements.TrollStore.plist` 对主可执行文件做 ldid 假签名，其余 Mach-O 使用空 entitlements；
4. `tools/ci/verify-ios-trollstore-ipa.sh` 校验签名结果：主二进制必须包含 `platform-application`、`com.apple.springboard.accessibility-window-hosting`、`com.apple.private.persona-mgmt` 等必需 entitlements 与 IOKit/Metal 例外，不得包含被 iOS 15/A12+ 封禁的 `com.apple.private.cs.debugger` / `dynamic-codesigning` / `com.apple.private.skip-library-validation`；app bundle 内所有 Mach-O 都必须已签名；
5. 用签名后的 IPA 覆盖 unsigned 产物，保持 artifact 命名不变：`out_app_ios_arm64_selfContained_ipa_TrollStore.ipa`（内容为 ldid signed IPA），随附同名 `.sha256`。

要点：

- 全过程**不需要 Apple 证书、provisioning profile 或 GitHub Secret**；ldid 是假签名，只对 TrollStore 设备有效，普通 iPhone 无法安装该 IPA。
- 矩阵的 `TrollStore` 档位对 PR 也会运行（会占用额外 macOS runner 时间）；如需节省额度，可后续把该档位限制在 `develop/v2/ios`、`ios-v*` tag 与 `workflow_dispatch`。
- `dotnet test` 会以 `-p:ClassIslandIosDistribution=TrollStore` 运行插件与课程回归测试；插件发行版测试与 Sideload 共用 `ClassIslandIosSideloadMode` 语义。
- 发布 job 的 artifact 校验循环已包含 `TrollStore`，下载到的 artifact 即为可直接安装的签名 IPA。

## 能力边界与风险

- **无开机自启**：iOS 不允许第三方 App 开机自启；设备重启后必须手动打开一次 ClassIsland，悬浮岛才会出现。也没有 LaunchDaemon 之类的常驻手段。
- **HUD 随父进程退出**：主 App 退出后，HUD 子进程的 watchdog（`getppid()==1`）会在约 3 秒内退出；主 App 主动关闭开关或正常退出时也会直接停止 HUD。主 App 被系统挂起时 HUD 进程仍可独立存活，但主 App 被卸载/杀死后的行为以 watchdog 为准。
- **系统版本上限**：TrollStore 依赖 CoreTrust 漏洞，仅支持 iOS ≤ 17.0 的设备（iOS 17.0.1 起漏洞已修复）；具体支持机型与版本以 TrollStore 官方说明为准。
- **私有 API 风险**：悬浮窗口依赖 SpringBoard 私有类与私有 entitlements，系统更新、不同机型或不同 TrollStore 版本都可能失效；一旦失效，HUD 拉起/注册会返回错误并写入诊断日志，主 App 其它功能应保持可用。
- **插件组件暂不显示**：HUD 只读取启动时生成的只读快照（`Settings.json`、`ManagementPreset.json`、`Profiles/`、`Config/`），不包含插件程序集与插件数据，因此插件提供的组件在悬浮岛中不渲染。主 App 内的插件能力与 `Sideload` 版一致。
- **root 子进程尚未真机验证**：`posix_spawn` + root persona、SpringBoard 窗口注册、Metal 上下文创建依赖具体系统行为，阶段 1 需要在真机上逐项验证（见下节）。
- **实时活动/通知可能受影响**：TrollStore 版 Live Activity Extension 在签名脚本中使用空 entitlements，系统可能不识别该扩展，实时活动可能不可用；本地通知等其它能力需要在真机上确认。
- **性能**：HUD 是常驻的全屏透明窗口，持续渲染会产生额外耗电；在低电量模式下可能被系统回收，需要重新开关一次悬浮主界面。

## 真机验证清单

阶段 1 合并前/发布前至少完成以下验证（当前分支尚未完成真机验证）：

- [ ] 在支持 TrollStore 的设备上安装签名 IPA，能正常启动，主界面与 Sideload 版一致。
- [ ] 设置页出现“显示悬浮主界面”开关；Sideload / AppStore 构建中该开关不显示。
- [ ] 打开开关后悬浮岛出现在主屏幕以及任意前台 App 之上，层级正确，不会随主 App 切到后台而消失。
- [ ] 悬浮岛只读：点击悬浮区域会穿透到下层 App，不会拦截触摸。
- [ ] 悬浮岛的位置/缩放/透明度/停靠方向与设置一致，刘海/安全区处理正常。
- [ ] 修改课表、组件或主题后，约 1–2 秒内悬浮岛重启并展示新内容。
- [ ] 关闭开关后悬浮岛消失且 HUD 进程退出；从多任务界面结束主 App 后 HUD 自动退出（无孤儿进程）。
- [ ] 设备重启后不会自动出现悬浮岛，手动打开主 App 后恢复。
- [ ] 含插件组件的课表下悬浮岛不崩溃（预期插件组件不显示）。
- [ ] 在 iOS 15.x、16.x、17.0 各至少一台设备上重复上述检查。
- [ ] 实时活动与本地通知在 TrollStore 版中的表现，确认是否退化。
- [ ] 卸载/覆盖安装后数据与残留行为符合预期。

## 故障排查

- 主 App 启动诊断日志：`Documents/ClassIsland/Diagnostics/startup-latest.log`（上一次启动为 `startup-previous.log`），可通过 Files App 访问。
- HUD 子进程诊断日志：主 App 日志中“悬浮主界面数据快照”所记录的快照目录下的 `ClassIsland/Diagnostics/startup-latest.log`（`CLASSISLAND_HUD_DATA_DIR` 指向 `Documents/ClassIsland/HudData/Snapshot-<guid>`）。
- 打开开关后没有悬浮岛：先看主 App 日志中“无法启动悬浮主界面宿主”的 `result` 与 `ci_hud_last_error` 文本，再确认安装包是否确实经过 ldid 签名、entitlements 是否包含 `platform-application` 与 `com.apple.springboard.accessibility-window-hosting`。
- 安装后无法启动：检查 IPA 是否用 `Entitlements.TrollStore.plist` 签名、是否误加了被 iOS 15/A12+ 封禁的调试 entitlements。
- 升级后数据“丢失”：确认没有声明 `no-container` / `container-required=false`；只保留 `no-sandbox` 才能保住数据容器。
- 签名脚本报 ldid 缺失：从 ProcursusTeam/ldid releases 下载对应的 `ldid_macosx_<arch>`（或 `brew install ldid-procursus`），再用 `LDID=<path>` 指定；不要使用 iphoneos 版 `ldid`。

## 本地实现验证记录（2026-10-25，阶段 1）

以下为本次实现的本地验证结果；真机验证仍未进行（清单见上节）。

- 构建工具链：固定 .NET SDK/workload set **10.0.202**（iOS SDK 26.2.10233）+ **Xcode 26.3**，均附加 `-p:EnableCodeSigning=false -m:1`。
  - `ClassIslandIosDistribution=TrollStore` Debug：**0 错误**，产物 `ClassIsland.iOS/bin/TrollStore/Debug/net10.0-ios/ios-arm64/ClassIsland.iOS.app`。
  - `Sideload`、`AppStore` Debug：同样 **0 错误**，确认发行版条件拆分无回归。
  - TrollStore 产物 `Entitlements.xcent` 包含 `Entitlements.TrollStore.plist` 的全部键；app 内 `ClassIslandLiveActivityBridge.framework` 导出 6 个 `ci_hud_*` 符号；`ClassIsland.iOS.dll` 含 HUD 类型。
  - 预期警告：`MT7139`（未提供 provisioning profile 时 `increased-memory-limit` 的告警；TrollStore 版由 ldid 在安装前写入 entitlements）、`CA1422`（`UIApplication.Windows` 自 iOS 15 起弃用，HUD 窗口只能从应用级窗口列表获取）。二者不影响 ldid 签名后的安装。
- 签名脚本：用本机构建出的 100 MB TrollStore `.app` 打包 IPA，配合**伪造 ldid** 做端到端验证：7 个 Mach-O 被签名（主可执行使用 `Entitlements.TrollStore.plist`，Extension 与 5 个 Framework 使用空 entitlements），输出 `*-TrollStore-signed.ipa` 与 SHA-256；无参数/缺 ldid/缺 IPA/非法 plist 的错误路径均给出明确提示。
- 真实 ldid 验证：本机（x86_64 macOS）使用 ProcursusTeam/ldid 的 `ldid_macosx_x86_64` 对同一 100 MB IPA 完成真实签名，`verify-ios-trollstore-ipa.sh` 校验通过（14 个必需 entitlements 存在、封禁项不存在、7 个 Mach-O 均已签名）。macOS arm64 版 `ldid_macosx_arm64` 将在 CI 中使用。
- `dotnet build build/_build.csproj`（仅编译 NUKE 工程）通过；`tools/ci/verify-ios-build-config.ps1` 已在本机用 PowerShell 7.4.6 运行，`Sideload`/`AppStore`/`TrollStore` 三种模式全部通过（含新增的 CI 断言）。
- CI 变更静态验证：`actionlint` 解析 `.github/workflows/build_release.yml` 无新增问题（仅仓库既有的旧 action 版本提示与既有 `trim_assets` 表达式提示）；`tools/ci/verify-ios-trollstore-ipa.sh` 用伪造 ldid 对真实 100 MB signed IPA 做正/负向测试通过（错误 bundle id 能正确失败）。
- 首次 GitHub CI 运行（run 40175498518）在 `Sign and verify TrollStore IPA` 步骤失败：下载的 `releases/latest/download/ldid` 是 **iphoneos** arm64 二进制，在 macOS arm64 runner 上被系统以 `Killed: 9` 终止。已改为固定 `ProcursusTeam/ldid` 的 `ldid_macosx_arm64`、校验 SHA-256 后用 `codesign --force --sign -` 重签再执行。
- 修复后的 CI 运行（run 40176193178）中 `build_ios (arm64, TrollStore)` 全部步骤成功；下载的 artifact `out_app_ios_arm64_selfContained_ipa_TrollStore.ipa`（74.9 MB，SHA-256 `75796442…`）已在本机用真实 ldid 复核：14 个必需 entitlements 存在、封禁项不存在、bundle id 为 `cn.classisland.ios.sideload`、所有 Mach-O 已签名，包内包含 HUD 符号与托管类型。
- 未验证：所有真机运行项（HUD 子进程的 `UIApplicationMain`/Avalonia 窗口引导、root persona、SpringBoard 窗口注册、`object_setClass` 生效时机、透明渲染、锁屏、触摸穿透、停止/退出、无沙盒下的数据容器路径、ldid 签名后 entitlements 是否被 TrollStore 保留）。

## 参考

- [TrollStore（opa334/TrollStore）](https://github.com/opa334/TrollStore)
- [ldid（ProcursusTeam/ldid）](https://github.com/ProcursusTeam/ldid)
- 侧载版/App Store 版与 iOS 通用构建说明：[doc/iOS.md](iOS.md)
