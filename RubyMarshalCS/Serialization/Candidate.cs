using System.Reflection;
using RubyMarshalCS.Serialization.Enums;

namespace RubyMarshalCS.Serialization;

public class Candidate
{
    public Candidate(CandidateType type, MemberInfo member)
    {
        Type = type;
        Member = member;
    }

    public Candidate(CandidateType type, MemberInfo member, CandidateFlags flags)
    {
        Type = type;
        Member = member;
        Flags = flags;
    }

    public CandidateType Type { get; }
    public MemberInfo Member { get; }
    public CandidateFlags Flags { get; }

    public override string ToString() => Member.Name;
}
