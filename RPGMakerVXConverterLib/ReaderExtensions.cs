using RPGMakerVXConverterLib.Entities;

namespace RPGMakerVXConverterLib;

public static class ReaderExtensions
{
    public static int ReadPackedInt(this BinaryReader r)
    {
        var e = new PackedInt();
        e.ReadData(r);
        return e.Value;
    }
    
    public static void WritePackedInt(this BinaryWriter r, int value)
    {
        var e = new PackedInt();
        e.Value = value;
        e.WriteData(r);
    }
}