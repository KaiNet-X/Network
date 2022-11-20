namespace UnitTests;

using Net.Connection.Servers;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

public static class Helpers
{
    public static Server Server 
    {
        get
        {
            server ??= new Server(new IPEndPoint(IPAddress.Loopback, 0), 10, new Net.NetSettings
            {
                EncryptChannels = false,
                UseEncryption = false,
            });
            if (!server.Active)
                server.Start();
            return server;
        }
    }

    public static Server EncryptedServer
    {
        get
        {
            encryptedServer ??= new Server(new IPEndPoint(IPAddress.Loopback, 0), 10, new Net.NetSettings
            {
                EncryptChannels = true,
                UseEncryption = true,
            });
            if (!encryptedServer.Active)
                encryptedServer.Start();
            return encryptedServer;
        }
    }

    private static Server server;
    private static Server encryptedServer;

    public static int Port = 10000;

    public static bool AreEqual<T>(T A, object B)
    {
        if (A != null && B != null)
        {
            var type = typeof(T);
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var allSimpleProperties = allProperties.Where(pi => pi.PropertyType.IsSimpleType());
            var unequalProperties =
                   from pi in allSimpleProperties
                   let AValue = type.GetProperty(pi.Name).GetValue(A, null)
                   let BValue = type.GetProperty(pi.Name).GetValue(B, null)
                   where AValue != BValue && (AValue == null || !AValue.Equals(BValue))
                   select pi.Name;
            return unequalProperties.Count() == 0;
        }
        else
        {
            throw new ArgumentNullException("You need to provide 2 non-null objects");
        }
    }

    public static bool IsSimpleType(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // nullable type, check if the nested type is simple.
            return type.GetGenericArguments()[0].IsSimpleType();
        }
        return type.IsPrimitive
          || type.IsEnum
          || type.Equals(typeof(string))
          || type.Equals(typeof(decimal));
    }

    public static int WaitForPort(int port)
    {
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        while (ipGlobalProperties.GetActiveTcpConnections().Where(pr => pr.LocalEndPoint.Port == port).Any()) 
            ;

        return port;
    }

    public static int WaitForPort() =>
        WaitForPort(Port);
}
