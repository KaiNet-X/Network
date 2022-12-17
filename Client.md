# Client : ObjectClient
The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.

#### Constructors

- `Client(IPEndPoint ep)` - Client targeting endpoint
- `Client(IPAddress address, int port)` - Client targeting endpoint
- `Client(string address, int port)` - Client targeting endpoint

#### Fields/Properties
- `List<IChannel> Channels` - List of channels
- `IPEndPoint LocalEndpoint` - Local endpoint
- `IPEndPoint RemoteEndpoint`- Remote endpoint
- `ConnectState ConnectionState { get; protected set; }` - State of the connection
- `ushort LoopDelay` - Delay between client updates; highly reduces CPU usage

#### Events/Delegates

- `event Action<MessageBase> OnUnregisteredMessage` - Invoked when an unregistered message is recieved
- `event Action<bool> OnDisconnect` - Invoked when disconnected from
- `event Action<object> OnReceiveObject` - Invoked when an object is received
- `event Action<IChannel> OnChannelOpened` - Invoked when a channel is opened

#### Methods
- `void SendObject<T>(T obj)` - Sends an object to the server
- `async Task SendObjectAsync<T>(T obj, CancellationToken token = default)` - Sends an object to the server
- `void SendMessage(MessageBase msg)` - Sends a message to the server
- `async Task SendMessageAsync(MessageBase msg, CancellationToken token = default)` - Sends a message to the server
- `void Close()` - Closes the connection
- `void CloseAsync()` - Closes the connection
- `async Task<IChannel> OpenChannelAsync<C>() where C: IChannel` - Opens a channel (NOTE: the channel has to be pre-registered either manually or built in like TcpChannel and UdpChannel)
- `void CloseChannel(IChannel c)` - Closes and removes a channel
- `async Task CloseChannelAsync(IChannel c, CancellationToken token = default)` - Closes and removes a channel
- `void RegisterChannelType<T>(Func<Task<T>> open, Func<ChannelManagementMessage, Task> channelManagement, Func<T, Task> close) where T : IChannel` - Registeres a custom channel so the client can open it, facilitate it on the other end, and close it automatically. (Must also be done server-side to work)
- `void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase` - Registers handler for a custom message type
- `void RegisterMessageHandler(Action<MessageBase> handler, Type messageType)` - Registers handler for a custom message type
