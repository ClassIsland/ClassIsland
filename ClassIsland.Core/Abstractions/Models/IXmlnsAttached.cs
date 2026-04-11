namespace ClassIsland.Core.Abstractions.Models;

/// <summary>
/// 代表一个附加了 XML 命名空间信息的对象
/// </summary>
public interface IXmlnsAttached
{
    /// <summary>
    /// 附加的 XML 命名空间
    /// </summary>
    public IDictionary<string, string> Xmlns { get; }

    /// <summary>
    /// 将指定的 <see cref="IXmlnsAttached"/> 中的命名空间依次合并到一起，后面的元素可以覆盖前面的命名空间。
    /// </summary>
    /// <param name="attached">要合并的 <see cref="IXmlnsAttached"/></param>
    /// <returns>合并的 XML 命名空间字典</returns>
    public static Dictionary<string, string> Combine(IList<IXmlnsAttached> attached)
    {
        var xmlns = new Dictionary<string, string>();
        foreach (var i in attached)
        {
            foreach (var (key, value) in i.Xmlns)
            {
                xmlns[key] = value;
            }
        }

        return xmlns;
    }
}