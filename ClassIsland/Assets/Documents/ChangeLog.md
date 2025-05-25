# 新增功能

1.7 - RyouYamada 的新增功能。

## 直接禁用课程

![档案编辑 UI](http://res.classisland.tech/screenshots/changelogs/1.7/1.7.0.0/1.png)

ClassIsland 现在可以直接禁用某个课程表中的某些课程，无需复制和编辑时间表，同时不影响原时间表和其它课表。禁用后的课程对应的时间点及其前后的课间休息时间点和分割线也会被一起禁用。

## 主题

![主题市场](https://res.classisland.tech/screenshots/changelogs/1.7/1.6.3.0/2.png)

ClassIsland 已将主题系统集成到应用中，并加入了主题市场，以便用户分享和下载主题。您可以在[【应用设置】->【主题】](classisland://app/settings/classisland.themes)中了解更多信息。

## 导出到表格

![导出到表格](https://res.classisland.tech/screenshots/changelogs/1.7/1.6.3.0/1.png)

ClassIsland 现在支持将课表内容导出到 Excel 表格，以便打印和分发。您可以在【导出到表格】工具中选择要导出的课表，并进行基本的编辑操作。

## 新提醒 API

![提醒设置](https://res.classisland.tech/screenshots/changelogs/1.7/1.6.2.0/1.png)

加入了新版提醒 API，具体变化如下：

- 允许提醒在提醒提供方下注册提醒渠道，提供更细化的提醒自定义选项。
- 提醒内容支持使用数据模板，减少开发提醒提供方的重复代码。
- 加入链式提醒。取消链式提醒中的提醒的同时也会取消链式提醒中的其它提醒。

目前已将上下课提醒中的准备上课提醒、上课提醒和下课提醒分离到了各自的提醒渠道中，以便单独设置提醒设置。您可以在[【应用设置】->【提醒】](classisland://app/settings/notification)中了解更多信息。

## 天气增强

现在天气可以使用系统定位接口进行定位，提升降水预报的准确度（仅限内地）和自动设置天气城市。在[【应用设置】->【天气】](classisland://app/settings/weather)中以了解更多信息。

除此以外，天气组件也添加了以下可以显示的信息：

- 湿度
- 风力与风向
- AQI
- 气压
- 体感温度

同时 ClassIsland 现在改为使用 SF Symbol Icon 作为天气类型的图标，增强了各个降水天气间的区分度。

## 插件间联动

在 1.6.1.0 开始，插件可以声明插件依赖项，在插件加载程序集时会自动从声明的依赖中查找需要的程序集。并且加载插件时会自动验证依赖关系和处理插件加载顺序。

***

# 1.7.0.0

1.7 -RyouYamada

## 🚀 新增功能与优化

- **【主题】主题功能：** 将主题功能整合到应用中，并加入主题市场支持 ([#766](https://github.com/ClassIsland/ClassIsland/issues/766))
- **【提醒】提醒 API v2：** 加入了新版提醒 API，具体变化如下：
    - 允许提醒在提醒提供方下注册提醒渠道，提供更细化的提醒自定义选项。
    - 提醒内容支持使用数据模板，减少开发提醒提供方的重复代码。
    - 加入链式提醒。取消链式提醒中的提醒的同时也会取消链式提醒中的其它提醒。
- **【导出到表格】导出到表格：**支持将课表导出到 Excel 表格，以便进行打印和分发 ([#410](https://github.com/ClassIsland/ClassIsland/issues/410))
- **【插件】允许插件间联动：** 允许插件间互相调用接口，并自动验证和处理依赖关系。 ([#758](https://github.com/ClassIsland/ClassIsland/issues/758))
- 【组件】滚动组件 ([#393](https://github.com/ClassIsland/ClassIsland/issues/393))
- 【组件/轮播组件】轮播组件动画 ([#504](https://github.com/ClassIsland/ClassIsland/issues/504))
- 【组件/天气】显示更多天气信息 ([#480](https://github.com/ClassIsland/ClassIsland/issues/480))
- 【组件/天气】天气组件降水预警 m->min
- 【组件/倒计时】倒计时连接词新增“仅剩”选项 (([#1028](https://github.com/ClassIsland/ClassIsland/issues/1028)))
- 【应用设置】自动时间校准按钮 ([#123](https://github.com/ClassIsland/ClassIsland/issues/123))
- 【应用设置/组件】优化组件设置页面组件库 VirtualizingWrapPanel 外观
- 【应用设置/组件】新增组件操作：包裹组件、创建组件副本
- 【应用设置/插件】添加插件市场空白占位符
- 【应用设置/插件】显示插件下载量和 stars
- 【应用设置/调试】在 Release 构建中启用调试菜单的确认
- 【应用设置/提醒】优化高级提醒设置 UI
- 【提醒】将应用内置的提醒提供方迁移到 v2 提醒 API
- 【提醒】优化户外课程默认提示语文本表述 ([#933](https://github.com/ClassIsland/ClassIsland/issues/933))
- 【提醒】户外课程支持自定义提醒文本
- 【提醒/上课提醒】提醒渠道支持，将上下课提醒拆分到多个提醒渠道中 ([#517](https://github.com/ClassIsland/ClassIsland/issues/517))
- 【提醒/上课提醒】优化即将上课提醒发布逻辑
- 【档案】直接隐藏一定时间的课程 ([#182](https://github.com/ClassIsland/ClassIsland/issues/182))
- 【档案】添加复姓处理逻辑到 Subject 类
- 【档案编辑】按下 Ctrl+S 保存档案
- 【天气】天气自动定位，并根据经纬坐标获取天气信息 ([#547](https://github.com/ClassIsland/ClassIsland/issues/547))
- 【天气】将天气图标修改为 Sf Symbol Icon
- 【应用】崩溃时自动禁用导致崩溃的异常插件
- 【应用】优化日志中内存使用量的显示、添加初步本地化语言文本 ([#956](https://github.com/ClassIsland/ClassIsland/issues/956))
- 【自动化】在特定时间点前指定时间触发自动化 ([#458](https://github.com/ClassIsland/ClassIsland/issues/458))
- 【行动】加入重启应用、设置主界面偏移和设置主界面置顶状态的行动 ([#981](https://github.com/ClassIsland/ClassIsland/issues/981)) ([#833](https://github.com/ClassIsland/ClassIsland/issues/833)) ([#773](https://github.com/ClassIsland/ClassIsland/issues/773))
- 【主题色提取】引入实验性取色算法
- 【应用】将未捕获的异常 UCEERR_RENDERTHREADFAILURE (0x88980406) 视为关键异常
- 【诊断】崩溃来源插件分析
- 【崩溃报告】崩溃报告显示 traceId
- 【崩溃报告】优化崩溃报告字体
- 【日志】在日志输出中添加在提取主题色时的结果
- 【日志】优化日志记录中内存使用量的显示
- 【日志】日志打码功能
- 【日志】优化控制台日志输出，添加作用域支持
- 【日志】在组件加载耗时太长时记录警告日志
- 【备份】将备份存储方式由文件夹形式改为压缩文件形式
- 【集控】集控上传审计事件 ([#744](https://github.com/ClassIsland/ClassIsland/issues/744))
- 【集控】添加组件与认证配置拉取支持 ([#744](https://github.com/ClassIsland/ClassIsland/issues/744))
- 【本地化】添加本地化语言初步支持
- 【API/语音】添加自定义语音服务 API ([#528](https://github.com/ClassIsland/ClassIsland/issues/528))
- 【API/启动动画】引入启动屏幕抽象基类，支持插件自定义启动屏幕
- 【API/插件 SDK】打包插件包时支持跳过 MD5 计算和自定义 Powershell 命令

## 🐛 Bug 修复

- 【组件/轮播组件】修复轮播组件在重载后因重复注册计时器 Tick 事件导致无法正常轮播的问题 ([#878](https://github.com/ClassIsland/ClassIsland/issues/878))
- 【组件/课表】修复切换时间表后当前时间点不显示的问题 ([#912](https://github.com/ClassIsland/ClassIsland/issues/912))
- 【组件/轮播组件】修复轮播组件内存泄漏的问题 ([#882](https://github.com/ClassIsland/ClassIsland/issues/882))
- 【主界面】修复提醒时切换多行组件配置方案或重载主题出现 Bug 的问题 ([#628](https://github.com/ClassIsland/ClassIsland/issues/628))
- 【主界面】修复在主界面行控件未完全初始化时发出提醒报错的问题 ([#737](https://github.com/ClassIsland/ClassIsland/issues/737))
- 【主界面】修复禁用鼠标穿透后无法点击主界面元素的问题
- 【应用设置】修复在系统 TTS 不可用时选择其它 TTS 提供方时设置被覆盖的问题
- 【应用设置/插件】修复无法正常更新插件的问题 ([#924](https://github.com/ClassIsland/ClassIsland/issues/924))
- 【应用设置/关于】修复无法显示贡献人员的问题
- 【应用设置/调试】修复在测试 Markdown 时输入无效 URI 导致崩溃的问题 ([#886](https://github.com/ClassIsland/ClassIsland/issues/886))
- 【应用设置/插件】([#1049](https://github.com/ClassIsland/ClassIsland/issues/1049)) 修复插件页面无法正常绑定选择的插件的问题
- 【提醒】修复附加设置中的户外课程提醒文本不生效的问题
- 【提醒】修复缺少标点符号导致播报任教老师时可能出现断句问题
- 【提醒】修复提醒渠道设置不能保存的问题 ([#987](https://github.com/ClassIsland/ClassIsland/issues/987))
- 【提醒】修复滚动文字提醒（天气预警、滚动文字提醒模板）在部分主题下高度错位的问题  ([#943](https://github.com/ClassIsland/ClassIsland/issues/943))
- 【提醒】修复准备上课时覆盖的附加设置不生效的问题 ([#935](https://github.com/ClassIsland/ClassIsland/issues/935))
- 【提醒】修复发出准备上课提醒时报错的问题 ([#905](https://github.com/ClassIsland/ClassIsland/issues/905))
- 【提醒/行动】行动提醒遮罩字体大小使用强调字体大小
- 【提醒/行动】修复在行动提醒中启用高级设置时，无法使用语音的问题 ([#913](https://github.com/ClassIsland/ClassIsland/issues/913)) 
- 【UI】修正过时的文档链接
- 【UI】缓解在部分界面 UI 停止渲染的问题
- 【应用】修复重复触发应用停止事件的问题 ([#1014](https://github.com/ClassIsland/ClassIsland/issues/1014))
- 【课程服务】修复编辑当前加载的时间表时课程服务索引溢出的问题
- 【档案】修复 ClassInfo 获取课程对应时间点超出索引的问题
- 【档案编辑】修复调课日历中课表选择界面不显示课表的第一个下划线的问题 ([#921](https://github.com/ClassIsland/ClassIsland/issues/921))
- 【档案编辑】修复选择临时课表时，课表名中的第一个下划线不显示的问题 ([#885](https://github.com/ClassIsland/ClassIsland/issues/885)) 
- 【档案编辑】修复调课页面在更新选择日期后没有及时更新表格内容的问题 ([#1017](https://github.com/ClassIsland/ClassIsland/issues/1017))
- 【集控】正常执行集控服务器的 DataUpdated 指令

## 🐛 Bug 修复（1.6.3.0 - 1.7.0.0）

- 【组件/天气】([#1033](https://github.com/ClassIsland/ClassIsland/issues/1033)) 修复天气图标大小不统一的问题
- 【应用设置/组件】修复组件操作菜单从按钮打开时重复组件命令绑定错误的问题
- 【应用设置/提醒】修复语音源未移除旧设置属性绑定的问题
- 【应用设置/提醒】修复未移除的提醒语音禁用逻辑导致无法修改提醒语音设置的问题
- 【应用】([#1044](https://github.com/ClassIsland/ClassIsland/issues/1044)) 为由用户输入的 TimeSpan 添加边界限制
- 【提醒】修复提醒语音会被自动禁用的问题
- 【提醒】([#1041](https://github.com/ClassIsland/ClassIsland/issues/1041)) 修复直接通过提醒请求指定提醒渠道 ID 时提醒渠道的设置不生效的问题
- 【提醒】修复启动时获取语音提供方服务卡死的问题
- 【提醒】修复上课提醒提前结束时重复发送上课提醒的问题

## ◀️ 回滚的功能（1.6.2.0 - 1.7.0.0）

- 【提醒】**破坏性更改** 移除手动指定提醒模板资源键功能
