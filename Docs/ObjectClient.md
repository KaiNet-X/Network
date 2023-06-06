# ObjectClient : ObjectClient\<TcpChannel\>

The object client is the base class for Client and ServerClient and provides a concrete implementation of ObjectClient\<MainChannel\>, as well as provides methods to send objects. This client uses a TcpChannel for communication.

Namespace: `Net.Connection.Clients.Tcp`

#### Fields/Properties:
- `ConnectState ConnectionState` - State of the connection
- `IPEndPoint LocalEndpoint` - Local endpoint
- `IPEndPoint RemoteEndpoint` - Remote endpoint
- `List<IChannel> Channels` - List of channels

#### Events/Deleages:
- `event Action<object> OnReceiveObject` - Called when an object is received
- `event Action<IChannel> OnChannelOpened` - Called when a channel is opened
- `event Action<bool> OnDisconnect` - Called when disconnected from
- `Action<MessageBase> OnUnregisteredMessage` - Called when there is an unregistered message

#### Methods:
- `void SendObject<T>(T obj)` - Sends an object to the server
- `async Task SendObjectAsync<T>(T obj, CancellationToken token = default)` - Sends an object to the server
- `void SendMessage(MessageBase msg)` - Sends a message to the server
- `async Task SendMessageAsync(MessageBase msg)` - Sends a message to the server
- `void Close()` - Closes the connection
- `void CloseAsync()` - Closes the connection
- `public async Task<T> OpenChannelAsync<T>() where T : class, IChannel` - Opens a channel
- `void CloseChannel(IChannel c)` - Closes and removes a channel
- `async Task CloseChannelAsync(IChannel c, CancellationToken token = default)` - Closes and removes a channel
- `void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase` - Registers handler for a custom message type
- `void RegisterMessageHandler(Action<MessageBase> handler, Type messageType)` - Registers handler for a custom message type
