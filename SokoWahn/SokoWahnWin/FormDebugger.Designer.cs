namespace SokoWahnWin
{
  partial class FormDebugger
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
      this.button1 = new System.Windows.Forms.Button();
      this.listRooms = new System.Windows.Forms.ListBox();
      this.pictureBoxField = new System.Windows.Forms.PictureBox();
      this.timerDisplay = new System.Windows.Forms.Timer(this.components);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).BeginInit();
      this.SuspendLayout();
      // 
      // button1
      // 
      this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.button1.Location = new System.Drawing.Point(1054, 12);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(186, 36);
      this.button1.TabIndex = 0;
      this.button1.Text = "Step";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // listRooms
      // 
      this.listRooms.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
      this.listRooms.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.listRooms.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.listRooms.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.listRooms.FormattingEnabled = true;
      this.listRooms.ItemHeight = 15;
      this.listRooms.Location = new System.Drawing.Point(1, 1);
      this.listRooms.Name = "listRooms";
      this.listRooms.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
      this.listRooms.Size = new System.Drawing.Size(132, 724);
      this.listRooms.TabIndex = 1;
      this.listRooms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // pictureBoxField
      // 
      this.pictureBoxField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.pictureBoxField.Location = new System.Drawing.Point(139, 7);
      this.pictureBoxField.Name = "pictureBoxField";
      this.pictureBoxField.Size = new System.Drawing.Size(909, 720);
      this.pictureBoxField.TabIndex = 2;
      this.pictureBoxField.TabStop = false;
      // 
      // timerDisplay
      // 
      this.timerDisplay.Enabled = true;
      this.timerDisplay.Interval = 1;
      this.timerDisplay.Tick += new System.EventHandler(this.timerDisplay_Tick);
      // 
      // FormDebugger
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Black;
      this.ClientSize = new System.Drawing.Size(1252, 735);
      this.Controls.Add(this.pictureBoxField);
      this.Controls.Add(this.listRooms);
      this.Controls.Add(this.button1);
      this.Name = "FormDebugger";
      this.Text = "Form1";
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.ListBox listRooms;
    private System.Windows.Forms.PictureBox pictureBoxField;
    private System.Windows.Forms.Timer timerDisplay;
  }
}

