using RPGMakerVXConverterLib.Entities;
using RPGMakerVXConverterLib.Enums;
using RPGMakerVXConverterLib.Exceptions;

namespace RPGMakerVXConverterLib;

public class RubyReader
{
    protected readonly BinaryReader Reader;

    public AbstractEntity Root { get; set; }
    
    private EntityFactory Factory { get; }

    public RubyReader(BinaryReader r)
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

        Root = Factory.Read(Reader);
    }
}
