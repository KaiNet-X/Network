using Net.Connection.Clients;

namespace FileClient;

public partial class MainForm : Form
{
    private Client _client => Program.Client;
    private string _path;

    public MainForm()
    {
        InitializeComponent();

        cAddr.Text = _client.LocalEndpoint.Address.ToString();
        cPort.Text = _client.LocalEndpoint.Port.ToString();
        sAddr.Text = _client.RemoteEndpoint.Address.ToString();
        sPort.Text = _client.RemoteEndpoint.Port.ToString();

        _client.OnReceiveObject += (obj) =>
        {
            if (obj is Tree t)
            {
                treeView.Nodes.Clear();
                Invoke(() => { treeView.Nodes.Add(ToNode(t)); });
            }
        };
        _client.CustomMessageHandlers.Add(nameof(FileRequestMessage), async (msg) =>
        {
            var fMsg = msg as FileRequestMessage;

            var dir = @$"{Directory.GetCurrentDirectory()}\Files";
            Directory.CreateDirectory(dir);

            using (FileStream fs = File.Create($@"{dir}\{fMsg.FileName}"))
            {
                await fs.WriteAsync(fMsg.FileData);
            }
        });
        _client.OnChannelOpened += async (obj) =>
        {
            var bytes = await _client.RecieveBytesFromChannelAsync(obj);

            var dir = @$"{Directory.GetCurrentDirectory()}\Files";
            Directory.CreateDirectory(dir);

            using (FileStream fs = File.Create($@"{dir}\{"FFFF"}"))
            {
                await fs.WriteAsync(bytes);
            }
        };
        _client.OnDisconnect += (obj) =>
        {
            MessageBox.Show("Server disconnected");
            Application.Exit();
        };

        Task.Run(async () =>
        {
            while (_client.ConnectionState != ConnectState.CONNECTED) await Task.Delay(10);

            _client.SendMessage(new FileRequestMessage { RequestType = FileRequestMessage.FileRequestType.Tree });
        });
    }

    private void downloadButton_Click(object sender, EventArgs e)
    {
        var path = _path.Substring(5);

        _client.SendMessage(new FileRequestMessage { RequestType=FileRequestMessage.FileRequestType.Download, PathRequest = path });
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
        _path = e.Node.FullPath;
    }

    private TreeNode ToNode(Tree tree)
    {
        TreeNode treeNode = new TreeNode();
        treeNode.Text = tree.Value;

        foreach (var node in tree.Nodes)
        {
            treeNode.Nodes.Add(ToNode(node));
        }
        return treeNode;
    }
}
