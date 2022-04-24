namespace Net.Messages;

using Net.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public abstract class MessageBase
{
    private static Dictionary<string, Type> Registered { get; set; } = new Dictionary<string, Type>();
    public abstract string MessageType { get; }

    public static void InitializeMessages()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types =
            from type in assembly.GetTypes()
            where type.IsDefined(typeof(RegisterMessageAttribute), false)
            select type;

        foreach (var v in types)
            if (!Registered.ContainsKey(v.Name)) Registered[v.Name] = v;
    }

    public static MessageBase Deserialize(byte[] obj)
    {
        string str = Encoding.UTF8.GetString(obj);
        Message msg = JsonSerializer.Deserialize<Message>(str);

        if (Registered.Count == 0)
            InitializeMessages();

        Type t = Registered[msg.MessageType];
        
        return JsonSerializer.Deserialize(str, t) as MessageBase;
    }

    internal protected virtual List<byte> Serialize() =>
        new List<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, GetType())));

    internal protected virtual async Task<List<byte>> SerializeAsync(CancellationToken token)
    {
        using MemoryStream stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, this, GetType(), cancellationToken: token);
        return new List<byte>(stream.ToArray());
    }
}
