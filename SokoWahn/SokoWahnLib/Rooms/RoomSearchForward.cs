// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable RedundantIfElseBlock

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
    /// merkt sich die Feld-Position mit den dazugehörigen verlinkten Räumen
    /// </summary>
    readonly int[] posToRoom;
    /// <summary>
    /// merkt sich die Zustände, welche ein fertig gelösten Spiel darstellen
    /// </summary>
    readonly HashSet<ulong> finishStates;
    /// <summary>
    /// Zustände, welche noch abgearbeitet werden müssen (optimale Bewegungs-Schritte)
    /// </summary>
    readonly List<KeyValuePair<uint, List<uint>>> todoMoveStates;
    /// <summary>
    /// merkt sich die Größe eines Aufgaben-Elementes (1 + rooms.Length)
    /// </summary>
    readonly int todoElement;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomSolver">Räume-Netzwerk, welches verwendet werden soll</param>
    public RoomSearchForward(RoomSolver roomSolver)
    {
      this.roomSolver = roomSolver;
      rooms = roomSolver.rooms;
      field = roomSolver.field;
      posToRoom = new int[field.Width * field.Height];
      finishStates = new HashSet<ulong>();
      todoMoveStates = new List<KeyValuePair<uint, List<uint>>>();
      todoElement = 1 + rooms.Length; // Spieler + Zustände der Räume

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

      // --- Start-Stellung suchen ---
      for (int r = 0; r < states.Length; r++)
      {
        var room = rooms[r];

        bool roomPlayer = room.fieldPosis.Any(pos => field.IsPlayer(pos));    // Raum enthält den Spieler am Start?
        var boxes = room.fieldPosis.Where(pos => field.IsBox(pos)).ToArray(); // merkt sich die Kisten im Raum, welche am Start stehen

        if (roomPlayer) // Zustände mit Spieler durchsuchen
        {
          for (uint sp = 0; sp < room.statePlayerUsed; sp++)
          {
            var info = room.GetPlayerStateInfo(sp);
            if (!field.IsPlayer(info.playerPos)) continue; // Spieler-Position stimmt nicht
            if (info.boxCount != boxes.Length) continue;   // Kisten-Anzahl stimmt nicht
            hasPlayer = info.playerPos;

            int i;
            for (i = 0; i < boxes.Length; i++)
            {
              if (boxes[i] != info.boxPosis[i]) break;
            }

            if (i == boxes.Length) // passenden Start-Zustand mit Spieler gefunden?
            {
              states[r] = sp;
              break;
            }
          }
        }
        else // Zustände ohne Spieler durchsuchen
        {
          for (uint sb = 0; sb < room.stateBoxUsed; sb++)
          {
            var info = room.GetBoxStateInfo(sb);
            if (info.boxCount != boxes.Length) continue; // Kisten-Anzahl stimmt nicht

            int i;
            for (i = 0; i < boxes.Length; i++)
            {
              if (boxes[i] != info.boxPosis[i]) break;
            }

            if (i == boxes.Length) // passenden Start-Zustand ohne Spieler gefunden?
            {
              states[r] = sb + room.statePlayerUsed;
              break;
            }
          }
        }
      }

      // --- posToRoom-Map befüllen ---
      for (int r = 0; r < rooms.Length; r++)
      {
        foreach (int pos in rooms[r].fieldPosis) posToRoom[pos] = r;
      }

      // --- Start-Stellung als erste Aufgabe hinzufügen ---
      todoMoveStates.Add(new KeyValuePair<uint, List<uint>>(0, new[] { (uint)hasPlayer }.Concat(states).ToList()));

      roomSolver.DisplayRoomStates(states);
    }

    /// <summary>
    /// führt einen oder mehrere Suchschritte durch
    /// </summary>
    /// <param name="maxTicks">maximale Anzahl der Rechenschritte</param>
    /// <returns>fertige Lösung oder null, wenn noch keine Lösung gefunden wurde</returns>
    public string Tick(int maxTicks)
    {
      if (todoMoveStates.Count == 0) return null;

      var states = new uint[rooms.Length];
      int playerPos = 0;

      // --- Methode zum verarbeiten der forlaufenden Spieler-Schritte ---
      Action<VariantDebugInfo, uint[], int, uint> nextPlayerScan = null;
      nextPlayerScan = (v, state, stateIndex, moves) =>
      {
        moves += v.moves;
        var portal = v.outgoingPortal;
        roomSolver.DisplayRoomStates(state, portal.posTo);

        state[stateIndex] = v.outgoingState;
        int nextStateIndex = posToRoom[portal.posTo];
        var room = portal.roomTo;

        foreach (uint vp in portal.roomToPlayerVariants) // Varianten durcharbeiten, wo der Spieler den Raum wieder verlässt (rekursive Suche)
        {
          var vst = room.GetVariantStates(vp);
          if (vst.Key != state[nextStateIndex]) continue; // Variante passt nicht zum Status, Todo: optimieren durch sortierte Liste
          var vs = room.GetVariantInfo(vp);
          if (vs.outgoingBox)
          {
            throw new Exception("box variant?");
          }
          else
          {
            nextPlayerScan(vs, state, nextStateIndex, moves);
          }
        }

        foreach (uint vb in portal.roomToBoxVariants) // Varianten durcharbeiten, wo nur eine Kiste den Raum verlässt
        {
          throw new NotImplementedException();
        }

        state[stateIndex] = v.incomingState;
      };

      var movesBase = todoMoveStates.First().Key;
      var firstList = todoMoveStates.First().Value;
      while (maxTicks > 0 && firstList.Count > 0)
      {
        for (int i = 0; i < states.Length; i++) states[i] = firstList[firstList.Count - states.Length + i];
        playerPos = (int)firstList[firstList.Count - todoElement];
        firstList.RemoveRange(firstList.Count - todoElement, todoElement);
        int stateIndex = posToRoom[playerPos];
        var room = rooms[stateIndex];

        for (uint vp = 0; vp < room.startPlayerVariants.Count; vp++)
        {
          throw new NotImplementedException();
        }

        for (uint vb = 0; vb < room.startBoxVariants.Count; vb++)
        {
          var vst = room.GetVariantStates(vb);
          if (vst.Key != states[stateIndex]) continue; // Variante passt nicht zum Status, Todo: optimieren durch sortierte Liste
          var vs = room.GetVariantInfo(vb);
          nextPlayerScan(vs, states, stateIndex, movesBase);
        }

        maxTicks--;
      }

      if (firstList.Count == 0)
      {
        todoMoveStates.RemoveAt(0);
      }

      // --- letzten Zustand anzeigen ---
      Console.Clear();
      roomSolver.DisplayRoomStates(states);
      Console.WriteLine("  State: " + playerPos + " - " + string.Join(" ", states));
      Console.WriteLine();
      foreach (var todo in todoMoveStates)
      {
        Console.WriteLine("  {0:N0}: {1:N0}", todo.Key, todo.Value.Count / todoElement);
        if (Console.CursorTop >= Console.WindowWidth - 2) break;
      }

      if (Console.ReadKey(true).Key == ConsoleKey.Escape) Environment.Exit(0);

      return null;
    }
  }
}
