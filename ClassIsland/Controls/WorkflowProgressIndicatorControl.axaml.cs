using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Automation;

namespace ClassIsland.Controls;

[PseudoClasses(":mini", ":open")]
public class WorkflowProgressIndicatorControl : TemplatedControl
{
    public static readonly StyledProperty<Workflow?> WorkflowProperty = AvaloniaProperty.Register<WorkflowProgressIndicatorControl, Workflow?>(
        nameof(Workflow));

    public Workflow? Workflow
    {
        get => GetValue(WorkflowProperty);
        set => SetValue(WorkflowProperty, value);
    }

    private IDisposable? _observer1;
    
    private IDisposable? _observer2;
    private IDisposable? _observer3;

    public WorkflowProgressIndicatorControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _observer1?.Dispose();
        _observer1 = this.GetObservable(WorkflowProperty).Subscribe(_ => UpdateContent());
    }
    
    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _observer1?.Dispose();
    }

    private void UpdateContent()
    {
        if (Workflow == null)
        {
            return;
        }
    }
}