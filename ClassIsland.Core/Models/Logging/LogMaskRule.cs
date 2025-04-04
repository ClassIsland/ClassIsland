using System.Text.RegularExpressions;

namespace ClassIsland.Core.Models.Logging;

/// <summary>
/// 日志打码规则
/// </summary>
/// <param name="Regex">要匹配的正则表达式</param>
/// <param name="MatchIndex">要打码的部分的匹配索引</param>
public record LogMaskRule(Regex Regex, int MatchIndex);