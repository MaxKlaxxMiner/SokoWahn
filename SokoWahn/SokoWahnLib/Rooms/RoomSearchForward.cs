// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum suchen nach einer Lösung per vorwärts-Suche
  /// </summary>
  public sealed class RoomSearchForward
  {
    /// <summary>
    /// merkt sich das Räume-Netzwerk
    /// </summary>
    readonly RoomSolver roomSolver;
    /// <summary>
    /// merkt sich die einzelnen Räume des Netzwerkes
    /// </summary>
    readonly Room[] rooms;
    /// <summary>
    /// merkt sich das Spielfeld
    /// </summary>
    readonly ISokoField field;
    /// <summary>
    /// merkt sich die Zustände, welche ein fertig gelösten Spiel darstellen
    /// </summary>
    readonly HashSet<ulong> finishStates = new HashSet<ulong>();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomSolver">Räume-Netzwerk, welches verwendet werden soll</param>
    public RoomSearchForward(RoomSolver roomSolver)
    {
      this.roomSolver = roomSolver;
      rooms = roomSolver.rooms;
      field = roomSolver.field;

      var states = new uint[rooms.Length]; // aktuelle Zustände
      for (int i = 0; i < states.Length; i++) states[i] = uint.MaxValue;
      int roomIndex = 0;
      int hasPlayer = -1; // merkt sich den Raum, wo sich der Spieler befindet (-1 = noch kein Raum mit Spieler vorhanden)
      for (; ; )
      {
        states[roomIndex]++;
        if (states[roomIndex] >= rooms[roomIndex].StateUsed) // alle Zustände durchgezählt?
        {
          if (roomIndex == 0) break; // gesamter Vorgang abgeschlossen?
          states[roomIndex] = uint.MaxValue;
          roomIndex--;
          if (roomIndex == hasPlayer) hasPlayer = -1;  // Spieler-Zustand wieder entfernen
          continue;
        }

        var state = rooms[roomIndex].GetStateInfo(states[roomIndex]);
        if (state.boxCount != rooms[roomIndex].goalPosis.Count) continue; // nicht gelöster Zustand

        if (state.playerPos > 0)
        {
          if (hasPlayer >= 0) continue; // ein anderer Raum mit Spieler-Zustand ist bereits vorhanden?
          int pos = state.playerPos;    // Spielerposition abfragen
          // --- prüfen, ob die Spielerposition eine End-Stellung darstellen kann ---
          if ((!field.IsGoal(pos - 1) || field.IsGoal(pos + 1) || field.GetField(pos + 1) == '#')
           && (!field.IsGoal(pos + 1) || field.IsGoal(pos - 1) || field.GetField(pos - 1) == '#')
           && (!field.IsGoal(pos - field.Width) || field.IsGoal(pos + field.Width) || field.GetField(pos + field.Width) == '#')
           && (!field.IsGoal(pos + field.Width) || field.IsGoal(pos - field.Width) || field.GetField(pos - field.Width) == '#')
          ) continue;
          hasPlayer = roomIndex;
        }

        roomIndex++;
        if (roomIndex == states.Length)
        {
          roomIndex--;
          if (hasPlayer < 0) continue; // kein einziger Spieler-Zustand gesetzt? -> kein gültiger Gesamt-Zustand

          roomSolver.DisplayRoomStates(states);
          ulong crc = Crc64.Start.Crc64Update(states);
          finishStates.Add(crc);

          if (roomIndex == hasPlayer) hasPlayer = -1;  // Spieler-Zustand wieder entfernen
        }
      }

      Console.WriteLine("  finish States: " + finishStates.Count.ToString("N0"));
      Console.ReadKey(true);
    }

    /// <summary>
    /// führt einen oder mehrere Suchschritte durch
    /// </summary>
    /// <param name="maxTicks">maximale Anzahl der Rechenschritte</param>
    /// <returns>fertige Lösung oder null, wenn noch keine Lösung gefunden wurde</returns>
    public string Tick(int maxTicks)
    {
      return null;
    }
  }
}
