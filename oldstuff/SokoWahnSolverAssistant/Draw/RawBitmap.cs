
#region # using *.*

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Rohes Bitmap-Bild mit Basis-Anforderungen
  /// </summary>
  public unsafe struct RawBitmap
  {
    /// <summary>
    /// merkt sich die Höhe und Breite des Bildes in Pixeln
    /// </summary>
    public readonly SizeInt size;

    /// <summary>
    /// merkt sich die eigentlichen Pixel-Daten des Bildes
    /// </summary>
    public readonly uint[] data;

    /// <summary>
    /// gibt die Breite des Bildes in Pixeln zurück
    /// </summary>
    public int Width { get { return size.w; } }

    /// <summary>
    /// gibt die Höhe des Bildes in Pixeln zurück
    /// </summary>
    public int Height { get { return size.h; } }

    /// <summary>
    /// Konstruktor zum laden des Bildes
    /// </summary>
    /// <param name="bitmapFile">Datei, welche gelesen werden soll</param>
    public RawBitmap(string bitmapFile)
    {
      if (!File.Exists(bitmapFile)) throw new FileNotFoundException(bitmapFile);

      using (var tmpBitmap = new Bitmap(bitmapFile))
      {
        ReadBitmap(tmpBitmap, out size, out data);
      }
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="bitmap">vorhandenes Bild, was verwendet werden soll</param>
    public RawBitmap(Bitmap bitmap)
    {
      ReadBitmap(bitmap, out size, out data);
    }

    /// <summary>
    /// liest das Bitmap ein 
    /// </summary>
    /// <param name="bitmap">Bild, welches gelesen werden soll</param>
    /// <param name="size">erkannte Größe des Bilder</param>
    /// <param name="data">eingelesene Pixel-Daten</param>
    static void ReadBitmap(Bitmap bitmap, out SizeInt size, out uint[] data)
    {
      size = new SizeInt(bitmap.Width, bitmap.Height);
      data = new uint[size.w * size.h];

      var bData = bitmap.LockBits(new Rectangle(0, 0, size.w, size.h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      if (size.w * sizeof(uint) != bData.Stride) throw new Exception("inavlid stride-size");
      Unsafe.CopyMemory(Marshal.UnsafeAddrOfPinnedArrayElement(data, 0), bData.Scan0, data.Length * sizeof(uint));
      bitmap.UnlockBits(bData);
    }

    /// <summary>
    /// füllt einen bestimmten Bildbereich
    /// </summary>
    /// <param name="xPos">X-Position des Bereiches</param>
    /// <param name="yPos">Y-Position des Bereiches</param>
    /// <param name="width">Breite des Bereiches</param>
    /// <param name="height">Höhe des Bereiches</param>
    /// <param name="color">Füllfarbe</param>
    public void FillRectangle(int xPos, int yPos, int width, int height, uint color)
    {
      Debug.Assert(xPos >= 0 && xPos < size.w);
      Debug.Assert(yPos >= 0 && yPos < size.h);
      Debug.Assert(width >= 0 && xPos + width <= size.w);
      Debug.Assert(height >= 0 && yPos + height <= size.h);

      color |= 0xff000000;

      fixed (uint* p = &data[xPos + yPos * size.w])
      {
        for (int line = 0; line < height; line++)
        {
          Unsafe.FillLine(p + line * size.w, width, color);
        }
      }
    }

    /// <summary>
    /// zeichnet einen Bildausschnitt
    /// </summary>
    /// <param name="xPos">X-Position des Bildes</param>
    /// <param name="yPos">Y-Position des Bildes</param>
    /// <param name="bitmap">Bild, welches gezeichnet werden soll</param>
    /// <param name="bitmapSource">Ausschnitt des Bildes, welches gezeichnet werden soll</param>
    public void Blit(int xPos, int yPos, RawBitmap bitmap, RectInt bitmapSource)
    {
      Debug.Assert(xPos >= 0 && xPos < size.w);
      Debug.Assert(yPos >= 0 && yPos < size.h);
      Debug.Assert(bitmapSource.w >= 0 && xPos + bitmapSource.w <= size.w);
      Debug.Assert(bitmapSource.h >= 0 && yPos + bitmapSource.h <= size.h);
      Debug.Assert(bitmapSource.x >= 0 && bitmapSource.x < bitmap.size.w);
      Debug.Assert(bitmapSource.y >= 0 && bitmapSource.y < bitmap.size.h);
      Debug.Assert(bitmapSource.x + bitmapSource.w <= bitmap.size.w);
      Debug.Assert(bitmapSource.y + bitmapSource.h <= bitmap.size.h);

      fixed (uint* pd = &data[xPos + yPos * size.w])
      fixed (uint* ps = &bitmap.data[bitmapSource.x + bitmapSource.y * bitmap.size.w])
      {
        for (int line = 0; line < bitmapSource.h; line++)
        {
          Unsafe.CopyLine(pd + line * size.w, ps + line * bitmap.size.w, bitmapSource.w);
        }
      }
    }

    /// <summary>
    /// zeichnet einen Bildausschnitt mit Alpha-Kanal
    /// </summary>
    /// <param name="xPos">X-Position des Bildes</param>
    /// <param name="yPos">Y-Position des Bildes</param>
    /// <param name="bitmap">Bild, welches gezeichnet werden soll</param>
    /// <param name="bitmapSource">Ausschnitt des Bildes, welches gezeichnet werden soll</param>
    public void BlitAlpha(int xPos, int yPos, RawBitmap bitmap, RectInt bitmapSource)
    {
      Debug.Assert(xPos >= 0 && xPos < size.w);
      Debug.Assert(yPos >= 0 && yPos < size.h);
      Debug.Assert(bitmapSource.w >= 0 && xPos + bitmapSource.w <= size.w);
      Debug.Assert(bitmapSource.h >= 0 && yPos + bitmapSource.h <= size.h);
      Debug.Assert(bitmapSource.x >= 0 && bitmapSource.x < bitmap.size.w);
      Debug.Assert(bitmapSource.y >= 0 && bitmapSource.y < bitmap.size.h);
      Debug.Assert(bitmapSource.x + bitmapSource.w <= bitmap.size.w);
      Debug.Assert(bitmapSource.y + bitmapSource.h <= bitmap.size.h);

      fixed (uint* pd = &data[xPos + yPos * size.w])
      fixed (uint* ps = &bitmap.data[bitmapSource.x + bitmapSource.y * bitmap.size.w])
      {
        for (int line = 0; line < bitmapSource.h; line++)
        {
          Unsafe.CopyLineAlpha(pd + line * size.w, ps + line * bitmap.size.w, bitmapSource.w);
        }
      }
    }

    /// <summary>
    /// kopiert den Bildinhalt in ein reguläres Bitmap
    /// </summary>
    /// <param name="outputBitmap">reguläres Bitmap, wohin der Bereich kopiert werden soll</param>
    /// <param name="xPos">X-Position des Bereiches</param>
    /// <param name="yPos">Y-Position des Bereiches</param>
    /// <param name="width">Breite des Bereiches</param>
    /// <param name="height">Höhe des Bereiches</param>
    public void Present(Bitmap outputBitmap, int xPos, int yPos, int width, int height)
    {
      Debug.Assert(outputBitmap.Width == size.w && outputBitmap.Height == size.h);
      Debug.Assert(xPos >= 0 && xPos < size.w);
      Debug.Assert(yPos >= 0 && yPos < size.h);
      Debug.Assert(width >= 0 && xPos + width <= size.w);
      Debug.Assert(height >= 0 && yPos + height <= size.h);

      var bData = outputBitmap.LockBits(new Rectangle(xPos, yPos, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

      var pd = (uint*)bData.Scan0;
      fixed (uint* ps = &data[xPos + yPos * size.w])
      {
        for (int line = 0; line < height; line++)
        {
          Unsafe.CopyLine(pd, ps + line * size.w, width);
          pd += bData.Stride / 4;
        }
      }

      outputBitmap.UnlockBits(bData);
    }
  }
}
