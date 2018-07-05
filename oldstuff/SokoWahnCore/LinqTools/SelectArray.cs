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
    /// <typeparam name="TOut">Ausgabe-Datentyp</typeparam>
    /// <typeparam name="TIn">Eingabe-Datentyp</typeparam>
    /// <param name="source">Enumerable mit den Quelldaten</param>
    /// <param name="selectMethod">Select-Methode zum umwandeln des Datensatzes</param>
    /// <returns>fertiges Array</returns>
    public static TOut[] SelectArray<TOut, TIn>(this IEnumerable<TIn> source, Func<TIn, TOut> selectMethod)
    {
      var sourceArray = (source as TIn[]) ?? source.ToArray();

      var outputArray = new TOut[sourceArray.Length];

      for (int i = 0; i < outputArray.Length; i++) outputArray[i] = selectMethod(sourceArray[i]);

      return outputArray;
    }
  }
}
