using Net.Connection.Channels;
using Net.Connection.Clients;
using Net.Connection.Servers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForms
{
    public partial class Form1 : Form
    {
        Server s;
        Client c;
        Guid cId;
        int width, height;

        public Form1()
        {
            InitializeComponent();
            s = new Server(IPAddress.Loopback, 7777, 1);
            s.StartServer();
            s.OnClientChannelOpened = WaitForImage;
            s.OnClientObjectReceived = RecievedObject;
            c = new Client(IPAddress.Loopback, 7777);
            c.Connect();
            cId = c.OpenChannel();
        }

        public void CopyImageFromScreen()
        {
            Color[,] colorMap;
            using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                     Screen.PrimaryScreen.Bounds.Y,
                                     0, 0,
                                     bmpScreenCapture.Size,
                                     CopyPixelOperation.SourceCopy);
                }
                width = bmpScreenCapture.Width;
                height = bmpScreenCapture.Height;
                colorMap = new Color[width, height];

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        bmpScreenCapture.GetPixel(x, y);
            }
            c.SendObject(colorMap);
        }

        void WaitForImage(Guid cId, ServerClient c)
        {

        }

        void RecievedObject(object o, ServerClient c)
        {
            Color[,] co = o as Color[,];

            Bitmap b = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    b.SetPixel(x, y, co[x, y]);

            BackgroundImage = b;
        }
    }
}
