using Net.Connection.Clients;
using System.Net;

#nullable disable
namespace FileClient
{
    internal static class Program
    {
        public static Client Client { get; private set; }
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Client = new Client(IPAddress.Parse("192.168.0.10"), 6969);
            Client.Connect();
            Application.Run(new MainForm());
        }
    }
}