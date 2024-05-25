using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class BootService : IHostedService
{
    private MainWindow MainWindow { get; }
    public BootService(MainWindow mainWindow, 
        MiniInfoProviderHostService miniInfoProviderHostService)
    {
        MainWindow = mainWindow;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        MainWindow.Show();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}