# ClassIsland Flatpak 打包支持

为改善ClassIsland在Linux平台的分发体验、减小打包维护成本，在此文件夹中引入了对Flatpak打包相关支持。

文件结构如下：

```
tools/flatpak/
├── build-flatpak.sh # 自动化打包脚本
├── com.classisland.ClassIsland.json # Flatpak打包配置文件
├── run.sh # 在Flatpak包中引导应用程序启动的脚本
```

## 构建

只需运行`build-flatpak.sh`脚本即可自动化完成Flatpak包的构建过程。该脚本需要:

- Linux系统
- Flatpak工具链（包括`flatpak-builder`和`flatpak`命令行工具）
- Python 3环境
- 网络连接

请注意：如果经常运行`build-flatpak.sh`脚本的话，请注意自动生成的`.flatpak-builder`文件夹的大小。

脚本将输出一个Flatpak包文件，可以直接安装。

## TO-DO

- [ ] 完善`com.classisland.ClassIsland.json`中的依赖和权限配置
  - [ ] 将session-bus权限细化，避免权限过宽
- [ ] 改善应用程序与Flatpak打包的兼容性
  - [ ] 调查应用程序在正常关闭时出现的未捕获异常
