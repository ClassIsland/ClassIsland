﻿using System.Drawing;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 主界面组件属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ComponentInfo : Attribute
{
    /// <summary>
    /// 组件GUID
    /// </summary>
    public Guid Guid { get; }

    /// <summary>
    /// 组件名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 组件图标
    /// </summary>
    public IconSource? IconSource { get; }

    /// <summary>
    /// 组件位图图标uri
    /// </summary>
    public string BitmapIconUri { get; } = "";

    /// <summary>
    /// 是否使用位图图标
    /// </summary>
    public bool UseBitmapIcon { get; } = false;

    /// <summary>
    /// 组件描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 设置界面类型
    /// </summary>
    public Type? SettingsType { get; internal set; }

    /// <summary>
    /// 组件类型
    /// </summary>
    public Type? ComponentType { get; internal set; }

    /// <summary>
    /// 空组件信息
    /// </summary>
    public static ComponentInfo Empty { get; } = new(Guid.Empty.ToString(), "？？？");

    /// <summary>
    /// 组件是否是组件容器
    /// </summary>
    public bool IsComponentContainer { get; internal set; } = false;

    internal List<string> MigrateSources { get; } = new();

    /// <inheritdoc />
    public ComponentInfo(string guid, string name, string iconSource, string description = "") : this(guid, name,
        description)
    {
        IconSource = IconExpressionHelper.TryParseOrNull(iconSource);
    }

    /// <inheritdoc />
    public ComponentInfo(string guid, string name, string description = "")
    {
        Guid = Guid.Parse(guid);
        Name = name;
        Description = description;
    }
}