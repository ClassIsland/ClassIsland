---
name: cipx-manifest
description: ClassIsland 插件清单 manifest.yml 的完整格式参考和字段说明。
---

# 插件清单结构

插件清单文件为 `manifest.yml`。它位于插件项目的根目录，用于描述插件的元数据。

## 完整格式

```yml
id: Example.Plugin # 插件 ID
name: 插件名称
description: 插件描述
entranceAssembly: "xxx.dll" # 插件入口程序集（通常不要改！）
url: https://github.com/username/repository # 插件 Url
author: 插件作者
repoOwner: github-username
repoName: github-repository
assetsRoot: master/PluginDir # 插件的资源根目录，格式为 <默认分支>/<插件项目相对存储库的路径>。
artifactName: xxx.cipx # 指定插件 Release 中给用户要下载的插件包的工件名称。通常不需要声明。
tagPattern: 1.*.** # 查找最新发行版时要匹配的 Tag 模式。通常不需要声明。
version: 1.2.3.4 # 插件版本。创建 Release 时的 Tag 必须严格遵循 a.b.c.d 的格式，否则索引生成工具可能因无法正确识别版本名而无法生成您的插件的索引。
apiVersion: 2.0.0.0 # 插件面向的 ClassIsland 版本。通常不需要更改。
supportedOSPlatforms: # 插件所支持的操作系统平台。通常不需要声明。
    - Windows
    - Linux
    - OSX # 请注意，ClassIsland 的 macOS 版本暂不支持插件。
```

## 字段说明

| 字段                   | 说明                                                        | 是否必需       |
| ---------------------- | ----------------------------------------------------------- | -------------- |
| `id`                   | 插件唯一标识符，建议使用命名空间风格（如 `Example.Plugin`） | 是             |
| `name`                 | 插件显示名称                                                | 是             |
| `description`          | 插件功能描述                                                | 是             |
| `entranceAssembly`     | 插件入口程序集文件名，通常保持默认                          | 是             |
| `url`                  | 插件项目链接                                                | 推荐           |
| `author`               | 插件作者                                                    | 推荐           |
| `repoOwner`            | GitHub 仓库所有者                                           | 发布时需要     |
| `repoName`             | GitHub 仓库名称                                             | 发布时需要     |
| `assetsRoot`           | 资源根目录，格式 `<默认分支>/<插件项目相对路径>`            | 发布时需要     |
| `artifactName`         | Release 中插件包的文件名                                    | 可选           |
| `tagPattern`           | 匹配最新发行版的 Tag 模式                                   | 可选           |
| `version`              | 插件版本号，格式 `a.b.c.d`；Release Tag 必须严格匹配        | 是             |
| `apiVersion`           | 面向的 ClassIsland 版本                                     | 通常不需要更改 |
| `supportedOSPlatforms` | 支持的操作系统平台列表                                      | 可选           |

## 校验与示例

- `version` 必须严格采用 `a.b.c.d` 四段数字格式（例如 `1.2.3.4`），发布时 Tag 名称应能被索引工具正确识别。
- `assetsRoot` 格式应为 `<默认分支>/<插件项目相对路径>`，以便索引器能定位插件资源。
- `tagPattern` 支持简单通配符匹配（例如 `1.*`），用于在自动索引或检索最新发布版本时筛选 Tag。

### 最小示例

```yml
id: Example.Plugin
name: 示例插件
description: 示例说明
entranceAssembly: "Example.Plugin.dll"
version: 1.0.0.0
apiVersion: 2.0.0.0
assetsRoot: master/Plugins/Example
supportedOSPlatforms:
	- Windows
```

## 发布注意

- 在把插件发布到仓库并生成索引前，请先确保 `repoOwner`/`repoName` 与仓库实际值一致，且 `assetsRoot` 能在默认分支下被访问。
- 建议在 CI 中运行一个 manifest 校验步骤（YAML schema 校验、版本格式校验）。

## 参考文档

- Manifest 规范（参考）： https://docs.classisland.tech/dev/plugins/manifest.html
- YAML 模式校验工具： https://json-schema.org/understanding-json-schema/
