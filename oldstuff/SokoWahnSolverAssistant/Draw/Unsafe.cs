
#region # using *.*

using System;
using System.Runtime.InteropServices;

#endregion

namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Klasse mit Hilfmethoden zum Arbeiten mit Unsafe-Code
  /// </summary>
  public static unsafe class Unsafe
  {
    /// <summary>
    /// kopiert einen bestimmten Speicherbereich
    /// </summary>
    /// <param name="dest">Zieladresse</param>
    /// <param name="src">Quelladresse</param>
    /// <param name="copyBytes">Anzahl der Bytes, welche kopiert werden sollen</param>
    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, int copyBytes);

    /// <summary>
    /// füllt eine Zeile aus
    /// </summary>
    /// <param name="ofs">Startposition</param>
    /// <param name="count">Länge der Zeile</param>
    /// <param name="color">zusetzende Füllfarbe</param>
    public static void FillLine(uint* ofs, int count, uint color)
    {
      for (int i = 0; i < count; i++) ofs[i] = color;
    }

    /// <summary>
    /// kopiert eine Zeile
    /// </summary>
    /// <param name="dst">Zieladresse</param>
    /// <param name="src">Quelladresse</param>
    /// <param name="count">Länge der Zeile</param>
    public static void CopyLine(uint* dst, uint* src, int count)
    {
      for (int i = 0; i < count; i++) dst[i] = src[i];
    }

    static uint BlendAlpha(uint colora, uint colorb, uint alpha)
    {
      uint rb1 = ((0x100 - alpha) * (colora & 0xFF00FF)) >> 8;
      uint rb2 = (alpha * (colorb & 0xFF00FF)) >> 8;
      uint g1 = ((0x100 - alpha) * (colora & 0x00FF00)) >> 8;
      uint g2 = (alpha * (colorb & 0x00FF00)) >> 8;
      return ((rb1 | rb2) & 0xFF00FF) + ((g1 | g2) & 0x00FF00);
    }

    /// <summary>
    /// kopiert eine Zeile mit Alpha-Kanal
    /// </summary>
    /// <param name="dst">Zieladresse</param>
    /// <param name="src">Quelladresse</param>
    /// <param name="count">Länge der Zeile</param>
    public static void CopyLineAlpha(uint* dst, uint* src, int count)
    {
      for (int i = 0; i < count; i++)
      {
        dst[i] = BlendAlpha(dst[i], src[i], src[i] >> 24) | 0xff000000;
      }
    }
  }
}
