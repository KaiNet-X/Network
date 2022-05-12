# Network

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to transmit objects using this message protocol. Your program is notified via the OnObjectReceived/OnClientObjectReceived events. The default implementations also have the ability to send raw UDP data (doesn't use the message protocol) via channels.

Click [here](https://github.com/KaiNet-X/Network/blob/master/gettingStarted.md) to get started, or [here](https://github.com/KaiNet-X/Network/blob/master/ExtensiveDocs.md) for more extensive documentation.

Want to donate? [Buy me a coffee!](https://www.buymeacoffee.com/kainet)

This documentation is in progress. All the essential classes are covered, but the rest will be added and updated periodically.
