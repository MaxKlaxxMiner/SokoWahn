#region # using *.*

using System;
using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SokoWahnCore
{
  /// <summary>
  /// eigene Linq-Erweiterungen
  /// </summary>
  public static class LinqExtensions
  {
    /// <summary>
    /// fragt wie "Where" mehrere Felder ab, gibt aber statt den Datensätzen nur die entsprechenden Index-Positionen zurück
    /// </summary>
    /// <typeparam name="T">Typ des Datensatzes</typeparam>
    /// <param name="query">Enumerable mit den zu überprüfenden Elementen</param>
    /// <param name="compare">Abfrage-Methode</param>
    /// <returns>Enumerable mit den entsprechenden Index-Positionen</returns>
    public static IEnumerable<int> WhereGetIndices<T>(this IEnumerable<T> query, Func<T, bool> compare)
    {
      var array = query as T[];
      if (array != null)
      {
        for (int i = 0; i < array.Length; i++)
        {
          if (compare(array[i])) yield return i;
        }
        yield break;
      }

      var list = query as IList<T>;
      if (list != null)
      {
        int len = list.Count;
        for (int i = 0; i < len; i++)
        {
          if (compare(list[i])) yield return i;
        }
        yield break;
      }

      int index = 0;
      foreach (var val in query)
      {
        if (compare(val)) yield return index;
        index++;
      }
    }
  }
}
