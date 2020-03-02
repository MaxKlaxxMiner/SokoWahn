// ReSharper disable UnusedMember.Global
// ReSharper disable TailRecursiveCall

/* * * * * * * * * * * * *
 *  Quelle: ngMax.Lite   *
 * * * * * * * * * * * * */

using System;
using System.Collections.Generic;

namespace SokoWahnLib
{
  public static class Sort
  {
    /// <summary>
    /// Kernmethode QuickSort für den Typ UInt64
    /// </summary>
    /// <param name="values">Zeiger auf die Liste mit UInt64-Werten</param>
    /// <param name="start">erster Wert in der Liste</param>
    /// <param name="end">letzter Wert in der Liste (zeigt direkt auf den Datensatz)</param>
    static unsafe void QuickSort(ulong* values, int start, int end)
    {
      if (start + 32 > end) // Insertsort bevorzugen?
      {
        var valS = &values[start];
        var valE = &values[end];
        for (var valP = valS + 1; valP <= valE; valP++)
        {
          var valJ = valP;
          ulong tmp = *valJ;
          for (; valJ > valS && tmp < valJ[-1]; valJ--) *valJ = valJ[-1];
          *valJ = tmp;
        }
      }
      else
      {
        int center = (start + end) >> 1;
        ulong tmp;
        if (values[center] < values[start])
        {
          tmp = values[start]; values[start] = values[center]; values[center] = tmp;
        }
        if (values[end] < values[start])
        {
          tmp = values[start]; values[start] = values[end]; values[end] = tmp;
        }
        if (values[end] < values[center])
        {
          tmp = values[center]; values[center] = values[end]; values[end] = tmp;
        }
        tmp = values[center]; values[center] = values[end - 1]; values[end - 1] = tmp;
        ulong pivot = values[end - 1];
        int i, j;
        for (i = start, j = end - 1; ; )
        {
          while (values[++i] < pivot) { }
          while (pivot < values[--j]) { }
          if (i >= j) break;
          tmp = values[i]; values[i] = values[j]; values[j] = tmp;
        }
        tmp = values[i]; values[i] = values[end - 1]; values[end - 1] = tmp;
        QuickSort(values, start, i - 1);
        QuickSort(values, i + 1, end);
      }
    }

    /// <summary>
    /// sortiert eine Liste mit UInt64-Werten nach der Quicksort-Methode
    /// </summary>
    /// <param name="values">Zeiger auf die Liste, welche sortiert werden soll</param>
    /// <param name="count">Anzahl der zu sortierenden Einträge</param>
    public static unsafe void Quick(ulong* values, int count)
    {
      if (count < 2) return;
      QuickSort(values, 0, --count);
    }

    /// <summary>
    /// sortiert eine Liste mit UInt64-Werten nach der Quicksort-Methode
    /// </summary>
    /// <param name="values">Liste, welche sortiert werden soll</param>
    public static unsafe void Quick(ulong[] values)
    {
      if (values.Length < 2) return;
      fixed (ulong* listeP = values)
      {
        QuickSort(listeP, 0, values.Length - 1);
      }
    }

    #region # public static void QuickSortLowStack<T>(T[] array, Comparison<T> vergleich) // QuickSort für unbekannte Typen. Sortiert das ganze Array. geringer Stack-Verbrauch
    /// <summary>
    /// Sortierung mit eigenem Stack um StackOverflowException zu vermeiden
    /// </summary>
    /// <typeparam name="T">Typ, welcher sortiert werden soll</typeparam>
    private sealed class SortLowStack<T>
    {
      readonly T[] array;
      readonly Comparison<T> compare;

      struct QuickElement
      {
        public int first;
        public int last;
      }

      readonly Stack<QuickElement> stack;

      public SortLowStack(T[] array, Comparison<T> compare)
      {
        if (array == null) throw new NullReferenceException("array");
        if (compare == null) throw new NullReferenceException("compare");
        this.array = array;
        this.compare = compare;
        stack = new Stack<QuickElement>();
      }

      public void Sort(int first, int last)
      {
        stack.Push(new QuickElement { first = first, last = last });
        while (stack.Count > 0)
        {
          var p = stack.Pop();
          SortInternal(p.first, p.last);
        }
      }

      void SortInternal(int first, int last)
      {
        var list = array;
        if (first + 32 > last) // Insertsort bevorzugen?
        {
          for (int pos = first + 1; pos <= last; pos++)
          {
            int j = pos;
            var tmp = list[j];
            for (; j > first && compare(tmp, list[j - 1]) < 0; j--) list[j] = list[j - 1];
            list[j] = tmp;
          }
        }
        else
        {
          int middle = (first + last) >> 1;
          T tmp;
          if (compare(list[middle], list[first]) < 0)
          {
            tmp = list[first]; list[first] = list[middle]; list[middle] = tmp;
          }
          if (compare(list[last], list[first]) < 0)
          {
            tmp = list[first]; list[first] = list[last]; list[last] = tmp;
          }
          if (compare(list[last], list[middle]) < 0)
          {
            tmp = list[middle]; list[middle] = list[last]; list[last] = tmp;
          }
          tmp = list[middle]; list[middle] = list[last - 1]; list[last - 1] = tmp;
          var pivot = list[last - 1];
          int i, j;
          for (i = first, j = last - 1; ; )
          {
            while (compare(list[++i], pivot) < 0) { }
            while (compare(pivot, list[--j]) < 0) { }
            if (i >= j) break;
            tmp = list[i]; list[i] = list[j]; list[j] = tmp;
          }
          tmp = list[i]; list[i] = list[last - 1]; list[last - 1] = tmp;

          stack.Push(new QuickElement { first = i + 1, last = last });
          stack.Push(new QuickElement { first = first, last = i - 1 });
        }
      }
    }

    /// <summary>
    /// QuickSort für unbekannte Typen. Sortiert das ganze Array. geringer Stack-Verbrauch
    /// </summary>
    /// <param name="array">Liste mit den zu sortierenden Daten</param>
    /// <param name="compare">Vergleichsmethode zweier Datensätze</param>
    public static void QuickSortLowStack<T>(T[] array, Comparison<T> compare)
    {
      var sorter = new SortLowStack<T>(array, compare);
      sorter.Sort(0, array.Length - 1);
    }
    #endregion
  }
}
