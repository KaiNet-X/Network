# BaseServer<TClient, TChannel> where TClient : [BaseClient]()\<TChannel> where TChannel : [IChannel]()

Abstract baseclass for servers

#### Fields/Properties
- `readonly virtual List<TClient> Clients`

#### Methods
- `abstract void Start()`
- `abstract Task StartAsync()`
- `abstract void SendMessageToAll(MessageBase msg)`
- `abstract Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default)`
- `abstract void ShutDown()`
- `abstract Task ShutDownAsync()`
- `abstract void Stop()`
- `abstract Task StopAsync()`