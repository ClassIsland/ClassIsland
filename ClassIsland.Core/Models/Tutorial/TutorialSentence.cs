using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ClassIsland.Core.Abstractions.Models;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Helpers.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.Tutorial;

/// <summary>
/// 代表教程中的一句话。
/// </summary>
public partial class TutorialSentence : ObservableObject, IXmlnsAttached
{
    /// <summary>
    /// 语句标题
    /// </summary>
    [ObservableProperty] private string _title = "";
    
    /// <summary>
    /// 语句正文
    /// </summary>
    [ObservableProperty] private string _content = "";
    
    /// <summary>
    /// 语句图标表达式
    /// </summary>
    [NotifyPropertyChangedFor(nameof(IconSource))]
    [ObservableProperty] private string _iconExpression = "";

    /// <summary>
    /// 语句图标
    /// </summary>
    public IconSource? IconSource => IconExpressionHelper.TryParseOrNull(IconExpression);

    /// <summary>
    /// 语句头图
    /// </summary>
    [ObservableProperty] private string _heroImage = "";

    /// <summary>
    /// 要附加的控件的选择器，留空代表不附加控件
    /// </summary>
    [ObservableProperty] private string _targetSelector = "";

    /// <summary>
    /// 是否高亮目标控件
    /// </summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [ObservableProperty] private bool _highlightTarget;

    /// <summary>
    /// 是否为目标控件添加模态，仅允许点击目标控件
    /// </summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [ObservableProperty] private bool _modalTarget;

    /// <summary>
    /// 教程提示框会指向目标控件
    /// </summary>
    [ObservableProperty] private bool _pointToTarget = true;
    
    /// <summary>
    /// 右侧按钮文字，留空代表没有这个按钮
    /// </summary>
    [ObservableProperty] private string _rightButtonText = "下一条";

    /// <summary>
    /// 右侧按钮点击动作
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialAction> _rightButtonActions = [
        new()
        {
            Kind = TutorialActionKind.NextSentence
        }
    ];
    
    /// <summary>
    /// 左侧按钮文字，留空代表没有这个按钮
    /// </summary>
    [ObservableProperty] private string _leftButtonText = "";

    /// <summary>
    /// 左侧按钮点击动作
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialAction> _leftButtonActions = [
        new()
        {
            Kind = TutorialActionKind.PreviousSentence
        }
    ];
    
    /// <summary>
    /// 初始化动作
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialAction> _initializeActions = [];

    /// <summary>
    /// 是否等待外部手动推进教程进度
    /// </summary>
    [ObservableProperty] private bool _waitForNextCommand = false;
    
    /// <summary>
    /// 使用 LightDismiss 代替右侧按钮的行为
    /// </summary>
    [ObservableProperty] private bool _useLightDismiss = true;

    /// <summary>
    /// 语句标签
    /// </summary>
    [ObservableProperty] private string _tag = "";

    /// <summary>
    /// 教学提示拜访位置
    /// </summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [ObservableProperty] private TeachingTipPlacementMode _placementMode;
    
    [ObservableProperty] private IDictionary<string, string> _xmlns = new Dictionary<string, string>();
}