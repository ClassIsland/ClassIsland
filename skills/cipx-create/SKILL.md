---
name: cipx-create
description: 创建 ClassIsland 插件项目：使用 cipx-template 脚手架生成插件项目结构。
---

# 创建插件

## 前置条件

首先，确保你已配置开发环境。参见 `cipx-dev-env` skill。

## 创建项目

使用以下命令创建插件：

```pwsh
# MyPlugin 是您的插件项目名称
mkdir MyPlugin
cd MyPlugin
dotnet new cipx-template -n MyPlugin
```

这将会生成一个完整的插件项目结构，包含必要的代码文件、项目文件和清单文件。

## 创建后检查项

- 确认 `manifest.yml` 已生成并填写 `id`、`name`、`version`、`assetsRoot` 等必需字段。
- 在项目根执行一次构建和发布生成 `.cipx` 以确保模板可用：

```pwsh
dotnet restore
dotnet build
dotnet publish -p:CreateCipx=true
```

- 项目命名建议：类库程序集名与 `manifest.yml` 中的 `entranceAssembly` 保持一致，资源引用建议使用 `avares://` 前缀。

## 示例与参考

- 创建插件的官方说明： https://docs.classisland.tech/dev/plugins/create.html
- 插件模板包（示例）： ClassIsland.PluginTemplate.Packaging
