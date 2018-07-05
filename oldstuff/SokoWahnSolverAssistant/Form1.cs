
#region # using *.*

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SokoWahn;
using SokoWahnCore;
using SokoWahnSolverAssistent.Draw;

#endregion

namespace SokoWahnSolverAssistent
{
  public sealed partial class Form1 : Form
  {
    /// <summary>
    /// System zum zeichnen des Spielfeldes
    /// </summary>
    readonly DrawSystem drawSystem;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public Form1()
    {
      InitializeComponent();

      drawSystem = new DrawSystem(drawPictureBox, "../../../SokoWahnWinPhone/Assets/skin-yasc.png");
    }

    void Form1_Load(object sender, EventArgs e)
    {
      var playField = new SokowahnField(TestData.Level3);
      drawSystem.DrawField(playField);
    }

    void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void pictureBox1_Click(object sender, EventArgs e)
    {

    }

    private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {

    }

  }
}
