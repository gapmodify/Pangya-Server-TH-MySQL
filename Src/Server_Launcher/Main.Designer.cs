using System.Windows.Forms;

namespace Server_Launcher
{
    partial class Main
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
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                RED?.Dispose();
                GREEN?.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Button1 = new System.Windows.Forms.Button();
            this.Button2 = new System.Windows.Forms.Button();
            this.Button3 = new System.Windows.Forms.Button();
            this.Button4 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.Button5 = new System.Windows.Forms.Button();
            this.Button6 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.Button9 = new System.Windows.Forms.Button();
            this.Button10 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.pictureBox5 = new System.Windows.Forms.PictureBox();
            this.Button11 = new System.Windows.Forms.Button();
            this.Button12 = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 439);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(914, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Location = new System.Drawing.Point(187, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(727, 421);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(719, 395);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Auth Server";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(719, 395);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "LoginServer";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(719, 395);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Game Server";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(719, 395);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Message Server";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(163, 26);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(22, 22);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Auth Server";
            // 
            // Button1
            // 
            this.Button1.Location = new System.Drawing.Point(89, 25);
            this.Button1.Name = "Button1";
            this.Button1.Size = new System.Drawing.Size(31, 23);
            this.Button1.TabIndex = 0;
            this.Button1.Text = "On";
            this.Button1.UseVisualStyleBackColor = true;
            this.Button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // Button2
            // 
            this.Button2.Location = new System.Drawing.Point(126, 25);
            this.Button2.Name = "Button2";
            this.Button2.Size = new System.Drawing.Size(31, 23);
            this.Button2.TabIndex = 15;
            this.Button2.Text = "Off";
            this.Button2.UseVisualStyleBackColor = true;
            this.Button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // Button3
            // 
            this.Button3.Location = new System.Drawing.Point(126, 54);
            this.Button3.Name = "Button3";
            this.Button3.Size = new System.Drawing.Size(31, 23);
            this.Button3.TabIndex = 19;
            this.Button3.Text = "Off";
            this.Button3.UseVisualStyleBackColor = true;
            this.Button3.Click += new System.EventHandler(this.Button3_Click);
            // 
            // Button4
            // 
            this.Button4.Location = new System.Drawing.Point(89, 54);
            this.Button4.Name = "Button4";
            this.Button4.Size = new System.Drawing.Size(31, 23);
            this.Button4.TabIndex = 16;
            this.Button4.Text = "On";
            this.Button4.UseVisualStyleBackColor = true;
            this.Button4.Click += new System.EventHandler(this.Button4_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Login Server";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(163, 55);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(22, 22);
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // Button5
            // 
            this.Button5.Location = new System.Drawing.Point(126, 129);
            this.Button5.Name = "Button5";
            this.Button5.Size = new System.Drawing.Size(31, 23);
            this.Button5.TabIndex = 23;
            this.Button5.Text = "Off";
            this.Button5.UseVisualStyleBackColor = true;
            this.Button5.Click += new System.EventHandler(this.Button5_Click);
            // 
            // Button6
            // 
            this.Button6.Location = new System.Drawing.Point(89, 129);
            this.Button6.Name = "Button6";
            this.Button6.Size = new System.Drawing.Size(31, 23);
            this.Button6.TabIndex = 20;
            this.Button6.Text = "On";
            this.Button6.UseVisualStyleBackColor = true;
            this.Button6.Click += new System.EventHandler(this.Button6_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 21;
            this.label3.Text = "Game Server";
            // 
            // pictureBox3
            // 
            this.pictureBox3.Location = new System.Drawing.Point(163, 130);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(22, 22);
            this.pictureBox3.TabIndex = 22;
            this.pictureBox3.TabStop = false;
            // 
            // Button9
            // 
            this.Button9.Location = new System.Drawing.Point(126, 291);
            this.Button9.Name = "Button9";
            this.Button9.Size = new System.Drawing.Size(31, 23);
            this.Button9.TabIndex = 31;
            this.Button9.Text = "Off";
            this.Button9.UseVisualStyleBackColor = true;
            this.Button9.Click += new System.EventHandler(this.Button9_Click);
            // 
            // Button10
            // 
            this.Button10.Location = new System.Drawing.Point(89, 291);
            this.Button10.Name = "Button10";
            this.Button10.Size = new System.Drawing.Size(31, 23);
            this.Button10.TabIndex = 28;
            this.Button10.Text = "On";
            this.Button10.UseVisualStyleBackColor = true;
            this.Button10.Click += new System.EventHandler(this.Button10_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 296);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 13);
            this.label5.TabIndex = 29;
            this.label5.Text = "Message Server";
            // 
            // pictureBox5
            // 
            this.pictureBox5.Location = new System.Drawing.Point(163, 292);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new System.Drawing.Size(22, 22);
            this.pictureBox5.TabIndex = 30;
            this.pictureBox5.TabStop = false;
            // 
            // Button11
            // 
            this.Button11.Location = new System.Drawing.Point(6, 356);
            this.Button11.Name = "Button11";
            this.Button11.Size = new System.Drawing.Size(81, 52);
            this.Button11.TabIndex = 0;
            this.Button11.Text = "Server ON";
            this.Button11.UseVisualStyleBackColor = true;
            this.Button11.Click += new System.EventHandler(this.Button11_Click);
            // 
            // Button12
            // 
            this.Button12.Location = new System.Drawing.Point(99, 356);
            this.Button12.Name = "Button12";
            this.Button12.Size = new System.Drawing.Size(82, 52);
            this.Button12.TabIndex = 1;
            this.Button12.Text = "Server OFF";
            this.Button12.UseVisualStyleBackColor = true;
            this.Button12.Click += new System.EventHandler(this.Button12_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(914, 461);
            this.Controls.Add(this.Button12);
            this.Controls.Add(this.Button11);
            this.Controls.Add(this.Button9);
            this.Controls.Add(this.Button10);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.pictureBox5);
            this.Controls.Add(this.Button5);
            this.Controls.Add(this.Button6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.pictureBox3);
            this.Controls.Add(this.Button3);
            this.Controls.Add(this.Button4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.Button2);
            this.Controls.Add(this.Button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(930, 500);
            this.MinimumSize = new System.Drawing.Size(930, 500);
            this.Name = "Main";
            this.Text = "Pangya  - Launcher Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Button1;
        private System.Windows.Forms.Button Button2;
        private System.Windows.Forms.Button Button3;
        private System.Windows.Forms.Button Button4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button Button5;
        private System.Windows.Forms.Button Button6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Button Button9;
        private System.Windows.Forms.Button Button10;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox pictureBox5;
        private System.Windows.Forms.Button Button11;
        private System.Windows.Forms.Button Button12;

    }
}

