# ClassIsland.Platforms.HarmonyOS

ClassIsland 的 HarmonyOS 平台适配层。

## 概述

本项目为 ClassIsland 提供 HarmonyOS 平台的特定功能实现，包括：

- 桌面服务（自启动、URL协议注册）
- 窗口平台服务（窗口管理、前台窗口检测等）
- 位置服务（地理位置获取）

## 当前状态

⚠️ **开发中** - 此平台适配目前处于早期开发阶段

由于 HarmonyOS NEXT 是一个相对较新的平台，且 .NET 官方尚未正式支持 HarmonyOS，本适配层目前包含的是接口实现的框架代码。

## 实现状态

### ✅ 已完成
- [x] 项目结构搭建
- [x] 服务接口实现框架
- [x] 构建系统集成
- [x] CI/CD 配置

### 🚧 待实现
- [ ] HarmonyOS 系统 API 集成
- [ ] 自启动功能实现
- [ ] URL 协议注册
- [ ] 窗口管理功能
- [ ] 位置服务实现
- [ ] 平台特定的 UI 适配

## 技术说明

### 依赖要求
- .NET 8.0
- HarmonyOS NEXT SDK（待官方支持）

### 架构设计
本适配层遵循 ClassIsland 的平台抽象设计模式：
- 实现 `IDesktopService` 接口
- 实现 `IWindowPlatformService` 接口  
- 实现 `ILocationService` 接口

### 构建配置
使用条件编译符号 `Platforms_HarmonyOS` 来启用 HarmonyOS 特定的代码路径。

## 开发计划

1. **第一阶段**：基础框架搭建 ✅
2. **第二阶段**：等待 .NET 官方 HarmonyOS 支持
3. **第三阶段**：实现核心平台功能
4. **第四阶段**：优化和测试

## 贡献

如果您有 HarmonyOS 开发经验并希望参与此平台适配的开发，欢迎提交 Pull Request 或创建 Issue。

## 许可证

本项目遵循与 ClassIsland 主项目相同的许可证。
