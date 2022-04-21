namespace Net.Messages;

using System.Collections.Generic;
using System.Security.Cryptography;

internal static class MessageParser
{
    static readonly byte[] Start = new byte[] { 0x3c, 0x53, 0x54, 0x41, 0x52, 0x54, 0x3e };
    static readonly byte[] End = new byte[] { 0x3c, 0x45, 0x4e, 0x44, 0x3e };

    public static List<MessageBase> GetMessages(ref List<byte> obj)
    {
        List<byte> sub = new List<byte>();
        List<MessageBase> msg = new List<MessageBase>();

        while (true)
        {
            sub = new List<byte>(GetTags(ref obj));
            if (sub.Count == 0) break;

            msg.Add(MessageBase.Deserialize(sub.ToArray()));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    public static List<MessageBase> GetMessagesAes(ref List<byte> obj, byte[] encKey)
    {
        List<byte> sub = new List<byte>();
        List<MessageBase> msg = new List<MessageBase>();
        while (true)
        {
            sub = new List<byte>(GetTags(ref obj));
            if (sub.Count == 0) break;

            sub = new List<byte>(CryptoServices.DecryptAES(sub.ToArray(), encKey));

            msg.Add(MessageBase.Deserialize(sub.ToArray()));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    public static List<MessageBase> GetMessagesRsa(ref List<byte> obj, RSAParameters encKey)
    {
        List<byte> sub = new List<byte>();
        List<MessageBase> msg = new List<MessageBase>();

        while (true)
        {
            sub = new List<byte>(GetTags(ref obj));
            if (sub.Count == 0) break;

            sub = new List<byte>(CryptoServices.DecryptRSA(sub.ToArray(), encKey));

            msg.Add(MessageBase.Deserialize(sub.ToArray()));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    public static List<byte> GetTags(ref List<byte> b)
    {
        byte[] arr = b.ToArray();
        int start = Utilities.IndexInByteArray(arr, Start);
        int end = Utilities.IndexInByteArray(arr, End);

        var s2str = System.Text.Encoding.UTF8.GetString(b.ToArray());

        if (end == -1 || start == -1) 
            return new List<byte>();

        List<byte> sub = b.GetRange(start + Start.Length, end - start - Start.Length);
        b.RemoveRange(start, end - start + End.Length);
        return sub;
    }

    public static List<byte> AddTags(List<byte> b)
    {
        List<byte> lst = new List<byte>(Start);
        lst.AddRange(b);
        lst.AddRange(End);
        return lst;
    }
}