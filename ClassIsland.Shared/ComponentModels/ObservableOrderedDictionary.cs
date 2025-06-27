using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ClassIsland.Shared.ComponentModels;

/// <summary>
/// 同时实现 <see cref="IDictionary"/>、<see cref="IList"/>、<see cref="INotifyCollectionChanged"/> 的字典结构。
/// </summary>
/// <typeparam name="TKey">字典键类型</typeparam>
/// <typeparam name="TValue">字典值类型</typeparam>
public class ObservableOrderedDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IList<KeyValuePair<TKey, TValue>>,
    INotifyCollectionChanged,
    INotifyPropertyChanged
    where TKey : notnull
{
    private readonly List<KeyValuePair<TKey, TValue>> _items = new();
    private readonly Dictionary<TKey, TValue> _dict = new();

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 引发 CollectionChanged 事件
    /// </summary>
    /// <param name="args">事件参数</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// 引发 PropertyChanged 事件
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region IDictionary<TKey, TValue>

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            if (_dict.ContainsKey(key))
            {
                int index = _items.FindIndex(kv => EqualityComparer<TKey>.Default.Equals(kv.Key, key));
                var old = _items[index];
                _items[index] = new KeyValuePair<TKey, TValue>(key, value);
                _dict[key] = value;

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    _items[index], old, index));
                OnPropertyChanged("Item[]");
                OnPropertyChanged("Values");
            }
            else
            {
                Add(key, value);
            }
        }
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys => new List<TKey>(_dict.Keys);

    /// <inheritdoc/>
    public ICollection<TValue> Values => new List<TValue>(_dict.Values);

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        if (_dict.ContainsKey(key)) throw new ArgumentException("Key 已存在", nameof(key));
        var kv = new KeyValuePair<TKey, TValue>(key, value);
        _dict.Add(key, value);
        _items.Add(kv);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            kv, _items.Count - 1));
        OnPropertyChanged("Count");
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        if (!_dict.TryGetValue(key, out _)) return false;
        int index = _items.FindIndex(kv => EqualityComparer<TKey>.Default.Equals(kv.Key, key));
        var kvp = _items[index];
        _dict.Remove(key);
        _items.RemoveAt(index);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            kvp, index));
        OnPropertyChanged("Count");
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
        return true;
    }

    /// <inheritdoc/>
    #if NETCOREAPP
    public bool TryGetValue(TKey key, [NotNullWhen(true)]out TValue? value) => _dict.TryGetValue(key, out value);
    #else
    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);
    #endif
    
    #endregion

    #region ICollection<KeyValuePair<TKey,TValue>>

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public void Clear()
    {
        _dict.Clear();
        _items.Clear();

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset));
        OnPropertyChanged("Count");
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        _dict.TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    #endregion

    #region IEnumerable<KeyValuePair<TKey,TValue>>

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region IList<KeyValuePair<TKey,TValue>>

    /// <inheritdoc/>
    public int IndexOf(KeyValuePair<TKey, TValue> item) {
        for (var i = 0; i < _items.Count; i++)
        {
            var current = _items[i];
            if (EqualityComparer<TKey>.Default.Equals(current.Key, item.Key) &&
                EqualityComparer<TValue>.Default.Equals(current.Value, item.Value))
            {
                return i;
            }
        }
        return -1;
    }

    /// <inheritdoc/>
    public void Insert(int index, KeyValuePair<TKey, TValue> item)
    {
        if (_dict.ContainsKey(item.Key)) throw new ArgumentException("Key 已存在", nameof(item.Key));
        _dict.Add(item.Key, item.Value);
        _items.Insert(index, item);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            item, index));
        OnPropertyChanged("Count");
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        var kvp = _items[index];
        _items.RemoveAt(index);
        _dict.Remove(kvp.Key);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            kvp, index));
        OnPropertyChanged("Count");
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
    }

    /// <inheritdoc/>
    public KeyValuePair<TKey, TValue> this[int index]
    {
        get => _items[index];
        set
        {
            var old = _items[index];
            if (!EqualityComparer<TKey>.Default.Equals(old.Key, value.Key))
            {
                if (_dict.ContainsKey(value.Key)) throw new ArgumentException("Key 已存在", nameof(value.Key));
                _dict.Remove(old.Key);
                _dict.Add(value.Key, value.Value);
            }
            else
            {
                _dict[value.Key] = value.Value;
            }

            _items[index] = value;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace,
                value, old, index));
            OnPropertyChanged("Values");
            OnPropertyChanged("Item[]");
        }
    }

    #endregion
}