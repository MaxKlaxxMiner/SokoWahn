
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SokoWahnCore.CoreTools
{
  /// <summary>
  /// Klasse zum berechnen von Crc64-Schlüsseln (FNV)
  /// </summary>
  public static class Crc64
  {
    /// <summary>
    /// Crc64 Startwert
    /// </summary>
    public const ulong Start = 0xcbf29ce484222325u;
    /// <summary>
    /// Crc64 Multiplikator
    /// </summary>
    public const ulong Mul = 0x100000001b3;

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Crc64Update(this ulong crc64, ushort wert)
    {
      return (crc64 ^ wert) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="buffer">Buffer mit den zu berechnenden Datenwerten</param>
    /// <param name="ofs">Anfangsposition im Buffer</param>
    /// <param name="len">Anzahl der zu berechnenden Datenwerte</param>
    /// <returns>neuer Crc64-Wert</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Crc64Update(this ulong crc64, ushort[] buffer, int ofs, int len)
    {
      crc64 ^= buffer[ofs];

      for (int i = 1; i < len; i++)
      {
        crc64 = crc64 * Mul ^ buffer[i + ofs];
      }

      return crc64 * Mul;
    }

  }
}
