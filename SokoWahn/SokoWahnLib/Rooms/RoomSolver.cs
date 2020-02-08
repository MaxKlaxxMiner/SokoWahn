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
// ReSharper disable ConvertToConstant.Global

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
    readonly Action actionDebugDisplay;

    /// <summary>
    /// merkt sich die Position innerhalb einer Aufgabe, wo die Raumnummer und das eingehende Portal gespeichert wurde
    /// </summary>
    readonly uint taskRoomPortalOfs;

    /// <summary>
    /// Anzahl der zusätzlichen Werte innerhalb einer Aufgabe
    /// </summary>
    const uint TaskInfoValues = 1;

    /// <summary>
    /// merkt sich die gesamte Länge einer Aufgabe: Zustände[] + (Raum-Nummer und eingehende Portal-Nummer)
    /// </summary>
    readonly uint taskSize;

    #region # // --- Aufgaben-Hilfsmethoden ---
    /// <summary>
    /// setzt die Basis-Infos innerhalb einer Aufgabe
    /// </summary>
    /// <param name="task">Aufgabe, welche angepasst werden soll</param>
    /// <param name="roomIndex">Raum-Nummer, welche gesetzt werden soll</param>
    /// <param name="iPortalIndex">eingehende Portal-Nummer des Raumes</param>
    public void SetTaskInfos(ulong[] task, uint roomIndex, uint iPortalIndex)
    {
      Debug.Assert(task.Length == taskSize);
      Debug.Assert(Enumerable.Range(0, rooms.Length).All(i => task[i] < rooms[i].stateList.Count));
      Debug.Assert(roomIndex < rooms.Length);
      Debug.Assert(iPortalIndex < rooms[roomIndex].incomingPortals.Length || iPortalIndex == uint.MaxValue);

      task[taskRoomPortalOfs] = (ulong)roomIndex << 32 | iPortalIndex;

      Debug.Assert(GetTaskRoomIndex(task) == roomIndex);
      Debug.Assert(GetTaskPortalIndex(task) == iPortalIndex);
    }

    /// <summary>
    /// gibt die gespeicherte Raum-Nummer einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Raum-Nummer</returns>
    public uint GetTaskRoomIndex(ulong[] task)
    {
      Debug.Assert(task.Length == taskSize);
      Debug.Assert((uint)(task[taskRoomPortalOfs] >> 32) < rooms.Length);
      return (uint)(task[taskRoomPortalOfs] >> 32);
    }

    /// <summary>
    /// gibt den Raum einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>Raum, welcher aufgefragt wurde</returns>
    public Room GetTaskRoom(ulong[] task)
    {
      return rooms[GetTaskRoomIndex(task)];
    }

    /// <summary>
    /// gibt die gespeicherte eingehende Portalnummer einer Aufgabe zurück (oder uint.MaxValue, wenn es sich um einen Spielerstart innerhalb des Raumes handelt)
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Portal-Nummer</returns>
    public uint GetTaskPortalIndex(ulong[] task)
    {
      Debug.Assert(task.Length == taskSize);
      Debug.Assert((uint)task[taskRoomPortalOfs] == uint.MaxValue
                || (uint)task[taskRoomPortalOfs] < rooms[GetTaskRoomIndex(task)].incomingPortals.Length);
      return (uint)task[taskRoomPortalOfs];
    }

    /// <summary>
    /// gibt den Laufweg der Spielerfigur einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <param name="variant">Variante, welche abgefragt werden soll</param>
    /// <returns>Enumerable der Spielerpositionen auf dem Spielfeld</returns>
    public IEnumerable<int> GetTaskPlayerPath(ulong[] task, ulong variant)
    {
      uint roomIndex = GetTaskRoomIndex(task);
      uint iPortalIndex = GetTaskPortalIndex(task);
      if (variant == ulong.MaxValue)
      {
        yield return roomNetwork.field.PlayerPos;
        yield break;
      }

      var room = rooms[roomIndex];
      var variantData = room.variantList.GetData(variant);
      int playerPos;

      if (iPortalIndex < uint.MaxValue)
      {
        var iPortal = room.incomingPortals[iPortalIndex];
        yield return iPortal.fromPos;
        playerPos = iPortal.toPos;
      }
      else
      {
        playerPos = roomNetwork.field.PlayerPos; // globale Start-Position
      }

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
    /// erstellt eine Kopie einer Aufgabe und gibt diese zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche dupliziert werden soll</param>
    /// <returns>fertig kopierte Aufgabe</returns>
    static ulong[] TaskClone(ulong[] task)
    {
      var result = new ulong[task.Length];
      for (int i = 0; i < task.Length; i++) result[i] = task[i];
      return result;
    }

    /// <summary>
    /// kopierte eine Aufgabe
    /// </summary>
    /// <param name="srcTask">Aufgabe, welche kopierte werden soll</param>
    /// <param name="dstTask">Ziel, wohin die Aufgabe kopiert werden soll</param>
    static void TaskCopy(ulong[] srcTask, ulong[] dstTask)
    {
      if (srcTask == null) throw new NullReferenceException("srcTask");
      if (dstTask == null) throw new NullReferenceException("dstTask");
      if (srcTask.Length != dstTask.Length) throw new IndexOutOfRangeException("srcTask.Length != dstTask.Length");
      for (int i = 0; i < srcTask.Length; i++) dstTask[i] = srcTask[i];
    }


    #region # struct TaskVariantInfo // Ergebnis-Information eines berechneten Sub-Zustandes
    /// <summary>
    /// Ergebnis-Information eines berechneten Sub-Zustandes
    /// </summary>
    struct TaskVariantInfo
    {
      /// <summary>
      /// Anzahl der gemachten Laufschritte um diesen Zustand zu erreichen
      /// </summary>
      public readonly ulong moves;
      /// <summary>
      /// Anzahl der durchgeführten Kistenverschiebungen
      /// </summary>
      public readonly ulong pushes;
      /// <summary>
      /// berechnete Prüfsumme
      /// </summary>
      public readonly ulong crc;
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

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { moves, pushes, crc }.ToString();
      }
    }
    #endregion

    /// <summary>
    /// verarbeitet die Variante einer bestimmte Aufgabe und gibt alle möglichen folgenden Aufgaben zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche verarbeitet werden soll</param>
    /// <param name="variant">Variante der Aufgabe, welche verarbeitet werden soll</param>
    /// <param name="outputTask">Rückgabe der neuen Aufgabe (wird pro Enumerable-Element geändert)</param>
    /// <returns>Enumerable der neuen Aufgaben</returns>
    IEnumerable<TaskVariantInfo> ResolveTask(ulong[] task, ulong variant, ulong[] outputTask)
    {
      uint startRoomIndex = GetTaskRoomIndex(task);
      uint startPortalIndex = GetTaskPortalIndex(task);
      ulong startCrc = Crc64.Start.Crc64Update(startRoomIndex).Crc64Update(startPortalIndex).Crc64Update(variant);

      // --- Liste mit allen Varianten erstellen, welche per Laufschritte direkt erreichbar sind ---
      var list = Enumerable.Repeat(new
      {
        moves = 0UL,                     // Anzahl der bisher gemachten Laufschritte
        roomIndex = startRoomIndex,      // Raum, welcher betroffen ist
        iPortalIndex = startPortalIndex, // eingehendes Portal, welches betroffen ist (uint.MaxValue = kein Portal/Startvariante)
        variant,                         // Variante, welche verarbeitet wird
        crc = startCrc                   // Prüfsumme aus alle Infos außer Anzahl Laufschritte
      }, 1).ToList();

      var movesDict = new Dictionary<ulong, ulong> { { startCrc, 0UL } }; // Dictionary zum vermeiden von doppelten bzw. ineffizienten Laufwegen (keine Loops)
      var pushDict = new HashSet<ulong>(); // merkt sich alle Prüfsummen der Varianten, wo auch Kistenverschiebungen enthalten sind (nur diese werden zum Schluss als neue Aufgaben zurück gegeben)

      for (int listIndex = 0; listIndex < list.Count; listIndex++)
      {
        var step = list[listIndex];
        var room = rooms[step.roomIndex];
        var variantData = room.variantList.GetData(step.variant);
        Debug.Assert(variantData.oldState == task[room.roomIndex]);

        if (variantData.pushes > 0) // fertige Variante mit Kistenverschiebungen gefunden -> kann nicht weiter durchsucht werden
        {
          DebugConsoleV("Move-Step " + (listIndex + 1) + " / " + list.Count + " (finish)", task, step.variant, step.roomIndex, step.iPortalIndex);
          pushDict.Add(step.crc);
          continue;
        }
        else
        {
          DebugConsoleV("Move-Step " + (listIndex + 1) + " / " + list.Count, task, step.variant, step.roomIndex, step.iPortalIndex);
        }
        Debug.Assert(variantData.oPortalIndexBoxes.Length == 0);

        ulong toMoves = step.moves + variantData.moves;
        var toPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
        var toRoom = toPortal.toRoom;
        ulong toCrc = Crc64.Start.Crc64Update(toRoom.roomIndex).Crc64Update(toPortal.iPortalIndex);
        foreach (var toVariant in toPortal.variantStateDict.GetVariants(task[toRoom.roomIndex]))
        {
          ulong crc = toCrc.Crc64Update(toVariant);
          ulong oldMoves;
          if (!movesDict.TryGetValue(crc, out oldMoves)) oldMoves = ulong.MaxValue;
          if (oldMoves <= toMoves) continue; // diese oder eine bessere Version schon bekannt? -> überspringen

          if (oldMoves == ulong.MaxValue) movesDict.Add(crc, toMoves); else movesDict[crc] = toMoves;

          list.Add(new
          {
            moves = toMoves,
            toRoom.roomIndex,
            toPortal.iPortalIndex,
            variant = toVariant,
            crc
          });
        }
      }

      // --- alle gesammelten Varianten durchsuchen und nur gültige Aufgaben zurück geben, welche Kistenverschiebungen enthalten ---
      foreach (var step in list)
      {
        if (step.moves > movesDict[step.crc]) continue; // eine andere Variante mit kürzerer Laufstrecke ist bekannt? -> diese hier überspringen
        if (!pushDict.Contains(step.crc)) continue; // Variante enthält keine Kistenverschiebungen? -> überspringen

        var room = rooms[step.roomIndex];
        var variantData = room.variantList.GetData(step.variant);
        Debug.Assert(variantData.pushes > 0);
        Debug.Assert(variantData.oldState == task[room.roomIndex]);

        ulong toMoves = step.moves + variantData.moves;

        DebugConsoleV("Push-Step (Moves: " + toMoves + ")", task, step.variant, step.roomIndex, step.iPortalIndex);

        TaskCopy(task, outputTask);
        if (ResolveTaskPortalBoxes(outputTask, room, variantData.oPortalIndexBoxes))
        {
          outputTask[room.roomIndex] = variantData.newState;

          if (variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung erreicht?
          {
            bool validEnd = true;
            for (int i = 0; i < outputTask.Length - TaskInfoValues; i++) if (outputTask[i] != 0) { validEnd = false; break; } // unvollständigen Raum gefunden?
            if (!validEnd) continue; // kein gültiges Ende erkannt -> überspringen

            SetTaskInfos(outputTask, room.roomIndex, uint.MaxValue); // im bisherigen Raum verbleibend, kein Portal zum verlassen benutzt
            yield return new TaskVariantInfo(toMoves, variantData.pushes);
          }
          else
          {
            var toPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
            var toRoom = toPortal.toRoom;
            if (toPortal.variantStateDict.GetVariants(outputTask[toRoom.roomIndex]).Any()) // mindestens eine gültige Nachfolge-Variante gefunden?
            {
              yield return new TaskVariantInfo(toMoves, variantData.pushes, Crc64.Get(outputTask));
            }
          }
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
        var oPortal = room.outgoingPortals[boxPortal];
        uint roomIndex = oPortal.toRoom.roomIndex;
        ulong oldState = task[roomIndex];
        ulong newState = oPortal.stateBoxSwap.Get(oldState);
        if (oldState == newState) return false; // Kiste kann nicht vom banachbarten Raum aufgenommen werden -> gesamte Variante ungültig

        task[roomIndex] = newState; // neuen Zustand im benachbarten Raum setzen
      }

      return true;
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
    public readonly ulong[] currentTask;

    /// <summary>
    /// merkt sich die aktuelle Variante, welche momentan abgearbeitet wird
    /// </summary>
    public ulong currentVariant;

    /// <summary>
    /// merkt sich die letzte Variante, welche bei der aktuellen Aufgabe abgearbeitet wird
    /// </summary>
    public ulong currentVariantEnd;

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
      taskRoomPortalOfs = (uint)rooms.Length;
      taskSize = taskRoomPortalOfs + TaskInfoValues;
      currentTask = new ulong[taskSize];
      actionDebugDisplay = debugDisplay ?? (() => { });
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
          if (roomNetwork.rooms.Length > 65535) throw new SokoFieldException("too many rooms (" + roomNetwork.rooms.Length + " > 65535)");
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

          forwardTasks.Add(new TaskListNormal(taskSize)); // erste Aufgaben-Liste für die Start-Züge hinzufügen

          SetTaskInfos(currentTask, (uint)startRoomIndex, uint.MaxValue);
          currentVariant = 0; // Start-Variante setzen, damit es ab Beginn durch das inkrementieren mit 0 beginnt
          currentVariantEnd = roomNetwork.rooms[startRoomIndex].startVariantCount;
          Debug.Assert(currentVariantEnd > 0);

          solveState = SolveState.AddStarts;
        } break;
        #endregion

        #region # // --- AddStarts - Anfangswerte als Aufgaben hinzufügen ---
        case SolveState.AddStarts:
        {
          Debug.Assert(GetTaskPortalIndex(currentTask) == uint.MaxValue);

          var tmpTask = TaskClone(currentTask);

          for (; maxTicks > 0; maxTicks--)
          {
            // --- Start-Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            DebugConsole("Start-Variant");

            foreach (var taskInfo in ResolveTask(currentTask, currentVariant, tmpTask))
            {
              ulong totalMoves = taskInfo.moves;

              if (taskInfo.crc == 0)
              {
                throw new NotImplementedException(); // Ende erreicht?
              }

              while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
              forwardTasks[(int)(uint)totalMoves].Add(tmpTask);
            }

            currentVariant++;

            if (currentVariant == currentVariantEnd) // alle Start-Varianten bereits abgearbeitet?
            {
              solveState = SolveState.ScanForward;
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
            taskList.Dispose();
            forwardIndex++;
            goto case SolveState.ScanForward;
          }

          //for (; maxTicks > 0 && taskList.Count > 0; maxTicks--)
          //{
          //taskList.PeekFirst(currentTask);
          //DebugConsole("Search-Forward [" + forwardIndex + "], remain: " + (taskList.Count + 1).ToString("N0"));

          //// todo: Move+Push Resolver zusammensetzen und Start-Varianten nur mit pushes erlauben

          //// --- Laufwege checken ---
          //var moveFilter = new Dictionary<ulong, ulong>();
          //var moveTicks = ResolveTaskMoveVariants(currentTask, moveFilter, 0).Select(x => new { moves = x, task = currentTask.ToArray(), crc = Crc64.Get(currentTask) }).ToList();
          //for (int tick = 0; tick < moveTicks.Count; tick++)
          //{
          //  ulong moves = moveTicks[tick].moves;
          //  var task = moveTicks[tick].task.ToArray();
          //  moveTicks.AddRange(ResolveTaskMoveVariants(task, moveFilter, moves).Select(x => new { moves = x, task = task.ToArray(), crc = Crc64.Get(task) }));
          //}
          //moveTicks.Sort((x, y) =>
          //{
          //  int dif = x.crc.CompareTo(y.crc);
          //  if (dif == 0) dif = (int)(uint)(x.moves - y.moves);
          //  return dif;
          //});

          //// --- alle Varianten mit Kistenverschiebungen ermitteln ---
          //taskList.FetchFirst(currentTask);
          //ulong lastCrc = 0;
          //foreach (var moveTick in moveTicks)
          //{
          //  if (moveTick.crc == lastCrc) continue;
          //  lastCrc = moveTick.crc;
          //  Array.Copy(moveTick.task, currentTask, currentTask.Length);
          //  foreach (var taskInfo in ResolveTaskPushVariants(currentTask, moveTick.moves))
          //  {
          //    ulong totalMoves = (uint)forwardIndex + taskInfo.moves;
          //    if (taskInfo.crc == 0)
          //    {
          //      throw new NotImplementedException("todo: gesamten Pfad ermitteln und als beste Lösung speichern"); // Ziel erreicht?
          //    }
          //    ulong oldMoves = hashTable.Get(taskInfo.crc, ulong.MaxValue);
          //    if (totalMoves >= oldMoves) continue; // keine neue oder bessere Variante gefunden?
          //    if (oldMoves == ulong.MaxValue) hashTable.Add(taskInfo.crc, totalMoves); else hashTable.Update(taskInfo.crc, totalMoves);

          //    // --- Variante als neue Aufgabe hinzufügen ---
          //    while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
          //    forwardTasks[(int)(uint)totalMoves].Add(currentTask);
          //  }
          //}


          // --- Aufgabe abarbeiten und neue Einträge in die Aufgaben-Liste hinzufügen ---
          //var moveFilter = new Dictionary<ulong, ulong>();
          //foreach (var taskInfo in ResolveTaskVariants(currentTask, moveFilter, 0))
          //{
          //  ulong totalMoves = (uint)forwardIndex + taskInfo.moves;
          //  if (taskInfo.crc == 0)
          //  {
          //    throw new NotImplementedException("todo: gesamten Pfad ermitteln und als beste Lösung speichern"); // Ziel erreicht?
          //  }
          //  ulong oldMoves = hashTable.Get(taskInfo.crc, ulong.MaxValue);

          //  if (totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
          //  {
          //    if (oldMoves == ulong.MaxValue) hashTable.Add(taskInfo.crc, totalMoves); else hashTable.Update(taskInfo.crc, totalMoves);

          //    // --- Variante als neue Aufgabe hinzufügen ---
          //    while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
          //    forwardTasks[(int)(uint)totalMoves].Add(currentTask);
          //  }
          //}
          //}

          //// --- nächsten Current-State nachladen (für Debugging) ---
          //while (taskList.Count == 0 && forwardIndex + 1 < forwardTasks.Count)
          //{
          //  taskList.Dispose();
          //  taskList = forwardTasks[++forwardIndex];
          //}
          //if (taskList.PeekFirst(currentTask)) DebugConsole("Next");
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    #region # public int[] PlayerPathPosis // gibt die Spielerpositionen der aktuellen Aufgabe zurück
    /// <summary>
    /// gibt die Spielerpositionen (Pfad) der aktuellen Aufgabe zurück
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
            return GetTaskPlayerPath(currentTask, currentVariant).ToArray();
          }
          default: return new[] { roomNetwork.field.PlayerPos }; // nur Startposition des Spielers zurückgeben
        }
      }
    }
    #endregion
    #region # public int[] CurrentBoxIndices // gibt die aktuellen Kisten-Positionen zurück
    /// <summary>
    /// gibt die aktuellen Kisten-Positionen zurück
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
    #endregion

    #region # public void DebugConsole(string title = null, ulong[] task = null) // gibt das Spielfeld mit der entsprechenden Aufgabe in der Console aus (nur Debug-Modus)
    /// <summary>
    /// gibt an, ob die Console verfügbar ist
    /// </summary>
    public static readonly bool IsConsoleApplication = Console.OpenStandardInput() != Stream.Null;
    /// <summary>
    /// gibt an, ob zusätzliche Debug-Infos in der Console ausgegeben werden sollen
    /// </summary>
    public static readonly bool DebugConsoleVerbose = true;

    /// <summary>
    /// gibt das Spielfeld mit der entsprechenden Aufgabe in der Console aus (nur Debug-Modus)
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    /// <param name="variant">optionale Variante, welche verwendet werden soll (default: currentVariant)</param>
    /// <param name="roomIndex">optionaler expliziter Raum, welcher verwendet werden soll (default: Raum aus der Aufgabe)</param>
    /// <param name="iPortalIndex">optional explizites Portal, welches verwendet werden soll (default: Portal aus der Aufgabe)</param>
    [Conditional("DEBUG")]
    public void DebugConsole(string title = null, ulong[] task = null, ulong variant = ulong.MaxValue, uint roomIndex = uint.MaxValue, uint iPortalIndex = uint.MaxValue)
    {
      if (!IsConsoleApplication) return;
      if (Console.CursorTop == 0) Console.WriteLine();
      if (!string.IsNullOrWhiteSpace(title))
      {
        if (task == null)
        {
          title = "  --- " + title + (currentVariant < ulong.MaxValue ? " (Variant: " + (currentVariant + 1) + " / " + currentVariantEnd + ")" : "") + " ---\r\n";
        }
        else
        {
          title = "  --- " + title + (variant < ulong.MaxValue ? " (Variant: " + variant + ")" : "") + " ---\r\n";
        }
      }
      Console.WriteLine(title + ("\r\n" + DebugStr(task, variant, roomIndex, iPortalIndex)).Replace("\r\n", "\r\n  "));
    }

    /// <summary>
    /// gleiche wie DebugConsole, jedoch für sehr häufige Ausgaben geeignet
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    /// <param name="variant">optionale Variante, welche verwendet werden soll (default: currentVariant)</param>
    /// <param name="roomIndex">optionaler expliziter Raum, welcher verwendet werden soll (default: Raum aus der Aufgabe)</param>
    /// <param name="iPortalIndex">optional explizites Portal, welches verwendet werden soll (default: Portal aus der Aufgabe)</param>
    [Conditional("DEBUG")]
    public void DebugConsoleV(string title = null, ulong[] task = null, ulong variant = ulong.MaxValue, uint roomIndex = uint.MaxValue, uint iPortalIndex = uint.MaxValue)
    {
      // ReSharper disable once RedundantJumpStatement
      if (!DebugConsoleVerbose) return;
      DebugConsole(title, task, variant, roomIndex, iPortalIndex);
    }
    #endregion

    #region # public string DebugStr(ulong[] task = null) // gibt das Spielfeld einer bestimmten Aufgabe zurück (nebeneinander: vorher/nacher)
    /// <summary>
    /// gibt das Spielfeld einer bestimmten Aufgabe zurück (nebeneinander: vorher/nacher)
    /// </summary>
    /// <param name="task">optional: Aufgabe, welche angezeigt werden soll (default: currentTask)</param>
    /// <param name="variant">optionale Variante, welche verwendet werden soll (default: currentVariant)</param>
    /// <param name="roomIndex">optionaler expliziter Raum, welcher verwendet werden soll (default: Raum aus der Aufgabe)</param>
    /// <param name="iPortalIndex">optional explizites Portal, welches verwendet werden soll (default: Portal aus der Aufgabe)</param>
    /// <returns>Spielfeld als Text Standard-Notation</returns>
    public string DebugStr(ulong[] task = null, ulong variant = ulong.MaxValue, uint roomIndex = uint.MaxValue, uint iPortalIndex = uint.MaxValue)
    {
      if (task == null)
      {
        task = currentTask;
        variant = currentVariant;
      }
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

      if (roomIndex == uint.MaxValue)
      {
        roomIndex = GetTaskRoomIndex(task);
        iPortalIndex = GetTaskPortalIndex(task);
      }
      var variantData = rooms[roomIndex].variantList.GetData(variant);

      int playerPos = iPortalIndex < uint.MaxValue ? rooms[roomIndex].incomingPortals[iPortalIndex].fromPos : roomNetwork.field.PlayerPos;
      switch (outputChars[playerPos])
      {
        case ' ': outputChars[playerPos] = '@'; break;
        case '.': outputChars[playerPos] = '+'; break;
        default: throw new Exception("player-problem: " + playerPos);
      }

      var linesBefore = Enumerable.Range(0, height).Select(y => new string(outputChars, y * width, width)).ToArray();

      // --- Variante simulieren um den Endstand des Spielfeldes zu erhalten ---
      var tmpField = new SokoField(string.Join("\r\n", linesBefore) + "\r\n");
      bool invalidMove = false;
      if (iPortalIndex < uint.MaxValue) // Eintritt durch das eingehende Portal als ersten durchführen (aber später nicht darstellen)
      {
        char dirChar = rooms[roomIndex].incomingPortals[iPortalIndex].dirChar;
        if (!tmpField.SafeMove(dirChar))
        {
          invalidMove = true;
        }
        linesBefore = tmpField.GetText().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // das Vorher-Spielfeld neu setzen
      }

      int moves = 0;
      if (!invalidMove)
      {
        foreach (var dirChar in variantData.path)
        {
          if (!tmpField.SafeMove(dirChar))
          {
            invalidMove = true;
            break;
          }
          moves++;
        }
      }
      Debug.Assert(linesBefore.Length == height);


      var linesAfter = tmpField.GetText().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      Debug.Assert(linesAfter.Length == height);

      var linesFill = new string[linesAfter.Length];
      for (int i = 0; i < linesFill.Length; i++) linesFill[i] = "       ";
      string tmp = "[" + moves + "]";
      while (tmp.Length < 7) { tmp = " " + tmp; if (tmp.Length < 7) tmp += " "; }
      linesFill[0] = tmp;
      linesFill[linesFill.Length - 1] = "  -->  ";

      return string.Join("\r\n", Enumerable.Range(0, height).Select(y => linesBefore[y] + linesFill[y] + linesAfter[y])) + "\r\n\r\nPath: " + variantData.path + (invalidMove ? " - BLOCKED" : "") + "\r\n\r\n";
    }
    #endregion

    #region # public override string ToString() // gibt Informationen über den aktuellen Such-Status zurück
    /// <summary>
    /// gibt Informationen über den aktuellen Such-Status zurück
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

      if (solveState == SolveState.Init) return sb.ToString();
      uint roomIndex = GetTaskRoomIndex(currentTask);
      ulong variant = currentVariant;
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
    #endregion
  }
}
