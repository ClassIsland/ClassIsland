---
name: cipx-core
description: 开发 ClassIsland 插件所需的一切。在提到 ClassIsland 插件开发时，总是先激活此 Skill。
---

# ClassIsland 插件开发

此 Skill 是 ClassIsland 插件开发的总入口。具体内容已拆分到以下子 skill 中：

| Skill             | 说明                                                              |
| ----------------- | ----------------------------------------------------------------- |
| `cipx-dev-env`    | 配置开发环境（系统要求、安装软件、环境变量、项目模板）            |
| `cipx-dev-basics` | 插件基础知识（程序集隔离、资源引用、依赖注入、AppBase、配置保存） |
| `cipx-create`     | 创建插件项目（使用 cipx-template 脚手架）                         |
| `cipx-manifest`   | 插件清单 `manifest.yml` 完整格式参考                              |
| `cipx-pack`       | 打包插件为 `.cipx` 格式                                           |

## 资源

### API 参考

https://api.docs.classisland.tech/

### 官方文档

- 插件章节：https://docs.classisland.tech/dev/plugins/
- 总开发文档（包括本体开发）：https://docs.classisland.tech/dev/

## 快速检查清单

- 已安装正确的 .NET SDK（参见仓库根 `global.json`）。
- `manifest.yml` 格式正确且包含必需字段：`id`、`version`、`assetsRoot`。
- 资源引用使用 `avares://` 绝对路径。
- 在 CI 中包含构建与 `.cipx` 生成步骤。

## 使用建议

- 在需要配置环境时调用 `cipx-dev-env`。
- 在需要模板或初始化项目时调用 `cipx-create`。
- 在需要校验 manifest/发布包时调用 `cipx-manifest` 与 `cipx-pack`。

## 参考文档

- 官方插件总览：https://docs.classisland.tech/dev/plugins/
- API 参考：https://api.docs.classisland.tech/
