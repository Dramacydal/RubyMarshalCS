using RubyMarshalCS.SpecialTypes.Interfaces;

namespace RubyMarshalCS.SpecialTypes;

public class DefDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDefDictionary
{
    public object DefaultValue { get; set; }
}
