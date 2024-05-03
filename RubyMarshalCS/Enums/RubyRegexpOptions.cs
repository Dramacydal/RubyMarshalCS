namespace RubyMarshalCS.Enums;

[Flags]
public enum RubyRegexpOptions
{
    IgnoreCase = 0x1,
    Extend = 0x2,
    Multiline = 0x4,
    SingleLine = 0x8,
    FindLongest = 0x10,
    FindNotEmpty = 0x20,
    NegateSingleLine = 0x40,
    DontCaptureGroup = 0x80,
    NotBoL = 0x100,
    NotEoL = 0x200,
    NotBoS = 0x400,
    NotEoS = 0x400,
    ASCIIRange = 0x800,
    PosixBracketAllRange = 0x1000,
    WordBoundAllRange = 0x2000,
    NewlinCRLF = 0x4000,
}
