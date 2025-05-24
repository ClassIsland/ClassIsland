using System.Collections.Specialized;
using System.ComponentModel;

namespace ClassIsland.Shared;

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged,
    INotifyPropertyChanged
{
    public ObservableDictionary()
        : base()
    {
    }

    private int _index;
    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public new KeyCollection Keys => base.Keys;

    public new ValueCollection Values => base.Values;

    public new int Count => base.Count;

    public new TValue this[TKey key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }

    public TValue this[int index]
    {
        get => GetIndexValue(index);
        set => SetIndexValue(index, value);
    }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, FindPair(key), _index));
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
        OnPropertyChanged("Count");
    }

    public new void Clear()
    {
        base.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
        OnPropertyChanged("Count");
    }

    public new bool Remove(TKey key)
    {
        var pair = FindPair(key);
        if (base.Remove(key))
        {
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, pair, _index));
            OnPropertyChanged("Keys");
            OnPropertyChanged("Values");
            OnPropertyChanged("Count");
            return true;
        }

        return false;
    }

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (CollectionChanged != null) CollectionChanged(this, e);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #region private方法

    private TValue GetIndexValue(int index)
    {
        for (var i = 0; i < Count; i++)
            if (i == index)
            {
                var pair = this.ElementAt(i);
                return pair.Value;
            }

        return default;
    }

    private void SetIndexValue(int index, TValue value)
    {
        try
        {
            var pair = this.ElementAtOrDefault(index);
            SetValue(pair.Key, value);
        }
        catch (Exception)
        {
        }
    }

    private TValue GetValue(TKey key)
    {
        if (ContainsKey(key))
            return base[key];
        else
            return default;
    }

    private void SetValue(TKey key, TValue value)
    {
        if (ContainsKey(key))
        {
            var pair = FindPair(key);
            var index = _index;
            base[key] = value;
            var newpair = FindPair(key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newpair,
                pair, index));
            OnPropertyChanged("Values");
            OnPropertyChanged("Item[]");
        }
        else
        {
            Add(key, value);
        }
    }

    private KeyValuePair<TKey, TValue> FindPair(TKey key)
    {
        _index = 0;
        foreach (var item in this)
        {
            if (item.Key.Equals(key)) return item;
            _index++;
        }

        return default;
    }

    private int IndexOf(TKey key)
    {
        var index = 0;
        foreach (var item in this)
        {
            if (item.Key.Equals(key)) return index;
            index++;
        }

        return -1;
    }

    #endregion
}