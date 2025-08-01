 using System.Linq;
 using System.Threading.Tasks;
 using System.Windows;
 using Avalonia;
 using Avalonia.Controls;
 using Avalonia.Controls.Templates;
 using Avalonia.Interactivity;
 using Avalonia.VisualTree;
 using ClassIsland.Core.Abstractions.Controls;
  using ClassIsland.Models.ActionSettings;
 using ClassIsland.Views;
 using FluentAvalonia.UI.Controls;

 namespace ClassIsland.Controls.ActionSettingsControls;

 /// <summary>
 /// NotificationActionSettingsControl.xaml 的交互逻辑
 /// </summary>
 public partial class NotificationActionSettingsControl : ActionSettingsControlBase<NotificationActionSettings>
 {
     public static readonly StyledProperty<bool> IsShowInDialogProperty = 
         AvaloniaProperty.Register<NotificationActionSettingsControl, bool>(nameof(IsShowInDialog));

     public bool IsShowInDialog
     {
         get => GetValue(IsShowInDialogProperty);
         set => SetValue(IsShowInDialogProperty, value);
     }

     public NotificationActionSettingsControl()
     {
         InitializeComponent();
         DataContext = this;
     }

     public void ButtonShowSettings_OnClick(object? sender, RoutedEventArgs e)
     {
         ShowSettings();
     }
     private async Task ShowSettings(){
         if (!this.TryFindResource("NSettingsDrawer", out var resource) ||
             resource is not IDataTemplate template)
         {
             return;
         }

         var drawer = template.Build(new ContentControl());
         if (drawer == null) return;

         drawer.DataContext = this;

         if (this.GetVisualRoot() is not Window window) return;

         // if (window is SettingsWindowNew)
         // {
         //     IsShowInDialog = false;
         //     if (SettingsPageBase.OpenDrawerCommand?.CanExecute(drawer) == true)
         //     {
         //         SettingsPageBase.OpenDrawerCommand.Execute(drawer);
         //     }
         // }
         // else
         {
             IsShowInDialog = true;
        
             var dialog = new ContentDialog
             {
                 Content = drawer,
                 PrimaryButtonText = "确定",
                 DefaultButton = ContentDialogButton.Primary,
             };

             await dialog.ShowAsync();
         }
     }
 }
