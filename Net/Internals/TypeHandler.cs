namespace Net.Internals;

using Net.Attributes;
using Net.Messages;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

public static class TypeHandler
{
    private static readonly Type[] _allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();
    private static readonly ConcurrentDictionary<string, Type> registeredMessages = new();
    public static readonly GuardedDictionary<string, Type> RegisteredMessages = new(registeredMessages);

    private static ConcurrentDictionary<string, Type> NameTypeAssociations = new ConcurrentDictionary<string, Type>();

    static TypeHandler()
    {
        var aliasedTypes = _allTypes
            .Where(type => type.IsDefined(typeof(NetAliasAttribute), false))
            .ToDictionary(
                type => type.GetCustomAttribute<NetAliasAttribute>().TypeAlias,
                type => type
            );

        foreach (var pair in aliasedTypes)
            NameTypeAssociations.TryAdd(pair.Key, pair.Value);

        foreach (var v in _allTypes.Where(IsHerritableType<MessageBase>))
            registeredMessages.TryAdd(v.Name, v);
    }

    public static bool TryGetTypeFromName(string name, out Type type)
    {
        if (NameTypeAssociations.ContainsKey(name))
            type = NameTypeAssociations[name];
        else
            NameTypeAssociations.TryAdd(name, type = ResolveType(name));

        return type is not null;
    }
    
    private static bool IsArray(string typeName) =>
        typeName.Contains('[');

    private static Type ResolveType(string name)
    {
        Type type = _allTypes.FirstOrDefault(x => x.Name == GetBaseTypeName(name));

        return type switch
        {
            null => null,
            _ when !IsArray(name) => type,
            _ when name.Contains(',') => MultiDimensionalArrayType(type, (byte)name.Where(c => c == ',').Count()),
            _ => JaggedArrayType(type, (byte)name.Where(c => c == '[').Count())
        };
    }

    public static bool IsHerritableType<T>(Type obType) =>
        typeof(T).IsAssignableFrom(obType);

    private static string GetBaseTypeName(string typeName) =>
        typeName.Replace("[", "").Replace(",", "").Replace("]", "");

    private static Type JaggedArrayType(Type baseType, byte dimensions)
    {
        Type type = baseType;
        for (int i = 0; i < dimensions; i++)
            type = Array.CreateInstance(type, 0).GetType();
        return type;
    }

    private static Type MultiDimensionalArrayType(Type baseType, byte dimensions)
    {
        int[] lengths = new int[dimensions + 1];
        for (int i = 0; i <= dimensions; i++)
            lengths[i] = 0;
        return Array.CreateInstance(baseType, lengths).GetType();
    }
}
