
#region # using *.*

using System;
using System.Runtime.InteropServices.WindowsRuntime;
// ReSharper disable MemberCanBeInternal
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global

#endregion

namespace Windows.UI.Xaml.Media.Imaging
{
  /// <summary>
  /// A disposable cross-platform wrapper around a WriteableBitmap, allowing a common API for Silverlight + WPF with locking + unlocking if necessary
  /// </summary>
  /// <remarks>Attempting to put as many preprocessor hacks in this file, to keep the rest of the codebase relatively clean</remarks>
  public sealed class BitmapContext : IDisposable
  {
    private readonly WriteableBitmap writeableBitmap;

    private readonly int length;
    private readonly byte[] pixelBytes;
    private readonly int[] pixels;

    /// <summary>
    /// Width of the bitmap
    /// </summary>
    public int Width { get { return writeableBitmap.PixelWidth; } }

    /// <summary>
    /// Height of the bitmap
    /// </summary>
    public int Height { get { return writeableBitmap.PixelHeight; } }

    /// <summary>
    /// Creates an instance of a BitmapContext, with default mode = ReadWrite
    /// </summary>
    /// <param name="writeableBitmap"></param>
    public BitmapContext(WriteableBitmap writeableBitmap)
    {
      this.writeableBitmap = writeableBitmap;

      length = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight;
      pixelBytes = new byte[length * 4];
      pixels = new int[length];
      CopyPixels();
    }

    private void CopyPixels()
    {
      writeableBitmap.PixelBuffer.CopyTo(pixelBytes);
      Buffer.BlockCopy(pixelBytes, 0, pixels, 0, pixelBytes.Length);
    }

    /// <summary>
    /// Gets the Pixels array 
    /// </summary>        
    public int[] Pixels { get { return pixels; } }

    /// <summary>
    /// Gets the length of the Pixels array 
    /// </summary>
    public int Length { get { return length; } }

    /// <summary>
    /// Performs a Copy operation from source BitmapContext to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
    public static void BlockCopy(BitmapContext src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
      Buffer.BlockCopy(src.Pixels, srcOffset, dest.Pixels, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from source Array to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
    public static void BlockCopy(int[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
      Buffer.BlockCopy(src, srcOffset, dest.Pixels, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from source Array to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
    public static void BlockCopy(byte[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
      Buffer.BlockCopy(src, srcOffset, dest.Pixels, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from source BitmapContext to destination Array
    /// </summary>
    /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
    public static void BlockCopy(BitmapContext src, int srcOffset, byte[] dest, int destOffset, int count)
    {
      Buffer.BlockCopy(src.Pixels, srcOffset, dest, destOffset, count);
    }

    /// <summary>
    /// Clears the BitmapContext, filling the underlying bitmap with zeros
    /// </summary>
    public void Clear()
    {
      Array.Clear(Pixels, 0, Pixels.Length);
    }

    /// <summary>
    /// Disposes this instance if the underlying platform needs that.
    /// </summary>
    public void Dispose()
    {
      Present();
    }

    public void Present()
    {
      // Copy data back
      using (var stream = writeableBitmap.PixelBuffer.AsStream())
      {
        Buffer.BlockCopy(pixels, 0, pixelBytes, 0, pixelBytes.Length);
        stream.Write(pixelBytes, 0, pixelBytes.Length);
      }
      writeableBitmap.Invalidate();
    }
  }
}