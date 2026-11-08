using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

public partial class ProfileRecoveryWindow : ViewBase
{
    public ProfileRecoveryWindow()
    {
        InitializeComponent();
    }

    public ProfileRecoveryWindow(ProfileRecoveryViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
