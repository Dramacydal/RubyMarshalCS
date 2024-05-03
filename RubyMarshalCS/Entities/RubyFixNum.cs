﻿using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFixNum : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.FixNum;

    public int Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }
}
