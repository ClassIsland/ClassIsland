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
    // Dictionary 的枚举顺序不是跨运行时的契约，档案排序需要独立保存键的顺序。
    private readonly List<TKey> _order = new();

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
            _order.AddRange(dictionary.Keys);
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
    public ICollection<TKey> Keys => new OrderedCollection<TKey>(this, pair => pair.Key, _inner.ContainsKey);

    /// <inheritdoc/>
    public ICollection<TValue> Values => new OrderedCollection<TValue>(this, pair => pair.Value, value => _inner.Values.Contains(value));

    bool IDictionary.IsFixedSize => ((IDictionary)_inner).IsFixedSize;

    ICollection IDictionary.Keys => (ICollection)Keys;

    ICollection IDictionary.Values => (ICollection)Values;

    bool ICollection.IsSynchronized => ((IDictionary)_inner).IsSynchronized;

    object ICollection.SyncRoot => ((IDictionary)_inner).SyncRoot;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

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
        set => this[(TKey)key] = (TValue)value!;
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
        var old = _order.Select(key => new KeyValuePair<TKey, TValue>(key, _inner[key])).ToArray();

        _inner.Clear();
        _order.Clear();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));

        if (CollectionChanged != null)
        {
            var e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                old,
                -1);
            CollectionChanged(this, e);
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _order.Select(key => new KeyValuePair<TKey, TValue>(key, _inner[key])).ToArray().CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        _order.Select(key => new KeyValuePair<TKey, TValue>(key, _inner[key])).GetEnumerator();

    /// <summary>
    /// 移动指定索引的项目，保留原有键和值，仅改变枚举和序列化顺序。
    /// </summary>
    /// <param name="oldIndex">原索引。</param>
    /// <param name="newIndex">移动后的索引。</param>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Count)
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        if (newIndex < 0 || newIndex >= Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (oldIndex == newIndex)
            return;

        var key = _order[oldIndex];
        _order.RemoveAt(oldIndex);
        _order.Insert(newIndex, key);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Move, new KeyValuePair<TKey, TValue>(key, _inner[key]), newIndex, oldIndex));
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
#if NETCOREAPP
        if (_inner.Remove(key, out var value))
#else
        if (_inner.TryGetValue(key, out var value) && _inner.Remove(key))
#endif
        {
            _order.RemoveAt(_order.FindIndex(storedKey => _inner.Comparer.Equals(storedKey, key)));
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
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index)
    {
        var items = array is DictionaryEntry[]
            ? (Array)_order.Select(key => new DictionaryEntry(key, _inner[key])).ToArray()
            : _order.Select(key => new KeyValuePair<TKey, TValue>(key, _inner[key])).ToArray();
        ((ICollection)items).CopyTo(array, index);
    }

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
    IDictionaryEnumerator IDictionary.GetEnumerator() => new OrderedEnumerator(GetEnumerator());

    /// <inheritdoc/>
    void IDictionary.Remove(object key) => Remove((TKey)key);

    private void NotifyAdd(TKey key, TValue value)
    {
        _order.Add(key);
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

    private sealed class OrderedCollection<T>(ObservableDictionary<TKey, TValue> owner,
        Func<KeyValuePair<TKey, TValue>, T> selector, Func<T, bool> contains) : ICollection<T>, ICollection
    {
        public int Count => owner.Count;
        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => ((ICollection)owner).SyncRoot;
        public bool Contains(T item) => contains(item);
        public IEnumerator<T> GetEnumerator() => owner.Select(selector).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void CopyTo(T[] array, int arrayIndex) => owner.Select(selector).ToArray().CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ((ICollection)owner.Select(selector).ToArray()).CopyTo(array, index);
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }

    private sealed class OrderedEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator) : IDictionaryEnumerator, IDisposable
    {
        public DictionaryEntry Entry => new(enumerator.Current.Key, enumerator.Current.Value);
        public object Key => enumerator.Current.Key;
        public object? Value => enumerator.Current.Value;
        public object Current => Entry;
        public bool MoveNext() => enumerator.MoveNext();
        public void Reset() => enumerator.Reset();
        public void Dispose() => enumerator.Dispose();
    }
}
