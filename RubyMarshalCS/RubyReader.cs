using System.Diagnostics;
using System.Text;
using RubyMarshalCS;
using RubyMarshalCS.Settings;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS;

public class RubyReader
{
    private readonly DeserializationSettings _settings;
    private readonly BinaryReader _reader;
    private SerializationContext _serializationContext;

    public RubyReader(BinaryReader reader, DeserializationSettings? settings = null)
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

        _serializationContext = new SerializationContext();

        var entity = ReadEntity(readContext);

        if (_settings.EnsureReadToEnd && _reader.BaseStream.Position != _reader.BaseStream.Length)
            throw new Exception("Trailing data detected");

        return entity;
    }

    private AbstractEntity ReadEntity(ReaderContext context)
    {
        var code = (RubyCodes)_reader.ReadByte();

        switch (code)
        {
            case RubyCodes.Nil:
                return _serializationContext.Nil;
            case RubyCodes.True:
                return _serializationContext.True;
            case RubyCodes.False:
                return _serializationContext.False;
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
        var className = r_symbol(Plain(context));

        // the value inside a user class is the one that has the instance variables of the whole: it takes them or leaves them to the code that wrapped it
        var entity = r_object(context, context.HasIVars);
        entity.UserClass = className;

        return entity;
    }

    private AbstractEntity ReadData(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyData>();
        entity.ClassName = r_symbol(Plain(context));

        _serializationContext.RememberObject(entity);

        entity.Object = r_object(context);

        return entity;
    }

    private AbstractEntity ReadStruct(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyStruct>();
        entity.Name = r_symbol(Plain(context));

        _serializationContext.RememberObject(entity);

        var fieldsCount = _reader.ReadFixNum();
        for (var i = 0; i < fieldsCount; ++i)
        {
            var key = r_symbol(Plain(context));
            var value = r_object(context);

            entity.Fields.Add(new(key, value));
        }

        return entity;
    }

    private AbstractEntity ReadModuleOld(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyModuleOld>();
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadModule(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyModule>();
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadRegExp(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyRegExp>();

        entity.Bytes = _reader.ReadByteSequence();
        entity.Options = (RubyRegexpOptions)_reader.ReadByte();

        _serializationContext.RememberObject(entity);

        ReadOwnIVars(context, entity);

        return entity;
    }

    private AbstractEntity ReadBigNum(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyBigNum>();
        _serializationContext.RememberObject(entity);

        entity.Value = _reader.ReadPackedBigInteger();

        return entity;
    }

    private AbstractEntity ReadExtended(ReaderContext context, List<AbstractEntity>? modules = null)
    {
        modules ??= new();

        var moduleName = r_symbol(Plain(context));
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
        var entity = _serializationContext.Create<RubyClass>();
        _serializationContext.RememberObject(entity);

        entity.Bytes = _reader.ReadByteSequence();

        return entity;
    }

    private AbstractEntity ReadUserMarshal(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyUserMarshal>();
        entity.ClassName = r_symbol(Plain(context));
        
        _serializationContext.RememberObject(entity);

        entity.Object = r_object(context);

        return entity;
    }

    private AbstractEntity ReadUserDefined(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyUserDefined>();
        entity.ClassName = r_symbol(Plain(context));
        entity.Bytes = _reader.ReadByteSequence();

        _serializationContext.RememberObject(entity);

        ReadOwnIVars(context, entity);

        return entity;
    }

    private AbstractEntity ReadHashDef(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyHash>();
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
        var entity = _serializationContext.Create<RubyHash>();
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
        var entity = _serializationContext.Create<RubyArray>();
        _serializationContext.RememberObject(entity);
        
        var len = _reader.ReadFixNum();
        for (var i = 0; i < len; ++i)
            entity.Elements.Add(r_object(context));

        return entity;
    }

    private AbstractEntity ReadString(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyString>();
        entity.Bytes = _reader.ReadByteSequence();

        _serializationContext.RememberObject(entity);

        ReadOwnIVars(context, entity);

        return entity;
    }

    private AbstractEntity ReadObjectLink(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyObjectLink>();
        entity.ReferenceId = _reader.ReadFixNum();
        if (_settings.ResolveLinks)
            return _serializationContext.LookupRememberedObject(entity);

        return entity;
    }

    private AbstractEntity ReadSymbolLink(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubySymbolLink>();
        entity.ReferenceId = _reader.ReadFixNum();
        if (_settings.ResolveLinks)
            return _serializationContext.LookupRememberedSymbol(entity);

        return entity;
    }

    private AbstractEntity ReadSymbol(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubySymbol>();
        entity.Value = _reader.ReadByteSequence();

        _serializationContext.RememberSymbol(entity);

        ReadOwnIVars(context, entity);

        return entity;
    }

    private AbstractEntity ReadIVar(ReaderContext context)
    {
        return context.WithSubContext(true, readContext =>
        {
            var entity = readContext.Action(readContext);

            // the value did not take its instance variables itself (an array, a hash, an object, a struct, anything inside a user class...)
            ReadOwnIVars(readContext, entity, context);

            return entity;
        });
    }

    /// <summary>
    /// Reads the instance variables that follow a value written with the "has instance variables" code, once: a value that reads
    /// them itself (a string, a symbol, a regexp, a user defined one) does it as soon as it is read, for any other one the code
    /// that wrapped it (<see cref="ReadIVar"/>) does it after it. <paramref name="owner"/> is the context of the wrapping code.
    /// </summary>
    private void ReadOwnIVars(ReaderContext context, AbstractEntity entity, ReaderContext? owner = null)
    {
        if (!context.HasIVars || context.IVarsRead)
            return;

        context.IVarsRead = true;
        r_ivar(Plain(owner ?? context), entity.InstanceVariables);
    }

    private AbstractEntity ReadObject(ReaderContext context)
    {
        var className = r_symbol(Plain(context));

        var entity = _serializationContext.Create<RubyObject>();
        entity.ClassName = className;

        _serializationContext.RememberObject(entity);

        r_ivar(context, entity.Fields);

        return entity;
    }

    private AbstractEntity ReadFixNum(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyFixNum>();
        entity.Value = _reader.ReadFixNum();

        return entity;
    }

    private AbstractEntity ReadFloat(ReaderContext context)
    {
        var entity = _serializationContext.Create<RubyFloat>();
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
                throw new Exception($"Unsupported object r_symbol: {code}");
        }
    }

    private AbstractEntity r_object(ReaderContext context, bool hasIVar = false)
    {
        return context.HasIVars == hasIVar ? context.Action(context) : context.WithSubContext(hasIVar, context.Action);
    }

    private static ReaderContext Plain(ReaderContext context) => context.HasIVars ? context.WithoutIVars() : context;

    private void r_ivar(ReaderContext context, List<KeyValuePair<AbstractEntity, AbstractEntity>> ret)
    {

        var count = _reader.ReadFixNum();

        for (var i = 0; i < count; ++i)
        {
            var key = r_symbol(context);
            var val = r_object(context);

            ret.Add(new(key, val));
        }
    }
}