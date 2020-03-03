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
          if (variantData.oPortalIndexPlayer == uint.MaxValue) continue; // End-Varianten können nicht weiter verfolgt werden

          var newTask = new DeadlockTask
          (
            variantData.oPortalIndexPlayer,
            variantData.oPortalIndexBoxes.Contains(variantData.oPortalIndexPlayer),
            variantData.newState
          );
          tasks.Push(newTask);
        }
      }
      else
      {
        var newTask = new DeadlockTask
        (
          uint.MaxValue,
          false,
          room.startState
        );
        tasks.Push(newTask);
      }
      #endregion
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
