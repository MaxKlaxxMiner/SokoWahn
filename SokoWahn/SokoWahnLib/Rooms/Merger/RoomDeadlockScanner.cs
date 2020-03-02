using System;

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse zum Suchen von nicht lösbaren Varianten in einem Raum
  /// </summary>
  public class RoomDeadlockScanner
  {
    /// <summary>
    /// merkt sich den Raum, welcher optimiert werden soll
    /// </summary>
    readonly Room room;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Raum, welcher durchsucht werden soll</param>
    public RoomDeadlockScanner(Room room)
    {
      if (room == null) throw new NullReferenceException("room");
      this.room = room;
    }
  }
}
