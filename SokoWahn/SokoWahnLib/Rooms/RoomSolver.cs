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
    /// Anzahl der zusätzlichen Werte innerhalb einer Aufgabe
    /// </summary>
    const uint TaskInfoValues = 2;

    /// <summary>
    /// merkt sich die gesamte Länge einer Aufgabe: Zustände[] + Variante + (Raum-Nummer und eingehende Portal-Nummer)
    /// </summary>
    readonly uint taskSize;

    #region # // --- Aufgaben-Hilfsmethoden ---
    /// <summary>
    /// setzt die Basis-Infos innerhalb einer Aufgabe
    /// </summary>
    /// <param name="task">Aufgabe, welche bearbeitet werden soll</param>
    /// <param name="roomIndex">Raum-Nummer, welche gesetzt werden soll</param>
    /// <param name="iPortalIndex">Portal-Nummer für das eingehende Portal oder uint.MaxValue, wenn es sich um eine Start-Variante handelt</param>
    void SetTaskInfos(ulong[] task, uint roomIndex, uint iPortalIndex)
    {
      Debug.Assert(task.Length == taskSize);
      Debug.Assert(roomIndex < rooms.Length);

      task[taskRoomPortalOfs] = (ulong)roomIndex << 32 | iPortalIndex;

      Debug.Assert(GetTaskRoomIndex(task) == roomIndex);
      Debug.Assert(GetTaskPortalIndex(task) == iPortalIndex);
      Debug.Assert(Enumerable.Range(0, rooms.Length).All(i => task[i] < rooms[i].stateList.Count));
    }

    /// <summary>
    /// setzt die Variante innerhalb einer Aufgabe
    /// </summary>
    /// <param name="task">Aufgabe, welche bearbeitet werden soll</param>
    /// <param name="variant">Variante, welche gesetzt werden soll</param>
    void SetTaskVariant(ulong[] task, ulong variant)
    {
      Debug.Assert(task.Length == taskSize);
      Debug.Assert(variant < rooms[GetTaskRoomIndex(task)].variantList.Count || variant == ulong.MaxValue);
      Debug.Assert(variant == ulong.MaxValue
        || (GetTaskPortalIndex(task) < rooms[GetTaskRoomIndex(task)].incomingPortals.Length
            && rooms[GetTaskRoomIndex(task)].incomingPortals[GetTaskPortalIndex(task)].variantStateDict.GetVariants(task[GetTaskRoomIndex(task)]).Any(v => v == variant))
        || (GetTaskPortalIndex(task) == uint.MaxValue && variant < rooms[GetTaskRoomIndex(task)].startVariantCount));

      task[taskVariantOfs] = variant;

      Debug.Assert(GetTaskVariant(task) == variant);
      Debug.Assert(Enumerable.Range(0, rooms.Length).All(i => task[i] < rooms[i].stateList.Count));
    }

    /// <summary>
    /// gibt die gespeicherte Variante einer Aufgabe zurück
    /// </summary>
    /// <param name="task">Aufgabe, welche abgefragt werden soll</param>
    /// <returns>enthaltene Variante</returns>
    public ulong GetTaskVariant(ulong[] task)
    {
      Debug.Assert(task.Length == taskSize);
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
      Debug.Assert(task.Length == taskSize);
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
      Debug.Assert(task.Length == taskSize);
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
      Debug.Assert(variant == task[task.Length - TaskInfoValues]);
      var variantData = room.variantList.GetData(variant);

      if (variantData.oPortalIndexBoxes.Length > 0)
      {
        Array.Copy(task, tmpTask, task.Length); // Aufgabe temporär kopieren, damit Zustände geändert werden dürfen

        // Kisten-Zustände anpassen und prüfen
        if (!ResolveTaskPortalBoxes(tmpTask, room, variantData.oPortalIndexBoxes)) return false;

        if (variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung erreicht?
        {
          tmpTask[room.roomIndex] = variantData.newState;
          for (int i = 0; i < tmpTask.Length - TaskInfoValues; i++) if (tmpTask[i] != 0) return false; // unvollständiges Ende gefunden
          return true; // Ende erreicht!
        }
        else
        {
          var oPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
          return oPortal.variantStateDict.GetVariants(tmpTask[oPortal.toRoom.roomIndex]).Any(); // mindestens eine gültige Nachfolge-Variante gefunden?
        }
      }
      else
      {
        if (variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung erreicht?
        {
          throw new NotImplementedException("todo: check debug");

          for (int i = 0; i < task.Length - TaskInfoValues; i++) if (task[i] != 0) return false; // unvollständiges Ende gefunden
          return true; // Ende erreicht!
        }
        else
        {
          var oPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
          return oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]).Any(); // mindestens eine gültige Nachfolge-Variante gefunden?
        }
      }
    }

    #region # struct TaskVariantInfo // Ergebnis-Informationen einer berechneten Variante
    /// <summary>
    /// Ergebnis-Informationen einer berechneten Variante
    /// </summary>
    struct TaskVariantInfo
    {
      /// <summary>
      /// Anzahl der gemachten Laufschritte
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
    /// verarbeitet eine Aufgabe und berechnet daraus weitere neue Aufgaben
    /// </summary>
    /// <param name="task">Aufgabe, welche abgearbeitet werden soll</param>
    /// <param name="moveFilter">Filter zum verhindern von doppelten Laufschritten</param>
    /// <param name="moveOffset">Offset für den Move-Filter</param>
    /// <returns>Enumerable der neuen Aufgaben</returns>
    IEnumerable<TaskVariantInfo> ResolveTaskVariants(ulong[] task, Dictionary<ulong, ulong> moveFilter, ulong moveOffset)
    {
      uint roomIndex = GetTaskRoomIndex(task);
      var room = rooms[roomIndex];

      Debug.Assert(GetTaskVariant(task) < room.variantList.Count);
      var variantData = room.variantList.GetData(GetTaskVariant(task));

      if (!ResolveTaskPortalBoxes(task, room, variantData.oPortalIndexBoxes))
      {
        throw new Exception("invalid task (resolve portal-boxes)"); // Check-Funktion bei "AddStarts" nicht ausgeführt?
      }

      task[room.roomIndex] = variantData.newState; // neuen Zustand des eigenen Raumes setzen

      if (variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung schon erreicht?
      {
        for (int i = 0; i < task.Length - TaskInfoValues; i++) if (task[i] != 0) throw new Exception("invalid task (end-states)");

        yield return new TaskVariantInfo(variantData.moves, variantData.pushes);
        yield break;
      }

      var oPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];

      SetTaskInfos(task, oPortal.toRoom.roomIndex, oPortal.iPortalIndex);

      var tmpTask = new ulong[taskSize];

      // --- Sub-Varianten durcharbeiten ---
      foreach (ulong variant in oPortal.variantStateDict.GetVariants(task[oPortal.toRoom.roomIndex]))
      {
        SetTaskVariant(task, variant);

        if (CheckTask(task, tmpTask, oPortal.toRoom, variant))
        {
          variantData = oPortal.toRoom.variantList.GetData(variant);
          ulong totalMoves = moveOffset + variantData.moves;

          if (variantData.pushes > 0) // sinnvolle Variante mit Kistenverschiebungen erkannt?
          {
            DebugConsoleV("Next-Variant " + variant, task);

            yield return new TaskVariantInfo(totalMoves, variantData.pushes, Crc64.Get(task));
          }
          else // Variante nur mit Laufwegen erkannt -> rekursiv weiter suchen
          {
            ulong moveCrc = Crc64.Start.Crc64Update(task[task.Length - 2]).Crc64Update(task[task.Length - 1]);
            ulong oldMoves;
            if (!moveFilter.TryGetValue(moveCrc, out oldMoves)) oldMoves = ulong.MaxValue;

            if (totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
            {
              DebugConsoleV("Next-Move " + variant, task);
              if (oldMoves == ulong.MaxValue) moveFilter.Add(moveCrc, totalMoves); else moveFilter[moveCrc] = totalMoves;
            }
            else
            {
              DebugConsoleV("Next-Move " + variant + " (skipped)", task);
              continue;
            }

            Array.Copy(task, tmpTask, task.Length); // Backup erstellen
            foreach (var subInfo in ResolveTaskVariants(task, moveFilter, totalMoves))
            {
              yield return subInfo;
            }
            Array.Copy(tmpTask, task, task.Length); // Backup wiederherstellen
          }
        }
        else
        {
          DebugConsoleV("Next-Variant " + variant + " (skipped)", task);
        }
      }
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
      taskSize = taskRoomPortalOfs + 1;
      currentTask = new ulong[taskSize];
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

          SetTaskInfos(currentTask, (uint)startRoomIndex, uint.MaxValue);
          SetTaskVariant(currentTask, ulong.MaxValue); // Start-Variante als -1 setzen, damit es ab Beginn durch das inkrementieren mit 0 beginnt

          forwardTasks.Add(new TaskListNormal(taskSize)); // erste Aufgaben-Liste für die Start-Züge hinzufügen

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

          var tmpTask = new ulong[taskSize];

          for (; maxTicks > 0; maxTicks--)
          {
            if (variant == room.startVariantCount) // alle Start-Varianten bereits ermittelt?
            {
              solveState = SolveState.ScanForward;
              maxTicks = Math.Max(1, maxTicks);
              goto case SolveState.ScanForward;
            }

            // --- Start-Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            SetTaskVariant(currentTask, variant);

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

            variant++;
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

          for (; maxTicks > 0 && taskList.Count > 0; maxTicks--)
          {
            taskList.FetchFirst(currentTask);
            DebugConsole("Search-Forward [" + forwardIndex + "], remain: " + (taskList.Count + 1).ToString("N0"));

            // --- Aufgabe abarbeiten und neue Einträge in die Aufgaben-Liste hinzufügen ---
            var moveFilter = new Dictionary<ulong, ulong>();
            foreach (var taskInfo in ResolveTaskVariants(currentTask, moveFilter, 0))
            {
              ulong totalMoves = (uint)forwardIndex + taskInfo.moves;
              if (taskInfo.crc == 0)
              {
                throw new NotImplementedException("todo: gesamten Pfad ermitteln und als beste Lösung speichern"); // Ziel erreicht?
              }
              ulong oldMoves = hashTable.Get(taskInfo.crc, ulong.MaxValue);

              if (totalMoves < oldMoves) // bessere bzw. erste Variante gefunden?
              {
                if (oldMoves == ulong.MaxValue) hashTable.Add(taskInfo.crc, totalMoves); else hashTable.Update(taskInfo.crc, totalMoves);

                // --- Variante als neue Aufgabe hinzufügen ---
                while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
                forwardTasks[(int)(uint)totalMoves].Add(currentTask);
              }
            }
          }

          // --- nächsten Current-State nachladen (für Debugging) ---
          while (taskList.Count == 0 && forwardIndex + 1 < forwardTasks.Count)
          {
            taskList.Dispose();
            taskList = forwardTasks[++forwardIndex];
          }
          if (taskList.PeekFirst(currentTask)) DebugConsole("Next");
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    #region # public int[] PlayerPathPosis // gibt die Spielerpositionen der aktuellen Aufgabe zurück
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
    public static readonly bool DebugConsoleVerbose = false;

    /// <summary>
    /// gibt das Spielfeld mit der entsprechenden Aufgabe in der Console aus (nur Debug-Modus)
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    [Conditional("DEBUG")]
    public void DebugConsole(string title = null, ulong[] task = null)
    {
      return;
      if (!IsConsoleApplication) return;
      if (Console.CursorTop == 0) Console.WriteLine();
      if (!string.IsNullOrWhiteSpace(title)) title = "  --- " + title + " ---\r\n";
      Console.WriteLine(title + ("\r\n" + DebugStr(task)).Replace("\r\n", "\r\n  "));
    }

    /// <summary>
    /// gleiche wie DebugConsole, jedoch für sehr häufige Ausgaben geeignet
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    [Conditional("DEBUG")]
    public void DebugConsoleV(string title = null, ulong[] task = null)
    {
      // ReSharper disable once RedundantJumpStatement
      if (!DebugConsoleVerbose) return;
      DebugConsole(title, task);
    }
    #endregion

    #region # public string DebugStr(ulong[] task = null) // gibt das Spielfeld einer bestimmten Aufgabe zurück (nebeneinander: vorher/nacher)
    /// <summary>
    /// gibt das Spielfeld einer bestimmten Aufgabe zurück (nebeneinander: vorher/nacher)
    /// </summary>
    /// <param name="task">optional: Aufgabe, welche angezeigt werden soll (default: currentTask)</param>
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

      uint roomIndex = GetTaskRoomIndex(task);
      uint iPortalIndex = GetTaskPortalIndex(task);
      ulong variant = GetTaskVariant(task);
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
    #endregion
  }
}
