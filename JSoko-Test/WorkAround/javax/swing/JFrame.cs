
using System.Drawing;

namespace JSoko.javax.swing
{
  public class JFrame : Form1
  {
    public void SetTitle(string title)
    {
      Text = title;
    }

    public void SetIconImage(Bitmap image)
    {
      Icon = image == null ? null : Icon.FromHandle(image.GetHicon());
    }
  }
}
