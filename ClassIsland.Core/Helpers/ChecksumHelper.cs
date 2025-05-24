using ClassIsland.Core.Exceptions;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 校验和相关工具。
/// </summary>
public static class ChecksumHelper
{
    /// <summary>
    /// 从发行日志中提取校验和。<br/>
    ///
    /// 可以被此方法提取的校验和信息字典（键为文件名，值为MD5校验和）包裹在以 CLASSISLAND_PKG_MD5 开头的注释标签中，并以 json 格式存储，例如：<br/>
    /// 
    /// &lt;!-- CLASSISLAND_PKG_MD5 {"ClassIsland.zip":"5D413B24956724B004B1069C0AA6B3DE"} --&gt;
    /// </summary>
    /// <param name="releaseNote">要提取的发行日志</param>
    /// <param name="artifactKey">要提取的文件名</param>
    /// <returns>提取的MD5校验和（十六进制）</returns>
    public static string ExtractHashInfo(string releaseNote, string artifactKey)
    {
        var regex = new Regex(@"<!-- CLASSISLAND_PKG_MD5 (.+?) -->");
        var match = regex.Match(releaseNote);
        if (!match.Success)
        {
            return "";
        }

        var json = match.Groups[1].Value;
        try
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return d![artifactKey];
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    /// <summary>
    /// 检查文件是否符合指定的 MD5 校验和。如果校验通过，则返回 true。
    /// </summary>
    /// <param name="filePath">要检查的文件</param>
    /// <param name="checksum">MD5校验和（十六进制）</param>
    /// <returns>校验结果</returns>
    public static bool CheckChecksum(string filePath, string checksum)
    {
        var stream = File.OpenRead(filePath);
        var md5 = MD5.HashData(stream);
        var md5Hex = Convert.ToHexString(md5);
        stream.Close();
        return string.Compare(checksum, md5Hex, StringComparison.CurrentCultureIgnoreCase) == 0;
    }

    /// <summary>
    /// 检查文件是否符合指定的 MD5 校验和。如果校验不通过，则抛出 <see cref="ChecksumUnMatchException"/> 异常。
    /// </summary>
    /// <param name="filePath">要检查的文件</param>
    /// <param name="checksum">MD5校验和（十六进制）</param>
    /// <exception cref="ChecksumUnMatchException">校验和校验不通过时抛出此异常。</exception>
    public static void VerifyChecksum(string filePath, string checksum)
    {
        if (!CheckChecksum(filePath, checksum))
        {
            throw new ChecksumUnMatchException($"文件 {filePath} 校验和不匹配。");
        }
    }
}