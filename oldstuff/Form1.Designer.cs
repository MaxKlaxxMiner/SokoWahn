namespace Sokosolver
{
 partial class Form1
 {
  /// <summary>
  /// Erforderliche Designervariable.
  /// </summary>
  private System.ComponentModel.IContainer components = null;

  /// <summary>
  /// Verwendete Ressourcen bereinigen.
  /// </summary>
  /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
  protected override void Dispose(bool disposing)
  {
   if (disposing && (components != null))
   {
    components.Dispose();
   }
   base.Dispose(disposing);
  }

  #region Vom Windows Form-Designer generierter Code

  /// <summary>
  /// Erforderliche Methode für die Designerunterstützung.
  /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
  /// </summary>
  private void InitializeComponent()
  {
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
      this.button1 = new System.Windows.Forms.Button();
      this.textBox4 = new System.Windows.Forms.TextBox();
      this.button2 = new System.Windows.Forms.Button();
      this.textBox5 = new System.Windows.Forms.TextBox();
      this.button3 = new System.Windows.Forms.Button();
      this.comboBox1 = new System.Windows.Forms.ComboBox();
      this.timer1 = new System.Windows.Forms.Timer(this.components);
      this.button4 = new System.Windows.Forms.Button();
      this.comboBox2 = new System.Windows.Forms.ComboBox();
      this.SuspendLayout();
      // 
      // button1
      // 
      this.button1.Location = new System.Drawing.Point(12, 14);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(229, 62);
      this.button1.TabIndex = 0;
      this.button1.Text = "Scan";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.Click += new System.EventHandler(this.button1_Click);
      // 
      // textBox4
      // 
      this.textBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBox4.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.textBox4.Location = new System.Drawing.Point(247, 80);
      this.textBox4.Multiline = true;
      this.textBox4.Name = "textBox4";
      this.textBox4.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.textBox4.Size = new System.Drawing.Size(791, 504);
      this.textBox4.TabIndex = 5;
      this.textBox4.Text = resources.GetString("textBox4.Text");
      this.textBox4.WordWrap = false;
      this.textBox4.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox4_KeyDown);
      // 
      // button2
      // 
      this.button2.Location = new System.Drawing.Point(247, 14);
      this.button2.Name = "button2";
      this.button2.Size = new System.Drawing.Size(204, 62);
      this.button2.TabIndex = 6;
      this.button2.Text = "Next";
      this.button2.UseVisualStyleBackColor = true;
      this.button2.Click += new System.EventHandler(this.button2_Click);
      // 
      // textBox5
      // 
      this.textBox5.Location = new System.Drawing.Point(457, 14);
      this.textBox5.Name = "textBox5";
      this.textBox5.Size = new System.Drawing.Size(65, 20);
      this.textBox5.TabIndex = 7;
      this.textBox5.Text = "-256";
      // 
      // button3
      // 
      this.button3.Location = new System.Drawing.Point(457, 42);
      this.button3.Name = "button3";
      this.button3.Size = new System.Drawing.Size(125, 32);
      this.button3.TabIndex = 8;
      this.button3.Text = "Refresh";
      this.button3.UseVisualStyleBackColor = true;
      this.button3.Click += new System.EventHandler(this.button3_Click);
      // 
      // comboBox1
      // 
      this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox1.FormattingEnabled = true;
      this.comboBox1.Location = new System.Drawing.Point(13, 83);
      this.comboBox1.Name = "comboBox1";
      this.comboBox1.Size = new System.Drawing.Size(228, 21);
      this.comboBox1.TabIndex = 9;
      // 
      // timer1
      // 
      this.timer1.Interval = 10;
      this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
      // 
      // button4
      // 
      this.button4.Location = new System.Drawing.Point(12, 110);
      this.button4.Name = "button4";
      this.button4.Size = new System.Drawing.Size(229, 62);
      this.button4.TabIndex = 10;
      this.button4.Text = "Auto";
      this.button4.UseVisualStyleBackColor = true;
      this.button4.Click += new System.EventHandler(this.button4_Click);
      // 
      // comboBox2
      // 
      this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox2.FormattingEnabled = true;
      this.comboBox2.Items.AddRange(new object[] {
            "Stop bei 0,1 GB",
            "Stop bei 0,2 GB",
            "Stop bei 0,3 GB",
            "Stop bei 0,5 GB",
            "Stop bei 1,0 GB",
            "Stop bei 1,5 GB",
            "Stop bei 2,0 GB",
            "Stop bei 2,5 GB",
            "Stop bei 3,0 GB",
            "Stop bei 3,5 GB",
            "Stop bei 4,0 GB",
            "Stop bei 5,0 GB",
            "Stop bei 6,0 GB",
            "Stop bei 7,0 GB",
            "Stop bei 8,0 GB",
            "Stop bei 10,0 GB",
            "Stop bei 12,0 GB",
            "Stop bei 14,0 GB",
            "Stop bei 16,0 GB",
            "Stop bei 18,0 GB",
            "Stop bei 20,0 GB",
            "Stop bei 22,0 GB",
            "Stop bei 24,0 GB",
            "Stop bei 26,0 GB",
            "Stop bei 28,0 GB",
            "Stop bei 30,0 GB",
            "Stop bei 32,0 GB"});
      this.comboBox2.Location = new System.Drawing.Point(13, 178);
      this.comboBox2.Name = "comboBox2";
      this.comboBox2.Size = new System.Drawing.Size(228, 21);
      this.comboBox2.TabIndex = 11;
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1050, 596);
      this.Controls.Add(this.comboBox2);
      this.Controls.Add(this.button4);
      this.Controls.Add(this.comboBox1);
      this.Controls.Add(this.button3);
      this.Controls.Add(this.textBox5);
      this.Controls.Add(this.button2);
      this.Controls.Add(this.textBox4);
      this.Controls.Add(this.button1);
      this.Name = "Form1";
      this.Text = "Form1";
      this.Load += new System.EventHandler(this.Form1_Load);
      this.ResumeLayout(false);
      this.PerformLayout();

  }

  #endregion

  private System.Windows.Forms.Button button1;
  private System.Windows.Forms.TextBox textBox4;
  private System.Windows.Forms.Button button2;
  private System.Windows.Forms.TextBox textBox5;
  private System.Windows.Forms.Button button3;
  private System.Windows.Forms.ComboBox comboBox1;
  private System.Windows.Forms.Timer timer1;
  private System.Windows.Forms.Button button4;
  private System.Windows.Forms.ComboBox comboBox2;
 }
}

