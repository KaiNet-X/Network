# Client
The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.

#### Constructors

- `Client(IPEndPoint ep)` - Client targeting endpoint
- `Client(IPAddress address, int port)` - Client targeting endpoint
- `Client(string address, int port)` - Client targeting endpoint

#### Fields/Properties
- `readonly Dictionary<string, Action<`[MessageBase]()`>> CustomMessageHandlers` - Handlers for custom messages
- `Dictionary<Guid, `[Channel]()`> Channels` - Dictionary of channels by their ID
- `IPEndPoint LocalEndpoint` - Local endpoint
- `IPEndPoint RemoteEndpoint`- Remote endpoint
- [ConnectState]()` ConnectionState { get; protected set; }` - State of the connection
- `ushort LoopDelay` - Delay between client updates; highly reduces CPU usage

#### Events/Delegates

- `event Action<`[MessageBase]()`> OnReceivedUnregisteredCustomMessage` - Invoked when an unregistered message is recieved
- `event Action<bool> OnDisconnect` - Invoked when disconnected from
- `event Action<object> OnReceiveObject` - Invoked when an object is received
- `event Action<`[Channel]()`> OnChannelOpened` - Invoked when a channel is opened

#### Methods
- `void SendObject<T>(T obj)` - Sends an object to the server
- `async Task SendObjectAsync<T>(T obj, CancellationToken token = default)` - Sends an object to the server
- `void SendMessage(`[MessageBase]()` msg)` - Sends a message to the server
- `async Task SendMessageAsync(`[MessageBase]()` msg)` - Sends a message to the server
- `void Close()` - Closes the connection
- `void CloseAsync()` - Closes the connection
- `void OpenChannel()` - Opens a channel
- `async Task OpenChannelAsync(CancellationToken token = default)` - Opens a channel
- `void CloseChannel(`[Channel]()` c)` - Closes and removes a channel
- `async Task CloseChannelAsync(`[Channel]()` c, CancellationToken token = default)` - Closes and removes a channel
- `void CloseChannel(Guid id)` - Closes and removes a channel
- `async Task CloseChannelAsync(Guid id, CancellationToken token = default)` - Closes and removes a channel
- `void SendBytesOnChannel(byte[] bytes, Guid id)` - Sends raw bytes on a channel
- `async Task SendBytesOnChannelAsync(byte[] bytes, Guid id, CancellationToken token = default)` - Sends raw bytes on a channel
- `void ReceiveBytesOnChannel(Guid id)` - Receives raw bytes on a channel
- `async Task ReceiveBytesOnChannelAsync(Guid id, CancellationToken token = default)` - Receives raw bytes on a channel