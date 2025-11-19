namespace RubyMarshalCS.Helper;

public class TwoWayDictionary<T, TV>
{
    private readonly Dictionary<T, TV> _TtoTV = new();
    private readonly Dictionary<TV, T> _TVtoT = new();

    public bool TryGetValue(T index, out TV? value) => _TtoTV.TryGetValue(index, out value);

    public bool TryGetValue(TV index, out T? value) => _TVtoT.TryGetValue(index, out value);

    public void Add(T index, TV value)
    {
        _TtoTV[index] = value;
        _TVtoT[value] = index;
    }

    public void Remove(T index)
    {
        if (_TtoTV.Remove(index, out var val))
            _TVtoT.Remove(val);
    }

    public void Remove(TV index)
    {
        if (_TVtoT.Remove(index, out var val))
            _TtoTV.Remove(val);
    }

    public void Clear()
    {
        _TtoTV.Clear();
        _TVtoT.Clear();
    }
}