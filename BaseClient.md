# BaseClient<TChannel> where TChannel : <[IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)>

Abstract baseclass of all client objects

#### Fields/Properties

- `Dictionary<Guid, TChannel> Channels`

#### Methods

- `abstract void SendMessage(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message)`
- `abstract Task SendMessageAsync(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message, CancellationToken token = default)`
- `abstract IEnumerable<`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)`> RecieveMessages()`
- `abstract TChannel OpenChannel()`
- `abstract void CloseChannel(TChannel c)`
- `abstract Task CloseChannelAsync(TChannel c, CancellationToken token = default)`
- `abstract Task<TChannel> OpenChannelAsync(CancellationToken token = default)`
- `abstract void Close()`
- `abstract Task CloseAsync()`