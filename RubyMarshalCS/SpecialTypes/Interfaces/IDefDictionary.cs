using System.Collections;

namespace RubyMarshalCS.SpecialTypes.Interfaces;

public interface IDefDictionary : IDictionary
{
    public object? DefaultValue { get; set; }
}
