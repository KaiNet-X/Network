namespace Net.Messages;

using Net.Attributes;
using Net.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract class MessageBase
{
    public static Dictionary<string, Type> Registered { get; set; } = InitializeMessages();
    private string _messageType;
    public static ISerializer Serializer = new MpSerializer();

    public string MessageType => 
        _messageType ??= GetType().Name;

    internal static Dictionary<string, Type> InitializeMessages()
    {
        var dict = new Dictionary<string, Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types =
            from type in assemblies.SelectMany(a => a.GetTypes())
            where type.IsDefined(typeof(RegisterMessageAttribute), false)
            select type;

        foreach (var v in types)
            if (!dict.ContainsKey(v.Name)) dict[v.Name] = v;

        return dict;
    }

    internal static MessageBase Deserialize(byte[] bytes, Type type) => Serializer.Deserialize(bytes, type) as MessageBase;
}