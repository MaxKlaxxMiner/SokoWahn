#region # using *.*

using System;
using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SokoWahnCore.CoreTools
{
  public static class LinqExtensions
  {
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
