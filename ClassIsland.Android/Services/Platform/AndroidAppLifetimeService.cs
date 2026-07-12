using Android.Content;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class AndroidAppLifetimeService : IAppLifetimeService
{
    internal const string RestartParametersExtra = "cn.classisland.android.RESTART_PARAMETERS";

    private readonly object _restartLock = new();
    private string[] _restartParameters = [];
    private bool _isRestartScheduled;

    public void Shutdown()
    {
        bool restartScheduled;
        lock (_restartLock)
        {
            restartScheduled = _isRestartScheduled;
        }

        if (restartScheduled)
        {
            RestartProcess();
            return;
        }

        if (MainActivity.Current?.TryGetTarget(out var mainActivity) == true)
        {
            mainActivity.FinishAndRemoveTask();
        }

        global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        lock (_restartLock)
        {
            _restartParameters = [.. parameters];
            if (_isRestartScheduled)
            {
                return;
            }

            _isRestartScheduled = true;
        }
    }

    private void RestartProcess()
    {
        string[] parameters;
        lock (_restartLock)
        {
            parameters = [.. _restartParameters];
        }

        using var intent = new Intent(Application.Instance, typeof(MainActivity));
        intent.SetAction(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);
        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
        intent.PutExtra(RestartParametersExtra, parameters);

        try
        {
            Application.Instance.StartActivity(intent);
        }
        finally
        {
            global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
        }
    }
}
