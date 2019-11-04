using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse, welche einen kompletten Raum darstellt
  /// </summary>
  public class Room : IDisposable
  {
    /// <summary>
    /// merkt sich das gesamte Spiel, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// merkt sich alle Felder, welche zum Raum gehören
    /// </summary>
    public readonly HashSet<int> fieldPosis;
    /// <summary>
    /// merkt sich alle Zielfelder, welche zum Raum gehören
    /// </summary>
    public readonly HashSet<int> targetPosis;
    /// <summary>
    /// merkt sich die Portale zu den anderen Räumen
    /// </summary>
    public readonly RoomPortal[] portals;
    /// <summary>
    /// merkt sich die die Daten der Zustände
    /// </summary>
    public readonly Bitter stateData;
    /// <summary>
    /// Größe eines einzelnen Zustand-Elementes
    /// </summary>
    public readonly ulong stateDataElement;
    /// <summary>
    /// Anzahl der benutzen Zustand-Elemente
    /// </summary>
    public uint stateDataUsed;

    /// <summary>
    /// Konstruktor um ein Raum aus einem einzelnen Feld zu erstellen
    /// </summary>
    /// <param name="field">gesamtes Spielfeld, welches verwendet wird</param>
    /// <param name="pos">Position des Feldes, worraus der Raum generiert werden soll</param>
    /// <param name="portals">vorhandene Portale zu anderen Räumen</param>
    public Room(ISokoField field, int pos, RoomPortal[] portals)
    {
      if (field == null) throw new ArgumentNullException("field");
      if (!field.ValidPos(pos)) throw new ArgumentOutOfRangeException("pos");
      if (portals == null) throw new ArgumentNullException("portals");
      this.field = field;
      fieldPosis = new HashSet<int> { pos };
      targetPosis = new HashSet<int>();
      if (field.GetField(pos) == '.' || field.GetField(pos) == '*') targetPosis.Add(pos);
      this.portals = portals;
      stateDataElement = sizeof(ushort) * 8       // Spieler-Position (wenn auf dem Spielfeld vorhanden, sonst = 0)
                       + sizeof(byte) * 8         // Anzahl der Kisten, welche sich auf dem Spielfeld befinden
                       + sizeof(byte) * 8         // Anzahl der Kisten, welche sich bereits auf Zielfelder befinden
                       + (ulong)fieldPosis.Count; // Bit-markierte Felder, welche mit Kisten belegt sind
      stateData = new Bitter(stateDataElement * (ulong)fieldPosis.Count * 3UL); // Raum mit einzelnen Feld kann nur drei Zustände annehmen: 1 = leer, 2 = Spieler, 3 = Kiste
      stateDataUsed = 0;
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      for (int i = 0; i < portals.Length; i++)
      {
        if (portals[i] != null) portals[i].Dispose();
        portals[i] = null;
      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~Room()
    {
      Dispose();
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new
      {
        startPos = fieldPosis.Min(),
        size = fieldPosis.Count,
        portals = portals.Length + ": " + string.Join(", ", portals.AsEnumerable()),
        posis = string.Join(",", fieldPosis.OrderBy(pos => pos))
      }.ToString();
    }
  }
}
