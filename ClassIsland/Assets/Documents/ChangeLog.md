# 新增功能

1.4-Firefly 的新增功能

## 朗读提醒内容

在发出提醒时，ClassIsland可以大声读出提醒的内容。此功能默认禁用，您可以前往[【设置】->【提醒】->【更多选项】](ci://app/settings/notification)调整相关设置。

## 增强提醒

在发出提醒时，ClassIsland会全屏播放提醒特效，并且可以播放提示音效，增强提醒效果。提醒音效默认禁用，您可以自定义要播放的提示音效。您还可以给每个提醒来源单独设置音效。

您可以在[【设置】->【提醒】->【更多选项】](ci://app/settings/notification)调整相关设置。

![1712379341205](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1712379341205.png)

## 精确时间

ClassIsland现在支持从服务器同步当前的精确时间。也可以在此基础上自定义时间偏移值，用于微调时间。此外，ClassIsland还支持在每天以设定的增量值调整时间偏移值。您可以前往[【设置】->【基本】->【时钟】](ci://app/settings/general)调整相关设置。

![1711863876445](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1711863876445.png)

## 集控

您可以将ClassIsland实例加入到集控中，以统一分发课表、时间表等信息，并控制各实例的行为。

[了解更多…](https://classisland-docs.readthedocs.io/zh-cn/latest/management/)

![1711241863976](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1711241863976.png)

![1711241942861](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1711241942861.png)


***

# 1.4.0.3


1.4 - Firefly

本版本主要包含了对 1.4.0.x 的一些优化和修复。

## 新增功能
- 【提醒】显示任课老师功能开关

## 新增功能

***


# 1.4.0.2

1.4 - Firefly

本版本主要包含了对 1.4.0.0 与 1.4.0.1 的一些优化和修复。

## 新增功能
- 【提醒】在显示提醒时展示下节课任课教师（[#156](https://github.com/ClassIsland/ClassIsland/pull/156)）（by [@clover-yan](https://github.com/clover-yan)）

## Bug修复
- 【应用】修复无法正确判断系统TTS是否可用的问题（[#161](https://github.com/ClassIsland/ClassIsland/discussions/161)）
- 【应用】修复因无法正确获取窗口类名，导致在Windows桌面被识别成全屏窗口误隐藏的问题 （[#158](https://github.com/ClassIsland/ClassIsland/issues/158)）
- 【应用】修复窗口选择界面无法正确加载窗口的问题

***


# 1.4.0.1

1.4 - Firefly

本版本主要包含了对 1.4.0.0 的一些优化和修复。

## 新增功能
- 【倒计时】倒计时功能添加字体颜色与字体大小的设定
- 【更新】更新时添加MD5校验 （[#131](https://github.com/ClassIsland/ClassIsland/issues/131)）
- 【天气】排除气象预警（[#139](https://github.com/ClassIsland/ClassIsland/issues/139)）
- 【问题反馈】从崩溃报告中直接打开issues提交界面
- 【集控】在加入集控后自动清除上一个集控会话的文件

## 优化
- 【天气】优化应用设置中天气页面的天气预警显示布局
- 【更新】优化更新判断逻辑
- 【档案】禁用默认档案中科目不清晰的上下课前提醒附加设置
- 【应用】优化配置文件写入逻辑

## Bug修复
- 【提醒】修复系统 TTS 不可用导致程序无法启动的问题（[#148](https://github.com/ClassIsland/ClassIsland/issues/148)）
- 【档案】修复档案文件因某些因素写入失败后导致档案文件被清空的问题 （[#150](https://github.com/ClassIsland/ClassIsland/issues/150)）
- 【档案】修复在一些情况下临时层课表不清除的问题 （[#135](https://github.com/ClassIsland/ClassIsland/issues/135)）
- 【档案编辑】修复在档案编辑界面中打开其它抽屉后，临时课表失效的问题 （[#154](https://github.com/ClassIsland/ClassIsland/issues/154)）
- 【倒计时】修复考试倒计时在跨天时无法更新剩余天数的问题
- 【主界面】修复在一些情况下可能导致访问没有连接到 `PresentationSource` 的 `Visual` 的问题

***


# 1.4.0.0

1.4 - Firefly

## 新增功能
- **【提醒】增强提醒：** 在发出提醒时，ClassIsland会全屏播放提醒特效，并且可以播放提示音效 *（默认关闭）*，置顶应用窗口，以增强提醒效果。
- **【提醒】朗读：** 在发出提醒时，ClassIsland可以大声读出提醒的内容，支持调用EdgeTTS和系统TTS。 *(默认禁用)*
- **【应用】精确时间：** 支持从NTP服务器同步时间，也支持设置自定义时间偏移和自动累加时间偏移。
- **【集控】集控：** 支持将ClassIsland实例加入到集控中，统一分发课表、时间表等信息。
- 【提醒】支持调整提醒优先级
- 【提醒】允许禁用准备上课提醒文字 （[#64](https://github.com/HelloWRC/ClassIsland/issues/64)）
- 【简略信息】倒计时简略信息功能（[#95](https://github.com/HelloWRC/ClassIsland/pull/95) [#60](https://github.com/HelloWRC/ClassIsland/issues/60)）（by [@Doctor-yoi](https://github.com/Doctor-yoi)）
- 【课表】临时课表持久化 （[#75](https://github.com/HelloWRC/ClassIsland/issues/75)）
- 【诊断】导出诊断数据
- 【诊断】日志监视功能
- 【应用设置】自动拉取贡献者名单 （[#102](https://github.com/HelloWRC/ClassIsland/issues/102)）

## 优化
- 【网络】添加网络请求自动重试机制
- 【UI】优化在【帮助】界面下的触屏滚动体验
- 【UI】优化上下课提醒设置界面。

## Bug修复
- 【提醒】修复在没有时间点时会意外发出即将上课提醒的问题
- 【提醒】修复在即将上课提醒时清除所有提醒后不提醒上课的问题 （[#106](https://github.com/HelloWRC/ClassIsland/issues/106)）
- 【提醒】修复了即将上课时，当倒计时为0时数字不显示的问题。
- 【档案编辑】修复在编辑科目时，在编辑状态下添加科目导致科目信息丢失的问题 （[#62](https://github.com/HelloWRC/ClassIsland/issues/62)）
- 【课表编辑】修复课表编辑不能及时同步的问题
- 【天气】修复在一些情况下城市数据库查询出错的问题
- 【主界面】修复在一些情况下可能导致访问没有连接到 `PresentationSource` 的 `Visual` 的问题
- 【UI】修复主界面不定期停止渲染的问题（[#92](https://github.com/HelloWRC/ClassIsland/issues/92)）
- 【UI】修复加载动画中版本号被进度条遮挡的问题
