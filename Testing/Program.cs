using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Net.Connection.Clients;

namespace ClientTest;

public class Program
{
    public static Client c1;

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.Write("Server IP: ");
            if(IPAddress.TryParse(Console.ReadLine(), out IPAddress addr))
            {
                c1 = new Client(addr, 6969);
                break;
            }
        }

        c1.OnRecieveObject += rec;
        c1.OnDisconnect += C1_OnDisconnect;
        c1.OnChannelOpened += C1_OnChannelOpened;
        await c1.ConnectAsync(15);
        Console.WriteLine("Connected");

        Console.Write("Enter your name: ");
        var uname = Console.ReadLine();

        string l;
        while ((l = Console.ReadLine()) != "EXIT")
        {
            c1.SendObject(new MSG { Message = l, Sender = uname});
        }
        await c1.CloseAsync();
    }

    private static async void C1_OnChannelOpened(Guid obj)
    {
        var bytes = await c1.RecieveBytesFromChannelAsync(obj);
        Console.WriteLine($"{obj}: {Encoding.UTF8.GetString(bytes)}");
        c1.Channels[obj].Dispose();
        c1.Channels.Remove(obj);
    }

    private static void C1_OnDisconnect()
    {
        Console.WriteLine("Disconnected");
        c1.Connect(15);
    }

    static void rec(object obj)
    {
        if (obj is MSG msg)
            Console.WriteLine($"{msg.Sender}: {msg.Message}");
    }
}