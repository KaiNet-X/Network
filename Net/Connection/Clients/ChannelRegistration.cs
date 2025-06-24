namespace Net.Connection.Clients;

using Channels;
using Messages;
using System;
using System.Threading.Tasks;


public class ChannelRegistration
{
    public Func<Task<BaseChannel>> OnOpen { get; private set; }
    public Func<ChannelManagementMessage, Task> OnMessage { get; private set; }
    public Func<BaseChannel, Task> OnClose { get; private set; }

    public ChannelRegistration(Func<Task<BaseChannel>> onOpen, Func<ChannelManagementMessage, Task> onMessage, Func<BaseChannel, Task> onClose)
    {
        OnOpen = onOpen;
        OnMessage = onMessage;
        OnClose = onClose;
    }

    public static ChannelRegistration Create<T>(Func<Task<T>> open, Func<ChannelManagementMessage, Task> channelManagement, Func<T, Task> close) where T : BaseChannel =>
        new ChannelRegistration(async () => await open(), channelManagement, async c => await close((T)c)); 
}