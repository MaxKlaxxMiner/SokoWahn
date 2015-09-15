using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sokosolver
{
  public static class ArrayTools
  {
    /// <summary>
    /// wandelt das gesamte Struct-Array in ein Byte-Array um
    /// </summary>
    /// <param name="array">Array, welches umgewandelt werden soll</param>
    /// <returns>fertig umgewandeltes Byte-Array</returns>
    public static byte[] ToByteArray<T>(this T[] array) where T : struct
    {
      return array.ToByteArray(0, array.Length);
    }

    /// <summary>
    /// wandelt die Daten des Struct-Arrays in ein Byte-Array um
    /// </summary>
    /// <param name="array">Array, deren Daten umgewandelt werden soll</param>
    /// <param name="start">Start-Position im Quell-Array</param>
    /// <param name="anzahl">Anzahl der zu kopierenden Einträge im Array</param>
    /// <returns>fertig umgewandeltes Byte-Array</returns>
    public static byte[] ToByteArray<T>(this T[] array, int start, int anzahl) where T : struct
    {
      int len = Marshal.SizeOf(typeof(T));
      var ausgabe = new byte[anzahl * len];
      array.ToByteArray(start, anzahl, ausgabe, 0);
      return ausgabe;
    }

    /// <summary>
    /// wandelt die Daten des Struct-Arrays in ein Byte-Array um
    /// </summary>
    /// <param name="array">Array, deren Daten umgewandelt werden soll</param>
    /// <param name="start">Start-Position im Quell-Array</param>
    /// <param name="anzahl">Anzahl der zu kopierenden Einträge im Array</param>
    /// <param name="zielData">Byte-Array, wohin die Daten gespeichert werden sollen</param>
    /// <param name="zielStart">Offset im Ziel-Array</param>
    public static unsafe void ToByteArray<T>(this T[] array, int start, int anzahl, byte[] zielData, int zielStart) where T : struct
    {
      int len = Marshal.SizeOf(typeof(T));
      int bytes = anzahl * len;
      if (start < 0 || anzahl < 0 || start + anzahl > array.Length || zielStart < 0 || zielStart + bytes > zielData.Length) throw new IndexOutOfRangeException();
      if (bytes == 0) return;

      var ghandle = GCHandle.Alloc(array, GCHandleType.Pinned);
      var pArray = (byte*)ghandle.AddrOfPinnedObject();

      fixed (byte* pZielData = &zielData[zielStart])
      {
        CopyBytes(&pArray[start * len], pZielData, bytes);
      }

      ghandle.Free();
    }

    /// <summary>
    /// kopiert Bytes von einer Speicheradresse auf eine andere Speicheradresse
    /// </summary>
    /// <param name="quelle">Adresse auf die Quelldaten</param>
    /// <param name="ziel">Adresse auf die Zieldaten</param>
    /// <param name="bytes">Anzahl der Bytes, welche kopiert werden sollen</param>
    public static unsafe void CopyBytes(byte* quelle, byte* ziel, int bytes)
    {
      int pos;

      // --- schnellere Kopier-Variante verwenden (je nach Modus) ---
      if (Environment.Is64BitProcess)
      {
        // --- 64-Bit Modus (als longs kopieren) ---
        int bis = bytes >> 3;
        var pQuelle = (long*)quelle;
        var pZiel = (long*)ziel;
        for (int i = 0; i < bis; i++)
        {
          pZiel[i] = pQuelle[i];
        }
        pos = bis << 3;
      }
      else
      {
        // --- 32-Bit Modus (als ints kopieren) ---
        int bis = bytes >> 2;
        var pQuelle = (int*)quelle;
        var pZiel = (int*)ziel;
        for (int i = 0; i < bis; i++)
        {
          pZiel[i] = pQuelle[i];
        }
        pos = bis << 2;
      }

      // --- die restlichen Bytes kopieren ---
      for (; pos < bytes; pos++)
      {
        ziel[pos] = quelle[pos];
      }
    }

    /// <summary>
    /// gibt Positions-Bytes zurück mit den zu packenden Daten
    /// </summary>
    /// <typeparam name="T">Typ eines Datensatzes</typeparam>
    /// <param name="data">Datensatz, welcher sich vergleichen lässt</param>
    /// <returns>Pack-Bytes</returns>
    public static byte[] RlePackerStats<T>(this T[] data) where T : IEquatable<T>
    {
      int pos = 0;
      int max = data.Length;
      int rle = 0;
      int rleSkip = typeof(T).IsClass || Marshal.SizeOf(typeof(T)) > 1 ? 1 : 4;
      var buf = new byte[16 + 16];
      int bufPos = 0;

      while (pos < max)
      {
        int keinPack = 0;
        while (pos < max)
        {
          rle = 1;
          int suchPos = pos;
          var suchWert = data[suchPos++];
          while (suchPos < max && data[suchPos++].Equals(suchWert)) rle++;
          if (rle > rleSkip) break;
          pos += rle;
          keinPack += rle;
        }

        if (keinPack < 255)
        {
          buf[bufPos++] = (byte)keinPack;
        }
        else
        {
          buf[bufPos++] = 0xff;
          if (keinPack < 65535)
          {
            buf[bufPos++] = (byte)keinPack;
            buf[bufPos++] = (byte)(keinPack >> 8);
          }
          else
          {
            buf[bufPos++] = 0xff;
            buf[bufPos++] = 0xff;
            buf[bufPos++] = (byte)keinPack;
            buf[bufPos++] = (byte)(keinPack >> 8);
            buf[bufPos++] = (byte)(keinPack >> 16);
            buf[bufPos++] = (byte)(keinPack >> 24);
          }
        }

        if (rle > rleSkip)
        {
          if (rle < 255)
          {
            buf[bufPos++] = (byte)rle;
          }
          else
          {
            buf[bufPos++] = 0xff;
            if (rle < 65535)
            {
              buf[bufPos++] = (byte)rle;
              buf[bufPos++] = (byte)(rle >> 8);
            }
            else
            {
              buf[bufPos++] = 0xff;
              buf[bufPos++] = 0xff;
              buf[bufPos++] = (byte)rle;
              buf[bufPos++] = (byte)(rle >> 8);
              buf[bufPos++] = (byte)(rle >> 16);
              buf[bufPos++] = (byte)(rle >> 24);
            }
          }
          pos += rle;
        }

        if (bufPos > buf.Length - 16) Array.Resize(ref buf, (buf.Length - 16) * 2 + 16);
      }

      Array.Resize(ref buf, bufPos);
      return buf;
    }

    /// <summary>
    /// gibt die eigentlich gepackten Daten zurück
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="packerBytes"></param>
    /// <returns></returns>
    public static IEnumerable<T> RlePackerFilter<T>(this T[] data, byte[] packerBytes) where T : IEquatable<T>
    {
      int lesePos = 0;
      int max = packerBytes.Length;
      int dataPos = 0;
      while (lesePos < max)
      {
        int gro = packerBytes[lesePos++];
        if (gro == 255)
        {
          gro = packerBytes[lesePos++];
          gro += packerBytes[lesePos++] << 8;
          if (gro == 65535)
          {
            gro = packerBytes[lesePos++];
            gro += packerBytes[lesePos++] << 8;
            gro += packerBytes[lesePos++] << 16;
            gro += packerBytes[lesePos++] << 24;
          }
        }
        for (int i = 0; i < gro; i++)
        {
          yield return data[dataPos++];
        }
        if (lesePos == max) yield break;
        gro = packerBytes[lesePos++];
        if (gro == 255)
        {
          gro = packerBytes[lesePos++];
          gro += packerBytes[lesePos++] << 8;
          if (gro == 65535)
          {
            gro = packerBytes[lesePos++];
            gro += packerBytes[lesePos++] << 8;
            gro += packerBytes[lesePos++] << 16;
            gro += packerBytes[lesePos++] << 24;
          }
        }
        yield return data[dataPos];
        dataPos += gro;
      }
    }

    /// <summary>
    /// packt ein Daten-Array mit RLE gibt ein fertiges Byte-Array zurück
    /// </summary>
    /// <typeparam name="T">Typ der Struktur, welche gepackt werden soll</typeparam>
    /// <param name="data">Daten-Array, welches gepackt werden soll</param>
    /// <returns>fertig gepackte Daten als Byte-Array</returns>
    public static byte[] RlePack<T>(this T[] data) where T : struct, IEquatable<T>
    {
      var packBytes = data.RlePackerStats();
      var packData = data.RlePackerFilter(packBytes).ToArray().ToByteArray();
      int len = packBytes.Length;
      var output = new byte[8];
      int o = 0;
      if (len < 255)
      {
        output[o++] = (byte)len;
      }
      else
      {
        output[o++] = 0xff;
        if (len < 65535)
        {
          output[o++] = (byte)len;
          output[o++] = (byte)(len >> 8);
        }
        else
        {
          output[o++] = 0xff;
          output[o++] = 0xff;
          output[o++] = (byte)len;
          output[o++] = (byte)(len >> 8);
          output[o++] = (byte)(len >> 16);
          output[o++] = (byte)(len >> 24);
        }
      }
      Array.Resize(ref output, o + packBytes.Length + packData.Length);
      // ReSharper disable once ForCanBeConvertedToForeach
      for (int i = 0; i < packBytes.Length; i++) output[o++] = packBytes[i];
      // ReSharper disable once ForCanBeConvertedToForeach
      for (int i = 0; i < packData.Length; i++) output[o++] = packData[i];
      return output;
    }

    /// <summary>
    /// wandelt ein Byte-Array in ein Struct-Array um
    /// </summary>
    /// <param name="array">Bytes, welche umgewandelt werden sollen</param>
    /// <param name="start">Offset im Byte-Array</param>
    /// <param name="bytes">Anzahl der Bytes, welche umgewandelt werden sollen (muss durch dir Länge des Structs teilbar sein)</param>
    /// <returns>fertig erstelltes Struct-Array</returns>
    public static T[] ToStructArray<T>(this byte[] array, int start, int bytes) where T : struct
    {
      var ausgabe = new T[bytes / Marshal.SizeOf(typeof(T))];

      array.ToStructArray(start, bytes, ausgabe, 0);

      return ausgabe;
    }

    /// <summary>
    /// wandelt ein Byte-Array in ein Struct-Array um
    /// </summary>
    /// <param name="array">Bytes, welche umgewandelt werden sollen</param>
    /// <param name="start">Offset im Byte-Array</param>
    /// <param name="bytes">Anzahl der Bytes, welche umgewandelt werden sollen (muss durch die Länge des Structs teilbar sein)</param>
    /// <param name="zielData">Ziel-Array</param>
    /// <param name="zielStart">Startposition im Ziel-Array</param>
    /// <returns>Anzahl der kopierten Einträge</returns>
    public static unsafe int ToStructArray<T>(this byte[] array, int start, int bytes, T[] zielData, int zielStart) where T : struct
    {
      int len = Marshal.SizeOf(typeof(T));
      int anzahl = bytes / len;
      if (start < 0 || bytes < 0 || (bytes % len) != 0 || start + bytes > array.Length || zielStart < 0 || zielStart + anzahl > zielData.Length) throw new IndexOutOfRangeException();

      var ghandle = GCHandle.Alloc(zielData, GCHandleType.Pinned);
      var pZielData = (byte*)ghandle.AddrOfPinnedObject();

      fixed (byte* pArray = &array[start])
      {
        CopyBytes(pArray, &pZielData[zielStart * len], bytes);
      }

      ghandle.Free();

      return anzahl;
    }

    /// <summary>
    /// wandelt ein Struct-Array in ein anderes Struct-Array um, die Größen der Structs müssen identisch sein oder um ein vielfaches der Array-Länge entsprechen (z.B. short[12] -> uint[6] -> double[3] -> byte[24] usw...)
    /// </summary>
    /// <typeparam name="TQuelle">Struktur der Quell-Daten</typeparam>
    /// <typeparam name="TZiel">Struktur der Ziel-Daten</typeparam>
    /// <param name="array">Array mit den Quelldaten</param>
    /// <returns>fertig umgewandeltes Array</returns>
    public static TZiel[] ToStructArray<TQuelle, TZiel>(this TQuelle[] array)
      where TQuelle : struct
      where TZiel : struct
    {
      return array.ToStructArray<TQuelle, TZiel>(0, array.Length);
    }

    /// <summary>
    /// wandelt ein Struct-Array in ein anderes Struct-Array um, die Größen der Structs müssen identisch sein oder um ein vielfaches der Array-Länge entsprechen (z.B. short[12] -> uint[6] -> double[3] -> byte[24] usw...)
    /// </summary>
    /// <typeparam name="TQuelle">Struktur der Quell-Daten</typeparam>
    /// <typeparam name="TZiel">Struktur der Ziel-Daten</typeparam>
    /// <param name="array">Array mit den Quelldaten</param>
    /// <param name="start">Offset im Quell-Array</param>
    /// <param name="anzahl">Anzahl der Quell-Einträge, welche umgewandelt werden sollen</param>
    /// <returns>fertig umgewandeltes Array</returns>
    public static TZiel[] ToStructArray<TQuelle, TZiel>(this TQuelle[] array, int start, int anzahl)
      where TQuelle : struct
      where TZiel : struct
    {
      int lenQuelle = Marshal.SizeOf(typeof(TQuelle));
      int lenZiel = Marshal.SizeOf(typeof(TZiel));

      var ausgabe = new TZiel[anzahl * lenQuelle / lenZiel];

      array.ToStructArray(start, anzahl, ausgabe, 0);

      return ausgabe;
    }

    /// <summary>
    /// wandelt ein Struct-Array in ein anderes Struct-Array um, die Größen der Structs müssen identisch sein oder um ein vielfaches der Array-Länge entsprechen (z.B. short[12] -> uint[6] -> double[3] -> byte[24] usw...)
    /// </summary>
    /// <typeparam name="TQuelle">Struktur der Quell-Daten</typeparam>
    /// <typeparam name="TZiel">Struktur der Ziel-Daten</typeparam>
    /// <param name="quellArray">Array mit den Quelldaten</param>
    /// <param name="start">Offset im Quell-Array</param>
    /// <param name="anzahl">Anzahl der Quell-Einträge, welche umgewandelt werden sollen</param>
    /// <param name="zielArray">Array mit den Zieldaten</param>
    /// <param name="startZiel">Offset im Ziel-Array</param>
    /// <returns>Anzahl der kopierten Einträge im Ziel-Array</returns>
    public static unsafe int ToStructArray<TQuelle, TZiel>(this TQuelle[] quellArray, int start, int anzahl, TZiel[] zielArray, int startZiel)
    {
      int lenQuelle = Marshal.SizeOf(typeof(TQuelle));
      int lenZiel = Marshal.SizeOf(typeof(TZiel));
      int bytes = anzahl * lenQuelle;
      int anzahlZiel = bytes / lenZiel;

      if (start < 0 || anzahl < 0 || start + anzahl > quellArray.Length || startZiel < 0 || startZiel + anzahlZiel > zielArray.Length || bytes % lenZiel > 0) throw new IndexOutOfRangeException();

      var ghandleZiel = GCHandle.Alloc(zielArray, GCHandleType.Pinned);
      var ghandleQuelle = GCHandle.Alloc(quellArray, GCHandleType.Pinned);

      var pZielArray = (byte*)ghandleZiel.AddrOfPinnedObject();
      var pQuellArray = (byte*)ghandleQuelle.AddrOfPinnedObject();

      CopyBytes(&pQuellArray[start * lenQuelle], &pZielArray[startZiel * lenZiel], bytes);

      ghandleZiel.Free();
      ghandleQuelle.Free();

      return anzahlZiel;
    }

    /// <summary>
    /// entpackt ein mit Rle gepacktes Struct-Array
    /// </summary>
    /// <param name="packData">gepackte Daten</param>
    /// <returns>Enumerable mit allen entpackten Structs</returns>
    public static IEnumerable<T> RleUnpack<T>(this byte[] packData) where T : struct, IEquatable<T>
    {
      int pos = 0;
      int gro = packData[pos++];
      if (gro == 255)
      {
        gro = packData[pos++];
        gro += packData[pos++] << 8;
        if (gro == 65535)
        {
          gro = packData[pos++];
          gro += packData[pos++] << 8;
          gro += packData[pos++] << 16;
          gro += packData[pos++] << 24;
        }
      }
      int packBytesEnde = pos + gro;
      int structSize = Marshal.SizeOf(typeof(T));
      var structData = packData.ToStructArray<T>(packBytesEnde, (packData.Length - packBytesEnde) / structSize * structSize);
      int structPos = 0;

      while (pos < packBytesEnde)
      {
        gro = packData[pos++];
        if (gro == 255)
        {
          gro = packData[pos++];
          gro += packData[pos++] << 8;
          if (gro == 65535)
          {
            gro = packData[pos++];
            gro += packData[pos++] << 8;
            gro += packData[pos++] << 16;
            gro += packData[pos++] << 24;
          }
        }
        while (gro-- > 0)
        {
          yield return structData[structPos++];
        }
        if (pos == packBytesEnde) yield break;
        gro = packData[pos++];
        if (gro == 255)
        {
          gro = packData[pos++];
          gro += packData[pos++] << 8;
          if (gro == 65535)
          {
            gro = packData[pos++];
            gro += packData[pos++] << 8;
            gro += packData[pos++] << 16;
            gro += packData[pos++] << 24;
          }
        }

        if (gro <= 0) continue;

        var str = structData[structPos++];
        while (gro-- > 0)
        {
          yield return str;
        }
      }
    }
  }

}
