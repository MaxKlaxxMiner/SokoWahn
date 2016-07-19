
#region # using *.*

using System;
// ReSharper disable CheckNamespace
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  #region Enums

  /// <summary>
  /// The interpolation method.
  /// </summary>
  public enum Interpolation
  {
    /// <summary>
    /// The nearest neighbor algorithm simply selects the color of the nearest pixel.
    /// </summary>
    NearestNeighbor = 0,

    /// <summary>
    /// Linear interpolation in 2D using the average of 3 neighboring pixels.
    /// </summary>
    Bilinear
  }

  /// <summary>
  /// The mode for flipping.
  /// </summary>
  public enum FlipMode
  {
    /// <summary>
    /// Flips the image vertical (around the center of the y-axis).
    /// </summary>
    Vertical,

    /// <summary>
    /// Flips the image horizontal (around the center of the x-axis).
    /// </summary>
    Horizontal
  }

  #endregion

  /// <summary>
  /// Collection of transformation extension methods for the WriteableBitmap class.
  /// </summary>
  public static partial class WriteableBitmapExtensions
  {
    #region Methods

    #region Crop

    /// <summary>
    /// Creates a new cropped WriteableBitmap.
    /// </summary>
    /// <param name="srcContext">The WriteableBitmap.</param>
    /// <param name="x">The x coordinate of the rectangle that defines the crop region.</param>
    /// <param name="y">The y coordinate of the rectangle that defines the crop region.</param>
    /// <param name="width">The width of the rectangle that defines the crop region.</param>
    /// <param name="height">The height of the rectangle that defines the crop region.</param>
    /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
    public static WriteableBitmap Crop(this BitmapContext srcContext, int x, int y, int width, int height)
    {
      var srcWidth = srcContext.Width;
      var srcHeight = srcContext.Height;

      // If the rectangle is completly out of the bitmap
      if (x > srcWidth || y > srcHeight)
      {
        return BitmapFactory.New(0, 0);
      }

      // Clamp to boundaries
      if (x < 0) x = 0;
      if (x + width > srcWidth) width = srcWidth - x;
      if (y < 0) y = 0;
      if (y + height > srcHeight) height = srcHeight - y;

      // Copy the pixels line by line using fast BlockCopy
      var result = BitmapFactory.New(width, height);
      using (var destContext = result.GetBitmapContext())
      {
        for (var line = 0; line < height; line++)
        {
          var srcOff = ((y + line) * srcWidth + x) * SizeOfArgb;
          var dstOff = line * width * SizeOfArgb;
          BitmapContext.BlockCopy(srcContext, srcOff, destContext, dstOff, width * SizeOfArgb);
        }

        return result;
      }
    }

    /// <summary>
    /// Creates a new cropped WriteableBitmap.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="region">The rectangle that defines the crop region.</param>
    /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
    public static WriteableBitmap Crop(this BitmapContext context, RectInt region)
    {
      return context.Crop(region.x, region.y, region.w, region.h);
    }

    #endregion

    #region Resize

    /// <summary>
    /// Creates a new resized WriteableBitmap.
    /// </summary>
    /// <param name="srcContext">The WriteableBitmap.</param>
    /// <param name="width">The new desired width.</param>
    /// <param name="height">The new desired height.</param>
    /// <param name="interpolation">The interpolation method that should be used.</param>
    /// <returns>A new WriteableBitmap that is a resized version of the input.</returns>
    public static WriteableBitmap Resize(this BitmapContext srcContext, int width, int height, Interpolation interpolation)
    {
      var pd = Resize(srcContext, srcContext.Width, srcContext.Height, width, height, interpolation);

      var result = BitmapFactory.New(width, height);
      BitmapContext.BlockCopy(pd, 0, srcContext, 0, SizeOfArgb * pd.Length);
      return result;
    }

    /// <summary>
    /// Creates a new resized bitmap.
    /// </summary>
    /// <param name="srcContext">The source context.</param>
    /// <param name="widthSource">The width of the source pixels.</param>
    /// <param name="heightSource">The height of the source pixels.</param>
    /// <param name="width">The new desired width.</param>
    /// <param name="height">The new desired height.</param>
    /// <param name="interpolation">The interpolation method that should be used.</param>
    /// <returns>A new bitmap that is a resized version of the input.</returns>
    public static int[] Resize(BitmapContext srcContext, int widthSource, int heightSource, int width, int height, Interpolation interpolation)
    {
      var pixels = srcContext.Pixels;
      var pd = new int[width * height];
      var xs = (float)widthSource / width;
      var ys = (float)heightSource / height;

      float sx, sy;
      int x0;
      int y0;

      // Nearest Neighbor
      switch (interpolation)
      {
        case Interpolation.NearestNeighbor:
        {
          var srcIdx = 0;
          for (var y = 0; y < height; y++)
          {
            for (var x = 0; x < width; x++)
            {
              sx = x * xs;
              sy = y * ys;
              x0 = (int)sx;
              y0 = (int)sy;

              pd[srcIdx++] = pixels[y0 * widthSource + x0];
            }
          }
        } break;
        case Interpolation.Bilinear:
        {
          var srcIdx = 0;
          for (var y = 0; y < height; y++)
          {
            for (var x = 0; x < width; x++)
            {
              sx = x * xs;
              sy = y * ys;
              x0 = (int)sx;
              y0 = (int)sy;

              // Calculate coordinates of the 4 interpolation points
              var fracx = sx - x0;
              var fracy = sy - y0;
              var ifracx = 1f - fracx;
              var ifracy = 1f - fracy;
              var x1 = x0 + 1;
              if (x1 >= widthSource)
              {
                x1 = x0;
              }
              var y1 = y0 + 1;
              if (y1 >= heightSource)
              {
                y1 = y0;
              }


              // Read source color
              var c = pixels[y0 * widthSource + x0];
              var c1A = (byte)(c >> 24);
              var c1R = (byte)(c >> 16);
              var c1G = (byte)(c >> 8);
              var c1B = (byte)(c);

              c = pixels[y0 * widthSource + x1];
              var c2A = (byte)(c >> 24);
              var c2R = (byte)(c >> 16);
              var c2G = (byte)(c >> 8);
              var c2B = (byte)(c);

              c = pixels[y1 * widthSource + x0];
              var c3A = (byte)(c >> 24);
              var c3R = (byte)(c >> 16);
              var c3G = (byte)(c >> 8);
              var c3B = (byte)(c);

              c = pixels[y1 * widthSource + x1];
              var c4A = (byte)(c >> 24);
              var c4R = (byte)(c >> 16);
              var c4G = (byte)(c >> 8);
              var c4B = (byte)(c);


              // Calculate colors
              // Alpha
              var l0 = ifracx * c1A + fracx * c2A;
              var l1 = ifracx * c3A + fracx * c4A;
              var a = (byte)(ifracy * l0 + fracy * l1);

              // Red
              l0 = ifracx * c1R * c1A + fracx * c2R * c2A;
              l1 = ifracx * c3R * c3A + fracx * c4R * c4A;
              var rf = ifracy * l0 + fracy * l1;

              // Green
              l0 = ifracx * c1G * c1A + fracx * c2G * c2A;
              l1 = ifracx * c3G * c3A + fracx * c4G * c4A;
              var gf = ifracy * l0 + fracy * l1;

              // Blue
              l0 = ifracx * c1B * c1A + fracx * c2B * c2A;
              l1 = ifracx * c3B * c3A + fracx * c4B * c4A;
              var bf = ifracy * l0 + fracy * l1;

              // Divide by alpha
              if (a > 0)
              {
                rf = rf / a;
                gf = gf / a;
                bf = bf / a;
              }

              // Cast to byte
              var r = (byte)rf;
              var g = (byte)gf;
              var b = (byte)bf;

              // Write destination
              pd[srcIdx++] = (a << 24) | (r << 16) | (g << 8) | b;
            }
          }
        } break;
      }
      return pd;
    }

    #endregion

    #region Rotate

    /// <summary>
    /// Rotates the bitmap in 90° steps clockwise and returns a new rotated WriteableBitmap.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="angle">The angle in degress the bitmap should be rotated in 90° steps clockwise.</param>
    /// <returns>A new WriteableBitmap that is a rotated version of the input.</returns>
    public static WriteableBitmap Rotate(this BitmapContext context, int angle)
    {
      // Use refs for faster access (really important!) speeds up a lot!
      var w = context.Width;
      var h = context.Height;
      var p = context.Pixels;
      var i = 0;
      WriteableBitmap result;
      angle %= 360;

      if (angle > 0 && angle <= 90)
      {
        result = BitmapFactory.New(h, w);
        using (var destContext = result.GetBitmapContext())
        {
          var rp = destContext.Pixels;
          for (var x = 0; x < w; x++)
          {
            for (var y = h - 1; y >= 0; y--)
            {
              var srcInd = y * w + x;
              rp[i] = p[srcInd];
              i++;
            }
          }
        }
      }
      else if (angle > 90 && angle <= 180)
      {
        result = BitmapFactory.New(w, h);
        using (var destContext = result.GetBitmapContext())
        {
          var rp = destContext.Pixels;
          for (var y = h - 1; y >= 0; y--)
          {
            for (var x = w - 1; x >= 0; x--)
            {
              var srcInd = y * w + x;
              rp[i] = p[srcInd];
              i++;
            }
          }
        }
      }
      else if (angle > 180 && angle <= 270)
      {
        result = BitmapFactory.New(h, w);
        using (var destContext = result.GetBitmapContext())
        {
          var rp = destContext.Pixels;
          for (var x = w - 1; x >= 0; x--)
          {
            for (var y = 0; y < h; y++)
            {
              var srcInd = y * w + x;
              rp[i] = p[srcInd];
              i++;
            }
          }
        }
      }
      else
      {
        result = context.Clone();
      }
      return result;
    }

    /// <summary>
    /// Rotates the bitmap in any degree returns a new rotated WriteableBitmap.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="angle">Arbitrary angle in 360 Degrees (positive = clockwise).</param>
    /// <param name="crop">if true: keep the size, false: adjust canvas to new size</param>
    /// <returns>A new WriteableBitmap that is a rotated version of the input.</returns>
    public static WriteableBitmap RotateFree(this BitmapContext context, double angle, bool crop = true)
    {
      // rotating clockwise, so it's negative relative to Cartesian quadrants
      double cnAngle = -1.0 * (Math.PI / 180) * angle;

      int iCentreX, iCentreY;
      int iDestCentreX, iDestCentreY;
      int iWidth, iHeight, newWidth, newHeight;

      iWidth = context.Width;
      iHeight = context.Height;

      if (crop)
      {
        newWidth = iWidth;
        newHeight = iHeight;
      }
      else
      {
        var rad = angle / (180 / Math.PI);
        newWidth = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iHeight) + Math.Abs(Math.Cos(rad) * iWidth));
        newHeight = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iWidth) + Math.Abs(Math.Cos(rad) * iHeight));
      }


      iCentreX = iWidth / 2;
      iCentreY = iHeight / 2;

      iDestCentreX = newWidth / 2;
      iDestCentreY = newHeight / 2;

      var bmBilinearInterpolation = BitmapFactory.New(newWidth, newHeight);

      using (var bilinearContext = bmBilinearInterpolation.GetBitmapContext())
      {
        var newp = bilinearContext.Pixels;
        var oldp = context.Pixels;
        var oldw = context.Width;

        // assigning pixels of destination image from source image
        // with bilinear interpolation
        int i;
        for (i = 0; i < newHeight; ++i)
        {
          int j;
          for (j = 0; j < newWidth; ++j)
          {
            // convert raster to Cartesian
            var x = j - iDestCentreX;
            var y = iDestCentreY - i;

            // convert Cartesian to polar
            var fDistance = Math.Sqrt(x * x + y * y);
            double fPolarAngle;
            if (x == 0)
            {
              if (y == 0)
              {
                // centre of image, no rotation needed
                newp[i * newWidth + j] = oldp[iCentreY * oldw + iCentreX];
                continue;
              }
              if (y < 0)
              {
                fPolarAngle = 1.5 * Math.PI;
              }
              else
              {
                fPolarAngle = 0.5 * Math.PI;
              }
            }
            else
            {
              fPolarAngle = Math.Atan2(y, x);
            }

            // the crucial rotation part
            // "reverse" rotate, so minus instead of plus
            fPolarAngle -= cnAngle;

            // convert polar to Cartesian
            var fTrueX = fDistance * Math.Cos(fPolarAngle);
            var fTrueY = fDistance * Math.Sin(fPolarAngle);

            // convert Cartesian to raster
            fTrueX = fTrueX + iCentreX;
            fTrueY = iCentreY - fTrueY;

            var iFloorX = (int)(Math.Floor(fTrueX));
            var iFloorY = (int)(Math.Floor(fTrueY));
            var iCeilingX = (int)(Math.Ceiling(fTrueX));
            var iCeilingY = (int)(Math.Ceiling(fTrueY));

            // check bounds
            if (iFloorX < 0 || iCeilingX < 0 || iFloorX >= iWidth || iCeilingX >= iWidth || iFloorY < 0 ||
                iCeilingY < 0 || iFloorY >= iHeight || iCeilingY >= iHeight) continue;

            var fDeltaX = fTrueX - iFloorX;
            var fDeltaY = fTrueY - iFloorY;

            var clrTopLeft = context.GetPixel(iFloorX, iFloorY);
            var clrTopRight = context.GetPixel(iCeilingX, iFloorY);
            var clrBottomLeft = context.GetPixel(iFloorX, iCeilingY);
            var clrBottomRight = context.GetPixel(iCeilingX, iCeilingY);

            // linearly interpolate horizontally between top neighbours
            var fTopRed = (1 - fDeltaX) * clrTopLeft.R + fDeltaX * clrTopRight.R;
            var fTopGreen = (1 - fDeltaX) * clrTopLeft.G + fDeltaX * clrTopRight.G;
            var fTopBlue = (1 - fDeltaX) * clrTopLeft.B + fDeltaX * clrTopRight.B;
            var fTopAlpha = (1 - fDeltaX) * clrTopLeft.A + fDeltaX * clrTopRight.A;

            // linearly interpolate horizontally between bottom neighbours
            var fBottomRed = (1 - fDeltaX) * clrBottomLeft.R + fDeltaX * clrBottomRight.R;
            var fBottomGreen = (1 - fDeltaX) * clrBottomLeft.G + fDeltaX * clrBottomRight.G;
            var fBottomBlue = (1 - fDeltaX) * clrBottomLeft.B + fDeltaX * clrBottomRight.B;
            var fBottomAlpha = (1 - fDeltaX) * clrBottomLeft.A + fDeltaX * clrBottomRight.A;

            // linearly interpolate vertically between top and bottom interpolated results
            var iRed = (int)(Math.Round((1 - fDeltaY) * fTopRed + fDeltaY * fBottomRed));
            var iGreen = (int)(Math.Round((1 - fDeltaY) * fTopGreen + fDeltaY * fBottomGreen));
            var iBlue = (int)(Math.Round((1 - fDeltaY) * fTopBlue + fDeltaY * fBottomBlue));
            var iAlpha = (int)(Math.Round((1 - fDeltaY) * fTopAlpha + fDeltaY * fBottomAlpha));

            // make sure colour values are valid
            if (iRed < 0) iRed = 0;
            if (iRed > 255) iRed = 255;
            if (iGreen < 0) iGreen = 0;
            if (iGreen > 255) iGreen = 255;
            if (iBlue < 0) iBlue = 0;
            if (iBlue > 255) iBlue = 255;
            if (iAlpha < 0) iAlpha = 0;
            if (iAlpha > 255) iAlpha = 255;

            var a = iAlpha + 1;
            newp[i * newWidth + j] = (iAlpha << 24)
                                   | ((byte)((iRed * a) >> 8) << 16)
                                   | ((byte)((iGreen * a) >> 8) << 8)
                                   | ((byte)((iBlue * a) >> 8));
          }
        }
        return bmBilinearInterpolation;
      }
    }

    #endregion

    #region Flip

    /// <summary>
    /// Flips (reflects the image) eiter vertical or horizontal.
    /// </summary>
    /// <param name="context">The WriteableBitmap.</param>
    /// <param name="flipMode">The flip mode.</param>
    /// <returns>A new WriteableBitmap that is a flipped version of the input.</returns>
    public static WriteableBitmap Flip(this BitmapContext context, FlipMode flipMode)
    {
      // Use refs for faster access (really important!) speeds up a lot!
      var w = context.Width;
      var h = context.Height;
      var p = context.Pixels;
      var i = 0;
      WriteableBitmap result = null;

      switch (flipMode)
      {
        case FlipMode.Horizontal:
        {
          result = BitmapFactory.New(w, h);
          using (var destContext = result.GetBitmapContext())
          {
            var rp = destContext.Pixels;
            for (var y = h - 1; y >= 0; y--)
            {
              for (var x = 0; x < w; x++)
              {
                var srcInd = y * w + x;
                rp[i] = p[srcInd];
                i++;
              }
            }
          }
        } break;
        case FlipMode.Vertical:
        {
          result = BitmapFactory.New(w, h);
          using (var destContext = result.GetBitmapContext())
          {
            var rp = destContext.Pixels;
            for (var y = 0; y < h; y++)
            {
              for (var x = w - 1; x >= 0; x--)
              {
                var srcInd = y * w + x;
                rp[i] = p[srcInd];
                i++;
              }
            }
          }
        } break;
      }

      return result;
    }

    #endregion

    #endregion
  }
}
