# ClassIsland

![软件截图-主界面](https://github.com/HelloWRC/ClassIsland/assets/55006226/65e2bdba-be83-444c-b42f-b893aaace9c3)

![GitHub Repo stars](https://img.shields.io/github/stars/HelloWRC/ClassIsland)
[![GitHub License](https://img.shields.io/github/license/HelloWRC/ClassIsland?style=flat-square)](LICENSE.txt)
[![AppVeyor Build](https://img.shields.io/appveyor/build/HelloWRC/classisland-yw873?style=flat-square&logo=appveyor)](https://ci.appveyor.com/project/HelloWRC/classisland-yw873) 
[![GitHub Release](https://img.shields.io/github/v/release/HelloWRC/ClassIsland?style=flat-square&logo=GitHub&color=%233fb950)](https://github.com/HelloWRC/ClassIsland/releases/latest) 
[![GitHub Release](https://img.shields.io/github/v/release/HelloWRC/ClassIsland?include_prereleases&style=flat-square&logo=GitHub&label=BETA)](https://github.com/HelloWRC/ClassIsland/releases/)
[![加入QQ群](https://img.shields.io/badge/QQ%E7%BE%A4-958840932-%230066cc?style=flat-square&logo=TencentQQ)](https://qm.qq.com/q/4NsDQKiAuQ) 

ClassIsland是一款适用于班级多媒体屏幕的课表的信息显示工具，可以一目了然地显示各种信息。本应用的名字灵感源于 iOS 灵动岛（Dynamic Island）功能。


[![官网](https://img.shields.io/badge/%E7%82%B9%E6%88%91%E6%89%93%E5%BC%80-%E5%AE%98%E7%BD%91-%114514cc?style=flat-square)](https://classisland.tech)

## 功能
### 课表显示
- [X] 显示当天的课表
- [X] 显示目前进行的课程信息
- [X] 在上下课、放学等重要时间点发出提醒
- [X] 在发出提醒时使用语音、强调特效和音效增强提醒
### 课表编辑与管理
- [X] 简洁直观的课表编辑工具
- [X] 从表格导入课表
- [X] 临时与永久换课
- [X] 临时启用某一个课表
### 其它功能
- [X] 课表界面缩放，不透明度调节
- [X] 自动同步软件时间，支持手动调整和自动累加时间偏移
- [X] 天气、极端天气预警
- [X] 快速显示（日期时间,考试倒计时,天气简报）
- [X] 从壁纸提取软件主题色
- [X] 丝滑、流畅的过渡动画
- [X] 明亮、黑暗主题
- [X] 软件更新
- [ ] 集控管理 _(即将发布)_
- [ ] ……

<details>
<summary><strong>🖼️软件截图</strong></summary>

>  背景图片来自[Pixiv@辰暮sora](https://www.pixiv.net/artworks/110847880)  

### 主界面

##### 1.软件效果
![软件截图-整体效果](https://github.com/HelloWRC/ClassIsland/assets/55006226/784a2f8c-a9e2-4656-b66d-9f8105f0600c)
##### 2.软件主页面
![软件截图-主界面](https://github.com/HelloWRC/ClassIsland/assets/55006226/5299c6e5-5668-43ed-af27-9abe027411a0)
##### 3.演示上课提醒视频
<video src="https://github.com/HelloWRC/ClassIsland/assets/55006226/b797138a-84ef-4296-b69b-3989f331f289" loop label="软件截图-上课提醒" autoplay muted></video>

### 档案编辑器

##### 1.课表编辑
![软件截图-课表编辑](https://github.com/HelloWRC/ClassIsland/assets/55006226/29d91bf2-4c8a-4cbd-a778-a9034e7d7420)
##### 2.时间表编辑
![软件截图-时间表编辑](https://github.com/HelloWRC/ClassIsland/assets/55006226/2b3b5c87-c8bb-46f0-8470-01edf3ca52a2)
##### 3.科目编辑
![软件截图-科目编辑](https://github.com/HelloWRC/ClassIsland/assets/55006226/a2e64983-dfa0-4565-a45a-31c9f9c298a8)

### 设置界面

![软件截图-应用设置](https://github.com/HelloWRC/ClassIsland/assets/55006226/063123a1-1bf2-4b41-bef7-1dc731631d08)


</details>

## 开始使用

### 1. 使用此软件需求

首先，请确保您的设备满足以下推荐需求：
- Windows 10 及以上版本的系统，x64架构
- 已安装[.NET 8.0桌面运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.1-windows-x64-installer)
- <div style="opacity: 0.5">开启Aero效果（Windows 8及以上的系统可以忽略此项）</div>

<details>
<summary>最低设备需求</summary>

ClassIsland 理论上可以在以下的系统环境中运行：

- Windows 7 及以上版本系统，x64架构

**注意：在这些系统上运行 ClassIsland 可能会出现运行不稳定的情况。**

> ⚠️**不建议在 Windows 10 以下的系统运行本应用。** 在 Windows 7 中，.NET 运行时会产生**严重的内存泄漏问题**（[#91](https://github.com/HelloWRC/ClassIsland/issues/91)），需要手动进行修复。

要在 Windows 7 中安装并运行 ClassIsland，并修复内存泄漏问题，您还需要额外进行以下准备工作：
[在Windows7中安装 ClassIsland](doc/InstallOnLegaceyOS.md)

</details>

### 2. 下载软件压缩包

对于普通用户，可以在以下渠道下载到本软件，请根据自身网络环境选择合适的渠道。

> 测试版包含最新的功能，但也可能包含未完善和不稳定的功能，请谨慎使用。<br/>
> 每日构建包含最新的未经测试的功能，可能出现软件运行不稳定的问题，请谨慎使用。

| 下载渠道/通道 | **🚀正式版** <br/>[![GitHub Release](https://img.shields.io/github/v/release/HelloWRC/ClassIsland?style=flat-square&logo=GitHub&color=%233fb950)](https://github.com/HelloWRC/ClassIsland/releases/latest)  | 🚧测试版<br/>[![GitHub Release](https://img.shields.io/github/v/release/HelloWRC/ClassIsland?include_prereleases&style=flat-square&logo=GitHub&label=BETA)](https://github.com/HelloWRC/ClassIsland/releases/) | 📅每日构建<br/>[![GitHub Release](https://img.shields.io/github/v/release/ClassIsland/ClassIsland_DailyBuild?include_prereleases&style=flat&logo=github&label=Daily%20Build)](https://github.com/ClassIsland/ClassIsland_DailyBuild/releases) |
| -- | -- | -- | -- |
| GitHub | [**GitHub下载**](https://github.com/HelloWRC/ClassIsland/releases/latest) | [GitHub下载](https://github.com/HelloWRC/ClassIsland/releases) | [GitHub下载](https://github.com/ClassIsland/ClassIsland_DailyBuild/releases) |
| AppCenter | [**AppCenter下载**](https://install.appcenter.ms/users/hellowrc/apps/classisland/distribution_groups/public/releases/latest) | [AppCenter下载](https://install.appcenter.ms/users/hellowrc/apps/classisland/distribution_groups/publicbeta/releases/latest) | 🚫无 |

<!-- > GitHub Releases 还没有同步历史版本。要下载历史版本，请前往[AppCenter](https://install.appcenter.ms/users/hellowrc/apps/classisland/distribution_groups/public/releases/latest)。 -->

### 3. 解压软件

下载完成后，将软件压缩包解压到一个您喜欢的地方，运行软件即可开始使用。

### 4. 开始使用

首次启动时，会有一个简短的欢迎向导来引导您完成本软件的基本设置，并展示软件的一些基本操作。

#### 您也可以观看[![入门教程视频](https://img.shields.io/badge/ClassIsland%E5%85%A5%E9%97%A8%E8%A7%86%E9%A2%91-250067cc?style=flat-square&logo=bilibili)](https://www.bilibili.com/video/BV1fA4m1A7uZ/)来快速上手本软件。如果您要进一步了解本软件，您可以阅读本软件内置的帮助文档。


## 开发

本项目目前开发状态：

- 正在[`dev`](https://github.com/HelloWRC/ClassIsland/tree/dev)分支上开发版本 [1.5 - Griseo](https://github.com/HelloWRC/ClassIsland/milestone/6)。
- 正在[`master`](https://github.com/HelloWRC/ClassIsland/tree/master)分支上维护版本 [1.4 - Firefly](https://github.com/HelloWRC/ClassIsland/milestone/5)。

要在本地编译应用，您需要安装以下负载和工具：
- [.NET 8.0 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
- [Visual Studio](https://visualstudio.microsoft.com/)

对于Visual Studio，您需要在安装时勾选以下工作负载：
- .NET 桌面开发


如果您有意愿为 ClassIsland 做出代码贡献，请先阅读[贡献指南](CONTRIBUTING.md)来了解如何为 ClassIsland 做代码贡献。我们欢迎想要为本应用实现新功能或进行改进的同学提交 [Pull Request](https://github.com/HelloWRC/ClassIsland/pulls)。

## 获取帮助&加入社区

[![查看Issues](https://img.shields.io/github/issues-search/ClassIsland/ClassIsland?query=is%3Aopen&style=flat-square&logo=github&label=Opened%20issues&color=%233fb950)]() [![GitHub Discussions](https://img.shields.io/github/discussions/HelloWRC/ClassIsland?style=flat-square&logo=Github)](https://github.com/HelloWRC/ClassIsland/discussions) [![加入QQ群](https://img.shields.io/badge/QQ%E7%BE%A4-958840932-%230066cc?style=flat-square&logo=TencentQQ)](https://qm.qq.com/q/4NsDQKiAuQ) 

您可以访问以下页面来获取**帮助**：

- 应用内置的帮助文档
- [技术性文档](https://classisland-docs.readthedocs.io/)

- 您也可以加入上方的社区获取帮助

如果您确定您遇到的问题是一个 **BUG**，或者您要提出一项**新的功能**，请[提交一个Issue](https://github.com/HelloWRC/ClassIsland/issues/new/choose)。

## 相关项目

- [ClassIsland/ManagementServer](https://github.com/ClassIsland/ManagementServer) - **ClassIsland 集控服务端仓库**
- [ClassIsland/classisland-docs](https://github.com/ClassIsland/classisland-docs) - **ClassIsland 文档仓库**


## 致谢

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-11-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

本项目受到[DuguSand/class_form](https://github.com/DuguSand/class_form)的启发而开发。

感谢以下同学为本项目为本项目的开发提供支持([emoji key](https://allcontributors.org/docs/en/emoji-key))：


<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/HelloWRC"><img src="https://avatars.githubusercontent.com/u/55006226?v=4?s=100" width="100px;" alt="HelloWRC"/><br /><sub><b>HelloWRC</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=HelloWRC" title="Code">💻</a> <a href="#design-HelloWRC" title="Design">🎨</a> <a href="https://github.com/ClassIsland/ClassIsland/commits?author=HelloWRC" title="Documentation">📖</a> <a href="#ideas-HelloWRC" title="Ideas, Planning, & Feedback">🤔</a> <a href="#maintenance-HelloWRC" title="Maintenance">🚧</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Doctor-yoi"><img src="https://avatars.githubusercontent.com/u/106463935?v=4?s=100" width="100px;" alt="Doctor-yoi"/><br /><sub><b>Doctor-yoi</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=Doctor-yoi" title="Code">💻</a> <a href="#question-Doctor-yoi" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.jiangyin14.top/"><img src="https://avatars.githubusercontent.com/u/106649516?v=4?s=100" width="100px;" alt="姜胤"/><br /><sub><b>姜胤</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=jiangyin14" title="Documentation">📖</a> <a href="#ideas-jiangyin14" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/ClassIsland/ClassIsland/issues?q=author%3Ajiangyin14" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://fossa.com/"><img src="https://avatars.githubusercontent.com/u/29791463?v=4?s=100" width="100px;" alt="fossabot"/><br /><sub><b>fossabot</b></sub></a><br /><a href="#infra-fossabot" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://allcontributors.org/"><img src="https://avatars.githubusercontent.com/u/46410174?v=4?s=100" width="100px;" alt="All Contributors"/><br /><sub><b>All Contributors</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=all-contributors" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/TV-ZHU"><img src="https://avatars.githubusercontent.com/u/88492699?v=4?s=100" width="100px;" alt="DSZDev"/><br /><sub><b>DSZDev</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=TV-ZHU" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/LiuYan-xwx"><img src="https://avatars.githubusercontent.com/u/66517348?v=4?s=100" width="100px;" alt="流焰xwx"/><br /><sub><b>流焰xwx</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=LiuYan-xwx" title="Documentation">📖</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/TuanZiGit"><img src="https://avatars.githubusercontent.com/u/46892455?v=4?s=100" width="100px;" alt="团子"/><br /><sub><b>团子</b></sub></a><br /><a href="#example-TuanZiGit" title="Examples">💡</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.gusui.site/"><img src="https://avatars.githubusercontent.com/u/170245818?v=4?s=100" width="100px;" alt="吕璟辰"/><br /><sub><b>吕璟辰</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=GusuiCommunity" title="Documentation">📖</a> <a href="#promotion-GusuiCommunity" title="Promotion">📣</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jizilin6732"><img src="https://avatars.githubusercontent.com/u/162853646?v=4?s=100" width="100px;" alt="jizilin6732"/><br /><sub><b>jizilin6732</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=jizilin6732" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.khyan.top/"><img src="https://avatars.githubusercontent.com/u/56215525?v=4?s=100" width="100px;" alt="clover_yan"/><br /><sub><b>clover_yan</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=clover-yan" title="Code">💻</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->


本项目使用到的第三方库和框架：
- [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/)
- [ReoGrid](https://github.com/unvell/ReoGrid)
- [EPPlus](https://epplussoftware.com/)
- [NAudio](https://github.com/naudio/NAudio)
- [Grpc.Tools](https://github.com/grpc/grpc)
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
- [FluentWpf](https://github.com/sourcechord/FluentWPF)
- [gong-wpf-dragdrop](https://github.com/punker76/gong-wpf-dragdrop)
- [MdXaml](https://github.com/whistyun/MdXaml)
- [Microsoft.AppCenter](https://aka.ms/telgml)
- [Downloader](https://github.com/bezzad/Downloader)
- [HarmonyOS Sans](https://developer.harmonyos.com/cn/design/resource)
- [MVVM Toolkit](https://github.com/CommunityToolkit/dotnet)
- [WPF](https://github.com/dotnet/Wpf)
- [.NET](https://github.com/microsoft/dotnet)

详细的致谢信息请前往【应用设置】->【关于】界面中查看。


## 许可证


[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FHelloWRC%2FClassIsland.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FHelloWRC%2FClassIsland?ref=badge_shield&style=flat-square) 

本项目基于 [MIT License](LICENSE.txt) 获得许可。

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FHelloWRC%2FClassIsland.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FHelloWRC%2FClassIsland?ref=badge_large)
