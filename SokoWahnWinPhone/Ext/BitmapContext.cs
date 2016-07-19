
#region # using *.*

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
// ReSharper disable MemberCanBeInternal
// ReSharper disable CheckNamespace

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

    private readonly static Dictionary<WriteableBitmap, int> UpdateCountByBmp = new Dictionary<WriteableBitmap, int>();
    private readonly static Dictionary<WriteableBitmap, int[]> PixelCacheByBmp = new Dictionary<WriteableBitmap, int[]>();
    private readonly int length;
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

      // Ensure the bitmap is in the dictionary of mapped Instances
      if (!UpdateCountByBmp.ContainsKey(writeableBitmap))
      {
        // Set UpdateCount to 1 for this bitmap 
        UpdateCountByBmp.Add(writeableBitmap, 1);
        length = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight;
        pixels = new int[length];
        CopyPixels();
        PixelCacheByBmp.Add(writeableBitmap, pixels);
      }
      else
      {
        // For previously contextualized bitmaps increment the update count
        IncrementRefCount(writeableBitmap);
        pixels = PixelCacheByBmp[writeableBitmap];
        length = pixels.Length;
      }
    }

    private unsafe void CopyPixels()
    {
      var data = writeableBitmap.PixelBuffer.ToArray();
      fixed (byte* srcPtr = data)
      {
        fixed (int* dstPtr = pixels)
        {
#if DEBUG
          if (pixels.Length < length) throw new IndexOutOfRangeException("pixels.Length < length (" + pixels.Length + " < " + length + ")");
          if (data.Length < length * 4) throw new IndexOutOfRangeException("data.Length < length (" + data.Length + " < " + length + " * 4)");
#endif
          for (var i = 0; i < length; i++)
          {
            dstPtr[i] = (srcPtr[i * 4 + 3] << 24) | (srcPtr[i * 4 + 2] << 16) | (srcPtr[i * 4 + 1] << 8) | srcPtr[i * 4 + 0];
          }
        }
      }
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
    public unsafe void Dispose()
    {
      // Decrement the update count. If it hits zero
      if (DecrementRefCount(writeableBitmap) != 0) return;

      // Remove this bitmap from the update map 
      UpdateCountByBmp.Remove(writeableBitmap);
      PixelCacheByBmp.Remove(writeableBitmap);

      // Copy data back
      using (var stream = writeableBitmap.PixelBuffer.AsStream())
      {
        var buffer = new byte[4];
        fixed (int* srcPtr = pixels)
        {
          for (var i = 0; i < length; i++)
          {
            buffer[3] = (byte)((srcPtr[i] >> 24) & 0xff);
            buffer[2] = (byte)((srcPtr[i] >> 16) & 0xff);
            buffer[1] = (byte)((srcPtr[i] >> 8) & 0xff);
            buffer[0] = (byte)((srcPtr[i]) & 0xff);
            stream.Write(buffer, 0, 4);
          }
        }
      }
      writeableBitmap.Invalidate();
    }

    private static void IncrementRefCount(WriteableBitmap target)
    {
      UpdateCountByBmp[target]++;
    }

    private static int DecrementRefCount(WriteableBitmap target)
    {
      int current = UpdateCountByBmp[target];
      current--;
      UpdateCountByBmp[target] = current;
      return current;
    }
  }
}