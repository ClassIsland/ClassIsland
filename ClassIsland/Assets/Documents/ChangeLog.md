# 新增功能

2.2 - Misha（米沙） 的新增功能。

## 移动端支持

![banner](https://res.classisland.tech/screenshots/changelogs/2.2/1.webp)

2.2 为 ClassIsland 引入了移动端支持。现在，您可以在支持的手机、平板等 Android 设备上运行 ClassIsland，并在手机上查看课程信息了！

在 2.1.2.0 中，我们进一步完善了移动端的课表显示和编辑体验。您可以在“今天”页面以竖直列表查看一天的课程，通过日历切换日期，也可以在“本周”页面查看整周的课程安排。同时也加入了在日程模式下适应移动端的课程编辑体验。

同时也为移动端加入了实验性的悬浮窗主界面显示支持，欢迎大家体验。

## 科目信息扩展

![banner](https://res.classisland.tech/screenshots/changelogs/2.2/2.webp)

现在您可以为每个科目设置主题色、图标和上课地点，让课表中的课程更容易辨认。科目主题色和图标会显示在日程课表中，课表组件和上课、准备上课提醒也支持显示科目图标。

上课地点可以在科目中统一设置，也可以为单节课程单独指定。上下课提醒支持选择显示上课地点、任课教师和持续时间，方便您查看课程信息。

## 日程课表格式（实验性）

![banner](https://res.classisland.tech/screenshots/changelogs/2.2/3.webp)

本版本引入了日程课表格式。您可以为每节课程单独设置开始时间、结束时间和启用规则，按周重复或在指定日期安排课程，让不同时间的课程安排更加灵活。

日程课表支持调课、临时覆盖课程和禁用课程，启用规则还支持排除和附加特定日期。您可以通过日程课表编辑器或移动端课表界面编辑课程。

## 框架升级

本版本将 .NET 升级到了 10.0，Avalonia 升级到了 12.1，一定程度上可提升应用的性能与稳定性。

***

# 2.1.2.0

![banner](https://res.classisland.tech/banners/2.2-Misha-DP1.webp)

> [!caution]
> # 警告！请不要使用此版本
>
> 当前版本为 2.2 的技术预览版本，仅适用于开发者进行插件移植和技术性预览，不要在生产环境使用此版本。请使用[稳定版](https://github.com/ClassIsland/ClassIsland/releases/latest)。本版本的 Android 版本仍有部分功能未适配，欢迎在 Issue 中汇报您使用时遇到的问题。

> [!important]
> 在升级到 2.0 前，建议先阅读[2.0 相关问答（Ⅱ）](https://github.com/ClassIsland/ClassIsland/discussions/1486)和[2.0 相关问答（Ⅰ）](https://github.com/ClassIsland/ClassIsland/discussions/1145)。

2.2 - Misha（米沙） Developer Preview 3

这一段时间累积了一些功能，先发出来吧。由于目前我暂时还没有开发 KhaslanaMarketplace 的动力，所以新插件市场支持和转换到正常 Beta 至少得等到下个预览版了。

## 🚀 新增功能与优化

- **【平台/Android】移动端课表显示改进：** 添加适配手机和平板的课表界面，支持按天和按周查看课程，并在日程模式下直接编辑课表
- **【档案】科目信息扩展：** 为科目添加主题色、图标和上课地点字段，并自动迁移已有科目扩展信息 ([#1112](https://github.com/ClassIsland/ClassIsland/issues/1112)) ([#85](https://github.com/ClassIsland/ClassIsland/issues/85)) ([#339](https://github.com/ClassIsland/ClassIsland/issues/339))
- **【档案】日程课表格式（实验性）：** 引入日程式课表及对应的课表编辑器，支持为每节课程单独设置起止时间和启用规则
- 【应用】升级 Avalonia 到 12.1.4
- 【应用】引入 Blob 资源存储系统
- 【数据迁移】支持从 Class Widgets 2 导入课表文件
- 【诊断】支持获取 Android 设备的制造商和型号 ([#1983](https://github.com/ClassIsland/ClassIsland/pull/1983)) by @diann34
- 【档案】日程课表支持调课、临时覆盖课程和禁用课程
- 【档案】支持为单节课程单独设置上课地点 ([#339](https://github.com/ClassIsland/ClassIsland/issues/339))
- 【档案】时间启用规则支持排除和附加特定日期
- 【主页】显示一天的课表，并支持编辑日程课程
- 【主页】为日程课程编辑器添加手势缩放和震动反馈
- 【主界面】新增 Rectangles 主题
- 【主界面】添加背景材质效果，支持 Mica 材质，并允许为单行和单个组件设置背景效果
- 【主界面】为隐藏和显示添加动画
- 【平台/Android】实现定位服务
- 【平台/Android】添加 Toast 提示支持
- 【平台/Android】优化实时通知外观
- 【平台/Android】支持自定义背景图像
- 【平台/Android】添加渲染模式选择功能
- 【平台/Android】将运行时由 Mono 切换为 CoreCLR
- 【平台/Android】使用系统字体，减小安装包体积，并提升与系统界面的一致性
- 【平台/Android】在调试构建中添加调试浮窗
- 【组件/课表】支持显示科目图标
- 【提醒/上下课】支持选择显示上课地点、任课教师和持续时间
- 【提醒/上下课】在上课提醒和准备上课提醒中显示科目图标 ([#85](https://github.com/ClassIsland/ClassIsland/issues/85))
- 【提醒/行动】支持自定义图标
- 【应用设置/天气】优化页面布局，统一空气质量表述，并调整页面图标 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【应用设置/实时通知】添加设置示意图
- 【UI】为加载较慢的界面元素启用懒加载
- 【UI】为选项卡添加切换动画
- 【UI】优化弹出项样式和动画
- 【UI】支持长按显示工具提示
- 【UI】添加图标选择器和日期多选控件
- 【UI】添加自适应导航视图和竖直版课程控件
- 【UI】支持滑动关闭 Toast 提示
- 【UI】竖直抽屉支持全高度模式、手势展开与关闭，以及手势切换高度
- 【UI】在移动端以抽屉形式显示下拉选择项
- 【UI】允许调整 Skia GPU 缓存大小，以缓解显示大图时的卡顿
- 【UI】添加过度滚动回弹效果和滚动到端点时的反馈
- 【UI/档案编辑器】优化课表导入、导出流程和课程编辑器界面
- 【UI/档案编辑器】在日程课表编辑器中显示科目主题色和图标，并适配窄屏幕
- 【UI/档案编辑器】优化日程模式和经典模式下的编辑行为
- 【UI/档案编辑器】支持在周视图和“编辑课程”页中修改课程启用状态 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【API/课程服务】添加获取指定日期课程安排的 API
- 【API/UI】添加图片图标表达式
- 【API/UI】为 FluentIcon、FluentIconSource、LucideIcon 和 LucideIconSource 添加 Icon 属性 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187

## 🐛 Bug 修复

- 【应用】修复启用 CepCompat 时，应用无法在部分影子栈支持不完整的平台上运行的问题 ([#1979](https://github.com/ClassIsland/ClassIsland/issues/1979))
- 【应用】修复多处流资源未释放的问题
- 【应用】修复获取进程信息时资源未及时释放导致的潜在资源泄漏 ([#1983](https://github.com/ClassIsland/ClassIsland/pull/1983)) by @diann34
- 【构建】修复 Linux 和 macOS 解决方案筛选文件缺少项目及错误包含 Android 项目的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【主界面】修复 Rectangles 主题在显示天气提醒时崩溃的问题
- 【主界面/编辑模式】修复“重置视图”报错的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【主界面/编辑模式】修复组件设置界面显示异常及设置抽屉偶尔无法打开的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【平台/Android】修复拖拽无法正常使用的问题
- 【平台/Android】修复天气预警图标无法显示的问题
- 【平台/Android】修复提醒小图标过大导致系统界面卡死的问题
- 【平台/Android】尝试修复系统默认字体可能选到非中文字体的问题
- 【组件/课表】修复图标缩放滑动条没有工具提示的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【语音/EdgeTTS】修复无法朗读的问题
- 【UI】修复打开弹出窗口时卡顿的问题
- 【UI】修复视图内存泄漏的问题
- 【UI】修正窗口加载时间的统计
- 【UI】修复页面切换时内容错位、选项卡过渡动画卡住及部分页面样式应用异常的问题
- 【UI】修复禁用动画等待后，加载指示器仍意外显示的问题
- 【UI/提醒设置】修复再次打开提醒提供方设置项时报错的问题
- 【UI/档案编辑器】修复新建课表弹窗宽度异常及课表编辑表格意外挤压的问题 ([#1975](https://github.com/ClassIsland/ClassIsland/issues/1975))
- 【UI/档案编辑器】修复日程课程编辑器无法使用触屏操作的问题
- 【UI/档案编辑器】修复未选择有效课程时仍可操作“启用课程”复选框，以及只读课表错误显示复选框的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【UI/档案编辑器】修复周视图滚动到选中课程时列位置偏移的问题 ([#2049](https://github.com/ClassIsland/ClassIsland/pull/2049)) by @lrs2187
- 【UI/教学】修复教学中心内容变成空白的问题

<!-- generated by git-cliff -->

***

# 2.1.1.1

![banner](https://res.classisland.tech/banners/2.2-Misha-DP1.webp)

> [!caution]
> # 警告！请不要使用此版本
>
> 当前版本为 2.2 的早期技术预览版本，仅适用于开发者进行插件移植和技术性预览，不要在生产环境使用此版本。请使用[稳定版 2.0.1.1](https://github.com/ClassIsland/ClassIsland/releases/tag/2.0.1.1)。本版本的 Android 版本仍有大量功能未适配，仅作跨平台可行性验证，欢迎在 Issue 中汇报您使用时遇到的问题。


> [!important]
> 在升级到 2.0 前，建议先阅读[2.0 相关问答（Ⅱ）](https://github.com/ClassIsland/ClassIsland/discussions/1486)和[2.0 相关问答（Ⅰ）](https://github.com/ClassIsland/ClassIsland/discussions/1145)。

2.2 - Misha（米沙） Developer Preview 2

由于个人状态不佳，我在近期休息了一段时间。在休息一段时间后，我终于再次捡起了 ClassIsland 的开发工作。在这个版本中，ClassIsland Misha 平台的基础建设工作已基本完成，将在下个版本开始扩大测试范围。

## 🚀 新增功能与优化

- 【应用设置/组件】组件库按来源分组展示，区分内置与插件提供 (([#1916](https://github.com/ClassIsland/ClassIsland/issues/1916))) by @WindDrift
- 【启动器】为启动器添加调试用的实例选择功能
- 【插件】扩展SupportedOSPlatforms支持平台Android和iOS (Misha) (([#1921](https://github.com/ClassIsland/ClassIsland/issues/1921)))
- 【插件】插件源添加OSPlatform转换器 (([#1926](https://github.com/ClassIsland/ClassIsland/issues/1926))) by @diann34
- 【档案】课表支持在特定日期启用或循环启用
- 【档案】为课程添加启用日期限制
- 【顶层效果窗口】移植提醒特效精度功能
- 【UI】改进 DrawerHost 渲染流畅度及动画
- 【UI】缓解 WindowViewHost 的内存泄露问题
- 【UI】优化部分窗口关闭后的内存占用
- 【UI】为窗口开启前添加加载动画
- 【UI/换课】在换课界面添加指向调课界面的链接 (([#1823](https://github.com/ClassIsland/ClassIsland/issues/1823))) by @diann34
- 【UI/档案编辑器】为档案编辑器元素添加拖拽功能 ([#655](https://github.com/ClassIsland/ClassIsland/issues/655))
- 【API/UI】添加 EnumToIntConverter

## 🐛 Bug 修复

- 【插件】修复更新已禁用的插件后被启用的问题 (([#1900](https://github.com/ClassIsland/ClassIsland/issues/1900))) ([#1886](https://github.com/ClassIsland/ClassIsland/issues/1886)) by @diann34
- 【UI】修复部分 UI 控件的属性类型错误，导致无法显示课程的问题
- 【UI】修复亮色模式下窗口标题栏无法拖动的问题 ([#1934](https://github.com/ClassIsland/ClassIsland/issues/1934))
- 【UI】修复使用系统标题栏选项无效的问题
- 【UI/档案编辑器】修复档案编辑器中的启用规则编辑意外选择空项的问题
- 【UI/档案编辑器】修复课表群组合框选项在切换抽屉后丢失的问题
- 【天气】坐标获取城市失败时降低精度 (([#1913](https://github.com/ClassIsland/ClassIsland/issues/1913)))  ([#1912](https://github.com/ClassIsland/ClassIsland/issues/1912)) by @baiyao105

***

# 2.1.1.0

![banner](https://res.classisland.tech/banners/2.2-Misha-DP1.webp)

> [!caution]
> # 警告！请不要使用此版本
>
> 当前版本为 2.2 的早期技术预览版本，仅适用于开发者进行插件移植和技术性预览，不要在生产环境使用此版本。请使用[稳定版 2.0.1.1](https://github.com/ClassIsland/ClassIsland/releases/tag/2.0.1.1)。本版本的 Android 版本仍有大量功能未适配，仅作跨平台可行性验证，欢迎在 Issue 中汇报您使用时遇到的问题。


> [!important]
> 在升级到 2.0 前，建议先阅读[2.0 相关问答（Ⅱ）](https://github.com/ClassIsland/ClassIsland/discussions/1486)和[2.0 相关问答（Ⅰ）](https://github.com/ClassIsland/ClassIsland/discussions/1145)。

2.2 - Misha（米沙） Developer Preview 1

## 🚀 新增功能与优化

- 【应用】升级到 .NET 10、Avalonia 12 和 FluentAvalonia 3
- 【平台/Android】实现在 Android 平台上运行
- 【UI】引入 MishaViewEngine

<!-- generated by git-cliff -->
