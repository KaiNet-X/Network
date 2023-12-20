namespace Net.Attributes;

using System;

/// <summary>
/// Use this attribute when the type name of an object is the same as another object or the names mismatch between client and server
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public class NetAliasAttribute : Attribute
{
    /// <summary>
    /// Replaces the type name of this object
    /// </summary>
    public readonly string TypeAlias;

    /// <summary>
    /// Alias for the object when sending between client and server
    /// </summary>
    /// <param name="typeAlias">Replaces the type name of this object</param>
    public NetAliasAttribute(string typeAlias)
    {
        TypeAlias = typeAlias;
    }
}
