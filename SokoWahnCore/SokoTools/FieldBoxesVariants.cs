
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Local

#endregion

namespace SokoWahnCore
{
  public static partial class SokoTools
  {
    /// <summary>
    /// berechnet alle Varianten, wie Kisten auf mehreren Felder verteilt liegen können (gleiches Array wird weiter benutzt)
    /// </summary>
    /// <param name="fieldCount">Anzahl der möglichen Felder (muss größergleich der Kisten-Anzahl sein)</param>
    /// <param name="boxesCount">Anzahl der Kisten, welche verteilt werden sollen (muss kleinergleich der Felder-Anzahl sein)</param>
    /// <returns>Enumerable der Varianten</returns>
    public static IEnumerable<int[]> FieldBoxesVariantsStatic(int fieldCount, int boxesCount)
    {
      int dif = fieldCount - boxesCount;
      int end = boxesCount - 1;

      var boxesVariant = new int[boxesCount];

      for (int box = 0; ; )
      {
        while (box < end) boxesVariant[box + 1] = boxesVariant[box++] + 1;

        yield return boxesVariant;

        while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;
      }
    }

    /// <summary>
    /// berechnet alle Varianten, wie Kisten auf mehreren Felder verteilt liegen können (neue Arrays werden erstellt)
    /// </summary>
    /// <param name="fieldCount">Anzahl der möglichen Felder (muss größergleich der Kisten-Anzahl sein)</param>
    /// <param name="boxesCount">Anzahl der Kisten, welche verteilt werden sollen (muss kleinergleich der Felder-Anzahl sein)</param>
    /// <returns>Enumerable der Varianten</returns>
    public static IEnumerable<int[]> FieldBoxesVariantsClone(int fieldCount, int boxesCount)
    {
      int dif = fieldCount - boxesCount;
      int end = boxesCount - 1;

      var boxesVariant = new int[boxesCount];

      for (int box = 0; ; )
      {
        while (box < end) boxesVariant[box + 1] = boxesVariant[box++] + 1;

        var tmp = new int[boxesVariant.Length];
        Buffer.BlockCopy(boxesVariant, 0, tmp, 0, boxesVariant.Length * sizeof(int));
        yield return tmp;

        while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;
      }
    }

    /// <summary>
    /// berechnet alle Varianten, wie Kisten auf mehreren Felder verteilt liegen können (gleiches Array wird weiter benutzt, inkl. Einzelkisten-Validierung)
    /// </summary>
    /// <param name="fieldCount">Anzahl der möglichen Felder (muss größergleich der Kisten-Anzahl sein)</param>
    /// <param name="boxesCount">Anzahl der Kisten, welche verteilt werden sollen (muss kleinergleich der Felder-Anzahl sein)</param>
    /// <param name="valid">Methode zum prüfen, ob eine Teil-Stellung gültig ist (int[] boxPosis, int boxIndex)</param>
    /// <returns>Enumerable der Varianten</returns>
    public static IEnumerable<int[]> FieldBoxesVariantsExtended(int fieldCount, int boxesCount, Func<int[], int, bool> valid)
    {
      int dif = fieldCount - boxesCount;
      int end = boxesCount - 1;

      var boxesVariant = new int[boxesCount];

      for (int box = 0; ; )
      {
        while (box < end)
        {
          boxesVariant[box + 1] = boxesVariant[box++] + 1;
          if (!valid(boxesVariant, box - 1))
          {
            box--;
            while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;
          }
        }

        if (valid(boxesVariant, box)) yield return boxesVariant;

        while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;
      }
    }
  }
}
