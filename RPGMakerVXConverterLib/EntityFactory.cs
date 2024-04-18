using RPGMakerVXConverterLib.Entities;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib;

public class EntityFactory
{
    protected List<RubySymbol> SymbolInstances = new();
    protected List<AbstractEntity> ObjectInstances = new();

    public RubySymbol LookupSymbol(AbstractEntity symbolOrLink)
    {
        if (symbolOrLink is RubySymbol s)
            return s;
        if (symbolOrLink is RubySymbolLink sl)
            return sl.ReferenceId < SymbolInstances.Count ? SymbolInstances[sl.ReferenceId] : null;

        throw new Exception("Entity is not symbol or link");
    }

    public AbstractEntity LookupObject(AbstractEntity objectOrLink)
    {
        if (objectOrLink is RubyObjectLink ol)
            return ol.ReferenceId < ObjectInstances.Count ? ObjectInstances[ol.ReferenceId] : null;

        return objectOrLink;
    }

    private static List<Type> nonLinkableObjectTypes = new()
    {
        typeof(RubySymbol),
        typeof(RubySymbolLink),
        typeof(RubyObjectLink),
        typeof(RubyNil),
        typeof(RubyTrue),
        typeof(RubyFalse),
        typeof(PackedInt),
    };

    public AbstractEntity Create(RubyCodes code)
    {
        AbstractEntity e;

        switch (code)
        {
            case RubyCodes.String:
                e = new RubyString();
                break;
            case RubyCodes.Nil:
                e = new RubyNil();
                break;
            case RubyCodes.Symbol:
            {
                e = new RubySymbol();
                SymbolInstances.Add(e as RubySymbol);
                break;
            }
            case RubyCodes.SymbolLink:
                e = new RubySymbolLink();
                break;
            case RubyCodes.ObjectLink:
                e = new RubyObjectLink();
                break;
            case RubyCodes.False:
                e = new RubyFalse();
                break;
            case RubyCodes.InstanceVar:
                e = new RubyInstanceVariable();
                break;
            case RubyCodes.True:
                e = new RubyTrue();
                break;
            case RubyCodes.Array:
                e = new RubyArray();
                break;
            case RubyCodes.Float:
                e = new RubyFloat();
                break;
            case RubyCodes.PackedInt:
                e = new PackedInt();
                break;
            case RubyCodes.Object:
            {
                e = new RubyObject();
                ObjectInstances.Add(e as RubyObject);
                return e;
            }
            case RubyCodes.UserDefined:
                e = new RubyUserDefined();
                break;
            case RubyCodes.RubyHash:
                e = new RubyHash();
                break;
            default:
                throw new Exception($"Unsupported code {code}");
        }

        if (!nonLinkableObjectTypes.Contains(e.GetType()))
        {
            ObjectInstances.Add(e);
        }

        return e;
    }
}
