using System;
using System.Net;
using System.Text;
using CmdLineMsgClient;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;

Client client;
IPAddress Addr;

while (true)
{
    Console.Write("Server IP: ");
    if (IPAddress.TryParse(Console.ReadLine(), out IPAddress addr))
    {
        Addr = addr;
        InitializeClient();
        break;
    }
}

//Tries to connect up to 5 times, if there is an exception, throw it
await client.ConnectAsync(5, true);

Console.WriteLine("Connected");

Console.Write("Enter your name: ");
var uname = Console.ReadLine();

string l;
while ((l = Console.ReadLine()) != "EXIT")
    client.SendObject(new MSG { Message = l, Sender = uname});

await client.CloseAsync();

async void C1_OnChannelOpened(UdpChannel c)
{
    byte[] bytes = null;
    while (bytes == null || bytes.Length == 0)
        bytes = c.ReceiveBytes();

    Console.WriteLine($"{c.Local.Port}: {Encoding.UTF8.GetString(bytes)}");
    await client.CloseChannelAsync(c);
}

void C1_OnDisconnect(bool graceful)
{
    Console.WriteLine($"Disconnected {(graceful ? "gracefully" : "ungracefully")}");
    client.Connect(15);
}

void rec(object obj)
{
    if (obj is MSG msg)
        Console.WriteLine($"{msg.Sender}: {msg.Message}");
}

void InitializeClient()
{
    //Target the client to the chosen address and port 5555
    client = new Client(Addr, 5555);
    client.OnReceiveObject += rec;
    client.OnDisconnect += C1_OnDisconnect;
    client.OnChannelOpened += C1_OnChannelOpened;
}