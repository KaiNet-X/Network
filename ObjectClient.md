# ObjectClient : [GeneralClient](https://github.com/KaiNet-X/Network/blob/master/GeneralClient.md)<[Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md)>

The object client is the base class for [Client](https://github.com/KaiNet-X/Network/blob/master/Client.md) and [ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md) and provides a concrete implementation of [GeneralClient](https://github.com/KaiNet-X/Network/blob/master/GeneralClient.md) using [Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md), as well as provides ways of sending objects.

#### Constructors:
- `ObjectClient()`

#### Fields/Properties:
- Derived from base class

#### Events/Deleages:
- `event Action<object> OnReceiveObject`
- `event Action<`[Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md)`> OnChannelOpened`
- Rest derived from base class

#### Methods:
- `virtual void SendObject<T>(T obj)`
- `virtual async Task SendObjectAsync<T>(T obj, CancellationToken token = default)`
- `void CloseChannel(Guid id)`
- `void SendBytesOnChannel(byte[] bytes, Guid id)`
- `async Task SendBytesOnChannelAsync(byte[] bytes, Guid id, CancellationToken token = default)`
- `byte[] ReceiveBytesFromChannel(Guid id)`
- `async Task<byte[]> ReceiveBytesFromChannelAsync(Guid id, CancellationToken token = default)`
-  Rest derived from base class