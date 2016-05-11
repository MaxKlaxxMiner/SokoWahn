#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SokoWahnCore
{
  public static partial class LinqToolsExtensions
  {
    /// <summary>
    /// vergleichbar mit ".Select().ToArray()", jedoch schneller
    /// </summary>
    /// <typeparam name="Tout">Ausgabe-Datentyp</typeparam>
    /// <typeparam name="Tin">Eingabe-Datentyp</typeparam>
    /// <param name="source">Enumerable mit den Quelldaten</param>
    /// <param name="selectMethod">Select-Methode zum umwandeln des Datensatzes</param>
    /// <returns>fertiges Array</returns>
    public static Tout[] SelectArray<Tout, Tin>(this IEnumerable<Tin> source, Func<Tin, Tout> selectMethod)
    {
      var sourceArray = (source as Tin[]) ?? source.ToArray();

      var outputArray = new Tout[sourceArray.Length];

      for (int i = 0; i < outputArray.Length; i++) outputArray[i] = selectMethod(sourceArray[i]);

      return outputArray;
    }
  }
}
