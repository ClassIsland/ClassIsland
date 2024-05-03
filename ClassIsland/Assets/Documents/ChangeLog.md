# 新增功能

1.3.1.0 ~ 1.3.4.0 (1.4-Firefly Beta 1~4) 的新增功能

> **您当前正在使用的是 1.4-Firefly 的测试版本，可能包含未完善和不稳定的功能。**

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

[了解更多…](https://github.com/HelloWRC/ClassIsland/wiki/%E9%9B%86%E6%8E%A7)

[#43](https://github.com/HelloWRC/ClassIsland/issues/43)

![1711241863976](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1711241863976.png)

![1711241942861](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1711241942861.png)



***


# 1.3.4.0

> 1.4 - Firefly 测试版，可能包含未完善和不稳定的功能。

🎉 本版本是 1.4 的最后一个 Beta 版，将在确认稳定并修复在这个版本收到的问题后，在下一个版本发布 1.4 的正式版。

## ⚠️破坏性更改

- 【提醒】提醒设置已被重构，提醒语音、效果、音效等设置需要重新启用。

## 新增功能
- 【提醒】自定义提醒特效渲染精度，并根据设备自动设置精度 [#88](https://github.com/HelloWRC/ClassIsland/issues/88)
- 【提醒】调节提醒优先级 [#42](https://github.com/HelloWRC/ClassIsland/issues/42)
- 【提醒】在提醒发出时强制置顶窗口 [#24](https://github.com/HelloWRC/ClassIsland/issues/24)
- 【诊断】导出诊断数据
- 【诊断】日志监视功能
- 【集控】添加对集控服务器的支持
- 【集控】添加对url模板的支持
- 【集控】添加退出集控功能
- 【集控】支持从集控服务器接受命令（发送提醒，重启应用）
- 【应用设置】自动拉取贡献者名单 [#102](https://github.com/HelloWRC/ClassIsland/issues/102)

## 优化
- 【集控】异步拉取集控信息
- 【网络】添加网络请求自动重试机制
- 【精确时间】在系统时间变动时重新同步时间 [#100](https://github.com/HelloWRC/ClassIsland/issues/100)

## Bug修复
- 【提醒】修复在没有时间点时会意外发出即将上课提醒的问题
- 【提醒】修复在即将上课提醒时清除所有提醒后不提醒上课的问题 [#106](https://github.com/HelloWRC/ClassIsland/issues/106)
- 【提醒】修复提醒特效窗口会被意外关闭的问题 [#103](https://github.com/HelloWRC/ClassIsland/issues/103)
- 【天气】修复在一些情况下城市数据库查询出错的问题
- 【主界面】修复在一些情况下可能导致访问没有连接到 `PresentationSource` 的 `Visual` 的问题
- 【课表编辑】修复课表编辑不能及时同步的问题
- 【课表编辑】修复在分配临时课表后，在课表编辑器中打开其它抽屉界面会丢失临时课表设置的问题


***


# 1.3.3.1

> 1.4 - Firefly 测试版，可能包含未完善和不稳定的功能。

本版本主要修复了 `1.3.3.0` 中的一些问题。

## 新增功能
- 【简略信息】倒计时简略信息功能（[#95](https://github.com/HelloWRC/ClassIsland/pull/95) [#60](https://github.com/HelloWRC/ClassIsland/issues/60)）（by [@Doctor-yoi](https://github.com/Doctor-yoi)）
- 【课表】临时课表持久化 （[#75](https://github.com/HelloWRC/ClassIsland/issues/75)）

## 优化与Bug修复
- 【UI】修复主界面不定期停止渲染的问题（[#92](https://github.com/HelloWRC/ClassIsland/issues/92)）
- 【UI】优化在【帮助】界面下的触屏滚动体验
- 【提醒】修复无法显示上课前提醒语的问题（[#86](https://github.com/HelloWRC/ClassIsland/issues/86)）
- 【提醒】修复EdgeTTS在朗读出错后无法继续朗读的问题（[#84](https://github.com/HelloWRC/ClassIsland/issues/84)）
- 【提醒】修复无法保存提醒设置的问题


***


# 1.3.3.0

> 1.4 - Firefly 测试版，可能包含未完善和不稳定的功能。

## 新增功能

- 【提醒】全屏提醒强调特效 
- 【提醒】提醒强调音效 [#41](https://github.com/HelloWRC/ClassIsland/issues/41)
- 【提醒】按提醒来源分别设置语音、强调特效和音效开关 
- 【提醒】允许禁用准备上课提醒文字 [#64](https://github.com/HelloWRC/ClassIsland/issues/64)
- 【时钟】设置时间偏移值 [#55](https://github.com/HelloWRC/ClassIsland/issues/55)
- 【时钟】按增量自动调整时间偏移值 [#58](https://github.com/HelloWRC/ClassIsland/issues/58)

## 优化与Bug修复
- 【档案编辑】修复在编辑科目时，在编辑状态下添加科目导致科目信息丢失的问题 [#62](https://github.com/HelloWRC/ClassIsland/issues/62)
- 【提醒】修复了即将上课时，当倒计时为0时数字不显示的问题。
- 【UI】优化上下课提醒设置界面。
- 【UI】优化时钟设置界面。

***


# 1.3.2.0

> 1.4 - Firefly 测试版，可能包含未完善和不稳定的功能。

## 新增功能
- 【应用】从NTP服务器获取当前时间
- 【提醒】支持使用EdgeTTS朗读服务

## 优化与Bug修复
- 【提醒】在播放大于等于一小时的时间时不发出语音（[#51](https://github.com/HelloWRC/ClassIsland/issues/51)）



***


# 1.3.1.0

> 1.4 - Firefly 测试版，可能包含未完善和不稳定的功能。

## 新增功能
- 【集控】手动加入集控
- 【集控】拉取与合并档案信息
- 【集控】加载功能限制策略
- 【提醒】朗读提醒内容
- 【帮助文档】加入新增共能内容

## 优化与Bug修复
- 【UI】修复加载动画中版本号被进度条遮挡的问题
