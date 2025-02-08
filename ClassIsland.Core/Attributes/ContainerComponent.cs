namespace ClassIsland.Core.Attributes;

/// <summary>
/// 指示这个组件是组件容器，可以承载其它组件。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ContainerComponent : Attribute;