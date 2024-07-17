# 新增功能

1.5-Griseo 的新增功能。

## 课表群

您可以通过课表群对课表分组，同时可以划分不同启用条件的课表。ClassIsland 只会自动加载启用的课表群和全局课表群中的课表。您可以在 [【档案编辑】](classisland://app/profile) 中编辑课表群。

此外，您可以临时启用某个课表群。临时启用的课表群的失效时间默认是完整轮换完课表群中课表的时间，您可以手动更改失效时间。

> 已知问题：在调整课表所属的课表群后，课表列表界面不能及时反映更改，需要手动进行刷新。

[(#136)](https://github.com/ClassIsland/ClassIsland/issues/136)

![1721193302305](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1721193302305.png)

## Url 导航

目前应用可支持通过 Url 协议（ `classisland://` ）从外部调用应用的功能。要启用此功能，请在[【应用设置】->【基本】](classisland://app/settings/general)中启用“注册 Url 协议”

您可以在【运行】中运行 [classisland://app/test](classisland://app/test) 来验证 Url 协议是否注册成功。

## 课表控件重构

目前课表相关控件已经被重构，大幅提升了应用和【换课】界面的加载速度。要体验重构后的课表组件，请向主界面添加“课程表（实验）”组件。

此外，重构后的课表控件支持在课表已加载时调整对应的时间表。此功能需要在【应用设置】->【更多选项…（右上角三个点）】->【实验性功能】中手动启用

## 组件

![1718443026319](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1718443026319.png)

新增的【组件】功能目前取代了原有的【快速信息】功能。您可以在主界面上任意排列、添加或删除组件。

如果您从旧版本升级，您的【快速信息】设置会被迁移到【组件】上。


## 设置窗口更新

![1718443093077](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ChangeLog/1718443093077.png)

应用设置窗口目前经过了重构，并且加入了导航动画。此外，在重构设置界面之后，应用的启动速度也得到了一定的提升。



***

# 1.4.2.0

> 1.5 - Griseo 测试版，可能包含未完善和不稳定的功能。

## 新增功能与优化

- **【档案编辑】课表群：** 通过课表群对课表分组，同时可以划分不同启用条件的课表。 [(#136)](https://github.com/ClassIsland/ClassIsland/issues/136)
- **【课表】重构课表组件：** 目前课表相关控件已经被重构，大幅提升了应用和【换课】界面的加载速度。要体验重构后的课表组件，请向主界面添加“课程表（实验）”组件。
- **【应用】Url 协议：** 目前应用可支持通过 Url 协议（ `classisland://` ）从外部调用应用的功能。要启用此功能，请在[【应用设置】->【基本】](classisland://app/settings/general)中启用“注册 Url 协议”
- 【课表】新增“倒计时”类型的附加信息 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【课表】将课表附加信息中时间段格式改为时分制 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【应用】新增托盘图标点击操作“打开换课界面 [(#191)](https://github.com/ClassIsland/ClassIsland/pull/191) (by [@DryIce-cc](https://github.com/DryIce-cc))
- 【倒计时组件】使`倒计时`组件的`倒计时名称`设置对绑定源即时更新 [(#213)](https://github.com/ClassIsland/ClassIsland/pull/213) (by [@LiuYan-xwx](https://github.com/LiuYan-xwx))
- 【文本组件】更改`测试组件`为`文本`组件 [(#212)](https://github.com/ClassIsland/ClassIsland/pull/212) (by [@LiuYan-xwx](https://github.com/LiuYan-xwx))
- 【档案编辑】优化课表列表界面，在课表列表界面显示触发规则及启用状态
- 【档案编辑】档案编辑器延迟初始化
- 【档案编辑】允许在课表启用时编辑对应的时间表 _（需要在【应用设置】->【更多选项…（右上角三个点）】->【实验性功能】中手动启用）_
- 【换课】支持在欢迎向导添加快捷换课快捷方式

## 移除功能

- 【调试】移除了在 Release 配置的构建中的调试终端。要使用调试终端，请切换到 Debug 配置进行构建。

## Bug 修复

- 【应用】修复在保存配置文件时配置文件会意外丢失的问题
- 【应用】修复频繁触发配置文件保存的问题
- 【应用】修复左键点击托盘图标显示主菜单有延迟的问题 [(#199)](https://github.com/ClassIsland/ClassIsland/issues/199)
- 【课表】修复编辑时间表并再次启用课表后，课后休息不会显示在课程表组件中的问题 [(#201)](https://github.com/ClassIsland/ClassIsland/issues/201)
- 【课表】修复课程表组件小时时间部分向上取整导致显示错误的问题 [(#209)](https://github.com/ClassIsland/ClassIsland/issues/209)
- 【档案编辑】修复在课表编辑界面中科目下拉框科目名变为 GUID 的问题

## 已知问题

- 在调整课表所属的课表群后，课表列表界面不能及时反映更改，需要手动进行刷新。

***

# 1.4.1.2

> 1.5 - Griseo 测试版，可能包含未完善和不稳定的功能。

本版本同步了 `master` 分支上的一些重要修复，请尽快更新。

## 新增功能
- 【应用】检测到现有实例运行时允许重启现有实例（[#172](https://github.com/ClassIsland/ClassIsland/issues/172#issuecomment-2174931999)）

## Bug 修复
- 【主界面】修复在不显示提醒时置底窗口闪烁的问题（[#174](https://github.com/ClassIsland/ClassIsland/issues/174)）
- 【主界面】修复更改显示器后，在应用设置中刷新显示器列表或切换显示器，导致应用崩溃的问题（[#175](https://github.com/ClassIsland/ClassIsland/issues/175)）
- 【应用设置】修复在刷新显示器后当显示器布局变化时，ComboBox 选择负值索引的问题
- 【应用】修复在获取窗口类名时读取错误的内存地址导致崩溃的问题（[#169](https://github.com/ClassIsland/ClassIsland/issues/169)）



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
- 【应用】启动时检测并提醒用户启用 Aero 效果

## Bug 修复
- 【应用】修复在`1.4.1.0`中迁移设置时因大小写无法正确识别 guid 键的问题

> 如果您正在从 1.4 升级到此版本，您的【快速信息】设置会被自动迁移到【组件】上。
