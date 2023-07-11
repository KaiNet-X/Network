# KaiNet.Net

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to transmit objects using this message protocol. Your program is notified via the OnObjectReceived/OnClientObjectReceived events. 

Channels allow you to open parallel connections to a host to send raw byte data. The two built-in channels use Tcp and Udp respectively, but you can implement IChannel and register a custom channel.

Click [here](https://github.com/KaiNet-X/Network/blob/master/Docs/GettingStarted.md) to get started.

Want to donate? [Buy me a coffee!](https://www.buymeacoffee.com/kainet)

Feel free to contribute! If you have any issues or bugs, go ahead and submit an issue, create a pull request, or contribute to the docs.

## V4 Docs (in progress)
*Note: older versions of the docs can be found on this repo's commit history.*

#### Clients (Tcp)

- [Client](https://github.com/KaiNet-X/Network/blob/master/Docs/Client.md) - The client represents one host, and connects to a server. The server will fire an event that provides a ServerClient object.
- [ServerClient](https://github.com/KaiNet-X/Network/blob/master/Docs/ServerClient.md) - Once a client connects to the server, it fires an event that provides a ServerClient object. This object is used to communicate with the remote client. 
- [ObjectClient\<TcpChannel\>](https://github.com/KaiNet-X/Network/blob/master/Docs/ObjectClient.md) - The base class of server and client, encapsulating the common functionality of both.
- All of these client types are also able to open channels that use different protocol types, and new channel types can be configured to work with them.

#### Server (Tcp)

- [Server](https://github.com/KaiNet-X/Network/blob/master/Docs/Server.md) - Accepts incomming connections from clients and fires an event when new ones come in. Allows for simple management and one-to-many interaction. By default, encrypts the main connection.

#### Clients (Common base)

- [ObjectClient\(generic\)](https://github.com/KaiNet-X/Network/blob/master/Docs/ObjectClient_MainChannel_.md) - A generic client object that provides the ability to send and receive objects, as well as functionality to use channels. It is the base class of ObjectClient\<TcpChannel\>.
- [GeneralClient\(generic\)](https://github.com/KaiNet-X/Network/blob/master/Docs/GeneralClient.md) - Generic client that impliments the message protocol and handles a connection handshake by default. It is the base class of ObjectClient\(generic\)

#### Channels

- [TcpChannel](https://github.com/KaiNet-X/Network/blob/master/Docs/TcpChannel.md) - Tcp communication channel
- [UdpChannel](https://github.com/KaiNet-X/Network/blob/master/Docs/UdpChannel.md) - Udp communication channel
- [EncryptedTcpChannel](https://github.com/KaiNet-X/Network/blob/master/Docs/EncryptedTcpChannel.md) - Tcp communication channel that implements simple end-to-end encryption.

A comprehensive list of public types defined in this library can be found here, although many of them don't yet have dedicated documentation pages. [Types](https://github.com/KaiNet-X/Network/blob/master/Docs/AllTypes.md)
