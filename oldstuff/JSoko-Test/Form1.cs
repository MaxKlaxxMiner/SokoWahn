
#region # using *.*

using System;
using System.Windows.Forms;

#endregion

namespace JSoko
{
  public partial class Form1 : Form
  {
    protected Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      var jsoko = this as JSoko;
      if (jsoko != null) jsoko.StartProgram();
    }
  }
}
