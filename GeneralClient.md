# GeneralClient\<TChannel> : [BaseClient](https://github.com/KaiNet-X/Network/blob/master/BaseClient.md)\<TChannel> where TChannel : [IChannel](https://github.com/KaiNet-X/Network/blob/master/IChannel.md)

The general client is the base class for [ObjectClient](https://github.com/KaiNet-X/Network/blob/master/ObjectClient.md) and inherrits [BaseClient](https://github.com/KaiNet-X/Network/blob/master/BaseClient.md), and handles everything to do with the connection once it is initiated.

#### Fields/Properties:
- `IPEndPoint LocalEndpoint { get; }`
- `IPEndPoint RemoteEndpoint { get; }`
- `ConnectState ConnectionState { get; protected set; }`
- `readonly Dictionary<string, Action<MessageBase>> CustomMessageHandlers`
- Rest derived from base class

#### Events/Deleages:
- `event Action<`[MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)`> OnUnregisteredMessage`
- `event Action<bool> OnDisconnect`
- Rest derived from base class

#### Methods:
-  Derived from base class