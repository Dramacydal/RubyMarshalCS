using System.Globalization;
using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyFloat : AbstractEntity
{
    public FloatType Type { get; set; }
    public float Value { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Float;

    public override void ReadData(RubyFile r)
    {
        var len = r.ReadPackedInt();
        var bytes = r.Reader.ReadBytes(len).TakeWhile(_ => _ != 0).ToArray();
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

    public override void WriteData(RubyFile f)
    {
        switch (Type)
        {
            case FloatType.Normal:
                var str = Value.ToString(CultureInfo.InvariantCulture);
                f.Writer.Write(str.Length);
                f.Writer.Write(Encoding.ASCII.GetBytes(str));
                break;
            case FloatType.Inf:
                f.Writer.Write((byte)3);
                f.Writer.Write(Encoding.ASCII.GetBytes("inf"));
                break;
            case FloatType.NegInf:
                f.Writer.Write((byte)4);
                f.Writer.Write(Encoding.ASCII.GetBytes("-inf"));
                break;
            case FloatType.NaN:
                f.Writer.Write((byte)3);
                f.Writer.Write(Encoding.ASCII.GetBytes("nan"));
                break;
        }
    }
}