﻿using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyTrue : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.True;

    public override void ReadData(BinaryReader r)
    {
    }

    public override void WriteData(BinaryWriter w)
    {
    }
}