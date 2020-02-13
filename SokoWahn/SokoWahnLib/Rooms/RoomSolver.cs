using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable RedundantIfElseBlock
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ConvertToConstant.Global
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ConvertToConstant.Local

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

    /// <summary>
    /// gibt an, ob die Hashtable schonender behandelt werden soll: neue Hash-Einträge werden erst beim Abarbeiten von Aufgaben hinzugefügt (nicht beim Erstellen),
    /// 
    /// aktiv: Vorteil: geringerer Füllrate der Hashtable (besonders bei längeren Varianten)
    ///        Nachteil: teils deutlich größere Aufgaben-Listen und dadurch höherer Rechenaufwand
    ///        Info: Aufgaben-Listen wären streamfähig, was bei Hashtables nicht der Fall ist (kann bei maximaler Skalierung Speichervorteile bieten)
    /// 
    /// inaktiv: Vorteil: generell schnellere Verarbeitung durch kleinere Aufgaben-Listen (dank Vorfilterung per Hashtable)
    ///          Nachteil: höherer Speicherverbrauch der Hashtable während der Verarbeitung, welche nicht streamfähig ist
    /// 
    /// Default: false / inaktiv
    /// </summary>
    static readonly bool HashRelieve = false;

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
      var room = rooms[GetTaskRoomIndex(task)];
      if (room == null) throw new NullReferenceException("room");
      return room;
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
      var room = GetTaskRoom(task);
      uint iPortalIndex = GetTaskPortalIndex(task);
      if (variant == ulong.MaxValue)
      {
        if (iPortalIndex < uint.MaxValue)
        {
          yield return room.incomingPortals[iPortalIndex].fromPos;
          yield return room.incomingPortals[iPortalIndex].toPos;
        }
        else
        {
          yield return roomNetwork.field.PlayerPos;
        }
        yield break;
      }

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
          DebugConsoleV("Move-Step " + (listIndex + 1) + " / " + list.Count + " (last)", task, step.variant, step.roomIndex, step.iPortalIndex);
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

        DebugConsoleV("Push-Step (moves: " + toMoves + ")", task, step.variant, step.roomIndex, step.iPortalIndex);

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
            if (variantData.newState == 0)
            {
              bool end = true;
              for (int i = 0; i < outputTask.Length - TaskInfoValues; i++) if (outputTask[i] != 0) { end = false; break; }
              if (end) continue; // ineffizientes Ende gefunden -> überspringen
            }
            var toPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
            var toRoom = toPortal.toRoom;
            if (toPortal.variantStateDict.GetVariants(outputTask[toRoom.roomIndex]).Any(variantCheck => CheckTask(outputTask, toRoom, variantCheck, 0))) // mindestens eine gültige Nachfolge-Variante gefunden?
            {
              SetTaskInfos(outputTask, toRoom.roomIndex, toPortal.iPortalIndex); // durch ein Portal zum nächsten Raum wechseln
              yield return new TaskVariantInfo(toMoves, variantData.pushes, Crc64.Get(outputTask));
            }
          }
        }
      }
    }

    /// <summary>
    /// prüft, ob eine bestimmte Variante bei einer Aufgabe gültig ist
    /// </summary>
    /// <param name="task">Aufgabe, welche geprüft werden soll</param>
    /// <param name="room">Raum, welcher betroffen ist</param>
    /// <param name="variant">Variante, welche geprüft werden soll</param>
    /// <param name="depth">zusätzlich Prüf-Tiefe</param>
    /// <returns>true, wenn die Variante gültig ist</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckTask(ulong[] task, Room room, ulong variant, int depth)
    {
      var variantData = room.variantList.GetData(variant);

      if (depth > 0)
      {
        var tmp = TaskClone(task);
        if (!ResolveTaskPortalBoxes(tmp, room, variantData.oPortalIndexBoxes)) return false;
        depth--;
        var toPortal = room.outgoingPortals[variantData.oPortalIndexPlayer];
        var toRoom = toPortal.toRoom;
        return toPortal.variantStateDict.GetVariants(tmp[toRoom.roomIndex]).Any(checkVariant => CheckTask(tmp, toRoom, checkVariant, depth));
      }
      else
      {
        foreach (var boxPortal in variantData.oPortalIndexBoxes) // Kisten durch benachbarte Portale schieben (testen)
        {
          var oPortal = room.outgoingPortals[boxPortal];
          uint roomIndex = oPortal.toRoom.roomIndex;
          ulong oldState = task[roomIndex];
          ulong newState = oPortal.stateBoxSwap.Get(oldState);
          if (oldState == newState) return false; // Kiste kann nicht vom banachbarten Raum aufgenommen werden -> gesamte Variante ungültig
        }
        return true;
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
        if (oldState == newState) return false; // Kiste kann nicht vom benachbarten Raum aufgenommen werden -> gesamte Variante ungültig

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
    /// merkt sich die beste Lösung als Pfad
    /// </summary>
    string bestSolutionPath;

    /// <summary>
    /// merkt sich die Anzahl der Kistenverschiebungen der besten bekannten Lösung
    /// </summary>
    ulong bestSolutionPushes = ulong.MaxValue;

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
#if DEBUG
      if (IsConsoleApplication) Console.Clear();
#endif
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
          var tmpTask = TaskClone(currentTask);

          Debug.Assert(GetTaskPortalIndex(currentTask) == uint.MaxValue);

          for (; maxTicks > 0; maxTicks--)
          {
            // --- Start-Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            DebugConsole("Start-Variant");

            #region # // --- Variante abarbeiten ---
            foreach (var taskInfo in ResolveTask(currentTask, currentVariant, tmpTask))
            {
              ulong totalMoves = taskInfo.moves;

              if (taskInfo.crc == 0) // Ende erreicht?
              {
                UpdateBestSolution(tmpTask, totalMoves);
                continue;
              }

              if (bestSolutionPath == null || totalMoves < (uint)bestSolutionPath.Length)
              {
                while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
                DebugConsoleTask("new Task [" + totalMoves + ", " + forwardTasks[(int)(uint)totalMoves].Count + "] (moves: " + taskInfo.moves + ", pushes: " + taskInfo.pushes + ")", tmpTask);
                forwardTasks[(int)(uint)totalMoves].Add(tmpTask);
              }
            }
            #endregion

            currentVariant++;

            if (currentVariant == currentVariantEnd) // alle Start-Varianten bereits abgearbeitet?
            {
              solveState = SolveState.ScanForward;
              break;
            }
          }
        } break;
        #endregion

        #region # // --- ScanForward - Lösungssuche vorwärts ---
        case SolveState.ScanForward:
        {
          var tmpTask = TaskClone(currentTask);

          for (; maxTicks > 0; )
          {
            if (currentVariant < currentVariantEnd)
            {
              DebugConsole("Search-Forward [" + forwardIndex + "], task-remain: " + (forwardTasks[forwardIndex].Count + 1).ToString("N0") + ", remain variants: " + (currentVariantEnd - currentVariant));

              #region # // --- Variante abarbeiten ---
              foreach (var taskInfo in ResolveTask(currentTask, currentVariant, tmpTask))
              {
                ulong totalMoves = taskInfo.moves + (uint)forwardIndex;

                if (taskInfo.crc == 0) // Ende erreicht?
                {
                  UpdateBestSolution(tmpTask, totalMoves);
                  continue;
                }

                ulong oldMoves = hashTable.Get(taskInfo.crc, ulong.MaxValue);
                if (oldMoves <= totalMoves) continue;
                if (!HashRelieve)
                {
                  if (oldMoves == ulong.MaxValue) hashTable.Add(taskInfo.crc, totalMoves); else hashTable.Update(taskInfo.crc, totalMoves);
                }

                if (bestSolutionPath == null || totalMoves < (uint)bestSolutionPath.Length)
                {
                  while (totalMoves >= (ulong)forwardTasks.Count) forwardTasks.Add(new TaskListNormal(taskSize));
                  DebugConsoleTask("new Task [" + totalMoves + ", " + forwardTasks[(int)(uint)totalMoves].Count + "] (moves: " + taskInfo.moves + ", pushes: " + taskInfo.pushes + ")", tmpTask);
                  forwardTasks[(int)(uint)totalMoves].Add(tmpTask);
                }
              }
              #endregion

              maxTicks--;
              currentVariant++;
            }

            #region # // --- neue Variante oder Aufgabe wählen (falls notwendig) ---
            while (currentVariant == currentVariantEnd) // alle Varianten einer Aufgabe abgearbeitet?
            {
              if (forwardIndex >= forwardTasks.Count) return true; // Ende aller Aufgaben-Listen erreicht?
              if (bestSolutionPath != null && forwardIndex >= bestSolutionPath.Length) // beste Lösung gefunden?
              {
                while (forwardIndex < forwardTasks.Count) forwardTasks[forwardIndex++].Dispose(); // restliche Aufgaben löschen
                return true;
              }
              var taskList = forwardTasks[forwardIndex];

              // --- neue Aufgaben abholen und zugehörige Varianten ermitteln ---
              for (; ; )
              {
                if (!taskList.FetchFirst(currentTask))
                {
                  // gesamte Aufgabenliste für diesen Zug bereits abgearbeitet?
                  taskList.Dispose();
                  forwardIndex++;
                  return false;
                }
                DebugConsoleTask("Fetch-Task [" + forwardIndex + ", " + (taskList.CountFetchedFirst - 1) + "]", currentTask);
                maxTicks--;

                ulong crc = Crc64.Get(currentTask);
                ulong oldMoves = hashTable.Get(crc, ulong.MaxValue);
                if (HashRelieve)
                {
                  if (oldMoves <= (uint)forwardIndex)
                  {
                    if (maxTicks <= 0) return false;
                    continue;
                  }
                }
                else
                {
                  if (oldMoves < (uint)forwardIndex)
                  {
                    if (maxTicks <= 0) return false;
                    continue;
                  }
                }
                if (oldMoves == ulong.MaxValue) hashTable.Add(crc, (uint)forwardIndex); else hashTable.Update(crc, (uint)forwardIndex);

                break; // nützliche Aufgabe gefunden
              }

              var room = GetTaskRoom(currentTask);
              var iPortalIndex = GetTaskPortalIndex(currentTask);

              // todo: Varianten in Ketten-Logik speichern, da diese immer zusammenhängend sind
              ulong firstVariant = ulong.MaxValue;
              ulong lastVariant = 0;
              foreach (var variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariants(currentTask[room.roomIndex]))
              {
                if (variant < firstVariant) // erste Variante erkannt?
                {
                  Debug.Assert(firstVariant == ulong.MaxValue);
                  firstVariant = variant;
                  Debug.Assert(lastVariant == 0);
                  lastVariant = variant;
                  continue;
                }
                Debug.Assert(variant == lastVariant + 1); // fortlaufende Variante ohne Lücke erwartet
                lastVariant = variant;
              }
              Debug.Assert(firstVariant < ulong.MaxValue);

              currentVariant = firstVariant;
              currentVariantEnd = lastVariant + 1;
            }
            #endregion
          }
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    /// <summary>
    /// aktualisiert die beste Lösung
    /// </summary>
    /// <param name="endTask">End-Aufgabe (alle Raumzustände sind 0)</param>
    /// <param name="moves">Anzahl der Laufschritte</param>
    /// <returns>true, wenn eine bessere Lösung erkannt wurde</returns>
    bool UpdateBestSolution(ulong[] endTask, ulong moves)
    {
      if (bestSolutionPath == null || moves <= (uint)bestSolutionPath.Length)
      {
        if (bestSolutionPath != null && moves < (uint)bestSolutionPath.Length) bestSolutionPushes = moves; // kürzeren Laufweg gefunden: beste Zahl der Kistenverschiebungen zurücksetzten

        // todo: Pfad auflösen und bei gleicher Anzahl der Moves nach niedrigster Push-Variante suchen
        bestSolutionPath = new string('x', (int)(uint)moves);
        bestSolutionPushes = moves;

        return true;
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
            return GetTaskPlayerPath(currentTask, currentVariant < currentVariantEnd ? currentVariant : ulong.MaxValue).ToArray();
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
    /// gibt das Spielfeld mit der entsprechenden Aufgabe + Variante in der Console aus (nur Debug-Modus)
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
      if (!string.IsNullOrWhiteSpace(title)) title = "  --- " + title + " ---\r\n";
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

    /// <summary>
    /// gibt das Spielfeld einer Aufgabe ohne Variante in der Console aus
    /// </summary>
    /// <param name="title">optionale Überschrift über dem Spielfeld</param>
    /// <param name="task">optionale Aufgabe, welche direkt angezeigt werden soll (default: currentTask)</param>
    /// <param name="roomIndex">optionaler expliziter Raum, welcher verwendet werden soll (default: Raum aus der Aufgabe)</param>
    /// <param name="iPortalIndex">optional explizites Portal, welches verwendet werden soll (default: Portal aus der Aufgabe)</param>
    [Conditional("DEBUG")]
    public void DebugConsoleTask(string title = null, ulong[] task = null, uint roomIndex = uint.MaxValue, uint iPortalIndex = uint.MaxValue)
    {
      if (!IsConsoleApplication) return;
      if (Console.CursorTop == 0) Console.WriteLine();
      if (!string.IsNullOrWhiteSpace(title)) title = "  --- " + title + " ---\r\n";
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine(title + ("\r\n" + DebugStr(task, ulong.MaxValue, roomIndex, iPortalIndex)).Replace("\r\n", "\r\n  "));
      Console.ForegroundColor = ConsoleColor.Gray;
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

      if (variant == ulong.MaxValue) // Task-Version
      {
        return string.Join("\r\n", Enumerable.Range(0, height).Select(y => linesBefore[y])) + (invalidMove ? "\r\n\r\n--- BLOCKED ---" : "") + "\r\n\r\n";
      }

      var variantData = rooms[roomIndex].variantList.GetData(variant);
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

      if (bestSolutionPath == null || forwardIndex < forwardTasks.Count)
      {
        ulong totalTasks = 0;
        for (int moveIndex = forwardIndex; moveIndex < forwardTasks.Count; moveIndex++) totalTasks += forwardTasks[moveIndex].Count;
        sb.AppendLine(" Tasks: " + totalTasks.ToString("N0")).AppendLine();
        sb.AppendLine(" Moves: " + forwardIndex.ToString("N0")).AppendLine();
        sb.AppendLine(" State: " + solveState);
      }
      sb.AppendLine();

      if (solveState == SolveState.Init) return sb.ToString();
      uint roomIndex = GetTaskRoomIndex(currentTask);
      switch (solveState)
      {
        case SolveState.AddStarts:
        {
          sb.AppendLine(string.Format(" Add-Starts: {0:N0} / {1:N0}", currentVariant + 1, rooms[roomIndex].startVariantCount)).AppendLine();
        } break;
      }

      if (bestSolutionPath != null)
      {
        if (forwardIndex >= forwardTasks.Count)
        {
          sb.AppendLine(" --- PERFECT SOLUTION (" + bestSolutionPath.Length.ToString("N0") + " / " + bestSolutionPushes.ToString("N0") + ") ---");
        }
        else
        {
          sb.AppendLine(" --- solution found (" + bestSolutionPath.Length.ToString("N0") + " / " + bestSolutionPushes.ToString("N0") + ") ---");
        }
        sb.AppendLine();
        sb.AppendLine("   Path: " + bestSolutionPath);
        sb.AppendLine("  Moves: " + bestSolutionPath.Length.ToString("N0"));
        sb.AppendLine(" Pushes: " + bestSolutionPushes.ToString("N0"));
        sb.AppendLine();
      }

      if (forwardIndex < forwardTasks.Count)
      {
        if (currentVariant < ulong.MaxValue)
        {
          sb.AppendLine(" Room: " + roomIndex);
          sb.AppendLine(" V-ID: " + (currentVariant + 1) + " / " + currentVariantEnd);
          sb.AppendLine(" Path: " + (currentVariant < currentVariantEnd ? rooms[roomIndex].variantList.GetData(currentVariant).path : "-"));
          sb.AppendLine();

          for (int moveIndex = forwardIndex; moveIndex < forwardTasks.Count; moveIndex++)
          {
            if (moveIndex == forwardIndex && currentVariant < currentVariantEnd)
            {
              sb.AppendLine(" [" + moveIndex.ToString("N0") + "]: " + (forwardTasks[moveIndex].Count).ToString("N0") + " (+" + (currentVariantEnd - currentVariant) + " V)");
            }
            else
            {
              sb.AppendLine(" [" + moveIndex.ToString("N0") + "]: " + forwardTasks[moveIndex].Count.ToString("N0"));
            }
          }
        }
      }
      else if (bestSolutionPath == null)
      {
        sb.AppendLine(" --- no solution found ---");
      }

      return sb.ToString();
    }
    #endregion
  }
}
