using Net.Connection.Clients;

namespace FileClient;

public partial class MainForm : Form
{
    private Client _client => Program.Client;
    public MainForm()
    {
        InitializeComponent();

        cAddr.Text = _client.LocalEndpoint.Address.ToString();
        cPort.Text = _client.LocalEndpoint.Port.ToString();
        sAddr.Text = _client.RemoteEndpoint.Address.ToString();
        sPort.Text = _client.RemoteEndpoint.Port.ToString();

        _client.OnRecieveObject += (obj) =>
        {
            if (obj is Tree t)
            {

            }
        };

        Task.Run(async () =>
        {
            while (_client.ConnectionState != ConnectState.CONNECTED) await Task.Delay(10);

            _client.SendMessage(new FileRequestMessage { RequestType = FileRequestMessage.FileRequestType.Tree });
        });
    }

    private void downloadButton_Click(object sender, EventArgs e)
    {

    }

    private void uploadButton_Click(object sender, EventArgs e)
    {

    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _client.Close();
        Task.Delay(1000).Wait();
    }

    private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
    {

    }
}
