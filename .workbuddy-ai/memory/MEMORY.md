# 项目长期记忆 — ClassIsland（temp_repo）

## 环境与构建
- 用户机器上 ClassIsland.Desktop.exe 经常处于运行状态，会锁住
  `ClassIsland.Desktop/bin/Debug/net8.0-windows10.0.19041.0/` 下的 dll；
  此时只能编译 `ClassIsland/ClassIsland.csproj`，或让用户先关闭应用。
- **`dotnet build` 报 `Value cannot be null. (Parameter 'path1')` 的真实原因：不是仓库/网络问题，
  而是 agent shell 的 Windows 环境变量被裁剪**（实测 `SystemRoot`、`ProgramFiles`、`ProgramData`、
  `APPDATA`、`COMSPEC`、`PATHEXT`、`PROCESSOR_ARCHITECTURE`、`OS` 全为空）。
  .NET SDK 与 NuGet 依赖这些变量，因此 `dotnet --info` 会在
  `Microsoft.DotNet.Installer.Windows.InstallerBase` 静态构造里抛 `NullReferenceException`，
  `dotnet nuget list source` 也会报同样的 `path1` 错。
  在任意**全新**项目（仓库外、零 PackageReference）上同样复现，可据此确认与仓库无关。
  **解决办法**：命令前先补齐这些变量再调 dotnet，例如
  `export SystemRoot='C:\Windows' ProgramData='C:\ProgramData' ProgramFiles='C:\Program Files'
   APPDATA='C:\Users\YangTianming\AppData\Roaming' COMSPEC='C:\Windows\system32\cmd.exe'
   PATHEXT='.COM;.EXE;.BAT;.CMD' PROCESSOR_ARCHITECTURE='AMD64' OS='Windows_NT'`
  （注意 `ProgramFiles(x86)` 在 bash 里不是合法标识符，需用 env 命令传或省略）。
  补齐后 `dotnet restore` 可正常工作，无需再依赖 `--no-restore`。
- `GeneratePackage.props` 里 `<GeneratePackageOnBuild>True</GeneratePackageOnBuild>`，
  编译时会走 Pack 目标并报 `GetProjectReferencesFromAssetsFileTask` 失败，
  需加 `-p:GeneratePackageOnBuild=false`。
- **想绕开被锁的 Debug 输出目录，用 `-c Release`，不要覆写 `BaseOutputPath`**：
  - `-p:BaseOutputPath=<相对路径>` 在 Debug 下会踩到 **HotAvalonia 的 Fody 编织失败**
    （`Mono.Cecil.ModuleDefinition.Write` → `CreateFile` 失败）。HotAvalonia 只在 Debug 启用，
    见 `AvaloniaShared.props:14`（`<HotAvalonia Condition="'$(Configuration)' == 'Debug'">True</HotAvalonia>`）。
    （此前把 `NETSDK1060` 归因于 BaseOutputPath 是**错的**，真实原因是环境变量缺失，已纠正。）
  - Release 下 HotAvalonia 自动关闭，且 `bin/Release/` 不被运行中的 Debug 实例锁定，
    可直接跑通**整个应用**的编译验证：
    `dotnet build ClassIsland.Desktop/ClassIsland.Desktop.csproj -c Release --no-restore -p:GeneratePackageOnBuild=false`
- 标准验证命令：
  `dotnet build ClassIsland/ClassIsland.csproj -c Debug --no-restore -p:GeneratePackageOnBuild=false`
- **不要在用户可能正在 VS Code 里编译时跑命令行 `dotnet build`**：两者会同时重写
  `ClassIsland/bin`、`ClassIsland.Desktop/bin` 下的同名输出，撞车表现为
  「文件被占用 / 另一个进程正在使用」。用户报「编译不了」时先确认这一点。
- 用户的 VS Code（`.vscode/tasks.json`）默认生成任务是
  `dotnet build ClassIsland.Desktop/ClassIsland.Desktop.csproj -c Debug`，
  另有 `build` = `dotnet build ClassIsland/ClassIsland.csproj`（F5 的 preLaunchTask）。
  **两者都没带 `-p:GeneratePackageOnBuild=false`，但实测 Pack 目标能正常跑完**，
  并不必然失败。`.vscode/settings.json` 里 `dotnet.defaultSolution = ClassIsland.sln`。
