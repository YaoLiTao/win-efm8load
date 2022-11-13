namespace win_efm8load
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openFileButton = new System.Windows.Forms.Button();
            this.infoTextBox = new System.Windows.Forms.TextBox();
            this.panel = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.resetMucButton = new System.Windows.Forms.Button();
            this.baudRatelabel = new System.Windows.Forms.Label();
            this.scanComComboBox = new System.Windows.Forms.ComboBox();
            this.baudRateComboBox = new System.Windows.Forms.ComboBox();
            this.scanComButton = new System.Windows.Forms.Button();
            this.programButton = new System.Windows.Forms.Button();
            this.openFileTextBox = new System.Windows.Forms.TextBox();
            this.scanMcuButton = new System.Windows.Forms.Button();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileButton
            // 
            this.openFileButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.openFileButton.Location = new System.Drawing.Point(5, 115);
            this.openFileButton.Name = "openFileButton";
            this.openFileButton.Size = new System.Drawing.Size(97, 29);
            this.openFileButton.TabIndex = 1;
            this.openFileButton.Text = "打开文件";
            this.openFileButton.UseVisualStyleBackColor = true;
            this.openFileButton.Click += new System.EventHandler(this.openFileButton_Click);
            // 
            // infoTextBox
            // 
            this.infoTextBox.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.infoTextBox.Location = new System.Drawing.Point(266, 6);
            this.infoTextBox.Multiline = true;
            this.infoTextBox.Name = "infoTextBox";
            this.infoTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.infoTextBox.Size = new System.Drawing.Size(519, 303);
            this.infoTextBox.TabIndex = 0;
            this.infoTextBox.WordWrap = false;
            // 
            // panel
            // 
            this.panel.Controls.Add(this.progressBar);
            this.panel.Controls.Add(this.resetMucButton);
            this.panel.Controls.Add(this.baudRatelabel);
            this.panel.Controls.Add(this.scanComComboBox);
            this.panel.Controls.Add(this.baudRateComboBox);
            this.panel.Controls.Add(this.scanComButton);
            this.panel.Controls.Add(this.programButton);
            this.panel.Controls.Add(this.openFileTextBox);
            this.panel.Controls.Add(this.scanMcuButton);
            this.panel.Controls.Add(this.openFileButton);
            this.panel.Location = new System.Drawing.Point(7, 6);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(253, 303);
            this.panel.TabIndex = 2;
            // 
            // progressBar
            // 
            this.progressBar.ForeColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.progressBar.Location = new System.Drawing.Point(5, 276);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(240, 17);
            this.progressBar.Step = 1;
            this.progressBar.TabIndex = 12;
            // 
            // resetMucButton
            // 
            this.resetMucButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.resetMucButton.Location = new System.Drawing.Point(5, 165);
            this.resetMucButton.Name = "resetMucButton";
            this.resetMucButton.Size = new System.Drawing.Size(97, 29);
            this.resetMucButton.TabIndex = 11;
            this.resetMucButton.Text = "重启芯片";
            this.resetMucButton.UseVisualStyleBackColor = true;
            this.resetMucButton.Click += new System.EventHandler(this.resetMcuButton_Click);
            // 
            // baudRatelabel
            // 
            this.baudRatelabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.baudRatelabel.Location = new System.Drawing.Point(23, 69);
            this.baudRatelabel.Name = "baudRatelabel";
            this.baudRatelabel.Size = new System.Drawing.Size(59, 23);
            this.baudRatelabel.TabIndex = 10;
            this.baudRatelabel.Text = "波特率";
            // 
            // scanComComboBox
            // 
            this.scanComComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.scanComComboBox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scanComComboBox.FormattingEnabled = true;
            this.scanComComboBox.Location = new System.Drawing.Point(108, 15);
            this.scanComComboBox.MaxDropDownItems = 100;
            this.scanComComboBox.Name = "scanComComboBox";
            this.scanComComboBox.Size = new System.Drawing.Size(137, 27);
            this.scanComComboBox.TabIndex = 0;
            // 
            // baudRateComboBox
            // 
            this.baudRateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.baudRateComboBox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.baudRateComboBox.FormattingEnabled = true;
            this.baudRateComboBox.Location = new System.Drawing.Point(108, 65);
            this.baudRateComboBox.MaxDropDownItems = 100;
            this.baudRateComboBox.Name = "baudRateComboBox";
            this.baudRateComboBox.Size = new System.Drawing.Size(137, 27);
            this.baudRateComboBox.TabIndex = 1;
            // 
            // scanComButton
            // 
            this.scanComButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanComButton.Location = new System.Drawing.Point(5, 15);
            this.scanComButton.Name = "scanComButton";
            this.scanComButton.Size = new System.Drawing.Size(97, 29);
            this.scanComButton.TabIndex = 0;
            this.scanComButton.Text = "扫描串口";
            this.scanComButton.UseVisualStyleBackColor = true;
            this.scanComButton.Click += new System.EventHandler(this.scanComButton_Click);
            // 
            // programButton
            // 
            this.programButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.programButton.Location = new System.Drawing.Point(76, 215);
            this.programButton.Name = "programButton";
            this.programButton.Size = new System.Drawing.Size(96, 38);
            this.programButton.TabIndex = 8;
            this.programButton.Text = "下载/编程";
            this.programButton.UseVisualStyleBackColor = true;
            this.programButton.Click += new System.EventHandler(this.programButton_Click);
            // 
            // openFileTextBox
            // 
            this.openFileTextBox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openFileTextBox.Location = new System.Drawing.Point(108, 115);
            this.openFileTextBox.Name = "openFileTextBox";
            this.openFileTextBox.Size = new System.Drawing.Size(137, 26);
            this.openFileTextBox.TabIndex = 6;
            // 
            // scanMcuButton
            // 
            this.scanMcuButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanMcuButton.Location = new System.Drawing.Point(148, 165);
            this.scanMcuButton.Name = "scanMcuButton";
            this.scanMcuButton.Size = new System.Drawing.Size(97, 29);
            this.scanMcuButton.TabIndex = 5;
            this.scanMcuButton.Text = "扫描芯片";
            this.scanMcuButton.UseVisualStyleBackColor = true;
            this.scanMcuButton.Click += new System.EventHandler(this.scanMcuButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(788, 312);
            this.Controls.Add(this.infoTextBox);
            this.Controls.Add(this.panel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Location = new System.Drawing.Point(15, 15);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EFM8系列芯片串口下载工具";
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }



        #endregion
        private System.Windows.Forms.Label baudRatelabel;
        private System.Windows.Forms.Button openFileButton;
        private System.Windows.Forms.TextBox infoTextBox;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Button programButton;
        private System.Windows.Forms.TextBox openFileTextBox;
        private System.Windows.Forms.Button resetMucButton;
        private System.Windows.Forms.Button scanMcuButton;
        private System.Windows.Forms.ComboBox scanComComboBox;
        private System.Windows.Forms.ComboBox baudRateComboBox;
        private System.Windows.Forms.Button scanComButton;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}

