using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ClassIsland.Shared.ComponentModels;

/// <summary>
/// 同时实现 <see cref="IDictionary"/>、<see cref="IList"/>、<see cref="INotifyCollectionChanged"/> 的有序字典结构。
/// </summary>
/// <typeparam name="TKey">字典键类型</typeparam>
/// <typeparam name="TValue">字典值类型</typeparam>
public class ObservableOrderedDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    IList<KeyValuePair<TKey, TValue>>,
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IDictionary
    where TKey : notnull
{
    private const string IndexerName = "Item[]";

    private readonly List<KeyValuePair<TKey, TValue>> _items;
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly KeyCollection _keys;
    private readonly ValueCollection _values;
    private readonly object _syncRoot = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableOrderedDictionary{TKey, TValue}"/> class.
    /// </summary>
    public ObservableOrderedDictionary()
        : this(0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableOrderedDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the collection can contain.</param>
    public ObservableOrderedDictionary(int capacity)
    {
        _items = new List<KeyValuePair<TKey, TValue>>(capacity);
        _dictionary = new Dictionary<TKey, TValue>(capacity);
        _keys = new KeyCollection(this);
        _values = new ValueCollection(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableOrderedDictionary{TKey, TValue}"/> class using an
    /// <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary whose elements are copied to the new collection.</param>
    /// <param name="comparer">The comparer to use when comparing keys.</param>
    public ObservableOrderedDictionary(
        IDictionary<TKey, TValue> dictionary,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        _items = new List<KeyValuePair<TKey, TValue>>(dictionary.Count);
        _dictionary = new Dictionary<TKey, TValue>(
            dictionary.Count,
            comparer ?? EqualityComparer<TKey>.Default);
        _keys = new KeyCollection(this);
        _values = new ValueCollection(this);

        foreach (var item in dictionary)
        {
            _dictionary.Add(item.Key, item.Value);
            _items.Add(item);
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
    public int Count => _items.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public ICollection<TKey> Keys => _keys;

    /// <inheritdoc/>
    public ICollection<TValue> Values => _values;

    bool IDictionary.IsFixedSize => false;

    bool IDictionary.IsReadOnly => false;

    ICollection IDictionary.Keys => _keys;

    ICollection IDictionary.Values => _values;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => _syncRoot;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _values;

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            if (_dictionary.TryGetValue(key, out _))
            {
                var index = IndexOfKey(key);
                var oldItem = _items[index];
                var newItem = new KeyValuePair<TKey, TValue>(oldItem.Key, value);

                _dictionary[oldItem.Key] = value;
                _items[index] = newItem;

                NotifyReplace(newItem, oldItem, index, false);
            }
            else
            {
                Add(key, value);
            }
        }
    }

    /// <inheritdoc/>
    public KeyValuePair<TKey, TValue> this[int index]
    {
        get => _items[index];
        set
        {
            var oldItem = _items[index];
            var keyChanged = !_dictionary.Comparer.Equals(oldItem.Key, value.Key);
            var newItem = value;

            if (keyChanged)
            {
                _dictionary.Add(value.Key, value.Value);
                _dictionary.Remove(oldItem.Key);
            }
            else
            {
                _dictionary[oldItem.Key] = value.Value;
                newItem = new KeyValuePair<TKey, TValue>(oldItem.Key, value.Value);
            }

            _items[index] = newItem;
            NotifyReplace(newItem, oldItem, index, keyChanged);
        }
    }

    object? IDictionary.this[object key]
    {
        get
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return key is TKey typedKey && _dictionary.TryGetValue(typedKey, out var value)
                ? value
                : null;
        }
        set => this[CastKey(key)] = CastValue(value);
    }

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        Insert(_items.Count, new KeyValuePair<TKey, TValue>(key, value));
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Insert(_items.Count, item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _dictionary.Clear();
        _items.Clear();

        NotifyCountAndContentsChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key, out var value) &&
               EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(KeyValuePair<TKey, TValue> item)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var current = _items[i];
            if (_dictionary.Comparer.Equals(current.Key, item.Key) &&
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
        if (index < 0 || index > _items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _dictionary.Add(item.Key, item.Value);
        _items.Insert(index, item);

        NotifyCountAndContentsChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            item,
            index));
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        if (!_dictionary.TryGetValue(key, out _))
        {
            return false;
        }

        var index = IndexOfKey(key);
        RemoveAt(index);
        return true;
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Contains(item) && Remove(item.Key);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        _dictionary.Remove(item.Key);

        NotifyCountAndContentsChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            item,
            index));
    }

    /// <summary>
    /// Moves the item at the specified index to a new index.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">The zero-based destination index.</param>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        }

        if (newIndex < 0 || newIndex >= _items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        }

        if (oldIndex == newIndex)
        {
            return;
        }

        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);

        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(IndexerName);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Move,
            item,
            newIndex,
            oldIndex));
    }

    /// <inheritdoc/>
