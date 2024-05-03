using System.Diagnostics;
using System.Text;
using RubyMarshalCS;
using RubyMarshalCS.Settings;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS;

public class RubyReader
{
    private readonly SerializationSettings _settings;
    private readonly BinaryReader _reader;
    private SerializationContext _serializationContext;

    public RubyReader(BinaryReader reader, SerializationSettings? settings = null)
    {
        _reader = reader;
        _settings = settings ?? new();
    }

    public AbstractEntity Read()
    {
        var version = _reader.ReadUInt16();
        if (version != 0x804)
            throw new Exception($"Wrong ruby serializer version: {version}");

        ReaderContext readContext = new()
        {
            HasIVars = false,
            Action = ReadEntity,
        };

        _serializationContext = new SerializationContext(_settings);

        var entity = ReadEntity(readContext);

        if (_settings.EnsureEOF && _reader.BaseStream.Position != _reader.BaseStream.Length)
            throw new Exception("Trailing data detected");

        return entity;
    }

    private AbstractEntity ReadEntity(ReaderContext context)
    {
        var code = (RubyCodes)_reader.ReadByte();

        switch (code)
        {
            case RubyCodes.Nil:
                return _serializationContext.Create(RubyCodes.Nil);
            case RubyCodes.True:
                return _serializationContext.Create(RubyCodes.True);
            case RubyCodes.False:
                return _serializationContext.Create(RubyCodes.False);
            case RubyCodes.FixNum:
                return ReadFixNum(context);
            case RubyCodes.Float:
                return ReadFloat(context);
            case RubyCodes.Object:
                return ReadObject(context);
            case RubyCodes.ObjectLink:
                return ReadObjectLink(context);
            case RubyCodes.InstanceVar:
                return ReadIVar(context);
            case RubyCodes.Symbol:
                return ReadSymbol(context);
            case RubyCodes.SymbolLink:
                return ReadSymbolLink(context);
            case RubyCodes.String:
                return ReadString(context);
            case RubyCodes.RegExp:
                return ReadRegExp(context);
            case RubyCodes.Array:
                return ReadArray(context);
            case RubyCodes.Hash:
                return ReadHash(context);
            case RubyCodes.HashDef:
                return ReadHashDef(context);
            case RubyCodes.UserDefined:
                return ReadUserDefined(context);
            case RubyCodes.UserMarshal:
                return ReadUserMarshal(context);
            case RubyCodes.Class:
                return ReadClass(context);
            case RubyCodes.Extended:
                return ReadExtended(context);
            case RubyCodes.BigNum:
                return ReadBigNum(context);
            case RubyCodes.Module:
                return ReadModule(context);
            case RubyCodes.ModuleOld:
                return ReadModuleOld(context);
            case RubyCodes.Struct:
                return ReadStruct(context);
            case RubyCodes.Data:
                return ReadData(context);
            case RubyCodes.UClass:
                return ReadUClass(context);

        }

        throw new Exception($"Unsupported entity: {code}");
    }

    private AbstractEntity ReadUClass(ReaderContext context)
    {
        var className = context.WithSubContext(false, r_symbol);

        var entity = r_object(context);
        entity.UserClass = className;

        return entity;
    }

    private AbstractEntity ReadData(ReaderContext context)
    {
        var entity = (RubyData)_serializationContext.Create(RubyCodes.Data);
        entity.ClassName = context.WithSubContext(false, r_symbol);

        _serializationContext.RememberObject(entity);

        entity.Object = r_object(context);

        return entity;
    }

    private AbstractEntity ReadStruct(ReaderContext context)
    {
        var entity = (RubyStruct)_serializationContext.Create(RubyCodes.Struct);
        entity.Name = context.WithSubContext(false, r_symbol);

        _serializationContext.RememberObject(entity);

        var fieldsCount = _reader.ReadFixNum();
        for (var i = 0; i < fieldsCount; ++i)
        {
            var key = context.WithSubContext(false, r_symbol);
            var value = r_object(context);

            entity.Fields.Add(new(key, value));
        }

        return entity;
    }

