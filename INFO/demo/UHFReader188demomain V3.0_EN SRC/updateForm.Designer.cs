namespace UHFReader188demomain
{
    partial class updateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(updateForm));
            this.btOpenFile = new System.Windows.Forms.Button();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btStart = new System.Windows.Forms.Button();
            this.btStop = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.StatusBar1 = new System.Windows.Forms.StatusBar();
            this.statusBPCmdInfo = new System.Windows.Forms.StatusBarPanel();
            this.statusBPBacklogStatu = new System.Windows.Forms.StatusBarPanel();
            this.IR1 = new System.Windows.Forms.StatusBarPanel();
            this.IR2 = new System.Windows.Forms.StatusBarPanel();
            this.IR3 = new System.Windows.Forms.StatusBarPanel();
            this.IR4 = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanel1 = new System.Windows.Forms.StatusBarPanel();
            ((System.ComponentModel.ISupportInitialize)(this.statusBPCmdInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBPBacklogStatu)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).BeginInit();
            this.SuspendLayout();
            // 
            // btOpenFile
            // 
            this.btOpenFile.Location = new System.Drawing.Point(96, 60);
            this.btOpenFile.Name = "btOpenFile";
            this.btOpenFile.Size = new System.Drawing.Size(91, 23);
            this.btOpenFile.TabIndex = 0;
            this.btOpenFile.Text = "打开升级文件";
            this.btOpenFile.UseVisualStyleBackColor = true;
            this.btOpenFile.Click += new System.EventHandler(this.btOpenFile_Click);
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(96, 12);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(361, 21);
            this.txtFileName.TabIndex = 1;
            // 
            // btStart
            // 
            this.btStart.Enabled = false;
            this.btStart.Location = new System.Drawing.Point(215, 60);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(91, 23);
            this.btStart.TabIndex = 2;
            this.btStart.Text = "下载";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.btStart_Click);
            // 
            // btStop
            // 
            this.btStop.Enabled = false;
            this.btStop.Location = new System.Drawing.Point(343, 60);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(91, 23);
            this.btStop.TabIndex = 3;
            this.btStop.Text = "Stop Query Tag";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 12);
            this.label1.TabIndex = 4;
            this.label1.Text = "Bin升级文件：";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(96, 114);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(338, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // StatusBar1
            // 
            this.StatusBar1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.StatusBar1.Location = new System.Drawing.Point(0, 187);
            this.StatusBar1.Name = "StatusBar1";
            this.StatusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBPCmdInfo,
            this.statusBPBacklogStatu,
            this.IR1,
            this.IR2,
            this.IR3,
            this.IR4,
            this.statusBarPanel1});
            this.StatusBar1.ShowPanels = true;
            this.StatusBar1.Size = new System.Drawing.Size(481, 20);
            this.StatusBar1.SizingGrip = false;
            this.StatusBar1.TabIndex = 66;
            this.StatusBar1.Text = "StatusBar1";
            // 
            // statusBPCmdInfo
            // 
            this.statusBPCmdInfo.Name = "statusBPCmdInfo";
            this.statusBPCmdInfo.Width = 380;
            // 
            // statusBPBacklogStatu
            // 
            this.statusBPBacklogStatu.Name = "statusBPBacklogStatu";
            this.statusBPBacklogStatu.Width = 220;
            // 
            // IR1
            // 
            this.IR1.Alignment = System.Windows.Forms.HorizontalAlignment.Center;
            this.IR1.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.IR1.Name = "IR1";
            this.IR1.Width = 10;
            // 
            // IR2
            // 
            this.IR2.Name = "IR2";
            this.IR2.Width = 80;
            // 
            // IR3
            // 
            this.IR3.Name = "IR3";
            this.IR3.Width = 80;
            // 
            // IR4
            // 
            this.IR4.Name = "IR4";
            this.IR4.Width = 80;
            // 
            // statusBarPanel1
            // 
            this.statusBarPanel1.Name = "statusBarPanel1";
            this.statusBarPanel1.Width = 14;
            // 
            // updateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 207);
            this.Controls.Add(this.StatusBar1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(this.btOpenFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "updateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "固件升级";
            ((System.ComponentModel.ISupportInitialize)(this.statusBPCmdInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBPBacklogStatu)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IR4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btOpenFile;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar1;
        internal System.Windows.Forms.StatusBar StatusBar1;
        internal System.Windows.Forms.StatusBarPanel statusBPCmdInfo;
        internal System.Windows.Forms.StatusBarPanel statusBPBacklogStatu;
        internal System.Windows.Forms.StatusBarPanel IR1;
        private System.Windows.Forms.StatusBarPanel IR2;
        private System.Windows.Forms.StatusBarPanel IR3;
        private System.Windows.Forms.StatusBarPanel IR4;
        private System.Windows.Forms.StatusBarPanel statusBarPanel1;
    }
}