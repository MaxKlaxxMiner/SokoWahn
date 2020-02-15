using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBeProtected.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse, welche eine Liste mit Raum-Zuständen speichern kann
  /// </summary>
  public abstract class StateList : IEnumerable<KeyValuePair<ulong, int[]>>, IDisposable
  {
    /// <summary>
    /// merkt sich alle Felder, welche zum Raum gehören
    /// </summary>
    public readonly int[] fieldPosis;
    /// <summary>
    /// merkt sich die Zielfelder, welche zum Raum gehören
    /// </summary>
    public readonly int[] goalPosis;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fieldPosis">alle Felder, welche zum Raum gehören</param>
    /// <param name="goalPosis">die Zielfelder, welche zum Raum gehören</param>
    protected StateList(int[] fieldPosis, int[] goalPosis)
    {
      if (fieldPosis == null) throw new ArgumentNullException("fieldPosis");
      if (goalPosis == null) throw new ArgumentNullException("goalPosis");

      Debug.Assert(fieldPosis.Length > 0);          // es muss mindestens ein Spielfeld vorhanden sein
      Debug.Assert(goalPosis.All(fieldPosis.Contains)); // alle Zielfelder müssen zum Spielfeld gehören
      Debug.Assert(fieldPosis.Length == new HashSet<int>(fieldPosis).Count && goalPosis.Length == new HashSet<int>(goalPosis).Count); // Doppler sind nicht erlaubt

      this.fieldPosis = fieldPosis;
      this.goalPosis = goalPosis;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Zustände zurück
    /// </summary>
    public abstract ulong Count { get; }

    /// <summary>
    /// fügt einen weiteren Zustand in die Liste hinzu und gibt die entsprechende ID zurück
    /// </summary>
    /// <param name="boxPosis">Kisten-Positionen des Zustandes</param>
    /// <returns>ID des neuen Zustandes</returns>
    public abstract ulong Add(int[] boxPosis);

    /// <summary>
    /// gibt die Kisten-Positionen eines bestimmten Zustandes zurück
    /// </summary>
    /// <param name="id">Zustand-ID, welche abgefragt werden soll</param>
    /// <returns>Array mit den gesetzten Kisten-Positionen</returns>
    public abstract int[] Get(ulong id);

    #region # // --- IEnumerable ---
    /// <summary>
    /// gibt alle Zustände als Enumerable zurück
    /// </summary>
    /// <returns>Enumerable aller Zustände</returns>
    public IEnumerable<KeyValuePair<ulong, int[]>> AsEnumerable()
    {
      for (ulong state = 0; state < Count; state++) yield return new KeyValuePair<ulong, int[]>(state, Get(state));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<KeyValuePair<ulong, int[]>> GetEnumerator()
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
    ~StateList()
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
