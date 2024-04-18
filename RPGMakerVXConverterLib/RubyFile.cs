using RPGMakerVXConverterLib.Entities;
using RPGMakerVXConverterLib.Enums;
using RPGMakerVXConverterLib.Exceptions;

namespace RPGMakerVXConverterLib;

public class RubyFile
{
    private static int _contextCounter = 0;

    private int context;

    public readonly BinaryReader Reader;
    public readonly BinaryWriter Writer;

    public AbstractEntity Root { get; set; }
    
    public EntityFactory Factory { get; }

    public RubyFile(BinaryReader r)
    {
        Factory = new EntityFactory();

        Reader = r;

        _read();
    }

    private void _read()
    {
        var version = Reader.ReadUInt16();
        if (version != 0x804)
        {
            throw new ValidationException($"Wrong version: {version}");
        }

        Root = Read();
    }

    private void _write()
    {
        Writer.Write((ushort)0x804);
        Write(Root);
    }
    
    public AbstractEntity Read()
    {
        var code = Reader.ReadByte();
        var e = Factory.Create((RubyCodes)code);
        e.ReadData(this);

        return e;
    }

    public void Write(AbstractEntity e)
    {
        switch (e.Code)
        {
            
        }
        
        throw new Exception("Not implemented");
    }
}
