using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFloat : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Float;
    
    public double Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }
}