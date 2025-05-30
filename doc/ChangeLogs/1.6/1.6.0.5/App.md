# 1.6.0.5

本版本修复了先前版本的一些问题。

1.6 - Himeko

## 🚀 新增功能与优化

- 【应用设置】在 Release 构建中启用调试菜单确认窗口
- 【API/插件 SDK】打包插件包时支持通过设置属性`GenerateHashSummary`为`false`跳过 MD5 计算
- 【API/插件 SDK】支持计算 MD5 时通过设置属性`PowershellBinaryName`自定义 Powershell 命令

## 🐛 Bug 修复

- 【UI】缓解在部分界面 UI 停止渲染的问题
- 【行动】([#913](https://github.com/ClassIsland/ClassIsland/issues/913)) 修复在行动提醒中启用高级设置时，无法使用语音的问题
- 【档案编辑器】([#885](https://github.com/ClassIsland/ClassIsland/issues/885)) 修复选择临时课表时，课表名中的第一个下划线不显示的问题
- 【应用设置/调试】([#886](https://github.com/ClassIsland/ClassIsland/issues/886)) 修复在测试 Markdown 时输入无效 URI 导致崩溃的问题

<!-- generated by git-cliff -->
