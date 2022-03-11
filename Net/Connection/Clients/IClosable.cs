using System.Threading.Tasks;

namespace Net.Connection.Clients
{
    public interface IClosable
    {
        public void Close();
        public Task CloseAsync();
    }
}