using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyModule : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Class;

    public byte[] Bytes { get; set; }

    public override string ToString() => "Module: " + Encoding.UTF8.GetString(Bytes);
}
