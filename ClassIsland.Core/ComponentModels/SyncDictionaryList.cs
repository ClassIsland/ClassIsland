using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClassIsland.Core.ComponentModels;

/// <summary>
/// 可同步字典与列表的数据类型，用于将字典绑定到前端数据上。
/// </summary>
public class SyncDictionaryList<TKey, TValue> : INotifyPropertyChanged where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _dictionary;
    private readonly Func<TKey> _newKey;
    private bool _isProcessing = false;

    /// <summary>
    /// 公开的用于进行绑定的列表。
    /// </summary>
    public ObservableCollection<KeyValuePair<TKey, TValue>> List { get; } = []; 
    
    /// <summary>
    /// 初始化一个 <see cref="SyncDictionaryList{TKey,TValue}"/> 对象。
    /// </summary>
    public SyncDictionaryList(IDictionary<TKey, TValue> dictionary, Func<TKey> newKey)
    {
        _dictionary = dictionary;
        _newKey = newKey;

        foreach (var v in _dictionary)
        {
            List.Add(v);
        }

        List.CollectionChanged += ListOnCollectionChanged;
        if (_dictionary is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged += DictionaryOnCollectionChanged;
        }
    }

    private void DictionaryOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }
        try
        {
            _isProcessing = true;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null)
                    {
                        break;
                    }
                    foreach (var i in e.NewItems.OfType<KeyValuePair<TKey, TValue>>())
                    {
                        List.Add(i);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null)
                    {
                        break;
                    }
                    foreach (var i in e.OldItems.OfType<KeyValuePair<TKey, TValue>>())
                    {
                        foreach (var k in List.Where(k => k.Key.Equals(i.Key)))
                        {
                            List.Remove(k);
                            break;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            _isProcessing = false;
        }    
    }

    private void ListOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        try
        {
            _isProcessing = true;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null)
                    {
                        break;
                    }
                    foreach (var i in e.NewItems)
                    {
                        _dictionary[_newKey()] = (TValue)i;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null)
                    {
                        break;
                    }
                    foreach (var i in e.OldItems)
                    {
                        foreach (var k in _dictionary.Where(k => k.Value?.Equals(i) ?? false))
                        {
                            _dictionary.Remove(k.Key);
                            break;
                        }
                    }

                    //Subjects = ConfigureFileHelper.CopyObject(Subjects);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}