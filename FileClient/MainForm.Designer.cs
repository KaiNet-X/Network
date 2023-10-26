namespace FileClient
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            treeView = new TreeView();
            label1 = new Label();
            downloadButton = new Button();
            uploadButton = new Button();
            panel1 = new Panel();
            cPort = new Label();
            cAddr = new Label();
            label10 = new Label();
            label11 = new Label();
            label12 = new Label();
            sPort = new Label();
            sAddr = new Label();
            label6 = new Label();
            label5 = new Label();
            label3 = new Label();
            label2 = new Label();
            directoryButton = new Button();
            deleteFileButton = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // treeView
            // 
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            treeView.Location = new Point(15, 52);
            treeView.Margin = new Padding(3, 2, 3, 2);
            treeView.Name = "treeView";
            treeView.Size = new Size(175, 254);
            treeView.TabIndex = 0;
            treeView.AfterSelect += treeView_AfterSelect;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 23);
            label1.Name = "label1";
            label1.Size = new Size(72, 15);
            label1.TabIndex = 1;
            label1.Text = "Remote files";
            // 
            // downloadButton
            // 
            downloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            downloadButton.Location = new Point(15, 316);
            downloadButton.Margin = new Padding(3, 2, 3, 2);
            downloadButton.Name = "downloadButton";
            downloadButton.Size = new Size(174, 22);
            downloadButton.TabIndex = 2;
            downloadButton.Text = "Download file";
            downloadButton.UseVisualStyleBackColor = true;
            downloadButton.Click += downloadButton_Click;
            // 
            // uploadButton
            // 
            uploadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uploadButton.Location = new Point(216, 316);
            uploadButton.Margin = new Padding(3, 2, 3, 2);
            uploadButton.Name = "uploadButton";
            uploadButton.Size = new Size(181, 22);
            uploadButton.TabIndex = 3;
            uploadButton.Text = "Upload file";
            uploadButton.UseVisualStyleBackColor = true;
            uploadButton.Click += uploadButton_Click;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(cPort);
            panel1.Controls.Add(cAddr);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(sPort);
            panel1.Controls.Add(sAddr);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label3);
            panel1.Location = new Point(216, 52);
            panel1.Margin = new Padding(3, 2, 3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(181, 253);
            panel1.TabIndex = 4;
            // 
            // cPort
            // 
            cPort.Anchor = AnchorStyles.Right;
            cPort.AutoSize = true;
            cPort.Location = new Point(138, 96);
            cPort.Name = "cPort";
            cPort.Size = new Size(29, 15);
            cPort.TabIndex = 15;
            cPort.Text = "Port";
            cPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cAddr
            // 
            cAddr.Anchor = AnchorStyles.Right;
            cAddr.AutoSize = true;
            cAddr.Location = new Point(124, 81);
            cAddr.Name = "cAddr";
            cAddr.Size = new Size(49, 15);
            cAddr.TabIndex = 14;
            cAddr.Text = "Address";
            cAddr.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.Left;
            label10.AutoSize = true;
            label10.Location = new Point(17, 81);
            label10.Name = "label10";
            label10.Size = new Size(52, 15);
            label10.TabIndex = 13;
            label10.Text = "Address:";
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Left;
            label11.AutoSize = true;
            label11.Location = new Point(17, 96);
            label11.Name = "label11";
            label11.Size = new Size(32, 15);
            label11.TabIndex = 12;
            label11.Text = "Port:";
            // 
            // label12
            // 
            label12.Anchor = AnchorStyles.Left;
            label12.AutoSize = true;
            label12.Location = new Point(3, 66);
            label12.Name = "label12";
            label12.Size = new Size(41, 15);
            label12.TabIndex = 11;
            label12.Text = "Client:";
            // 
            // sPort
            // 
            sPort.Anchor = AnchorStyles.Right;
            sPort.AutoSize = true;
            sPort.Location = new Point(138, 30);
            sPort.Name = "sPort";
            sPort.Size = new Size(29, 15);
            sPort.TabIndex = 10;
            sPort.Text = "Port";
            sPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // sAddr
            // 
            sAddr.Anchor = AnchorStyles.Right;
            sAddr.AutoSize = true;
            sAddr.Location = new Point(122, 15);
            sAddr.Name = "sAddr";
            sAddr.Size = new Size(49, 15);
            sAddr.TabIndex = 9;
            sAddr.Text = "Address";
            sAddr.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Left;
            label6.AutoSize = true;
            label6.Location = new Point(13, 15);
            label6.Name = "label6";
            label6.Size = new Size(52, 15);
            label6.TabIndex = 8;
            label6.Text = "Address:";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Left;
            label5.AutoSize = true;
            label5.Location = new Point(13, 30);
            label5.Name = "label5";
            label5.Size = new Size(32, 15);
            label5.TabIndex = 7;
            label5.Text = "Port:";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new Point(-1, 0);
            label3.Name = "label3";
            label3.Size = new Size(42, 15);
            label3.TabIndex = 5;
            label3.Text = "Server:";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(220, 23);
            label2.Name = "label2";
            label2.Size = new Size(96, 15);
            label2.TabIndex = 0;
            label2.Text = "Connection stats";
            // 
            // directoryButton
            // 
            directoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            directoryButton.Image = Properties.Resources.icons8_folder_48;
            directoryButton.Location = new Point(355, 9);
            directoryButton.Margin = new Padding(3, 2, 3, 2);
            directoryButton.Name = "directoryButton";
            directoryButton.Size = new Size(42, 36);
            directoryButton.TabIndex = 5;
            directoryButton.UseVisualStyleBackColor = true;
            directoryButton.Click += directoryButton_Click;
            // 
            // deleteFileButton
            // 
            deleteFileButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            deleteFileButton.Location = new Point(16, 349);
            deleteFileButton.Margin = new Padding(3, 2, 3, 2);
            deleteFileButton.Name = "deleteFileButton";
            deleteFileButton.Size = new Size(174, 22);
            deleteFileButton.TabIndex = 6;
            deleteFileButton.Text = "Delete file";
            deleteFileButton.UseVisualStyleBackColor = true;
            deleteFileButton.Click += deleteFileButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(408, 382);
            Controls.Add(deleteFileButton);
            Controls.Add(directoryButton);
            Controls.Add(label2);
            Controls.Add(panel1);
            Controls.Add(uploadButton);
            Controls.Add(downloadButton);
            Controls.Add(label1);
            Controls.Add(treeView);
            Margin = new Padding(3, 2, 3, 2);
            MinimumSize = new Size(361, 385);
            Name = "MainForm";
            Text = "File client v1";
            FormClosing += MainForm_FormClosing;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView treeView;
        private Label label1;
        private Button downloadButton;
        private Button uploadButton;
        private Panel panel1;
        private Label label2;
        private Label label3;
        private Label cPort;
        private Label cAddr;
        private Label label10;
        private Label label11;
        private Label label12;
        private Label sPort;
        private Label sAddr;
        private Label label6;
        private Label label5;
        private Button directoryButton;
        private Button deleteFileButton;
    }
}