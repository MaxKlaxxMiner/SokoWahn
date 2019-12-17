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
  public abstract class StateList : IEnumerable<KeyValuePair<ulong, int[]>>
  {
    /// <summary>
    /// merkt sich alle Felder, welche zum Raum gehören
    /// </summary>
    public readonly int[] fields;
    /// <summary>
    /// merkt sich die Zielfelder, welche zum Raum gehören
    /// </summary>
    public readonly int[] goals;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fields">alle Felder, welche zum Raum gehören</param>
    /// <param name="goals">die Zielfelder, welche zum Raum gehören</param>
    protected StateList(int[] fields, int[] goals)
    {
      if (fields == null) throw new ArgumentNullException("fields");
      if (goals == null) throw new ArgumentNullException("goals");

      Debug.Assert(fields.Length > 0);          // es muss mindestens ein Spielfeld vorhanden sein
      Debug.Assert(goals.All(fields.Contains)); // alle Zielfelder müssen zum Spielfeld gehören
      Debug.Assert(fields.Length == new HashSet<int>(fields).Count && goals.Length == new HashSet<int>(goals).Count); // Doppler sind nicht erlaubt

      this.fields = fields;
      this.goals = goals;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Zustände zurück
    /// </summary>
    public abstract ulong Count { get; }

    /// <summary>
    /// fügt einen weiteren Zustand in die Liste hinzu und gibt die entsprechende ID zurück
    /// </summary>
    /// <param name="boxes">Kisten-Positionen des Zustandes</param>
    /// <returns>ID des neuen Zustandes</returns>
    public abstract ulong Add(int[] boxes);

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
      for (ulong id = 0; id < Count; id++) yield return new KeyValuePair<ulong, int[]>(id, Get(id));
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
  }
}
