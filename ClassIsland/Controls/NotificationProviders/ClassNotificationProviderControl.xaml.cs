namespace ClassIsland.Controls.NotificationProviders;

public partial class ClassNotificationProviderControl : UserControl, INotifyPropertyChanged, IDisposable
{
    private FrameworkElement? _element;
    private string _message = "";
    private int _slideIndex = 0;
    private bool _showTeacherName = false;
    private string _maskMessage = "";

    public FrameworkElement? Element
    {
        get => _element;
        set => SetField(ref _element, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public int SlideIndex
    {
        get => _slideIndex;
        set => SetField(ref _slideIndex, value);
    }

    public bool ShowTeacherName
    {
        get => _showTeacherName;
        set => SetField(ref _showTeacherName, value);
    }

    public string MaskMessage
    {
        get => _maskMessage;
        set => SetField(ref _maskMessage, value);
    }

    public ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();

    private DispatcherTimer Timer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(10)
    };

    public ClassNotificationProviderControl(string key)
    {
        InitializeComponent();
        Element = FindResource(key) as FrameworkElement;
        Timer.Tick += TimerOnTick;

        if (key is "ClassPrepareNotifyOverlay" or "ClassOffOverlay")
        {
            Timer.Start();
        }
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            SlideIndex = SlideIndex == 1 ? 0 : 1;
        }
    }

    public string NextTimeLayoutDurationHumanized
    {
        get
        {
            TimeSpan span = LessonsService.CurrentTimeLayoutItem.Last;
            if (span.TotalSeconds <= 0) return "0 分钟";

            var parts = new List<string>();
            if (span.Hours > 0) parts.Add($"{span.Hours} 小时");
            if (span.Minutes > 0) parts.Add($"{span.Minutes} 分钟");
            if (span.Seconds > 0) parts.Add($"{span.Seconds} 秒");

            return string.Join(" ", parts);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;ClassIsland/Controls/NotificationProviders/ClassNotificationProviderControl.xaml.cs
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        Timer.Stop();
        Timer.Tick -= TimerOnTick;
    }
}