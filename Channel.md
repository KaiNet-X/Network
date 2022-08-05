# Channel : [IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)

This class is normally managed by a client object, but can be used on it's own. The default implementation of channel uses UDP to send raw byte data between endpoints. When used with a client, it uses encryption based on the client's settings. This may change in the future.

NOTE: this class hasn't been fully tested using encryption and may throw exceptions when attempting to decrypt malformed data. Handeling exceptions/otherwise malformed data is up to the user

#### Constructors

- `Channel(IPAddress localAddr, IPEndPoint remote, Guid? id = null)` - If you want to directly create a channel, use this constructor.
- `Channel(IPAddress localAddr, Guid? id = null)` - Used when the remote endpoint is not yet known

#### Fields/Properties

- `readonly Guid Id` - ID of the channel
- `byte[] AesKey` - Optional symetric encryption key
- `IPEndPoint LocalEndpoint {get; private set; }` - Local endpoint
- `IPEndPoint RemoteEndpoint {get; private set; }` - Remote endpoint
- `bool Connected { get; private set; }`- Get or set connection state
- `bool Disposed { get; private set; }` - Check if disposed

#### Methods

- `void SendBytes(byte[] data)` - Send raw bytes
- `byte[] RecieveBytes()` - Receive raw bytes
- `async Task SendBytesAsync(byte[] data, CancellationToken token = default)` - Send raw bytes
- `Task<byte[]> RecieveBytesAsync(CancellationToken token = default)` - Receive raw bytes
- `void SetRemote(IPEndPoint remote)` - Only use when calling second constructor