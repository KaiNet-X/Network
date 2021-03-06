namespace Net.Messages;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MessageParser
{
    static readonly byte[] Start = new byte[] { 0x3c, 0x53, 0x54, 0x41, 0x3e };
    static readonly byte[] End = new byte[] { 0x3c, 0x45, 0x4e, 0x44, 0x3e };

    public static byte[] Encapsulate(byte[] bytes)
    {
        int l1 = Start.Length + bytes.Length;
        int l2 = l1 + End.Length;

        byte[] b = new byte[l2];

        Array.Copy(Start, 0, b, 0, Start.Length);
        Array.Copy(bytes, 0, b, Start.Length, bytes.Length);
        Array.Copy(End, 0, b, Start.Length + bytes.Length, End.Length);

        return b;
    }

    public static byte[] RemoveTags(List<byte> b)
    {
        int start = Utilities.IndexInByteArray(b, Start);
        int end = Utilities.IndexInByteArray(b, End);

        if (end == -1 || start == -1)
            return new byte[0];

        List<byte> sub = b.GetRange(start + Start.Length, end - start - Start.Length);
        b.RemoveRange(start, end - start + End.Length);
        return sub.ToArray();
    }

    #region GetMessages

    public static IEnumerable<MessageBase> GetMessagesEnum(List<byte> obj)
    {
        byte[] sub = null;

        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            yield return Deserialize(sub);
            if (obj.Count == 0) break;
        }
    }

    public static IEnumerable<MessageBase> GetMessagesAesEnum(List<byte> obj, byte[] encKey)
    {
        byte[] sub = null;
        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            sub = CryptoServices.DecryptAES(sub, encKey, encKey);

            yield return Deserialize(sub);
            if (obj.Count == 0) break;
        }
    }

    public static IEnumerable<MessageBase> GetMessagesRsaEnum(List<byte> obj, RSAParameters encKey)
    {
        byte[] sub = null;
        List<MessageBase> msg = new List<MessageBase>();

        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            sub = CryptoServices.DecryptRSA(sub, encKey);

            yield return Deserialize(sub);
            if (obj.Count == 0) break;
        }
    }

    #endregion
    #region Serialization

    private static readonly byte[] _end = { 0x7d, 0x7d };

    public static MessageBase Deserialize(byte[] obj)
    {
        int e = Utilities.IndexInByteArray(obj, new byte[] { 0x7d });

        string type = Encoding.UTF8.GetString(obj[..e]);

        Type t = MessageBase.Registered[type];

        return MessageBase.Deserialize(obj[(e + 1)..^0], t);
    }

    public static byte[] Serialize(MessageBase message)
    {
        var serializer = MessageBase.Serializer;
        byte[] serialized = serializer.Serialize(message, message.GetType());
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 1];

        Array.Copy(Encoding.UTF8.GetBytes($"{message.MessageType}}}"), bytes, message.MessageType.Length + 1);
        Array.Copy(serialized, 0, bytes, message.MessageType.Length + 1, serialized.Length);
        
        return bytes;
    }

    public static async Task<byte[]> SerializeAsync(MessageBase message, CancellationToken token)
    {
        var serializer = MessageBase.Serializer;
        
        byte[] serialized = await serializer.SerializeAsync(message, message.GetType(), token);
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 1];
        
        Array.Copy(Encoding.UTF8.GetBytes($"{message.MessageType}}}"), bytes, message.MessageType.Length + 1);
        Array.Copy(serialized, 0, bytes, message.MessageType.Length + 1, serialized.Length);

        return bytes;
    }

    #endregion
}
