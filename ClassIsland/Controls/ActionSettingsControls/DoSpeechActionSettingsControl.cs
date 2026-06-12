using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public class DoSpeechSettingsControl : ActionSettingsControlBase<DoSpeechActionSettings>
{
    private readonly TextBox _textBox;

    public DoSpeechSettingsControl()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new(10) };

        panel.Children.Add(new TextBlock
        {
            Text = "语音播报",
            FontWeight = Avalonia.Media.FontWeight.Bold,
            FontSize = 14
        });

        _textBox = new TextBox
        {
            Watermark = "请输入要播报的文字",
            AcceptsReturn = true,
            Height = 120,
            Width = 420
        };
        _textBox.TextChanged += (_, _) => { Settings.Text = _textBox.Text ?? string.Empty; };
        panel.Children.Add(_textBox);

        Content = panel;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _textBox.Text = Settings.Text;
    }
}