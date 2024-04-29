using System.Numerics;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyBigNum : AbstractEntity
{
    public BigInteger Value;

    private const byte PositiveSign = 43;
    private const byte NegativeSign = 45;

    public override RubyCodes Code { get; protected set; } = RubyCodes.BigNum;

    public override void ReadData(BinaryReader reader)
    {
        var sign = reader.ReadByte();

        List<ushort> digits = new();
        var len = reader.ReadFixNum();
        for (var i = 0; i < len; ++i)
            digits.Add(reader.ReadUInt16());

        BigInteger result = new();

        for (var i = 0; i < digits.Count; ++i)
        {
            BigInteger bigIntDigit = digits[i];

            result += bigIntDigit << (16 * i);
        }

        if (sign == NegativeSign)
            result *= -1;

        Value = result;
    }

    public override void WriteData(BinaryWriter writer)
    {
        List<ushort> digits = new();

        BigInteger other = Value;
        if (other < 0)
            other *= -1;

        for (;;)
        {
            if (other == 0)
                break;

            var d = (ushort)(other & 0xFFFF);
            digits.Add(d);

            other >>= 16;
        }

        if (Value >= 0)
            writer.Write(PositiveSign);
        else
            writer.Write(NegativeSign);

        writer.WriteFixNum(digits.Count);
        foreach (var t in digits)
            writer.Write(t);
    }
}
