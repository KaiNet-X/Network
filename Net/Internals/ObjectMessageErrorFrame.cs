namespace Net.Internals;

using Net.Serialization;
using System;

public class ObjectMessageErrorFrame
{
    private readonly ISerializer _serializer;

    public readonly byte[] Data;
    public readonly string TypeName;
    public readonly UnregisteredTypeReason Reason;

    internal ObjectMessageErrorFrame(byte[] data, string typeName, UnregisteredTypeReason reason, ISerializer serializer)
    {
        Data = data;
        TypeName = typeName;
        Reason = reason;
        _serializer = serializer;
    }

    public bool TryGetType(out Type type) =>
        TypeHandler.TryGetTypeFromName(TypeName, out type);

    public bool TryDeserializeData(out object obj)
    {
        if (!TryGetType(out Type type))
        {
            obj = null;
            return false;
        }

        try
        {
            obj = _serializer.Deserialize(Data, type);
            return true;
        }
        catch
        {
            obj = null;
            return false;
        }
    }

    public enum UnregisteredTypeReason
    {
        TypeUnknown,
        TypeUnregistered
    }
}
