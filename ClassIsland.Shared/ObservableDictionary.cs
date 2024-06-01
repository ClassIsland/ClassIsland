using System.Collections.Specialized;
using System.ComponentModel;

namespace ClassIsland.Shared;

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
{
    public ObservableDictionary()
        : base()
    {
    }

    private int _index;
    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public new KeyCollection Keys
    {
        get
        {
            return base.Keys;
        }
    }

    public new ValueCollection Values
    {
        get
        {
            return base.Values;
        }
    }

    public new int Count
    {
        get
        {
            return base.Count;
        }
    }

    public new TValue this[TKey key]
    {
        get
        {
            return this.GetValue(key);
        }
        set
        {
            this.SetValue(key, value);
        }
    }

    public TValue this[int index]
    {
        get
        {
            return this.GetIndexValue(index);
        }
        set
        {
            this.SetIndexValue(index, value);
        }
    }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.FindPair(key), _index));
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
        OnPropertyChanged("Count");
    }

    public new void Clear()
    {
        base.Clear();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged("Keys");
        OnPropertyChanged("Values");
        OnPropertyChanged("Count");
    }

    public new bool Remove(TKey key)
    {
        var pair = this.FindPair(key);
        if (base.Remove(key))
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, pair, _index));
            OnPropertyChanged("Keys");
            OnPropertyChanged("Values");
            OnPropertyChanged("Count");
            return true;
        }
        return false;
    }

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (this.CollectionChanged != null)
        {
            this.CollectionChanged(this, e);
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        if (this.PropertyChanged != null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region private方法
    private TValue GetIndexValue(int index)
    {
        for (int i = 0; i < this.Count; i++)
        {
            if (i == index)
            {
                var pair = this.ElementAt(i);
                return pair.Value;
            }
        }

        return default(TValue);
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
        if (base.ContainsKey(key))
        {
            return base[key];
        }
        else
        {
            return default(TValue);
        }
    }

    private void SetValue(TKey key, TValue value)
    {
        if (base.ContainsKey(key))
        {
            var pair = this.FindPair(key);
            int index = _index;
            base[key] = value;
            var newpair = this.FindPair(key);
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newpair, pair, index));
            OnPropertyChanged("Values");
            OnPropertyChanged("Item[]");
        }
        else
        {
            this.Add(key, value);
        }
    }

    private KeyValuePair<TKey, TValue> FindPair(TKey key)
    {
        _index = 0;
        foreach (var item in this)
        {
            if (item.Key.Equals(key))
            {
                return item;
            }
            _index++;
        }
        return default(KeyValuePair<TKey, TValue>);
    }

    private int IndexOf(TKey key)
    {
        int index = 0;
        foreach (var item in this)
        {
            if (item.Key.Equals(key))
            {
                return index;
            }
            index++;

        }
        return -1;
    }
    

    #endregion

}