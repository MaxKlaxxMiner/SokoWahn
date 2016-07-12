
#region # using *.*

using System;
using System.Windows.Forms;

#endregion

namespace JSoko
{
  static class Program
  {
    /// <summary>
    /// Der Haupteinstiegspunkt für die Anwendung.
    /// </summary>
    [STAThread]
    static void Main(string[] argv)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new JSoko(argv));
    }
  }
}
