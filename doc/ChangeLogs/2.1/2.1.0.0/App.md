2.1.0.0

![banner](https://res.classisland.tech/banners/2.1.webp)

> [!important]
> 在升级到 2.0 前，建议先阅读[2.0 相关问答（Ⅱ）](https://github.com/ClassIsland/ClassIsland/discussions/1486)和[2.0 相关问答（Ⅰ）](https://github.com/ClassIsland/ClassIsland/discussions/1145)。

2.1 - LiliyaOlenyeva（莉莉娅·阿琳）

在 2.1 - LiliyaOlenyeva，我们提升了 ClassIsland 的易用性，使其更好用，更易于上手。

## 🚀 新增功能与优化

- **【主界面/编辑模式】编辑模式：** 支持通过编辑模式更直观和便捷地编辑主界面上的组件和调整相关设置 ([#1249](https://github.com/ClassIsland/ClassIsland/issues/1249))
- **【UI/档案编辑器】表格课表编辑视图：** 支持以周表格的形式快速录入和查看课表。([#238](https://github.com/ClassIsland/ClassIsland/issues/238))
- **【教程】教程：** 加入教程，手把手助您上手本应用
- **【翻新】翻新与迎新：** 在长假长时间没有启动应用后，应用会询问您是否需要翻新部分设置以适应新学期。迎新模式将在翻新的基础上重置教程状态，以便新同学快速上手本应用 ([#496](https://github.com/ClassIsland/ClassIsland/issues/496))
- **【UI/应用设置】导航栏分组：** 实现应用设置页面分组，优化侧边栏高亮逻辑 ([#1587](https://github.com/ClassIsland/ClassIsland/issues/1587)) ([#1249](https://github.com/ClassIsland/ClassIsland/issues/1249))
- 【插件】实现 MacOS 自定义程序集解析器
- 【插件】允许插件指定所支持的OS平台 (([#1712](https://github.com/ClassIsland/ClassIsland/issues/1712)))
- 【插件】在插件安装/卸载时添加日志 (([#1824](https://github.com/ClassIsland/ClassIsland/issues/1824))) by @diann34
- 【档案编辑】为时间表编辑器添加撤销/重做功能 (([#1818](https://github.com/ClassIsland/ClassIsland/issues/1818))) by @Akiyama-Mizuki-44
- 【档案编辑器】创建临时层课表时支持同时创建临时时间表 (([#1781](https://github.com/ClassIsland/ClassIsland/issues/1781))) by @Chineseshuaji
- 【附加设置/上下课提醒】上下课提醒特殊设置分组，并隐藏当前节点不支持的设置
- 【规则集/天气】固定天气规则集设置下拉框宽度 ([#1849](https://github.com/ClassIsland/ClassIsland/issues/1849)) by @HelloWRC
- 【恢复】修改了回滚按钮在恢复模式中的启用状态，使其由是否有多个有效的安装决定，同时为回滚按钮添加了OnClick事件 ([#1598](https://github.com/ClassIsland/ClassIsland/issues/1598))
- 【开发】增强调试体验 (([#1621](https://github.com/ClassIsland/ClassIsland/issues/1621)))
- 【平台/Windows】在当前前台窗口不是 ClassIsland 时，不移走窗口焦点 by @HelloWRC
- 【平台/X11】重新移植兼容缩放模式
- 【启动屏幕】移植启动加载界面
- 【示例】移植 ExamplePlugin 到 ClassIsland 2.0
- 【数据迁移】打包导入与导出应用数据 ([#1080](https://github.com/ClassIsland/ClassIsland/issues/1080)) by @HelloWRC
- 【提醒】新版提醒后台工作服务
- 【天气】允许使用不加密的 HTTP 协议获取天气信息 ([#1569](https://github.com/ClassIsland/ClassIsland/issues/1569)) (([#1570](https://github.com/ClassIsland/ClassIsland/issues/1570)))
- 【天气】添加明天天气规则和天气模糊匹配支持 (([#1746](https://github.com/ClassIsland/ClassIsland/issues/1746)))
- 【遥测】关闭日志收集功能 by @HelloWRC
- 【依赖】将 Avalonia 升级到 11.3.12
- 【应用】在插件崩溃信息中添加插件版本 (([#1671](https://github.com/ClassIsland/ClassIsland/issues/1671)))
- 【应用】升级 Sentry SDK，并优化遥测埋点
- 【应用】添加自启动参数实现自启动重复启动静默退出 (([#1782](https://github.com/ClassIsland/ClassIsland/issues/1782))) by @xfy2412
- 【应用】阻止 Warning 及以下等级的日志进入事件查看器刷屏 ([#1812](https://github.com/ClassIsland/ClassIsland/issues/1812)) by @HelloWRC
- 【应用设置/插件】拖拽安装插件&安装确定提示 (([#1765](https://github.com/ClassIsland/ClassIsland/issues/1765))) by @baiyao105
- 【应用设置/插件】插件页ui优化 (([#1769](https://github.com/ClassIsland/ClassIsland/issues/1769))) by @baiyao105
- 【应用设置/插件】添加信息至InstallPluginConfirmTemplate (([#1831](https://github.com/ClassIsland/ClassIsland/issues/1831))) by @diann34
- 【应用设置/天气】调整天气预览布局 (([#1773](https://github.com/ClassIsland/ClassIsland/issues/1773))) by @baiyao105
- 【应用设置/天气】定位状态反馈 (([#1764](https://github.com/ClassIsland/ClassIsland/issues/1764))) by @baiyao105
- 【应用设置/主题】在主题设置界面显示分体主界面启用提醒
- 【诊断】在生成的诊断信息中输出 TraceId
- 【诊断】为诊断模式添加逻辑 (([#1778](https://github.com/ClassIsland/ClassIsland/issues/1778))) by @diann34
- 【主界面】优化主界面动画曲线
- 【主界面】允许设置更高的置顶频率 ([#536](https://github.com/ClassIsland/ClassIsland/issues/536))
- 【主界面】部分回滚新版主界面布局，仅在编辑模式下采取新版主界面布局模式
- 【主界面】实现主界面主题阴影安全区
- 【主界面】行隐藏功能 (([#1606](https://github.com/ClassIsland/ClassIsland/issues/1606)))
- 【主界面】优化编辑模式下的主界面标题 by @HelloWRC
- 【主界面】添加了防截图和防录制的可选项在“窗口”设置中。 (([#1663](https://github.com/ClassIsland/ClassIsland/issues/1663))) by @OutHimic
- 【主界面】将主界面行间距修改为 Spacing 实现 ([#1845](https://github.com/ClassIsland/ClassIsland/issues/1845)) by @HelloWRC
- 【主界面】将最小行间距限制为 0 ([#1845](https://github.com/ClassIsland/ClassIsland/issues/1845)) by @HelloWRC
- 【组件/倒计时】倒计时组件进度显示添加新的格式化变量 %p显示两位小数 ([#1516](https://github.com/ClassIsland/ClassIsland/issues/1516)) ([#1757](https://github.com/ClassIsland/ClassIsland/issues/1757))
- 【组件/滚动容器】钳制滚动组件动画速度 by @HelloWRC
- 【组件/文本组件】文本组件文本支持使用默认前景色 ([#599](https://github.com/ClassIsland/ClassIsland/issues/599)) ([#1410](https://github.com/ClassIsland/ClassIsland/issues/1410))
- 【API/附加设置】附加设置附加的节点信息 API
- 【UI】FluentIconsMappingGenerator Roslyn 生成器、FIExtension XAML 扩展 (([#1791](https://github.com/ClassIsland/ClassIsland/issues/1791))) by @lrsgzs
- 【UI】优化 MainWindow.axaml 和 TimeLineListControl.axaml 中的 Adorner 层级 by @HelloWRC
- 【UI/档案编辑】编辑档案 档案列表 TreeView (([#1833](https://github.com/ClassIsland/ClassIsland/issues/1833))) by @lrsgzs
- 【UI/档案编辑器】优化时间线编辑视图的外观
- 【UI/档案编辑器】优化时间线视图的时间块外观
- 【UI/档案编辑器】档案编辑器 Uri 导航支持导航到子页面 ([#1039](https://github.com/ClassIsland/ClassIsland/issues/1039))
- 【UI/档案编辑器】课表编辑器集成日历视图
- 【UI/档案编辑器】为时间线编辑器添加快速添加时间点按钮 by @HelloWRC
- 【UI/教学】优化教学提示框外观 by @HelloWRC
- 【UI/应用设置】将【基本】设置页面的部分内容拆分到【时钟】与【高级】页面中
- 【UI/应用设置】将应用设置界面的标题栏高度设置为 48
- 【UI/应用设置】确保同时“需要重启”信息框只显示一个
- 【UI/应用设置/更新】移植更新日志查看功能，添加更新日志懒加载 ([#1637](https://github.com/ClassIsland/ClassIsland/issues/1637))
- 【UI/应用设置/更新】优化更新页面错误显示
- 【UI/应用设置/关于】补全关于界面的第三方库列表 by @HelloWRC
- 【UI/应用设置/提醒】在禁用所有主界面行的提醒时在提醒页面显示警告
- 【UI/应用设置/提醒】优化提醒设置操作逻辑，为提醒高级设置添加快速指示
- 【UI/应用设置/自动化】优化规则集工作指示器外观
- 【UI/主菜单】为主菜单添加图标
- 【UI/主菜单】优化主菜单项目排序与描述
- 【UI/主菜单】为托盘菜单点击添加“打开设置窗口”行为，并将其设置为非Windows平台的默认值 ([#1655](https://github.com/ClassIsland/ClassIsland/issues/1655))

## ♻️ 移除的功能

- 【主界面】弃用主界面点击功能

## 🐛 Bug 修复

- 【备份】修复无法正常清理自动备份的问题 (([#1750](https://github.com/ClassIsland/ClassIsland/issues/1750)))
- 【插件市场】修复插件更新通知和自动更新选项无效的问题 ([#1661](https://github.com/ClassIsland/ClassIsland/issues/1661))
- 【档案编辑】修复了在时间表中点击刷新按钮时，若时间表中存在两个起始时间相同的项就会发生顺序跳变的问题 (([#1839](https://github.com/ClassIsland/ClassIsland/issues/1839))) ([#1820](https://github.com/ClassIsland/ClassIsland/issues/1820)) by @wan-an-zz
- 【档案编辑】修复时间线编辑器在使用快捷添加时间点面板时滚动回弹的问题 by @HelloWRC
- 【档案编辑】修复集控策略围栏不生效的问题 by @HelloWRC
- 【顶层效果窗口】修复顶层效果窗口错位的问题 by @HelloWRC
- 【集控】修复自集控拉取组件数据时无法解析颜色值的问题 (([#1703](https://github.com/ClassIsland/ClassIsland/issues/1703)))
- 【集控】收到通知后无通知显示 (([#1707](https://github.com/ClassIsland/ClassIsland/issues/1707)))
- 【教学】修复了“编辑主界面”窗口报错、崩溃的问题(issue([#1829](https://github.com/ClassIsland/ClassIsland/issues/1829))) (([#1837](https://github.com/ClassIsland/ClassIsland/issues/1837))) by @wan-an-zz
- 【课表控件】修复课表组件双向绑定会意外更新时间点信息的问题 ([#1714](https://github.com/ClassIsland/ClassIsland/issues/1714))
- 【平台/Windows】修复在 Arm64 架构上因 Harmony 不支持 arm64 导致修补失败的问题 ([#1638](https://github.com/ClassIsland/ClassIsland/issues/1638))
- 【平台/Windows】修复在 x86 架构上播放音频崩溃的问题 ([#1485](https://github.com/ClassIsland/ClassIsland/issues/1485))
- 【平台/Windows】修复在将窗口焦点移动到其它窗口时的逻辑错误 by @HelloWRC
- 【提醒】孤儿提醒移交，修复提醒在显示的主界面行被卸载后消失的问题 ([#1332](https://github.com/ClassIsland/ClassIsland/issues/1332))
- 【提醒】在遮罩期间保持播放提醒音 ([#1704](https://github.com/ClassIsland/ClassIsland/issues/1704)) ([#1673](https://github.com/ClassIsland/ClassIsland/issues/1673))
- 【提醒/上下课提醒】修复户外课程即将上课提醒未使用设置的户外提醒遮罩的问题 ([#1686](https://github.com/ClassIsland/ClassIsland/issues/1686))
- 【提醒/上下课提醒】修复上下课提醒部分文字不跟随用户文字颜色设置的问题 ([#1659](https://github.com/ClassIsland/ClassIsland/issues/1659))
- 【音频】修复在 Linux 下特定情况下因快速重复初始化 AudioPlaybackDevice 导致应用崩溃的问题
- 【应用】修复设计器无法使用的问题
- 【应用】修复 WOA 平台上程序无法启动的问题
- 【应用】修复在特定情况下 GetRootWindow 会返回已关闭的窗口的问题
- 【应用设置/主题】分体主题infobar重叠 (([#1768](https://github.com/ClassIsland/ClassIsland/issues/1768))) by @baiyao105
- 【应用设置/组件】修复组件 ListBoxItem 只能拖动一次的问题 by @HelloWRC
- 【应用设置/组件设置】修复组件设置页面的组件规则集无法正常编辑的问题 ([#1665](https://github.com/ClassIsland/ClassIsland/issues/1665))
- 【语音/EdgeTTS】修复 EdgeTTS Websocket 可能会在不适合的时机断开的问题 ([#1567](https://github.com/ClassIsland/ClassIsland/issues/1567))
- 【主窗口/编辑模式】修复在 Linux 下的编辑模式中窗口容易消失的问题
- 【主界面】修复主界面行设置中的自定义字体大小默认为 0 的问题
- 【主界面】修复提醒进度条导致主界面无法收缩的问题
- 【主界面】修复主界面会被意外最大化/最小化的问题 ([#1649](https://github.com/ClassIsland/ClassIsland/issues/1649))
- 【主界面】修复 RawInput 在不受支持的平台上初始化的问题
- 【主界面】修复主界面会意外获得焦点的问题 ([#1822](https://github.com/ClassIsland/ClassIsland/issues/1822)) by @HelloWRC
- 【主界面】修复在支持 AcrylicBlur 的系统版本上意外启用了不透明背景回退，进而导致出现模糊背景残留的问题 ([#1827](https://github.com/ClassIsland/ClassIsland/issues/1827)) by @HelloWRC
- 【主界面】修复主界面竖直方位计算错误的问题 by @HelloWRC
- 【UI】修复部分窗口可能会被意外最大化的问题 ([#1650](https://github.com/ClassIsland/ClassIsland/issues/1650))
- 【UI】修复 ControlPreventNullConverter 因静态控件重复使用导致的视觉树冲突 (([#1748](https://github.com/ClassIsland/ClassIsland/issues/1748)))
- 【UI】修复 Linux 下 TeachingTip 弹出时屏幕左上角会出现异常白色窗口的问题 ([#1676](https://github.com/ClassIsland/ClassIsland/issues/1676)) by @HelloWRC
- 【UI/档案编辑器】修复表格课表编辑器重载后部分元素消失的问题
- 【UI/档案编辑器】修复档案编辑器退出时未自动保存档案的问题 ([#1667](https://github.com/ClassIsland/ClassIsland/issues/1667))
- 【UI/档案编辑器】修复时间线编辑器的时间线视图拖动时间点会出现越界的问题 ([#1729](https://github.com/ClassIsland/ClassIsland/issues/1729))
- 【UI/档案编辑器】修复新建课表时无法复制临时层课表的问题 ([#1675](https://github.com/ClassIsland/ClassIsland/issues/1675))
- 【UI/档案编辑器】修复档案编辑>课表>列表视图 右侧的“选择后移动到下一节课”失效的问题 ([#1674](https://github.com/ClassIsland/ClassIsland/issues/1674))
- 【UI/规则集】修复规则集编辑界面因虚拟化问题导致滚动卡死的问题 ([#1627](https://github.com/ClassIsland/ClassIsland/issues/1627))
- 【UI/欢迎】修复欢迎界面的基本页面可能出现无法前进的问题
- 【UI/课表】修复时间点附加信息会在切换主题时消失的问题 ([#1468](https://github.com/ClassIsland/ClassIsland/issues/1468))
- 【UI/课表控件】修复初始化课表控件时的绑定错误，提升加载速度
- 【UI/认证】修复认证窗口不能正常在截图中隐藏的问题
- 【UI/应用设置】修复自定义插件镜像源输入框不可见/不好定位的问题 ([#1534](https://github.com/ClassIsland/ClassIsland/issues/1534))
- 【UI/应用设置/更新】修复更新页面下载中动画偏移的问题
- 【UI/应用设置/天气】纠正天气设置页面重复「使用」的笔误 (([#1722](https://github.com/ClassIsland/ClassIsland/issues/1722)))
- 【UI/应用设置/主题】解决主题信息可编辑的问题
- 【UI/应用设置/组件】修复组件设置页面按钮无法使用的问题，优化组件设置页面拖拽逻辑 ([#1592](https://github.com/ClassIsland/ClassIsland/issues/1592)) ([#1603](https://github.com/ClassIsland/ClassIsland/issues/1603))
- 【UI/应用设置/组件】修复旧版组件编辑界面的规则集选项消失的问题 ([#1604](https://github.com/ClassIsland/ClassIsland/issues/1604))
- 【UI/应用设置/组件】修复组件设置底部边距不正确的bug
