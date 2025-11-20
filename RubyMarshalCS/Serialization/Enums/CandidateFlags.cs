namespace RubyMarshalCS.Serialization.Enums;

[Flags]
public enum CandidateFlags: byte
{
    None = 0,
    In = 1,
    Out = 2,
    Dynamic = 4,
    Character = 8,
    Compressed = 16,

    InOut = In | Out,
}
