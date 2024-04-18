using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyUserDefined : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.UserDefined;

    public override void ReadData(RubyFile r)
    {
        ClassName = r.Read();
        
        var len = r.ReadPackedInt();
        Bytes = r.Reader.ReadBytes(len);
    }

    public override void WriteData(RubyFile f)
    {
        f.Write(ClassName);
        f.WritePackedInt(Bytes.Length);
        f.Writer.Write(Bytes);
    }
}
