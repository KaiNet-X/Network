# ServerClient  : ObjectClient
The default ServerClient is the server-side representation of a client and has many of the same methods as a regular client.

Namespace: `Net.Connection.Clients.Tcp`

#### Fields/Properties
- `List<IChannel> Channels` - List of channels
- `IPEndPoint LocalEndpoint` - Local endpoint
- `IPEndPoint RemoteEndpoint`- Remote endpoint
- `ConnectState ConnectionState { get; protected set; }` - State of the connection

#### Events/Delegates

- `event Action<MessageBase> OnUnregisteredMessage` - Invoked when an unregistered message is recieved
- `event Action<bool> OnDisconnect` - Invoked when disconnected from
- `event Action<object> OnReceiveObject` - Invoked when an object is received
- `event Action<IChannel> OnChannelOpened` - Invoked when a channel is opened

#### Methods
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
