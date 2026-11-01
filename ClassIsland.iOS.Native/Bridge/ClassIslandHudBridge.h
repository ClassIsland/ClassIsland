#ifndef CLASS_ISLAND_HUD_BRIDGE_H
#define CLASS_ISLAND_HUD_BRIDGE_H

#include <stdint.h>

#if defined(__cplusplus)
extern "C" {
#endif

#if defined(__GNUC__)
#define CI_HUD_EXPORT __attribute__((visibility("default")))
#else
#define CI_HUD_EXPORT
#endif

/*
 * ClassIsland iOS 原生 HUD 桥（TrollStore 发行版的全局悬浮主界面）。
 *
 * 两个进程：
 *   1. 宿主 App（mobile）：调用 ci_hud_start 以 root persona 重新 spawn 自身可执行文件
 *      （参数 --hud），然后通过轮询 ci_hud_is_running / ci_hud_get_pid 观察子进程。
 *   2. HUD 子进程（root，同可执行文件）：Avalonia 创建自己的 UIWindow 后，调用
 *      ci_hud_register_window 把该窗口交给 SpringBoard 的 accessibility window hosting
 *      服务托管，从而浮在其他 App 之上；注册失败原因通过 ci_hud_last_error 返回。
 *
 * 约定：
 *   - 所有函数使用 C 调用约定（iOS arm64 上即默认约定），可从任意线程调用；
 *     ci_hud_register_window 内部会切换到主线程执行 UIKit 操作。
 *   - 返回的字符串指针由桥接内部持有（不归调用方释放），内容为 UTF-8、以 NUL 结尾，
 *     在下一次 ci_hud_* 调用前有效；调用方如需保留必须立即复制。
 *   - ci_hud_last_error 永远不会返回 NULL：无错误时返回空字符串。
 *     返回值/返回码是判断成功与否的唯一依据，last_error 用于诊断。
 *   - 本头文件中的错误码必须与 ClassIslandHudBridge.swift 里的 HudError 保持一致。
 */

/* ci_hud_start 返回 0：HUD 已启动，或此前已经在运行（幂等）。 */
#define CI_HUD_OK 0
/* data_dir_utf8 为 NULL/空串，或内部参数无效。 */
#define CI_HUD_ERR_INVALID_ARGUMENT (-1)
/* 取不到 Bundle.main.executablePath，或该路径不是可执行文件。 */
#define CI_HUD_ERR_EXECUTABLE_UNAVAILABLE (-2)
/* posix_spawn 失败；ci_hud_last_error 含 errno 与 strerror 文案。 */
#define CI_HUD_ERR_SPAWN_FAILED (-3)
/* 请求 root 启动，但 persona 私有 API 不可用或被拒绝；HUD 未启动。 */
#define CI_HUD_ERR_ROOT_PERSONA_UNAVAILABLE (-4)
/* 需要终止 HUD 但当前进程无权发送信号，且 dismissal 通知未能让子进程退出。 */
#define CI_HUD_ERR_TERMINATION_FORBIDDEN (-5)
/* 其他内部错误（posix_spawnattr_init 失败等）。 */
#define CI_HUD_ERR_INTERNAL (-6)

/*
 * 启动 HUD 子进程。
 *
 * 行为：
 *   - 被 spawn 的二进制固定为当前 App 的 Bundle.main.executablePath；
 *     argv = [executablePath, "--hud"]。
 *   - 环境变量在继承当前进程环境的基础上追加：
 *       CLASSISLAND_HUD=1
 *       CLASSISLAND_HUD_DATA_DIR=<data_dir_utf8>
 *       CLASSISLAND_HUD_PARENT_PID=<当前进程 pid>
 *       CLASSISLAND_HUD_DISMISSAL_NOTIFY=<桥接内部使用的 dismissal 通知名>
 *     （最后一个变量是桥接内部约定，C# 侧无需读取。）
 *   - spawn_as_root 非 0 时使用 posix_spawnattr_set_persona_np 设置 root persona
 *     （persona 99，uid/gid 0），需要 com.apple.private.persona-mgmt entitlement；
 *     设置失败直接返回 CI_HUD_ERR_ROOT_PERSONA_UNAVAILABLE，不会退化成非 root 启动。
 *   - data_dir_utf8 指向 HUD 子进程使用的数据目录（UTF-8）。桥接会尽力创建该目录，
 *     但创建失败不算启动失败，只记录到 ci_hud_last_error。
 *   - pid 写入 NSTemporaryDirectory()/classisland-hud.pid；已存在且确实存活的 HUD
 *     会被复用（返回 0），因此该函数幂等。
 *
 * 返回：0 成功/已在运行；负数为上面的 CI_HUD_ERR_* 错误码。
 */
CI_HUD_EXPORT int32_t
ci_hud_start(int32_t spawn_as_root, const char *data_dir_utf8);

/*
 * 终止 HUD 子进程。
 *
 * 行为：
 *   - 依据 marker + kill(pid, 0)（并对自己的子进程做 waitpid 回收）判定存活；
 *     marker 指向已退出/复用的 pid 时按“没有运行”处理并清理 marker。
 *   - 先 SIGTERM；最多等待 0.5 秒仍未退出则 SIGKILL；再等待 0.2 秒。
 *   - 宿主 App 以 mobile 运行、HUD 以 root 运行时 kill 会返回 EPERM（TrollStore
 *     场景的常态）。此时桥接会 post 一个 <bundle-id>.hud.dismissal 的 darwin
 *     notification，HUD 子进程在启动早期安装的观察者收到后自行
 *     _exit(0)（与 TrollRecorder TRHud 相同的退出机制），随后再等待最多 0.6 秒。
 *   - 清理 pid marker 仅在确认终止后执行；失败时保留 marker 以便重试。
 *
 * 返回：1 = 调用后 HUD 不再运行（已终止、或确认此前已退出）；
 *       0 = 调用前就没有运行中的 HUD；
 *       负数 = 无法终止（例如 CI_HUD_ERR_TERMINATION_FORBIDDEN），详情见 last_error。
 */
CI_HUD_EXPORT int32_t
ci_hud_stop(void);

/*
 * 仅由 HUD 子进程入口调用，在 UIKit/Avalonia 初始化前安装退出通知和父进程看门狗。
 * 该函数幂等，主 App 不应调用。
 */
CI_HUD_EXPORT void
ci_hud_initialize_child_lifecycle(void);

/*
 * 由 root stop helper 调用，终止指定 HUD PID。普通 mobile 主 App 不应直接调用。
 */
CI_HUD_EXPORT int32_t
ci_hud_stop_target(int32_t pid);

/*
 * 仅由 HUD 子进程入口调用：以“手工 UIKit 启动 + __completeAndRunAsPlugin”方式
 * 启动 UIApplication，取代标准 UIApplicationMain。
 *
 * 为什么需要：HUD 子进程由主 App posix_spawn 拉起，没有 SpringBoard/launchd 提供的
 * UIApplication 启动上下文。直接 UIApplicationMain 得到的应用没有 WindowServer 连接，
 * UIWindow 拿不到 CAContext（_boundContext），因此无法注册到 SpringBoard，悬浮窗口
 * 永远不会出现。该函数按 TrollSpeed/TRHud/XExternalHUD 等已验证实现的做法初始化
 * GraphicsServices/BackBoard，使本进程可以创建真正的全局悬浮窗口。
 *
 * app_delegate 是托管侧 AppDelegate 的 NSObject 裸指针（不加引用计数）。函数内部会把
 * 它设为 UIApplication.delegate；调用方必须保证该对象在进程生命周期内不被回收。
 * 启动完成后函数进入 CFRunLoopRun，正常情况下不会返回（随进程退出结束）。
 *
 * 返回：0 = CFRunLoopRun 结束后返回；
 *       负数 = 预检查失败（缺少私有符号/选择器、参数无效）。此时尚未初始化 UIKit，
 *       调用方可以安全回退到标准 UIApplicationMain。
 */
CI_HUD_EXPORT int32_t
ci_hud_run_as_plugin(void *app_delegate);

/* 1 = HUD 子进程正在运行；0 = 没有运行。 */
CI_HUD_EXPORT int32_t
ci_hud_is_running(void);

/* 正在运行的 HUD 子进程 pid；没有运行时返回 0。 */
CI_HUD_EXPORT int32_t
ci_hud_get_pid(void);

/* 最近一次错误/警告信息；无错误时为空字符串。永不为 NULL。 */
CI_HUD_EXPORT const char *
ci_hud_last_error(void);

/*
 * 把 UIWindow* 注册为 SpringBoard 全局 accessibility 窗口（悬浮在其他 App 之上）。
 *
 * ui_window 必须是当前进程持有的 UIWindow* 裸指针（不加引用计数；桥接内部会强引用
 * 该窗口与 hosting controller，直到进程退出或再次注册别的窗口）。应在窗口已经上屏
 * （拥有 CAContext）之后调用；调用后桥接会把窗口类替换为
 * ClassIslandHudPassthroughWindow（点击穿透、非 App 托管窗口、系统窗口），
 * 设置 windowLevel = 10000001、透明背景，并把 CAContext 设为 secure，
 * 最后调用 SBSAccessibilityWindowHostingController 的
 * registerWindowWithContextID:atLevel:。
 *
 * 该函数不返回错误码；任一步失败都会写 ci_hud_last_error 并安全返回（不 crash）。
 * 同一个窗口重复调用是幂等的。
 */
CI_HUD_EXPORT void
ci_hud_register_window(void *ui_window);

#if defined(__cplusplus)
}
#endif

#endif
