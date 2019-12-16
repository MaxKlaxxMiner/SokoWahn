using System.Collections.Generic;
using System.Diagnostics;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachstes Inhaltsverzeichnis für Varianten
  /// </summary>
  public class VariantStateDictNormal : VariantStateDict
  {
    /// <summary>
    /// merkt sich alle Zustand/Varianten Kombinationen
    /// </summary>
    public readonly Dictionary<ulong, List<ulong>> data = new Dictionary<ulong, List<ulong>>();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="stateList">Liste mit allen Zuständen im Raum</param>
    /// <param name="variantList">Liste mit allen Varianten im Raum</param>
    public VariantStateDictNormal(StateList stateList, VariantList variantList) : base(stateList, variantList) { }

    /// <summary>
    /// fügt eine weitere Variante pro Raumzustand hinzu
    /// </summary>
    /// <param name="stateId">Raumzustand, welcher betroffen ist</param>
    /// <param name="variantId">Variante, welche hinzugefügt werden soll</param>
    public override void Add(ulong stateId, ulong variantId)
    {
      Debug.Assert(stateId < stateList.Count);
      Debug.Assert(variantId < variantList.Count);

      List<ulong> list;

      if (!data.TryGetValue(stateId, out list))
      {
        list = new List<ulong>();
        data.Add(stateId, list);
      }

      Debug.Assert(!list.Contains(variantId));

      list.Add(variantId);
    }
  }
}
