#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CollectionNeverUpdated.Global

// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// merkt sich das Portal, welches zwei Räume miteinander veknüpft
  /// </summary>
  public sealed class RoomPortal : IDisposable
  {
    #region # // --- Variablen ---
    /// <summary>
    /// merkt sich das gesamte Spielfeld
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// Austritts-Position aus dem Quell-Raum
    /// </summary>
    public readonly int posFrom;
    /// <summary>
    /// Eintritts-Position den Ziel-Raum
    /// </summary>
    public readonly int posTo;
    /// <summary>
    /// merkt sich den Quell-Raum, wo das Portal herkommt
    /// </summary>
    public readonly Room roomFrom;
    /// <summary>
    /// merkt sich den Ziel-Raum, wohin das Portal führt
    /// </summary>
    public readonly Room roomTo;
    /// <summary>
    /// merkt sich das zurückführende Portal
    /// </summary>
    public RoomPortal oppositePortal;
    /// <summary>
    /// merkt sich die Varianten, wenn der Spieler in das benachbarte Portal läuft
    /// </summary>
    public readonly List<uint> roomToPlayerVariants = new List<uint>();
    /// <summary>
    /// merkt sich die Varianten, wenn eine Kiste in das benachbarte Portal geschoben wird
    /// </summary>
    public readonly List<uint> roomToBoxVariants = new List<uint>();
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="posFrom">Austritts-Position aus dem eigenen Raum</param>
    /// <param name="posTo">Eintritts-Position in den benachbarten Raum</param>
    /// <param name="roomFrom">Quell-Raum, woher das Portal herkommt</param>
    /// <param name="roomTo">Ziel-Raum, wohin das Portal führt</param>
    public RoomPortal(int posFrom, int posTo, Room roomFrom, Room roomTo)
    {
      field = roomFrom.field;
      if (!field.ValidPos(posFrom)) throw new ArgumentOutOfRangeException("posFrom");
      if (!field.ValidPos(posTo)) throw new ArgumentOutOfRangeException("posTo");
      if (roomFrom == null) throw new ArgumentNullException("roomFrom");
      if (roomTo == null) throw new ArgumentNullException("roomTo");
      if (roomFrom.fieldPosis.All(pos => pos != posFrom)) throw new ArgumentOutOfRangeException("posFrom"); // Quell-Position gehört nicht zum Quell-Raum?
      if (roomTo.fieldPosis.All(pos => pos != posTo)) throw new ArgumentOutOfRangeException("posTo");       // Ziel-Position gehört nicht zum Ziel-Raum?
      this.posFrom = posFrom;
      this.posTo = posTo;
      this.roomFrom = roomFrom;
      this.roomTo = roomTo;
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~RoomPortal()
    {
      Dispose();
    }
    #endregion

    #region # // --- ToString() ---
    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return "( " + posFrom + " -> " + posTo + " )";
    }
    #endregion
  }
}
