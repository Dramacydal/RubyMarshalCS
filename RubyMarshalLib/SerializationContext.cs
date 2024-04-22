using RubyMarshal.Entities;
using RubyMarshal.Enums;
using RubyMarshal.Settings;

namespace RubyMarshal;

public class SerializationContext
{
    private readonly List<AbstractEntity> _symbolInstances = new();
    private readonly List<AbstractEntity> _objectInstances = new();

    private readonly ReaderSettings _settings = new();

    public SerializationContext(ReaderSettings settings = null)
    {
        _settings = settings ?? _settings;
    }

    public RubySymbol LookupSymbol(AbstractEntity symbolOrLink)
    {
        if (symbolOrLink.Code == RubyCodes.Symbol)
            return (RubySymbol)symbolOrLink;
        if (symbolOrLink.Code == RubyCodes.SymbolLink)
        {
            var sl = (RubySymbolLink)symbolOrLink;

            if (sl.ReferenceId < _symbolInstances.Count)
                return (RubySymbol)_symbolInstances[sl.ReferenceId];
            throw new Exception("Symbol link is out of bounds");
        }

        throw new Exception("Entity is not symbol or link");
    }

    public AbstractEntity LookupObject(AbstractEntity objectOrLink)
    {
        if (objectOrLink.Code == RubyCodes.ObjectLink)
        {
            var ol = (RubyObjectLink)objectOrLink;
            if (ol.ReferenceId < _objectInstances.Count)
                return _objectInstances[ol.ReferenceId];

            throw new Exception("Object link is out of bounds");
        }

        return objectOrLink;
    }

    private static readonly List<RubyCodes> LinkableObjectTypes = new()
    {
        RubyCodes.Array,
        RubyCodes.Hash,
        RubyCodes.InstanceVar,
        RubyCodes.Object,
        RubyCodes.Float,
        RubyCodes.String,
        RubyCodes.UserDefined,
    };

    private static readonly Dictionary<RubyCodes, Type> CodeToObjectTypeMap = new()
    {
        [RubyCodes.String] = typeof(RubyString),
        [RubyCodes.Nil] = typeof(RubyNil),
        [RubyCodes.Symbol] = typeof(RubySymbol),
        [RubyCodes.SymbolLink] = typeof(RubySymbolLink),
        [RubyCodes.ObjectLink] = typeof(RubyObjectLink),
        [RubyCodes.False] = typeof(RubyFalse),
        [RubyCodes.InstanceVar] = typeof(RubyInstanceVariable),
        [RubyCodes.True] = typeof(RubyTrue),
        [RubyCodes.Array] = typeof(RubyArray),
        [RubyCodes.Float] = typeof(RubyFloat),
        [RubyCodes.FixNum] = typeof(RubyFixNum),
        [RubyCodes.Object] = typeof(RubyObject),
        [RubyCodes.UserDefined] = typeof(RubyUserDefined),
        [RubyCodes.Hash] = typeof(RubyHash),
    };

    public AbstractEntity Create(RubyCodes code)
    {
        if (!CodeToObjectTypeMap.TryGetValue(code, out var type))
            throw new Exception($"Unsupported code {code}");

        var e = Activator.CreateInstance(type) as AbstractEntity;
        e!.Context = this;

        if (code == RubyCodes.Symbol)
            _symbolInstances.Add(e);
        else if (LinkableObjectTypes.Contains(code))
            _objectInstances.Add(e);

        return e;
    }

    public AbstractEntity Read(BinaryReader reader)
    {
        var code = reader.ReadByte();
        var e = Create((RubyCodes)code);
        e.ReadData(reader);

        if (_settings.ResolveLinks)
        {
            if (e.Code == RubyCodes.ObjectLink)
                e = LookupObject(e);
            else if (e.Code == RubyCodes.SymbolLink)
                e = LookupSymbol(e);
        }

        return e;
    }

    public void Write(BinaryWriter writer, AbstractEntity entity)
    {
        if (LinkableObjectTypes.Contains(entity.Code))
        {
            var index = _objectInstances.IndexOf(entity);
            if (index != -1)
            {
                Write(writer, new RubyObjectLink() { ReferenceId = index });
                return;
            }

            _objectInstances.Add(entity);
        }
        else if (entity.Code == RubyCodes.Symbol)
        {
            var index = _symbolInstances.IndexOf((RubySymbol)entity);
            if (index != -1)
            {
                Write(writer, new RubySymbolLink() { ReferenceId = index });
                return;
            }

            _symbolInstances.Add(entity);
        }

        writer.Write((byte)entity.Code);
        entity.WriteData(writer);
    }
}
