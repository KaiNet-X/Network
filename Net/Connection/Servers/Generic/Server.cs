﻿namespace Net.Connection.Servers.Generic;

using Channels;
using Clients.Generic;
using Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Default server implementation
/// </summary>
public abstract class Server<ConnectionType> : BaseServer<ServerClient<ConnectionType>> where ConnectionType : class, IChannel
{
    private volatile SemaphoreSlim _semaphore;

    /// <summary>
    /// If the server is active or not
    /// </summary>
    public bool Active { get; private set; } = false;
    
    /// <summary>
    /// If the server is listening for connections
    /// </summary>
    public bool Listening { get; private set; } = false;

    /// <summary>
    /// Settings for this server that are set in the constructor
    /// </summary>
    public readonly NetSettings Settings;

    /// <summary>
    /// Max connections at one time
    /// </summary>
    public ushort? MaxClients;

    /// <summary>
    /// Handlers for custom message types
    /// </summary>
    public Dictionary<string, Action<MessageBase, ServerClient<ConnectionType>>> CustomMessageHandlers = new();

    /// <summary>
    /// Delay between client updates; highly reduces CPU usage
    /// </summary>
    public ushort LoopDelay = 1;

    /// <summary>
    /// Invoked when a channel is opened on a client
    /// </summary>
    public event Action<IChannel, ServerClient<ConnectionType>> OnClientChannelOpened;

    /// <summary>
    /// Invoked when a client receives an object
    /// </summary>
    public event Action<object, ServerClient<ConnectionType>> OnClientObjectReceived;

    /// <summary>
    /// Invoked when a client receives an unregistered custom message
    /// </summary>
    public event Action<MessageBase, ServerClient<ConnectionType>> OnUnregisteredMessege;

    /// <summary>
    /// Invoked when a client is connected
    /// </summary>
    public event Action<ServerClient<ConnectionType>> OnClientConnected;

    /// <summary>
    /// Invoked when a client disconnects
    /// </summary>
    public event Action<ServerClient<ConnectionType>, bool> OnClientDisconnected;

    /// <summary>
    /// Sends an object to all clients
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public void SendObjectToAll<T>(T obj) =>
        SendMessageToAll(new ObjectMessage(obj));

    /// <summary>
    /// Sends an object to all clients
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public async Task SendObjectToAllAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageToAllAsync(new ObjectMessage(obj), token);


    public override void Start()
    {
        Active = Listening = true;

        if (Settings.SingleThreadedServer)
            Task.Run(async () =>
            {
                while (Active)
                {
                    await Utilities.ConcurrentAccessAsync(async (ct) =>
                    {
                        foreach (var c in Clients)
                        {
                            if (ct.IsCancellationRequested || c.ConnectionState == ConnectState.CLOSED)
                                return;
                            await c.GetNextMessageAsync();
                        }
                    }, _semaphore);
                }
            });

        Task.Run(async () =>
        {
            while (Listening)
            {
                if (MaxClients != null && Clients.Count >= MaxClients)
                {
                    await Task.Delay(LoopDelay);
                    continue;
                }

                var c = await InitializeClient();

                c.OnChannelOpened += (ch) => OnClientChannelOpened?.Invoke(ch, c);
                c.OnReceiveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                c.OnDisconnect += async (g) =>
                {
                    await Utilities.ConcurrentAccessAsync((ct) =>
                    {
                        Clients.Remove(c);
                        return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                    }, _semaphore);

                    OnClientDisconnected?.Invoke(c, g);
                };
                c.OnUnregisteredMessage += (m) =>
                {
                    OnUnregisteredMessege?.Invoke(m, c);
                };

                foreach (var v in CustomMessageHandlers)
                    c.CustomMessageHandlers.Add(v.Key, (msg) => v.Value(msg, c));

                await Utilities.ConcurrentAccessAsync((ct) =>
                {
                    Clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (!Settings.SingleThreadedServer)
                    Task.Run(async () =>
                    {
                        while (c.ConnectionState != ConnectState.CLOSED && Active)
                        {
                            await c.GetNextMessageAsync();
                        }
                    });
                while (c.ConnectionState == ConnectState.PENDING) ;

                c.SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                OnClientConnected?.Invoke(c);
            }
        });
    }

    public override async Task StartAsync()
    {
        await Task.Run(Start);
    }

    public override void ShutDown()
    {
        Stop();
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.Close();
        }, _semaphore);
    }

    public override async Task ShutDownAsync()
    {
        await StopAsync();
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.CloseAsync();
        }, _semaphore);
    }

    public override void SendMessageToAll(MessageBase msg)
    {
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.SendMessage(msg);
        }, _semaphore);
    }

    public override async Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default)
    {
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.SendMessageAsync(msg, token);
        }, _semaphore);
    }

    /// <summary>
    /// Registers an object type. This is used as an optimization before the server sends or receives objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    protected abstract Task<ServerClient<ConnectionType>> InitializeClient();
}

public class Server : Server<IChannel>
{
    private Dictionary<Type, Func<Task<ServerClient<IChannel>>>> ListenMethods;
    private List<Task<ServerClient<IChannel>>> WaitList;

    public override void Start()
    {
        base.Start();
    }

    public override void Stop()
    {
        throw new NotImplementedException();
    }

    public override Task StopAsync()
    {
        throw new NotImplementedException();
    }

    protected override async Task<ServerClient<IChannel>> InitializeClient()
    {
        var task = await Task.WhenAny(WaitList);
        WaitList.Remove(task);
        var result = task.GetAwaiter().GetResult();
        WaitList.Add(ListenMethods[result.GetType()]());
        return result;
    }
}