using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyModuleOld : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Class;

    public byte[] Bytes { get; set; }

    public override string ToString() => "Old module: " + Encoding.UTF8.GetString(Bytes);
}
