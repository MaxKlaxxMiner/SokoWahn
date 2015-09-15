namespace Sokohack
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
   this.button1 = new System.Windows.Forms.Button();
   this.pictureBox1 = new System.Windows.Forms.PictureBox();
   this.textBox1 = new System.Windows.Forms.TextBox();
   this.textBox2 = new System.Windows.Forms.TextBox();
   this.button2 = new System.Windows.Forms.Button();
   this.tickButton = new System.Windows.Forms.Button();
   this.zurückButton = new System.Windows.Forms.Button();
   this.vorButton = new System.Windows.Forms.Button();
   this.button3 = new System.Windows.Forms.Button();
   this.button4 = new System.Windows.Forms.Button();
   this.button5 = new System.Windows.Forms.Button();
   this.optimizeButton = new System.Windows.Forms.Button();
   this.button6 = new System.Windows.Forms.Button();
   ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
   this.SuspendLayout();
   // 
   // button1
   // 
   this.button1.Location = new System.Drawing.Point(723, 12);
   this.button1.Name = "button1";
   this.button1.Size = new System.Drawing.Size(169, 54);
   this.button1.TabIndex = 0;
   this.button1.Text = "Scan";
   this.button1.UseVisualStyleBackColor = true;
   this.button1.Click += new System.EventHandler(this.button1_Click);
   // 
   // pictureBox1
   // 
   this.pictureBox1.Location = new System.Drawing.Point(13, 13);
   this.pictureBox1.Name = "pictureBox1";
   this.pictureBox1.Size = new System.Drawing.Size(704, 638);
   this.pictureBox1.TabIndex = 1;
   this.pictureBox1.TabStop = false;
   // 
   // textBox1
   // 
   this.textBox1.Location = new System.Drawing.Point(723, 73);
   this.textBox1.Name = "textBox1";
   this.textBox1.Size = new System.Drawing.Size(169, 20);
   this.textBox1.TabIndex = 2;
   // 
   // textBox2
   // 
   this.textBox2.Location = new System.Drawing.Point(723, 99);
   this.textBox2.Name = "textBox2";
   this.textBox2.Size = new System.Drawing.Size(51, 20);
   this.textBox2.TabIndex = 3;
   this.textBox2.Text = "0";
   // 
   // button2
   // 
   this.button2.Enabled = false;
   this.button2.Location = new System.Drawing.Point(780, 99);
   this.button2.Name = "button2";
   this.button2.Size = new System.Drawing.Size(112, 40);
   this.button2.TabIndex = 4;
   this.button2.Text = "dazu";
   this.button2.UseVisualStyleBackColor = true;
   this.button2.Click += new System.EventHandler(this.button2_Click);
   // 
   // tickButton
   // 
   this.tickButton.Location = new System.Drawing.Point(723, 178);
   this.tickButton.Name = "tickButton";
   this.tickButton.Size = new System.Drawing.Size(206, 40);
   this.tickButton.TabIndex = 5;
   this.tickButton.Text = "Tick";
   this.tickButton.UseVisualStyleBackColor = true;
   this.tickButton.Click += new System.EventHandler(this.button3_Click);
   // 
   // zurückButton
   // 
   this.zurückButton.Enabled = false;
   this.zurückButton.Location = new System.Drawing.Point(723, 224);
   this.zurückButton.Name = "zurückButton";
   this.zurückButton.Size = new System.Drawing.Size(99, 40);
   this.zurückButton.TabIndex = 6;
   this.zurückButton.Text = "zurück";
   this.zurückButton.UseVisualStyleBackColor = true;
   this.zurückButton.Click += new System.EventHandler(this.zurückButton_Click);
   // 
   // vorButton
   // 
   this.vorButton.Enabled = false;
   this.vorButton.Location = new System.Drawing.Point(828, 224);
   this.vorButton.Name = "vorButton";
   this.vorButton.Size = new System.Drawing.Size(101, 40);
   this.vorButton.TabIndex = 7;
   this.vorButton.Text = "vor";
   this.vorButton.UseVisualStyleBackColor = true;
   this.vorButton.Click += new System.EventHandler(this.vorButton_Click);
   // 
   // button3
   // 
   this.button3.Location = new System.Drawing.Point(935, 178);
   this.button3.Name = "button3";
   this.button3.Size = new System.Drawing.Size(47, 40);
   this.button3.TabIndex = 8;
   this.button3.Text = "x10";
   this.button3.UseVisualStyleBackColor = true;
   this.button3.Click += new System.EventHandler(this.button3_Click_1);
   // 
   // button4
   // 
   this.button4.Location = new System.Drawing.Point(988, 178);
   this.button4.Name = "button4";
   this.button4.Size = new System.Drawing.Size(47, 40);
   this.button4.TabIndex = 9;
   this.button4.Text = "x100";
   this.button4.UseVisualStyleBackColor = true;
   this.button4.Click += new System.EventHandler(this.button4_Click);
   // 
   // button5
   // 
   this.button5.Location = new System.Drawing.Point(1041, 178);
   this.button5.Name = "button5";
   this.button5.Size = new System.Drawing.Size(47, 40);
   this.button5.TabIndex = 10;
   this.button5.Text = "x1000";
   this.button5.UseVisualStyleBackColor = true;
   this.button5.Click += new System.EventHandler(this.button5_Click);
   // 
   // optimizeButton
   // 
   this.optimizeButton.Location = new System.Drawing.Point(935, 224);
   this.optimizeButton.Name = "optimizeButton";
   this.optimizeButton.Size = new System.Drawing.Size(210, 40);
   this.optimizeButton.TabIndex = 11;
   this.optimizeButton.Text = "Hash-Optimize";
   this.optimizeButton.UseVisualStyleBackColor = true;
   this.optimizeButton.Click += new System.EventHandler(this.button6_Click);
   // 
   // button6
   // 
   this.button6.Location = new System.Drawing.Point(1094, 178);
   this.button6.Name = "button6";
   this.button6.Size = new System.Drawing.Size(51, 40);
   this.button6.TabIndex = 12;
   this.button6.Text = "x10000";
   this.button6.UseVisualStyleBackColor = true;
   this.button6.Click += new System.EventHandler(this.button6_Click_1);
   // 
   // Form1
   // 
   this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
   this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
   this.ClientSize = new System.Drawing.Size(1157, 663);
   this.Controls.Add(this.button6);
   this.Controls.Add(this.optimizeButton);
   this.Controls.Add(this.button5);
   this.Controls.Add(this.button4);
   this.Controls.Add(this.button3);
   this.Controls.Add(this.vorButton);
   this.Controls.Add(this.zurückButton);
   this.Controls.Add(this.tickButton);
   this.Controls.Add(this.button2);
   this.Controls.Add(this.textBox2);
   this.Controls.Add(this.textBox1);
   this.Controls.Add(this.pictureBox1);
   this.Controls.Add(this.button1);
   this.Name = "Form1";
   this.Text = "Form1";
   this.Load += new System.EventHandler(this.Form1_Load);
   ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
   this.ResumeLayout(false);
   this.PerformLayout();

  }

  #endregion

  private System.Windows.Forms.Button button1;
  private System.Windows.Forms.PictureBox pictureBox1;
  private System.Windows.Forms.TextBox textBox1;
  private System.Windows.Forms.TextBox textBox2;
  private System.Windows.Forms.Button button2;
  private System.Windows.Forms.Button tickButton;
  private System.Windows.Forms.Button zurückButton;
  private System.Windows.Forms.Button vorButton;
  private System.Windows.Forms.Button button3;
  private System.Windows.Forms.Button button4;
  private System.Windows.Forms.Button button5;
  private System.Windows.Forms.Button optimizeButton;
  private System.Windows.Forms.Button button6;
 }
}

