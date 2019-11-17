using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /*
   * --- Speicher-Nutzung mit Spieler ---
   * ushort playerPos
   * byte   boxCount
   * byte   finishedBoxCount
   * bits[] boxPosis
   * 
   * --- Speicher-Nutzung ohne Spieler ---
   * byte   boxCount
   * byte   finishedBoxCount
   * bits[] boxPosis
   * 
   */

  /// <summary>
  /// Struktur zum speichern eines Raum-Status (für Debug-Zwecke)
  /// </summary>
  public class StateDebugInfo
  {
    /// <summary>
    /// Raum, welcher diesen Raumzustand speichert
    /// </summary>
    public readonly Room room;
    /// <summary>
    /// Spieler-Position im Raum (sofern vorhanden, sonst 0)
    /// </summary>
    public readonly int playerPos;

    /// <summary>
    /// Anzahl der Kisten, welche sich im Raum befinden
    /// </summary>
    public readonly int boxCount;
    /// <summary>
    /// Anzahl der Kisten, welche sich bereits auf Zielfelder befinden
    /// </summary>
    public readonly int finishedBoxCount;

    /// <summary>
    /// Position der Kisten (sofern vorhanden, sonst leeres Array)
    /// </summary>
    public readonly int[] boxPosis;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Raum, welcher diesen Raumzustand speichert</param>
    /// <param name="playerPos">Spieler-Position im Raum (sofern vorhanden, sonst 0)</param>
    /// <param name="boxCount">Anzahl der Kisten, welche sich im Raum befinden</param>
    /// <param name="finishedBoxCount">Anzahl der Kisten, welche sich bereits auf Zielfelder befinden</param>
    /// <param name="boxPosis">Position der Kisten (sofern vorhanden, sonst leeres Array)</param>
    public StateDebugInfo(Room room, int playerPos, int boxCount, int finishedBoxCount, int[] boxPosis)
    {
      this.room = room;
      this.playerPos = playerPos;
      this.boxCount = boxCount;
      this.finishedBoxCount = finishedBoxCount;
      this.boxPosis = boxPosis;
      if (boxPosis.Length != boxCount) throw new ArgumentException("boxCount");
    }
  }
}
