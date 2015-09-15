#region # using *.*

using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
// ReSharper disable CanBeReplacedWithTryCastAndCheckForNull

#endregion

namespace Sokosolver
{
  /// <summary>
  /// Interface zum berechnen eines Crc64-Schlüssels
  /// </summary>
  internal interface ICrc64
  {
    /// <summary>
    /// Methode zum berechnen eines Crc64-Schlüssels
    /// </summary>
    /// <param name="basis">Basiswert von vorherigen Berechnung (muss beim Start sein: Crc64.Start)</param>
    /// <returns>fertig berechneter Crc64-Schlüssel</returns>
    ulong GetCrc64(ulong basis);
  }

  /// <summary>
  /// Klasse zum berechnen von Crc64-Schlüsseln (FNV)
  /// </summary>
  internal static class Crc64
  {
    /// <summary>
    /// Crc64 Startwert
    /// </summary>
    internal const ulong Start = 0xcbf29ce484222325u;
    /// <summary>
    /// Crc64 Multiplikator
    /// </summary>
    private const ulong Mul = 0x100000001b3;

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static unsafe ulong Crc64Update(this ulong crc64, float wert)
    {
      return (crc64 ^ *(uint*)&wert) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static unsafe ulong Crc64Update(this ulong crc64, double wert)
    {
      ulong l = *(ulong*)&wert;
      return (((crc64 ^ (uint)l) * Mul) ^ (l >> 32)) * Mul;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static ulong Crc64Update(this ulong crc64, byte[] wert)
    {
      foreach (byte b in wert)
      {
        crc64 = (crc64 ^ b) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static ulong Crc64Update(this ulong crc64, char[] wert)
    {
      foreach (char w in wert)
      {
        crc64 = (crc64 ^ w) * Mul;
      }
      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static unsafe ulong Crc64Update(this ulong crc64, string wert)
    {
      ulong crc64B = crc64;
      int len = wert.Length;
      fixed (char* c = wert)
      {
        for (int i = 0; i < len; i++)
        {
          crc64B = (crc64B ^ c[i]) * Mul;
        }
      }
      return crc64B;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static ulong Crc64Update(this ulong crc64, StringBuilder wert)
    {
      for (int i = 0; i < wert.Length; i++)
      {
        crc64 = (crc64 ^ wert[i]) * Mul;
      }

      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenarray, welcher einberechnet werden soll</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static ulong Crc64Update(this ulong crc64, Array wert)
    {
      if (wert.Length == 0) return crc64;

      Func<ulong, object, ulong> funktion;
      if (crc64Dict.TryGetValue(wert.GetType().GetElementType(), out funktion))
      {
        for (int i = 0; i < wert.Length; i++)
        {
          crc64 = funktion(crc64, wert.GetValue(i));
        }
      }
      else
      {
        if (wert.GetValue(0) is ICrc64)
        {
          for (int i = 0; i < wert.Length; i++)
          {
            crc64 = ((ICrc64)wert.GetValue(i)).GetCrc64(crc64);
          }
        }
        else
        {
          for (int i = 0; i < wert.Length; i++)
          {
            crc64 = crc64.Crc64Update(wert.GetValue(i));
          }
        }
      }

      return crc64;
    }

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="wert">Datenwert, welcher das Interface ICrc64 unterstützt</param>
    /// <returns>neuer Crc64-Wert</returns>
    private static ulong Crc64Update(this ulong crc64, IEnumerable wert)
    {
      var t = wert.GetType();
      if (t.IsArray)
      {
        return crc64.Crc64Update((Array)wert);
      }
      if (t.IsGenericType)
      {
        var args = t.GetGenericArguments();
        if (args.Length == 1)
        {
          var t2 = args[0];
          Func<ulong, object, ulong> funktion;
          if (crc64Dict.TryGetValue(t2, out funktion))
          {
            foreach (var w in wert)
            {
              crc64 = funktion(crc64, w);
            }
          }
          else
          {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (t2 is ICrc64)
            {
              foreach (var w in wert)
              {
                crc64 = ((ICrc64)w).GetCrc64(crc64);
              }
            }
            else
            {
              foreach (var w in wert)
              {
                crc64 = crc64.Crc64Update(w);
              }
            }
          }
        }
        else
        {
          var enumerator = wert.GetEnumerator();
          if (enumerator.MoveNext())
          {
            var t2 = enumerator.Current.GetType().GetGenericTypeDefinition();
            if (t2 == typeof(KeyValuePair<,>))
            {
              //var id = (IDictionary)larf;
              //var k = id.Keys;
              //var v = id.Values;
              //var ke = k.GetEnumerator();
              //var ve = v.GetEnumerator();
              //for (; ; )
              //{
              //  if (!ke.MoveNext()) break;
              //  ve.MoveNext();
              //  Console.WriteLine(String.Format("{0} - {1}", (int)ke.Current, (string)ve.Current));
              //}

              var proKey = enumerator.Current.GetType().GetProperty("Key");
              var proVal = enumerator.Current.GetType().GetProperty("Value");
              do
              {
                crc64 = crc64.Crc64Update(proKey.GetValue(enumerator.Current, null));
                crc64 = crc64.Crc64Update(proVal.GetValue(enumerator.Current, null));
              } while (enumerator.MoveNext());
            }
            else
            {
              throw new Exception("Type-Error");
            }
          }
        }
      }
      else
      {
        foreach (var w in wert)
        {
          Func<ulong, object, ulong> funktion;
          if (crc64Dict.TryGetValue(w.GetType(), out funktion))
          {
            crc64 = funktion(crc64, w);
          }
          else
          {
            if (w is ICrc64)
            {
              crc64 = ((ICrc64)w).GetCrc64(crc64);
            }
            else
            {
              if (wert is Array)
              {
                crc64 = crc64.Crc64Update((Array)w);
              }
              else
              {
                throw new Exception("Unbekannter Typ: " + wert.GetType());
              }
            }
          }
        }
      }

      return crc64;
    }

    /// <summary>
    /// internes
    /// </summary>
    static readonly Dictionary<Type, Func<ulong, object, ulong>> crc64Dict = new Dictionary<Type, Func<ulong, object, ulong>>
    {
      { typeof(byte), (crc64, wert) => (crc64 ^ (ulong)(byte)wert) * Mul },
      { typeof(sbyte), (crc64, wert) => (crc64 ^ (ulong)(byte)(sbyte)wert) * Mul },
      { typeof(ushort), (crc64, wert) => (crc64 ^ (ulong)(ushort)wert) * Mul },
      { typeof(short), (crc64, wert) => (crc64 ^ (ulong)(ushort)(short)wert) * Mul },
      { typeof(uint), (crc64, wert) => (crc64 ^ (ulong)(uint)wert) * Mul },
      { typeof(int), (crc64, wert) => (crc64 ^ (ulong)(uint)(int)wert) * Mul },
      { typeof(ulong), (crc64, wert) => (((crc64 ^ (ulong)(uint)(ulong)wert) * Mul) ^ ((ulong)wert >> 32)) * Mul },
      { typeof(long), (crc64, wert) => (((crc64 ^ (ulong)(uint)(long)wert) * Mul) ^ ((ulong)(long)wert >> 32)) * Mul },
      { typeof(byte[]), (crc64, wert) => crc64.Crc64Update((byte[])wert) },
      { typeof(char[]), (crc64, wert) => crc64.Crc64Update((char[])wert) },
      { typeof(string), (crc64, wert) => crc64.Crc64Update((string)wert) },
      { typeof(float), (crc64, wert) => crc64.Crc64Update((float)wert) },
      { typeof(double), (crc64, wert) => crc64.Crc64Update((double)wert) },
      { typeof(StringBuilder), (crc64, wert) => crc64.Crc64Update((StringBuilder)wert) }
    };

    /// <summary>
    /// aktualisiert die Prüfsumme
    /// </summary>
    /// <param name="crc64">ursprünglicher Crc64-Wert</param>
    /// <param name="werte">Datenwerte, welche einberechnet werden sollen</param>
    /// <returns>neuer Crc64-Wert</returns>
    public static ulong Crc64Update(this ulong crc64, params object[] werte)
    {
      foreach (var wert in werte)
      {
        Func<ulong, object, ulong> funktion;
        if (crc64Dict.TryGetValue(wert.GetType(), out funktion))
        {
          crc64 = funktion(crc64, wert);
        }
        else
        {
          if (wert is ICrc64)
          {
            crc64 = ((ICrc64)wert).GetCrc64(crc64);
          }
          else
          {
            if (wert is Array)
            {
              crc64 = crc64.Crc64Update((Array)wert);
            }
            else
            {
              if (wert is IEnumerable)
              {
                crc64 = crc64.Crc64Update((IEnumerable)wert);
              }
              else
              {
                if (wert.GetType().GetGenericTypeDefinition().Name.StartsWith("Tuple`", StringComparison.Ordinal))
                {
                  foreach (var pro in wert.GetType().GetProperties())
                  {
                    crc64 = crc64.Crc64Update(pro.GetValue(wert, null));
                  }
                }
                else
                {
                  throw new Exception("Unbekannter Typ: " + wert.GetType());
                }
              }
            }
          }
        }
      }

      return crc64;
    }
  }
}
