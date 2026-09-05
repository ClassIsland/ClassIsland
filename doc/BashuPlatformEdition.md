# 两江巴蜀平台托管版：同步与播报修复

本分支包含 ClassIsland 桌面端修复源码，保留原项目 GPL-3.0 许可证、历史和现有发布流程。它不是已完成 Windows 真机验收的安装包。

## 本轮改动

- 整周课表按星期分别同步，支持不同星期的自定义作息和教师姓名。平台托管模式优先使用云端当天课表，避免被本地默认或临时课表覆盖。
- 平台改课、客户端档案重新加载后自动应用数据，并立即重算悬浮岛的当前课程、下一节课及倒计时。
- 对讲轮询与播放队列分离，音频按顺序播放；同一会话只提示一次。播放成功后才回执，回执重试不会重复播放。
- 通知标题与正文显示发出人；只通过原生通知语音通道朗读。滚动时长按内容长度、标点停顿、发出人和重复次数估算，不再固定为 6 秒。此为阅读时间估算，不是语音引擎实测时长。
- 绑定窗口提供立即同步、语音测试、连接错误说明，以及需要确认并持久保存的解除绑定。平台管理课表、作息和学科，本机保留外观与音量设置。

## 必须配套更新平台

本分支不能单独替换旧客户端后即宣称线上问题全部解决。平台需要同步提供：

1. `GET /api/display-client/poll` 的 `dashboard.scheduleWeek` 整周数组，包含 `weekday`（1–7）、`subject_name`、`teacher_name`、`starts_at`、`ends_at`；保留旧客户端使用的 `dashboard.schedule` 今日数组。
2. 通知 `items` 返回 `created_by_name`。无课程时返回空数组，清除客户端上一份课程。
3. 网页将独立对讲片段转换为 16 kHz、单声道、16-bit PCM WAV，上传接口接受 `audio/wav`。客户端无需额外安装 FFmpeg 或依赖系统播放器解码 WebM。

平台服务更新后，教师应刷新旧网页再发起对讲。旧网页生成的 WebM 不会被新版客户端误报为播放成功。平台后端代码不包含在本公开仓库中。

## 验证

```sh
dotnet run --project tests/BashuPlatformSmoke/BashuPlatformSmoke.csproj
```

10 项基于实际 ClassIsland 模型的回归覆盖整周分离、自定义放学时间、科目位置、教师姓名、重复同步、空课表清理和通知时长。

本次源码已在隔离构建环境通过 Windows x64 目标编译；Windows 真机实际发声、窗口显示及生产端到端联动仍需验收。

构建依赖须按 `.gitmodules` 初始化 `vendors/EdgeTtsSharp`，使用项目声明的 `classisland-v2` 版本，不能替换为 Windows-only 的原始分支。发布所需的无密钥配置按现有 `tools/release-gen/generate-secrets.ps1` 生成，不要提交 `secrets.g.cs` 或真实密钥。
