using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Views;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class UriCommandService : IHostedService
{
    private DispatcherTimer CommandProcessTimer
    {
        get;
    } = new(DispatcherPriority.Normal)
    {
        Interval = TimeSpan.FromSeconds(0.01)
    };

    private ClassChangingWindow ClassChangingWindow { get; set; }

    private ILessonsService LessonsService { get; }

    private MainWindow MainWindow { get; }

    public UriCommandService(ILessonsService lessonsService,MainWindow mainWindow)
    {
        this.LessonsService = lessonsService;
        this.MainWindow = mainWindow;
    }

    public void Init()
    {
        CommandProcessTimer.Tick += CommandProcessTimerOnTick;
        CommandProcessTimer.Start();
    }

    private void CommandProcessTimerOnTick(object? sender, EventArgs e)
    {
        if (!File.Exists("./Temp/Command.ci")) return;
        foreach (var b in File.ReadAllBytes("./Temp/Command.ci"))
        {
            switch ((UriCommandType)(int)b)
            {
                case UriCommandType.ChangeClass:
                    if (LessonsService.CurrentClassPlan is null) break;
                    App.GetService<MainWindow>().ViewModel.IsBusy = true;
                    ClassChangingWindow = new ClassChangingWindow()
                    {
                        ClassPlan = LessonsService.CurrentClassPlan
                    };
                    ClassChangingWindow.ShowDialog();
                    ClassChangingWindow.DataContext = null;
                    ClassChangingWindow = null;
                    App.GetService<MainWindow>().ViewModel.IsBusy = false;
                    break;
                default:
                    break;
            }
        }
        File.Delete("./Temp/Command.ci");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
