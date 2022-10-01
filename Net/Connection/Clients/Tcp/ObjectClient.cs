﻿namespace Net.Connection.Clients.Tcp;

using Channels;
using Messages;
using Net.Connection.Clients.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

/// <summary>
/// Base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public class ObjectClient : ObjectClient<TcpChannel>
{
    public IPEndPoint LocalEndpoint => localEndPoint;
    public IPEndPoint RemoteEndpoint => remoteEndPoint;

    protected IPEndPoint localEndPoint;
    protected IPEndPoint remoteEndPoint;

    protected List<IChannel> _connectionWait = new();

    protected ObjectClient()
    {
        CustomMessageHandlers.Add(typeof(ChannelManagementMessage).Name, (mb) =>
        {
            var m = mb as ChannelManagementMessage;
            if (m.Info is not null && m.Info.ContainsKey("Type"))
            {
                foreach (var name in ChannelMessages.Keys)
                {
                    if (name.Name == m.Info["Type"])
                    {
                        ChannelMessages[name](m);
                        break;
                    }
                }
            }
        });
        RegisterChannelType<UdpChannel>(async () =>
        {
            var remoteAddr = ((IPEndPoint)Connection.Socket.RemoteEndPoint).Address;
            var localAddr = ((IPEndPoint)Connection.Socket.LocalEndPoint).Address;
            var key = Settings.EncryptChannels ? CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray())) : null;

            UdpChannel c = Settings.EncryptChannels ?
                new(new IPEndPoint(remoteAddr, 0), key) :
                new(new IPEndPoint(localAddr, 0));

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

            if (Settings.EncryptChannels)
                m.Crypto = key;

            _connectionWait.Add(c);

            await SendMessageAsync(m);

            while (_connectionWait.Contains(c))
                await Task.Delay(10);

            Channels.Add(c);

            return c;
        }, (m) =>
        {
            var remoteAddr = ((IPEndPoint)Connection.Socket.RemoteEndPoint).Address;
            var localAddr = ((IPEndPoint)Connection.Socket.LocalEndPoint).Address;
            if (m.Info["Mode"] == "Create")
            {
                var remoteEndpoint = new IPEndPoint(remoteAddr, int.Parse(m.Info["Port"]));
                UdpChannel c = Settings.EncryptChannels ?
                    new(new IPEndPoint(localAddr, 0), m.Crypto) :
                    new(new IPEndPoint(localAddr, 0));

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

                SendMessage(msg);
                Channels.Add(c);

                ChannelOpened(c);
            }
            else if (m.Info["Mode"] == "Confirm")
            {
                var c = _connectionWait.First(c => c is UdpChannel ch && ch.Local.Port.ToString() == m.Info["IdPort"]) as UdpChannel;
                c.SetRemote(new IPEndPoint(localAddr, int.Parse(m.Info["Port"])));
                _connectionWait.Remove(c);
            }
            else if (m.Info["Mode"] == "Close")
            {
                var c = Channels.First(ch => ch is UdpChannel c && c.Local.Port.ToString() == m.Info["IdPort"]) as UdpChannel;
                c.Close();
                Channels.Remove(c);
            }
            return Task.CompletedTask;
        }, async (c) =>
        {
            await c.CloseAsync();
            await SendMessageAsync(new ChannelManagementMessage
            {
                Type = typeof(UdpChannel).Name,
                Info = new Dictionary<string, string>
                {
                    { "IdPort", c.Remote.Port.ToString() },
                    { "Mode", "Create" }
                }
            });
        });
    }
}