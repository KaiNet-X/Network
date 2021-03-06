# Net

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box 
implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to 
transmit objects using this message protocol. Your program is notified via the OnObjectRecieved/OnClientObjectRecieved events.
The default implementations also have the ability to send raw UDP data (doesn't use the message protocol) via channels.

Click [here]() to get started, or [here]() for more extensive documentation.