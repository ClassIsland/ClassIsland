# ClassIsland Flatpak 打包支持

此目录包含 ClassIsland 的 Flatpak 构建清单和构建脚本。构建结果是一个可以交给 `flatpak install` 安装的 `ClassIsland.flatpak` bundle。

## 构建前准备

构建脚本只检查环境，不会自动添加远程仓库、安装 SDK 或安装应用。请先手动准备：

- Linux 系统，以及 `flatpak`、`flatpak-builder` 和 Python 3；
- 已配置名为 `flathub` 的 Flatpak remote；
- `org.freedesktop.Sdk//24.08`；
- `org.freedesktop.Sdk.Extension.dotnet9//24.08`；构建时会从生成的 NuGet 源清单中提取应用需要的 .NET 8 runtime 文件；
- 网络连接，用于恢复 NuGet 依赖和下载缺少的构建工具。

例如，可以使用以下命令准备 Flathub 和 SDK（命令由用户主动执行）：

```bash
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
flatpak install flathub \
  org.freedesktop.Sdk//24.08 \
  org.freedesktop.Sdk.Extension.dotnet9//24.08
```

## 构建

从仓库的任意目录运行：

```bash
./tools/flatpak/build-flatpak.sh
```

脚本会根据当前机器架构生成 NuGet 离线源清单，使用 `org.classisland.ClassIsland.json` 构建应用，并在 `tools/flatpak/ClassIsland.flatpak` 写出 bundle。应用以 framework-dependent 方式发布，bundle 中单独放置 .NET 8 runtime；启动入口仍然是 `run.sh`，它会调用 `/app/bin/ClassIsland.Desktop.dll`。构建失败时会保留原有的 `sources.json`，避免用不完整的依赖清单覆盖上一次成功的结果。

构建完成后可以自行安装或运行：

```bash
flatpak install --user ./tools/flatpak/ClassIsland.flatpak
flatpak run org.classisland.ClassIsland
```

如果经常构建，请留意自动生成的 `.flatpak-builder` 缓存大小。构建清单还会安装以下 bundle 元数据，使应用能出现在桌面环境的应用菜单中：

- `org.classisland.ClassIsland.desktop`；
- `org.classisland.ClassIsland.metainfo.xml`；
- `org.classisland.ClassIsland` 的 128×128 图标。
