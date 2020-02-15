using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachstes Inhaltsverzeichnis für Varianten
  /// </summary>
  public sealed class VariantStateDictNormal : VariantStateDict
  {
    /// <summary>
    /// merkt sich alle Zustand/Varianten Kombinationen
    /// </summary>
    public readonly Dictionary<ulong, List<ulong>> data = new Dictionary<ulong, List<ulong>>();

    /// <summary>
    /// merkt sich die Anzahl der insgesamt gespeicherten Varianten
    /// </summary>
    ulong totalVariantCount;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="stateList">Liste mit allen Zuständen im Raum</param>
    /// <param name="variantList">Liste mit allen Varianten im Raum</param>
    public VariantStateDictNormal(StateList stateList, VariantList variantList) : base(stateList, variantList) { }

    /// <summary>
    /// fügt eine weitere Variante pro Raumzustand hinzu
    /// </summary>
    /// <param name="state">Raumzustand, welcher betroffen ist</param>
    /// <param name="variant">Variante, welche hinzugefügt werden soll</param>
    public override void Add(ulong state, ulong variant)
    {
      Debug.Assert(state < stateList.Count);
      Debug.Assert(variant < variantList.Count);

      List<ulong> list;

      if (!data.TryGetValue(state, out list))
      {
        list = new List<ulong>();
        data.Add(state, list);
      }

      Debug.Assert(!list.Contains(variant));

      list.Add(variant);
      totalVariantCount++;
    }

    /// <summary>
    /// gibt alle Zustände zurück, wofür Varianten bekannt sind
    /// </summary>
    /// <returns>Enumerable der bekannten Zustände</returns>
    public override IEnumerable<ulong> GetAllStates()
    {
      return data.Keys;
    }

    /// <summary>
    /// fragt alle Varianten ab, welche zu einem bestimmten Zustand gehören und gibt diese als Kette zurück
    /// </summary>
    /// <param name="state">Raumzustand, welcher abgefragt werden soll</param>
    /// <returns>entsprechende Variante-Kette</returns>
    public override VariantSpan GetVariantSpan(ulong state)
    {
      Debug.Assert(state < stateList.Count);

      List<ulong> resultList;

      if (!data.TryGetValue(state, out resultList)) return new VariantSpan(0, 0); // leere Kette zurück geben

      Debug.Assert(resultList.Count > 0);
      Debug.Assert(resultList[resultList.Count - 1] == resultList[0] + (uint)resultList.Count - 1);
      Debug.Assert(Enumerable.Range(0, resultList.Count).All(i => resultList[i] == resultList[0] + (uint)i));

      return new VariantSpan(resultList[0], (uint)resultList.Count);
    }

    /// <summary>
    /// gibt an, wieviel Zustände insgesamt gespeichert wurden (wofür Varianten bekannt sind)
    /// </summary>
    public override ulong TotalStateCount{get { return (uint)data.Count; }}

    /// <summary>
    /// gibt an, wieviel Varianten-Verlinkungen insgesamt gespeichert wurden
    /// </summary>
    public override ulong TotalVariantCount{get { return totalVariantCount; }}

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose() { }
  }
}
