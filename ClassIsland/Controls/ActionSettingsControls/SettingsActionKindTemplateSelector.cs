using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
namespace ClassIsland.Controls.ActionSettingsControls;

public class SettingsActionKindTemplateSelector : AvaloniaObject, IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; set; } = new();

    public static readonly DirectProperty<SettingsActionKindTemplateSelector, string> KindProperty =
        AvaloniaProperty.RegisterDirect<SettingsActionKindTemplateSelector, string>(nameof(Kind), o => o.Kind, (o, v) => o.Kind = v);

    string _kind;
    public string Kind
    {
        get => _kind;
        set => SetAndRaise(KindProperty, ref _kind, value);
    }

    public Control? Build(object? param) => Templates.GetValueOrDefault(Kind)?.Build(param);

    public bool Match(object? data) => true;
}