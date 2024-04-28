namespace RubyMarshalCS;

public static class LibExtensions
{
    public static int ReadFixNum(this BinaryReader reader)
    {
        byte code = reader.ReadByte();

        uint encoded1 = 0;
        byte encoded2 = 0;
        switch (code)
        {
            case 1:
            {
                encoded1 = reader.ReadByte();
                break;
            }
            case 2:
            {
                encoded1 = reader.ReadUInt16();
                break;
            }
            case 3:
            {
                encoded1 = reader.ReadUInt16();
                encoded2 = reader.ReadByte();
                break;
            }
            case 4:
            {
                encoded1 = reader.ReadUInt32();
                break;
            }
            case 252:
            {
                encoded1 = reader.ReadUInt32();
                break;
            }
            case 253:
            {
                encoded1 = reader.ReadUInt16();
                encoded2 = reader.ReadByte();
                break;
            }
            case 254:
            {
                encoded1 = reader.ReadUInt16();
                break;
            }
            case 255:
            {
                encoded1 = reader.ReadByte();
                break;
            }
        }

        var isImmediate = code > 4 && code < 252;

        return (int)((isImmediate
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

    public static int ReadFixNum2(this BinaryReader reader)
    {
        var head = reader.ReadSByte();
        if (head == 0)
            return 0;

        if (head > 5)
        {
            return head - 5;
        }

        if (head < -4)
        {
            return head + 5;
        }

        var len = head > 0 ? head : head * -1;

        byte b1 = reader.ReadByte(), b2 = 0, b3 = 0, b4 = 0;
        if (head <= 0)
            b2 = b3 = b4 = 0xFF;

        if (len >= 2)
            b2 = reader.ReadByte();
        if (len >= 3)
            b3 = reader.ReadByte();
        if (len >= 4)
            b4 = reader.ReadByte();

        return ((0xFF << 0x00) & (b1 << 0x00))
               | ((0xFF << 0x08) & (b2 << 0x08))
               | ((0xFF << 0x10) & (b3 << 0x10))
               | ((0xFF << 0x18) & (b4 << 0x18));
    }

    public static void WriteFixNum(this BinaryWriter writer, int value)
    {
        if (value == 0)
        {
            writer.Write((byte)0);
            return;
        }

        if (0 < value && value < 123)
        {
            writer.Write((byte)(value + 5));
            return;
        }

        if (-124 < value && value < 0)
        {
            writer.Write((byte)((value - 5) & 0xFF));
            return;
        }

        var buf = new byte[5];

        var v = value;
        sbyte i = 1;
        for (; i < 4 + 1; ++i)
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

        writer.Write(buf, 0, i + 1);
    }

    public static void WriteFixNum2(this BinaryWriter writer, int value)
    {
        if (value == 0)
        {
            writer.Write((byte)0);
            return;
        }

        if (0 < value && value < 123)
        {
            writer.Write((byte)(value + 5));
            return;
        }

        if (-124 < value && value < 0)
        {
            writer.Write((sbyte)value - 5);
            return;
        }

        sbyte len = 0;
        if (value > 0)
        {
            /* Positive number */
            if (value <= 0x7F)
            {
                /* 1 byte wide */
                len = 1;
            }
            else if (value <= 0x7FFF)
            {
                /* 2 bytes wide */
                len = 2;
            }
            else if (value <= 0x7FFFFF)
            {
                /* 3 bytes wide */
                len = 3;
            }
            else
            {
                /* 4 bytes wide */
                len = 4;
            }
        }
        else
        {
            /* Negative number */
            if (value >= (int)0x80)
            {
                /* 1 byte wide */
                len = -1;
            }
            else if (value >= (int)0x8000)
            {
                /* 2 bytes wide */
                len = -2;
            }
            else if (value <= (int)0x800000)
            {
                /* 3 bytes wide */
                len = -3;
            }
            else
            {
                /* 4 bytes wide */
                len = -4;
            }

            /* Write length */
            writer.Write(len);

            /* Write bytes */
            if (len >= 1 || len <= -1)
            {
                writer.Write((byte)((value & 0x000000FF) >> 0x00));
            }

            if (len >= 2 || len <= -2)
            {
                writer.Write((byte)((value & 0x0000FF00) >> 0x08));
            }

            if (len >= 3 || len <= -3)
            {
                writer.Write((byte)((value & 0x00FF0000) >> 0x10));
            }

            if (len == 4 || len == -4)
            {
                writer.Write((byte)((value & 0xFF000000) >> 0x18));
            }
        }
    }

    public static byte[] ReadByteSequence(this BinaryReader reader)
    {
        var len = reader.ReadFixNum();
        return reader.ReadBytes(len);
    }

    public static void WriteByteSequence(this BinaryWriter writer, byte[] value)
    {
        writer.WriteFixNum(value.Length);
        writer.Write(value);
    }

    public static byte[] GetTrimmedBuffer(this MemoryStream stream)
    {
        var bytes = stream.GetBuffer();
        if (stream.Length < bytes.Length)
            Array.Resize(ref bytes, (int)stream.Length);

        return bytes;
    }
}
