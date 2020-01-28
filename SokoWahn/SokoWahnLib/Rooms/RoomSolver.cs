using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable RedundantIfElseBlock

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum Lösen eines Spielfeldes
  /// </summary>
  public sealed class RoomSolver
  {
    /// <summary>
    /// aktuelles Räume-Netzwerk, welches für die Suche verwendet werden soll
    /// </summary>
    readonly RoomNetwork roomNetwork;

    /// <summary>
    /// merkt sich die Räume im Netzwerk
    /// </summary>
    readonly Room[] rooms;

    /// <summary>
    /// merkt sich die Anzahl der Räume im Netzwerk
    /// </summary>
    readonly uint roomCount;

    /// <summary>
    /// optionale Methode zum Anzeigen im Debug-Modus
    /// </summary>
    readonly Action debugDisplay;

    /// <summary>
    /// Anzahl der Bits für die Variante innerhalb eines 64-Bit Wertes
    /// </summary>
    const int VariantBits = 48;
    /// <summary>
    /// Bit-Maske für die Variante
    /// </summary>
    const ulong VariantMask = (1UL << VariantBits) - 1;

    /// <summary>
    /// aktueller Such-Status
    /// </summary>
    enum SolveState
    {
      /// <summary>
      /// initialer Anfangs-Zustand
      /// </summary>
      Init,
      /// <summary>
      /// fügt alle Start-Varianten hinzu
      /// </summary>
      AddStarts,
      /// <summary>
      /// Suchmodus in Vorwärts-Richtung
      /// </summary>
      ScanForward
    }

    /// <summary>
    /// merkt sich den aktuellen Such-Status
    /// </summary>
    SolveState solveState = SolveState.Init;

    /// <summary>
    /// merkt sich alle bereits verarbeiteten Hash-Einträge (nur Varianten mit Kisten verschiebungen)
    /// </summary>
    readonly HashCrc hashTable = new HashCrcNormal();

    /// <summary>
    /// merkt sich den aktuellen Spielfeld-Zustand
    /// </summary>
    readonly ulong[] currentTask;

    /// <summary>
    /// merkt sich die abzuarbeitenden Aufgaben pro Zugtiefe
    /// </summary>
    readonly List<TaskList> forwardTasks = new List<TaskList>();

    /// <summary>
    /// merkt sich die aktuelle Vorwärts-Suchtiefe
    /// </summary>
    int forwardIndex;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomNetwork">Netzwerk, welches verwendet werden soll</param>
    /// <param name="debugDisplay">optionale Methode für die Debug-Anzeige</param>
    public RoomSolver(RoomNetwork roomNetwork, Action debugDisplay = null)
    {
      if (roomNetwork == null) throw new ArgumentNullException("roomNetwork");
      this.roomNetwork = roomNetwork;
      rooms = roomNetwork.rooms;
      if (rooms == null) throw new ArgumentNullException("roomNetwork");
      roomCount = (uint)rooms.Length;
      currentTask = new ulong[roomCount + 1]; // Raum-Zustände[] + (uhort)Raum-Nummer | ausgewählte Variante
      this.debugDisplay = debugDisplay ?? (() => { });
    }

    /// <summary>
    /// sucht nach der Lösung
    /// </summary>
    /// <param name="maxTicks">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn eine Lösung gefunden wurde (bzw. Ende erreicht wurde), sonst: false</returns>
    public bool SearchCycle(int maxTicks)
    {
      switch (solveState)
      {
        #region # // --- Init - erste Initialisierung ---
        case SolveState.Init:
        {
          if (roomNetwork.rooms.Length > 65535) throw new SokoFieldException("to many rooms (" + roomNetwork.rooms.Length + " > 65535)");
          int startRoomIndex = -1; // Raum suchen, wo der Spieler beginnt
          for (int i = 0; i < roomNetwork.rooms.Length; i++)
          {
            currentTask[i] = roomNetwork.rooms[i].startState;
            if (roomNetwork.rooms[i].variantList.Count > VariantMask) throw new SokoFieldException("overflow variant.Count");
            if (roomNetwork.rooms[i].startVariantCount > 0)
            {
              if (startRoomIndex >= 0) throw new SokoFieldException("duplicate start-room");
              startRoomIndex = i;
            }
          }
          if (startRoomIndex < 0) throw new SokoFieldException("no start-room");
          currentTask[roomCount] = (ulong)(uint)startRoomIndex << VariantBits;
          solveState = SolveState.AddStarts;
        } break;
        #endregion

        #region # // --- AddStarts - Anfangswerte als Aufgaben hinzufügen ---
        case SolveState.AddStarts:
        {
          uint startRoomIndex = (uint)(currentTask[roomCount] >> VariantBits);
          ulong variant = currentTask[roomCount] & VariantMask;
          var tmpTask = new ulong[roomCount + 1];

          for (; maxTicks > 0; maxTicks--)
          {
            // --- Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            Array.Copy(currentTask, tmpTask, tmpTask.Length);
            int pushes;
            int moves = ResolveVariant(tmpTask, rooms[startRoomIndex], variant, out pushes);
            if (moves > 0)
            {
              ulong crc = Crc64.Get(tmpTask);
              hashTable.Add(crc, (uint)moves);

              while (moves >= forwardTasks.Count) forwardTasks.Add(new TaskListNormal(roomCount + 1));
              forwardTasks[moves].Add(tmpTask);
            }

            // --- zur nächsten Start-Variante springen ---
            variant++;
            currentTask[roomCount]++;

            if (variant == rooms[startRoomIndex].startVariantCount) // alle Start-Varianten bereits ermittelt?
            {
              solveState = SolveState.ScanForward;
              if (maxTicks == 0) maxTicks = 1; // ein Arbeitsschritt wieder hinzufügen (für den initialen Scan-Vorgang)
              goto case SolveState.ScanForward;
            }
          }
        } break;
        #endregion

        #region # // --- ScanForward - Lösungssuche vorwärts ---
        case SolveState.ScanForward:
        {
          if (forwardIndex >= forwardTasks.Count) return true; // Ende der Aufgaben-Listen erreicht?
          var taskList = forwardTasks[forwardIndex];
          if (taskList.Count == 0) // Aufgabenliste für diesen Zug bereits abgearbeitet?
          {
            forwardIndex++;
            goto case SolveState.ScanForward;
          }

          for (; maxTicks > 0 && taskList.Count > 0; maxTicks--)
          {
            taskList.FetchFirst(currentTask);

            // --- Aufgabe prüfen und neue Einträge in die Aufgaben-Liste hinzufügen ---
            foreach (int moves in VariantMoves(currentTask))
            {
              int totalMoves = forwardIndex + moves;
              ulong crc = Crc64.Get(currentTask);
              ulong oldMoves = hashTable.Get(crc, ulong.MaxValue);
              if ((uint)totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
              {
                if (moves == 0 && currentTask.Take((int)roomCount).All(x => x == 0)) // Ende gefunden?
                {
                  throw new NotImplementedException();
                }

                if (oldMoves == ulong.MaxValue) hashTable.Add(crc, (uint)totalMoves); else hashTable.Update(crc, (uint)totalMoves);

                // --- Variante als Aufgabe hinzufügen ---
                while (totalMoves >= forwardTasks.Count) forwardTasks.Add(new TaskListNormal(roomCount + 1));
                forwardTasks[totalMoves].Add(currentTask);
              }
            }
          }

          // --- nächsten Current-State nachladen (für Debugging) ---
          if (taskList.Count == 0 && forwardIndex + 1 < forwardTasks.Count)
          {
            taskList = forwardTasks[++forwardIndex];
          }
          taskList.PeekFirst(currentTask);
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    /// <summary>
    /// berechnet eine Variante durch und gibt die Anzahl der Laufschritte zurück (oder -1, wenn die Variante ungültig ist)
    /// </summary>
    /// <param name="task">Aufgabe mit den Zuständen, welche aktualisiert werden soll</param>
    /// <param name="room">aktueller Raum für die Berechnung</param>
    /// <param name="variant">Variante im Raum, welche verwendet werden soll</param>
    /// <param name="pushes">Anzahl der durchgeführten Kistenverschiebungen</param>
    /// <returns>Anzahl der erkannten Laufschritte oder -1 wenn die Variante ungültig ist</returns>
    static int ResolveVariant(ulong[] task, Room room, ulong variant, out int pushes)
    {
      Debug.Assert(variant < room.variantList.Count);
      var variantData = room.variantList.GetData(variant);
      pushes = (int)(uint)variantData.pushes;

      foreach (var boxPortal in variantData.boxPortalsIndices) // Kisten wurden durch benachbarte Portale geschoben?
      {
        var oPortal = room.outgoingPortals[boxPortal];
        ulong oldState = task[oPortal.toRoom.roomIndex];
        ulong newState = oPortal.stateBoxSwap.Get(oldState);
        if (oldState == newState) return -1; // Kiste kann nicht durch ein benachbartes Portal geschoben werden -> gesamte Variante ungültig
        task[oPortal.toRoom.roomIndex] = newState; // neuen Zustand im benachbarten Raum setzen
      }

      task[room.roomIndex] = variantData.newState; // neuen Zustand des eigenen Raumes setzen

      if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
      {
        for (int i = 0; i < task.Length - 1; i++) if (task[i] != 0) return -1; // unvollständiges Ende gefunden
        return (int)(uint)variantData.moves; // Ende erreicht!
      }
      else
      {
        var oPortal = room.outgoingPortals[variantData.playerPortalIndex];
        if (!oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]).Any()) return -1; // keine gültigen Varianten gefunden?
      }

      Debug.Assert(variantData.moves < int.MaxValue);
      return (int)(uint)variantData.moves;
    }

    /// <summary>
    /// verarbeitet eine Varianten und gibt alle gültigen Sub-Varianten zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche aktualisiert werden sollen</param>
    /// <returns>Enumerable der Anzahl der Moves pro Variante, gleichzeitig wird task erneuert</returns>
    IEnumerable<int> VariantMoves(ulong[] task)
    {
      uint roomIndex = (uint)(task[roomCount] >> VariantBits);
      var room = rooms[roomIndex];
      ulong variant = task[roomCount] & VariantMask;

      Debug.Assert(variant < room.variantList.Count);
      var variantData = room.variantList.GetData(variant);

      if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
      {
        throw new NotImplementedException();
      }

      var oPortal = room.outgoingPortals[variantData.playerPortalIndex];

      // todo: Rückgabe-Varianten sammeln und vorher von Dopplern befreien

      using (var tmpHash = new HashCrcNormal()) // temporäre Hashtable zum merken bereits verarbeiteter Varianten
      using (var tmpMoveTasks = new TaskListNormal((uint)task.Length)) // temporäre Aufgaben-Liste für die Lauf-Varianten, welche noch geprüft werden müssen
      {
        // --- original Zustände sichern ---
        var originStates = new ulong[task.Length];
        Array.Copy(task, originStates, task.Length);

        foreach (var vId in oPortal.variantStateDict.GetVariants(originStates[oPortal.toRoom.roomIndex])) // alle Sub-Varianten durcharbeiten
        {
          Array.Copy(originStates, task, task.Length);
          task[task.Length - 1] = (ulong)oPortal.toRoom.roomIndex << VariantBits | vId;
          int pushes;
          int moves = ResolveVariant(task, oPortal.toRoom, vId, out pushes);
          ulong crc = Crc64.Get(task);
          ulong crcMoves = tmpHash.Get(crc, ulong.MaxValue);
          if (crcMoves <= (uint)moves) continue; // Variante schon bekannt (bzw. bereits eine Bessere gefunden)
          if (crcMoves == ulong.MaxValue) tmpHash.Add(crc, (uint)moves); else tmpHash.Update(crc, (uint)moves);

          if (pushes > 0) // Variante mit Kistenverschiebung gefunden 
          {
            yield return moves; // direkt zurück geben
          }
          else
          {
            tmpMoveTasks.Add(task); // reine Laufvariante als Aufgabe weiter durchsuchen lassen
          }
        }

        // --- alle gesammelten Laufvarianten weiter durchsuchen, bis Varianten mit Kistenverschiebungen erkannt wurden
        for (; tmpMoveTasks.Count > 0; )
        {
          tmpMoveTasks.FetchFirst(task);

          ulong baseCrc = Crc64.Get(task);
          int baseMoves = (int)(uint)tmpHash.Get(baseCrc);
          Debug.Assert(baseMoves > 0);

          Array.Copy(task, originStates, task.Length);
          roomIndex = (uint)(task[roomCount] >> VariantBits);
          room = rooms[roomIndex];
          variant = task[roomCount] & VariantMask;
          variantData = room.variantList.GetData(variant);

          if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
          {
            throw new NotImplementedException();
          }

          oPortal = room.outgoingPortals[variantData.playerPortalIndex];

          foreach (var vId in oPortal.variantStateDict.GetVariants(originStates[oPortal.toRoom.roomIndex])) // alle Sub-Varianten durcharbeiten
          {
            Array.Copy(originStates, task, task.Length);
            task[task.Length - 1] = (ulong)oPortal.toRoom.roomIndex << VariantBits | vId;
            int pushes;
            int moves = ResolveVariant(task, oPortal.toRoom, vId, out pushes) + baseMoves;
            ulong crc = Crc64.Get(task);
            ulong crcMoves = tmpHash.Get(crc, ulong.MaxValue);
            if (crcMoves <= (uint)moves) continue; // Variante schon bekannt (bzw. bereits eine Bessere gefunden)
            if (crcMoves == ulong.MaxValue) tmpHash.Add(crc, (uint)moves); else tmpHash.Update(crc, (uint)moves);

            if (pushes > 0) // Variante mit Kistenverschiebung gefunden 
            {
              yield return moves; // direkt zurück geben
            }
            else
            {
              tmpMoveTasks.Add(task); // reine Laufvariante als Aufgabe weiter durchsuchen lassen
            }
          }
        }
      }
    }

    /// <summary>
    /// ermittelt die vorhergehende Spielerposition
    /// </summary>
    int PlayerPosFrom
    {
      get
      {
        uint roomIndex = (uint)(currentTask[roomCount] >> VariantBits);
        ulong variant = currentTask[roomCount] & VariantMask;

        switch (solveState)
        {
          case SolveState.AddStarts:
          case SolveState.ScanForward:
          {
            var variantData = rooms[roomIndex].variantList.GetData(variant);
            if (variantData.playerPortalIndex == uint.MaxValue) return rooms[roomIndex].fieldPosis.First();
            return rooms[roomIndex].outgoingPortals[variantData.playerPortalIndex].fromPos;
          }
          default: return roomNetwork.field.PlayerPos;
        }
      }
    }

    /// <summary>
    /// gibt die aktuelle Spielerposition zurück
    /// </summary>
    public int CurrentPlayerPos
    {
      get
      {
        uint roomIndex = (uint)(currentTask[roomCount] >> VariantBits);
        ulong variant = currentTask[roomCount] & VariantMask;

        switch (solveState)
        {
          case SolveState.AddStarts:
          case SolveState.ScanForward:
          {
            var variantData = rooms[roomIndex].variantList.GetData(variant);
            if (variantData.playerPortalIndex == uint.MaxValue) return rooms[roomIndex].fieldPosis.First();
            return rooms[roomIndex].outgoingPortals[variantData.playerPortalIndex].toPos;
          }
          default: return roomNetwork.field.PlayerPos;
        }
      }
    }

    /// <summary>
    /// gibt die aktuelle Kisten-Positionen zurück
    /// </summary>
    public int[] CurrentBoxIndices
    {
      get
      {
        switch (solveState)
        {
          case SolveState.Init: return Enumerable.Range(0, roomNetwork.field.Width * roomNetwork.field.Height).Where(roomNetwork.field.IsBox).ToArray();
          default:
          {
            var boxIndices = new List<int>();

            for (int r = 0; r < roomCount; r++)
            {
              boxIndices.AddRange(rooms[r].stateList.Get(currentTask[r]));
            }

            int playerPosTo = CurrentPlayerPos;
            for (int i = 0; i < boxIndices.Count; i++)
            {
              if (boxIndices[i] == playerPosTo)
              {
                int newboxPos = playerPosTo + playerPosTo - PlayerPosFrom;
                if (boxIndices.All(pos => pos != newboxPos)) boxIndices[i] = newboxPos; // Kiste auf die neue Position verschieben (nur wenn vorher dort keine Kiste stand)
              }
            }
            return boxIndices.ToArray();
          }
        }
      }
    }

    /// <summary>
    /// gibt den aktuellen Such-Status zurück
    /// </summary>
    /// <returns>lesbarer Inhalt vom Such-Status</returns>
    public override string ToString()
    {
      var sb = new StringBuilder("\r\n");
      sb.AppendLine("  Hash: " + hashTable.Count.ToString("N0"));
      ulong totalTasks = 0;
      for (int moveIndex = forwardIndex; moveIndex < forwardTasks.Count; moveIndex++) totalTasks += forwardTasks[moveIndex].Count;
      sb.AppendLine(" Tasks: " + totalTasks.ToString("N0")).AppendLine();
      sb.AppendLine(" Moves: " + forwardIndex.ToString("N0")).AppendLine();
      sb.AppendLine(" State: " + solveState).AppendLine();

      uint roomIndex = (uint)(currentTask[roomCount] >> VariantBits);
      ulong variant = currentTask[roomCount] & VariantMask;
      switch (solveState)
      {
        case SolveState.AddStarts:
        {
          sb.AppendLine(string.Format(" Add-Starts: {0:N0} / {1:N0}", (currentTask[roomCount] & VariantMask) + 1, rooms[roomIndex].startVariantCount)).AppendLine();
        } break;
      }

      if (solveState != SolveState.Init)
      {
        sb.AppendLine(" Room: " + roomIndex);
        sb.AppendLine(" V-ID: " + variant);
        sb.AppendLine(" Path: " + rooms[roomIndex].variantList.GetData(variant).path);
        sb.AppendLine();

        for (int moveIndex = forwardIndex; moveIndex < forwardTasks.Count; moveIndex++)
        {
          sb.AppendLine(" [" + moveIndex.ToString("N0") + "]: " + forwardTasks[moveIndex].Count.ToString("N0"));
        }
      }

      return sb.ToString();
    }
  }
}
