# 新增功能

2.1 - LiliyaOlenyeva（莉莉娅·阿琳） 的新增功能。

## 表格课表编辑器

现在档案编辑器支持以周表格的形式快速录入和查看课表。此外，在通过点击表头创建课表时，应用会自动填充部分信息，提升课表创建效率。

![](https://res.classisland.tech/screenshots/changelogs/2.1/2.webp)

## 编辑模式

现在 ClassIsland 支持通过编辑模式所见即所得地编辑主界面上的组件，在各个组件行和容器组件间直接拖动组件，并快捷地调整各个设置。

![](https://res.classisland.tech/screenshots/changelogs/2.1/3.webp)

![](https://res.classisland.tech/screenshots/changelogs/2.1/4.webp)

## 设置页面分组

现在设置页面导航栏已按照类别进行分组，以便于用户查找。并且，插件也可以利用此功能将插件中的设置页面归纳到同一分组下。

![](https://res.classisland.tech/screenshots/changelogs/2.1/1.webp)

此外，设置页面导航栏的高亮逻辑也得到了优化。

***

# 2.0.2.0

2.1 - LiliyaOlenyeva（莉莉娅·阿琳）

## 🚀 新增功能与优化

- **【UI/档案编辑器】表格课表编辑视图：** 支持以周表格的形式快速录入和查看课表。([#238](https://github.com/ClassIsland/ClassIsland/issues/238))
- 【UI/档案编辑器】优化时间线视图的时间块外观
- 【UI/档案编辑器】档案编辑器 Uri 导航支持导航到子页面 ([#1039](https://github.com/ClassIsland/ClassIsland/issues/1039))
- 【UI/档案编辑器】课表编辑器集成日历视图
- 【主界面】部分回滚新版主界面布局，仅在编辑模式下采取新版主界面布局模式
- 【主界面】实现主界面主题阴影安全区
- 【主界面】行隐藏功能 (([#1606](https://github.com/ClassIsland/ClassIsland/issues/1606)))
- 【提醒】新版提醒后台工作服务
- 【附加设置/上下课提醒】上下课提醒特殊设置分组，并隐藏当前节点不支持的设置
- 【UI/应用设置/自动化】优化规则集工作指示器外观
- 【UI/应用设置/提醒】优化提醒设置操作逻辑，为提醒高级设置添加快速指示
- 【UI/应用设置/更新】移植更新日志查看功能，添加更新日志懒加载 ([#1637](https://github.com/ClassIsland/ClassIsland/issues/1637))
- 【UI/应用设置/更新】优化更新页面错误显示
- 【天气】允许使用不加密的 HTTP 协议获取天气信息 ([#1569](https://github.com/ClassIsland/ClassIsland/issues/1569)) (([#1570](https://github.com/ClassIsland/ClassIsland/issues/1570)))
- 【开发】增强调试体验 (([#1621](https://github.com/ClassIsland/ClassIsland/issues/1621)))
- 【平台/X11】重新移植兼容缩放模式
- 【示例】移植 ExamplePlugin 到 ClassIsland 2.0
- 【API/附加设置】附加设置附加的节点信息 API

## 🐛 Bug 修复

- 【提醒】孤儿提醒移交，修复提醒在显示的主界面行被卸载后消失的问题 ([#1332](https://github.com/ClassIsland/ClassIsland/issues/1332))
- 【音频】修复在 Linux 下特定情况下因快速重复初始化 AudioPlaybackDevice 导致应用崩溃的问题
- 【主界面】修复提醒进度条导致主界面无法收缩的问题
- 【UI/档案编辑器】修复表格课表编辑器重载后部分元素消失的问题
- 【UI/应用设置/更新】修复更新页面下载中动画偏移的问题
- 【UI/应用设置/组件】修复组件设置底部边距不正确的bug

***

# 2.0.1.1

2.1 - LiliyaOlenyeva（莉莉娅·阿琳）

## 🚀 新增功能与优化

- 【主界面/编辑模式】优化主界面编辑模式画布行为 ([#1592](https://github.com/ClassIsland/ClassIsland/issues/1592))
- 【主界面/编辑模式】添加编辑模式使用教学
- 【主界面/编辑模式】优化主界面在编辑模式下的置顶行为
- 【主界面/编辑模式】优化拖拽预览行为
- 【主界面/编辑模式】在选择的组件没有组件设置时隐藏组件设置页签 ([#1592](https://github.com/ClassIsland/ClassIsland/issues/1592))
- 【插件】实现 MacOS 自定义程序集解析器
- 【UI/应用设置/提醒】在禁用所有主界面行的提醒时在提醒页面显示警告
- 【UI/档案编辑器】优化时间线编辑视图的外观

## 🐛 Bug 修复

- 【主界面/编辑模式】修复编辑模式在主界面隐藏的状态下无法编辑主界面的问题
- 【语音/EdgeTTS】修复 EdgeTTS Websocket 可能会在不适合的时机断开的问题 ([#1567](https://github.com/ClassIsland/ClassIsland/issues/1567))
- 【UI/应用设置/组件】修复组件设置页面按钮无法使用的问题，优化组件设置页面拖拽逻辑 ([#1592](https://github.com/ClassIsland/ClassIsland/issues/1592)) ([#1603](https://github.com/ClassIsland/ClassIsland/issues/1603))
- 【UI/应用设置/组件】修复旧版组件编辑界面的规则集选项消失的问题 ([#1604](https://github.com/ClassIsland/ClassIsland/issues/1604))

***

# 2.0.1.0

2.1 - LiliyaOlenyeva（莉莉娅·阿琳）

## 🚀 新增功能与优化

- **【主界面/编辑模式】编辑模式：** 支持通过编辑模式更直观和便捷地编辑主界面上的组件和调整相关设置 ([#1249](https://github.com/ClassIsland/ClassIsland/issues/1249))
- **【UI/应用设置】导航栏分组：** 实现应用设置页面分组，优化侧边栏高亮逻辑 ([#1587](https://github.com/ClassIsland/ClassIsland/issues/1587)) ([#1249](https://github.com/ClassIsland/ClassIsland/issues/1249))
- 【主界面】优化主界面动画曲线
- 【主界面】允许设置更高的置顶频率 ([#536](https://github.com/ClassIsland/ClassIsland/issues/536))
- 【UI/应用设置】将【基本】设置页面的部分内容拆分到【时钟】与【高级】页面中

## ♻️移除的功能

- 【主界面】弃用主界面点击功能

## 🐛 Bug 修复

- 【主界面】修复主界面行设置中的自定义字体大小默认为 0 的问题
- 【UI/课表控件】修复初始化课表控件时的绑定错误，提升加载速度
- 【UI/应用设置】修复自定义插件镜像源输入框不可见/不好定位的问题 ([#1534](https://github.com/ClassIsland/ClassIsland/issues/1534))
