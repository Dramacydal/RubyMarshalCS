using RubyMarshalCS.Serialization.Enums;

namespace RubyMarshalCS.Serialization;

public class Candidate
{
    public Candidate(CandidateType type, string name)
    {
        Type = type;
        Name = name;
    }

    public Candidate(CandidateType type, string name, CandidateFlags flags)
    {
        Type = type;
        Name = name;
        Flags = flags;
    }

    public CandidateType Type { get; }
    public string Name { get; }
    public CandidateFlags Flags { get; }
}
