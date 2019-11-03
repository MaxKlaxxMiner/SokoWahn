#region # using *.*
using System;
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// merkt sich das Portal, welches zwei Räume miteinander veknüpft
  /// </summary>
  public class RoomPortal : IDisposable
  {
    /// <summary>
    /// merkt sich das gesamte Spielfeld
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// Austritts-Position aus dem eigenen Raum
    /// </summary>
    public readonly int posFrom;
    /// <summary>
    /// Eintritts-Position in den benachbarten Raum
    /// </summary>
    public readonly int posTo;
    /// <summary>
    /// merkt sich den Raum, wohin das Portal führt
    /// </summary>
    public readonly Room roomTo;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="posFrom">Austritts-Position aus dem eigenen Raum</param>
    /// <param name="posTo">Eintritts-Position in den benachbarten Raum</param>
    /// <param name="roomTo">benachbarter Raum</param>
    public RoomPortal(int posFrom, int posTo, Room roomTo)
    {
      if (roomTo == null) throw new ArgumentNullException("roomTo");
      field = roomTo.field;
      if (!field.ValidPos(posFrom)) throw new ArgumentOutOfRangeException("posFrom");
      if (!field.ValidPos(posTo)) throw new ArgumentOutOfRangeException("posTo");
      this.posFrom = posFrom;
      this.posTo = posTo;
      this.roomTo = roomTo;
    }

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

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return "( " + posFrom + " -> " + posTo + " )";
    }
  }
}
