using RubyMarshalCS.Settings;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS;

public class RubyWriter
{
    private const ushort HeaderMagic = 0x804;
    
    private readonly BinaryWriter _writer;
    private readonly SerializationSettings _settings;
    private SerializationContext _context;

    public RubyWriter(BinaryWriter writer, SerializationSettings? settings = null)
    {
        _writer = writer;
        _settings = settings ?? new();
    }

    public void Write(AbstractEntity entity)
    {
        _context = entity.Context;
        _context.Reset();

        _writer.Write(HeaderMagic);

        WriteEntity(entity);
    }

    private void WriteEntity(AbstractEntity entity)
    {
        switch (entity.Code)
        {
            case RubyCodes.Nil:
            case RubyCodes.True:
            case RubyCodes.False:
                _writer.Write((byte)entity.Code);
                break;
            case RubyCodes.FixNum:
                WriteFixNum((RubyFixNum)entity);
                break;
            case RubyCodes.BigNum:
                WriteWrapper(entity, () => WriteBigNum((RubyBigNum)entity));
                break;
            case RubyCodes.Float:
                WriteWrapper(entity, () => WriteFloat((RubyFloat)entity));
                break;
            case RubyCodes.Symbol:
                WriteSymbol((RubySymbol)entity);
                break;
            case RubyCodes.String:
                WriteWrapper(entity, () => WriteString((RubyString)entity));
                break;
            case RubyCodes.RegExp:
                WriteWrapper(entity, () => WriteRegExp((RubyRegExp)entity));
                break;
            case RubyCodes.Array:
                WriteWrapper(entity, () => WriteArray((RubyArray)entity));
                break;
            case RubyCodes.Hash:
                WriteWrapper(entity, () => WriteHash((RubyHash)entity));
                break;
            case RubyCodes.ModuleOld:
                WriteWrapper(entity, () => WriteModuleOld((RubyModuleOld)entity));
                break;
            case RubyCodes.Class:
                WriteWrapper(entity, () => WriteClass((RubyClass)entity));
                break;
            case RubyCodes.Module:
                WriteWrapper(entity, () => WriteModule((RubyModule)entity));
                break;
            case RubyCodes.UserDefined:
                WriteWrapper(entity, () => WriteUserDefined((RubyUserDefined)entity));
                break;
            case RubyCodes.UserMarshal:
                WriteWrapper(entity, () => WriteUserMarshal((RubyUserMarshal)entity));
                break;
            case RubyCodes.Struct:
                WriteWrapper(entity, () => WriteStruct((RubyStruct)entity));
                break;
            case RubyCodes.Object:
                WriteWrapper(entity, () => WriteObject((RubyObject)entity));
                break;
            case RubyCodes.Data:
                WriteWrapper(entity, () => WriteData((RubyData)entity));
                break;
            case RubyCodes.UClass:
            default:
                throw new Exception($"Unsupported entity: {entity.Code}");
        }
    }

    private void WriteData(RubyData entity)
    {
        w_class(RubyCodes.Data, entity.ClassName);
        WriteEntity(entity.Object);
    }

    private void WriteObject(RubyObject entity)
    {
        w_class(RubyCodes.Object, entity.ClassName);

        _writer.WriteFixNum(entity.Attributes.Count);
        foreach (var attr in entity.Attributes)
        {
            WriteEntity(attr.Key);
            WriteEntity(attr.Value);
        }
    }

    private void WriteStruct(RubyStruct entity)
    {
        w_class(RubyCodes.Struct, entity.Name);

        _writer.WriteFixNum(entity.Fields.Count);
        foreach (var field in entity.Fields)
        {
            WriteEntity(field.Key);
            WriteEntity(field.Value);
        }
    }

    private void WriteUserDefined(RubyUserDefined entity)
    {
        w_class(RubyCodes.UserDefined, entity.ClassName);
        _writer.Write(entity.Bytes);
    }
    
    private void WriteUserMarshal(RubyUserMarshal entity)
    {
        w_class(RubyCodes.UserMarshal, entity.ClassName);
        WriteEntity(entity.Object);
    }

    private void WriteModule(RubyModule entity)
    {
        _writer.Write((byte)RubyCodes.Module);
        _writer.Write(entity.Bytes);
    }

    private void WriteClass(RubyClass entity)
    {
        _writer.Write((byte)RubyCodes.Class);
        _writer.Write(entity.Bytes);
    }

