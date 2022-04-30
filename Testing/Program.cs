using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Net.Connection.Channels;
using Net.Connection.Clients;

namespace ClientTest;

public class Program
{
    public static Client Client;
    public static IPAddress Addr;

    static async Task Main(string[] args)
    {
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
        InitializeClient();

        await Client.ConnectAsync(15);
        Console.WriteLine("Connected");

        Console.Write("Enter your name: ");
        var uname = Console.ReadLine();

        string l;
        while ((l = Console.ReadLine()) != "EXIT")
            Client.SendObject(new MSG { Message = l, Sender = uname});

        await Client.CloseAsync();
    }

    private static async void C1_OnChannelOpened(Channel c)
    {
        var bytes = await c.RecieveBytesAsync();
        Console.WriteLine($"{c.Id}: {Encoding.UTF8.GetString(bytes)}");
        await Client.CloseChannelAsync(c);
    }

    private static void C1_OnDisconnect(bool graceful)
    {
        Console.WriteLine($"Disconnected {(graceful ? "gracefully" : "ungracefully")}");
        Client.Connect(15);
    }

    static void rec(object obj)
    {
        if (obj is MSG msg)
            Console.WriteLine($"{msg.Sender}: {msg.Message}");
    }

    static void InitializeClient()
    {
        Client = new Client(Addr, 6969);
        Client.OnReceiveObject += rec;
        Client.OnDisconnect += C1_OnDisconnect;
        Client.OnChannelOpened += C1_OnChannelOpened;
    }
}