using System.ComponentModel;

namespace ClassIsland.Core.ComponentModels;

/// <summary>
/// 可监听 Value 内部属性变化的 KeyValuePair。
/// </summary>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TValue">值类型，须继承 INotifyPropertyChanged</typeparam>
public class ObservableKeyValuePair<TKey, TValue> : INotifyPropertyChanged, IDisposable 
    where TValue : INotifyPropertyChanged
{
    private TKey _key;
    private TValue _value;
    private bool _disposed = false;
    
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// 键
    /// </summary>
    public TKey Key
    {
        get => _key;
        set
        {
            if (Equals(_key, value)) return;
            _key = value;
            OnPropertyChanged(nameof(Key));
        }
    }

    /// <summary>
    /// 值
    /// </summary>
    public TValue Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value)) return;
            
            if (_value != null)
            {
                _value.PropertyChanged -= OnValuePropertyChanged;
            }
            
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _value.PropertyChanged += OnValuePropertyChanged;
            
            OnPropertyChanged(nameof(Value));
        }
    }

    /// <summary>
    /// 初始化一个 <see cref="ObservableKeyValuePair{TKey,TValue}"/> 对象。
    /// </summary>
    public ObservableKeyValuePair(TKey key, TValue value)
    {
        _key = key;
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _value.PropertyChanged += OnValuePropertyChanged;
    }
    
    /// <summary>
    /// 使用 <see cref="KeyValuePair{TKey,TValue}"/> 初始化一个 <see cref="ObservableKeyValuePair{TKey,TValue}"/> 对象。
    /// </summary>
    public ObservableKeyValuePair(KeyValuePair<TKey, TValue> keyValuePair)
        : this(keyValuePair.Key, keyValuePair.Value) {}
    
    private void OnValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged($"Value.{e.PropertyName}");
    }
    
    /// <summary>
    /// 触发属性更改事件
    /// </summary>
    /// <param name="propertyName">更改的属性名称</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// 销毁函数
    /// </summary>
    /// <param name="disposing">是否正在销毁</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            if (_value != null)
            {
                _value.PropertyChanged -= OnValuePropertyChanged;
            }
        }
            
        _disposed = true;
    }
    
    /// <summary>
    /// 在需要时从 KeyValuePair 隐式转换到 ObservableKeyValuePair。
    /// </summary>
    /// <param name="pair">KeyValuePair 实例</param>
    /// <returns>ObservableKeyValuePair 实例</returns>
    public static implicit operator ObservableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
    {
        return new ObservableKeyValuePair<TKey, TValue>(pair);
    }
    
    /// <summary>
    /// 析构函数
    /// </summary>
    ~ObservableKeyValuePair()
    {
        Dispose(false);
    }
}