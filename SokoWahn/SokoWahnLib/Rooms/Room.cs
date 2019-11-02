using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse, welche einen komlletten Raum darstellt
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
    /// merkt sich die Portale zu den anderen Räumen
    /// </summary>
    public readonly RoomPortal[] portals;

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
      this.portals = portals;
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      foreach (var portal in portals)
      {
        portal.Dispose();
      }
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
        portals = "[" + portals.Length + "] " + string.Join("-", portals.AsEnumerable()),
        posis = string.Join(",", fieldPosis.OrderBy(pos => pos))
      }.ToString();
    }
  }
}