    private void WriteModuleOld(RubyModuleOld entity)
    {
        _writer.Write((byte)RubyCodes.ModuleOld);
        _writer.Write(entity.Bytes);
    }

    private void WriteArray(RubyArray entity)
    {
        _writer.Write((byte)RubyCodes.Array);
        _writer.WriteFixNum(entity.Elements.Count);

        foreach (var e in entity.Elements)
            WriteEntity(e);
    }

    private void WriteRegExp(RubyRegExp entity)
    {
        _writer.Write((byte)RubyCodes.RegExp);
        _writer.Write(entity.Bytes);
        _writer.Write((byte)entity.Options);
    }

    private void WriteHash(RubyHash entity)
    {
        _writer.Write((byte)(entity.Default != null ? RubyCodes.HashDef : RubyCodes.Hash));
        _writer.WriteFixNum(entity.Pairs.Count);

        foreach (var pair in entity.Pairs)
        {
            WriteEntity(pair.Key);
            WriteEntity(pair.Value);
        }

        if (entity.Default != null)
            WriteEntity(entity.Default);
    }

    private void WriteString(RubyString entity)
    {
        _writer.Write((byte)RubyCodes.String);
        _writer.Write(entity.Bytes);
    }

    private void w_class(RubyCodes code, AbstractEntity className)
    {
        _writer.Write((byte)code);
        WriteEntity(className);
    }

    private void WriteSymbol(RubySymbol entity)
    {
        if (CheckWriteSymbolLink(entity))
            return;
        
        _context.RememberSymbol(entity);

        if (entity.InstanceVariables.Count > 0)
            _writer.Write((byte)RubyCodes.InstanceVar);
        
        _writer.Write((byte)RubyCodes.Symbol);
        _writer.Write(entity.Value);

        if (entity.InstanceVariables.Count > 0)
        {
            _writer.WriteFixNum(entity.InstanceVariables.Count);
            foreach (var v in entity.InstanceVariables)
            {
                WriteEntity(v.Key);
                WriteEntity(v.Value);
            }
        }
    }

    private void WriteFloat(RubyFloat entity)
    {
        _writer.Write((byte)RubyCodes.Float);
        _writer.WritePackedFloat(entity.Value);
    }

    private void WriteFixNum(RubyFixNum entity)
    {
        _writer.Write((byte)RubyCodes.FixNum);
        _writer.WriteFixNum(entity.Value);
    }

    private void WriteBigNum(RubyBigNum entity)
    {
        _writer.Write((byte)RubyCodes.BigNum);
        _writer.WritePackedBigInteger(entity.Value);
    }

    private void WriteWrapper(AbstractEntity entity, Action wrapper)
    {
        if (CheckWriteObjectLink(entity))
            return;
        
        _context.RememberObject(entity);
        
        if (entity.InstanceVariables.Count > 0)
            _writer.Write((byte)RubyCodes.InstanceVar);

        foreach (var t in entity.Modules)
        {
            _writer.Write((byte)RubyCodes.Extended);
            WriteEntity(t);
        }

        if (entity.UserClass != null)
        {
            _writer.Write((byte)RubyCodes.UClass);
            WriteEntity(entity.UserClass);
        }
        
        wrapper();

        if (entity.InstanceVariables.Count > 0)
        {
            _writer.WriteFixNum(entity.InstanceVariables.Count);
            foreach (var v in entity.InstanceVariables)
            {
                WriteEntity(v.Key);
                WriteEntity(v.Value);
            }
        }
    }
    
    private void WriteObjectLink(RubyObjectLink entity)
    {
        _writer.Write((byte)RubyCodes.ObjectLink);
        _writer.WriteFixNum(entity.ReferenceId);
    }

    private void WriteSymbolLink(RubySymbolLink entity)
    {
        _writer.Write((byte)RubyCodes.SymbolLink);
        _writer.WriteFixNum(entity.ReferenceId);
    }
    
    private bool CheckWriteObjectLink(AbstractEntity entity)
    {
        var storedIndex = _context.LookupStoredObjectIndex(entity);
        if (storedIndex == -1)
            return false;

        WriteObjectLink(new RubyObjectLink() { ReferenceId = storedIndex });

        return true;
    }
    
    private bool CheckWriteSymbolLink(AbstractEntity entity)
    {
        var storedIndex = _context.LookupStoredSymbolIndex(entity);
        if (storedIndex == -1)
            return false;

        WriteSymbolLink(new RubySymbolLink() { ReferenceId = storedIndex });

        return true;
    }
}