using System;
using System.Collections;
using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse, welche sich Zustandswechel merkt, wenn eine Kiste in einen Raum geschoben wurde
  /// </summary>
  public abstract class StateBoxSwap : IEnumerable<KeyValuePair<ulong, ulong>>, IDisposable
  {
    /// <summary>
    /// Liste mit allen Zuständen im Raum
    /// </summary>
    public readonly StateList stateList;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="stateList">Liste mit allen Zuständen im Raum</param>
    protected StateBoxSwap(StateList stateList)
    {
      if (stateList == null) throw new ArgumentNullException("stateList");

      this.stateList = stateList;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Einträge zurück
    /// </summary>
    public abstract ulong Count { get; }

    /// <summary>
    /// fügt eine weitere Variante pro Raumzustand hinzu
    /// </summary>
    /// <param name="oldStateId">vorheriger Raum-Zustand</param>
    /// <param name="newStateId">nachfolgender Raum-Zustand, nachdem eine Kiste rein geschoben wurde</param>
    public abstract void Add(ulong oldStateId, ulong newStateId);

    /// <summary>
    /// gibt einen bestimmten Zustandswechsel zurück (oder gleiche ID, wenn keine Kiste aufgenommen werden kann)
    /// </summary>
    /// <param name="stateId">Zustand, welcher abgefragt werden soll</param>
    /// <returns>Zustand-ID nach dem Wechsel (oder gleiche ID, wenn keine Kiste aufgenommen werden könnte)</returns>
    public abstract ulong Get(ulong stateId);

    /// <summary>
    /// gibt alle Zustand-IDs zurück, wo eine Kiste aufgenommen werden kann
    /// </summary>
    /// <returns>Enumerable der Zustand-IDs, wo eine Kiste aufgenommen werden kann</returns>
    public abstract IEnumerable<ulong> GetAllKeys();

    #region # // --- IEnumerable ---
    /// <summary>
    /// gibt alle gespeicherten Elemente als Enumerable zurück
    /// </summary>
    /// <returns>Enumerable der gespeicherten Elemente</returns>
    public IEnumerable<KeyValuePair<ulong, ulong>> AsEnumerable()
    {
      foreach (ulong id in GetAllKeys()) yield return new KeyValuePair<ulong, ulong>(id, Get(id));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<KeyValuePair<ulong, ulong>> GetEnumerator()
    {
      return AsEnumerable().GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// Destruktor
    /// </summary>
    ~StateBoxSwap()
    {
      Dispose();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public abstract void Dispose();
    #endregion
  }
}