#if NETCOREAPP
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#else
    public bool TryGetValue(TKey key, out TValue value)
#endif
        => _dictionary.TryGetValue(key, out value);

    /// <summary>
    /// Raises the <see cref="CollectionChanged"/> event.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    void IDictionary.Add(object key, object? value) => Add(CastKey(key), CastValue(value));

    bool IDictionary.Contains(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return key is TKey typedKey && ContainsKey(typedKey);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(_items.GetEnumerator());

    void IDictionary.Remove(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key is TKey typedKey)
        {
            Remove(typedKey);
        }
    }

    private static TKey CastKey(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key is TKey typedKey)
        {
            return typedKey;
        }

        throw new ArgumentException($"The value is not of type {typeof(TKey)}.", nameof(key));
    }

    private static TValue CastValue(object? value)
    {
        if (value is TValue typedValue)
        {
            return typedValue;
        }

        if (value == null && default(TValue) is null)
        {
            return default!;
        }

        throw new ArgumentException($"The value is not of type {typeof(TValue)}.", nameof(value));
    }

    private int IndexOfKey(TKey key)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_dictionary.Comparer.Equals(_items[i].Key, key))
            {
                return i;
            }
        }

        throw new InvalidOperationException("The ordered dictionary is in an invalid state.");
    }

    private void NotifyCountAndContentsChanged()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(IndexerName);
    }

    private void NotifyReplace(
        KeyValuePair<TKey, TValue> newItem,
        KeyValuePair<TKey, TValue> oldItem,
        int index,
        bool keyChanged)
    {
        if (keyChanged)
        {
            OnPropertyChanged(nameof(Keys));
        }

        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(IndexerName);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Replace,
            newItem,
            oldItem,
            index));
    }

    private sealed class KeyCollection :
        ICollection<TKey>,
        ICollection
    {
        private readonly ObservableOrderedDictionary<TKey, TValue> _owner;

        public KeyCollection(ObservableOrderedDictionary<TKey, TValue> owner)
        {
            _owner = owner;
        }

        public int Count => _owner.Count;

        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => _owner._syncRoot;

        public bool Contains(TKey item) => _owner.ContainsKey(item);

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The destination array is not large enough.", nameof(array));
            }

            for (var i = 0; i < _owner._items.Count; i++)
            {
                array[arrayIndex + i] = _owner._items[i].Key;
            }
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            foreach (var item in _owner._items)
            {
                yield return item.Key;
            }
        }

        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            var snapshot = new TKey[Count];
            CopyTo(snapshot, 0);
            ((ICollection)snapshot).CopyTo(array, index);
        }
    }

    private sealed class ValueCollection :
        ICollection<TValue>,
        ICollection
    {
        private readonly ObservableOrderedDictionary<TKey, TValue> _owner;

        public ValueCollection(ObservableOrderedDictionary<TKey, TValue> owner)
        {
            _owner = owner;
        }

        public int Count => _owner.Count;

        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => _owner._syncRoot;

        public bool Contains(TValue item)
        {
            var comparer = EqualityComparer<TValue>.Default;
            foreach (var current in _owner._items)
            {
                if (comparer.Equals(current.Value, item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The destination array is not large enough.", nameof(array));
            }

            for (var i = 0; i < _owner._items.Count; i++)
            {
                array[arrayIndex + i] = _owner._items[i].Value;
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var item in _owner._items)
            {
                yield return item.Value;
            }
        }

        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            var snapshot = new TValue[Count];
            CopyTo(snapshot, 0);
            ((ICollection)snapshot).CopyTo(array, index);
        }
    }

    private sealed class DictionaryEnumerator :
        IDictionaryEnumerator,
        IDisposable
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

        public DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
        {
            _enumerator = enumerator;
        }

        public DictionaryEntry Entry => new(Key, Value);

        public object Key => _enumerator.Current.Key;

        public object? Value => _enumerator.Current.Value;

        public object Current => Entry;

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public void Dispose() => _enumerator.Dispose();
    }
}
