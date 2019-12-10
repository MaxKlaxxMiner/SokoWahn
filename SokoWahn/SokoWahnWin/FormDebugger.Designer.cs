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
      this.button1 = new System.Windows.Forms.Button();
      this.listRooms = new System.Windows.Forms.ListBox();
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
      this.listRooms.Size = new System.Drawing.Size(132, 514);
      this.listRooms.TabIndex = 1;
      this.listRooms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // FormDebugger
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Black;
      this.ClientSize = new System.Drawing.Size(1252, 516);
      this.Controls.Add(this.listRooms);
      this.Controls.Add(this.button1);
      this.Name = "FormDebugger";
      this.Text = "Form1";
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.ListBox listRooms;
  }
}

