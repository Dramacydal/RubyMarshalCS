using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyRegExp : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.RegExp;

    public byte[] Bytes { get; set; }
    public RubyRegexpOptions Options { get; set; }

    public override string ToString()
    {
        return "/" + Encoding.UTF8.GetString(Bytes) + "/" + Options.AsString();
    }
}