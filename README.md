# KaiNet.Net

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to transmit objects using this message protocol. Your program is notified via the OnObjectReceived/OnClientObjectReceived events. 

Channels allow you to open parallel connections to a host to send raw byte data. The two built-in channels use Tcp and Udp respectively, but you can implement IChannel and register a custom channel.

Click [here](https://github.com/KaiNet-X/Network/blob/master/gettingStarted.md) to get started.

Want to donate? [Buy me a coffee!](https://www.buymeacoffee.com/kainet)

See the [v2](https://github.com/KaiNet-X/Network/blob/master/V2Docs.md) docs.

V3 docs in progress.