- `dotnet build` 会做增量判定，源文件没变时只报「已成功生成」而**不真正重编**；
  要确认真实重编需先 `touch` 改动的源文件。
- `.workbuddy-ai/` 与自建的调试目录（如 `.codex-debug/`）**都不在 .gitignore 中**，
  会以未跟踪文件出现在 `git status`；调试产物用完必须删除，避免污染仓库。

## Avalonia / FluentAvalonia 主题色坑位
- **`TryFindResource(key, out value)` 这个 2 参数重载等价于按 `ThemeVariant.Default` 查主题字典，
  会永远命中浅色字典**，深色外观下取到浅色值（实测：基色 #00B294、深色外观下
  `AccentTextFillColorPrimaryBrush` 得到 #00664A，而不是深色字典的 #67FFEE）。
  要按真实外观取色，必须用 3 参数重载显式传 `ThemeVariant`：
  `TryFindResource(key, theme, out value)`。
- `Application.ActualThemeVariant` 在「跟随系统」下**实测返回真实外观（Dark/Light），不是 Default**
  （此前记录为 Default 是错的，已纠正）。
  **`AccentColorPicker.GetEffectiveThemeVariant` 的取值顺序（实测推荐）**：
  ① `RequestedThemeVariant` 非 Default → 直接用；
  ② 否则用 `Application.ActualThemeVariant`（实测=真实外观）；
  ③ 再否则回落 `PlatformSettings.GetColorValues().ThemeVariant`。
  注意 `ThemeService.SetTheme` 在 `themeMode=0`（跟随系统）时会**显式把
  `RequestedThemeVariant` 设成 `ThemeVariant.Default`**（`ThemeService.cs:54-60`），所以 ① 必然落空、
  ②/③ 是真正生效的分支。
- **控件侧与 Application 侧一致**：探针实测「Window 内 TextBlock」的
  `ActualThemeVariant` 与 `Application.ActualThemeVariant` 相同（深色外观下均为 Dark），
  且按它查 `AccentTextFillColorPrimaryBrush` 得 #67FFEE —— 与分组标题的 `DynamicResource` 解析结果一致。
  即：用 3 参数重载 + 真实外观查色，就能复刻控件渲染值。
- 实测值表（系统基色 #00B294、OS 深色外观、`PreferUserAccentColor=true`）：
  | 资源 | Light 字典 | Dark 字典 | Default 字典 |
  |---|---|---|---|
  | `SystemAccentColor` | #00B294 | #00B294 | #00B294 |
  | `SystemAccentColorLight2` | #21FFE0 | #21FFE0 | #21FFE0 |
  | `SystemAccentColorDark1` | #009B7E | #009B7E | #009B7E |
  | `AccentTextFillColorPrimaryBrush` | #00664A | #67FFEE | #00664A |
  | `AccentFillColorDefaultBrush` | #009B7E | #21FFE0 | #009B7E |
  - **分组标题用的是 `AccentTextFillColorPrimaryBrush`**（两个窗口的
    `TextBlock.subject-group-header.default-color` 样式）。
  - 注意 `AccentFillColorDefaultBrush` 深色下 = #21FFE0，与标题用的画刷**不是同一个**，
    容易和用户说的「强调色 #21FFE0」混淆。
- Windows 强调色基色可在
  `HKCU\Software\Microsoft\Windows\DWM\AccentColor`（ABGR）与
  `HKCU\...\Explorer\Accent\AccentPalette`（BGRA×8，含浅/深变体）读取。
  用户看到的「强调色」往往是深色外观下的浅色变体，而非基色。

## 科目分组 / 分组标题颜色
- `SubjectGroup.Color` 为空字符串 = 跟随系统强调色（见 `ClassIsland.Shared/Models/Profile/SubjectGroup.cs`）。
- 分组标题默认色由 `ClassIsland/Views/ProfileSettingsWindow.axaml`、
  `ClassIsland/Views/ClassChangingWindow.axaml` 里
  `TextBlock.subject-group-header.default-color` 样式决定。
