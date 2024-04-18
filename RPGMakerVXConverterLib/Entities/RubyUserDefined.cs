using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyUserDefined : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.UserDefined;

    public override void ReadData(BinaryReader r)
    {
        ClassName = Factory.Read(r);
        
        var len = r.ReadPackedInt();
        Bytes = r.ReadBytes(len);
    }

    public override void WriteData(BinaryWriter w)
    {
        Factory.Write(w, ClassName);
        w.WritePackedInt(Bytes.Length);
        w.Write(Bytes);
    }
}
