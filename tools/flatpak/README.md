# ClassIsland Flatpak 打包支持
为改善ClassIsland在Linux平台的分发体验、减小打包维护成本，在此文件夹中引入了对Flatpak打包相关支持。

文件结构如下：

```
flatpak/
├── build.sh # 自动化打包脚本
├── com.classisland.ClassIsland.json # Flatpak打包配置文件
├── run.sh # 在Flatpak包中引导应用程序启动的脚本
```
## 构建

只需运行`build.sh`脚本即可自动化完成Flatpak包的构建过程。该脚本需要:
- Linux系统
- Flatpak工具链（包括`flatpak-builder`和`flatpak`命令行工具）
- Python 3环境
- 网络连接

脚本将输出一个Flatpak包文件，可以直接安装。
## TO-DO:
- [ ] 完善`com.classisland.ClassIsland.json`中的依赖和权限配置
- [ ] 改善应用程序与Flatpak打包的兼容性