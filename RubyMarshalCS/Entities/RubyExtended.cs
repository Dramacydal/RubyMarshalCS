using System.Diagnostics;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyExtended : AbstractEntity
{
    public AbstractEntity ModuleName { get; set; }
    public AbstractEntity Object { get; set; }
    public AbstractEntity Module { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Extended;

    public override void ReadData(BinaryReader reader)
    {
        ModuleName = Context.Read(reader);
        Debug.Assert(ModuleName.Code == RubyCodes.SymbolLink || ModuleName.Code == RubyCodes.Symbol);

        Object = Context.Read(reader);
        // Debug.Assert(Object.Code != RubyCodes.SymbolLink && Object.Code != RubyCodes.ObjectLink);
        if(Object.Code != RubyCodes.Array)
        {
            return;
        }
        
        return;
        // Module = Context.Read(reader);
    }

    public override void WriteData(BinaryWriter writer)
    {
        Context.Write(writer, ModuleName);
        Context.Write(writer, Object, true);
     //   Context.Write(writer, Module);
    }
}