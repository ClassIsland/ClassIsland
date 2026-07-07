---
name: cipx-dev-basics
description: ClassIsland 插件基础知识：程序集隔离、资源引用、依赖注入、访问 AppBase、保存插件配置、以及插件中图标使用注意事项。
---

# ClassIsland 插件基础知识

当你开始编写 ClassIsland 插件时，先阅读并遵循这份基础知识。它总结了官方文档中最常见的约定与最佳实践，适用于插件入口、资源加载、服务注册、应用生命周期和配置持久化。

## 官方文档

- 插件基础知识：https://docs.classisland.tech/dev/plugins/basics.html
- 插件入口类：https://docs.classisland.tech/dev/plugins/plugin-base.html
- 事件与生命周期：https://docs.classisland.tech/dev/events.html

## 1. 程序集隔离

默认情况下，每个插件都运行在独立的程序集加载上下文中，因此不同插件可以使用相同依赖库的不同版本而不发生冲突。

注意事项：

- 如果插件需要调用另一个插件中的类型，通常需要先声明依赖。
- 不要假设两个插件中的同名类型是同一个类型；只有来自同一个 Assembly 实例时才相等。

## 2. 资源引用

插件内的资源必须使用绝对资源 URI 进行引用，例如：

```csharp
avares://ExamplePlugin/Assets/Image.png
```

在 XAML 或代码中引用资源时，优先使用这种形式，避免相对路径在插件加载场景下失效。

## 3. 依赖注入

插件通常在入口类的 `Initialize` 方法中注册自己的服务与设置页：

```csharp
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExamplePlugin;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSettingsPage<HelloSettingsPage>();
        services.AddSingleton<MyService>();
    }
}
```

常见做法：

- 用 `services.AddSingleton` / `services.AddScoped` 注册服务。
- 使用 `services.AddSettingsPage<T>()` 注册设置页。
- 避免在插件初始化之外手动创建大量依赖对象。

## 4. 访问 Application 对象

ClassIsland 的应用对象由 `ClassIsland.Core.AppBase` 封装。插件可以通过 `AppBase.Current` 访问当前应用实例。

```csharp
var app = AppBase.Current;

app.AppStarted += (sender, args) => Console.WriteLine("App started!");
```

也可以通过以下方式控制应用生命周期：

```csharp
var app = AppBase.Current;
app.Restart();
```

## 5. 保存插件配置

插件应将配置文件保存在插件自己的配置目录，而不是安装目录。可以通过 `PluginConfigFolder` 获取绝对路径。

```csharp
using System.IO;
using ClassIsland.Shared.Helpers;

public class Plugin : PluginBase
{
    public Settings Settings { get; set; } = new();

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        Settings = ConfigureFileHelper.LoadConfig<Settings>(Path.Combine(PluginConfigFolder, "Settings.json"));
        Settings.PropertyChanged += (sender, args) =>
        {
            ConfigureFileHelper.SaveConfig(Path.Combine(PluginConfigFolder, "Settings.json"), Settings);
        };
    }
}
```

重要提醒：

- 不要把配置文件写入插件安装目录，因为更新插件时可能被覆盖或删除。
- 配置目录更适合保存 JSON、数据库文件和插件状态。

## 6. 图标使用与修复

当插件中出现图标显示为空白、错误、或使用了错误码点时，请优先引用专用图标修复 skill：

- [classisland-icons-fix](../classisland-icons-fix/SKILL.md) — 处理 Fluent / Lucide 图标码点修复、语义替换、XAML 与 C# 代码中的图标问题。

这类问题通常出现在以下场景：

- 设置页中的图标显示异常；
- 需要把随机旧码点替换为语义正确的图标；
- 插件中使用了 Lucide/Fluent 图标表达式，但没有正确加载；
- 通知提供程序或渠道信息中的图标无法显示。

遇到这些情况时，直接引用上述 skill 即可，而不需要在本 skill 中重复展开完整修复流程。

## 7. 典型开发顺序

1. 确定插件需要暴露什么能力：设置页、服务、通知、URI 导航、事件订阅等。
2. 在插件入口类中注册依赖、设置页与必要服务。
3. 使用 `avares://` 资源 URI 加载插件内资源。
4. 使用 `PluginConfigFolder` 保存插件状态与配置。
5. 通过 `AppBase.Current` 监听应用事件或控制应用生命周期。
6. 在界面中使用图标时，优先确认码点和图标库是否正确。

## 推荐检查与 CI

- 本地快速检查：

```pwsh
dotnet restore
dotnet build -c Release
```

- 若插件包含单元测试，请在 CI 中运行 `dotnet test`。
- 建议在 CI 中包含：还原、构建、单元测试、生成 `.cipx`（`dotnet publish -p:CreateCipx=true`）并上传产物。

### GitHub Actions 简要示例

```yaml
jobs:
    build:
        runs-on: windows-latest
        steps:
            - uses: actions/checkout@v4
            - uses: actions/setup-dotnet@v4
                with:
                    dotnet-version: '9.0.x'
            - run: dotnet restore
            - run: dotnet build --configuration Release
            - run: dotnet test --no-build --configuration Release || echo "No tests"
            - run: dotnet publish -p:CreateCipx=true
            - name: Upload cipx
                uses: actions/upload-artifact@v4
                with:
                    name: cipx-artifacts
                    path: ./cipx
```

## 参考文档与 API

- 插件开发主页：https://docs.classisland.tech/dev/plugins/
- API 参考：https://api.docs.classisland.tech/
