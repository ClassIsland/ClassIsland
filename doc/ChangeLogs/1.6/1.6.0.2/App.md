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
