namespace Net.Messages.Parser;

using Net.Serialization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MessageParser
{
    public static ISerializer Serializer = new MpSerializer();
    static readonly byte[] Start = new byte[] { 0x3c, 0x53, 0x54, 0x41, 0x3e };
    static readonly byte[] End = new byte[] { 0x3c, 0x45, 0x4e, 0x44, 0x3e };

    public static byte[] Encapsulate(byte[] bytes)
    {
        int len = Start.Length + bytes.Length + End.Length;

        byte[] b = new byte[len];
        Buffer.BlockCopy(Start, 0, b, 0, Start.Length);
        Buffer.BlockCopy(bytes, 0, b, Start.Length, bytes.Length);
        Buffer.BlockCopy(End, 0, b, Start.Length + bytes.Length, End.Length);

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
        byte[] sub;

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
        byte[] sub;
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
        byte[] sub;
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

        return Serializer.Deserialize(obj[(e + 1)..^0], t) as MessageBase;
    }

    public static byte[] Serialize(MessageBase message)
    {
        byte[] serialized = Serializer.Serialize(message, message.GetType());
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 1];

        Array.Copy(Encoding.UTF8.GetBytes($"{message.MessageType}}}"), bytes, message.MessageType.Length + 1);
        Buffer.BlockCopy(serialized, 0, bytes, message.MessageType.Length + 1, serialized.Length);

        return bytes;
    }

    public static async Task<byte[]> SerializeAsync(MessageBase message, CancellationToken token)
    {
        byte[] serialized = await Serializer.SerializeAsync(message, message.GetType(), token);
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 1];

        Array.Copy(Encoding.UTF8.GetBytes($"{message.MessageType}}}"), bytes, message.MessageType.Length + 1);
        Buffer.BlockCopy(serialized, 0, bytes, message.MessageType.Length + 1, serialized.Length);

        return bytes;
    }

    #endregion
}
