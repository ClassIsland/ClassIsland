using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Shared;
using ClassIsland.ViewModels.EditMode;

namespace ClassIsland.Controls.EditMode;

public partial class EditModeView : UserControl
{
    public EditModeViewModel ViewModel = IAppHost.GetService<EditModeViewModel>();
    
    public EditModeView()
    {
        InitializeComponent();
    }
}