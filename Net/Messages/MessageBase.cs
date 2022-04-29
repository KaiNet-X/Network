namespace Net.Messages;

using Net.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public abstract class MessageBase
{
    private static Dictionary<string, Type> Registered { get; set; } = new Dictionary<string, Type>();
    private string _messageType;

    public string MessageType
    {
        get 
        {
            if (_messageType == null) _messageType = GetType().Name;
            return _messageType;
        }
    }

    public static void InitializeMessages()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //var assembly = Assembly.GetExecutingAssembly();
        var types =
            from type in assemblies.SelectMany(a => a.GetTypes())
            where type.IsDefined(typeof(RegisterMessageAttribute), false)
            select type;

        foreach (var v in types)
            if (!Registered.ContainsKey(v.Name)) Registered[v.Name] = v;
    }

    public static MessageBase Deserialize(byte[] obj)
    {
        string str = Encoding.UTF8.GetString(obj);
        MessageTypeChekcer msg = JsonSerializer.Deserialize<MessageTypeChekcer>(str);

        if (Registered.Count == 0)
            InitializeMessages();

        Type t = Registered[msg.MessageType];
        
        return JsonSerializer.Deserialize(str, t) as MessageBase;
    }

    internal protected virtual byte[] Serialize() =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, GetType()));

    internal protected virtual async Task<byte[]> SerializeAsync(CancellationToken token)
    {
        using MemoryStream stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, this, GetType(), cancellationToken: token);
        return stream.ToArray();
    }
}