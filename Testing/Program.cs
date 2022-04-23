using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
            Addr = IPAddress.Parse("192.168.0.10");
            Console.Write("Server IP: ");
            break;
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

    private static async void C1_OnChannelOpened(Guid obj)
    {
        var bytes = await Client.RecieveBytesFromChannelAsync(obj);
        Console.WriteLine($"{obj}: {Encoding.UTF8.GetString(bytes)}");
        Client.Channels[obj].Dispose();
        Client.Channels.Remove(obj);
    }

    private static void C1_OnDisconnect()
    {
        Console.WriteLine("Disconnected");
        //InitializeClient();
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
        Client.OnRecieveObject += rec;
        Client.OnDisconnect += C1_OnDisconnect;
        Client.OnChannelOpened += C1_OnChannelOpened;
    }
}