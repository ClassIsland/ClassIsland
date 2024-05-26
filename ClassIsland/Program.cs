using System;
using System.Collections;
using System.Diagnostics;

namespace ClassIsland
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 检查系统版本
            bool isWin7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;

            string? env_GCName = Environment.GetEnvironmentVariable("DOTNET_GCName");
            string? env_EnableWriteXorExecute = Environment.GetEnvironmentVariable("DOTNET_EnableWriteXorExecute");

            if (isWin7)
            {
                if (env_GCName != null && env_EnableWriteXorExecute != null)
                {
                    if (env_GCName == "clrgc.dll" && env_EnableWriteXorExecute == "0")
                    {
                        App app = new App();
                        app.InitializeComponent();
                        app.Run();
                    }
                    else
                    {
                        ProcessStartInfo psi = Process.GetCurrentProcess().StartInfo;
                        psi.EnvironmentVariables.Remove("DOTNET_GCName");
                        psi.EnvironmentVariables.Remove("DOTNET_EnableWriteXorExecute");
                        psi.EnvironmentVariables.Add("DOTNET_GCName", "clrgc.dll");
                        psi.EnvironmentVariables.Add("DOTNET_EnableWriteXorExecute", "0");

                        Process.Start(psi);
                        Environment.Exit(0);
                        return;
                    }
                }
                else
                {
                    ProcessStartInfo psi = Process.GetCurrentProcess().StartInfo;
                    psi.EnvironmentVariables.Add("DOTNET_GCName", "clrgc.dll");
                    psi.EnvironmentVariables.Add("DOTNET_EnableWriteXorExecute", "0");

                    Process.Start(psi);
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
