namespace Net;

using Net.Attributes;
using Net.Connection.Channels;
using Net.Connection.Clients.Generic;
using Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class Utilities
{
    private static Type[] allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();

    private static List<(IChannel channel, TaskCompletionSource tcs)> _wait = new();

    public static Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

    static Utilities()
    {
        var aliasedTypes = allTypes
            .Where(type => type.IsDefined(typeof(NetAliasAttribute), false))
            .ToDictionary(
                type => type.GetCustomAttribute<NetAliasAttribute>().TypeAlias,
                type => type
            );

        foreach (var pair in aliasedTypes)
            NameTypeAssociations.Add(pair.Key, pair.Value);
    }

    public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(Bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(Bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteArray(List<byte> bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Count - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteSpan(ReadOnlySpan<byte> bytes, Span<byte> SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
        return -1;
    }

    public static bool IsArray(string typeName) =>
        typeName.Contains('[');

    public static void RegisterType(Type t)
    {
        var objectAttribute = t.GetCustomAttribute<NetAliasAttribute>();
        if (objectAttribute is null)
            NameTypeAssociations[t.Name] = t;
        else
            NameTypeAssociations[objectAttribute.TypeAlias] = t;
    }

    public static Type GetTypeFromName(string name)
    {
        if (NameTypeAssociations.ContainsKey(name))
            return NameTypeAssociations[name];
        else
        {
            Type t = ResolveType(name);
            try
            {
                NameTypeAssociations[name] = t;
            }
            catch
            {
                return NameTypeAssociations[name];
            }
            return t;
        }
    }

    public static Type ResolveType(string name)
    {
        Type type = allTypes.First(x => x.Name == GetBaseTypeName(name));

        if (!IsArray(name))
            return type;
        else if (name.Contains(","))
            type = MultiDimensionalArrayType(type, (byte)name.Where(c => c == ',').Count());
        else
            type = JaggedArrayType(type, (byte)name.Where(c => c == '[').Count());

        return type;
    }

    public static bool IsHerritableType<T>(this Type obType) =>
        typeof(T).IsAssignableFrom(obType);

    public static string GetBaseTypeName(string typeName) =>
        typeName.Replace("[", "").Replace(",", "").Replace("]", "");

    public static Type JaggedArrayType(Type baseType, byte dimensions)
    {
        Type type = baseType;
        for (int i = 0; i < dimensions; i++)
            type = Array.CreateInstance(type, 0).GetType();
        return type;
    }

    public static Type MultiDimensionalArrayType(Type baseType, byte dimensions)
    {
        int[] lengths = new int[dimensions + 1];
        for (int i = 0; i <= dimensions; i++)
            lengths[i] = 0;
        return Array.CreateInstance(baseType, lengths).GetType();
    }

    public static void ConcurrentAccess(Action a, SemaphoreSlim s)
    {
        s.Wait();

        try
        {
            a();
        }
        finally
        {
            s?.Release();
        }
    }

    public static async Task ConcurrentAccessAsync(Func<CancellationToken, Task> a, SemaphoreSlim s, int? timeout = 2500)
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        if (timeout.HasValue)
            cts.CancelAfter(timeout.Value);

        await s.WaitAsync();

        try
        {
            await a(cts.Token);
        }
        finally
        {
            s?.Release();
        }
    }

    public static async Task ConcurrentAccessAsync(Func<Task> a, SemaphoreSlim s)
    {
        await s.WaitAsync();

        try
        {
            await a();
        }
        finally
        {
            s?.Release();
        }
    }

    public static bool MatchAny<T>(T original, params T[] matches) => MatchAny<T>(original, matches as IEnumerable<T>);

    public static bool MatchAny<T>(T original, IEnumerable<T> matches)
    {
        foreach (var match in matches)
            if (original.Equals(match))
                return true;
        return false;
    }

    public static bool TryDequeueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, out T[] result)
    {
        result = new T[queue.Count];

        for (int i = 0; i < result.Length; i++)
            if (queue.TryDequeue(out T item))
                result[i] = item;
            else if (i == 0)
                return false;
            else
                result = result[..(i - 1)];
        return true;
    }

    public static void EnqueueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, IEnumerable<T> range)
    {
        foreach (T r in range)
            queue.Enqueue(r);
    }

    internal static void RegisterTcpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : class, IChannel
    {
        client.RegisterChannelType<TcpChannel>(
            async () =>
            {
                var remoteAddr = mainConnection.Value.Remote.Address;
                var localAddr = mainConnection.Value.Local.Address;

                Socket servSoc = new Socket(localAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                servSoc.Bind(new IPEndPoint(localAddr, 0));
                servSoc.Listen();

                var info = new Dictionary<string, string>
                {
                    { "Port", (servSoc.LocalEndPoint as IPEndPoint).Port.ToString() },
                    { "Mode", "Create" }
                };

                var m = new ChannelManagementMessage
                {
                    Info = info,
                    Type = typeof(TcpChannel).Name
                };

                await client.SendMessageAsync(m);

                Socket s = null;
                do
                {
                    s = await servSoc.AcceptAsync();
                    if (!(s.RemoteEndPoint as IPEndPoint).Address.Equals(remoteAddr))
                    {
                        s.Close();
                        s = null;
                    }
                } while (s == null);

                servSoc.Close();

                var c = new TcpChannel(s);

                return c;
            },
            async (m) =>
            {
                if (m.Info["Mode"] == "Create")
                {
                    var remoteAddr = mainConnection.Value.Remote.Address;
                    var soc = new Socket(remoteAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    soc.Connect(remoteAddr, int.Parse(m.Info["Port"]));

                    var c = new TcpChannel(soc);

                    await client.ChannelOpenedAsync(c);
                }
                else if (m.Info["Mode"] == "Close")
                {
                    var c = client._channels.First(ch => ch is TcpChannel c && c.Local.Port.ToString() == m.Info["IdPort"]) as TcpChannel;
                    c.Dispose();
                    client._channels.Remove(c);
                }
            },
            async (c) =>
            {
                await client.SendMessageAsync(new ChannelManagementMessage
                {
                    Type = typeof(TcpChannel).Name,
                    Info = new Dictionary<string, string>
                    {
                        { "IdPort", c.Remote.Port.ToString() },
                        { "Mode", "Close" }
                    }
                });
                c.Dispose();
                client._channels.Remove(c);
            });
    }

    internal static void RegisterUdpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : class, IChannel
    {
        client.RegisterChannelType<UdpChannel>(
            async () =>
            {
                var remoteAddr = mainConnection.Value.Remote.Address;
                var localAddr = mainConnection.Value.Local.Address;

                var c = new UdpChannel(new IPEndPoint(localAddr, 0));

                var info = new Dictionary<string, string>
                {
                    { "Port", c.Local.Port.ToString() },
                    { "Mode", "Create" }
                };

                var m = new ChannelManagementMessage
                {
                    Info = info,
                    Type = typeof(UdpChannel).Name
                };

                var u = (c, new TaskCompletionSource());

                _wait.Add(u);

                await client.SendMessageAsync(m);

                await _wait.First(v => v == u).tcs.Task;

                _wait.Remove(u);

                return c;
            },
            async (m) =>
            {
                var remoteAddr = mainConnection.Value.Remote.Address;
                var localAddr = mainConnection.Value.Local.Address;
                if (m.Info["Mode"] == "Create")
                {
                    var remoteEndpoint = new IPEndPoint(remoteAddr, int.Parse(m.Info["Port"]));
                    var c = new UdpChannel(new IPEndPoint(localAddr, 0));

                    c.SetRemote(remoteEndpoint);

                    var msg = new ChannelManagementMessage
                    {
                        Info = new Dictionary<string, string>
                        {
                            { "Port", c.Local.Port.ToString() },
                            { "Mode", "Confirm" },
                            { "IdPort", m.Info["Port"] },
                        },
                        Type = typeof(UdpChannel).Name
                    };

                    await client.SendMessageAsync(msg);

                    await client.ChannelOpenedAsync(c);
                }
                else if (m.Info["Mode"] == "Confirm")
                {
                    var c = _wait.Select(w => w.channel).First(c => c is UdpChannel ch && ch.Local.Port.ToString() == m.Info["IdPort"]) as UdpChannel;
                    c.SetRemote(new IPEndPoint(localAddr, int.Parse(m.Info["Port"])));
                    _wait.First(u => u.channel == c).tcs.SetResult();
                }
                else if (m.Info["Mode"] == "Close")
                {
                    var c = client._channels.First(ch => ch is UdpChannel c && c.Local.Port.ToString() == m.Info["IdPort"]) as UdpChannel;
                    c.Dispose();
                    client._channels.Remove(c);
                }
            },
            async (c) =>
            {
                await client.SendMessageAsync(new ChannelManagementMessage
                {
                    Type = typeof(UdpChannel).Name,
                    Info = new Dictionary<string, string>
                    {
                        { "IdPort", c.Remote.Port.ToString() },
                        { "Mode", "Close" }
                    }
                });
                c.Dispose();
                client._channels.Remove(c);
            });
    }

    internal static void RegisterEncryptedTcpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : class, IChannel
    {
        client.RegisterChannelType<EncryptedTcpChannel>(
            async () =>
            {
                var remoteAddr = mainConnection.Value.Remote.Address;
                var localAddr = mainConnection.Value.Local.Address;

                var servSoc = new Socket(localAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                servSoc.Bind(new IPEndPoint(localAddr, 0));
                servSoc.Listen();

                var crypto = new CryptographyService();

                var aesKey = CryptographyService.CreateRandomKey(256 / 8);

                var info = new Dictionary<string, string>
                {
                    { "Port", (servSoc.LocalEndPoint as IPEndPoint).Port.ToString() },
                    { "Mode", "Create" },
                    { "AesKey", Convert.ToBase64String(crypto.AesKey) },
                    { "AesIv", Convert.ToBase64String(crypto.AesIv) }
                };

                var m = new ChannelManagementMessage
                {
                    Info = info,
                    Type = typeof(EncryptedTcpChannel).Name
                };

                await client.SendMessageAsync(m);

                Socket s = null;
                do
                {
                    s = await servSoc.AcceptAsync();
                    if (!(s.RemoteEndPoint as IPEndPoint).Address.Equals(remoteAddr))
                    {
                        s.Close();
                        s = null;
                    }
                } while (s == null);

                var c = new EncryptedTcpChannel(s, crypto);

                servSoc.Close();

                return c;
            },
            async (m) =>
            {
                if (m.Info["Mode"] == "Create")
                {
                    var soc = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    soc.Connect(mainConnection.Value.Remote.Address, int.Parse(m.Info["Port"]));
                    var crypto = new CryptographyService();
                    crypto.AesKey = Convert.FromBase64String(m.Info["AesKey"]);
                    crypto.AesIv = Convert.FromBase64String(m.Info["AesIv"]);
                    var c = new EncryptedTcpChannel(soc, crypto);

                    await client.ChannelOpenedAsync(c);
                }
                else if (m.Info["Mode"] == "Close")
                {
                    var c = client._channels.First(ch => ch is EncryptedTcpChannel c && c.Local.Port.ToString() == m.Info["IdPort"]) as EncryptedTcpChannel;
                    c.Dispose();
                    client._channels.Remove(c);
                }
            },
            async (c) =>
            {
                await client.SendMessageAsync(new ChannelManagementMessage
                {
                    Type = typeof(TcpChannel).Name,
                    Info = new Dictionary<string, string>
                    {
                        { "IdPort", c.Remote.Port.ToString() },
                        { "Mode", "Close" }
                    }
                });
                c.Dispose();
                client._channels.Remove(c);
            });
    }
}