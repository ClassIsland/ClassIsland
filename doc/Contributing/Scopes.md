# 提交范围（草案）

本草案仍在修订，如果您发现这里没有能很好地概括您的更改的范围，可以考虑修改此文档，添加一个新的范围。

## 基本

| 类别            | 说明             |
|---------------|----------------|
| app           | 对整个应用生效的更改     |
| mainwindow    | 主界面            |
| topmost-ew    | 顶层特效窗口         |
| ui\[/xxx\]    | UI 相关更改        |
| api           | 公共 API         |
| plugin-sdk    | 插件 SDK         |
| platforms/xxx | 关于 xxx 平台接口的更改 |
| build         | 构建系统           |
| packaging     | 打包系统           |


## 主要子系统

关于这些子系统的服务，以及其模型、视图模型类的更改可以使用这些类别。

| 类别                    | 说明            |
|-----------------------|---------------|
| lessons               | 课程服务          |
| profile               | 档案服务          |
| attached-settings     | 档案附加设置        |
| profile-analyze       | 档案分析服务        |
| notification          | 提醒服务          |
| settings              | 应用设置服务        |
| migration             | 设置、档案迁移（升级）功能 |
| data-transfer         | 设置、档案导入/导出功能  |
| component             | 组件服务          |
| automation            | 自动化服务         |
| actions               | 行动服务          |
| ruleset               | 规则集服务         |
| management            | 集控服务          |
| update                | 更新服务          |
| diag                  | 诊断服务          |
| ipc                   | IPC           |
| weather               | 天气服务          |
| audio                 | 音频服务          |
| uri                   | Uri 导航服务      |
| auth                  | 认证服务          |
| logs                  | 日志功能          |
| speech                | 语音服务          |
| exact-time            | 精确时间服务        |
| hang                  | 挂起检测服务        |
| mem-watchdog          | 内存看门狗服务       |
| plugin                | 插件服务          |
| plugin-marketplace    | 插件市场服务        |
| theme                 | 界面主题服务        |
| xaml-theme            | 主界面 XAML 主题服务 |
| window-rule           | 窗口规则服务        |
| metadata/announcement | 公告服务          |

特别的，在以上类别前方加上`ui/`可用于指有关这些子系统的界面部分的更改。

## 界面

对应用的主要界面可以使用以下范围。

| 类别                   | 说明      |
|----------------------|---------|
| ui/settings-window   | 应用设置窗口  |
| ui/profile-editor    | 档案编辑器   |
| ui/classplan-details | 课表看板    |
| ui/class-swap        | 换课      |
| ui/logs              | 日志查看器   |
| ui/auth              | 认证窗口    |
| ui/data-transfer     | 数据迁移    |
| ui/dev-portal        | 开发者门户   |
| ui/recovery          | 恢复      |
| ui/time-adjustment   | 时间对齐    |
| ui/welcome           | 欢迎向导    |
| ui/windowrule-debug  | 窗口规则集调试 |

## 注册项

对这些注册项的功能和 UI 的更改可以使用以下范围。

| 类别                                  | 说明     |
|-------------------------------------|--------|
| r/components/xxx                    | 组件     |
| r/notifications/xxx                 | 提醒提供方  |
| r/notification-effects/xxx          | 提醒特效   |
| r/actions/xxx                       | 行动     |
| r/triggers/xxx                      | 自动化触发器 |
| r/rules/xxx                         | 规则集规则  |
| r/loggers/xxx                       | 日志记录器  |
| r/speech/xxx                        | 语音提供方  |
| r/attached-settings/xxx             | 附加设置   |
| r/profile-transfer/xxx              | 附加设置   |
| r/weather-icons/xxx                 | 天气图标   |
| r/settings/xxx                      | 天气图标   |
