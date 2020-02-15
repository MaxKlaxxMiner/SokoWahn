#region # using *.*
using System;
using System.Diagnostics;
using System.Linq;
// ReSharper disable NotAccessedField.Local
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable RedundantIfElseBlock
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum verschmelzen von zwei Räumen
  /// </summary>
  public class RoomMerger
  {
    /// <summary>
    /// merkt sich das Räume-Netzwerk, welches bearbeitet werden soll
    /// </summary>
    readonly RoomNetwork network;

    /// <summary>
    /// merkt sich den ersten Raum, welcher verschmolzen werden soll
    /// </summary>
    readonly Room srcRoom1;
    /// <summary>
    /// merkt sich den zweiten Raum, welcher verschmolzen werden soll
    /// </summary>
    readonly Room srcRoom2;
    /// <summary>
    /// merkt sich den neuen Raum, welcher neu erstellt wird
    /// </summary>
    readonly Room newRoom;
    /// <summary>
    /// merkt sich die Portale beider alten Räume nach neuen Portal-Index
    /// </summary>
    readonly RoomPortal[] mapOldIncomingPortals;
    /// <summary>
    /// Mapping für die Portal-Nummern im ersten Quell-Raum
    /// </summary>
    readonly uint[] mapPortalIndex1;
    /// <summary>
    /// Mapping für die Portal-Nummer in zweiten Quell-Raum
    /// </summary>
    readonly uint[] mapPortalIndex2;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="network">Räume-Netzwerk, welches betroffen ist</param>
    /// <param name="srcRoom1">erster Raum, welcher verscholzen werden soll</param>
    /// <param name="srcRoom2">zweiter Raum, welcher verschmolzen werden soll</param>
    public RoomMerger(RoomNetwork network, Room srcRoom1, Room srcRoom2)
    {
      if (network == null) throw new NullReferenceException("network");
      if (srcRoom1 == null) throw new NullReferenceException("room1");
      if (srcRoom2 == null) throw new NullReferenceException("room2");
      if (ReferenceEquals(srcRoom1, srcRoom2)) throw new Exception("room1 == room2");
      if (!ReferenceEquals(network.rooms[srcRoom1.roomIndex], srcRoom1)) throw new Exception("invalid room1");
      if (!ReferenceEquals(network.rooms[srcRoom2.roomIndex], srcRoom2)) throw new Exception("invalid room2");
      if (srcRoom1.outgoingPortals.All(oPortal => !ReferenceEquals(oPortal.toRoom, srcRoom2))) throw new Exception("no connected Portals");
      if (srcRoom2.outgoingPortals.All(oPortal => !ReferenceEquals(oPortal.toRoom, srcRoom1))) throw new Exception("no connected Portals");

      // --- Räume tauschen, wenn die Reihenfolge des ersten Feldes besser passt ---
      if (srcRoom1.fieldPosis.First() > srcRoom2.fieldPosis.First())
      {
        var tmp = srcRoom1;
        srcRoom1 = srcRoom2;
        srcRoom2 = tmp;
      }

      this.network = network;
      this.srcRoom1 = srcRoom1;
      this.srcRoom2 = srcRoom2;

      // --- innere Portale ermitteln, welche durchs Verschmelzen aufgelöst werden ---
      var innerIncomingPortalsRoom1 = srcRoom1.incomingPortals.Where(iPortal => ReferenceEquals(iPortal.fromRoom, srcRoom2)).ToArray();
      var innerIncomingPortalsRoom2 = srcRoom2.incomingPortals.Where(iPortal => ReferenceEquals(iPortal.fromRoom, srcRoom1)).ToArray();
      Debug.Assert(innerIncomingPortalsRoom1.Length > 0);
      Debug.Assert(innerIncomingPortalsRoom1.Length == innerIncomingPortalsRoom2.Length);

      // --- äußere Portale ermitteln, welche durchs Verschmelzen erneuert werden ---
      var outerIncomingPortalsRoom1 = srcRoom1.incomingPortals.Where(iPortal => !ReferenceEquals(iPortal.fromRoom, srcRoom2)).ToArray();
      var outerIncomingPortalsRoom2 = srcRoom2.incomingPortals.Where(iPortal => !ReferenceEquals(iPortal.fromRoom, srcRoom1)).ToArray();
      Debug.Assert(outerIncomingPortalsRoom1.Length + innerIncomingPortalsRoom1.Length == srcRoom1.incomingPortals.Length);
      Debug.Assert(outerIncomingPortalsRoom2.Length + innerIncomingPortalsRoom2.Length == srcRoom2.incomingPortals.Length);

      // --- neue Portale erstellen und zugehörges Mapping befüllen ---
      var newIncomingPortals = new RoomPortal[outerIncomingPortalsRoom1.Length + outerIncomingPortalsRoom2.Length];
      var newOutgoingPortals = new RoomPortal[newIncomingPortals.Length];
      var newRoomIndex = Math.Min(srcRoom1.roomIndex, srcRoom2.roomIndex);
      newRoom = new Room(newRoomIndex, network.field, srcRoom1.fieldPosis.Concat(srcRoom2.fieldPosis).OrderBy(x => x).ToArray(), newIncomingPortals, newOutgoingPortals);

      mapOldIncomingPortals = new RoomPortal[newIncomingPortals.Length];
      mapPortalIndex1 = Enumerable.Range(0, srcRoom1.incomingPortals.Length).Select(x => uint.MaxValue).ToArray();
      mapPortalIndex2 = Enumerable.Range(0, srcRoom2.incomingPortals.Length).Select(x => uint.MaxValue).ToArray();
      for (uint iPortalIndex = 0; iPortalIndex < newIncomingPortals.Length; iPortalIndex++)
      {
        RoomPortal iPortalOld;
        if (iPortalIndex < outerIncomingPortalsRoom1.Length)
        {
          iPortalOld = outerIncomingPortalsRoom1[iPortalIndex];
          mapPortalIndex1[iPortalOld.iPortalIndex] = iPortalIndex;
        }
        else
        {
          iPortalOld = outerIncomingPortalsRoom2[iPortalIndex - outerIncomingPortalsRoom1.Length];
          mapPortalIndex2[iPortalOld.iPortalIndex] = iPortalIndex;
        }
        mapOldIncomingPortals[iPortalIndex] = iPortalOld;
        newIncomingPortals[iPortalIndex] = new RoomPortal(iPortalOld.fromRoom, iPortalOld.fromPos, newRoom, iPortalOld.toPos, iPortalIndex)
        {
          stateBoxSwap = new StateBoxSwapNormal(newRoom.stateList),
          variantStateDict = new VariantStateDictNormal(newRoom.stateList, newRoom.variantList)
        };
      }
    }
    #endregion

    #region # Step1_States() // Schritt 1: erstellt alle neuen Kisten-Zustände
    /// <summary>
    /// Schritt 1: erstellt alle neuen Kisten-Zustände
    /// </summary>
    public void Step1_States()
    {
      ulong state1Mul = srcRoom2.stateList.Count;
      var oldStateList1 = srcRoom1.stateList;
      var oldStateList2 = srcRoom2.stateList;
      var newStateList = newRoom.stateList;

      foreach (var state1 in oldStateList1)
      {
        foreach (var state2 in oldStateList2)
        {
          var newBoxPosis = new int[state1.Value.Length + state2.Value.Length];
          Array.Copy(state1.Value, newBoxPosis, state1.Value.Length);
          Array.Copy(state2.Value, 0, newBoxPosis, state1.Value.Length, state2.Value.Length);
          Array.Sort(newBoxPosis);
          Debug.Assert(newBoxPosis.All(boxPos => network.field.GetWalkPosis().Contains(boxPos)));
          Debug.Assert(newBoxPosis.GroupBy(x => x).Count() == newBoxPosis.Length);

          ulong newState = newStateList.Add(newBoxPosis);
          Debug.Assert(newState == state1.Key * state1Mul + state2.Key);
        }
      }
    }
    #endregion

    #region # Step2_StartVariants() // erstellt alle Startvarianten (falls der Spieler im eigenen Raum beginnt)
    /// <summary>
    /// erstellt alle Startvarianten (falls der Spieler im eigenen Raum beginnt)
    /// </summary>
    public void Step2_StartVariants()
    {
      var room1 = srcRoom1;
      var room2 = srcRoom2;

      if (room1.startVariantCount + room2.startVariantCount == 0) return; // keine Startvarianten vorhanden?

      throw new NotImplementedException("todo");
    }
    #endregion

    #region # Step3_PortalVariants() // Portale mit allen Varianten veschmelzen
    /// <summary>
    /// Portale mit allen Varianten veschmelzen
    /// </summary>
    public void Step3_PortalVariants()
    {
      var room1 = srcRoom1;
      var room2 = srcRoom2;
      var newIncomingPortals = newRoom.incomingPortals;
      ulong state1Mul = srcRoom2.stateList.Count;

      for (uint iPortalIndex = 0; iPortalIndex < newIncomingPortals.Length; iPortalIndex++)
      {
        var iPortalOld = mapOldIncomingPortals[iPortalIndex];
        var iPortalNew = newIncomingPortals[iPortalIndex];

        #region # // --- Portal-Kisten-Zustandsänderungen neu erstellen ---
        if (ReferenceEquals(iPortalOld.toRoom, room1))
        {
          foreach (var swap1 in iPortalOld.stateBoxSwap)
          {
            for (ulong state2 = 0; state2 < room2.stateList.Count; state2++)
            {
              ulong newStateFrom = swap1.Key * state1Mul + state2;
              ulong newStateTo = swap1.Value * state1Mul + state2;
              iPortalNew.stateBoxSwap.Add(newStateFrom, newStateTo);
            }
          }
        }
        else
        {
          Debug.Assert(ReferenceEquals(iPortalOld.toRoom, room2));
          for (ulong state1 = 0; state1 < room1.stateList.Count; state1++)
          {
            foreach (var swap2 in iPortalOld.stateBoxSwap)
            {
              ulong newStateFrom = state1 * state1Mul + swap2.Key;
              ulong newStateTo = state1 * state1Mul + swap2.Value;
              iPortalNew.stateBoxSwap.Add(newStateFrom, newStateTo);
            }
          }
        }
        #endregion

        #region # // --- reine Laufvarianten hinzufügen ---
        if (ReferenceEquals(iPortalOld.toRoom, room1))
        {
          MergeMoveVariants(iPortalOld, room2, (state1, state2) => state1 * state1Mul + state2, mapPortalIndex1, mapPortalIndex2, iPortalNew);
        }
        else
        {
          MergeMoveVariants(iPortalOld, room1, (state2, state1) => state1 * state1Mul + state2, mapPortalIndex2, mapPortalIndex1, iPortalNew);
        }
        #endregion

        #region # // --- Varianten mit Kistenverschiebungen hinzufügen ---
        if (ReferenceEquals(iPortalOld.toRoom, room1))
        {
          MergePushVariants(iPortalOld, room2, (state1, state2) => state1 * state1Mul + state2, mapPortalIndex1, mapPortalIndex2, iPortalNew);
        }
        else
        {
          MergePushVariants(iPortalOld, room1, (state2, state1) => state1 * state1Mul + state2, mapPortalIndex2, mapPortalIndex1, iPortalNew);
        }
        #endregion
      }
    }
    #endregion

    #region # MergeMoveVariants(...) // verschmilzt alle Lauf-Varianten eines Portales
    /// <summary>
    /// verschmilzt alle Lauf-Varianten eines Portales
    /// </summary>
    /// <param name="iPortal1">eingehendes altes Portal vom 1. Raum</param>
    /// <param name="room2">2. Raum</param>
    /// <param name="stateCalc">Methode zum berechnen der neuen Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="iPortalNew">Neues Portal, wohin die neu erzeugten Varianten gespeichert werden sollen</param>
    static void MergeMoveVariants(RoomPortal iPortal1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = iPortal1.toRoom;
      foreach (ulong state1 in iPortal1.variantStateDict.GetAllStates())
      {
        foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
        {
          var variantData1 = room1.variantList.GetData(variant1);
          if (variantData1.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
          Debug.Assert(variantData1.oPortalIndexPlayer < uint.MaxValue);
          Debug.Assert(variantData1.newState == variantData1.oldState);
          var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
          if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden sollen?
          {
            foreach (ulong state2 in iPortalOld2.variantStateDict.GetAllStates())
            {
              foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(state2).AsEnumerable())
              {
                var variantData2 = room2.variantList.GetData(variant2);
                if (variantData2.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
                Debug.Assert(variantData2.oPortalIndexPlayer < uint.MaxValue);
                Debug.Assert(variantData2.newState == variantData2.oldState);
                if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
                {
                  throw new NotImplementedException("todo"); // Schleife
                }
                else
                {
                  ulong newState = stateCalc(state1, state2);
                  uint oPortalIndexPlayer = mapPortalIndex2[variantData2.oPortalIndexPlayer];
                  Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
                  ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
                    newState,                                 // vorheriger Raum-Zustand
                    variantData1.moves + variantData2.moves,  // Anzahl der Laufschritte
                    0,                                        // Anzahl der Kistenverschiebungen
                    new uint[0],                              // rausgeschobene Kisten
                    oPortalIndexPlayer,                       // Portal, worüber der Spieler den Raum wieder verlässt
                    newState,                                 // nachfolgender Raum-Zustand
                    variantData1.path + variantData2.path     // Laufweg als Pfad
                  );
                  iPortalNew.variantStateDict.Add(newState, newVariant);
                }
              }
            }
          }
          else // ausgehendes Portal direkt erreicht
          {
            for (ulong state2 = 0; state2 < room2.stateList.Count; state2++) // 2. Raum könnte alle Zustände annehmen
            {
              ulong newState = stateCalc(state1, state2);
              uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
              Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
                newState,            // vorheriger Raum-Zustand
                variantData1.moves,  // Anzahl der Laufschritte
                0,                   // Anzahl der Kistenverschiebungen
                new uint[0],         // rausgeschobene Kisten
                oPortalIndexPlayer,  // Portal, worüber der Spieler den Raum wieder verlässt
                newState,            // nachfolgender Raum-Zustand
                variantData1.path    // Laufweg als Pfad
              );
              iPortalNew.variantStateDict.Add(newState, newVariant);
            }
          }
        }
      }
    }
    #endregion

    #region # MergePushVariants(...) // verschmilzt alle Varianten mit Kistenverschiebungen eines Portales (ohne End-Varianten)
    static void MergePushVariants(RoomPortal iPortal1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = iPortal1.toRoom;
      foreach (ulong state1 in iPortal1.variantStateDict.GetAllStates())
      {
        foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
        {
          var variantData1 = room1.variantList.GetData(variant1);
          if (variantData1.oPortalIndexPlayer == uint.MaxValue) break; // End-Varianten noch nicht übertragen
          var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
          if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden sollen?
          {
            throw new NotImplementedException("todo");
            //      foreach (ulong state2 in iPortalOld2.variantStateDict.GetAllStates())
            //      {
            //        foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(state2).AsEnumerable())
            //        {
            //          var variantData2 = room2.variantList.GetData(variant2);
            //          if (variantData2.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
            //          Debug.Assert(variantData2.oPortalIndexPlayer < uint.MaxValue);
            //          Debug.Assert(variantData2.newState == variantData2.oldState);
            //          if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
            //          {
            //            throw new NotImplementedException("todo"); // Schleife
            //          }
            //          else
            //          {
            //            ulong newState = stateCalc(state1, state2);
            //            uint oPortalIndexPlayer = mapPortalIndex2[variantData2.oPortalIndexPlayer];
            //            Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
            //            ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
            //              newState,                                 // vorheriger Raum-Zustand
            //              variantData1.moves + variantData2.moves,  // Anzahl der Laufschritte
            //              0,                                        // Anzahl der Kistenverschiebungen
            //              new uint[0],                              // rausgeschobene Kisten
            //              oPortalIndexPlayer,                       // Portal, worüber der Spieler den Raum wieder verlässt
            //              newState,                                 // nachfolgender Raum-Zustand
            //              variantData1.path + variantData2.path     // Laufweg als Pfad
            //            );
            //            iPortalNew.variantStateDict.Add(newState, newVariant);
            //          }
            //        }
            //      }
          }
          else // ausgehendes Portal direkt erreicht
          {
            throw new NotImplementedException("todo");
            //      for (ulong state2 = 0; state2 < room2.stateList.Count; state2++) // 2. Raum könnte alle Zustände annehmen
            //      {
            //        ulong newState = stateCalc(state1, state2);
            //        uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
            //        Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
            //        ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
            //          newState,            // vorheriger Raum-Zustand
            //          variantData1.moves,  // Anzahl der Laufschritte
            //          0,                   // Anzahl der Kistenverschiebungen
            //          new uint[0],         // rausgeschobene Kisten
            //          oPortalIndexPlayer,  // Portal, worüber der Spieler den Raum wieder verlässt
            //          newState,            // nachfolgender Raum-Zustand
            //          variantData1.path    // Laufweg als Pfad
            //        );
            //        iPortalNew.variantStateDict.Add(newState, newVariant);
            //      }
          }
        }
      }
    }
    #endregion
  }
}
