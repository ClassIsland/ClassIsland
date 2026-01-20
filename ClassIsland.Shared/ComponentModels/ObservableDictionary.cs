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
public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IDictionary where TKey : notnull
{
    private const string IndexerName = "Item";
    
    private Dictionary<TKey, TValue> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    public ObservableDictionary()
    {
        _inner = new Dictionary<TKey, TValue>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    public ObservableDictionary(int capacity)
    {
        _inner = new Dictionary<TKey, TValue>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class using an IDictionary.
    /// </summary>
    public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null)
    {
        if (dictionary != null)
        {
            _inner = new Dictionary<TKey, TValue>(dictionary, comparer ?? EqualityComparer<TKey>.Default);
        }
        else
        {
            throw new ArgumentNullException(nameof(dictionary));
        }
    }

    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Raised when a property on the collection changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public int Count => _inner.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public ICollection<TKey> Keys => _inner.Keys;

    /// <inheritdoc/>
    public ICollection<TValue> Values => _inner.Values;

    bool IDictionary.IsFixedSize => ((IDictionary)_inner).IsFixedSize;

    ICollection IDictionary.Keys => ((IDictionary)_inner).Keys;

    ICollection IDictionary.Values => ((IDictionary)_inner).Values;

    bool ICollection.IsSynchronized => ((IDictionary)_inner).IsSynchronized;

    object ICollection.SyncRoot => ((IDictionary)_inner).SyncRoot;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _inner.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _inner.Values;

    /// <summary>
    /// Gets or sets the named resource.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The resource, or null if not found.</returns>
    public TValue this[TKey key]
    {
        get { return _inner[key]; }

        set
        {
            bool replace = _inner.TryGetValue(key, out var old);
            _inner[key] = value;

            if (replace)
            {
                PropertyChanged?.Invoke(this,
                    new PropertyChangedEventArgs($"{IndexerName}[{key}]"));

                if (CollectionChanged != null)
                {
                    var e = new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, old!));
                    CollectionChanged(this, e);
                }
            }
            else
            {
                NotifyAdd(key, value);
            }
        }
    }

    object? IDictionary.this[object key]
    {
        get => ((IDictionary)_inner)[key];
        set => ((IDictionary)_inner)[key] = value;
    }

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        _inner.Add(key, value);
        NotifyAdd(key, value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        var old = _inner;

        _inner = new Dictionary<TKey, TValue>();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));

        if (CollectionChanged != null)
        {
            var e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                old.ToArray(),
                -1);
            CollectionChanged(this, e);
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((IDictionary<TKey, TValue>)_inner).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
#if NETCOREAPP
        if (_inner.Remove(key, out var value))
#else
        if (_inner.TryGetValue(key, out var value) && _inner.Remove(key))
#endif
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{IndexerName}[{key}]"));

            if (CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    new[] { new KeyValuePair<TKey, TValue>(key, value) },
                    -1);
                CollectionChanged(this, e);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
#if NETCOREAPP
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) 
#else
    public bool TryGetValue(TKey key, out TValue value) 
#endif
        => _inner.TryGetValue(key, out value);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_inner).CopyTo(array, index);

    /// <inheritdoc/>
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return _inner.Contains(item);
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    /// <inheritdoc/>
    void IDictionary.Add(object key, object? value) => Add((TKey)key, (TValue)value!);

    /// <inheritdoc/>
    bool IDictionary.Contains(object key) => ((IDictionary)_inner).Contains(key);

    /// <inheritdoc/>
    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_inner).GetEnumerator();

    /// <inheritdoc/>
    void IDictionary.Remove(object key) => Remove((TKey)key);

    private void NotifyAdd(TKey key, TValue value)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{IndexerName}[{key}]"));

        if (CollectionChanged != null)
        {
            var e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                new[] { new KeyValuePair<TKey, TValue>(key, value) },
                -1);
            CollectionChanged(this, e);
        }
    }
}