// ReSharper disable UnusedMember.Global
// ReSharper disable TailRecursiveCall

/* * * * * * * * * * * * *
 *  Quelle: ngMax.Lite   *
 * * * * * * * * * * * * */

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
  }
}
