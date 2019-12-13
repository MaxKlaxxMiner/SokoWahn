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
    /// merkt sich das aktuell angezeigte Räume-Netzwerk
    /// </summary>
    RoomNetwork network;
    /// <summary>
    /// merkt sich die aktuelle Anzahl der Räume
    /// </summary>
    int networkRooms;
    /// <summary>
    /// aktuell gezeichnete Einstellungen
    /// </summary>
    DisplaySettings settings;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pictureBox">Windows-Form, wo das Spielfeld angezeigt werden soll</param>
    public FieldDisplay(PictureBox pictureBox)
    {
      this.pictureBox = pictureBox;
    }

    /// <summary>
    /// erstellt einen neuen Pinsel
    /// </summary>
    /// <param name="color">Farbe, welche verwendet werden soll (z.B. 0xff0000 = Rot)</param>
    /// <param name="size">Größe des Pinsels (Default: 0.03f)</param>
    /// <returns>neu ersteltter Pinsel</returns>
    static Pen GetPen(int color, float size = 0.03f)
    {
      return new Pen(Color.FromArgb(color - 16777216), size);
    }

    #region # void DrawBackground() // zeichnet den gesamten Hintergrund
    /// <summary>
    /// zeichnet den gesamten Hintergrund
    /// </summary>
    void DrawBackground()
    {
      var field = network.field;
      using (var g = Graphics.FromImage(background))
      using (var wall = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(0x666666 - 16777216)))
      using (var wallBorderL = GetPen(0x888888))
      using (var wallBorderD = GetPen(0x444444))
      using (var way = new HatchBrush(HatchStyle.ZigZag, Color.FromArgb(0x442200 - 16777216)))
      {
        g.CompositingQuality = graphics.CompositingQuality;
        g.InterpolationMode = graphics.InterpolationMode;
        g.Transform = graphics.Transform;

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

        g.SmoothingMode = graphics.SmoothingMode;
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
    #endregion

    /// <summary>
    /// zeichnet den Vorderund neu (z.B. Spieler und Kisten)
    /// </summary>
    void DrawForeground()
    {
      UnsafeHelper.CopyBitmap(background, foreground);
      var field = network.field;
      var g = graphics;
      g.CompositingQuality = CompositingQuality.HighQuality;
      g.SmoothingMode = SmoothingMode.HighQuality;
      using (var player = GetPen(0x33ff33))
      using (var playerF = new SolidBrush(Color.FromArgb(0x115511 - 16777216)))
      {
        if (settings.playerPos >= 0)
        {
          int playerX = settings.playerPos % field.Width;
          int playerY = settings.playerPos / field.Width;

          g.FillEllipse(playerF, playerX + 0.1f, playerY + 0.1f, 0.8f, 0.8f);
          g.DrawEllipse(player, playerX + 0.1f, playerY + 0.1f, 0.8f, 0.8f);
        }
      }

      pictureBox.Refresh();
    }

    /// <summary>
    /// merkt sich, ob gerade ein Display-Update durchgeführt wird
    /// </summary>
    bool isUpdate;

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    /// <param name="network">Raum-Netzwerk (Spielfeld), welches gezeichnet werden soll</param>
    /// <param name="settings">optionale Einstellungen, welche Dinge angezeigt werden sollen (Default: null = normales Spielfeld)</param>
    public void Update(RoomNetwork network, DisplaySettings settings = null)
    {
      if (network == null) throw new ArgumentNullException("network");

      if (isUpdate) return;
      isUpdate = true;

      if (settings == null) settings = this.settings ?? new DisplaySettings(network.field);

      int newWidth = Math.Max(16, pictureBox.Width);
      int newHeight = Math.Max(16, pictureBox.Height);

      bool doDraw = false;

      // --- Hintergrund neu berechnen + zeichnen (sofern notwendig) ---
      if (network != this.network || network.rooms.Length != networkRooms // neues oder geändertes Spielfeld?
       || newWidth != background.Width || newHeight != background.Height) // oder Größenänderung? -> alles neu zeichnen
      {
        this.network = network;
        networkRooms = network.rooms.Length;

        // --- neuen Vordergrund erstellen und Graphics-Objekt neu einstellen  ---
        if (foreground != null) foreground.Dispose();
        foreground = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
        pictureBox.Image = foreground;

        // --- neues Graphics-Objekt erstellen ---
        if (graphics != null) graphics.Dispose();
        graphics = Graphics.FromImage(foreground);
        var field = network.field;
        float scale = field.Width / (float)newWidth > field.Height / (float)newHeight ? newWidth / (float)field.Width : newHeight / (float)field.Height;
        scale *= 0.98f; // Am Rand etwas Abstand lassen
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TranslateTransform(newWidth * 0.5f, newHeight * 0.5f);
        graphics.ScaleTransform(scale, scale);
        graphics.TranslateTransform(field.Width * -0.5f, field.Height * -0.5f);

        // --- neuen Hintergrund erstellen und zeichnen ---
        if (background != null) background.Dispose();
        background = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
        DrawBackground();

        doDraw = true;
      }

      if (this.settings == null || settings.ToString() != this.settings.ToString())
      {
        this.settings = settings.Clone();
        doDraw = true;
      }

      // --- Bild neu zeichnen und aktualisieren (sofern notwendig) ---
      if (doDraw)
      {
        DrawForeground();
      }

      isUpdate = false;
    }
  }
}
