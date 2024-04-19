using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyUserDefined : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.UserDefined;

    public override void ReadData(BinaryReader r)
    {
        ClassName = Context.Read(r);
        
        var len = r.ReadFixNum();
        Bytes = r.ReadBytes(len);
    }

    public override void WriteData(BinaryWriter w)
    {
        Context.Write(w, ClassName);
        w.WriteFixNum(Bytes.Length);
        w.Write(Bytes);
    }
}
