
#region # using *.*

using System;

#endregion

namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Struktur eines Skins
  /// </summary>
  public struct DrawSkin
  {
    /// <summary>
    /// merkt sich die Größe eines Bildelementes (Höhe und Breite) in Pixeln
    /// </summary>
    public readonly SizeInt spriteSize;

    /// <summary>
    /// merkt sich Bitmap mit den Sprites
    /// </summary>
    readonly RawBitmap spriteBitmap;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="skinFile">Skin-Datei welche geladen werden soll</param>
    public DrawSkin(string skinFile)
    {
      spriteBitmap = new RawBitmap(skinFile);

      int spriteWidth = spriteBitmap.Width / 7;
      int spriteHeight = spriteBitmap.Height / 8;
      if (spriteWidth != spriteHeight) throw new Exception("Skin-Format Error");

      spriteSize = new SizeInt(spriteWidth, spriteHeight);
    }

    /// <summary>
    /// zeichnet ein Sprite (mit Alpha-Kanal) in ein Bitmap
    /// </summary>
    /// <param name="target">Bild, wohin das Sprite gezeichnet werden soll</param>
    /// <param name="fieldX">Feld X-Position</param>
    /// <param name="fieldY">Feld Y-Position</param>
    /// <param name="type">Typ des zu zeichnenden Sprites</param>
    /// <param name="parts">Teile des Sprites</param>
    public void BlitSprite(RawBitmap target, int fieldX, int fieldY, SpriteType type, SpriteParts parts = SpriteParts.All)
    {
      var targetRect = new RectInt(fieldX * spriteSize.w, fieldY * spriteSize.h, spriteSize.w, spriteSize.h);
      int elY = (int)type / 7;
      int elX = (int)type - (elY * 7);
      var sourceRect = new RectInt(elX * spriteSize.w, elY * spriteSize.h, spriteSize.w, spriteSize.h);

      switch (parts)
      {
        case SpriteParts.All: break;
        case SpriteParts.Left: targetRect.w /= 2; sourceRect.w /= 2; break;
        case SpriteParts.Right: targetRect.w /= 2; sourceRect.w /= 2; targetRect.x += spriteSize.w / 2; sourceRect.x += spriteSize.w / 2; break;
        case SpriteParts.Top: targetRect.h /= 2; sourceRect.h /= 2; break;
        case SpriteParts.Bottom: targetRect.h /= 2; sourceRect.h /= 2; targetRect.y += spriteSize.h / 2; sourceRect.y += spriteSize.h / 2; break;
        case SpriteParts.TopLeft: targetRect.w /= 2; targetRect.h /= 2; sourceRect.w /= 2; sourceRect.h /= 2; break;
        case SpriteParts.TopRight: targetRect.w /= 2; targetRect.h /= 2; sourceRect.w /= 2; sourceRect.h /= 2; targetRect.x += spriteSize.w / 2; sourceRect.x += spriteSize.w / 2; break;
        case SpriteParts.BottomLeft: targetRect.w /= 2; targetRect.h /= 2; sourceRect.w /= 2; sourceRect.h /= 2; targetRect.y += spriteSize.h / 2; sourceRect.y += spriteSize.h / 2; break;
        case SpriteParts.BottomRight: targetRect.w /= 2; targetRect.h /= 2; sourceRect.w /= 2; sourceRect.h /= 2; targetRect.x += spriteSize.w / 2; sourceRect.x += spriteSize.w / 2; targetRect.y += spriteSize.h / 2; sourceRect.y += spriteSize.h / 2; break;
        default: throw new Exception("unknown parts: " + (int)parts);
      }

      if (type == SpriteType.Empty)
      {
        target.FillRectangle(targetRect.x, targetRect.y, targetRect.w, targetRect.h, 0x001122);
      }

      target.BlitAlpha(targetRect.x, targetRect.y, spriteBitmap, sourceRect);
    }
  }
}
