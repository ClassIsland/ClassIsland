using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace ClassIsland.Styling;

/// <summary>
/// 包裹应用级全局样式树的 <see cref="Styles"/> 容器，缓存整棵样式树的资源查找结果（命中与未命中），
/// 避免 Popup 打开等场景反复线性扫描全局 Styles 树。
/// </summary>
/// <remarks>
/// 基类的公开 TryGetResource 无法用 new 重映射接口槽位，故显式重新声明并实现 IResourceNode，
/// 确保父级以 IStyle/IResourceNode 调用时命中本类。成功结果须经两次稳定性探测确认是共享引用后才缓存，
/// 防止 x:Shared=False 每次返回的新对象被永久共享；未命中结果稳定可立即缓存。
/// 订阅跟随 Owner 生命周期（含 Owner 为 Application 时附加订阅 ActualThemeVariantChanged），
/// Owner 变更时全部退订并失效。仅限 UI 线程访问。
/// </remarks>
public sealed class ResourceLookupCachingStyles : Styles, IResourceNode
{
    private readonly Dictionary<CacheKey, (bool Found, object? Value)> _cache = new(CacheKey.Comparer.Instance);

    // 首次命中先存为候选再观察一次；确认稳定后晋升入 _cache 或判为不可缓存。
    private readonly Dictionary<CacheKey, object?> _candidates = new(CacheKey.Comparer.Instance);

    private readonly HashSet<CacheKey> _nonSharable = new(CacheKey.Comparer.Instance);

    private readonly HashSet<CacheKey> _queriesInProgress = new(CacheKey.Comparer.Instance);

    private int _generation;

    private IResourceHost? _subscribedOwner;
    private Application? _subscribedApplication;

    public ResourceLookupCachingStyles()
    {
        CollectionChanged += (_, _) => Invalidate();
        OwnerChanged += (_, _) => UpdateSubscriptions(Owner);
    }

    bool IResourceNode.HasResources =>
        // 本容器自身不持有资源，也不使用 Count 粗略判断；
        // 注意不要触碰 Resources 属性，基类访问时会创建空字典。
        this.Any(child => (child as IResourceNode)?.HasResources == true);

    bool IResourceNode.TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = null;
        var cacheKey = new CacheKey(key, theme);
        if (!_queriesInProgress.Add(cacheKey))
        {
            return base.TryGetResource(key, theme, out value);
        }

        try
        {
            if (_nonSharable.Contains(cacheKey))
            {
                return base.TryGetResource(key, theme, out value);
            }

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                value = cached.Value;
                return cached.Found;
            }

            var generation = _generation;
            var found = base.TryGetResource(key, theme, out var result);
            value = result;
            if (generation != _generation)
            {
                return found;
            }

            if (!found)
            {
                // 未命中不受 x:Shared 影响，可立即缓存。
                _cache[cacheKey] = (false, null);
                return false;
            }

            if (!_candidates.TryGetValue(cacheKey, out var candidate))
            {
                // 首次命中：只记录候选并照常返回，不缓存。
                _candidates[cacheKey] = result;
                return true;
            }

            _candidates.Remove(cacheKey);
            if (SameResult(candidate, result))
            {
                // 第二次结果与候选相同：可视为共享引用，安全缓存。
                _cache[cacheKey] = (true, result);
            }
            else
            {
                // 每次查询都得到新实例（如 x:Shared=False 的资源），该键永不缓存成功值。
                _nonSharable.Add(cacheKey);
            }

            return true;
        }
        finally
        {
            _queriesInProgress.Remove(cacheKey);
        }
    }

    // 值类型与 string 等不可变值按值判等，其余类型必须两次返回同一引用才可信。
    private static bool SameResult(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return (a is string || a.GetType().IsValueType) && a.Equals(b);
    }

    private void UpdateSubscriptions(IResourceHost? owner)
    {
        if (_subscribedOwner != null)
        {
            _subscribedOwner.ResourcesChanged -= OnResourcesChanged;
        }

        if (_subscribedApplication != null)
        {
            _subscribedApplication.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        }

        Invalidate();
        _subscribedOwner = owner;
        _subscribedApplication = owner as Application;
        if (_subscribedApplication != null)
        {
            _subscribedApplication.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }

        if (owner != null)
        {
            owner.ResourcesChanged += OnResourcesChanged;
        }
    }

    private void Invalidate()
    {
        unchecked
        {
            ++_generation;
        }

        _cache.Clear();
        _candidates.Clear();
        _nonSharable.Clear();
    }

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e) => Invalidate();

    private void OnActualThemeVariantChanged(object? sender, EventArgs e) => Invalidate();

    /// <summary>
    /// 资源 key 采用常规相等语义，theme 必须按引用区分：
    /// 不同 InheritVariant 设置的 ThemeVariant 实例可能 Equal 但语义不同，不能混用同一缓存条目。
    /// </summary>
    private readonly record struct CacheKey(object Key, ThemeVariant? Theme)
    {
        public sealed class Comparer : IEqualityComparer<CacheKey>
        {
            public static readonly Comparer Instance = new();

            private Comparer()
            {
            }

            public bool Equals(CacheKey x, CacheKey y) => Equals(x.Key, y.Key) && ReferenceEquals(x.Theme, y.Theme);

            public int GetHashCode(CacheKey obj) => HashCode.Combine(obj.Key, RuntimeHelpers.GetHashCode(obj.Theme));
        }
    }
}
