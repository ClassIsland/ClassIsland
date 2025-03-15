# 1.5.3.0

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能

## 🚀 新增功能与优化

- **【组件】多行组件：**支持为主界面设置多行组件 ([#181](https://github.com/ClassIsland/ClassIsland/issues/181))
- **【集控】本地集控：**允许在不使用集控服务的情况下在本地设置密码和限制策略
- **【集控】密码保护功能：**允许为某些功能设置访问密码 ([#311](https://github.com/ClassIsland/ClassIsland/issues/311))
- 【天气】天气的搜索城市不再使用本地数据库，改为在线获取城市信息
- 【天气】添加对非中国内地城市的支持
- 【天气】获取日出日落数据
- 【提醒/语音】添加 GPT-SoVITS 语音支持 ([#590](https://github.com/ClassIsland/ClassIsland/issues/590))
- 【更新】重构更新系统，支持从内地源下载应用更新 ([#401](https://github.com/ClassIsland/ClassIsland/issues/401))
- 【API/认证】认证提供方注册功能
- 【API/Uri 导航】允许在通过 Uri 导航应用设置页面时保留历史记录
- 【API/通用对话框】添加 CommonDialogBuilder.SetIconKind 方法以按照对话框类型设置图标

## ♻ 移除的功能

- 【调试】移除调试中集控策略选项

## 🐛 Bug 修复

- 【插件】修复加载损坏的插件配置导致应用无法启动的问题 ([#565](https://github.com/ClassIsland/ClassIsland/issues/565))
- 【构建】修复 Edge_tts_sharp 项目还原问题
