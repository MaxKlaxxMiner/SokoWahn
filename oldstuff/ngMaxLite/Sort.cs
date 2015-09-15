// ReSharper disable TailRecursiveCall
namespace Sokosolver
{
  internal static class Sort
  {
    /// <summary>
    /// Kernmethode QuickSort für den Typ UInt64
    /// </summary>
    /// <param name="liste">Zeiger auf die Liste mit UInt64-Werten</param>
    /// <param name="von">erster Wert in der Liste</param>
    /// <param name="bis">letzter Wert in der Liste (zeigt direkt auf den Datensatz)</param>
    static unsafe void QuickSort(ulong* liste, int von, int bis)
    {
      if (von + 32 > bis) // Insertsort bevorzugen?
      {
        var listeV = &liste[von];
        var listeB = &liste[bis];
        for (var listeP = listeV + 1; listeP <= listeB; listeP++)
        {
          var listeJ = listeP;
          ulong tmp = *listeJ;
          for (; listeJ > listeV && tmp < listeJ[-1]; listeJ--) *listeJ = listeJ[-1];
          *listeJ = tmp;
        }
      }
      else
      {
        int mitte = (von + bis) >> 1;
        ulong tmp;
        if (liste[mitte] < liste[von])
        {
          tmp = liste[von]; liste[von] = liste[mitte]; liste[mitte] = tmp;
        }
        if (liste[bis] < liste[von])
        {
          tmp = liste[von]; liste[von] = liste[bis]; liste[bis] = tmp;
        }
        if (liste[bis] < liste[mitte])
        {
          tmp = liste[mitte]; liste[mitte] = liste[bis]; liste[bis] = tmp;
        }
        tmp = liste[mitte]; liste[mitte] = liste[bis - 1]; liste[bis - 1] = tmp;
        ulong pivot = liste[bis - 1];
        int i, j;
        for (i = von, j = bis - 1; ; )
        {
          while (liste[++i] < pivot) { }
          while (pivot < liste[--j]) { }
          if (i >= j) break;
          tmp = liste[i]; liste[i] = liste[j]; liste[j] = tmp;
        }
        tmp = liste[i]; liste[i] = liste[bis - 1]; liste[bis - 1] = tmp;
        QuickSort(liste, von, i - 1);
        QuickSort(liste, i + 1, bis);
      }
    }

    /// <summary>
    /// sortiert eine Liste mit UInt64-Werten nach der Quicksort-Methode
    /// </summary>
    /// <param name="liste">Zeiger auf die Liste, welche sortiert werden soll</param>
    /// <param name="anzahl">Anzahl der zu sortierenden Einträge</param>
    public static unsafe void Quick(ulong* liste, int anzahl)
    {
      if (anzahl < 2) return;
      QuickSort(liste, 0, --anzahl);
    }

    /// <summary>
    /// sortiert eine Liste mit UInt64-Werten nach der Quicksort-Methode
    /// </summary>
    /// <param name="liste">Liste, welche sortiert werden soll</param>
    public static unsafe void Quick(ulong[] liste)
    {
      if (liste.Length < 2) return;
      fixed (ulong* listeP = liste)
      {
        QuickSort(listeP, 0, liste.Length - 1);
      }
    }
  }
}
