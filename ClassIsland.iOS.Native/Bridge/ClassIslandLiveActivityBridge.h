#ifndef CLASS_ISLAND_LIVE_ACTIVITY_BRIDGE_H
#define CLASS_ISLAND_LIVE_ACTIVITY_BRIDGE_H

#include <stdint.h>

#if defined(__cplusplus)
extern "C" {
#endif

#if defined(__GNUC__)
#define CI_LIVE_ACTIVITY_EXPORT __attribute__((visibility("default")))
#else
#define CI_LIVE_ACTIVITY_EXPORT
#endif

/// 可用性：0 = Unsupported，1 = Disabled，2 = Available。
typedef int32_t ci_live_activity_availability_t;

/// 结果：0 = Succeeded，1 = Unsupported，2 = Disabled，3 = InvalidContent，
/// 4 = NativeFailure，5 = Cancelled（预留给托管层取消等待）。
typedef int32_t ci_live_activity_result_t;

/// completion 在异步操作结束时恰好调用一次。
/// 字符串只在回调执行期间有效，调用方如需保留必须立即复制。
typedef void (*ci_live_activity_completion_t)(
    void *context,
    ci_live_activity_result_t result_code,
    const char *activity_id,
    const char *error_message);

/// 同步查询 ActivityKit 是否可用。
CI_LIVE_ACTIVITY_EXPORT ci_live_activity_availability_t
ci_live_activity_get_availability(void);

/// 发布或更新实时活动。JSON 必须是 UTF-8、以 NUL 结尾，schema 见共享 Swift 类型。
CI_LIVE_ACTIVITY_EXPORT void
ci_live_activity_publish_json(
    const char *json_utf8,
    ci_live_activity_completion_t completion,
    void *context);

/// 结束当前实时活动。dismissal_policy：0 = Default，1 = Immediate。
CI_LIVE_ACTIVITY_EXPORT void
ci_live_activity_end(
    int32_t dismissal_policy,
    ci_live_activity_completion_t completion,
    void *context);

#if defined(__cplusplus)
}
#endif

#endif
