namespace SokoWahnWin
{
  sealed partial class FormSolver
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
      this.components = new System.ComponentModel.Container();
      this.pictureBoxField = new System.Windows.Forms.PictureBox();
      this.textBoxInfo = new System.Windows.Forms.TextBox();
      this.timerDisplay = new System.Windows.Forms.Timer(this.components);
      this.textBoxLog = new System.Windows.Forms.TextBox();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).BeginInit();
      this.SuspendLayout();
      // 
      // pictureBoxField
      // 
      this.pictureBoxField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.pictureBoxField.Location = new System.Drawing.Point(2, 2);
      this.pictureBoxField.Name = "pictureBoxField";
      this.pictureBoxField.Size = new System.Drawing.Size(917, 600);
      this.pictureBoxField.TabIndex = 2;
      this.pictureBoxField.TabStop = false;
      // 
      // textBoxInfo
      // 
      this.textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.textBoxInfo.Font = new System.Drawing.Font("Consolas", 9.75F);
      this.textBoxInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.textBoxInfo.Location = new System.Drawing.Point(2, 607);
      this.textBoxInfo.Name = "textBoxInfo";
      this.textBoxInfo.Size = new System.Drawing.Size(917, 23);
      this.textBoxInfo.TabIndex = 6;
      this.textBoxInfo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // timerDisplay
      // 
      this.timerDisplay.Enabled = true;
      this.timerDisplay.Interval = 1;
      this.timerDisplay.Tick += new System.EventHandler(this.timerDisplay_Tick);
      // 
      // textBoxLog
      // 
      this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.textBoxLog.Font = new System.Drawing.Font("Consolas", 9.75F);
      this.textBoxLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.textBoxLog.Location = new System.Drawing.Point(925, 2);
      this.textBoxLog.Multiline = true;
      this.textBoxLog.Name = "textBoxLog";
      this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.textBoxLog.Size = new System.Drawing.Size(339, 628);
      this.textBoxLog.TabIndex = 7;
      this.textBoxLog.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // FormSolver
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Black;
      this.ClientSize = new System.Drawing.Size(1266, 632);
      this.Controls.Add(this.textBoxLog);
      this.Controls.Add(this.textBoxInfo);
      this.Controls.Add(this.pictureBoxField);
      this.Name = "FormSolver";
      this.Text = "Rooms Solver";
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      this.Resize += new System.EventHandler(this.FormSolver_Resize);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBoxField;
    private System.Windows.Forms.TextBox textBoxInfo;
    private System.Windows.Forms.Timer timerDisplay;
    private System.Windows.Forms.TextBox textBoxLog;
  }
}