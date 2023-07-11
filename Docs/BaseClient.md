# BaseClient
Abstract base class of all client objects

#### Methods

- `public abstract void SendMessage(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message)`
- `public abstract Task SendMessageAsync(`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)` message, CancellationToken token = default)`
- `protected abstract IAsyncEnumerable<MessageBase> ReceiveMessagesAsync()`
- `public abstract void Close()`
- `public abstract Task CloseAsync()`