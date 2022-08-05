# BaseServer\<TClient> where TClient : [BaseClient](https://github.com/KaiNet-X/Network/blob/master/BaseClient.md)

Abstract base class for servers

#### Fields/Properties
- `readonly virtual List<TClient> Clients`

#### Methods
- `public abstract void Start()`
- `public abstract Task StartAsync()`
- `public abstract void SendMessageToAll([MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md) msg)`
- `public abstract Task SendMessageToAllAsync([MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md) msg, CancellationToken token = default)`
- `public abstract void ShutDown()`
- `public abstract Task ShutDownAsync()`
- `public abstract void Stop()`
- `public abstract Task StopAsync()`