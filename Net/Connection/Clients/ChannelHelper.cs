namespace Net.Connection.Clients;

using Channels;
using Generic;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class ChannelHelper
{
    private static List<(BaseChannel channel, TaskCompletionSource tcs)> _wait = new();

    internal static void RegisterTcpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : BaseChannel
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
                    c.Close();
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
                c.Close();
                client._channels.Remove(c);
            });
    }

    internal static void RegisterUdpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : BaseChannel
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
                    c.Close();
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
                c.Close();
                client._channels.Remove(c);
            });
    }

    internal static void RegisterEncryptedTcpChannel<T>(ObjectClient<T> client, Lazy<TcpChannel> mainConnection) where T : BaseChannel
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
                    c.Close();
                    client._channels.Remove(c);
                }
            },
            async (c) =>
            {
                await client.SendMessageAsync(new ChannelManagementMessage
                {
                    Type = typeof(EncryptedTcpChannel).Name,
                    Info = new Dictionary<string, string>
                    {
                        { "IdPort", c.Remote.Port.ToString() },
                        { "Mode", "Close" }
                    }
                });
                c.Close();
                client._channels.Remove(c);
            });
    }
}
