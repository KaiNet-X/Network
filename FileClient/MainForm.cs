namespace FileClient;

using Net.Connection.Clients;
using System.Net;
using Microsoft.VisualBasic;
using System.Diagnostics;

public partial class MainForm : Form
{
    private Client _client
    { 
        get => Program.Client;
        set => Program.Client = value;
    } 

    private string _path;

    string _dir = @$"{Directory.GetCurrentDirectory()}\Files";

    public MainForm()
    {
        InitializeComponent();

        Directory.CreateDirectory(_dir);

        _client = new Client(IPAddress.Parse(Interaction.InputBox("What is a valid server address?", "Address", "127.0.0.1")), 6969);
        _client.Connect(15, true);

        cAddr.Text = _client.LocalEndpoint.Address.ToString();
        cPort.Text = _client.LocalEndpoint.Port.ToString();
        sAddr.Text = _client.RemoteEndpoint.Address.ToString();
        sPort.Text = _client.RemoteEndpoint.Port.ToString();

        _client.OnReceiveObject += (obj) =>
        {
            if (obj is Tree t)
            {
                Invoke(() =>
                {
                    treeView.Nodes.Clear();
                    Invoke(() => { treeView.Nodes.Add(ToNode(t)); });
                });
            }
        };
        _client.CustomMessageHandlers.Add(nameof(FileRequestMessage), async (msg) =>
        {
            var fMsg = msg as FileRequestMessage;

            Directory.CreateDirectory(_dir);

            using (FileStream fs = File.Create($@"{_dir}\{fMsg.FileName}"))
            {
                await fs.WriteAsync(fMsg.FileData);
            }
        });
        _client.OnChannelOpened += async (obj) =>
        {
            var bytes = await obj.RecieveBytesAsync();

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

    private async void uploadButton_Click(object sender, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            using FileStream fs = File.OpenRead(ofd.FileName);
            var newMsg = new FileRequestMessage() { RequestType = FileRequestMessage.FileRequestType.Upload, FileName = ofd.SafeFileName, PathRequest = "upload" };
            newMsg.FileData = new byte[fs.Length];
            await fs.ReadAsync(newMsg.FileData);
            await _client.SendMessageAsync(newMsg);
        }
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
            treeNode.Nodes.Add(ToNode(node));
        return treeNode;
    }

    private void directoryButton_Click(object sender, EventArgs e) =>
        Process.Start("explorer.exe", _dir);
}
