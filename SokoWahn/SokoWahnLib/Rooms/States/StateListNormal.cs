using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Version einer Zustände-Liste
  /// </summary>
  public sealed class StateListNormal : StateList
  {
    /// <summary>
    /// merkt sich die gespeicherten Zustände
    /// </summary>
    readonly List<int[]> data = new List<int[]>();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fieldPosis">alle Felder, welche zum Raum gehören</param>
    /// <param name="goalPosis">die Zielfelder, welche zum Raum gehören</param>
    public StateListNormal(int[] fieldPosis, int[] goalPosis) : base(fieldPosis, goalPosis) { }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Zustände zurück
    /// </summary>
    public override ulong Count
    {
      get
      {
        return (uint)data.Count;
      }
    }

    /// <summary>
    /// fügt einen weiteren Zustand in die Liste hinzu und gibt die entsprechende ID zurück
    /// </summary>
    /// <param name="boxPosis">Kisten-Positionen des Zustandes</param>
    /// <returns>ID des neuen Zustandes</returns>
    public override ulong Add(int[] boxPosis)
    {
      if (boxPosis == null) throw new ArgumentNullException("boxPosis");

      Debug.Assert(boxPosis.All(fieldPosis.Contains));                    // alle Kisten müssen sich auf den Feldern des Raumes befinden
      Debug.Assert(boxPosis.Length == new HashSet<int>(boxPosis).Count); // Doppler sind nicht erlaubt

      ulong id = (uint)data.Count;
      data.Add(boxPosis);
      return id;
    }

    /// <summary>
    /// gibt die Kisten-Positionen eines bestimmten Zustandes zurück
    /// </summary>
    /// <param name="id">Zustand-ID, welche abgefragt werden soll</param>
    /// <returns>Array mit den gesetzten Kisten-Positionen</returns>
    public override int[] Get(ulong id)
    {
      Debug.Assert(id < (uint)data.Count);

      return data[(int)(uint)id];
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose() { }
  }
}
