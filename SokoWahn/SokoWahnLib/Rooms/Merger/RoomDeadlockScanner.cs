using System;
using System.Collections.Generic;
using System.Linq;

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse zum Suchen von nicht lösbaren Varianten in einem Raum
  /// </summary>
  public sealed class RoomDeadlockScanner : IDisposable
  {
    /// <summary>
    /// merkt sich den Raum, welcher optimiert werden soll
    /// </summary>
    readonly Room room;

    /// <summary>
    /// merkt sich die Rückwärts-Varianten des Raumes
    /// </summary>
    RoomReverse reverseMap;

    /// <summary>
    /// merkt sich die Varianten, welche bei der Vorwärts-Suche im Einsatz waren
    /// </summary>
    readonly Bitter usedVariantsForward;
    /// <summary>
    /// merkt sich die Varianten, welche bei der Rückwärts-Suche im Einsatz waren
    /// </summary>
    readonly Bitter usedVariantsBackward;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Raum, welcher durchsucht werden soll</param>
    public RoomDeadlockScanner(Room room)
    {
      if (room == null) throw new NullReferenceException("room");
      this.room = room;
      usedVariantsForward = new Bitter(room.variantList.Count);
      usedVariantsBackward = new Bitter(room.variantList.Count);
    }

    /// <summary>
    /// erstellt die Rückwärts-Varianten für die Suche
    /// </summary>
    public void Step1_CreateReverseMap()
    {
      var reverseMap = new RoomReverse(room);

      reverseMap.Step1_FillPortalStateSwaps();

      reverseMap.Step2_CollectVariantsPerState();

      reverseMap.Step3_FillDicts();

      this.reverseMap = reverseMap;
    }

    /// <summary>
    /// Vorwärts-Suche durch alle erreichbaren Varianten
    /// </summary>
    /// <param name="scanInfo">Status-Meldung</param>
    public bool Step2_ScanForward(Func<string, bool> scanInfo)
    {
      var tasks = new Stack<DeadlockTask>();
      var room = this.room;
      var usedVariants = usedVariantsForward;
      ulong usedVariantCount = 0;

      #region # // --- erste Aufgaben sammeln ---
      if (room.startVariantCount > 0) // Start-Varianten vorhanden?
      {
        usedVariants.SetBits(0, room.startVariantCount); // Startvarianten als benutzt markieren
        usedVariantCount++;
        for (ulong variant = 0; variant < room.startVariantCount; variant++)
        {
          var variantData = room.variantList.GetData(variant);
          if (variantData.oPortalIndexPlayer == uint.MaxValue) continue; // End-Varianten brauchen nicht weiter verfolgt werden

          var newTask = new DeadlockTask
          (
            variantData.oPortalIndexPlayer,            // Portalnummer, worüber der Spieler den Raum verlassen hat
            variantData.oPortalIndexBoxes.Length > 0,  // Angabe, ob der Spieler eine Kiste rausgeschoben hat
            variantData.newState                       // neuer Kistenzustand des Raumes
          );
          tasks.Push(newTask);
        }
      }
      else
      {
        var newTask = new DeadlockTask
        (
          uint.MaxValue,   // Portalnummer, worüber der Spieler den Raum verlassen hat
          false,           // Angabe, ob der Spieler eine Kiste mitgenommen hat
          room.startState  // neuer Kistenzustand des Raumes
        );
        tasks.Push(newTask);
      }
      #endregion

      ulong portalMask = 1; // Bitmaske je nach Anzahl der Portale (theoretisch 63 Portale möglich)
      for (int i = 0; i < room.incomingPortals.Length; i++) portalMask <<= 1;

      var maskStateCache = new ulong[room.stateList.Count][];
      for (ulong checkState = 0; checkState < room.stateList.Count; checkState++)
      {
        if (Tools.TickRefresh() && !scanInfo("Build State-Mask: " + checkState.ToString("N0") + " / " + room.stateList.Count.ToString("N0"))) return false;
        var list = new List<ulong>();
        for (ulong mask = 1; mask < portalMask; mask++)
        {
          bool valid = true; // merkt sich, ob der Kistenwechsel gütig ist
          ulong state = checkState;
          for (int iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            if ((mask & 1UL << iPortalIndex) == 0) continue;

            // --- Kiste über das Portal reinschieben ---
            ulong nextState = room.incomingPortals[iPortalIndex].stateBoxSwap.Get(state);
            if (nextState == state) { valid = false; break; } // ungültige Kiste erkannt?
            state = nextState; // neuen Kistenzustand übernehmen
          }
          if (!valid) continue; // ungültigen Kistenzustand erkannt?
          list.Add(state);
        }
        maskStateCache[checkState] = list.ToArray();
      }

      while (tasks.Count > 0)
      {
        if (Tools.TickRefresh())
        {
          if (!scanInfo(usedVariantCount.ToString("N0") + " / " + usedVariants.Length.ToString("N0") + ", Tasks: " + tasks.Count.ToString("N0"))) return false;
          if (usedVariantCount == usedVariants.Length) return true; // alle Varianten sind in Benutzung?
        }
        var task = tasks.Pop();

        // --- Varianten ohne Kistenwechsel abarbeiten ---
        for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
        {
          if (iPortalIndex == task.portalIndexPlayer && !task.exportedBox) continue; // todo: bug? einfaches Zurücklaufen durch das gleiche Portal nicht erlaubt, außer wenn vorher eine Kiste aus dem Raum geschoben wurde

          foreach (ulong variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(task.state).AsEnumerable())
          {
            if (usedVariants.GetBit(variant)) continue; // Variante schon bekannt?
            usedVariants.SetBit(variant);
            usedVariantCount++;
            if (Tools.TickRefresh() && !scanInfo(usedVariantCount.ToString("N0") + " / " + usedVariants.Length.ToString("N0") + ", Tasks: " + tasks.Count.ToString("N0"))) return false;

            var variantData = room.variantList.GetData(variant);
            if (variantData.oPortalIndexPlayer == uint.MaxValue)
            {
              foreach (var oPortalIndexBox in variantData.oPortalIndexBoxes)
              {
                if (!room.field.IsGoal(room.outgoingPortals[oPortalIndexBox].toPos)) // rausgeschobene Kiste steht nicht auf einem Zielfeld?
                {
                  usedVariants.ClearBit(variant); // End-Variante wieder als ungültig markieren
                  usedVariantCount--;
                  break;
                }
              }
              continue; // End-Varianten brauchen nicht weiter verfolgt werden
            }

            var newTask = new DeadlockTask
            (
              variantData.oPortalIndexPlayer,            // Portalnummer, worüber der Spieler den Raum verlassen hat
              variantData.oPortalIndexBoxes.Length > 0,  // Angabe, ob der Spieler eine Kiste rausgeschoben hat
              variantData.newState                       // neuer Kistenzustand des Raumes
            );
            tasks.Push(newTask);
          }
        }

        // --- Varianten nach Kistenwechsel abarbeiten ---
        foreach (ulong state in maskStateCache[task.state])
        {
          for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            var span = room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(state);
            if (usedVariants.GetBit(span.variantStart) && span.variantCount > 0) continue;

            ulong variantEnd = span.VariantEnd;
            for (ulong variant = span.variantStart; variant < variantEnd; variant++)
            {
              usedVariants.SetBit(variant);
              usedVariantCount++;

              var variantData = room.variantList.GetData(variant);
              if (variantData.oPortalIndexPlayer == uint.MaxValue)
              {
                foreach (var oPortalIndexBox in variantData.oPortalIndexBoxes)
                {
                  if (!room.field.IsGoal(room.outgoingPortals[oPortalIndexBox].toPos)) // rausgeschobene Kiste steht nicht auf einem Zielfeld?
                  {
                    usedVariants.ClearBit(variant); // End-Variante wieder als ungültig markieren
                    usedVariantCount--;
                    break;
                  }
                }
                continue; // End-Varianten brauchen nicht weiter verfolgt werden
              }

              var newTask = new DeadlockTask
              (
                variantData.oPortalIndexPlayer,            // Portalnummer, worüber der Spieler den Raum verlassen hat
                variantData.oPortalIndexBoxes.Length > 0,  // Angabe, ob der Spieler eine Kiste rausgeschoben hat
                variantData.newState                       // neuer Kistenzustand des Raumes
              );
              tasks.Push(newTask);
            }
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Rückwärts-Suche nach allen erreichbaren Varianten
    /// </summary>
    /// <param name="scanInfo">Status-Meldung</param>
    public bool Step3_ScanBackward(Func<string, bool> scanInfo)
    {
      var tasks = new Stack<DeadlockTask>();
      var room = this.room;
      var usedVariants = usedVariantsBackward;
      ulong usedVariantCount = 0;

      // --- Bitmaske je nach Anzahl der Portale erstellen (theoretisch 63 Portale möglich) ---
      ulong portalMask = 1;
      for (int i = 0; i < room.incomingPortals.Length; i++) portalMask <<= 1;

      var maskStateCache = new ulong[room.stateList.Count][];
      for (ulong checkState = 0; checkState < room.stateList.Count; checkState++)
      {
        if (Tools.TickRefresh() && !scanInfo("Build State-Mask: " + checkState.ToString("N0") + " / " + room.stateList.Count.ToString("N0"))) return false;
        var list = new List<ulong>();
        for (ulong mask = 0; mask < portalMask; mask++)
        {
          bool valid = true; // merkt sich, ob der Kistenwechsel gütig ist
          ulong state = checkState;
          for (int iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            if ((mask & 1UL << iPortalIndex) == 0) continue;

            // --- Kiste über das Portal herrausziehen ---
            ulong nextState = reverseMap.portalStateSwaps[iPortalIndex].Get(state);
            if (nextState == state) { valid = false; break; } // ungültige Kiste erkannt?
            state = nextState; // neuen Kistenzustand übernehmen
          }
          if (!valid) continue; // ungültigen Kistenzustand erkannt?
          list.Add(state);
        }
        maskStateCache[checkState] = list.ToArray();
      }

      #region # // --- erste Aufgaben sammeln ---
      foreach (var vMap in reverseMap.variantMap)
      {
        var variantData = room.variantList.GetData(vMap.variant);
        if (variantData.newState != 0) continue; // alle Varianten filtern, welche nicht das Ende erreichen

        usedVariants.SetBit(vMap.variant); // End-Variante als benutzt markieren
        usedVariantCount++;
        if (!usedVariantsForward.GetBit(vMap.variant)) continue; // unbenutzte Vorwärts-Varianten nicht weiter beachten

        var newTask = new DeadlockTask
        (
          vMap.iPortalIndex,                         // Portalnummer, worüber der Spieler den Raum betreten hat
          variantData.oPortalIndexBoxes.Length > 0,  // Angabe, ob der Spieler eine Kiste mitgenommen hat
          variantData.oldState                       // vorheriger Kistenzustand des Raumes
        );
        tasks.Push(newTask);
      }

      // --- Portal-Swaps, welche das Ende erreichen könnten hinzufügen ---
      for (int i = 0; i < room.incomingPortals.Length; i++)
      {
        ulong newState = reverseMap.portalStateSwaps[i].Get(0);
        if (newState == 0) continue; // kein gültiger Portal-Swap
        var newTask = new DeadlockTask
        (
          uint.MaxValue,  // Portalnummer, worüber der Spieler den Raum betreten hat
          true,           // Angabe, ob der Spieler eine Kiste mitgenommen hat
          newState        // vorheriger Kistenzustand des Raumes
        );
        tasks.Push(newTask);
      }
      #endregion

      while (tasks.Count > 0)
      {
        if (Tools.TickRefresh())
        {
          if (!scanInfo(usedVariantCount.ToString("N0") + " / " + usedVariants.Length.ToString("N0") + ", Tasks: " + tasks.Count.ToString("N0"))) return false;
          if (usedVariantCount == usedVariants.Length) return true; // alle Varianten sind in Benutzung?
        }

        var task = tasks.Pop();

        // --- Varianten mit allen möglichen Kistenwechseln abarbeiten ---
        foreach (ulong state in maskStateCache[task.state])
        {
          for (uint oPortalIndex = 0; oPortalIndex < room.outgoingPortals.Length; oPortalIndex++)
          {
            foreach (var vMap in reverseMap.GetVariants(oPortalIndex, state, usedVariants))
            {
              usedVariants.SetBit(vMap.variant);
              usedVariantCount++;

              var variantData = room.variantList.GetData(vMap.variant);

              if (vMap.iPortalIndex == uint.MaxValue) continue; // Start-Varianten brauchen nicht weiter verfolgt werden

              var newTask = new DeadlockTask
              (
                vMap.iPortalIndex,                        // Portalnummer, worüber der Spieler den Raum betreten hat
                variantData.oPortalIndexBoxes.Length > 0, // Angabe, ob der Spieler eine Kiste reingezogen hat
                variantData.oldState                      // vorheriger Raum-Zustand
              );
              tasks.Push(newTask);
            }
          }
        }
      }

      return true;
    }

    /// <summary>
    /// entfernt unbenutzte Varianten
    /// </summary>
    public ulong Step4_RemoveUnusedVariants()
    {
      if (usedVariantsForward.CountMarkedBits(0) == usedVariantsForward.Length && usedVariantsBackward.CountMarkedBits(0) == usedVariantsBackward.Length) return 0; // weiter zu Rechnen macht keinen Sinn

      usedVariantsForward.FullAnd(usedVariantsBackward);

      OptimizeTools.RenewVariants(room, new SkipMapper(usedVariantsForward));

      OptimizeTools.RemoveUnusedStates(room);

      return usedVariantsForward.Length - usedVariantsForward.TotalCountMarkedBits;
    }

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (reverseMap != null)
      {
        reverseMap.Dispose();
        usedVariantsForward.Dispose();
        usedVariantsBackward.Dispose();
        reverseMap = null;
      }
    }

    /// <summary>
    /// Destruktor
    /// </summary>
    ~RoomDeadlockScanner()
    {
      Dispose();
    }
    #endregion
  }
}
