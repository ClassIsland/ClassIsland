# 新增功能

1.5-Griseo 的新增功能。


## 插件系统

![1722823221695](https://github.com/user-attachments/assets/7464a90d-ef66-47e1-8083-3edbdea603aa)

您可以通过插件扩展 ClassIsland 的功能，比如获取新的组件、提醒提供方等等。您可以在[插件市场](classisland://app/settings/classisland.plugins)浏览插件。如果您对开发 ClassIsland 插件感兴趣，不妨看看[开发文档](https://docs.classisland.tech/dev)来了解如何开发插件。

## 规则集

![1723953554739](https://github.com/user-attachments/assets/6491fc26-202d-4d0e-a91a-2dbd7d0f4cf4)

目前 ClassIsland 支持通过【规则集】功能，更详细地设定规则，并支持插件注册自定义的规则。

## 附加设置分析

![1723953043655](https://github.com/user-attachments/assets/ae1985e0-1997-4a1d-ba4a-928579eebb26)

![1723953105695](https://github.com/user-attachments/assets/377341c2-9379-413e-a766-cc4da72f7b74)

目前档案编辑器支持对各个节点的附加设置覆盖/继承情况进行分析，并支持查看某一课表中各个时间点的附加设置启用情况。

## 课表组件

目前课表相关控件已经被重构，大幅提升了应用和【换课】界面的加载速度。要体验重构后的课表组件，请向主界面添加“课程表”（原“课程表（实验）”）组件。

原来在【设置】->【基本】中的课表设置将被迁移到“课程表”组件下的设置中，您也可以在“课程表”组件设置的最下方通过“导入旧版设置”按钮手动迁移。原来的课表组件（“课程表（旧）”）将计划在 1.5 正式发布时彻底移除，届时还没有迁移到新课程表组件上的用户将自动迁移。

此外，重构后的课表控件支持在课表已加载时调整对应的时间表。此功能需要在【应用设置】->【更多选项…（右上角三个点）】->【实验性功能】中手动启用

## 精简模式

ClassIsland 目前推出了【精简模式】，精简模式的 ClassIsland 裁剪了不必要的资源文件，大幅减小了应用体积。详细请见[文档](https://docs.classisland.tech/app/setup/)。

## 课表群

您可以通过课表群对课表分组，同时可以划分不同启用条件的课表。ClassIsland 只会自动加载启用的课表群和全局课表群中的课表。您可以在 [【档案编辑】](classisland://app/profile) 中编辑课表群。

此外，您可以临时启用某个课表群。临时启用的课表群的失效时间默认是完整轮换完课表群中课表的时间，您可以手动更改失效时间。

> 已知问题：在调整课表所属的课表群后，课表列表界面不能及时反映更改，需要手动进行刷新。

[(#136)](https://github.com/ClassIsland/ClassIsland/issues/136)

![1721193302305](https://github.com/user-attachments/assets/400e3aa8-4a97-456a-9a1e-e45496eefca4)

## Url 导航

目前应用可支持通过 Url 协议（ `classisland://` ）从外部调用应用的功能。要启用此功能，请在[【应用设置】->【基本】](classisland://app/settings/general)中启用“注册 Url 协议”

您可以在【运行】中运行 [classisland://app/test](classisland://app/test) 来验证 Url 协议是否注册成功。

## 组件

![1718443026319](https://github.com/user-attachments/assets/a7b1a888-33a5-4163-9ae9-b23726c9eca0)

新增的【组件】功能目前取代了原有的【快速信息】功能。您可以在主界面上任意排列、添加或删除组件。

如果您从旧版本升级，您的【快速信息】设置会被迁移到【组件】上。

## 设置窗口更新

![1718443093077](https://github.com/user-attachments/assets/9cdf6e9a-0c47-42d6-b76b-a4789f2136e1)

应用设置窗口目前经过了重构，并且加入了导航动画。此外，在重构设置界面之后，应用的启动速度也得到了一定的提升。


***

# 1.5.0.2

1.5 - Griseo

本版本包含了对 1.5 的一些 Bug 修复和改进。

## 新增功能和优化

- 【组件/倒计时】修改倒计时数字与汉字之间的 margin
- 【应用设置】更新功能投票链接
- 【诊断】日志记录到文件
- 【API/提醒】通过提醒请求取消提醒

## 破坏性更改

- 【API/提醒】优化提醒请求封装，将`NotificationRequest`的`CancellationTokenSource`和`CompletedTokenSource`属性的可见性修改为`internal`，并公开暴露其对应的`Token`和取消事件。

## Bug 修复

- 【组件】修复部分组件无法局部覆盖 MainWindowBodyFontSize 的问题 ([#343](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【组件/课表】修复时间点附加信息【XX/持续时间】中时间格式不统一的问题
- 【主界面】修复当窗口宽度为 0 时回弹动画产生负值宽度导致崩溃的问题 ([#386](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【主界面】修复概率丢失置顶属性的问题 ([#358](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【档案编辑器】修复在课表编辑界面选中含有正在删除科目的课程时，使 SubjectId 为 null 导致主循环异常的问题 ([#376](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【档案编辑器】修复设置课表的同时设置科目，导致软件崩溃的问题 ([#375](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【档案】修复在时间表顶部变更一个“课间休息”为“上课”时，整个课程表会向前错位的问题 ([#387](https://github.com/ClassIsland/ClassIsland/issues/3))
- 【组件/课表】修复在删除临时层课表的源课表后应用崩溃的问题
- 【提醒】修复换课时如果下节课是户外课程换成室内课程倒计时显示时间不正常的问题 ([#385](https://github.com/ClassIsland/ClassIsland/issues/3))

***

# 1.5.0.1

1.5 - Griseo

本版本包含了对 1.5 的一些重要 Bug 的修复，请尽快更新。

## 新增功能与优化

- 【组件/课表】支持修改课程表文字间距
- 【组件/课表】时间点附加信息「剩余时间」支持在结束时显示精确倒计时
- 【组件/课表】优化「时间点结束倒计时」显示样式
- 【档案编辑】手动刷新时间表
- 【表格导入向导】从表格导入向导无法打开文件对话框添加权限提示  ([#327](https://github.com/ClassIsland/ClassIsland/issues/327))
- 【调试】调试选项添加「时间流速」功能  

## 移除的功能

- 【应用】删除 ClassIsland 移动向导

### Bug 修复

- 【系统】修复触摸冻结问题，并替换无法使用触摸问题的修复实现 ([#326](https://github.com/ClassIsland/ClassIsland/issues/326))
- 【UI/UX】修复文档坏链的问题
- 【UI/UX】修复部分 SettingsControl 中的 Switcher 控件出现绑定错误的问题
- 【应用设置】修复插件管理 ListDetailView 在紧凑视图中无法展开详细的问题
- 【更新】修复在更新下载错误后显示更新下载完成的问题
- 【组件/课表】修复临时层时间表与源时间表不对应导致应用崩溃的错误 ([#328](https://github.com/ClassIsland/ClassIsland/issues/328))
- 【组件】修复倒计日组件不能自定义全局字体大小的问题 ([#335](https://github.com/ClassIsland/ClassIsland/issues/335))
- 【组件】修复在组件加载时不判断是否满足隐藏规则的问题
- 【提醒】修复放学前的课间提醒中，下节课显示为过去课程的问题 ([#322](https://github.com/ClassIsland/ClassIsland/issues/322))


## 新增功能与优化

# 1.5.0.0

1.5 - Griseo

## 新增功能与优化

- **【组件】组件功能：** 您可以在主界面上任意排列、添加或删除组件。
- **【插件】加入插件系统**：支持通过插件扩展 ClassIsland 的功能，比如获取新的组件、提醒提供方等等。
- **【课表服务】多周轮换**：课表支持设置多周轮换，手动设置轮换偏移
- **【组件/课表】重构课表组件：** 目前课表相关控件已经被重构，大幅提升了应用和【换课】界面的加载速度。要体验重构后的课表组件，请向主界面添加“课程表（实验）”组件。
- **【档案编辑】附加设置分析**：目前档案编辑器支持对各个节点的附加设置覆盖/继承情况进行分析，并支持查看某一课表中各个时间点的附加设置启用情况。([#97](https://github.com/ClassIsland/ClassIsland/issues/97))
- **【档案编辑】课表群：** 通过课表群对课表分组，同时可以划分不同启用条件的课表。 [(#136)](https://github.com/ClassIsland/ClassIsland/issues/136)
- **【应用】Url 协议：** 目前应用可支持通过 Url 协议（ `classisland://` ）从外部调用应用的功能。要启用此功能，请在[【应用设置】->【基本】](classisland://app/settings/general)中启用“注册 Url 协议”
- **【规则集】规则集**：目前 ClassIsland 支持通过【规则集】功能，更详细地设定窗口隐藏规则。([#96](https://github.com/ClassIsland/ClassIsland/issues/96)) ([#179](https://github.com/ClassIsland/ClassIsland/issues/179))
- 【课表服务】重构课表服务
- 【IPC】跨进程获取 ClassIsland 应用信息，接收 ClassIsland 事件广播
- 【换课】记忆上次换课/调课模式
- 【换课】换课窗口“要换课的课表”按钮支持点击前往更换课表
- 【换课】支持在欢迎向导添加快捷换课快捷方式
- 【主界面】记忆隐藏主界面状态
- 【主界面】圆角课表 ([#81](https://github.com/ClassIsland/ClassIsland/issues/81))
- 【主界面】优化鼠标移入淡化课表功能 ([#82](https://github.com/ClassIsland/ClassIsland/issues/82))
- 【主界面】透明兼容模式
- 【主界面】主界面字体大小调整 ([#80](https://github.com/ClassIsland/ClassIsland/issues/80)) ([#26](https://github.com/ClassIsland/ClassIsland/issues/26))
- 【组件】时钟组件，支持显示到秒
- 【组件】为天气图标赋予颜色
- 【组件】自定义全局/组件字体大小和颜色 ([#80](https://github.com/ClassIsland/ClassIsland/issues/80))
- 【组件/课表】上课时仅显示当前时间点
- 【组件/课表】新增“倒计时”类型的附加信息 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【组件/课表】将课表附加信息中时间段格式改为时分制 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【档案】允许自定义课间名称 ([#63](https://github.com/ClassIsland/ClassIsland/issues/63)) ([#227](https://github.com/ClassIsland/ClassIsland/pull/227)) (by [@RoboMico](https://github.com/RoboMico))
- 【档案编辑】将默认时间点时长挪到档案编辑
- 【档案编辑】优化附加设置外观
- 【档案编辑】简化课表编辑方式
- 【档案编辑】自动填写科目简称
- 【档案编辑】粘贴批量导入科目
- 【档案编辑】优化时间线视图编辑体验
- 【档案编辑】优化课表列表界面，在课表列表界面显示触发规则及启用状态
- 【档案编辑】允许在课表启用时编辑对应的时间表 _（需要在【应用设置】->【更多选项…（右上角三个点）】->【实验性功能】中手动启用）_
- 【提醒】课间时长以自然语言显示
- 【提醒】课程提醒现在只在时间点开始时提醒
- 【提醒】提醒音效音量调节 ([#89](https://github.com/ClassIsland/ClassIsland/issues/89))
- 【提醒】自定义上下课提醒遮罩文字 ([#69](https://github.com/ClassIsland/ClassIsland/issues/69))
- 【提醒】显示提醒时，主界面也以半透明显示
- 【提醒】自定义天气预警显示速度 [#173](https://github.com/ClassIsland/ClassIsland/issues/173)
- 【提醒】极端天气提醒文字从预警信号图标背后消失
- 【应用设置】重构设置界面
- 【应用设置】优化关于界面
- 【应用设置】优化提醒音效音量调节边距大小
- 【应用】缩小应用体积
- 【应用】启动时检测并提醒用户启用 Aero 效果
- 【应用】配置文件备份，更新时自动备份
- 【应用】检测程序目录是否为桌面，并提示移动
- 【应用】新增托盘图标点击操作“打开换课界面 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【倒计时组件】使`倒计时`组件的`倒计时名称`设置对绑定源即时更新 [(#213)](https://github.com/ClassIsland/ClassIsland/pull/213) (by [@LiuYan-xwx](https://github.com/LiuYan-xwx))
- 【调试】调整【调试】选项卡，为【调试】标签页添加集控选项
- 【UI】优化人机交互
- 【UI】使用本地化日期选择控件
- 【UI】从屏幕上提取主题色
- 【UI】仅在可用时显示托盘菜单项目
- 【UI】应用设置小选项调整
- 【UI】日志现在只在监视时自动滚动到最下
- 【UI】优化调试页面外观
- 【UI】崩溃窗口文本自动换行
- 【UI/UX】优化进度条加载动画
- 【欢迎向导】欢迎向导完成页面加入快速链接
- 【API/提醒】优化提醒提供方 API

## 移除的功能

- 【应用帮助】删除应用内置的帮助文档，现改为提供[在线文档](https://docs.classisland.tech)。
- 【调试】移除了在 Release 配置的构建中的调试终端。要使用调试终端，请切换到 Debug 配置进行构建。

## Bug 修复

- 【主界面】修复启动加载时主界面显示 System.Object 的问题
- 【档案编辑】修复退出课表编辑时崩溃的问题
- 【档案编辑】修复集控禁用科目编辑后，仍能通过粘贴添加科目的漏洞
- 【档案编辑】删除日期选择器中的失效日期逻辑
- 【档案编辑】修复在课表编辑界面中科目下拉框科目名变为 GUID 的问题
- 【更新】修复部分更新节点无法测速的问题
- 【更新】修复更新完成后无法删除更新临时文件的问题 ([#269](https://github.com/ClassIsland/ClassIsland/issues/269))
- 【提醒】修复提醒队列顺序混乱的问题 ([#250](https://github.com/ClassIsland/ClassIsland/issues/250))
- 【提醒】修复第一节课没有准备上课提醒的问题
- 【提醒】修复在准备上课提醒结束时显示下节课的问题
- 【提醒】修复提醒强调特效会导致托盘菜单关闭的问题
- 【应用】修复文件占用判断不支持中文的问题
- 【应用】修复迁移设置后在重启时覆盖默认档案的问题 ([#251](https://github.com/ClassIsland/ClassIsland/issues/251))
- 【应用】修复不能正常判断是否处于放学状态的问题
- 【应用】修复在有时间偏移时，同步时间时对当前时间判断错误的问题
- 【应用】修复在显示主窗口前通过 `CommonDialog` 显示对话框导致应用退出的问题
- 【应用】修复在调整显示器缩放时报错的问题 [#217](https://github.com/ClassIsland/ClassIsland/issues/217)
- 【应用】修复内存溢出后无法正常重启的问题 [#230](https://github.com/ClassIsland/ClassIsland/issues/230)
- 【应用】修复在保存配置文件时配置文件会意外丢失的问题
- 【应用】修复频繁触发配置文件保存的问题
- 【应用】修复左键点击托盘图标显示主菜单有延迟的问题 [(#199)](https://github.com/ClassIsland/ClassIsland/issues/199)
- 【主题色提取】修复在选择的取色色盘索引为 -1 时无法提取主题色的问题
- 【调试】修复调试时间中网络时间相关问题
- 【UI】修复自定义启动加载界面文本输入框不等长的细节
- 【UI】修复在点击正常的 `Hyperlink` 时也会触发应用内导航的问题 [#216](https://github.com/ClassIsland/ClassIsland/issues/216)
- 【UI】加回应用窗口设置中的任务栏实时时间
- 【UI】修复加载指示条不随主题色变化的问题
- 【UI】修复应用设置窗口最大化时不取消折叠的问题
- 【组件/课表】修复在永久换课时课表不更新课程信息的问题 ([#265](https://github.com/ClassIsland/ClassIsland/issues/265))
- 【组件/课表】修复在课间休息时无法显示时间点持续时间的问题
- 【组件/课表】修复一些字体导致课表组件出现滚动条的问题 [#196](https://github.com/ClassIsland/ClassIsland/issues/196) 
- 【组件/课表】修复课程表组件小时时间部分向上取整导致显示错误的问题 [(#209)](https://github.com/ClassIsland/ClassIsland/issues/209)
- 【时间同步】修复在有时间偏移时同步时间会导致时间错误的问题
- 【系统】修复释放 Mutex 时报错的问题

> [!note]
> 如果您正在从 1.4 升级到此版本，您的【快速信息】设置会被自动迁移到【组件】上。
