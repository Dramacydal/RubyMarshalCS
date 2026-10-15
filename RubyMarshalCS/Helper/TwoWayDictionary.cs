namespace RubyMarshalCS.Helper;

public class TwoWayDictionary<T, TV>
{
    private readonly Dictionary<T, TV> _TtoTV = new();

    // reading a file only needs the way from the index to the value; the way back is made when it is asked for the first time
    private Dictionary<TV, T>? _TVtoT;

    private Dictionary<TV, T> Reverse
    {
        get
        {
            if (_TVtoT == null)
            {
                var reverse = new Dictionary<TV, T>(_TtoTV.Count);
                foreach (var (index, value) in _TtoTV)
                    reverse[value] = index;

                _TVtoT = reverse;
            }

            return _TVtoT;
        }
    }

    public bool TryGetValue(T index, out TV? value) => _TtoTV.TryGetValue(index, out value);

    public bool TryGetValue(TV index, out T? value) => Reverse.TryGetValue(index, out value);

    public void Add(T index, TV value)
    {
        // an index that gets another value: the old value does not lead back to it any more
        if (_TVtoT != null && _TtoTV.TryGetValue(index, out var previous))
            ForgetWayBack(previous, index);

        _TtoTV[index] = value;
        if (_TVtoT != null)
            _TVtoT[value] = index;
    }

    public void Remove(T index)
    {
        if (_TtoTV.Remove(index, out var val) && _TVtoT != null)
            ForgetWayBack(val, index);
    }

    // the way back of a value is dropped only if it leads to this index (the same value can be under another index too)
    private void ForgetWayBack(TV value, T index)
    {
        if (_TVtoT!.TryGetValue(value, out var back) && EqualityComparer<T>.Default.Equals(back, index))
            _TVtoT.Remove(value);
    }

    public void Remove(TV index)
    {
        if (Reverse.Remove(index, out var val))
            _TtoTV.Remove(val);
    }

    public void Clear()
    {
        _TtoTV.Clear();
        _TVtoT = null;
    }
}
