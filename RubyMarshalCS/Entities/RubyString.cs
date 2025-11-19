using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyString : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.String;

    public byte[] Bytes { get; set; }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Bytes);
    }
}