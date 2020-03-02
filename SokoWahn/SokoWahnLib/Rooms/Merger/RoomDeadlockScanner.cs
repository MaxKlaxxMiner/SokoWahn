using System;
using System.Collections.Generic;

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse zum Suchen von nicht lösbaren Varianten in einem Raum
  /// </summary>
  public class RoomDeadlockScanner
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
    /// Vorwärts-Suche nach allen erreichbaren Varianten
    /// </summary>
    public void Step1_StartScan()
    {
      if (room.startVariantCount == 0) return; // keine Startvarianten gefunden

      var tasks = new List<MergeTask>();
      var variantList = room.variantList;

      using (var usingVariants = new Bitter(variantList.Count))
      {
        usingVariants.SetBits(0, room.startVariantCount);

        #region # // --- erste Aufgaben sammeln ---
        for (ulong variant = 0; variant < room.startVariantCount; variant++)
        {
          var variantData = variantList.GetData(variant);


        }
        #endregion
      }
    }
  }
}
