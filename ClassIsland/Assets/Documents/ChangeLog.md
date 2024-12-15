# 新增功能

1.6 - Himeko 的新增功能。

## 容器组件

![图片](https://github.com/user-attachments/assets/1cd29caf-dacf-436e-baa7-00fda187f004)

容器组件是一种特殊的组件类型，可以容纳并展示其它组件。目前 ClassIsland 加入了轮播组件和分组组件等容器组件，您可以利用这些组件更好地自定义主界面上的内容。

## 自动化

![图片](https://github.com/user-attachments/assets/8678ead7-a468-4f53-8677-e3c038d805a7)

您可以通过自动化让 ClassIsland 在一些特定的时间节点执行一些特定的操作，比如切换组件配置，运行程序等。

此功能还在早期开发中，欢迎在[ClassIsland/ClassIsland#119](https://github.com/ClassIsland/ClassIsland/issues/119)讨论您的看法和建议。关于此功能的下一步开发方向，请见[此评论](https://github.com/ClassIsland/ClassIsland/issues/119#issuecomment-2408577363)。

***

# 1.5.2.2

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能。

本版本修复了 1.5.2.1 中一些重要问题，请及时更新。

## 新增功能与优化

- 【提醒/天气预报】将天气预报中分割最高与最低气温间的符号改为“~” ([#470](https://github.com/ClassIsland/ClassIsland/issues/470))
- 【应用设置】为天气组件设置添加Uri跳转,并调整UI风格 (by @LiPolymer)
- 【应用设置】统一部分倒计时日和文本组件设置页面风格 (by @LiPolymer)
- 【天气】搜索城市时使用小米天气 API 获取城市信息 (by @mcAmiya)

## 破坏性更改

- 【API/应用设置】修复 SettingsPageInfo 构造函数存在二义性的问题，将构造函数中的 `hideDefault` 参数移动到 `category` 之前。

## Bug 修复

- 【课程服务】修复在启动时课程服务因没有更新课表，导致获取到错误的信息的问题
- 【API/应用设置】修复带是否默认隐藏参数的 `SettingsPageInfo` 构造函数与旧版的构造函数不兼容的问题
- 【应用设置】去除设置页面导航列表虚拟化，以修复在导航后导航栏自动回到顶端的问题

***

# 1.5.2.1

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能。

> [!note]
> 从这个版本开始，发布的二进制文件将包含 [SignPath Foundation](https://signpath.org/) 的数字签名。如果在使用签名后的版本出现异常现象（如报毒等），请及时反馈。

## 新增功能与优化

- 【应用】优化未捕获错误显示方式
- 【组件】在组件被卸载后自动取消订阅事件 ([#541](https://github.com/ClassIsland/ClassIsland/issues/541))
- 【构建】引入数字签名
- 【UI】升级 MdXaml 版本，优化 Markdown 转换方式
- 【API/应用设置】支持隐藏特定设置页面
- 【API/应用设置】支持设置页面在导航时获取完整 Uri


## Bug 修复

- 【档案】修复删除临时层时间表中的时间点时可能出错的问题
- 【组件/课表】修复课程表组件在初始化时不显示次日课表的问题
- 【组件】修复时钟组件在拉伸对齐时显示不正常的问题
- 【应用设置】修复设置界面侧边导航栏不能虚拟化的问题
- 【自动化】修复重启后不会自动恢复行动的问题

***


# 1.5.2.0

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能。

## 新增功能与优化

- **【组件】容器组件：**容纳并展示其它组件
- 【组件】加入轮播组件和分组组件 ([#310](https://github.com/ClassIsland/ClassIsland/issues/310))
- 【组件】自定义组件宽度和对齐方式 ([#501](https://github.com/ClassIsland/ClassIsland/issues/501))
- 【组件】支持时钟组件显示未偏移的真实时间 ([#464](https://github.com/ClassIsland/ClassIsland/issues/464))
- 【组件】支持时间组件小圆点不跳动 ([#446](https://github.com/ClassIsland/ClassIsland/issues/446))
- 【应用设置】调整外观界面字体预览文本
- 【应用设置】微调自动化、调试时间、时间偏移、课程表文本间距等设置页面
- 【应用】在出现未捕获的错误时显示提示
- 【主界面】移除 MainWindow 中重复的 Settings.json 保存
- 【主界面】反转指针移入淡化 ([#462](https://github.com/ClassIsland/ClassIsland/issues/462))
- 【课程服务】重构课程处理方法
- 【API/课程服务】实现放学事件和时间状态
- 【规则集】应用启动时，重新进行规则判断
- 【规则集】优化空规则判断逻辑、区分最大化和全屏判定
- 【精确时间服务】异步时间同步，避免阻塞应用启动
- 【档案编辑器】标记活动时间表
- 【诊断】详细输出模式
- 【构建】为 Nuget 包启用源代码链接

## Bug 修复

- 【组件/课表】修复时间点附加信息 - 持续时间可能出现小数的问题 ([#430](https://github.com/ClassIsland/ClassIsland/issues/430))
- 【主界面】修复置底失效的问题
- 【主菜单】缓解托盘菜单弹出偏移的问题
- 【档案编辑器】修复时间线视图拖动时间点会自动拉动非相邻时间点的问题
- 【应用设置】修复组件界面拖拽插入位置异常的问题
- 【应用设置/调试】修复特定情况下特性调试窗口打开报错的问题 ([#455](https://github.com/ClassIsland/ClassIsland/issues/455))
- 【IPC】修复同时打开多个 ClassIsland 导致 IPC 广播消息时崩溃的问题 ([#475](https://github.com/ClassIsland/ClassIsland/issues/475))
- 【提醒/语音】修复 EdgeTTS 无法使用的问题 ([#489](https://github.com/ClassIsland/ClassIsland/issues/489))
- 【插件】修复插件无法加载依赖的问题

***


# 1.5.1.0

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能。

## 新增功能与优化

- **【自动化】自动化初步：** 让 ClassIsland 在一些特定的时间节点执行一些特定的操作，比如切换组件配置，运行程序等 ([#119](https://github.com/ClassIsland/ClassIsland/issues/119))
- 【组件/课表】显示次日课表 ([#288](https://github.com/ClassIsland/ClassIsland/issues/288)) ([#290](https://github.com/ClassIsland/ClassIsland/issues/290))
- 【档案/课表】优化标记换课课程
- 【规则集】优化规则集编辑控件边距
- 【API/课程服务】通过`IPublicLessonService.GetClassPlanByDate`获取特定日期的课表

## Bug 修复

- 【UI】修复托盘菜单弹出位置可能偏移的问题
- 【UI】修复天气图标可能与主题色相近的问题
- 【提醒】修复空时间段没有准备上课提醒的问题
- 【应用设置】修复隐藏窗口选项 CheckBox 间距不一的问题
