using System.Reflection;
using RubyMarshalCS.Serialization.Enums;

namespace RubyMarshalCS.Serialization;

public class Candidate
{
    public Candidate(MemberInfo member)
    {
        Member = member;
    }

    public Candidate(MemberInfo member, CandidateFlags flags)
    {
        Member = member;
        Flags = flags;
    }

    public MemberInfo Member { get; }
    public CandidateFlags Flags { get; }

    public override string ToString() => Member.Name;

    public Type ValueType
    {
        get
        {
            return Member.MemberType switch
            {
                MemberTypes.Field => (Member as FieldInfo)!.FieldType,
                MemberTypes.Property => (Member as PropertyInfo)!.PropertyType,
                _ => throw new Exception($"Unsupported member type: {Member.MemberType}")
            };
        }
    }

    public void SetValue(object obj, object? value)
    {
        switch (Member.MemberType)
        {
            case MemberTypes.Field:
                (Member as FieldInfo)!.SetValue(obj, value);
                break;
            case MemberTypes.Property:
                (Member as PropertyInfo)!.SetValue(obj, value);
                break;
            default:
                throw new Exception($"Unsupported member type: {Member.MemberType}");
        }
    }

    public object? GetValue(object obj)
    {
        switch (Member.MemberType)
        {
            case MemberTypes.Field:
                return (Member as FieldInfo)!.GetValue(obj);
                break;
            case MemberTypes.Property:
                return (Member as PropertyInfo)!.GetValue(obj);
                break;
            default:
                throw new Exception($"Unsupported member type: {Member.MemberType}");
        }
    }
}
