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
    /// <param name="fields">alle Felder, welche zum Raum gehören</param>
    /// <param name="goals">die Zielfelder, welche zum Raum gehören</param>
    public StateListNormal(int[] fields, int[] goals) : base(fields, goals) { }

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
    /// <param name="boxes">Kisten-Positionen des Zustandes</param>
    /// <returns>ID des neuen Zustandes</returns>
    public override ulong Add(int[] boxes)
    {
      if (boxes == null) throw new ArgumentNullException("boxes");

      Debug.Assert(boxes.All(fields.Contains));                    // alle Kisten müssen sich auf den Feldern des Raumes befinden
      Debug.Assert(boxes.Length == new HashSet<int>(boxes).Count); // Doppler sind nicht erlaubt

      ulong id = (uint)data.Count;
      data.Add(boxes);
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
