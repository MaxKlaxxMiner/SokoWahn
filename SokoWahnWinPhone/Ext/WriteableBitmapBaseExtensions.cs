
#region # using *.*

using System;
// ReSharper disable MemberCanBeInternal
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  /// <summary>
  /// Collection of extension methods for the WriteableBitmap class.
  /// </summary>
  public static partial class WriteableBitmapExtensions
  {
    const int SizeOfArgb = 4;

    #region Methods

    #region General

    static int ConvertColor(Color color)
    {
      var a = color.A + 1;
      var col = (color.A << 24)
               | ((byte)((color.R * a) >> 8) << 16)
               | ((byte)((color.G * a) >> 8) << 8)
               | ((byte)((color.B * a) >> 8));
      return col;
    }

    /// <summary>
    /// Fills the whole WriteableBitmap with a color.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="color">The color used for filling.</param>
    public static void Clear(this BitmapContext context, Color color)
    {
      var col = ConvertColor(color);
      var pixels = context.Pixels;
      var w = context.Width;
      var h = context.Height;
      var len = w * SizeOfArgb;

      // Fill first line
      for (var x = 0; x < w; x++)
      {
        pixels[x] = col;
      }

      // Copy first line
      var blockHeight = 1;
      var y = 1;
      while (y < h)
      {
        BitmapContext.BlockCopy(context, 0, context, y * len, blockHeight * len);
        y += blockHeight;
        blockHeight = Math.Min(2 * blockHeight, h - y);
      }
    }

    /// <summary>
    /// Clones the specified WriteableBitmap.
    /// </summary>
    /// <param name="srcContext">The WriteableBitmap.</param>
    /// <returns>A copy of the WriteableBitmap.</returns>
    public static WriteableBitmap Clone(this BitmapContext srcContext)
    {
      var result = BitmapFactory.New(srcContext.Width, srcContext.Height);
      using (var destContext = result.GetBitmapContext())
      {
        BitmapContext.BlockCopy(srcContext, 0, destContext, 0, srcContext.Length * SizeOfArgb);
      }
      return result;
    }

    #endregion

    #region ForEach

    /// <summary>
    /// Applies the given function to all the pixels of the bitmap in 
    /// order to set their color.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="func">The function to apply. With parameters x, y and a color as a result</param>
    public static void ForEach(this BitmapContext context, Func<int, int, Color> func)
    {
      var pixels = context.Pixels;
      int w = context.Width;
      int h = context.Height;
      int index = 0;

      for (int y = 0; y < h; y++)
      {
        for (int x = 0; x < w; x++)
        {
          var color = func(x, y);
          pixels[index++] = ConvertColor(color);
        }
      }
    }

    /// <summary>
    /// Applies the given function to all the pixels of the bitmap in 
    /// order to set their color.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="func">The function to apply. With parameters x, y, source color and a color as a result</param>
    public static void ForEach(this BitmapContext context, Func<int, int, Color, Color> func)
    {
      var pixels = context.Pixels;
      int w = context.Width;
      int h = context.Height;
      int index = 0;

      for (int y = 0; y < h; y++)
      {
        for (int x = 0; x < w; x++)
        {
          int c = pixels[index];
          var color = func(x, y, Color.FromArgb((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)(c)));
          pixels[index++] = ConvertColor(color);
        }
      }
    }

    #endregion

    #region Get Pixel / Brightness

    /// <summary>
    /// Gets the color of the pixel at the x, y coordinate as integer.  
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate of the pixel.</param>
    /// <param name="y">The y coordinate of the pixel.</param>
    /// <returns>The color of the pixel at x, y.</returns>
    public static int GetPixeli(this BitmapContext context, int x, int y)
    {
      return context.Pixels[y * context.Width + x];
    }

    /// <summary>
    /// Gets the color of the pixel at the x, y coordinate as a Color struct.  
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate of the pixel.</param>
    /// <param name="y">The y coordinate of the pixel.</param>
    /// <returns>The color of the pixel at x, y as a Color struct.</returns>
    public static Color GetPixel(this BitmapContext context, int x, int y)
    {
      var c = context.Pixels[y * context.Width + x];
      var a = (byte)(c >> 24);

      // Prevent division by zero
      int ai = a;
      if (ai == 0)
      {
        ai = 1;
      }

      // Scale inverse alpha to use cheap integer mul bit shift
      ai = ((255 << 8) / ai);
      return Color.FromArgb(a,
                           (byte)((((c >> 16) & 0xFF) * ai) >> 8),
                           (byte)((((c >> 8) & 0xFF) * ai) >> 8),
                           (byte)((((c & 0xFF) * ai) >> 8)));
    }

    /// <summary>
    /// Gets the brightness / luminance of the pixel at the x, y coordinate as byte.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate of the pixel.</param>
    /// <param name="y">The y coordinate of the pixel.</param>
    /// <returns>The brightness of the pixel at x, y.</returns>
    public static byte GetBrightness(this BitmapContext context, int x, int y)
    {
      // Extract color components
      var c = context.Pixels[y * context.Width + x];
      var r = (byte)(c >> 16);
      var g = (byte)(c >> 8);
      var b = (byte)(c);

      // Convert to gray with constant factors 0.2126, 0.7152, 0.0722
      return (byte)((r * 6966 + g * 23436 + b * 2366) >> 15);
    }

    #endregion

    #region SetPixel

    #region Without alpha

    /// <summary>
    /// Sets the color of the pixel using a precalculated index (faster). 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="r">The red value of the color.</param>
    /// <param name="g">The green value of the color.</param>
    /// <param name="b">The blue value of the color.</param>
    public static void SetPixeli(this BitmapContext context, int index, byte r, byte g, byte b)
    {
      context.Pixels[index] = (255 << 24) | (r << 16) | (g << 8) | b;
    }

    /// <summary>
    /// Sets the color of the pixel. 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate (row).</param>
    /// <param name="y">The y coordinate (column).</param>
    /// <param name="r">The red value of the color.</param>
    /// <param name="g">The green value of the color.</param>
    /// <param name="b">The blue value of the color.</param>
    public static void SetPixel(this BitmapContext context, int x, int y, byte r, byte g, byte b)
    {
      context.Pixels[y * context.Width + x] = (255 << 24) | (r << 16) | (g << 8) | b;
    }

    #endregion

    #region With alpha

    /// <summary>
    /// Sets the color of the pixel including the alpha value and using a precalculated index (faster). 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="a">The alpha value of the color.</param>
    /// <param name="r">The red value of the color.</param>
    /// <param name="g">The green value of the color.</param>
    /// <param name="b">The blue value of the color.</param>
    public static void SetPixeli(this BitmapContext context, int index, byte a, byte r, byte g, byte b)
    {
      context.Pixels[index] = (a << 24) | (r << 16) | (g << 8) | b;
    }

    /// <summary>
    /// Sets the color of the pixel including the alpha value. 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate (row).</param>
    /// <param name="y">The y coordinate (column).</param>
    /// <param name="a">The alpha value of the color.</param>
    /// <param name="r">The red value of the color.</param>
    /// <param name="g">The green value of the color.</param>
    /// <param name="b">The blue value of the color.</param>
    public static void SetPixel(this BitmapContext context, int x, int y, byte a, byte r, byte g, byte b)
    {
      context.Pixels[y * context.Width + x] = (a << 24) | (r << 16) | (g << 8) | b;
    }

    #endregion

    #region With System.Windows.Media.Color

    /// <summary>
    /// Sets the color of the pixel using a precalculated index (faster). 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="color">The color.</param>
    public static void SetPixeli(this BitmapContext context, int index, Color color)
    {
      context.Pixels[index] = ConvertColor(color);
    }

    /// <summary>
    /// Sets the color of the pixel. 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate (row).</param>
    /// <param name="y">The y coordinate (column).</param>
    /// <param name="color">The color.</param>
    public static void SetPixel(this BitmapContext context, int x, int y, Color color)
    {
      context.Pixels[y * context.Width + x] = ConvertColor(color);
    }

    /// <summary>
    /// Sets the color of the pixel using an extra alpha value and a precalculated index (faster). 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="a">The alpha value of the color.</param>
    /// <param name="color">The color.</param>
    public static void SetPixeli(this BitmapContext context, int index, byte a, Color color)
    {
      // Add one to use mul and cheap bit shift for multiplicaltion
      var ai = a + 1;
      context.Pixels[index] = (a << 24)
                 | ((byte)((color.R * ai) >> 8) << 16)
                 | ((byte)((color.G * ai) >> 8) << 8)
                 | ((byte)((color.B * ai) >> 8));
    }

    /// <summary>
    /// Sets the color of the pixel using an extra alpha value. 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate (row).</param>
    /// <param name="y">The y coordinate (column).</param>
    /// <param name="a">The alpha value of the color.</param>
    /// <param name="color">The color.</param>
    public static void SetPixel(this BitmapContext context, int x, int y, byte a, Color color)
    {
      // Add one to use mul and cheap bit shift for multiplicaltion
      var ai = a + 1;
      context.Pixels[y * context.Width + x] = (a << 24)
                                   | ((byte)((color.R * ai) >> 8) << 16)
                                   | ((byte)((color.G * ai) >> 8) << 8)
                                   | ((byte)((color.B * ai) >> 8));
    }

    /// <summary>
    /// Sets the color of the pixel using a precalculated index (faster).  
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="color">The color.</param>
    public static void SetPixeli(this BitmapContext context, int index, int color)
    {
      context.Pixels[index] = color;
    }

    /// <summary>
    /// Sets the color of the pixel. 
    /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate (row).</param>
    /// <param name="y">The y coordinate (column).</param>
    /// <param name="color">The color.</param>
    public static void SetPixel(this BitmapContext context, int x, int y, int color)
    {
      context.Pixels[y * context.Width + x] = color;
    }

    #endregion

    #endregion

    #endregion
  }
}