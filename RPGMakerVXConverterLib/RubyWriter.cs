using RPGMakerVXConverterLib.Entities;

namespace RPGMakerVXConverterLib;

public class RubyWriter
{
    protected readonly BinaryWriter Writer;

    public AbstractEntity Root { get; set; }
    
    private EntityFactory Factory { get; }

    public RubyWriter(BinaryWriter w)
    {
        Factory = new EntityFactory();

        Writer = w;
    }

    public void Write()
    {
        Writer.Write((ushort)0x804);

        Factory.Write(Writer, Root);
    }
}
