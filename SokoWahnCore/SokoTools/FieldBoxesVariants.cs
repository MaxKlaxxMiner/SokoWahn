
#region # using *.*

using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Local

#endregion

namespace SokoWahnCore
{
  public static partial class SokoTools
  {
    /// <summary>
    /// berechnet alle Varianten, wie Kisten auf mehreren Felder verteilt liegen können
    /// </summary>
    /// <param name="fieldCount">Anzahl der möglichen Felder (muss größergleich der Kisten-Anzahl sein)</param>
    /// <param name="boxesCount">Anzahl der Kisten, welche verteilt werden sollen (muss kleinergleich der Felder-Anzahl sein)</param>
    /// <param name="arrayCopies">gibt an, ob die Antwort-Varianten eigene Arrays sein sollen (sonst wird immer das gleiche Array benutzt)</param>
    /// <returns>Enumerable der Varianten</returns>
    public static IEnumerable<int[]> FieldBoxesVariants(int fieldCount, int boxesCount, bool arrayCopies)
    {
      int dif = fieldCount - boxesCount;
      int end = boxesCount - 1;

      var boxesVariant = new int[boxesCount];

      for (int box = 0; ; )
      {
        while (box < end) boxesVariant[box + 1] = boxesVariant[box++] + 1;

        yield return arrayCopies ? boxesVariant.ToArray() : boxesVariant;

        while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;
      }
    }
  }
}
