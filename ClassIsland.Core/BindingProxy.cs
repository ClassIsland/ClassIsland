using System.Windows;

namespace ClassIsland.Core;

/// <summary>
/// 绑定代理类
/// </summary>
public class BindingProxy : Freezable
{
    #region Overrides of Freezable

    /// <inheritdoc />
    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }

    #endregion

    public object Data
    {
        get { return (object)GetValue(DataProperty); }
        set { SetValue(DataProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy));
}
