using System.Collections.Generic;
using System.Diagnostics;
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
    public readonly Dictionary<ulong, VariantSpan> data = new Dictionary<ulong, VariantSpan>();

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

      totalVariantCount++;

      VariantSpan oldSpan;
      if (data.TryGetValue(state, out oldSpan))
      {
        Debug.Assert(variant == oldSpan.variantStart + oldSpan.variantCount);
        data[state] = new VariantSpan(oldSpan.variantStart, oldSpan.variantCount + 1);
      }
      else
      {
        data.Add(state, new VariantSpan(variant, 1));
      }
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

      VariantSpan result;

      return data.TryGetValue(state, out result) ? result : new VariantSpan(0, 0);
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
