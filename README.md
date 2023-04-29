# KaiNet.Net

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to transmit objects using this message protocol. Your program is notified via the OnObjectReceived/OnClientObjectReceived events. 

Channels allow you to open parallel connections to a host to send raw byte data. The two built-in channels use Tcp and Udp respectively, but you can implement IChannel and register a custom channel.

Click [here](https://github.com/KaiNet-X/Network/blob/master/Docs/GettingStarted.md) to get started.

Want to donate? [Buy me a coffee!](https://www.buymeacoffee.com/kainet)

See the [v2](https://github.com/KaiNet-X/Network/blob/master/Docs/V2Docs.md) docs.

## V3 Docs (in progress)

#### Clients (Tcp)

- [Client](https://github.com/KaiNet-X/Network/blob/master/Docs/Client.md) - The client represents one host, and connects to a server. The server will fire an event that provides a ServerClient object.
- [ServerClient](https://github.com/KaiNet-X/Network/blob/master/Docs/ServerClient.md) - Once a client connects to the server, it fires an event that provides a ServerClient object. This object is used to communicate with the remote client. 
- [ObjectClient\<TcpChannel\>](https://github.com/KaiNet-X/Network/blob/master/Docs/ObjectClient.md) - The base class of server and client, encapsulating the common functionality of both.

#### Server (Tcp)

- [Server](https://github.com/KaiNet-X/Network/blob/master/Docs/Server.md) - Accepts incomming connections from clients as well as ability to manage them.

#### Clients (Common base)

- [ObjectClient\(generic\)](https://github.com/KaiNet-X/Network/blob/master/Docs/ObjectClient_MainChannel_.md) - A generic client object that provides the ability to send and receive objects, as well as functionality to use channels.
- [GeneralClient\(generic\)](https://github.com/KaiNet-X/Network/blob/master/Docs/GeneralClient.md) - Generic client that impliments the message protocol and handles a connection handshake by default.

#### Channels

- [TcpChannel](https://github.com/KaiNet-X/Network/blob/master/Docs/TcpChannel.md) - Tcp communication channel
- [UdpChannel](https://github.com/KaiNet-X/Network/blob/master/Docs/UdpChannel.md) - Udp communication channel
