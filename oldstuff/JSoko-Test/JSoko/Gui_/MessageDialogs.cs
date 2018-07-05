
using System.Windows.Forms;

namespace JSoko.Gui_
{
  public class MessageDialogs
  {
    public static void ShowErrorString(JSoko application, string getText)
    {
      MessageBox.Show(getText, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      // todo
    }
  }
}
