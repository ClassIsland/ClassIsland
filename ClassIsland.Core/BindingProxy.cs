using Avalonia;

namespace ClassIsland.Core;

/// <summary>
/// 绑定代理
/// </summary>
public class BindingProxy : AvaloniaObject
{
    private BindingProxy()
    {
        
    }
}

/// <summary>
/// 绑定代理
/// </summary>
/// <typeparam name="T">绑定数据类型</typeparam>
public class BindingProxy<T> : AvaloniaObject
{
    
    public static readonly StyledProperty<T> DataProperty = AvaloniaProperty.Register<BindingProxy, T>(
        nameof(Data));

    public T Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
}