    private AbstractEntity ReadModuleOld(ReaderContext context)
    {
        var entity = (RubyModuleOld)_serializationContext.Create(RubyCodes.ModuleOld);
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadModule(ReaderContext context)
    {
        var entity = (RubyModule)_serializationContext.Create(RubyCodes.Module);
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadRegExp(ReaderContext context)
    {
        var entity = (RubyRegExp)_serializationContext.Create(RubyCodes.RegExp);

        entity.Bytes = _reader.ReadByteSequence();
        entity.Options = (RubyRegexpOptions)_reader.ReadByte();

        _serializationContext.RememberObject(entity);

        if (context.HasIVars)
        {
            context.WithSubContext(false, readContext => { entity.InstanceVariables.AddRange(r_ivar(readContext)); });
            // context.HasIVars = false;
        }

        return entity;
    }

    private AbstractEntity ReadBigNum(ReaderContext context)
    {
        var entity = (RubyBigNum)_serializationContext.Create(RubyCodes.BigNum);
        _serializationContext.RememberObject(entity);

        entity.Value = _reader.ReadPackedBigInteger();

        return entity;
    }

    private AbstractEntity ReadExtended(ReaderContext context, List<AbstractEntity>? modules = null)
    {
        modules ??= new();

        var moduleName = context.WithSubContext(false, r_symbol);
        modules.Add(moduleName);

        var code = (RubyCodes)_reader.ReadByte();
        if (code == RubyCodes.Extended)
            return ReadExtended(context, modules);
     
        _reader.BaseStream.Seek(-1, SeekOrigin.Current);

        var obj = context.Action(context);
        obj.Modules.AddRange(modules);

        return obj;
    }

    private AbstractEntity ReadClass(ReaderContext context)
    {
        var entity = (RubyClass)_serializationContext.Create(RubyCodes.Class);
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadUserMarshal(ReaderContext context)
    {
        var entity = (RubyUserMarshal)_serializationContext.Create(RubyCodes.UserMarshal);
        entity.ClassName = context.WithSubContext(false, r_symbol);
        
        _serializationContext.RememberObject(entity);

        entity.Object = r_object(context);

        return entity;
    }

    private AbstractEntity ReadUserDefined(ReaderContext context)
    {
        var entity = (RubyUserDefined)_serializationContext.Create(RubyCodes.UserDefined);
        entity.ClassName = context.WithSubContext(false, r_symbol);
        entity.Bytes = _reader.ReadByteSequence();

        _serializationContext.RememberObject(entity);

        if (context.HasIVars)
        {
            context.WithSubContext(false, readContext => { entity.InstanceVariables.AddRange(r_ivar(readContext)); });
            // context.HasIVars = false;
        }

        return entity;
    }

    private AbstractEntity ReadHashDef(ReaderContext context)
    {
        var entity = (RubyHash)_serializationContext.Create(RubyCodes.Hash);
        _serializationContext.RememberObject(entity);
        
        var len = _reader.ReadFixNum();
        for (var i = 0; i < len; ++i)
        {
            var key = r_object(context);
            var val = r_object(context);

            entity.Pairs.Add(new(key, val));
        }

        entity.Default = r_object(context);

        return entity;
    }
    
    private AbstractEntity ReadHash(ReaderContext context)
    {
        var entity = (RubyHash)_serializationContext.Create(RubyCodes.Hash);
        _serializationContext.RememberObject(entity);
        
        var len = _reader.ReadFixNum();
        for (var i = 0; i < len; ++i)
        {
            var key = r_object(context);
            var val = r_object(context);

            entity.Pairs.Add(new(key, val));
        }

        return entity;
    }

    private AbstractEntity ReadArray(ReaderContext context)
    {
        var entity = (RubyArray)_serializationContext.Create(RubyCodes.Array);
        _serializationContext.RememberObject(entity);
        
        var len = _reader.ReadFixNum();
        for (var i = 0; i < len; ++i)
            entity.Elements.Add(r_object(context));

        return entity;
    }

    private AbstractEntity ReadString(ReaderContext context)
    {
        var entity = (RubyString)_serializationContext.Create(RubyCodes.String);
        entity.Bytes = _reader.ReadByteSequence();

        _serializationContext.RememberObject(entity);

        if (context.HasIVars)
        {
            context.WithSubContext(false, readContext => { entity.InstanceVariables.AddRange(r_ivar(readContext)); });
            // context.HasIVars = false;
        }
        
        if (Encoding.ASCII.GetString(entity.Bytes) == "A" && entity.InstanceVariables.Count == 0)
        {
            Debug.WriteIf(false,"");
        }

        return entity;
    }

    private AbstractEntity ReadObjectLink(ReaderContext context)
    {
        var entity = (RubyObjectLink)_serializationContext.Create(RubyCodes.ObjectLink);
        entity.ReferenceId = _reader.ReadFixNum();
        if (_settings.ResolveLinks)
            return _serializationContext.LookupRememberedObject(entity);

        return entity;
    }

    private AbstractEntity ReadSymbolLink(ReaderContext context)
    {
        var entity = (RubySymbolLink)_serializationContext.Create(RubyCodes.SymbolLink);
        entity.ReferenceId = _reader.ReadFixNum();
        if (_settings.ResolveLinks)
            return _serializationContext.LookupRememberedSymbol(entity);

        return entity;
    }

    private AbstractEntity ReadSymbol(ReaderContext context)
    {
        var entity = (RubySymbol)_serializationContext.Create(RubyCodes.Symbol);
        entity.Value = _reader.ReadByteSequence();

        _serializationContext.RememberSymbol(entity);

        if (context.HasIVars)
        {
            context.WithSubContext(false, readContext => { entity.InstanceVariables.AddRange(r_ivar(readContext)); });
            // context.HasIVars = false;
        }

        return entity;
    }

    private AbstractEntity ReadIVar(ReaderContext context)
    {
        return context.WithSubContext(true, readContext =>
        {
            var entity = readContext.Action(readContext);
            if (readContext.HasIVars && IsMarshalExtendable(entity))
            {
                context.WithSubContext(false, marshalReadContext =>
                {
                    entity.InstanceVariables.AddRange(r_ivar(marshalReadContext));

                    return entity;
                });
            }

            return entity;
        });
    }

    private bool IsMarshalExtendable(AbstractEntity entity)
    {
        switch (entity.Code)
        {
            case RubyCodes.Hash:
            case RubyCodes.HashDef:
            case RubyCodes.Object:
            case RubyCodes.Struct:
                return true;
            default:
                return false;
        }
    }

    private AbstractEntity ReadObject(ReaderContext context)
    {
        var className = context.WithSubContext(false, r_symbol);

        var entity = (RubyObject)_serializationContext.Create(RubyCodes.Object);
        entity.ClassName = className;

        _serializationContext.RememberObject(entity);

        entity.Attributes.AddRange(r_ivar(context));

        return entity;
    }

    private AbstractEntity ReadFixNum(ReaderContext context)
    {
        var entity = (RubyFixNum)_serializationContext.Create(RubyCodes.FixNum);
        entity.Value = _reader.ReadFixNum();

        return entity;
    }

    private AbstractEntity ReadFloat(ReaderContext context)
    {
        var entity = (RubyFloat)_serializationContext.Create(RubyCodes.Float);
        _serializationContext.RememberObject(entity);
        
        entity.Value = _reader.ReadPackedFloat();

        return entity;
    }

    private AbstractEntity r_symbol(ReaderContext context)
    {
        var code = (RubyCodes)_reader.ReadByte();

        switch (code)
        {
            case RubyCodes.InstanceVar:
                return context.WithSubContext(true, r_symbol);
            case RubyCodes.Symbol:
                return ReadSymbol(context);
            case RubyCodes.SymbolLink:
                return ReadSymbolLink(context);
            default:
                throw new Exception("Unsupported object r_symbol: {code}");
        }
    }

    private AbstractEntity r_object(ReaderContext context, bool hasIVar = false)
    {
        return context.WithSubContext(hasIVar, context.Action);
    }

    private List<KeyValuePair<AbstractEntity, AbstractEntity>> r_ivar(ReaderContext context)
    {
        List<KeyValuePair<AbstractEntity, AbstractEntity>> ret = new();

        var count = _reader.ReadFixNum();

        for (var i = 0; i < count; ++i)
        {
            var key = r_symbol(context);
            var val = r_object(context);

            ret.Add(new(key, val));
        }

        return ret;
    }
}