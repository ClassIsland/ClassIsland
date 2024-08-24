using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.IpcDemo.ViewModels;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using dotnetCampus.Ipc.Pipes;

namespace ClassIsland.IpcDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new MainViewModel();

    private IpcClient _ipcClient;

    public MainWindow()
    {
        _ipcClient = new IpcClient();
        _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.CurrentTimeStateChangedNotifyId, () => MessageBox.Show("CurrentTimeStateChanged."));
        _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnClassNotifyId, () => MessageBox.Show("OnClass."));
        _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnBreakingTimeNotifyId, () => MessageBox.Show("OnBreakingTime."));
        _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnAfterSchoolNotifyId, () => MessageBox.Show("OnAfterSchool."));
        _ = _ipcClient.Connect();
        InitializeComponent();
        DataContext = this;
    }

    private void ButtonNavigate_OnClick(object sender, RoutedEventArgs e)
    {
        var uriSc = _ipcClient.Provider.CreateIpcProxy<IPublicUriNavigationService>(_ipcClient.PeerProxy!);
        uriSc.NavigateWrapped(new Uri(ViewModel.Uri, UriKind.RelativeOrAbsolute));
    }

    private void ButtonRefreshLessonInfos_OnClick(object sender, RoutedEventArgs e)
    {
        var lessonSc = _ipcClient.Provider.CreateIpcProxy<IPublicLessonsService>(_ipcClient.PeerProxy!);

        var sb = new StringBuilder();
        sb.AppendLine($"当前科目： {lessonSc.CurrentSubject?.Name}");
        sb.AppendLine($"当前时间点： {lessonSc.CurrentTimeLayoutItem.StartSecond} - {lessonSc.CurrentTimeLayoutItem.EndSecond}");

        ViewModel.LessonInfos = sb.ToString();
    }
}