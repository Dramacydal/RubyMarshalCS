﻿using RPGMakerVXConverterLib.Entities;

namespace RPGMakerVXConverterLib;

public static class ReaderExtensions
{
    public static int ReadFixNum(this BinaryReader r)
    {
        var e = new RubyFixNum();
        e.ReadData(r);
        return e.Value;
    }
    
    public static void WriteFixNum(this BinaryWriter r, int value)
    {
        var e = new RubyFixNum();
        e.Value = value;
        e.WriteData(r);
    }
}