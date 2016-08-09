
#region # using *.*

using System;
// ReSharper disable CheckNamespace
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedMember.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  #region Enum

  /// <summary>
  /// The blending mode.
  /// </summary>
  public enum BlendMode
  {
    /// <summary>
    /// Alpha blendiing uses the alpha channel to combine the source and destination. 
    /// </summary>
    Alpha,

    /// <summary>
    /// Additive blending adds the colors of the source and the destination.
    /// </summary>
    Additive,

    /// <summary>
    /// Subtractive blending subtracts the source color from the destination.
    /// </summary>
    Subtractive,

    /// <summary>
    /// Uses the source color as a mask.
    /// </summary>
    Mask,

    /// <summary>
    /// Multiplies the source color with the destination color.
    /// </summary>
    Multiply,

    /// <summary>
    /// Ignores the specified Color
    /// </summary>
    ColorKeying,

    /// <summary>
    /// No blending just copies the pixels from the source.
    /// </summary>
    None
  }

  #endregion

  public struct RectInt
  {
    public int x;
    public int y;
    public int w;
    public int h;

    public bool IsEmpty
    {
      get
      {
        return (w * h) == 0;
      }
    }

    public RectInt(int x, int y, int w, int h)
    {
      this.x = x;
      this.y = y;
      this.w = w;
      this.h = h;
    }

    public RectInt(PointInt p, SizeInt s)
    {
      x = p.x;
      y = p.y;
      w = s.w;
      h = s.h;
    }

    static readonly RectInt Empty = new RectInt(0, 0, 0, 0);

    public static RectInt Intersect(RectInt a, RectInt b)
    {
      int x = Math.Max(a.x, b.x);
      int num1 = Math.Min(a.x + a.w, b.x + b.w);
      int y = Math.Max(a.y, b.y);
      int num2 = Math.Min(a.y + a.h, b.y + b.h);
      if (num1 >= x && num2 >= y) return new RectInt(x, y, num1 - x, num2 - y);
      return Empty;
    }
  }

  public struct PointInt
  {
    public int x;
    public int y;
    public PointInt(int x, int y)
    {
      this.x = x;
      this.y = y;
    }
  }

  public struct SizeInt
  {
    public int w;
    public int h;
    public SizeInt(int w, int h)
    {
      this.w = w;
      this.h = h;
    }
  }

  /// <summary>
  /// Collection of blit (copy) extension methods for the WriteableBitmap class.
  /// </summary>
  public static partial class WriteableBitmapExtensions
  {
    #region Methods

    /// <summary>
    /// Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
    /// </summary>
    /// <param name="context">The destination WriteableBitmap.</param>
    /// <param name="destRect">The rectangle that defines the destination region.</param>
    /// <param name="source">The source WriteableBitmap.</param>
    /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
    /// <param name="BlendMode">The blending mode <see cref="BlendMode"/>.</param>
    public static void Blit(this BitmapContext context, RectInt destRect, BitmapContext source, RectInt sourceRect, BlendMode BlendMode)
    {
      Blit(context, destRect, source, sourceRect, Colors.White, BlendMode);
    }

    /// <summary>
    /// Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
    /// </summary>
    /// <param name="context">The destination WriteableBitmap.</param>
    /// <param name="destRect">The rectangle that defines the destination region.</param>
    /// <param name="source">The source WriteableBitmap.</param>
    /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
    public static void Blit(this BitmapContext context, RectInt destRect, BitmapContext source, RectInt sourceRect)
    {
      Blit(context, destRect, source, sourceRect, Colors.White, BlendMode.Alpha);
    }

    /// <summary>
    /// Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
    /// </summary>
    /// <param name="context">The destination WriteableBitmap.</param>
    /// <param name="destPosition">The destination position in the destination bitmap.</param>
    /// <param name="source">The source WriteableBitmap.</param>
    /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
    /// <param name="color">If not Colors.White, will tint the source image. A partially transparent color and the image will be drawn partially transparent.</param>
    /// <param name="blendMode">The blending mode <see cref="BlendMode"/>.</param>
    public static void Blit(this BitmapContext context, PointInt destPosition, BitmapContext source, RectInt sourceRect, Color color, BlendMode blendMode)
    {
      var destRect = new RectInt(destPosition, new SizeInt(sourceRect.w, sourceRect.h));
      Blit(context, destRect, source, sourceRect, color, blendMode);
    }

    /// <summary>
    /// Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
    /// </summary>
    /// <param name="destContext">The destination WriteableBitmap.</param>
    /// <param name="destRect">The rectangle that defines the destination region.</param>
    /// <param name="srcContext">The source WriteableBitmap.</param>
    /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
    /// <param name="color">If not Colors.White, will tint the source image. A partially transparent color and the image will be drawn partially transparent. If the BlendMode is ColorKeying, this color will be used as color key to mask all pixels with this value out.</param>
    /// <param name="blendMode">The blending mode <see cref="BlendMode"/>.</param>
    public static void Blit(this BitmapContext destContext, RectInt destRect, BitmapContext srcContext, RectInt sourceRect, Color color, BlendMode blendMode)
    {
      if (color.A == 0)
      {
        return;
      }
      int dw = destRect.w;
      int dh = destRect.h;

      int sourceWidth = srcContext.Width;
      int dpw = destContext.Width;
      int dph = destContext.Height;
      var intersect = RectInt.Intersect(new RectInt(0, 0, dpw, dph), destRect);
      if (intersect.IsEmpty)
      {
        return;
      }

      var sourcePixels = srcContext.Pixels;
      var destPixels = destContext.Pixels;
      int sourceLength = srcContext.Length;
      int px = destRect.x;
      int py = destRect.y;
      int y;
      double jj;
      int sr = 0;
      int sg = 0;
      int sb = 0;
      int sa = 0;
      int ca = color.A;
      int cr = color.R;
      int cg = color.G;
      int cb = color.B;
      bool tinted = color != Colors.White;
      var sw = sourceRect.w;
      var sdx = sourceRect.w / (double)destRect.w;
      var sdy = sourceRect.h / (double)destRect.h;
      int sourceStartX = sourceRect.x;
      int sourceStartY = sourceRect.y;
      int lastii, lastjj;
      lastii = -1;
      lastjj = -1;
      jj = sourceStartY;
      y = py;
      for (int j = 0; j < dh; j++)
      {
        if (y >= 0 && y < dph)
        {
          double ii = sourceStartX;
          var idx = px + y * dpw;
          var x = px;
          var sourcePixel = sourcePixels[0];

          // Scanline BlockCopy is much faster (3.5x) if no tinting and blending is needed,
          // even for smaller sprites like the 32x32 particles. 
          int sourceIdx;
          if (blendMode == BlendMode.None && !tinted)
          {
            sourceIdx = (int)ii + (int)jj * sourceWidth;
            var offset = x < 0 ? -x : 0;
            var xx = x + offset;
            var wx = sourceWidth - offset;
            var len = xx + wx < dpw ? wx : dpw - xx;
            if (len > sw) len = sw;
            if (len > dw) len = dw;
            BitmapContext.BlockCopy(srcContext, (sourceIdx + offset) * 4, destContext, (idx + offset) * 4, len * 4);
          }

          // Pixel by pixel copying
          else
          {
            for (int i = 0; i < dw; i++)
            {
              if (x >= 0 && x < dpw)
              {
                if ((int)ii != lastii || (int)jj != lastjj)
                {
                  sourceIdx = (int)ii + (int)jj * sourceWidth;
                  if (sourceIdx >= 0 && sourceIdx < sourceLength)
                  {
                    sourcePixel = sourcePixels[sourceIdx];
                    sa = ((sourcePixel >> 24) & 0xff);
                    sr = ((sourcePixel >> 16) & 0xff);
                    sg = ((sourcePixel >> 8) & 0xff);
                    sb = ((sourcePixel) & 0xff);
                    if (tinted && sa != 0)
                    {
                      sa = (((sa * ca) * 0x8081) >> 23);
                      sr = ((((((sr * cr) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
                      sg = ((((((sg * cg) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
                      sb = ((((((sb * cb) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
                      sourcePixel = (sa << 24) | (sr << 16) | (sg << 8) | sb;
                    }
                  }
                  else
                  {
                    sa = 0;
                  }
                }
                if (blendMode == BlendMode.None)
                {
                  destPixels[idx] = sourcePixel;
                }
                else if (blendMode == BlendMode.ColorKeying)
                {
                  sr = ((sourcePixel >> 16) & 0xff);
                  sg = ((sourcePixel >> 8) & 0xff);
                  sb = ((sourcePixel) & 0xff);

                  if (sr != color.R || sg != color.G || sb != color.B)
                  {
                    destPixels[idx] = sourcePixel;
                  }

                }
                else
                {
                  int dr;
                  int dg;
                  int db;
                  int da;
                  if (blendMode == BlendMode.Mask)
                  {
                    int destPixel = destPixels[idx];
                    da = ((destPixel >> 24) & 0xff);
                    dr = ((destPixel >> 16) & 0xff);
                    dg = ((destPixel >> 8) & 0xff);
                    db = ((destPixel) & 0xff);
                    destPixel = ((((da * sa) * 0x8081) >> 23) << 24) |
                                ((((dr * sa) * 0x8081) >> 23) << 16) |
                                ((((dg * sa) * 0x8081) >> 23) << 8) |
                                ((((db * sa) * 0x8081) >> 23));
                    destPixels[idx] = destPixel;
                  }
                  else if (sa > 0)
                  {
                    int destPixel = destPixels[idx];
                    da = ((destPixel >> 24) & 0xff);
                    if ((sa == 255 || da == 0) &&
                        blendMode != BlendMode.Additive
                        && blendMode != BlendMode.Subtractive
                        && blendMode != BlendMode.Multiply
                      )
                    {
                      destPixels[idx] = sourcePixel;
                    }
                    else
                    {
                      dr = ((destPixel >> 16) & 0xff);
                      dg = ((destPixel >> 8) & 0xff);
                      db = ((destPixel) & 0xff);
                      if (blendMode == BlendMode.Alpha)
                      {
                        destPixel = ((sa + (((da * (255 - sa)) * 0x8081) >> 23)) << 24) |
                                    ((sr + (((dr * (255 - sa)) * 0x8081) >> 23)) << 16) |
                                    ((sg + (((dg * (255 - sa)) * 0x8081) >> 23)) << 8) |
                                    ((sb + (((db * (255 - sa)) * 0x8081) >> 23)));
                      }
                      else if (blendMode == BlendMode.Additive)
                      {
                        int a = (255 <= sa + da) ? 255 : (sa + da);
                        destPixel = (a << 24) |
                                    (((a <= sr + dr) ? a : (sr + dr)) << 16) |
                                    (((a <= sg + dg) ? a : (sg + dg)) << 8) |
                                    (((a <= sb + db) ? a : (sb + db)));
                      }
                      else if (blendMode == BlendMode.Subtractive)
                      {
                        int a = da;
                        destPixel = (a << 24) |
                                    (((sr >= dr) ? 0 : (sr - dr)) << 16) |
                                    (((sg >= dg) ? 0 : (sg - dg)) << 8) |
                                    (((sb >= db) ? 0 : (sb - db)));
                      }
                      else if (blendMode == BlendMode.Multiply)
                      {
                        // Faster than a division like (s * d) / 255 are 2 shifts and 2 adds
                        int ta = (sa * da) + 128;
                        int tr = (sr * dr) + 128;
                        int tg = (sg * dg) + 128;
                        int tb = (sb * db) + 128;

                        int ba = ((ta >> 8) + ta) >> 8;
                        int br = ((tr >> 8) + tr) >> 8;
                        int bg = ((tg >> 8) + tg) >> 8;
                        int bb = ((tb >> 8) + tb) >> 8;

                        destPixel = (ba << 24) |
                                    ((ba <= br ? ba : br) << 16) |
                                    ((ba <= bg ? ba : bg) << 8) |
                                    ((ba <= bb ? ba : bb));
                      }

                      destPixels[idx] = destPixel;
                    }
                  }
                }
              }
              x++;
              idx++;
              ii += sdx;
            }
          }
        }
        jj += sdy;
        y++;
      }
    }

    #endregion
  }
}
