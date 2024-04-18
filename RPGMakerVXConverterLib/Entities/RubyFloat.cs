using System.Globalization;
using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyFloat : AbstractEntity
{
    public FloatType Type { get; set; }
    public float Value { get; set; }

    public byte[] RawBytes;

    public override RubyCodes Code { get; protected set; } = RubyCodes.Float;

    public override void ReadData(BinaryReader r)
    {
        var len = r.ReadPackedInt();
        var rawBytes = r.ReadBytes(len);
        RawBytes = rawBytes;
        var bytes = rawBytes.TakeWhile(_ => _ != 0).ToArray();
        var str = Encoding.ASCII.GetString(bytes);

        Value = 0.0f;
        Type = FloatType.Normal;
        
        switch (str)
        {
            case "inf":
                Type = FloatType.Inf;
                break;
            case "-inf":
                Type = FloatType.NegInf;
                break;
            case "nan":
                Type = FloatType.NaN;
                break;
            default:
                Value = float.Parse(str, CultureInfo.InvariantCulture);
                break;
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        // w.WritePackedInt(RawBytes.Length);
        // w.Write(RawBytes);
        // return;
        
        switch (Type)
        {
            case FloatType.Normal:
                var str = Value.ToString(CultureInfo.InvariantCulture);
                w.WritePackedInt(str.Length);
                w.Write(Encoding.ASCII.GetBytes(str));
                break;
            case FloatType.Inf:
                w.WritePackedInt(3);
                w.Write(Encoding.ASCII.GetBytes("inf"));
                break;
            case FloatType.NegInf:
                w.WritePackedInt(4);
                w.Write(Encoding.ASCII.GetBytes("-inf"));
                break;
            case FloatType.NaN:
                w.WritePackedInt(3);
                w.Write(Encoding.ASCII.GetBytes("nan"));
                break;
        }
    }
}