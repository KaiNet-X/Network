# BaseServer<TClient, TChannel> where TClient : [BaseClient](https://github.com/KaiNet-X/Network/blob/master/BaseClient.md)\<TChannel> where TChannel : [IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)

Abstract baseclass for servers

#### Fields/Properties
- `readonly virtual List<TClient> Clients`

#### Methods
- `abstract void Start()`
- `abstract Task StartAsync()`
- `abstract void SendMessageToAll([MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md) msg)`
- `abstract Task SendMessageToAllAsync([MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md) msg, CancellationToken token = default)`
- `abstract void ShutDown()`
- `abstract Task ShutDownAsync()`
- `abstract void Stop()`
- `abstract Task StopAsync()`