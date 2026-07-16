using Avalonia.Controls;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 档案数据迁移提供方控件基类。在该类上标记 <see cref="ContributorInfo"/> 特性。
/// </summary>
public abstract class ProfileTransferProviderControlBase : UserControl
{
    /// <summary>
    /// 进行迁移操作。
    /// </summary>
    public abstract Task<bool> InvokeTransfer();
}