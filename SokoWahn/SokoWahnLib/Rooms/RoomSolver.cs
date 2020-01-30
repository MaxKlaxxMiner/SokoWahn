using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable RedundantIfElseBlock
// ReSharper disable MemberCanBePrivate.Global

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
    /// optionale Methode zum Anzeigen im Debug-Modus
    /// </summary>
    readonly Action debugDisplay;

    /// <summary>
    /// merkt sich die Position innerhalb einer Aufgabe, wo die Variante gespeichert wurde
    /// </summary>
    readonly uint taskVariantOfs;

    /// <summary>
    /// merkt sich die Position innerhalb einer Aufgabe, wo die Raumnummer und das eingehende Portal gespeichert wurde
    /// </summary>
    readonly uint taskRoomPortalOfs;

    /// <summary>
    /// merkt sich die gesamte Länge einer Aufgabe (Zustände[] + Variante + (Raum-Nummer und eingehende Portal-Nummer)
    /// </summary>
    readonly uint taskLength;

    #region # // --- Aufgaben-Hilfsmethoden ---
    /// <summary>
    /// setzt die Variante innerhalb einer Aufgabe
    /// </summary>
    /// <param name="task">Aufgabe, welche bearbeitet werden soll</param>
    /// <param name="variant">Variante, welche gesetzt werden soll</param>
    /// <param name="roomIndex">Raum-Nummer, welche gesetzt werden soll</param>
    /// <param name="iPortalIndex">Portal-Nummer für das eingehende Portal oder uint.MaxValue, wenn es sich um eine Start-Variante handelt</param>
    void SetTaskInfos(ulong[] task, ulong variant, uint roomIndex, uint iPortalIndex)
    {
      Debug.Assert(task.Length == taskLength);
      Debug.Assert(roomIndex < rooms.Length);
      Debug.Assert(variant < rooms[roomIndex].variantList.Count);
      Debug.Assert((iPortalIndex < rooms[roomIndex].incomingPortals.Length && rooms[roomIndex].incomingPortals[iPortalIndex].variantStateDict.GetVariants(task[roomIndex]).Any(v => v == variant))
                || (iPortalIndex == uint.MaxValue && variant < rooms[roomIndex].startVariantCount));

      task[taskVariantOfs] = variant;
      task[taskRoomPortalOfs] = (ulong)roomIndex << 32 | iPortalIndex;

      Debug.Assert(GetTaskVariant(task) == variant);
      Debug.Assert(GetTaskRoomIndex(task) == roomIndex);
      Debug.Assert(GetTaskPortalIndex(task) == iPortalIndex);
      Debug.Assert(Enumerable.Range(0, rooms.Length).All(i => task[i] < rooms[i].stateList.Count));
    }

    /// <summary>
    /// gibt die gespeicherte Variante einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Variante</returns>
    ulong GetTaskVariant(ulong[] task)
    {
      Debug.Assert(task.Length == taskLength);
      Debug.Assert(task[taskVariantOfs] < rooms[GetTaskRoomIndex(task)].variantList.Count);
      return task[taskVariantOfs];
    }

    /// <summary>
    /// gibt die gespeicherte Raum-Nummer einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Raum-Nummer</returns>
    uint GetTaskRoomIndex(ulong[] task)
    {
      Debug.Assert(task.Length == taskLength);
      Debug.Assert((uint)(task[taskRoomPortalOfs] >> 32) < rooms.Length);
      return (uint)(task[taskRoomPortalOfs] >> 32);
    }

    /// <summary>
    /// gibt die gespeicherte eingehende Portalnummer einer Aufgabe zurück (oder uint.MaxValue, wenn es sich um einen Spielerstart innerhalb des Raumes handelt)
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Portal-Nummer</returns>
    uint GetTaskPortalIndex(ulong[] task)
    {
      Debug.Assert(task.Length == taskLength);
      Debug.Assert((uint)task[taskRoomPortalOfs] == uint.MaxValue
                || (uint)task[taskRoomPortalOfs] < rooms[GetTaskRoomIndex(task)].incomingPortals.Length);
      return (uint)task[taskRoomPortalOfs];
    }

    /// <summary>
    /// gibt den Laufweg der Spielerfigur einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>Enumerable der Spielerpositionen auf dem Spielfeld</returns>
    IEnumerable<int> GetTaskPlayerPath(ulong[] task)
    {
      // todo
      yield return roomNetwork.field.PlayerPos;
    }
    #endregion

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
      if (rooms == null || rooms.Length < 1) throw new ArgumentNullException("roomNetwork");
      taskVariantOfs = (uint)rooms.Length;
      taskRoomPortalOfs = taskVariantOfs + 1;
      taskLength = taskRoomPortalOfs + 1;
      currentTask = new ulong[taskLength];
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
            currentTask[i] = roomNetwork.rooms[i].startState; // Anfang-Zustände übertragen
            if (roomNetwork.rooms[i].startVariantCount > 0)
            {
              if (startRoomIndex >= 0) throw new SokoFieldException("duplicate start-room");
              startRoomIndex = i;
            }
          }
          if (startRoomIndex < 0) throw new SokoFieldException("no start-room");

          SetTaskInfos(currentTask, 0, (uint)startRoomIndex, uint.MaxValue);

          forwardTasks.Add(new TaskListNormal(taskLength)); // erste Aufgaben-Liste für die Start-Züge hinzufügen

          solveState = SolveState.AddStarts;
        } break;
        #endregion

        #region # // --- AddStarts - Anfangswerte als Aufgaben hinzufügen ---
        case SolveState.AddStarts:
        {
          uint startRoomIndex = GetTaskRoomIndex(currentTask);
          ulong variant = GetTaskVariant(currentTask);
          Debug.Assert(GetTaskPortalIndex(currentTask) == uint.MaxValue);

          DebugConsole("Start-Variant " + variant);

          for (; maxTicks > 0; maxTicks--)
          {
            // --- Start-Aufgabe in die Aufgaben-Liste hinzufügen ---
            ulong crc = Crc64.Get(currentTask);
            hashTable.Add(crc, 0);
            forwardTasks[0].Add(currentTask);

            // --- zur nächsten Start-Variante springen ---
            variant++;
            if (variant == rooms[startRoomIndex].startVariantCount) // alle Start-Varianten bereits ermittelt?
            {
              solveState = SolveState.ScanForward;
              if (maxTicks == 0) maxTicks = 1; // ein Arbeitsschritt wieder hinzufügen (für den initialen Scan-Vorgang)
              goto case SolveState.ScanForward;
            }
            SetTaskInfos(currentTask, variant, startRoomIndex, uint.MaxValue);
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
            DebugConsole("Search-Forward [" + forwardIndex + "], remain: " + (taskList.Count + 1).ToString("N0"));

            // --- Aufgabe prüfen und neue Einträge in die Aufgaben-Liste hinzufügen ---
            //todo
            //foreach (int moves in VariantMoves(currentTask))
            //{
            //  int totalMoves = forwardIndex + moves;
            //  ulong crc = Crc64.Get(currentTask);
            //  ulong oldMoves = hashTable.Get(crc, ulong.MaxValue);
            //  if ((uint)totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
            //  {
            //    if (moves == 0 && currentTask.Take(rooms.Length).All(x => x == 0)) // Ende gefunden?
            //    {
            //      throw new NotImplementedException();
            //    }

            //    if (oldMoves == ulong.MaxValue) hashTable.Add(crc, (uint)totalMoves); else hashTable.Update(crc, (uint)totalMoves);

            //    // --- Variante als Aufgabe hinzufügen ---
            //    while (totalMoves >= forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskLength));
            //    forwardTasks[totalMoves].Add(currentTask);
            //  }
            //}
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

    ///// <summary>
    ///// berechnet eine Variante durch, ändert die Kisten-Zustände und gibt die Anzahl der Laufschritte zurück (oder -1, wenn die Variante ungültig ist)
    ///// </summary>
    ///// <param name="task">Aufgabe mit den Zuständen, welche aktualisiert werden sollen</param>
    ///// <param name="room">aktueller Raum für die Berechnung</param>
    ///// <param name="variant">Variante im Raum, welche verwendet werden soll</param>
    ///// <param name="pushes">Anzahl der durchgeführten Kistenverschiebungen</param>
    ///// <returns>Anzahl der erkannten Laufschritte oder -1 wenn die Variante ungültig ist</returns>
    //static int ResolveTaskBoxes_larf(ulong[] task, Room room, ulong variant, out int pushes)
    //{
    //  Debug.Assert(variant < room.variantList.Count);
    //  var variantData = room.variantList.GetData(variant);
    //  pushes = (int)(uint)variantData.pushes;

    //  foreach (var boxPortal in variantData.boxPortalsIndices) // Kisten wurden durch benachbarte Portale geschoben?
    //  {
    //    var oPortal = room.outgoingPortals[boxPortal];
    //    uint roomIndex = oPortal.toRoom.roomIndex;
    //    ulong oldState = task[roomIndex];
    //    ulong newState = oPortal.stateBoxSwap.Get(oldState);
    //    if (oldState == newState) return -1; // Kiste kann nicht durch ein benachbartes Portal geschoben werden -> gesamte Variante ungültig
    //    task[roomIndex] = newState; // neuen Zustand im benachbarten Raum setzen
    //  }

    //  task[room.roomIndex] = variantData.newState; // neuen Zustand des eigenen Raumes setzen

    //  if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
    //  {
    //    for (int i = 0; i < task.Length - 2; i++) if (task[i] != 0) return -1; // unvollständiges Ende gefunden
    //    return (int)(uint)variantData.moves; // Ende erreicht!
    //  }
    //  else
    //  {
    //    var oPortal = room.outgoingPortals[variantData.playerPortalIndex];
    //    if (!oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]).Any()) return -1; // keine gültigen Varianten gefunden?
    //  }

    //  Debug.Assert(variantData.moves < int.MaxValue);
    //  return (int)(uint)variantData.moves;
    //}

    ///// <summary>
    ///// verarbeitet eine Varianten und gibt alle gültigen Sub-Varianten zurück
    ///// </summary>
    ///// <param name="task">Aufgabe, welche aktualisiert werden sollen</param>
    ///// <returns>Enumerable der Anzahl der Moves pro Variante, gleichzeitig wird task erneuert</returns>
    //IEnumerable<int> VariantMoves(ulong[] task)
    //{
    //  uint roomIndex = GetTaskRoomIndex(task);
    //  var room = rooms[roomIndex];
    //  ulong variant = GetTaskVariant(task);

    //  Debug.Assert(variant < room.variantList.Count);
    //  var variantData = room.variantList.GetData(variant);

    //  if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
    //  {
    //    throw new NotImplementedException();
    //  }

    //  var oPortal = room.outgoingPortals[variantData.playerPortalIndex];

    //  // todo: Rückgabe-Varianten sammeln und vorher von Dopplern befreien

    //  using (var tmpHash = new HashCrcNormal()) // temporäre Hashtable zum merken bereits verarbeiteter Varianten
    //  using (var tmpMoveTasks = new TaskListNormal((uint)task.Length)) // temporäre Aufgaben-Liste für die Lauf-Varianten, welche noch geprüft werden müssen
    //  {
    //    // --- original Zustände sichern ---
    //    var originStates = new ulong[task.Length];
    //    Array.Copy(task, originStates, task.Length);

    //    foreach (var vId in oPortal.variantStateDict.GetVariants(originStates[oPortal.toRoom.roomIndex])) // alle Sub-Varianten durcharbeiten
    //    {
    //      Array.Copy(originStates, task, task.Length);
    //      task[task.Length - 1] = (ulong)oPortal.toRoom.roomIndex << VariantBits | vId;
    //      int pushes;
    //      int moves = ResolveTaskBoxes_larf(task, oPortal.toRoom, vId, out pushes);
    //      ulong crc = Crc64.Get(task);
    //      ulong crcMoves = tmpHash.Get(crc, ulong.MaxValue);
    //      if (crcMoves <= (uint)moves) continue; // Variante schon bekannt (bzw. bereits eine Bessere gefunden)
    //      if (crcMoves == ulong.MaxValue) tmpHash.Add(crc, (uint)moves); else tmpHash.Update(crc, (uint)moves);

    //      if (pushes > 0) // Variante mit Kistenverschiebung gefunden 
    //      {
    //        yield return moves; // direkt zurück geben
    //      }
    //      else
    //      {
    //        tmpMoveTasks.Add(task); // reine Laufvariante als Aufgabe weiter durchsuchen lassen
    //      }
    //    }

    //    // --- alle gesammelten Laufvarianten weiter durchsuchen, bis Varianten mit Kistenverschiebungen erkannt wurden
    //    for (; tmpMoveTasks.Count > 0; )
    //    {
    //      tmpMoveTasks.FetchFirst(task);

    //      ulong baseCrc = Crc64.Get(task);
    //      int baseMoves = (int)(uint)tmpHash.Get(baseCrc);
    //      Debug.Assert(baseMoves > 0);

    //      Array.Copy(task, originStates, task.Length);
    //      roomIndex = (uint)(task[roomCount] >> VariantBits);
    //      room = rooms[roomIndex];
    //      variant = task[roomCount] & VariantMask;
    //      variantData = room.variantList.GetData(variant);

    //      if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
    //      {
    //        throw new NotImplementedException();
    //      }

    //      oPortal = room.outgoingPortals[variantData.playerPortalIndex];

    //      foreach (var vId in oPortal.variantStateDict.GetVariants(originStates[oPortal.toRoom.roomIndex])) // alle Sub-Varianten durcharbeiten
    //      {
    //        Array.Copy(originStates, task, task.Length);
    //        task[task.Length - 1] = (ulong)oPortal.toRoom.roomIndex << VariantBits | vId;
    //        int pushes;
    //        int moves = ResolveTaskBoxes_larf(task, oPortal.toRoom, vId, out pushes) + baseMoves;
    //        ulong crc = Crc64.Get(task);
    //        ulong crcMoves = tmpHash.Get(crc, ulong.MaxValue);
    //        if (crcMoves <= (uint)moves) continue; // Variante schon bekannt (bzw. bereits eine Bessere gefunden)
    //        if (crcMoves == ulong.MaxValue) tmpHash.Add(crc, (uint)moves); else tmpHash.Update(crc, (uint)moves);

    //        if (pushes > 0) // Variante mit Kistenverschiebung gefunden 
    //        {
    //          yield return moves; // direkt zurück geben
    //        }
    //        else
    //        {
    //          tmpMoveTasks.Add(task); // reine Laufvariante als Aufgabe weiter durchsuchen lassen
    //        }
    //      }
    //    }
    //  }
    //}

    /// <summary>
    /// gibt die Spielerpositionen der aktuellen Aufgabe zurück
    /// </summary>
    public int[] PlayerPathPosis
    {
      get
      {
        switch (solveState)
        {
          case SolveState.AddStarts:
          case SolveState.ScanForward:
          {
            return GetTaskPlayerPath(currentTask).ToArray();
          }
          default: return new[] { roomNetwork.field.PlayerPos }; // nur Startposition des Spielers zurückgeben
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

            for (int roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
            {
              boxIndices.AddRange(rooms[roomIndex].stateList.Get(currentTask[roomIndex]));
            }

            return boxIndices.ToArray();
          }
        }
      }
    }

    /// <summary>
    /// gibt an, ob die Console verfügbar ist
    /// </summary>
    public static readonly bool IsConsoleApplication = Console.OpenStandardInput() != Stream.Null;

    /// <summary>
    /// gibt das Spielfeld mit der entsprechenden Aufgabe in der Console aus (nur Debug-Modus)
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    [Conditional("DEBUG")]
    public void DebugConsole(string title = null, ulong[] task = null)
    {
      if (!IsConsoleApplication) return;
      if (Console.CursorTop == 0) Console.WriteLine();
      if (!string.IsNullOrWhiteSpace(title)) title = "  --- " + title + " ---\r\n";
      Console.WriteLine(title + ("\r\n" + DebugStr(task)).Replace("\r\n", "\r\n  "));
    }

    /// <summary>
    /// gibt das Spielfeld einer bestimmten Aufgabe zurück
    /// </summary>
    /// <param name="task">optional: Aufgabe, welche angezeigt werden soll</param>
    /// <returns>Spielfeld als Text Standard-Notation</returns>
    public string DebugStr(ulong[] task = null)
    {
      if (task == null) task = currentTask;
      int width = roomNetwork.field.Width;
      int height = roomNetwork.field.Height;
      var outputChars = Enumerable.Range(0, width * height).Select(roomNetwork.field.GetFieldChar).Select(c => c == '$' || c == '@' ? ' ' : (c == '*' || c == '+' ? '.' : c)).ToArray();

      for (int i = 0; i < rooms.Length; i++)
      {
        foreach (int boxPos in rooms[i].stateList.Get(task[i]))
        {
          switch (outputChars[boxPos])
          {
            case ' ': outputChars[boxPos] = '$'; break;
            case '.': outputChars[boxPos] = '*'; break;
            default: throw new Exception("invalid Field at pos " + boxPos + ": " + outputChars[boxPos]);
          }
        }
      }

      uint roomIndex = GetTaskRoomIndex(currentTask);
      uint iPortalIndex = GetTaskPortalIndex(currentTask);
      ulong variant = GetTaskVariant(currentTask);
      var variantData = rooms[roomIndex].variantList.GetData(variant);

      int playerPos = iPortalIndex < uint.MaxValue ? rooms[roomIndex].incomingPortals[iPortalIndex].fromPos : roomNetwork.field.PlayerPos;
      switch (outputChars[playerPos])
      {
        case ' ': outputChars[playerPos] = '@'; break;
        case '.': outputChars[playerPos] = '+'; break;
        default: throw new Exception("player-problem: " + playerPos);
      }

      var linesBefore = Enumerable.Range(0, height).Select(y => new string(outputChars, y * width, width)).ToArray();
      Debug.Assert(linesBefore.Length == height);

      // --- Variante simulieren um den Endstand des Spielfeldes zu erhalten ---
      var tmpField = new SokoField(string.Join("\r\n", linesBefore) + "\r\n");
      if (iPortalIndex < uint.MaxValue) // Eintritt durch das eingehende Portal als ersten durchführen
      {
        char dirChar = rooms[roomIndex].incomingPortals[iPortalIndex].dirChar;
        if (!tmpField.SafeMove(dirChar)) throw new Exception("invalid move");
      }
      foreach (var dirChar in variantData.path)
      {
        if (!tmpField.SafeMove(dirChar)) throw new Exception("invalid move");
      }

      var linesAfter = tmpField.GetText().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      Debug.Assert(linesAfter.Length == height);

      return string.Join("\r\n", Enumerable.Range(0, height).Select(y => linesBefore[y] + (y == height - 1 ? "  -->  " : "       ") + linesAfter[y])) + "\r\n\r\n";
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

      uint roomIndex = GetTaskRoomIndex(currentTask);
      ulong variant = GetTaskVariant(currentTask);
      switch (solveState)
      {
        case SolveState.AddStarts:
        {
          sb.AppendLine(string.Format(" Add-Starts: {0:N0} / {1:N0}", variant + 1, rooms[roomIndex].startVariantCount)).AppendLine();
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
