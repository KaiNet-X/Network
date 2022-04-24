namespace FileClient
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            cAddr.Text = Program.Client.LocalEndpoint.Address.ToString();
            cPort.Text = Program.Client.LocalEndpoint.Port.ToString();
            sAddr.Text = Program.Client.RemoteEndpoint.Address.ToString();
            sPort.Text = Program.Client.RemoteEndpoint.Port.ToString();
        }

        private void downloadButton_Click(object sender, EventArgs e)
        {

        }

        private void uploadButton_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Client.Close();
        }
    }
}