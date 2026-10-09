using System.ComponentModel;
using System.Reflection;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using dotnetCampus.Ipc.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class LessonsActivationTests
{
    [Fact]
    public void TimerTicks_WithUnchangedSchedule_DoNotNotifyActivationOrRebuildCalendar()
    {
        using var fixture = new LessonsFixture();
        var calendar = fixture.CreateCalendar();
        calendar.ActivateCalendar();
        var days = calendar.CalendarDays;
        var week = calendar.WeekDays;
        var changes = new List<string?>();
        var calendarRefreshes = 0;
        calendar.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewViewModel.CalendarDays)) calendarRefreshes++;
        };
        fixture.Plan.PropertyChanged += RecordActivation;
        fixture.Layout.PropertyChanged += RecordActivation;

        try
        {
            for (var i = 0; i < 100; i++)
            {
                fixture.Now = fixture.Now.AddMilliseconds(50);
                fixture.Tick();
            }

            Assert.True(changes.Count == 0 && calendarRefreshes == 0,
                $"100 ticks raised {changes.Count} activation changes and {calendarRefreshes} calendar rebuilds.");
            Assert.Same(days, calendar.CalendarDays);
            Assert.Same(week, calendar.WeekDays);
            Assert.True(fixture.Plan.IsActivated);
            Assert.True(fixture.Layout.IsActivated);
            Assert.Equal(TimeState.OnClass, fixture.Lessons.CurrentState);
        }
        finally
        {
            calendar.DeactivateCalendar();
        }

        void RecordActivation(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ClassPlan.IsActivated))
                changes.Add(args.PropertyName);
        }
    }

    [Fact]
    public void TimerTick_WhenPlanChanges_ActivatesOnlyTheNewPlanAndLayout()
    {
        using var fixture = new LessonsFixture();
        var layoutId = Guid.NewGuid();
        var nextLayout = new TimeLayout();
        fixture.Profile.TimeLayouts.Add(layoutId, nextLayout);
        var nextPlan = new ClassPlan { TimeLayoutId = layoutId };
        var nextPlanId = Guid.NewGuid();
        fixture.Profile.ClassPlans.Add(nextPlanId, nextPlan);
        fixture.Profile.TempClassPlanId = nextPlanId;
        fixture.Profile.TempClassPlanSetupTime = fixture.Now;

        fixture.Tick();

        Assert.Same(nextPlan, fixture.Lessons.CurrentClassPlan);
        Assert.False(fixture.Plan.IsActivated);
        Assert.False(fixture.Layout.IsActivated);
        Assert.True(nextPlan.IsActivated);
        Assert.True(nextLayout.IsActivated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TimerTick_WhenLoadingDisabled_PreservesManualLayoutActivation(bool manual)
    {
        using var fixture = new LessonsFixture();
        fixture.Layout.IsActivatedManually = manual;
        fixture.Lessons.IsClassPlanEnabled = false;

        fixture.Tick();

        Assert.Null(fixture.Lessons.CurrentClassPlan);
        Assert.False(fixture.Plan.IsActivated);
        Assert.Equal(manual, fixture.Layout.IsActivated);
        Assert.False(fixture.Lessons.IsClassPlanLoaded);
    }

    [Fact]
    public void TimerTick_WhenCurrentPlanLosesItsLayout_DeactivatesThePlan()
    {
        using var fixture = new LessonsFixture();
        fixture.Plan.TimeLayoutId = Guid.NewGuid();

        fixture.Tick();

        Assert.Same(fixture.Plan, fixture.Lessons.CurrentClassPlan);
        Assert.False(fixture.Plan.IsActivated);
        Assert.False(fixture.Layout.IsActivated);
        Assert.False(fixture.Lessons.IsClassPlanLoaded);
    }

    [Fact]
    public void TimerTick_AcrossLessonBoundary_StillRaisesTimeStateChange()
    {
        using var fixture = new LessonsFixture();
        var afterSchoolEvents = 0;
        fixture.Lessons.OnAfterSchool += (_, _) => afterSchoolEvents++;
        fixture.Now = fixture.Now.Date.AddHours(9);

        fixture.Tick();
        fixture.Tick();

        Assert.Equal(TimeState.AfterSchool, fixture.Lessons.CurrentState);
        Assert.Equal(1, afterSchoolEvents);
        Assert.True(fixture.Plan.IsActivated);
        Assert.True(fixture.Layout.IsActivated);
    }

    private sealed class LessonsFixture : IDisposable
    {
        private readonly IHost? _previousHost = IAppHost.Host;
        private readonly SynchronizationContext? _previousContext = SynchronizationContext.Current;
        private readonly QueuedSynchronizationContext _context = new();
        private readonly IHost _host;
        // 只注册 IPC 接口，不启动服务器；此库不支持释放尚未启动的 provider。
        private readonly IpcProvider _ipcProvider = new($"lessons-tests-{Guid.NewGuid():N}");
        private readonly IProfileService _profileService;
        private readonly Action<object?, EventArgs> _tick;

        public DateTime Now { get; set; } = new(2026, 10, 5, 8, 10, 0);
        public Profile Profile { get; } = new();
        public ClassPlan Plan { get; }
        public TimeLayout Layout { get; } = new()
        {
            Layouts = [new TimeLayoutItem
            {
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(8.75),
                TimeType = 0
            }]
        };
        public LessonsService Lessons { get; }

        public LessonsFixture()
        {
            // 每轮课程更新后统一处理日历队列，模拟 UI 线程的刷新合并。
            SynchronizationContext.SetSynchronizationContext(_context);
            var layoutId = Guid.NewGuid();
            Profile.TimeLayouts.Add(layoutId, Layout);
            Plan = new ClassPlan { TimeLayoutId = layoutId };
            var planId = Guid.NewGuid();
            Profile.ClassPlans.Add(planId, Plan);
            Profile.TempClassPlanId = planId;
            Profile.TempClassPlanSetupTime = Now;

            _profileService = new TestProfileService { Profile = Profile };
            var clock = CreateProxy<IExactTimeService>(method => method.Name switch
            {
                "GetCurrentLocalDateTime" => Now,
                _ => throw new NotSupportedException(method.Name)
            });
            var rules = CreateProxy<IRulesetService>(method => method.Name switch
            {
                "RegisterRuleHandler" or "NotifyStatusChanged" => null,
                _ => throw new NotSupportedException(method.Name)
            });
            var ipc = CreateProxy<IIpcService>(method => method.Name switch
            {
                "get_IpcProvider" => _ipcProvider,
                "BroadcastNotificationAsync" => Task.CompletedTask,
                _ => throw new NotSupportedException(method.Name)
            });
            var settings = new SettingsService(NullLogger<SettingsService>.Instance, null!);
            var dateStateType = typeof(MainViewViewModel).Assembly
                .GetType("ClassIsland.Models.HomeDateSelectionState", throwOnError: true)!;
            _host = new HostBuilder().ConfigureServices(services => services
                .AddSingleton(clock)
                .AddSingleton(settings)
                .AddSingleton(dateStateType, Activator.CreateInstance(dateStateType, nonPublic: true)!)).Build();
            IAppHost.Host = _host;
            Lessons = new LessonsService(settings, _profileService,
                NullLogger<LessonsService>.Instance, clock, rules, ipc);
            Lessons.StopMainTimer();
            _tick = typeof(LessonsService)
                .GetMethod("MainTimerOnTick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<object?, EventArgs>>(Lessons);
        }

        public void Tick()
        {
            _tick(null, EventArgs.Empty);
            _context.Drain();
        }

        public MainViewViewModel CreateCalendar() => new(null!, null!, null!, Lessons, _profileService);

        public void Dispose()
        {
            Lessons.StopMainTimer();
            IAppHost.Host = _previousHost;
            SynchronizationContext.SetSynchronizationContext(_previousContext);
            _host.Dispose();
        }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<Action> _callbacks = new();

        public override void Post(SendOrPostCallback callback, object? state) =>
            _callbacks.Enqueue(() => callback(state));

        public void Drain()
        {
            while (_callbacks.TryDequeue(out var callback)) callback();
        }
    }

    private sealed class TestProfileService : IProfileService
    {
        public string CurrentProfilePath { get; set; } = "Default.json";
        public Profile Profile { get; set; } = new();
        public bool IsCurrentProfileTrusted => true;
        public void CleanExpiredTempClassPlan() { }
        public void ClearExpiredTempClassPlanGroup() { }
        Task IProfileService.LoadProfileAsync() => throw new NotSupportedException();
        public void SaveProfile() => throw new NotSupportedException();
        public void SaveProfile(string filename) => throw new NotSupportedException();
        public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId = null, DateTime? enableDateTime = null) =>
            throw new NotSupportedException();
        public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId, DateTime? enableDateTime, bool createTempTimeLayout) =>
            throw new NotSupportedException();
        public void ClearTempClassPlan() => throw new NotSupportedException();
        public void ConvertToStdClassPlan() => throw new NotSupportedException();
        public void ConvertToStdClassPlan(Guid id) => throw new NotSupportedException();
        public void SetupTempClassPlanGroup(Guid key, DateTime? expireTime = null) => throw new NotSupportedException();
        public void ClearTempClassPlanGroup() => throw new NotSupportedException();
        public void TrustCurrentProfile() => throw new NotSupportedException();
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, CallbackDispatchProxy>();
        ((CallbackDispatchProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    public class CallbackDispatchProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Proxy method is unavailable."));
    }
}
