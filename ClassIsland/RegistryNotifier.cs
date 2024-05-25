using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassIsland;

public class RegistryNotifier
{
    [DllImport("advapi32.dll", EntryPoint = "RegNotifyChangeKeyValue")]
    private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, int hEvent, bool fAsynchronus);
    [DllImport("advapi32.dll", EntryPoint = "RegOpenKey")]
    private static extern int RegOpenKey(uint hKey, string lpSubKey, ref IntPtr phkResult);
    [DllImport("advapi32.dll", EntryPoint = "RegCloseKey")]
    private static extern int RegCloseKey(IntPtr hKey);
    public static uint HKEY_CLASSES_ROOT = 0x80000000;
    public static uint HKEY_CURRENT_USER = 0x80000001;
    public static uint HKEY_LOCAL_MACHINE = 0x80000002;
    public static uint HKEY_USERS = 0x80000003;
    public static uint HKEY_PERFORMANCE_DATA = 0x80000004;
    public static uint HKEY_CURRENT_CONFIG = 0x80000005;
    private static uint HKEY_DYN_DATA = 0x80000006;
    private static int REG_NOTIFY_CHANGE_NAME = 0x1;
    private static int REG_NOTIFY_CHANGE_ATTRIBUTES = 0x2;
    private static int REG_NOTIFY_CHANGE_LAST_SET = 0x4;
    private static int REG_NOTIFY_CHANGE_SECURITY = 0x8;
    /// <summary>
    /// 打开的注册表句柄
    /// </summary>
    private IntPtr _OpenIntPtr = IntPtr.Zero;

    public delegate void RegistryKeyUpdatedHandler();


    public event RegistryKeyUpdatedHandler? RegistryKeyUpdated;
    private bool _isWorking = false;

    private Task UpdatingTask
    {
        get;
    }

    private async void UpdateMain()
    {
        while (_isWorking)
        {
            RegNotifyChangeKeyValue(_OpenIntPtr, true,
                REG_NOTIFY_CHANGE_ATTRIBUTES +
                REG_NOTIFY_CHANGE_LAST_SET +
                REG_NOTIFY_CHANGE_NAME +
                REG_NOTIFY_CHANGE_SECURITY,
                0,
                false
            );
            RegistryKeyUpdated?.Invoke();
            Debug.WriteLine("Updated.");
        }
    }

    public RegistryNotifier(uint root, string path)
    {
        UpdatingTask = new Task(UpdateMain);
        RegOpenKey(root, path, ref _OpenIntPtr);
    }

    public void Start()
    {
        _isWorking = true;
        UpdatingTask.Start();
    }

    public void Stop()
    {
        _isWorking = false;
    }

    ~RegistryNotifier()
    {
        RegCloseKey(_OpenIntPtr);
    }


}