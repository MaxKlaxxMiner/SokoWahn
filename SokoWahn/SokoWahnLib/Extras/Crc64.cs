// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

/* * * * * * * * * * * * *
 *  Quelle: ngMax.Lite   *
 * * * * * * * * * * * * */

// ReSharper disable UnusedMethodReturnValue.Global
namespace SokoWahnLib
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
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, byte value)
    {
      return (crc64 ^ value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, sbyte value)
    {
      return (crc64 ^ (byte)value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, ushort value)
    {
      return (crc64 ^ value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, short value)
    {
      return (crc64 ^ (ushort)value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, uint value)
    {
      return (crc64 ^ value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, int value)
    {
      return (crc64 ^ (uint)value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, ulong value)
    {
      return (((crc64 ^ (uint)value) * Mul) ^ (value >> 32)) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, long value)
    {
      return (((crc64 ^ (uint)(ulong)value) * Mul) ^ ((ulong)value >> 32)) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static unsafe ulong Crc64Update(this ulong crc64, float value)
    {
      return (crc64 ^ *(uint*)&value) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static unsafe ulong Crc64Update(this ulong crc64, double value)
    {
      ulong l = *(ulong*)&value;
      return (((crc64 ^ (uint)l) * Mul) ^ (l >> 32)) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, byte[] value)
    {
      for (int i = 0; i < value.Length; i++)
      {
        crc64 = (crc64 ^ value[i]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="length">Länge der Daten</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, byte[] value, int offset, int length)
    {
      for (int i = 0; i < length; i++)
      {
        crc64 = (crc64 ^ value[offset++]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, char[] value)
    {
      for (int i = 0; i < value.Length; i++)
      {
        crc64 = (crc64 ^ value[i]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="length">Länge der Daten</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, char[] value, int offset, int length)
    {
      for (int i = 0; i < length; i++)
      {
        crc64 = (crc64 ^ value[offset++]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, uint[] value)
    {
      for (int i = 0; i < value.Length; i++)
      {
        crc64 = (crc64 ^ value[i]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="length">Länge der Daten</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, uint[] value, int offset, int length)
    {
      for (int i = 0; i < length; i++)
      {
        crc64 = (crc64 ^ value[offset++]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, int[] value)
    {
      for (int i = 0; i < value.Length; i++)
      {
        crc64 = (crc64 ^ (uint)value[i]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="length">Länge der Daten</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, int[] value, int offset, int length)
    {
      for (int i = 0; i < length; i++)
      {
        crc64 = (crc64 ^ (uint)value[offset++]) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="value">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static unsafe ulong Crc64Update(this ulong crc64, string value)
    {
      ulong tmp = crc64;
      int len = value.Length;
      fixed (char* valueP = value)
      {
        for (int i = 0; i < len; i++)
        {
          tmp = (tmp ^ valueP[i]) * Mul;
        }
      }
      return tmp;
    }

    /// <summary>
    /// berechnet eine CRC-Prüfsumme eines ulong-Arrays
    /// </summary>
    /// <param name="value">Array, welches gelesen werden soll</param>
    /// <returns>berechnete Prüfsumme</returns>
    public static ulong Get(ulong[] value)
    {
      ulong crc = Start;
      foreach (ulong v in value)
      {
        crc = ((crc ^ (uint)v) * Mul ^ v >> 32) * Mul;
      }
      return crc;
    }
  }
}
