using System;
using System.Collections.Generic;

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse zum Suchen von nicht lösbaren Varianten in einem Raum
  /// </summary>
  public sealed class RoomDeadlockScanner
  {
    /// <summary>
    /// merkt sich den Raum, welcher optimiert werden soll
    /// </summary>
    readonly Room room;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Raum, welcher durchsucht werden soll</param>
    public RoomDeadlockScanner(Room room)
    {
      if (room == null) throw new NullReferenceException("room");
      this.room = room;
    }

    /// <summary>
    /// erstellt die Rückwärts-Varianten für die Suche
    /// </summary>
    public void Step1_CreateReverseMap()
    {
      var reverseMap = new RoomReverse(room);

      reverseMap.Step1_CollectVariantsPerState();


    }

    /// <summary>
    /// Vorwärts-Suche nach allen erreichbaren Varianten
    /// </summary>
    public void Step2_StartScan()
    {
      var tasks = new List<MergeTask>();
      var variantList = room.variantList;

      using (var usingVariants = new Bitter(variantList.Count))
      {
        if (room.startVariantCount > 0) // Start-Varianten corhanden?
        {
          usingVariants.SetBits(0, room.startVariantCount);
          #region # // --- erste Aufgaben sammeln ---
          for (ulong variant = 0; variant < room.startVariantCount; variant++)
          {
            var variantData = variantList.GetData(variant);


          }
          #endregion
        }
        else
        {
          // todo: Start-Zustand verwenden für erste Varianten der eingehenden Portale
        }

      }
    }
  }
}
