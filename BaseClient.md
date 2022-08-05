# BaseClient
Abstract base class of all client objects

#### Fields/Properties

- `List<`[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)`> Channels`

#### Methods

- `public abstract void SendMessage(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message)`
- `public abstract Task SendMessageAsync(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message, CancellationToken token = default)`
- `protected abstract IEnumerable<`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)`> ReceiveMessages()`
- `protected abstract IAsyncEnumerable<MessageBase> ReceiveMessagesAsync()`
- `public abstract `[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)` OpenChannel()`
- `public abstract void CloseChannel(`[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)` c)`
- `public abstract Task CloseChannelAsync(`[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)` c, CancellationToken token = default)`
- `public abstract Task<`[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)`> OpenChannelAsync(CancellationToken token = default)`
- `public abstract void Close()`
- `public abstract Task CloseAsync()`