# 新增功能

1.6 - Himeko 的新增功能。

## 自动化

![Image](https://github.com/user-attachments/assets/bc7fb422-266d-4fc3-90ae-25f64e97de20)

您可以通过自动化让 ClassIsland 在一些特定的时间节点执行一些特定的操作，比如切换组件配置，运行程序等，显示提醒等等。总的来说，就是“当 XX 发生”，并且“满足 XX 规则”时“做什么”。

![Image](https://github.com/user-attachments/assets/32ced745-848d-4e9e-b29e-b917ed693495)

同时，【行动】也作为一种新的时间点类型加入到了时间表中。您可以通过【行动】时间点实现在一天的特定时间执行特定操作。

## 预定调课

![Image](https://github.com/user-attachments/assets/1814f254-00d9-4694-bdea-69dee0a76f14)

![Image](https://github.com/user-attachments/assets/503098e6-27d5-4b74-97c5-505b8dd84e9e)

您可以在[【档案编辑】](classisland://app/profile)的【调课】选项卡中预定调课安排，比如预定在某天临时启用的课表（包括临时层课表），以及跨天换课等等。

## 本地集控

![Image](https://github.com/user-attachments/assets/321b4305-560d-4ce4-bf15-d250f1eca661)

您可以在[【应用设置】->【集控选项】](classisland://app/settings/management)中在不使用集控服务的情况下，为应用设置密码保护和限制策略。您可以通过【限制策略】功能来限制某些功能的使用，也可以通过【密码保护】功能为某些功能设置密码。

## 组件

本版本添加了【容器组件】和【多行组件】功能，增强了组件的自定义能力。

![图片](https://github.com/user-attachments/assets/1cd29caf-dacf-436e-baa7-00fda187f004)

容器组件是一种特殊的组件类型，可以容纳并展示其它组件。目前 ClassIsland 加入了轮播组件和分组组件等容器组件，您可以利用这些组件更好地自定义主界面上的内容。

![Image](https://github.com/user-attachments/assets/2694ffa5-62e3-4812-a602-5c92767d2c66)

【多行组件】功能可以让组件显示在不同的行上。当某一行上的组件全部隐藏时，这一行也会随之隐藏。您可以利用此功能在主界面展示更多的信息。

***

# 1.6.0.4

1.6 - Himeko

本版本修复了先前版本的一些问题，请及时更新。

## 🚀 新增功能与优化

- 【组件/轮播组件】轮播组件跳过按规则隐藏的子组件
- 【应用】改进重复启动应用时的提醒对话框
- 【应用】允许通过主菜单重启应用
- 【天气】增补天气代码 301 雨 302 雪
- 【提醒】在准备上课提醒结束后停止轮播计时器、优化字符串拼接方法

### 🐛 Bug 修复

- 【插件】卸载插件时导致其他插件不加载的问题 #860
- 【精确时间服务】修复时间可能停止流动的问题 #868
- 【主界面】修复在特定情况下多行组件中其他行组件无法按规则隐藏后正确恢复的问题 #743
- 【档案编辑器】修复时间线编辑器各种奇奇怪怪的问题，添加吸附模式开关 #829
- 【提醒】行动提醒的遮罩文字以粗体显示
- 【天气】修复天气名称拼写错误 #820
- 【自动化】#843
- 【自动化】修复切换自动化配置文件不能正常使用的问题 #840 #742
- 【档案】修复在设置无持续时长类型的时间点时出现始末时间错误的问题 #825
- 【主题色提取】修复在一些情况下壁纸提取服务获取截图导致崩溃的问题
- 【应用】修复在启动过程中退出导致异常的问题
- 【应用设置】修复插件操作界面复选框未对齐的问题


***

# 1.6.0.3

1.6 - Himeko

本版本修复了先前版本的一些重要问题，请及时更新。

## 🚀 新增功能与优化

- 【插件市场】更换默认插件源
- 【应用设置/插件】在插件列表添加操作按钮
- 【提醒】允许使用主线程呈现提醒特效
- 【自动化】优化恢复模式自动触发逻辑
- 【应用设置】优化关于页面外观
- 【应用】在停止时取消挂钩系统事件，以防止在停止过程中因触发系统事件导致应用异常
- 【档案编辑】优化时间线视图查找前后时间点方法
- 【遥测】更换 Sentry 后端
- 【主界面】降低 ComponentPresenter 向上传递可视状态变化事件的频率

## 🐛 Bug 修复

- 【恢复】修复重启时可能直接进入恢复的问题
- 【提醒】回滚 7f0af28631d47652f0d1ebf94235512061e53ae2 中允许显示正文时长为 0 的提醒的行为，并从提醒行动中修复 #667 的问题
- 【构建】修复 x64 架构的发布识别为 unknown 的问题
- 【应用设置】#798 修复应用设置页面内存泄漏的问题
- 【档案编辑】修复行动时间点可能被遮挡的问题 #795
- 【档案编辑】修复在包含行动时间点的时间表中拖动分割线时间点导致分割线时间点顺序乱跳的问题
- 【更新】修正更新链接


***

# 1.6.0.2

## 🚀 新增功能与优化

- 【主界面】允许主界面位置判定时直接使用屏幕原始尺寸
- 【主界面】为主界面添加平移变化
- 【主界面】在启动时不自动激活主界面
- 【应用设置】启动界面预览 ([#694](https://github.com/ClassIsland/ClassIsland/issues/694))
- 【应用设置/插件】支持拖入 cipx 到窗口安装插件
- 【应用设置/插件】在插件源刷新失败时提示切换插件源
- 【档案编辑】重复添加行动提示
- 【精确时间服务】在系统时间突变时暂停更新时间
- 【构建】ARM64、x86 架构打包支持
- 【应用】重新创建快捷换课图标 ([#695](https://github.com/ClassIsland/ClassIsland/issues/695))
- 【应用】添加安全模式与诊断模式
- 【应用】为教学安全模式添加更多处理方式 ([#768](https://github.com/ClassIsland/ClassIsland/issues/768))
- 【恢复】添加恢复菜单
- 【配置】自动处理配置文件加载异常 ([#736](https://github.com/ClassIsland/ClassIsland/issues/736))
- 【UI】为天气组件界面提示增加圆角
- 【提醒】增加爱莉希雅、钟离、流萤、高考英语听力男声、三月七 TTS 选项
- 【API/插件 SDK】支持在构建时自动生成插件包

## 🐛 Bug 修复

- 【配置】修复天气城市 ID 不会自动迁移的问题 ([#745](https://github.com/ClassIsland/ClassIsland/issues/745))
- 【组件/课表】修复当前课表变化后不更新明天课表显示状态的问题
- 【应用】修复在 x64 以外的架构中出现启动时卡住无法加载的问题
- 【规则集】修复规则集中条件组反转无效的问题
- 【档案编辑】修复时间线视图中分割线和行动时间点无法被拖动的问题 ([#775](https://github.com/ClassIsland/ClassIsland/issues/775))
- 【档案编辑】修复在调课界面可以选择无效课程的问题 ([#779](https://github.com/ClassIsland/ClassIsland/issues/779))
- 【档案编辑】修复调课课表安排 ListBox 出现验证失败红框的问题 ([#778](https://github.com/ClassIsland/ClassIsland/issues/778))

***

# 1.6.0.1

## 🚀 新增功能与优化

- 在组件设置显示组件 GUID ([#710](https://github.com/ClassIsland/ClassIsland/issues/710))
- 撤销使开始界面的`注册Url协议`不默认勾选"的行为

## 🐛 Bug 修复

- 修复在启动时卡死的问题 ([#702](https://github.com/ClassIsland/ClassIsland/issues/702))
- 修复复制档案时档案路径错误的问题 ([#705](https://github.com/ClassIsland/ClassIsland/issues/705))
- 修复 cron 触发器无法工作的问题 ([#715](https://github.com/ClassIsland/ClassIsland/issues/715))
- 修复在调课周视图中显示“第 0 周”的问题 ([#724](https://github.com/ClassIsland/ClassIsland/issues/724))

[查看 1.6.0.0 的更新日志](https://github.com/ClassIsland/ClassIsland/releases/tag/1.6.0.0)

***

# 1.6.0.0

1.6 - Himeko

> [!important]
> 从此版本起，ClassIsland 的开源协议更换为 GPLv3.关于此更变的详细信息，请见讨论[#697](https://github.com/ClassIsland/ClassIsland/discussions/697)。

## 🚀 新增功能与优化

- **【自动化】自动化：** 支持通过自动化功能，在特定时间、特定事件触发时运行自定义操作。
- **【组件】容器组件：**容纳并展示其它组件
- **【组件】多行组件：**支持为主界面设置多行组件 ([#181](https://github.com/ClassIsland/ClassIsland/issues/181))
- **【档案】调课面板：** 支持提前预定换课课表和跨课表换课 [#321](https://github.com/ClassIsland/ClassIsland/issues/321) [#373](https://github.com/ClassIsland/ClassIsland/issues/373) [#617](https://github.com/ClassIsland/ClassIsland/issues/617)
- **【集控】本地集控：**允许在不使用集控服务的情况下在本地设置密码和限制策略
- **【集控】密码保护功能：**允许为某些功能设置访问密码 ([#311](https://github.com/ClassIsland/ClassIsland/issues/311))
- 【档案编辑器】优化档案编辑器行动编辑外观
- 【档案编辑器】分割线、行动时间点拖动功能 [#153](https://github.com/ClassIsland/ClassIsland/issues/153)
- 【档案编辑器】改进时间点添加体验
- 【档案编辑器】标记活动时间表
- 【档案】预定启用临时层课表 [#321](https://github.com/ClassIsland/ClassIsland/issues/321)
- 【档案】支持 CSES 课表格式转换 [#642](https://github.com/ClassIsland/ClassIsland/issues/642)
- 【档案/自动化】添加档案信任机制
- 【组件】加入轮播组件和分组组件 ([#310](https://github.com/ClassIsland/ClassIsland/issues/310))
- 【组件】自定义组件宽度和对齐方式 ([#501](https://github.com/ClassIsland/ClassIsland/issues/501))
- 【组件】支持时钟组件显示未偏移的真实时间 ([#464](https://github.com/ClassIsland/ClassIsland/issues/464))
- 【组件】支持时间组件小圆点不跳动 ([#446](https://github.com/ClassIsland/ClassIsland/issues/446))
- 【组件】在组件被卸载后自动取消订阅事件 ([#541](https://github.com/ClassIsland/ClassIsland/issues/541))
- 【组件/课表】隐藏上过的课程 [#193](https://github.com/ClassIsland/ClassIsland/issues/193) ([#648](https://github.com/ClassIsland/ClassIsland/issues/648) by @itsHenry35)
- 【组件/课表】模糊倒计时 [#313](https://github.com/ClassIsland/ClassIsland/issues/313)
- 【组件/课表】显示次日课表 ([#288](https://github.com/ClassIsland/ClassIsland/issues/288)) ([#290](https://github.com/ClassIsland/ClassIsland/issues/290))
- 【档案/课表】优化标记换课课程
- 【组件/天气简报】气象预警图标增加预警类型表示 [#568](https://github.com/ClassIsland/ClassIsland/issues/568)
- 【组件/天气简报】显示降水提示 [#176](https://github.com/ClassIsland/ClassIsland/issues/176)
- 【提醒】下课提醒文字自定义 [#341](https://github.com/ClassIsland/ClassIsland/issues/341)
- 【提醒】将顶层特效窗口移动到单独的 UI 线程，提升呈现性能 [#666](https://github.com/ClassIsland/ClassIsland/issues/666)
- 【提醒/天气】支持按小时显示天气预报 [#184](https://github.com/ClassIsland/ClassIsland/issues/184)
- 【提醒/天气】将天气预报中分割最高与最低气温间的符号改为“~” ([#470](https://github.com/ClassIsland/ClassIsland/issues/470))
- 【提醒/天气】逐小时天气预报显示绝对时间 [#693](https://github.com/ClassIsland/ClassIsland/issues/693)
- 【提醒/语音】添加 GPT-SoVITS 语音支持 ([#590](https://github.com/ClassIsland/ClassIsland/issues/590))
- 【更新】重构更新系统，支持从内地源下载应用更新 ([#401](https://github.com/ClassIsland/ClassIsland/issues/401))
- 【天气】天气的搜索城市不再使用本地数据库，改为在线获取城市信息
- 【天气】添加对非中国内地城市的支持
- 【天气】获取日出日落数据
- 【天气】搜索城市时使用小米天气 API 获取城市信息 (by @mcAmiya)
- 【主界面】移除 MainWindow 中重复的 Settings.json 保存
- 【主界面】反转指针移入淡化 ([#462](https://github.com/ClassIsland/ClassIsland/issues/462))
- 【规则集】应用启动时，重新进行规则判断
- 【规则集】优化空规则判断逻辑、区分最大化和全屏判定
- 【规则集】添加窗口规则测试工具 [#688](https://github.com/ClassIsland/ClassIsland/issues/688)
- 【规则集】扩充规则集规则 [#457](https://github.com/ClassIsland/ClassIsland/issues/457) [#575](https://github.com/ClassIsland/ClassIsland/issues/575)
- 【规则集】规则集满足状态显示 [#688](https://github.com/ClassIsland/ClassIsland/issues/688)
- 【应用设置】为天气组件设置添加 Uri 跳转，并调整 UI 风格 (by @LiPolymer)
- 【应用设置】统一部分倒计时日和文本组件设置页面风格 (by @LiPolymer)
- 【应用设置】调整外观界面字体预览文本
- 【应用设置】微调自动化、调试时间、时间偏移、课程表文本间距等设置页面
- 【UI】将启动 UI 移动到单独的线程，优化启动过程动画，支持显示启动详细信息
- 【UI】修改崩溃窗口文字
- 【UI】添加开发中画面水印
- 【UI】升级 MdXaml 版本，优化 Markdown 转换方式
- 【日志】日志、插件搜索忽略大小写
- 【调试】不保存调试时间和时间流速对时间偏移的改动
- 【应用】在出现未捕获的错误时显示提示
- 【构建】引入数字签名
- 【精确时间服务】异步时间同步，避免阻塞应用启动
- 【诊断】详细输出模式
- 【API/主界面】引入行高度 IslandContainerHeight 资源值
- 【API/主界面/组件】为 ContentPresenter 引入表示组件为根组件的 IsRootComponent 属性
- 【API/认证】认证提供方注册功能
- 【API/Uri 导航】允许在通过 Uri 导航应用设置页面时保留历史记录
- 【API/通用对话框】添加 CommonDialogBuilder.SetIconKind 方法以按照对话框类型设置图标
- 【API/应用设置】支持隐藏特定设置页面
- 【API/应用设置】支持设置页面在导航时获取完整 Uri
- 【API/课程服务】实现放学事件和时间状态
- 【API/课程服务】通过`IPublicLessonService.GetClassPlanByDate`获取特定日期的课表
- 【API/托盘】添加自定义通知点击回调接口

## ♻ 移除的功能

- 【调试】移除调试中集控策略选项

## 🐛 Bug 修复

- 【档案编辑器】[#684](https://github.com/ClassIsland/ClassIsland/issues/684) 修复使用「调课」功能切换临时课表后无法调课的问题
- 【档案编辑器】[#685](https://github.com/ClassIsland/ClassIsland/issues/685) 修复「调课」功能日历视图不能响应修改课程造成的课程表变动的问题
- 【档案编辑器】修复删除时间点时卡顿的问题
- 【档案编辑器】修复时间线视图拖动时间点会自动拉动非相邻时间点的问题
- 【组件】修复时钟组件在拉伸对齐时显示不正常的问题
- 【组件/课表】修复时间点附加信息 - 持续时间可能出现小数的问题 ([#430](https://github.com/ClassIsland/ClassIsland/issues/430))
- 【组件/课表】[#677](https://github.com/ClassIsland/ClassIsland/issues/677) 修复课程表组件<显示的课程>与<当日课程结束的占位符内容>重叠的问题
- 【档案】修复删除临时层时间表中的时间点时可能出错的问题
- 【主界面】修复置底失效的问题
- 【插件】修复加载损坏的插件配置导致应用无法启动的问题 ([#565](https://github.com/ClassIsland/ClassIsland/issues/565))
- 【课程服务】修复在启动时课程服务因没有更新课表，导致获取到错误的信息的问题
- 【应用设置】修复应用设置导航栏排序可能不正常的问题
- 【应用设置】修复隐藏窗口选项 CheckBox 间距不一的问题
- 【应用设置】修复组件界面拖拽插入位置异常的问题
- 【应用设置/调试】修复特定情况下特性调试窗口打开报错的问题 ([#455](https://github.com/ClassIsland/ClassIsland/issues/455))
- 【构建】修复 Edge_tts_sharp 项目还原问题
- 【主菜单】缓解托盘菜单弹出偏移的问题
- 【IPC】修复同时打开多个 ClassIsland 导致 IPC 广播消息时崩溃的问题 ([#475](https://github.com/ClassIsland/ClassIsland/issues/475))
- 【UI】修复托盘菜单弹出位置可能偏移的问题
- 【UI】修复天气图标可能与主题色相近的问题
- 【提醒】[#667](https://github.com/ClassIsland/ClassIsland/issues/667) 修复提醒的正文显示时间为 0 时无法执行的问题
- 【提醒】修复空时间段没有准备上课提醒的问题
- 【应用更新】修复检查更新后上次检查更新时间不刷新的问题
- 【插件市场】[#654](https://github.com/ClassIsland/ClassIsland/issues/654) 修复官方插件源丢失的问题
- 【API/UI】修复 SolidColorBrushToColorConverter 可能发生空值转换的问题
