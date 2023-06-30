namespace Net.Messages;

using Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all message types
/// </summary>
public abstract class MessageBase
{
    /// <summary>
    /// Dictionary of registered message types. By default is all messages with RegisterMessageAttribute.
    /// </summary>
    public static Dictionary<string, Type> Registered { get; set; } = InitializeMessages();
    private string _messageType;

    /// <summary>
    /// Gets the type of the message. This is used in the message protocol.
    /// </summary>
    public string MessageType => 
        _messageType ??= GetType().Name;

    internal static Dictionary<string, Type> InitializeMessages()
    {
        var dict = new Dictionary<string, Type>();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types =
            from type in assemblies.SelectMany(a => a.GetTypes())
            //where type.IsDefined(typeof(RegisterMessageAttribute), false)
            where type.IsHerritableType<MessageBase>()
            select type;

        foreach (var v in types)
            if (!dict.ContainsKey(v.Name)) dict[v.Name] = v;

        return dict;
    }
}