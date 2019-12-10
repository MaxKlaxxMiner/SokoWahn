#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SokoWahnLib;
using SokoWahnLib.Rooms;

#endregion

namespace SokoWahnWin
{
  /// <summary>
  /// Klasse zum darstellen eines Spielfeldes
  /// </summary>
  public class FieldDisplay
  {
    /// <summary>
    /// merkt sich das Windows-Form, wo das Spielfeld angezeigt werden soll
    /// </summary>
    readonly PictureBox pictureBox;
    /// <summary>
    /// merkt sich das aktuell angezeigt Spielfeld
    /// </summary>
    ISokoField field;
    /// <summary>
    /// merkt sich aktuellen Hintergrund des Spielfeldes
    /// </summary>
    Bitmap background;
    /// <summary>
    /// merkt sich die sichtbare Anzeige
    /// </summary>
    Bitmap foreground;
    /// <summary>
    /// merkt sich das Graphics-Objekt zur Anzeige
    /// </summary>
    Graphics graphics;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pictureBox">Windows-Form, wo das Spielfeld angezeigt werden soll</param>
    public FieldDisplay(PictureBox pictureBox)
    {
      this.pictureBox = pictureBox;
    }

    /// <summary>
    /// merkt sich, ob gerade ein Display-Update durchgeführt wird
    /// </summary>
    bool isUpdate;
    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    /// <param name="network"></param>
    public void Update(RoomNetwork network)
    {
      if (isUpdate) return;
      isUpdate = true;

      int newWidth = Math.Max(16, pictureBox.Width);
      int newHeight = Math.Max(16, pictureBox.Height);

      bool doDraw = false;

      if (network.field != field || newWidth != background.Width || newHeight != background.Height) // neues Spielfeld oder Größenänderung? -> alles neu zeichnen
      {
        field = network.field;

        // --- Hintergrund erstellen ---
        background = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
        background.SetPixel(100, 100, Color.White);

        // --- Vordergrund erstellen ---
        foreground = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
        graphics = Graphics.FromImage(foreground);
        graphics.DrawImageUnscaled(background, 0, 0);

        pictureBox.Image = foreground;
        doDraw = true;
      }

      if (doDraw) pictureBox.Refresh();

      isUpdate = false;
    }
  }
}
