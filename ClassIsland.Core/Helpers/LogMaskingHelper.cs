using ClassIsland.Core.Models.Logging;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 日志打码帮助类
/// </summary>
public static class LogMaskingHelper
{
    /// <summary>
    /// 打码规则
    /// </summary>
    public static List<LogMaskRule> Rules { get; } = [];

    /// <summary>
    /// 根据规则打码，将打码的部分替换为“***”。
    /// </summary>
    /// <param name="log">要打码的日志</param>
    /// <param name="replace">打码文本</param>
    /// <returns>打码后的日志</returns>
    public static string MaskLog(string log, string replace="***")
    {
        return Rules.Aggregate(log, (current, rule) => rule.Regex.Replace(current, match =>
        {
            if (match.Groups.Count == 1)
            {
                return match.Groups[0].Value;
            }

            List<string> parts = [];
            for (var i = 1; i < match.Groups.Count; i++)
            {
                if (i != rule.MatchIndex)
                {
                    parts.Add(match.Groups[i].Value);
                    continue;
                }

                parts.Add(replace);
            }

            return string.Join("", parts);
        }));
    }
}