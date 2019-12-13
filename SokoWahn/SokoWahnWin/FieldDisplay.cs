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
    /// zeichnet den gesamten Hintergrund
    /// </summary>
    void DrawBackground()
    {
      float scale = field.Width / (double)background.Width > field.Height / (double)background.Height ? (float)background.Width / field.Width : (float)background.Height / field.Height;
      using (var g = Graphics.FromImage(background))
      using (var wall = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(0x666666 - 16777216)))
      using (var wallBorderL = new Pen(Color.FromArgb(0x888888 - 16777216), 0.8f / scale))
      using (var wallBorderD = new Pen(Color.FromArgb(0x444444 - 16777216), 0.8f / scale))
      using (var way = new HatchBrush(HatchStyle.ZigZag, Color.FromArgb(0x442200 - 16777216)))
      {
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TranslateTransform(background.Width * 0.5f, background.Height * 0.5f);
        scale *= 0.98f;
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(field.Width * -0.5f, field.Height * -0.5f);
        var walkPosis = field.GetWalkPosis();
        for (int y = 0; y < field.Height; y++)
        {
          for (int x = 0; x < field.Width; x++)
          {
            int pos = x + y * field.Width;
            if (walkPosis.Contains(pos))
            {
              g.FillRectangle(way, x, y, 1, 1);
            }
          }
        }
        for (int y = 0; y < field.Height; y++)
        {
          for (int x = 0; x < field.Width; x++)
          {
            int pos = x + y * field.Width;
            if (!walkPosis.Contains(pos))
            {
              if (field.IsWall(pos))
              {
                g.FillRectangle(wall, x, y, 1, 1);
                if (x < 1 || !field.IsWall(pos - 1)) g.DrawLine(wallBorderL, x, y, x, y + 1);
                if (y < 1 || !field.IsWall(pos - field.Width)) g.DrawLine(wallBorderL, x, y, x + 1, y);
                if (x >= field.Width - 1 || !field.IsWall(pos + 1)) g.DrawLine(wallBorderD, x + 1, y, x + 1, y + 1);
                if (y >= field.Height - 1 || !field.IsWall(pos + field.Width)) g.DrawLine(wallBorderD, x, y + 1, x + 1, y + 1);
              }
            }
          }
        }
      }
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
        DrawBackground();

        // --- Vordergrund erstellen ---
        foreground = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
        pictureBox.Image = foreground;
        graphics = Graphics.FromImage(foreground);

        doDraw = true;
      }

      if (doDraw)
      {
        graphics.DrawImageUnscaled(background, 0, 0);
        pictureBox.Refresh();
      }

      isUpdate = false;
    }
  }
}
