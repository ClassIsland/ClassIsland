using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Shared.ComponentModels;
// ReSharper disable UsageOfDefaultStructEquality

namespace ClassIsland.Core.ComponentModels;

/// <summary>
/// 可同步字典与列表的数据类型，用于将字典绑定到前端数据上。
/// </summary>
public class SyncDictionaryList<TKey, TValue> : INotifyPropertyChanged, IDisposable where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _dictionary;
    private readonly ObservableOrderedDictionary<TKey, TValue>? _orderedDictionary;
    private readonly Func<TKey> _newKey;
    private bool _isProcessing = false;
    private bool _isDisposed;

    /// <summary>
    /// 公开的用于进行绑定的列表。
    /// </summary>
    public ObservableCollection<KeyValuePair<TKey, TValue>> List { get; } = [];
    
    /// <summary>
    /// 要向列表中添加的默认值。此默认值不会同步回字典。
    /// </summary>
    public KeyValuePair<TKey, TValue>? DefaultValue { get; } 
    
    /// <summary>
    /// 初始化一个 <see cref="SyncDictionaryList{TKey,TValue}"/> 对象。
    /// </summary>
    public SyncDictionaryList(IDictionary<TKey, TValue> dictionary, Func<TKey> newKey, KeyValuePair<TKey, TValue>? defaultValue=null)
    {
        _dictionary = dictionary;
        _orderedDictionary = dictionary as ObservableOrderedDictionary<TKey, TValue>;
        _newKey = newKey;
        DefaultValue = defaultValue;

        if (DefaultValue != null)
        {
            List.Add(DefaultValue.Value);
        }
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
                    var insertIndex = _orderedDictionary != null && e.NewStartingIndex >= 0
                        ? e.NewStartingIndex + (DefaultValue == null ? 0 : 1)
                        : List.Count;
                    foreach (var i in e.NewItems.OfType<KeyValuePair<TKey, TValue>>())
                    {
                        List.Insert(insertIndex++, i);
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
                case NotifyCollectionChangedAction.Reset:
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems is not { Count: 1 }
                        || e.NewItems is not { Count: 1 }
                        || e.OldItems[0] is not KeyValuePair<TKey, TValue> oldItem
                        || e.NewItems[0] is not KeyValuePair<TKey, TValue> newItem
                        || !EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(oldItem, newItem))
                    {
                        break;
                    }

                    var defaultValueOffset = DefaultValue == null ? 0 : 1;
                    var oldListIndex = e.OldStartingIndex + defaultValueOffset;
                    var newListIndex = e.NewStartingIndex + defaultValueOffset;
                    if (oldListIndex < defaultValueOffset
                        || oldListIndex >= List.Count
                        || newListIndex < defaultValueOffset
                        || newListIndex >= List.Count
                        || !EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(
                            List[oldListIndex], oldItem))
                    {
                        break;
                    }

                    if (oldListIndex != newListIndex)
                    {
                        List.Move(oldListIndex, newListIndex);
                    }
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
                    var insertIndex = Math.Max(
                        0,
                        e.NewStartingIndex - (DefaultValue == null ? 0 : 1));
                    foreach (var i in e.NewItems.OfType<KeyValuePair<TKey, TValue>>())
                    {
                        if (DefaultValue != null && Equals(i, DefaultValue.Value))
                        {
                            continue;
                        }
                        if (_orderedDictionary != null && e.NewStartingIndex >= 0)
                        {
                            _orderedDictionary.Insert(insertIndex++, i);
                        }
                        else
                        {
                            _dictionary[_newKey()] = i.Value;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null)
                    {
                        break;
                    }
                    foreach (var i in e.OldItems.OfType<KeyValuePair<TKey, TValue>>())
                    {
                        if (DefaultValue != null && Equals(i, DefaultValue.Value))
                        {
                            continue;
                        }
                        if (_orderedDictionary != null)
                        {
                            _orderedDictionary.Remove(i.Key);
                            continue;
                        }
                        foreach (var k in _dictionary.Where(k => k.Value?.Equals(i.Value) ?? false))
                        {
                            _dictionary.Remove(k.Key);
                            break;
                        }
                    }

                    //Subjects = ConfigureFileHelper.CopyObject(Subjects);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (_orderedDictionary == null
                        || e.OldItems is not { Count: 1 }
                        || e.NewItems is not { Count: 1 }
                        || e.OldItems[0] is not KeyValuePair<TKey, TValue> movedItem
                        || e.NewItems[0] is not KeyValuePair<TKey, TValue> newItem)
                    {
                        break;
                    }

                    var defaultValueOffset = DefaultValue == null ? 0 : 1;
                    var oldDictionaryIndex = e.OldStartingIndex - defaultValueOffset;
                    var newDictionaryIndex = e.NewStartingIndex - defaultValueOffset;
                    if (oldDictionaryIndex < 0
                        || oldDictionaryIndex >= _orderedDictionary.Count
                        || newDictionaryIndex < 0
                        || newDictionaryIndex >= _orderedDictionary.Count
                        || e.NewStartingIndex < 0
                        || e.NewStartingIndex >= List.Count
                        || !EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(movedItem, newItem)
                        || !EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(
                            List[e.NewStartingIndex], movedItem)
                        || !EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(
                            _orderedDictionary[oldDictionaryIndex], movedItem))
                    {
                        if (e.NewStartingIndex >= 0
                            && e.NewStartingIndex < List.Count
                            && e.OldStartingIndex >= 0
                            && e.OldStartingIndex < List.Count)
                        {
                            List.Move(e.NewStartingIndex, e.OldStartingIndex);
                        }
                        break;
                    }

                    if (oldDictionaryIndex != newDictionaryIndex)
                    {
                        _orderedDictionary.Move(oldDictionaryIndex, newDictionaryIndex);
                    }
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        List.CollectionChanged -= ListOnCollectionChanged;
        if (_dictionary is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged -= DictionaryOnCollectionChanged;
        }
    }
}
