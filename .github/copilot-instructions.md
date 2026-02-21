# Copilot / AI 代理说明 — ClassIsland

目的：快速让 AI 代码助手在此仓库中立即高效工作。说明基于仓库可发现的结构、构建任务和关键集成点。

- 项目概览
  - 解决方案文件：ClassIsland.sln。主要项目：`ClassIsland`（Avalonia UI 应用）、`ClassIsland.Core`（共享库）、`ClassIsland.Desktop`、`ClassIsland.PluginSdk`、`ClassIsland.ExamplePlugin`。
  - UI 使用 Avalonia：界面入口位于 `ClassIsland/App.axaml` 与 `ClassIsland/App.axaml.cs`，主窗口为 `ClassIsland/MainWindow.axaml`。
  - 插件与扩展：插件加载逻辑在 `ClassIsland/PluginLoadContext.cs`；插件清单模型在 `ClassIsland.Core/Models/Plugin/PluginManifest.cs`（默认 `Readme = "README.md"`）。
  - 进程间通信：仓库包含 `ClassIsland.Shared.IPC`，用于跨组件/插件的 IPC/消息交换。

- 构建 / 运行 / 发布（必须精确）
  - 构建工程：`dotnet build ClassIsland/ClassIsland.csproj`
  - 运行（watch）：`dotnet watch run --project ClassIsland/ClassIsland.csproj`
  - 发布：`dotnet publish ClassIsland/ClassIsland.csproj`
  - VS Code / 任务：仓库任务已定义（`build`、`publish`、`watch` 使用 `dotnet`），在工作区任务配置中可直接运行。

- 代码约定与模式（可从代码中直接观察到）
  - 保持 API 稳定：`ClassIsland.Core` 提供公共契约，尽量不改变公共类型签名。
  - XAML/VM 模式：视图放在 `Views/`，对应 `ViewModels/`；控件在 `Controls/`。遵循现有命名与目录布局。
  - 共享 usings：使用项目级全局 using，参见 `ClassIsland/GlobalUsing.cs`。
  - 资源与主题：Assets、Themes、XamlThemes 目录分别用于静态资源与主题样式。

- 插件开发相关（示例与注意点）
  - 插件示例：`ClassIsland.ExamplePlugin` 展示插件结构。插件清单（manifest）会引用 `README.md`。修改插件时保持清单字段兼容。
  - 加载：`PluginLoadContext.cs` 实现自定义 AssemblyLoadContext，避免直接修改加载逻辑，优先在插件级处理兼容性。

- 跨组件集成点（搜索与修改时优先查看）
  - 插件接口：`ClassIsland.PluginSdk` 与 `ClassIsland.Core/Models/Plugin`。
  - IPC 入口：`ClassIsland.Shared.IPC`。
  - 程序入口：`ClassIsland/Program.cs`、`ClassIsland/App.axaml.cs`。

- 提交、变更建议与审查要点
  - 避免改动公共库 API；若必须变更请同步更新 `ClassIsland.PluginSdk` 与示例插件。
  - UI 改动：保留 XAML 命名/绑定约定，更新 ViewModels 时同时调整绑定属性。

- 快速示例（修改插件 README 的常见任务）
  1. 在 `ClassIsland.ExamplePlugin` 中修改 `README.md`（插件描述）。
  2. 确认 `PluginManifest`（`ClassIsland.Core/Models/Plugin/PluginManifest.cs`）的 `Readme` 字段未被破坏。

如果有需要，我可以把本文件合并进仓库（已准备好写入）。请告诉我是否需要补充特定文件的示例或把某些路径换成可点击链接。
