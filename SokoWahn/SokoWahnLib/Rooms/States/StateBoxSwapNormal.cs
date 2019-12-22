using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Version welche sich Zustandswechel merkt, wenn eine Kiste in einen Raum geschoben wurde
  /// </summary>
  public sealed class StateBoxSwapNormal : StateBoxSwap
  {
    /// <summary>
    /// merkt sich die Zustandveränderungen
    /// </summary>
    public readonly Dictionary<ulong, ulong> data = new Dictionary<ulong, ulong>();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="stateList">Liste mit allen Zuständen im Raum</param>
    public StateBoxSwapNormal(StateList stateList) : base(stateList) { }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Einträge zurück
    /// </summary>
    public override ulong Count { get { return (uint)data.Count; } }

    /// <summary>
    /// fügt eine weitere Variante pro Raumzustand hinzu
    /// </summary>
    /// <param name="oldStateId">vorheriger Raum-Zustand</param>
    /// <param name="newStateId">nachfolgender Raum-Zustand, nachdem eine Kiste rein geschoben wurde</param>
    public override void Add(ulong oldStateId, ulong newStateId)
    {
      Debug.Assert(oldStateId < stateList.Count);
      Debug.Assert(newStateId < stateList.Count);
      Debug.Assert(oldStateId != newStateId);

      Debug.Assert(stateList.Get(oldStateId).Length + 1 == stateList.Get(newStateId).Length); // Kisten-Anzahl muss beim neuen Zustand genau um eins höher sein
      Debug.Assert(stateList.Get(oldStateId).Concat(stateList.Get(newStateId)).GroupBy(x => x).Count(x => x.Count() != 2) == 1); // nur eine Kistenänderung darf enthalten sein

      data.Add(oldStateId, newStateId);
    }

    /// <summary>
    /// gibt einen bestimmten Zustandswechsel zurück (oder gleiche ID, wenn keine Kiste aufgenommen werden kann)
    /// </summary>
    /// <param name="stateId">Zustand, welcher abgefragt werden soll</param>
    /// <returns>Zustand-ID nach dem Wechsel (oder gleiche ID, wenn keine Kiste aufgenommen werden könnte)</returns>
    public override ulong Get(ulong stateId)
    {
      Debug.Assert(stateId < stateList.Count);

      ulong newStateId;
      return data.TryGetValue(stateId, out newStateId) ? newStateId : stateId;
    }

    /// <summary>
    /// gibt alle Zustand-IDs zurück, wo eine Kiste aufgenommen werden kann
    /// </summary>
    /// <returns>Enumerable der Zustand-IDs, wo eine Kiste aufgenommen werden kann</returns>
    public override IEnumerable<ulong> GetAllKeys()
    {
      return data.Keys;
    }
  }
}
