using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;

namespace ClassIsland.Core.Controls.Ruleset;

/// <summary>
/// 规则集设置控件显示控件。
/// </summary>
public partial class RulesetSettingsControlPresenter : UserControl
{
    public static readonly DependencyProperty RuleIdProperty = DependencyProperty.Register(
        nameof(RuleId), typeof(string), typeof(RulesetSettingsControlPresenter), new PropertyMetadata(default(string), (o, args) =>
        {
            if (o is RulesetSettingsControlPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public string RuleId
    {
        get { return (string)GetValue(RuleIdProperty); }
        set { SetValue(RuleIdProperty, value); }
    }

    public static readonly DependencyProperty RuleProperty = DependencyProperty.Register(
        nameof(Rule), typeof(Rule), typeof(RulesetSettingsControlPresenter), new PropertyMetadata(default(Rule), (o, args) =>
        {
            if (o is RulesetSettingsControlPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public Rule? Rule
    {
        get { return (Rule)GetValue(RuleProperty); }
        set { SetValue(RuleProperty, value); }
    }

    /// <inheritdoc />
    public RulesetSettingsControlPresenter()
    {
        InitializeComponent();
    }

    private void UpdateContent()
    {
        if (Rule == null)
        {
            return;
        }
        if (!IRulesetService.Rules.TryGetValue(RuleId, out var info))
        {
            return;
        }

        var ruleSettings = Rule.Settings;
        RootContentPresenter.Content = RuleSettingsControlBase.GetInstance(info, ref ruleSettings);
        Rule.Settings = ruleSettings;
    }
}