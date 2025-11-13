using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using static ClassIsland.Models.Actions.ModifyAppSettingsActionSettings;
namespace ClassIsland.Controls.ActionSettingsControls;

public class ModifyAppSettingsActionKindTemplateSelector : AvaloniaObject, IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; set; } = new();

    public string? Name { get; set; }

    public static readonly DirectProperty<ModifyAppSettingsActionKindTemplateSelector, string> KindProperty =
        AvaloniaProperty.RegisterDirect<ModifyAppSettingsActionKindTemplateSelector, string>(nameof(Kind), o => o.Kind, (o, v) => o.Kind = v);

    string _kind;
    public string Kind
    {
        get => _kind;
        set => SetAndRaise(KindProperty, ref _kind, value);
    }

    public Control? Build(object? param) => Templates.GetValueOrDefault(Kind ?? "")?.Build(param);

    public bool Match(object? data) => true;
}