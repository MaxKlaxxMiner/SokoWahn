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
// ReSharper disable MemberCanBePrivate.Local

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
    public readonly Room[] rooms;

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
    public ulong GetTaskVariant(ulong[] task)
    {
      Debug.Assert(task.Length == taskLength);
      Debug.Assert(task[taskVariantOfs] < rooms[GetTaskRoomIndex(task)].variantList.Count || task[taskVariantOfs] == ulong.MaxValue);
      return task[taskVariantOfs];
    }

    /// <summary>
    /// gibt die gespeicherte Raum-Nummer einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Raum-Nummer</returns>
    public uint GetTaskRoomIndex(ulong[] task)
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
    public uint GetTaskPortalIndex(ulong[] task)
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
    public IEnumerable<int> GetTaskPlayerPath(ulong[] task)
    {
      uint roomIndex = GetTaskRoomIndex(task);
      uint iPortalIndex = GetTaskPortalIndex(task);
      ulong variant = GetTaskVariant(task);
      if (variant == ulong.MaxValue)
      {
        yield return roomNetwork.field.PlayerPos;
        yield break;
      }
      var variantData = rooms[roomIndex].variantList.GetData(variant);
      int playerPos = iPortalIndex < uint.MaxValue ? rooms[roomIndex].incomingPortals[iPortalIndex].fromPos : roomNetwork.field.PlayerPos;

      yield return playerPos;

      if (variantData.path != null)
      {
        foreach (var c in variantData.path)
        {
          switch (c)
          {
            case 'l': playerPos--; break;
            case 'r': playerPos++; break;
            case 'u': playerPos -= roomNetwork.field.Width; break;
            case 'd': playerPos += roomNetwork.field.Width; break;
            default: throw new Exception("invalid path-char: '" + c + "'");
          }
          yield return playerPos;
        }
      }
    }

    /// <summary>
    /// aktualisiert die Kisten-Zustände innerhalb einer Aufgabe durch die Portale
    /// </summary>
    /// <param name="task">Aufgabe, welche bearbeitet werden soll</param>
    /// <param name="room">Room, welcher betroffen ist</param>
    /// <param name="boxPortalsIndices">Nummern der Portale, wodurch Kisten geschoben wurden</param>
    /// <returns>true, wenn der Vorgang erfolgreich war</returns>
    static bool ResolveTaskPortalBoxes(ulong[] task, Room room, uint[] boxPortalsIndices)
    {
      foreach (var boxPortal in boxPortalsIndices) // Kisten durch benachbarte Portale schieben
      {
        throw new NotImplementedException("todo: check debug");

        var oPortal = room.outgoingPortals[boxPortal];
        uint roomIndex = oPortal.toRoom.roomIndex;
        ulong oldState = task[roomIndex];
        ulong newState = oPortal.stateBoxSwap.Get(oldState);
        if (oldState == newState) return false; // Kiste kann nicht vom banachbarten Raum aufgenommen werden -> gesamte Variante ungültig

        task[roomIndex] = newState; // neuen Zustand im benachbarten Raum setzen
      }

      return true;
    }

    /// <summary>
    /// prüft, ob eine bestimmte Aufgabe gültig ist
    /// </summary>
    /// <param name="task">Aufgabe, welche geprüft werden soll</param>
    /// <param name="tmpTask">temporäres Array zum Zwischenspeichern einer Aufgabe</param>
    /// <param name="room">Raum, welcher betroffen ist</param>
    /// <param name="variant">Variante, welche geprüft werden soll</param>
    /// <returns>true, wenn die Aufgabe gültig ist (sonst false)</returns>
    static bool CheckTask(ulong[] task, ulong[] tmpTask, Room room, ulong variant)
    {
      var variantData = room.variantList.GetData(variant);

      if (variantData.boxPortalsIndices.Length > 0)
      {
        throw new NotImplementedException("todo: check debug");

        Array.Copy(task, tmpTask, task.Length); // Aufgabe temporär kopieren, damit Zustände geändert werden dürfen

        // Kisten-Zustände anpassen und prüfen
        if (!ResolveTaskPortalBoxes(tmpTask, room, variantData.boxPortalsIndices)) return false;

        if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
        {
          for (int i = 0; i < tmpTask.Length - 2; i++) if (tmpTask[i] != 0) return false; // unvollständiges Ende gefunden
          return true; // Ende erreicht!
        }
        else
        {
          var oPortal = room.outgoingPortals[variantData.playerPortalIndex];
          return oPortal.variantStateDict.GetVariants(tmpTask[oPortal.toRoom.roomIndex]).Any(); // mindestens eine gültige Nachfolge-Variante gefunden?
        }
      }
      else
      {
        if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung erreicht?
        {
          throw new NotImplementedException("todo: check debug");

          for (int i = 0; i < task.Length - 2; i++) if (task[i] != 0) return false; // unvollständiges Ende gefunden
          return true; // Ende erreicht!
        }
        else
        {
          var oPortal = room.outgoingPortals[variantData.playerPortalIndex];
          return oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]).Any(); // mindestens eine gültige Nachfolge-Variante gefunden?
        }
      }
    }

    /// <summary>
    /// Ergebnis-Informationen einer berechneten Variante
    /// </summary>
    struct TaskVariantInfo
    {
      /// <summary>
      /// Anzahl der gemachten Laufschritte
      /// </summary>
      public ulong moves;
      /// <summary>
      /// Anzahl der durchgeführten Kistenverschiebungen
      /// </summary>
      public ulong pushes;
      /// <summary>
      /// berechnete Prüfsumme
      /// </summary>
      public ulong crc;
      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="moves">Anzahl der gemachten Laufschritte</param>
      /// <param name="pushes">Anzahl der durchgeführten Kistenverschiebungen</param>
      /// <param name="crc">berechnete Prüfsumme oder 0, wenn das gesamte Spielfeld bereits gelöst wurde</param>
      public TaskVariantInfo(ulong moves, ulong pushes, ulong crc = 0)
      {
        this.moves = moves;
        this.pushes = pushes;
        this.crc = crc;
      }
    }

    /// <summary>
    /// verarbeitet eine Aufgabe und berechnet daraus weitere neue Aufgaben
    /// </summary>
    /// <param name="task">Aufgabe, welche abgearbeitet werden soll</param>
    /// <returns>Enumerable der neuen Aufgaben</returns>
    IEnumerable<TaskVariantInfo> ResolveTaskVariants(ulong[] task)
    {
      uint roomIndex = GetTaskRoomIndex(task);
      var room = rooms[roomIndex];

      Debug.Assert(GetTaskVariant(task) < room.variantList.Count);
      var variantData = room.variantList.GetData(GetTaskVariant(task));

      if (!ResolveTaskPortalBoxes(task, room, variantData.boxPortalsIndices))
      {
        throw new Exception("invalid task (resolve portal-boxes)");
      }

      task[room.roomIndex] = variantData.newState; // neuen Zustand des eigenen Raumes setzen

      if (variantData.playerPortalIndex == uint.MaxValue) // End-Stellung schon erreicht?
      {
        throw new NotImplementedException("todo: check debug");

        for (int i = 0; i < task.Length - 2; i++) if (task[i] != 0) throw new Exception("invalid task (end-states)");

        yield return new TaskVariantInfo(variantData.moves, variantData.pushes);
        yield break;
      }

      using (var tmpMoveTasks = new TaskListNormal(taskLength))
      {
        var oPortal = room.outgoingPortals[variantData.playerPortalIndex];

        // --- Sub-Varianten durcharbeiten ---
        foreach (ulong variant in oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]))
        {
          //SetTaskInfos(task, variant, oPortal.toRoom.roomIndex, variantData.playerPortalIndex);
          //variantData = oPortal.toRoom.variantList.GetData(variant);
        }
      }
    }

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

    //}


    //  // todo: Rückgabe-Varianten sammeln und vorher von Dopplern befreien

    //  using (var tmpHash = new HashCrcNormal()) // temporäre Hashtable zum merken bereits verarbeiteter Varianten
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
    public readonly ulong[] currentTask;

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
          currentTask[taskVariantOfs] = ulong.MaxValue;

          forwardTasks.Add(new TaskListNormal(taskLength)); // erste Aufgaben-Liste für die Start-Züge hinzufügen

          solveState = SolveState.AddStarts;
        } break;
        #endregion

        #region # // --- AddStarts - Anfangswerte als Aufgaben hinzufügen ---
        case SolveState.AddStarts:
        {
          uint startRoomIndex = GetTaskRoomIndex(currentTask);
          var room = rooms[startRoomIndex];
          ulong variant = GetTaskVariant(currentTask) + 1;
          Debug.Assert(GetTaskPortalIndex(currentTask) == uint.MaxValue);

          var tmpTask = new ulong[taskLength];

          for (; maxTicks > 0; maxTicks--)
          {
            if (variant == room.startVariantCount) // alle Start-Varianten bereits ermittelt?
            {
              solveState = SolveState.ScanForward;
              maxTicks = Math.Max(1, maxTicks);
              goto case SolveState.ScanForward;
            }

            // --- Start-Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            SetTaskInfos(currentTask, variant, startRoomIndex, uint.MaxValue);

            if (CheckTask(currentTask, tmpTask, room, variant))
            {
              forwardTasks[0].Add(currentTask);
              hashTable.Add(Crc64.Get(currentTask), 0);
              DebugConsole("Start-Variant " + variant);
            }
            else
            {
              DebugConsole("Start-Variant " + variant + " (skipped)");
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
            DebugConsole("Search-Forward [" + forwardIndex + "], remain: " + (taskList.Count + 1).ToString("N0"));

            // --- Aufgabe abarbeiten und neue Einträge in die Aufgaben-Liste hinzufügen ---
            foreach (var taskInfo in ResolveTaskVariants(currentTask))
            {
              throw new NotImplementedException("todo: debug check");

              ulong totalMoves = (uint)forwardIndex + taskInfo.moves;
              if (taskInfo.crc == 0)
              {
                throw new NotImplementedException(); // Ziel schon erreicht?
              }
              ulong oldMoves = hashTable.Get(taskInfo.crc, ulong.MaxValue);

              if (totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
              {
                if (oldMoves == ulong.MaxValue) hashTable.Add(taskInfo.crc, totalMoves); else hashTable.Update(taskInfo.crc, totalMoves);

                // --- Variante als neue Aufgabe hinzufügen ---
                while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskLength));
                forwardTasks[(int)(uint)totalMoves].Add(currentTask);
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
      int moves = 0;
      if (iPortalIndex < uint.MaxValue) // Eintritt durch das eingehende Portal als ersten durchführen
      {
        char dirChar = rooms[roomIndex].incomingPortals[iPortalIndex].dirChar;
        if (!tmpField.SafeMove(dirChar)) throw new Exception("invalid move");
        moves++;
      }
      foreach (var dirChar in variantData.path)
      {
        if (!tmpField.SafeMove(dirChar)) throw new Exception("invalid move");
        moves++;
      }

      var linesAfter = tmpField.GetText().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      Debug.Assert(linesAfter.Length == height);

      var linesFill = new string[linesAfter.Length];
      for (int i = 0; i < linesFill.Length; i++) linesFill[i] = "       ";
      string tmp = "[" + moves + "]";
      while (tmp.Length < 7) { tmp = " " + tmp; if (tmp.Length < 7) tmp += " "; }
      linesFill[0] = tmp;
      linesFill[linesFill.Length - 1] = "  -->  ";

      return string.Join("\r\n", Enumerable.Range(0, height).Select(y => linesBefore[y] + linesFill[y] + linesAfter[y])) + "\r\n\r\n";
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

      if (variant < ulong.MaxValue)
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
