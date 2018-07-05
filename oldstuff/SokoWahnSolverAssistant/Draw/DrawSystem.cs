
#region # using *.*

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SokoWahnCore;

#endregion

namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Klasse zum zeichnen eines Sokowahn-Spielfeldes
  /// </summary>
  public sealed class DrawSystem
  {
    /// <summary>
    /// Spielfeld, welches aktuell sichtbar ist
    /// </summary>
    SokowahnField drawField = new SokowahnField("#####\n#@$.#\n#####"); // minimum Dummy-Feld

    /// <summary>
    /// Picturebox, welche zum Zeichnen des Spielfeldes verwendet werden soll
    /// </summary>
    readonly PictureBox pictureBox;

    /// <summary>
    /// aktuelles Bild, welches in der PictureBox dargestellt wird
    /// </summary>
    Bitmap pictureBitmap;

    /// <summary>
    /// Bitmap, welches zum eigentlichen Zeichnen verwendet wird
    /// </summary>
    RawBitmap drawBitmap;

    /// <summary>
    /// Skin, welches zum Zeichnen verwendet wird
    /// </summary>
    DrawSkin skin;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="drawPictureBox">Picturebox, welche zum Zeichnen des Spielfeldes verwendet werden soll</param>
    /// <param name="skinFile">Skin-Datei, welche zum Zeichnen verwendet werden soll</param>
    public DrawSystem(PictureBox drawPictureBox, string skinFile)
    {
      pictureBox = drawPictureBox;

      skin = new DrawSkin(skinFile);

      drawPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
    }

    /// <summary>
    /// erzeugt ein komplett neues Spielfeld
    /// </summary>
    void Init()
    {
      // alle Felder leeren, damit das gesamte Spielfeld neu gezeichnet
      Array.Clear(drawField.fieldData, 0, drawField.fieldData.Length);
      pictureBitmap = new Bitmap(drawField.width * skin.spriteSize.w, drawField.height * skin.spriteSize.h, PixelFormat.Format32bppArgb);
      pictureBox.Image = pictureBitmap;
      drawBitmap = new RawBitmap(pictureBitmap);
    }

    /// <summary>
    /// gibt an, ob das Feld nicht zu den Wand-Teilen dazu gehört
    /// </summary>
    /// <param name="fieldChars">Spielfeld mit den entsprechenden Zeichen</param>
    /// <param name="x">X-Position auf dem Spielfeld</param>
    /// <param name="y">Y-Position auf dem Spielfeld</param>
    /// <param name="fieldWidth">Breite des Spielfeldes</param>
    /// <returns>true, wenn es keine Wand ist</returns>
    static bool CheckField(char[] fieldChars, int x, int y, int fieldWidth)
    {
      if (x < 0 || y < 0 || x >= fieldWidth || y * fieldWidth >= fieldChars.Length) return true;
      return fieldChars[x + y * fieldWidth] != '#';
    }

    /// <summary>
    /// zeichnet ein bestimmtes Spielfeld
    /// </summary>
    /// <param name="field">Spielfeld, welches gezeichnet werden soll</param>
    public void DrawField(SokowahnField field)
    {
      if (field.width != drawField.width || field.height != drawField.height)
      {
        drawField = new SokowahnField(field);
        Init();
      }

      var drawData = drawField.fieldData;
      var f = field.fieldData;
      int w = field.width;

      var minField = new PointInt(int.MaxValue, int.MaxValue);
      var maxField = new PointInt(int.MinValue, int.MinValue);

      for (int y = 0; y < field.height; y++)
      {
        for (int x = 0; x < w; x++)
        {
          char c = f[x + y * w];
          if (drawData[x + y * w] == c) continue;

          if (x < minField.x) minField.x = x;
          if (y < minField.y) minField.y = y;
          if (x > maxField.x) maxField.x = x;
          if (y > maxField.y) maxField.y = y;

          drawData[x + y * w] = c;
          skin.BlitSprite(drawBitmap, x, y, SpriteType.Empty);
          switch (c)
          {
            case ' ': break;
            case '.': skin.BlitSprite(drawBitmap, x, y, SpriteType.EmptyFinish); break;
            case '$': skin.BlitSprite(drawBitmap, x, y, SpriteType.Box); break;
            case '*': skin.BlitSprite(drawBitmap, x, y, SpriteType.BoxFinish); break;
            case '@': skin.BlitSprite(drawBitmap, x, y, SpriteType.Player); break;
            case '+': skin.BlitSprite(drawBitmap, x, y, SpriteType.EmptyFinish); skin.BlitSprite(drawBitmap, x, y, SpriteType.Player); break;
            case '#':
            {
              var lo = SpriteType.WallFill;
              var ro = SpriteType.WallFill;
              var lu = SpriteType.WallFill;
              var ru = SpriteType.WallFill;

              if (CheckField(f, x - 1, y, w) && CheckField(f, x, y - 1, w)) lo = SpriteType.WallEdge;
              if (CheckField(f, x - 1, y, w) && !CheckField(f, x, y - 1, w)) lo = SpriteType.WallVertical;
              if (!CheckField(f, x - 1, y, w) && CheckField(f, x, y - 1, w)) lo = SpriteType.WallHorizon;
              if (!CheckField(f, x - 1, y, w) && !CheckField(f, x, y - 1, w)) lo = CheckField(f, x - 1, y - 1, w) ? SpriteType.WallCorner : SpriteType.WallFill;

              if (CheckField(f, x + 1, y, w) && CheckField(f, x, y - 1, w)) ro = SpriteType.WallEdge;
              if (CheckField(f, x + 1, y, w) && !CheckField(f, x, y - 1, w)) ro = SpriteType.WallVertical;
              if (!CheckField(f, x + 1, y, w) && CheckField(f, x, y - 1, w)) ro = SpriteType.WallHorizon;
              if (!CheckField(f, x + 1, y, w) && !CheckField(f, x, y - 1, w)) ro = CheckField(f, x + 1, y - 1, w) ? SpriteType.WallCorner : SpriteType.WallFill;

              if (CheckField(f, x - 1, y, w) && CheckField(f, x, y + 1, w)) lu = SpriteType.WallEdge;
              if (CheckField(f, x - 1, y, w) && !CheckField(f, x, y + 1, w)) lu = SpriteType.WallVertical;
              if (!CheckField(f, x - 1, y, w) && CheckField(f, x, y + 1, w)) lu = SpriteType.WallHorizon;
              if (!CheckField(f, x - 1, y, w) && !CheckField(f, x, y + 1, w)) lu = CheckField(f, x - 1, y + 1, w) ? SpriteType.WallCorner : SpriteType.WallFill;

              if (CheckField(f, x + 1, y, w) && CheckField(f, x, y + 1, w)) ru = SpriteType.WallEdge;
              if (CheckField(f, x + 1, y, w) && !CheckField(f, x, y + 1, w)) ru = SpriteType.WallVertical;
              if (!CheckField(f, x + 1, y, w) && CheckField(f, x, y + 1, w)) ru = SpriteType.WallHorizon;
              if (!CheckField(f, x + 1, y, w) && !CheckField(f, x, y + 1, w)) ru = CheckField(f, x + 1, y + 1, w) ? SpriteType.WallCorner : SpriteType.WallFill;

              skin.BlitSprite(drawBitmap, x, y, lo, SpriteParts.TopLeft);
              skin.BlitSprite(drawBitmap, x, y, ro, SpriteParts.TopRight);
              skin.BlitSprite(drawBitmap, x, y, lu, SpriteParts.BottomLeft);
              skin.BlitSprite(drawBitmap, x, y, ru, SpriteParts.BottomRight);
            } break;
            default: throw new Exception("unknown Char: '" + c + "'");
          }
        }
      }

      if (minField.x != int.MaxValue)
      {
        maxField.x = maxField.x - minField.x + 1;
        maxField.y = maxField.y - minField.y + 1;
        drawBitmap.Present(pictureBitmap, minField.x * skin.spriteSize.w, minField.y * skin.spriteSize.h, maxField.x * skin.spriteSize.w, maxField.y * skin.spriteSize.h);
      }
    }
  }
}
