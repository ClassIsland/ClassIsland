using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
namespace ClassIsland.Controls.ActionSettingsControls;

public class ModifyAppSettingsActionControlTemplateSelector : AvaloniaObject, IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; set; } = new();

    public static readonly DirectProperty<ModifyAppSettingsActionControlTemplateSelector, string>
        ControlTemplateNameProperty =
          AvaloniaProperty.RegisterDirect<ModifyAppSettingsActionControlTemplateSelector, string>(
            nameof(ControlTemplateName), o => o.ControlTemplateName, (o, v) => o.ControlTemplateName = v);

    string _controlTemplateName;
    public string ControlTemplateName
    {
        get => _controlTemplateName ?? "";
        set => SetAndRaise(ControlTemplateNameProperty, ref _controlTemplateName, value);
    }

    public Control? Build(object? param) => Templates.GetValueOrDefault(ControlTemplateName)?.Build(param);

    public bool Match(object? data) => true;
}