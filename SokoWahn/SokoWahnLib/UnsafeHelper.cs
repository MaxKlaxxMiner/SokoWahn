#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable RedundantUnsafeContext
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse mit Hilfsmethoden zum arbeiten mit Zeigern und direkten Speicherbereichen
  /// </summary>
  public static unsafe class UnsafeHelper
  {
    /// <summary>
    /// schnelle Methode um eine neue leere Zeichenkette zu erstellen
    /// </summary>
    public static readonly Func<int, string> FastAllocateString = GetFastAllocateString();

    /// <summary>
    /// gibt die Methode "string.FastAllocateString" zurück
    /// </summary>
    /// <returns>Delegate auf die Methode</returns>
    static Func<int, string> GetFastAllocateString()
    {
      try
      {
        return (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), typeof(string).GetMethod("FastAllocateString", BindingFlags.NonPublic | BindingFlags.Static));
      }
      catch
      {
        return count => new string('\0', count); // Fallback 
      }
    }

    #region # public static unsafe void CopyBytes(byte* quelle, byte* ziel, int bytes) // kopiert Bytes von einer Speicheradresse auf eine andere Speicheradresse
    /// <summary>
    /// gibt an, ob es sich um einen 64-Bit Prozess handelt
    /// </summary>
    public static readonly bool Is64BitProcess = Environment.Is64BitProcess;

    /// <summary>
    /// kopiert ein Bild vollständig in ein anderes Bild (Größe beider Bilder muss übereinstimmen)
    /// </summary>
    /// <param name="src">Quell-Bild, welches verwendet werden soll</param>
    /// <param name="dst">Ziel-Bild, wohin gezeichnet werden soll</param>
    public static void CopyBitmap(Bitmap src, Bitmap dst)
    {
      if (src == null) throw new ArgumentNullException("src");
      if (dst == null) throw new ArgumentNullException("dst");
      if (src.PixelFormat != dst.PixelFormat) throw new ArgumentException("src.PixelFormat != dst.PixelFormat");
      if (src.Size != dst.Size) throw new NotSupportedException("src.Size != dst.Size");

      var bdSrc = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, src.PixelFormat);
      var bdDst = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height), ImageLockMode.WriteOnly, dst.PixelFormat);

      CopyBytes(bdSrc.Scan0, bdDst.Scan0, bdDst.Stride * dst.Height);

      dst.UnlockBits(bdDst);
      src.UnlockBits(bdSrc);
    }

    /// <summary>
    /// kopiert Bytes von einer Speicheradresse auf eine andere Speicheradresse
    /// </summary>
    /// <param name="src">Adresse auf die Quelldaten</param>
    /// <param name="dst">Adresse auf die Zieldaten</param>
    /// <param name="bytes">Anzahl der Bytes, welche kopiert werden sollen</param>
    public static void CopyBytes(IntPtr src, IntPtr dst, int bytes)
    {
      CopyBytes((byte*)src, (byte*)dst, bytes);
    }

    /// <summary>
    /// kopiert Bytes von einer Speicheradresse auf eine andere Speicheradresse
    /// </summary>
    /// <param name="src">Adresse auf die Quelldaten</param>
    /// <param name="dst">Adresse auf die Zieldaten</param>
    /// <param name="bytes">Anzahl der Bytes, welche kopiert werden sollen</param>
    public static void CopyBytes(byte* src, byte* dst, int bytes)
    {
      int pos;

      // --- schnellere Kopier-Variante verwenden (je nach Modus) ---
      if (Is64BitProcess) // --- Info: diese If-Abfrage wird vom Compiler weg optimiert ---
      {
        // --- 64-Bit Modus (als longs kopieren) ---
        int bis = bytes >> 3;
        var pSrc = (long*)src;
        var pDst = (long*)dst;
        for (int i = 0; i < bis; i++)
        {
          pDst[i] = pSrc[i];
        }
        pos = bis << 3;
      }
      else
      {
        // --- 32-Bit Modus (als ints kopieren) ---
        int bis = bytes >> 2;
        var pSrc = (int*)src;
        var pDst = (int*)dst;
        for (int i = 0; i < bis; i++)
        {
          pDst[i] = pSrc[i];
        }
        pos = bis << 2;
      }

      // --- die restlichen Bytes kopieren ---
      for (; pos < bytes; pos++)
      {
        dst[pos] = src[pos];
      }
    }
    #endregion
  }
}
