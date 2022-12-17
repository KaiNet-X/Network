# ObjectClient\<MainChannel\> : GeneralClient\<MainChannel\> where MainChannel : class, IChannel

This client is the base class of ObjectClient\<TcpChannel\>. It provides methods of sending and receiving objects, as well as a framework for working with channels. This class can be inherrited to create a client that uses any underlying connection protocol provided by the MainChannel. For example, this could be adapted to send data via bluetooth or websockets.

#### Events/Deleages:
