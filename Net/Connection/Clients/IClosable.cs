using System.Threading.Tasks;

namespace Net.Connection.Clients
{
    public interface IClosable
    {
        public ConnectState ConnectionState { get; protected set; }
        public void Close();
        public Task CloseAsync();

        public enum ConnectState
        {
            PENDING,
            CONNECTED,
            CLOSED
        }
    }
}