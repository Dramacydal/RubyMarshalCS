using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyModule : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Class;

    public byte[] Bytes { get; set; }

    public override string ToString() => "Module: " + Encoding.UTF8.GetString(Bytes);
}
