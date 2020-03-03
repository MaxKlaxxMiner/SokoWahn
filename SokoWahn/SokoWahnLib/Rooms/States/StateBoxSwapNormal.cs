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
    /// <param name="oldState">vorheriger Raum-Zustand</param>
    /// <param name="newState">nachfolgender Raum-Zustand, nachdem eine Kiste rein geschoben wurde</param>
    public override void Add(ulong oldState, ulong newState)
    {
      Debug.Assert(oldState < stateList.Count);
      Debug.Assert(newState < stateList.Count);
      Debug.Assert(oldState != newState);

      Debug.Assert(stateList.Get(oldState).Length + 1 == stateList.Get(newState).Length   // Kisten-Anzahl muss beim neuen Zustand genau um eins höher sein
                || stateList.Get(oldState).Length == stateList.Get(newState).Length + 1); // oder eins niedriger, wenn Kisten herrausgezeogen werden
      Debug.Assert(stateList.Get(oldState).Concat(stateList.Get(newState)).GroupBy(x => x).Count(x => x.Count() != 2) == 1); // nur eine Kistenänderung darf enthalten sein

      data.Add(oldState, newState);
    }

    /// <summary>
    /// gibt einen bestimmten Zustandswechsel zurück (oder gleiche ID, wenn keine Kiste aufgenommen werden kann)
    /// </summary>
    /// <param name="state">Zustand, welcher abgefragt werden soll</param>
    /// <returns>Zustand-ID nach dem Wechsel (oder gleiche ID, wenn keine Kiste aufgenommen werden könnte)</returns>
    public override ulong Get(ulong state)
    {
      Debug.Assert(state < stateList.Count);

      ulong nextState;
      return data.TryGetValue(state, out nextState) ? nextState : state;
    }

    /// <summary>
    /// gibt alle Zustand-IDs zurück, wo eine Kiste aufgenommen werden kann
    /// </summary>
    /// <returns>Enumerable der Zustand-IDs, wo eine Kiste aufgenommen werden kann</returns>
    public override IEnumerable<ulong> GetAllKeys()
    {
      return data.Keys;
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose() { }
  }
}
