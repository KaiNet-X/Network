namespace Net.Connection.Clients
{
    using System.Threading.Tasks;

    public interface IConnectable
    {
        public void Connect();
        public Task ConnectAsync();
    }
}