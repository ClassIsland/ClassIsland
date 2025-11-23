# 1.7.106.2

<img src="https://res.classisland.tech/stickers/2.png" height="75" width="75" alt="白厄_再见" title="白厄_再见"/>

> 当前版本为 2.0 的测试版，部分功能可能不稳定或不支持。如果您在使用过程中遇到问题，欢迎前往 Issues 中反馈！

2.0 - Khaslana（卡厄斯兰那） Release Preview 3

🎉 本版本是 2.0 的最后一个测试版，将在确认稳定并修复在这个版本收到的问题后，在下一个版本发布 2.0 的正式版。

## 🚀 新增功能与优化

- 【数据迁移】在迁移完成后自动更新插件，并禁用旧版插件
- 【主界面】将主界面透明回滚笔刷设置为透明
- 【平台/Windows】允许使用系统标题栏，并在 1809 以下的版本默认使用系统标题栏以避免兼容性问题
- 【平台/Windows】优化系统标题栏的默认启用条件
- 【插件】拒绝加载 Api 版本小于 2.0.0.0 的插件
- 【插件市场】插件自动更新 ([#591](https://github.com/ClassIsland/ClassIsland/issues/591))
- 【插件市场】使用新版索引文件（index.v2.json）
- 【插件市场】添加忽略插件镜像ssl/tls错误开关
- 【组件/倒计时】提升倒计时环形进度条对比度，优化可读性
- 【组件/天气】修复天气组件设置主天气开关缺失的问题
- 【组件/天气】优化天气组件布局
- 【UI】为 AnimatedIconButton 添加动画
- 【UI】恢复文本选择手柄
- 【UI/档案编辑器】优化档案编辑器部分文字表述
- 【UI/档案编辑器】键盘上下键切换编辑的时间点
- 【UI/规则集】优化规则集编辑器外观
- 【UI/规则集】优化降水规则外观
- 【UI/规则集】为天气规则的天气组合框添加天气图标
- 【UI/课表启用】优化课表启用规则编辑界面交互
- 【UI/天气】添加 SF 符号天气图标包

## 🐛 Bug 修复

- 【组件/天气】移除天气预警未显示时的空白区域
- 【行动】修复 ActionService 获取的懒加载方式
- 【启动器】修复在启动启动器时存在 ClassIsland_PackageRoot 导致重复设置 ClassIsland_PackageRoot 导致启动器崩溃的问题
- 【启动器】优化启动器部署获取逻辑，修复启动器会选取空文件夹并忽略其它部署文件夹的问题
- 【启动器】修复启动器部署选择排序方向问题
- 【提醒】修复使用默认提醒音时不播放提醒音效的问题 ([#1462](https://github.com/ClassIsland/ClassIsland/issues/1462))
- 【提醒】修复等待提醒音效播放的问题导致提醒涟漪动画播放推迟的问题
- 【平台/Windows】修复在 Windows 上主界面置底时应用菜单和部分弹出窗口会置底的问题 ([#1404](https://github.com/ClassIsland/ClassIsland/issues/1404))
- 【平台/Windows】修复在使用非 WinUIComposition 的 CompositionMode 时会使用云母效果，导致窗口背景异常的问题
- 【平台/Windows】修复 Windows 平台下触摸键盘不弹出的问题
- 【平台/Windows】安全卸载WinEventHook
- 【平台/Windows/插件】修复插件携带的 WinRT 运行时和应用内置的 WinRT 运行时冲突的问题
- 【平台/Linux】修复 deb 打包未包含 libicu76/77 的问题 ([#1316](https://github.com/ClassIsland/ClassIsland/issues/1316))
- 【组件/轮播容器】修复轮播容器内的组件高度溢出的问题
- 【组件/天气】修复降水提示图标可读性差的问题 ([#1402](https://github.com/ClassIsland/ClassIsland/issues/1402))
- 【UI】修复课表组件和倒计时组件的进度条在变宽后无法收缩长度的问题
- 【UI/课程控件】修复当前没有选择的课程时不淡化已经上过的课程的问题 ([#1464](https://github.com/ClassIsland/ClassIsland/issues/1464))
- 【UI/主界面】修复部分控件未应用自定义前景色的问题
- 【UI/主界面】修复组件宽度过低时圆角异常的问题
- 【UI/课表启用】修复在首次显示时间规则编辑器时，【当本周是】栏目空白的问题
- 【UI/应用设置/组件】移除组件库卡片动画类 ([#1253](https://github.com/ClassIsland/ClassIsland/issues/1253))
- 【XAML 主题】为插件索引获取添加回滚
- 【开发】修正开发环境变量路径


