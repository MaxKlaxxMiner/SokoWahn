namespace SokoWahnWin
{
  sealed partial class FormDebugger
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
      this.buttonStep = new System.Windows.Forms.Button();
      this.listRooms = new System.Windows.Forms.ListBox();
      this.pictureBoxField = new System.Windows.Forms.PictureBox();
      this.timerDisplay = new System.Windows.Forms.Timer(this.components);
      this.listStates = new System.Windows.Forms.ListBox();
      this.splitContainer1 = new System.Windows.Forms.SplitContainer();
      this.listVariants = new System.Windows.Forms.ListBox();
      this.textBoxInfo = new System.Windows.Forms.TextBox();
      this.buttonValidate = new System.Windows.Forms.Button();
      this.buttonSolver = new System.Windows.Forms.Button();
      this.buttonMerge = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
      this.splitContainer1.Panel1.SuspendLayout();
      this.splitContainer1.Panel2.SuspendLayout();
      this.splitContainer1.SuspendLayout();
      this.SuspendLayout();
      // 
      // buttonStep
      // 
      this.buttonStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonStep.Location = new System.Drawing.Point(1162, 54);
      this.buttonStep.Name = "buttonStep";
      this.buttonStep.Size = new System.Drawing.Size(92, 36);
      this.buttonStep.TabIndex = 0;
      this.buttonStep.Text = "Step";
      this.buttonStep.UseVisualStyleBackColor = true;
      this.buttonStep.Click += new System.EventHandler(this.buttonStep_Click);
      this.buttonStep.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // listRooms
      // 
      this.listRooms.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.listRooms.Dock = System.Windows.Forms.DockStyle.Left;
      this.listRooms.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.listRooms.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.listRooms.FormattingEnabled = true;
      this.listRooms.ItemHeight = 15;
      this.listRooms.Location = new System.Drawing.Point(0, 0);
      this.listRooms.Name = "listRooms";
      this.listRooms.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
      this.listRooms.Size = new System.Drawing.Size(132, 632);
      this.listRooms.TabIndex = 1;
      this.listRooms.SelectedIndexChanged += new System.EventHandler(this.listRooms_SelectedIndexChanged);
      this.listRooms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // pictureBoxField
      // 
      this.pictureBoxField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.pictureBoxField.Location = new System.Drawing.Point(411, 2);
      this.pictureBoxField.Name = "pictureBoxField";
      this.pictureBoxField.Size = new System.Drawing.Size(742, 600);
      this.pictureBoxField.TabIndex = 2;
      this.pictureBoxField.TabStop = false;
      this.pictureBoxField.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxField_MouseDown);
      this.pictureBoxField.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxField_MouseMove);
      this.pictureBoxField.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxField_MouseUp);
      // 
      // timerDisplay
      // 
      this.timerDisplay.Enabled = true;
      this.timerDisplay.Interval = 1;
      this.timerDisplay.Tick += new System.EventHandler(this.timerDisplay_Tick);
      // 
      // listStates
      // 
      this.listStates.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.listStates.Dock = System.Windows.Forms.DockStyle.Fill;
      this.listStates.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.listStates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.listStates.FormattingEnabled = true;
      this.listStates.ItemHeight = 15;
      this.listStates.Location = new System.Drawing.Point(0, 0);
      this.listStates.Name = "listStates";
      this.listStates.Size = new System.Drawing.Size(270, 302);
      this.listStates.TabIndex = 3;
      this.listStates.SelectedIndexChanged += new System.EventHandler(this.listStates_SelectedIndexChanged);
      this.listStates.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // splitContainer1
      // 
      this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
      this.splitContainer1.Location = new System.Drawing.Point(135, 1);
      this.splitContainer1.Name = "splitContainer1";
      this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainer1.Panel1
      // 
      this.splitContainer1.Panel1.Controls.Add(this.listStates);
      // 
      // splitContainer1.Panel2
      // 
      this.splitContainer1.Panel2.Controls.Add(this.listVariants);
      this.splitContainer1.Size = new System.Drawing.Size(270, 628);
      this.splitContainer1.SplitterDistance = 302;
      this.splitContainer1.TabIndex = 4;
      this.splitContainer1.Resize += new System.EventHandler(this.splitContainer1_Resize);
      // 
      // listVariants
      // 
      this.listVariants.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.listVariants.Dock = System.Windows.Forms.DockStyle.Fill;
      this.listVariants.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.listVariants.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.listVariants.FormattingEnabled = true;
      this.listVariants.ItemHeight = 15;
      this.listVariants.Location = new System.Drawing.Point(0, 0);
      this.listVariants.Name = "listVariants";
      this.listVariants.Size = new System.Drawing.Size(270, 322);
      this.listVariants.TabIndex = 4;
      this.listVariants.SelectedIndexChanged += new System.EventHandler(this.listVariants_SelectedIndexChanged);
      this.listVariants.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // textBoxInfo
      // 
      this.textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
      this.textBoxInfo.Font = new System.Drawing.Font("Consolas", 9.75F);
      this.textBoxInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(86)))), ((int)(((byte)(156)))), ((int)(((byte)(214)))));
      this.textBoxInfo.Location = new System.Drawing.Point(411, 603);
      this.textBoxInfo.Name = "textBoxInfo";
      this.textBoxInfo.Size = new System.Drawing.Size(843, 23);
      this.textBoxInfo.TabIndex = 5;
      this.textBoxInfo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // buttonValidate
      // 
      this.buttonValidate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonValidate.Location = new System.Drawing.Point(1162, 96);
      this.buttonValidate.Name = "buttonValidate";
      this.buttonValidate.Size = new System.Drawing.Size(92, 36);
      this.buttonValidate.TabIndex = 6;
      this.buttonValidate.Text = "Validate";
      this.buttonValidate.UseVisualStyleBackColor = true;
      this.buttonValidate.Click += new System.EventHandler(this.buttonValidate_Click);
      this.buttonValidate.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // buttonSolver
      // 
      this.buttonSolver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonSolver.Location = new System.Drawing.Point(1162, 138);
      this.buttonSolver.Name = "buttonSolver";
      this.buttonSolver.Size = new System.Drawing.Size(92, 36);
      this.buttonSolver.TabIndex = 7;
      this.buttonSolver.Text = "Solver...";
      this.buttonSolver.UseVisualStyleBackColor = true;
      this.buttonSolver.Click += new System.EventHandler(this.buttonSolver_Click);
      this.buttonSolver.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // buttonMerge
      // 
      this.buttonMerge.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonMerge.Location = new System.Drawing.Point(1162, 12);
      this.buttonMerge.Name = "buttonMerge";
      this.buttonMerge.Size = new System.Drawing.Size(92, 36);
      this.buttonMerge.TabIndex = 8;
      this.buttonMerge.Text = "Merge Rooms";
      this.buttonMerge.UseVisualStyleBackColor = true;
      this.buttonMerge.Click += new System.EventHandler(this.buttonMerge_Click);
      this.buttonMerge.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      // 
      // FormDebugger
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Black;
      this.ClientSize = new System.Drawing.Size(1266, 632);
      this.Controls.Add(this.buttonMerge);
      this.Controls.Add(this.buttonSolver);
      this.Controls.Add(this.buttonValidate);
      this.Controls.Add(this.textBoxInfo);
      this.Controls.Add(this.splitContainer1);
      this.Controls.Add(this.pictureBoxField);
      this.Controls.Add(this.listRooms);
      this.Controls.Add(this.buttonStep);
      this.Name = "FormDebugger";
      this.Text = "Rooms Debugger";
      this.Load += new System.EventHandler(this.FormDebugger_Load);
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
      this.Resize += new System.EventHandler(this.FormDebugger_Resize);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBoxField)).EndInit();
      this.splitContainer1.Panel1.ResumeLayout(false);
      this.splitContainer1.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
      this.splitContainer1.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button buttonStep;
    private System.Windows.Forms.ListBox listRooms;
    private System.Windows.Forms.PictureBox pictureBoxField;
    private System.Windows.Forms.Timer timerDisplay;
    private System.Windows.Forms.ListBox listStates;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.ListBox listVariants;
    private System.Windows.Forms.TextBox textBoxInfo;
    private System.Windows.Forms.Button buttonValidate;
    private System.Windows.Forms.Button buttonSolver;
    private System.Windows.Forms.Button buttonMerge;
  }
}

