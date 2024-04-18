using Kaitai;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class PackedInt : AbstractEntity
{
    public int Value { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.PackedInt;

    public override void ReadData(RubyFile f)
    {
        byte code = f.Reader.ReadByte();

        uint encoded1 = 0;
        byte encoded2 = 0;
        switch (code)
        {
            case 1:
            {
                encoded1 = f.Reader.ReadByte();
                break;
            }
            case 2:
            {
                encoded1 = f.Reader.ReadUInt16();
                break;
            }
            case 3:
            {
                encoded1 = f.Reader.ReadUInt16();
                encoded2 = f.Reader.ReadByte();
                break;
            }
            case 4:
            {
                encoded1 = f.Reader.ReadUInt32();
                break;
            }
            case 252:
            {
                encoded1 = f.Reader.ReadUInt32();
                break;
            }
            case 253:
            {
                encoded1 = f.Reader.ReadUInt16();
                encoded2 = f.Reader.ReadByte();
                break;
            }
            case 254:
            {
                encoded1 = f.Reader.ReadUInt16();
                break;
            }
            case 255:
            {
                encoded1 = f.Reader.ReadByte();
                break;
            }
        }

        var isImmediate = code > 4 && code < 252;

        Value = (int)((isImmediate
            ? (code < 128 ? (code - 5) : (4 - (~(code) & 127)))
            : (code == 0
                ? 0
                : (code == 255
                    ? (encoded1 - 256)
                    : (code == 254
                        ? (encoded1 - 65536)
                        : (code == 253
                            ? (((encoded2 << 16) | encoded1) - 16777216)
                            : (code == 3 ? ((encoded2 << 16) | encoded1) : encoded1)))))));
    }

    public override void WriteData(RubyFile f)
    {
        if (Value == 0)
        {
            f.Writer.Write((byte)0);
            return;
        }

        if (0 < Value && Value < 123)
        {
            f.Writer.Write((byte)(Value + 5));
            return;
        }

        if (-124 < Value && Value < 0)
        {
            f.Writer.Write(((byte)(Value - 5) & 0xFF));
            return;
        }

        var buf = new byte[5];

        var v = Value;
        sbyte i = 1;
        for (;i < 4 + 1; ++i)
        {
            buf[i] = (byte)(v & 0xFF);
            v >>= 8;
            if (v == 0)
            {
                buf[0] = (byte)i;
                break;
            }

            if (v == -1)
            {
                buf[0] = (byte)-i;
                break;
            }
        }

        f.Writer.Write(buf, 0, i + 1);
    }
}