namespace Net.Attributes;

using System;

/// <summary>
/// Use this attribute when the type name of an object is the same as another object
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public class NetAliasAttribute : Attribute
{
    public readonly string TypeAlias;

    public NetAliasAttribute(string typeAlias)
    {
        TypeAlias = typeAlias;
    }
}
