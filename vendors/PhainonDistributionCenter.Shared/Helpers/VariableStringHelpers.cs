using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Helpers;

/// <summary>
/// 含变量字符串辅助类
/// </summary>
public class VariableStringHelpers
{
    /// <summary>
    /// 包含变量的字符串中的变量模板替换为变量字典中的值。
    /// </summary>
    /// <param name="str">输入字符串</param>
    /// <param name="variables">变量字典</param>
    /// <returns>展开的字符串</returns>
    public static string ExpandString(string str, Dictionary<string, string> variables)
    {
        var result = str;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"$({key})", value);
        }

        return result;
    }
}