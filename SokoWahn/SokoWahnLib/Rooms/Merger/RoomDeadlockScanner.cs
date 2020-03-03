using System;
using System.Collections.Generic;

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

      this.reverseMap = reverseMap;
    }

    /// <summary>
    /// Vorwärts-Suche nach allen erreichbaren Varianten
    /// </summary>
    public void Step2_ScanForward()
    {
      var tasks = new Stack<DeadlockTask>();
      var room = this.room;
      var usedVariants = usedVariantsForward;

      #region # // --- erste Aufgaben sammeln ---
      if (room.startVariantCount > 0) // Start-Varianten vorhanden?
      {
        usedVariants.SetBits(0, room.startVariantCount);
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

      ulong portalMask = 1;
      for (int i = 0; i < room.incomingPortals.Length; i++) portalMask <<= 1;

      while (tasks.Count > 0)
      {
        var task = tasks.Pop();

        for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
        {
          if (iPortalIndex == task.oPortalIndexPlayer && !task.exportedBox) continue; // einfaches Zurücklaufen durch das gleiche Portal nicht erlaubt, außer wenn vorher eine Kiste aus dem Raum geschoben wurde

          foreach (ulong variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(task.state).AsEnumerable())
          {
            if (usedVariants.GetBit(variant)) continue; // Variante schon bekannt?
            usedVariants.SetBit(variant);

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

        for (ulong mask = 1; mask < portalMask; mask++)
        {
          bool valid = true; // merkt sich, ob die Kisten-Variante gütig ist
          ulong state = task.state;
          for (int iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            if ((mask & 1UL << iPortalIndex) == 0) continue;

            // --- Kiste über das Portal reinschieben ---
            ulong nextState = room.incomingPortals[iPortalIndex].stateBoxSwap.Get(state);
            if (nextState == state) { valid = false; break; } // ungültige Kiste erkannt?
            state = nextState; // neuen Kistenzustand übernehmen
          }
          if (!valid) continue; // ungültigen Kistenzustand erkannt?

          for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            foreach (ulong variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(state).AsEnumerable())
            {
              if (usedVariants.GetBit(variant)) continue; // Variante schon bekannt?
              usedVariants.SetBit(variant);

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
        }
      }
    }

    /// <summary>
    /// Rückwärts-Suche nach allen erreichbaren Varianten
    /// </summary>
    public void Step3_ScanBackward()
    {
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
        reverseMap = null;
      }

      usedVariantsForward.Dispose();
      usedVariantsBackward.Dispose();
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
