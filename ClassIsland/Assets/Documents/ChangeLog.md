# 新增功能

1.5-Griseo 的新增功能。

## 组件

![1718443026319](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1718443026319.png)

新增的【组件】功能目前取代了原有的【快速信息】功能。您可以在主界面上任意排列、添加或删除组件。

如果您从旧版本升级，您的【快速信息】设置会被迁移到【组件】上。


## 设置窗口更新

![1718443093077](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1718443093077.png)

应用设置窗口目前经过了重构，并且加入了导航动画。此外，在重构设置界面之后，应用的启动速度也得到了一定的提升。



# 1.4.1.2

> 1.5 - Griseo 测试版，可能包含未完善和不稳定的功能。

本版本同步了 `master` 分支上的一些重要修复，请尽快更新。

## 新增功能
- 【应用】检测到现有实例运行时允许重启现有实例 （[#172](https://github.com/ClassIsland/ClassIsland/issues/172#issuecomment-2174931999)）

## Bug 修复
- 【主界面】修复在不显示提醒时置底窗口闪烁的问题 （[#174](https://github.com/ClassIsland/ClassIsland/issues/174)）
- 【主界面】修复更改显示器后，在应用设置中刷新显示器列表或切换显示器，导致应用崩溃的问题 （[#175](https://github.com/ClassIsland/ClassIsland/issues/175)）
- 【应用设置】修复在刷新显示器后当显示器布局变化时，ComboBox选择负值索引的问题
- 【应用】修复在获取窗口类名时读取错误的内存地址导致崩溃的问题 （[#169](https://github.com/ClassIsland/ClassIsland/issues/169)）



***

# 1.4.1.1

> 1.5 - Griseo 测试版，可能包含未完善和不稳定的功能。

## 新增功能
- **【组件】组件功能：** 您可以在主界面上任意排列、添加或删除组件。
- 【组件】时钟组件，支持显示到秒
- 【界面】重构设置界面
- 【提醒】显示任课老师功能开关
- 【应用】将服务接口、通用控件和实体移动到 `ClassIsland.Core` 中
- 【应用】重构课表服务
- 【应用】启动时检测并提醒用户启用Aero效果

## Bug 修复
- 【应用】修复在`1.4.1.0`中迁移设置时因大小写无法正确识别guid键的问题

> 如果您正在从 1.4 升级到此版本，您的【快速信息】设置会被自动迁移到【组件】上。
