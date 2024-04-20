﻿using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyFalse : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.False;

    public override void ReadData(BinaryReader r)
    {
    }

    public override void WriteData(BinaryWriter w)
    {
    }
}