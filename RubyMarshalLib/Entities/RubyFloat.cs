using System.Globalization;
using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFloat : AbstractEntity
{
    public double Value { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Float;

    public override void ReadData(BinaryReader reader)
    {
        var bytes = reader.ReadByteSequence().TakeWhile(_ => _ != 0).ToArray();
        var str = Encoding.ASCII.GetString(bytes);

        Value = str switch
        {
            "inf" => double.PositiveInfinity,
            "-inf" => double.NegativeInfinity,
            "nan" => double.NaN,
            _ => double.Parse(str, CultureInfo.InvariantCulture)
        };
    }

    public override void WriteData(BinaryWriter writer)
    {
        if (double.IsNaN(Value))
            writer.WriteByteSequence(Encoding.ASCII.GetBytes("nan"));
        else if (double.IsPositiveInfinity(Value))
            writer.WriteByteSequence(Encoding.ASCII.GetBytes("inf"));
        else if (double.IsNegativeInfinity(Value))
            writer.WriteByteSequence(Encoding.ASCII.GetBytes("-inf"));
        else
        {
            var str = Value.ToString(CultureInfo.InvariantCulture);
            writer.WriteByteSequence(Encoding.ASCII.GetBytes(str));
        }
    }
}