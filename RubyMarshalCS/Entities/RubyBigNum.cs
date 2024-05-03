using System.Numerics;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyBigNum : AbstractEntity
{
    public BigInteger Value;

    public override RubyCodes Code { get; protected set; } = RubyCodes.BigNum;
}