- 颜色选择器显示值由 `ClassIsland/AccentColorPicker.cs` 提供，必须与标题实际渲染一致。
- 注意：档案编辑左侧「科目分组」列表项文字是灰色，**不渲染分组色**，不能用来对照标题色。
- **分组标题共有 3 个渲染点**：① 分组信息色块（`ColorPicker`）② 换课窗口（ComboBox）
  ③ 课表/档案编辑的「编辑科目」（ListBox，含课表单元格 Popup）。三处必须一致。
- **★ 陷阱：`ListBoxItem:disabled` 的 `Opacity` 默认是 0.5**（FluentAvalonia 主题样式），
  而分组标题项靠 `IsEnabled="{Binding !IsGroupHeader}"` 禁止选中 → 整项降为半透明，
  颜色被背景冲淡（深色外观下 `#67FFEE` 混成 `#47968F`）。`ComboBoxItem:disabled` 的
  `Opacity` 天然是 1，所以 ComboBox 侧没有这个问题。
  修法：在对应 `<ListBox.Styles>` 里加
  `<Style Selector="ListBox:not(:disabled) ListBoxItem:disabled"><Setter Property="Opacity" Value="1" /></Style>`。
  **必须带 `ListBox:not(:disabled)` 限定** —— 周视图的 ListBox 自身有
  `IsEnabled="{Binding !ViewModel.SelectedClassInfo.IsEmpty}"`，不加限定会让「未选中课程」
  时整个列表不再变暗（回归）。
  涉及文件：`ClassIsland/Views/ProfileSettingsWindow.axaml`（列表视图 + 周视图）、
  `ClassIsland/Controls/ScheduleDataGrid/ScheduleDataGridCellControl.axaml`。

## 主题服务与窗口生命周期
- 主题变化最终都汇聚到 `ThemeService.SetTheme`：
  应用内改设置走 `MainWindow.UpdateTheme()`；
  系统改主题/强调色走 `PlatformSettings.ColorValuesChanged`
  → `MainWindow.OnSystemEventsOnUserPreferenceChanged` → `UpdateTheme()`。
  需要在主题变化时刷新就订阅 `IThemeService.ThemeUpdated`（已在 `SetTheme` 内触发）。
- `ProfileSettingsWindow` 注册为 **Singleton**；`ProfileSettingsViewModel` **已改为 Singleton**
  （2026-09-18 修复：它同时被档案窗口和 keyed transient 的规则设置控件解析，Transient 会累积泄漏）。
  订阅档案事件用 `EnsureProfileEventSubscriptions()`（幂等，按 `Profile`/`Subjects`/`SubjectGroups`
  实例比对后重挂），不要写「构造函数里挂一次」的裸订阅。
- 需要等 FluentAvalonia 更新完主题字典再取色时，用 `Dispatcher.UIThread.Post` 延后一个调度周期。

## 死代码
- `ClassIsland/Views/ExcelImportWindow.xaml.cs` 与 `ExcelExportWindow.xaml.cs` **整个文件被 `#if false` 包住**
  （第 1 行 ~ 832 行 `#endif`），类型不在编译产物里，dll 搜不到。不要对它们做审查结论或改动。

## Avalonia 绑定坑位（科目选择器相关）
- **编译绑定下 `$self` 的解析上下文是「目标控件」**，不是 TemplatedParent。
  所以 `Path="$self.(ns:Type.Prop)"` 能过编译（括号 + 显式类型走附加属性注册表），
  但 `Path="$self.SomeNormalProp"` 会报 `AVLN2000: Unable to resolve property ... on type 'ListBox'`。
  要绑 TemplatedParent 的普通属性就**去掉 `$self`**、只留 `RelativeSource={RelativeSource TemplatedParent}`。
- **`Popup` 里 `ListBox` 的 `ItemsSource` 是弹窗打开时才绑定的**（写在 `Popup[IsOpen=True] ListBox` 的
  Style Setter 里）。若此时数据源为空，ListBox 找不到 `SelectedValue` 的匹配项，会把它清成 null，
  并经双向绑定**把数据源的字段一起清掉**。因此课表单元格弹窗必须在
  `IsEditPopupOpen = true` **之前**重建 `SubjectSelectionItems`
  （见 `ScheduleDataGridCellControl.OnDoubleTapped`），不要改成「打开后再刷新」。
- 单元格控件会被 DataGrid 复用、一屏几十个，**不要在单元格里订阅档案字典**（会成倍放大订阅）。
