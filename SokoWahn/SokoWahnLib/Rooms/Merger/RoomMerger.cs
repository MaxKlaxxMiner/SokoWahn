#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable NotAccessedField.Local
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable RedundantIfElseBlock
#endregion

namespace SokoWahnLib.Rooms.Merger
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
    /// merkt sich die Portale der beiden alten Räume nach neuen Portal-Index
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
      newRoom = new Room(uint.MaxValue, network.field, srcRoom1.fieldPosis.Concat(srcRoom2.fieldPosis).OrderBy(x => x).ToArray(), newIncomingPortals, newOutgoingPortals);

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

    #region # // Step1_States() // Schritt 1: erstellt alle neuen Kisten-Zustände
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

      newRoom.startState = srcRoom1.startState * state1Mul + srcRoom2.startState;
    }
    #endregion

    #region # // Step2_StartVariants() // erstellt alle Startvarianten (falls der Spieler im eigenen Raum beginnt)
    /// <summary>
    /// erstellt alle Startvarianten (falls der Spieler im eigenen Raum beginnt)
    /// </summary>
    public void Step2_StartVariants()
    {
      var room1 = srcRoom1;
      var room2 = srcRoom2;
      ulong state1Mul = srcRoom2.stateList.Count;

      if (room1.startVariantCount + room2.startVariantCount == 0) return; // keine Startvarianten vorhanden?
      Debug.Assert(room1.startVariantCount == 0 || room2.startVariantCount == 0);

      if (room1.startVariantCount > 0) // im ersten Raum wird gestartet
      {
        MergeMoveStartVariants(room1, room2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, newRoom);

        MergePushStartVariants(room1, room2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, newRoom);

        MergeStartEndVariants(room1, room2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, newRoom);
      }
      else // im zweiten Raum wird gestartet
      {
        MergeMoveStartVariants(room2, room1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, newRoom);

        MergePushStartVariants(room2, room1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, newRoom);

        MergeStartEndVariants(room2, room1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, newRoom);
      }
    }
    #endregion

    #region # // Step3_PortalVariants() // Portale mit allen Varianten veschmelzen
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

        #region # // --- Portal-Varianten verarbeiten ---
        if (ReferenceEquals(iPortalOld.toRoom, room1))
        {
          for (ulong state1 = 0; state1 < room1.stateList.Count; state1++)
          {
            for (ulong state2 = 0; state2 < room2.stateList.Count; state2++)
            {
              MergeMoveVariants(iPortalOld, room2, state1, state2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, iPortalNew);

              MergePushVariants(iPortalOld, room2, state1, state2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, iPortalNew);

              MergeEndVariants(iPortalOld, room2, state1, state2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, iPortalNew);
            }
          }
        }
        else
        {
          for (ulong state2 = 0; state2 < room2.stateList.Count; state2++)
          {
            for (ulong state1 = 0; state1 < room1.stateList.Count; state1++)
            {
              MergeMoveVariants(iPortalOld, room1, state2, state1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, iPortalNew);

              MergePushVariants(iPortalOld, room1, state2, state1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, iPortalNew);

              MergeEndVariants(iPortalOld, room1, state2, state1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, iPortalNew);
            }
          }
        }
        #endregion
      }
    }
    #endregion

    #region # // Step4_UpdatePortals() // aktualisiert die Portale und deren Verlinkungen
    /// <summary>
    /// aktualisiert die Portale und deren Verlinkungen
    /// </summary>
    public void Step4_UpdatePortals()
    {
      var room = newRoom;
      var iPortals = room.incomingPortals;
      var oPortals = room.outgoingPortals;

      // --- ausgehende Portale neu verlinken ---
      for (uint portalIndex = 0; portalIndex < iPortals.Length; portalIndex++)
      {
        var oPortal = mapOldIncomingPortals[portalIndex].oppositePortal;
        iPortals[portalIndex].oppositePortal = oPortal;
        oPortals[portalIndex] = oPortal;
        oPortal.oppositePortal = iPortals[portalIndex];
        oPortal.toRoom.outgoingPortals[oPortal.iPortalIndex] = iPortals[portalIndex];
      }

      // --- Räume in den Portale neu setzen ---
      for (uint iPortalIndex = 0; iPortalIndex < iPortals.Length; iPortalIndex++)
      {
        var oPortal = mapOldIncomingPortals[iPortalIndex].oppositePortal;
        oPortal.fromRoom = room;
      }
    }
    #endregion

    #region # // Step5_UpdateRooms() // aktualisiert die Räume und deren Indizierungen
    /// <summary>
    /// aktualisiert die Räume und deren Indizierungen
    /// </summary>
    public void Step5_UpdateRooms()
    {
      uint fillRoomIndex = 0;
      for (uint roomIndex = 0; roomIndex < network.rooms.Length; roomIndex++)
      {
        var room = network.rooms[roomIndex];
        if (ReferenceEquals(room, srcRoom1)) room = newRoom; // 1. Raum überschreiben
        if (ReferenceEquals(room, srcRoom2)) continue; // 2. Raum überspringen
        room.roomIndex = fillRoomIndex;
        network.rooms[fillRoomIndex] = room;
        fillRoomIndex++;
      }
      if (fillRoomIndex + 1 != network.rooms.Length) throw new Exception("Room-Index error");
      Array.Resize(ref network.rooms, (int)fillRoomIndex);
    }
    #endregion

    #region # // MergeMoveStartVariants(...) // verschmilzt alle Lauf-Varianten, welche beim Start beginnen
    /// <summary>
    /// verschmilzt alle Lauf-Varianten, welche beim Start beginnen
    /// </summary>
    /// <param name="room1">erster Raum, wo gestartet wird</param>
    /// <param name="room2">zweiter Raum, welcher verschmolzen wird</param>
    /// <param name="stateCalc">Funktion zum berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="roomNew">neuer Raum, wohin die neuen Startvarianten gespeichert werden sollen</param>
    static void MergeMoveStartVariants(Room room1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, Room roomNew)
    {
      ulong newState = stateCalc(room1.startState, room2.startState);
      var tasks1 = new List<MoveSearchTask>();
      for (ulong variant1 = 0; variant1 < room1.startVariantCount; variant1++)
      {
        var variantData1 = room1.variantList.GetData(variant1);
        if (variantData1.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
        Debug.Assert(variantData1.oPortalIndexPlayer < uint.MaxValue);
        Debug.Assert(variantData1.newState == variantData1.oldState);
        var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
        if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden sollen?
        {
          tasks1.Add(new MoveSearchTask
          {
            oPortalIndexPlayer = variantData1.oPortalIndexPlayer,
            moves = variantData1.moves,
            path = variantData1.path
          });
        }
        else
        {
          uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
          Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
          ulong newVariant = roomNew.variantList.Add(
            newState,            // vorheriger Raum-Zustand
            variantData1.moves,  // Anzahl der Laufschritte
            0,                   // Anzahl der Kistenverschiebungen
            new uint[0],         // rausgeschobene Kisten
            oPortalIndexPlayer,  // Portal, worüber der Spieler den Raum wieder verlässt
            newState,            // nachfolgender Raum-Zustand
            variantData1.path    // Laufweg als Pfad
          );
          Debug.Assert(roomNew.startVariantCount == newVariant);
          roomNew.startVariantCount++;
        }
      }
      if (tasks1.Count == 0) return;

      var moveDict = new Dictionary<ulong, ulong>();
      var tasks2 = new List<MoveSearchTask>();
      while (tasks1.Count > 0)
      {
        foreach (var task1 in tasks1)
        {
          ulong crc = task1.GetCrc() + 1;
          ulong crcMoves;
          if (!moveDict.TryGetValue(crc, out crcMoves)) crcMoves = ulong.MaxValue;
          if (task1.moves >= crcMoves) continue; // bereits abgearbeitet?
          moveDict[crc] = task1.moves;

          var iPortalOld2 = room1.outgoingPortals[task1.oPortalIndexPlayer];
          foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(room2.startState).AsEnumerable())
          {
            var variantData2 = room2.variantList.GetData(variant2);
            if (variantData2.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
            Debug.Assert(variantData2.oPortalIndexPlayer < uint.MaxValue);
            Debug.Assert(variantData2.newState == variantData2.oldState);
            if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
            {
              tasks2.Add(new MoveSearchTask
              {
                oPortalIndexPlayer = variantData2.oPortalIndexPlayer,
                moves = task1.moves + variantData2.moves,
                path = task1.path + variantData2.path
              });
            }
            else
            {
              uint oPortalIndexPlayer = mapPortalIndex2[variantData2.oPortalIndexPlayer];
              Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              ulong newVariant = roomNew.variantList.Add(
                newState,                          // vorheriger Raum-Zustand
                task1.moves + variantData2.moves,  // Anzahl der Laufschritte
                0,                                 // Anzahl der Kistenverschiebungen
                new uint[0],                       // rausgeschobene Kisten
                oPortalIndexPlayer,                // Portal, worüber der Spieler den Raum wieder verlässt
                newState,                          // nachfolgender Raum-Zustand
                task1.path + variantData2.path     // Laufweg als Pfad
              );
              Debug.Assert(roomNew.startVariantCount == newVariant);
              roomNew.startVariantCount++;
            }
          }
        }
        tasks1.Clear();

        foreach (var task2 in tasks2)
        {
          ulong crc = task2.GetCrc() + 2;
          ulong crcMoves;
          if (!moveDict.TryGetValue(crc, out crcMoves)) crcMoves = ulong.MaxValue;
          if (task2.moves >= crcMoves) continue; // bereits abgearbeitet?
          moveDict[crc] = task2.moves;

          var iPortalOld1 = room2.outgoingPortals[task2.oPortalIndexPlayer];
          foreach (ulong variant1 in iPortalOld1.variantStateDict.GetVariantSpan(room1.startState).AsEnumerable())
          {
            var variantData1 = room1.variantList.GetData(variant1);
            if (variantData1.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
            Debug.Assert(variantData1.oPortalIndexPlayer < uint.MaxValue);
            Debug.Assert(variantData1.newState == variantData1.oldState);
            if (ReferenceEquals(room1.outgoingPortals[variantData1.oPortalIndexPlayer].toRoom, room2))
            {
              tasks1.Add(new MoveSearchTask
              {
                oPortalIndexPlayer = variantData1.oPortalIndexPlayer,
                moves = task2.moves + variantData1.moves,
                path = task2.path + variantData1.path
              });
            }
            else
            {
              uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
              Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              ulong newVariant = roomNew.variantList.Add(
                newState,                          // vorheriger Raum-Zustand
                task2.moves + variantData1.moves,  // Anzahl der Laufschritte
                0,                                 // Anzahl der Kistenverschiebungen
                new uint[0],                       // rausgeschobene Kisten
                oPortalIndexPlayer,                // Portal, worüber der Spieler den Raum wieder verlässt
                newState,                          // nachfolgender Raum-Zustand
                task2.path + variantData1.path     // Laufweg als Pfad
              );
              Debug.Assert(roomNew.startVariantCount == newVariant);
              roomNew.startVariantCount++;
            }
          }
        }
        tasks2.Clear();
      }
    }
    #endregion

    #region # // MergePushStartVariants(...) // verschmilzt alle Start-Varianten mit Kistenverschiebungen des Start-Raumes (ohne End-Varianten)
    /// <summary>
    /// verschmilzt alle Start-Varianten mit Kistenverschiebungen des Start-Raumes (ohne End-Varianten)
    /// </summary>
    /// <param name="room1">erster Raum, wo gestartet wird</param>
    /// <param name="room2">zweiter Raum, welcher verschmolzen wird</param>
    /// <param name="stateCalc">Funktion zum berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="roomNew">neuer Raum, wohin die neuen Startvarianten gespeichert werden sollen</param>
    static void MergePushStartVariants(Room room1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, Room roomNew)
    {
      return;
      throw new NotImplementedException();
    }
    #endregion

    #region # // MergeStartEndVariants(...) // verschmilzt alle Start-Varianten des Start-Raumes, welche das Spielende erreichen
    /// <summary>
    /// verschmilzt alle Start-Varianten des Start-Raumes, welche das Spielende erreichen
    /// </summary>
    /// <param name="room1">erster Raum, wo gestartet wird</param>
    /// <param name="room2">zweiter Raum, welcher verschmolzen wird</param>
    /// <param name="stateCalc">Funktion zum berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="roomNew">neuer Raum, wohin die neuen Startvarianten gespeichert werden sollen</param>
    static void MergeStartEndVariants(Room room1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, Room roomNew)
    {
      return;
      throw new NotImplementedException();
    }
    #endregion

    #region # // MergeMoveVariants(...) // verschmilzt alle Lauf-Varianten eines Portales
    /// <summary>
    /// verschmilzt alle reinen Lauf-Varianten eines Portales
    /// </summary>
    /// <param name="iPortal1">eingehendes altes Portal vom 1. Raum</param>
    /// <param name="room2">2. Raum</param>
    /// <param name="state1">Kistenzustand vom 1. Raum</param>
    /// <param name="state2">Kistenzustand vom 2. Raum</param>
    /// <param name="stateCalc">Funktion zum berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="iPortalNew">neues Portal, wohin die neu erzeugten Varianten gespeichert werden sollen</param>
    static void MergeMoveVariants(RoomPortal iPortal1, Room room2, ulong state1, ulong state2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = iPortal1.toRoom;
      ulong newState = stateCalc(state1, state2);
      var tasks1 = new List<MoveSearchTask>();
      foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
      {
        var variantData1 = room1.variantList.GetData(variant1);
        if (variantData1.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
        Debug.Assert(variantData1.oPortalIndexPlayer < uint.MaxValue);
        Debug.Assert(variantData1.newState == variantData1.oldState);
        var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
        if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden sollen?
        {
          tasks1.Add(new MoveSearchTask
          {
            oPortalIndexPlayer = variantData1.oPortalIndexPlayer,
            moves = variantData1.moves,
            path = variantData1.path
          });
        }
        else
        {
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
      if (tasks1.Count == 0) return;

      var moveDict = new Dictionary<ulong, ulong>();
      var tasks2 = new List<MoveSearchTask>();
      while (tasks1.Count > 0)
      {
        foreach (var task1 in tasks1)
        {
          ulong crc = task1.GetCrc() + 1;
          ulong crcMoves;
          if (!moveDict.TryGetValue(crc, out crcMoves)) crcMoves = ulong.MaxValue;
          if (task1.moves >= crcMoves) continue; // bereits abgearbeitet?
          moveDict[crc] = task1.moves;

          var iPortalOld2 = room1.outgoingPortals[task1.oPortalIndexPlayer];
          foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(state2).AsEnumerable())
          {
            var variantData2 = room2.variantList.GetData(variant2);
            if (variantData2.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
            Debug.Assert(variantData2.oPortalIndexPlayer < uint.MaxValue);
            Debug.Assert(variantData2.newState == variantData2.oldState);
            if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
            {
              tasks2.Add(new MoveSearchTask
              {
                oPortalIndexPlayer = variantData2.oPortalIndexPlayer,
                moves = task1.moves + variantData2.moves,
                path = task1.path + variantData2.path
              });
            }
            else
            {
              uint oPortalIndexPlayer = mapPortalIndex2[variantData2.oPortalIndexPlayer];
              Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
                newState,                          // vorheriger Raum-Zustand
                task1.moves + variantData2.moves,  // Anzahl der Laufschritte
                0,                                 // Anzahl der Kistenverschiebungen
                new uint[0],                       // rausgeschobene Kisten
                oPortalIndexPlayer,                // Portal, worüber der Spieler den Raum wieder verlässt
                newState,                          // nachfolgender Raum-Zustand
                task1.path + variantData2.path     // Laufweg als Pfad
              );
              iPortalNew.variantStateDict.Add(newState, newVariant);
            }
          }
        }
        tasks1.Clear();

        foreach (var task2 in tasks2)
        {
          ulong crc = task2.GetCrc() + 2;
          ulong crcMoves;
          if (!moveDict.TryGetValue(crc, out crcMoves)) crcMoves = ulong.MaxValue;
          if (task2.moves >= crcMoves) continue; // bereits abgearbeitet?
          moveDict[crc] = task2.moves;

          var iPortalOld1 = room2.outgoingPortals[task2.oPortalIndexPlayer];
          foreach (ulong variant1 in iPortalOld1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
          {
            var variantData1 = room1.variantList.GetData(variant1);
            if (variantData1.pushes > 0) break; // Varianten mit Kistenverschiebungen jetzt noch nicht übertragen
            Debug.Assert(variantData1.oPortalIndexPlayer < uint.MaxValue);
            Debug.Assert(variantData1.newState == variantData1.oldState);
            if (ReferenceEquals(room1.outgoingPortals[variantData1.oPortalIndexPlayer].toRoom, room2))
            {
              tasks1.Add(new MoveSearchTask
              {
                oPortalIndexPlayer = variantData1.oPortalIndexPlayer,
                moves = task2.moves + variantData1.moves,
                path = task2.path + variantData1.path
              });
            }
            else
            {
              if (variantData1.oPortalIndexPlayer == iPortal1.iPortalIndex) continue; // ausgendes Portal = eingehendes Portal?
              uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
              Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
                newState,                          // vorheriger Raum-Zustand
                task2.moves + variantData1.moves,  // Anzahl der Laufschritte
                0,                                 // Anzahl der Kistenverschiebungen
                new uint[0],                       // rausgeschobene Kisten
                oPortalIndexPlayer,                // Portal, worüber der Spieler den Raum wieder verlässt
                newState,                          // nachfolgender Raum-Zustand
                task2.path + variantData1.path     // Laufweg als Pfad
              );
              iPortalNew.variantStateDict.Add(newState, newVariant);
            }
          }
        }
        tasks2.Clear();
      }
    }
    #endregion

    #region # // MergePushVariants(...) // verschmilzt alle Varianten mit Kistenverschiebungen eines Portales (ohne End-Varianten)
    /// <summary>
    /// verschmilzt alle Varianten mit Kistenverschiebungen eines Portales (ohne End-Varianten)
    /// </summary>
    /// <param name="iPortal1">eingehendes altes Portal vom 1. Raum</param>
    /// <param name="room2">2. Raum</param>
    /// <param name="state1">Kistenzustand vom 1. Raum</param>
    /// <param name="state2">Kistenzustand vom 2. Raum</param>
    /// <param name="stateCalc">Methode zum berechnen der neuen Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="iPortalNew">Neues Portal, wohin die neu erzeugten Varianten gespeichert werden sollen</param>
    static void MergePushVariants(RoomPortal iPortal1, Room room2, ulong state1, ulong state2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = iPortal1.toRoom;
      ulong newState = stateCalc(state1, state2);
      foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
      {
        var variantData1 = room1.variantList.GetData(variant1);
        if (variantData1.oPortalIndexPlayer == uint.MaxValue) break; // End-Varianten noch nicht übertragen

        // --- Kisten verarbeiten ---
        ulong nextState2 = state2;
        foreach (uint boxPortalIndex in variantData1.oPortalIndexBoxes)
        {
          if (mapPortalIndex1[boxPortalIndex] == uint.MaxValue) // Kisten-Verschiebung nach Raum 2
          {
            throw new NotImplementedException();
          }
        }

        var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
        if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden soll?
        {
          foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(nextState2).AsEnumerable())
          {
            var variantData2 = room2.variantList.GetData(variant2);
            if (variantData2.oPortalIndexPlayer == uint.MaxValue) break; // End-Varianten noch nicht übertragen

            // --- Kisten verarbeiten ---
            ulong nextState1 = state1;
            foreach (uint boxPortalIndex in variantData2.oPortalIndexBoxes)
            {
              if (mapPortalIndex2[boxPortalIndex] == uint.MaxValue) // Kisten-Verschiebung nach Raum 1
              {
                throw new NotImplementedException();
              }
            }

            if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
            {
              continue;
              throw new NotImplementedException(); // rekursiver Aufruf
            }
            else
            {
              if (variantData1.pushes + variantData2.pushes == 0) continue; // keine Kistenverschiebungen gefunden?
              throw new NotImplementedException();
              //  ulong newState = stateCalc(state1, state2);
              //  uint oPortalIndexPlayer = mapPortalIndex2[variantData2.oPortalIndexPlayer];
              //  Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
              //  ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
              //    newState,                                 // vorheriger Raum-Zustand
              //    variantData1.moves + variantData2.moves,  // Anzahl der Laufschritte
              //    0,                                        // Anzahl der Kistenverschiebungen
              //    new uint[0],                              // rausgeschobene Kisten
              //    oPortalIndexPlayer,                       // Portal, worüber der Spieler den Raum wieder verlässt
              //    newState,                                 // nachfolgender Raum-Zustand
              //    variantData1.path + variantData2.path     // Laufweg als Pfad
              //  );
              //  iPortalNew.variantStateDict.Add(newState, newVariant);
            }
          }
        }
        else // ausgehendes Portal direkt erreicht
        {
          if (variantData1.pushes == 0) continue; // keine Kistenverschiebungen erkannt?
          throw new NotImplementedException();
          //  for (ulong state2 = 0; state2 < room2.stateList.Count; state2++) // 2. Raum könnte alle Zustände annehmen
          //  {
          //    ulong newState = stateCalc(state1, state2);
          //    uint oPortalIndexPlayer = mapPortalIndex1[variantData1.oPortalIndexPlayer];
          //    Debug.Assert(oPortalIndexPlayer < uint.MaxValue);
          //    ulong newVariant = iPortalNew.variantStateDict.variantList.Add(
          //      newState,            // vorheriger Raum-Zustand
          //      variantData1.moves,  // Anzahl der Laufschritte
          //      0,                   // Anzahl der Kistenverschiebungen
          //      new uint[0],         // rausgeschobene Kisten
          //      oPortalIndexPlayer,  // Portal, worüber der Spieler den Raum wieder verlässt
          //      newState,            // nachfolgender Raum-Zustand
          //      variantData1.path    // Laufweg als Pfad
          //    );
          //    iPortalNew.variantStateDict.Add(newState, newVariant);
          //  }
        }
      }
    }
    #endregion

    #region # // MergeEndVariants(...) // verschmilzt alle Varianten eines Portales, welche das Spielende erreichen
    /// <summary>
    /// verschmilzt alle Varianten eines Portales, welche das Spielende erreichen
    /// </summary>
    /// <param name="iPortal1">eingehendes altes Portal vom 1. Raum</param>
    /// <param name="room2">2. Raum</param>
    /// <param name="state1">Kistenzustand vom 1. Raum</param>
    /// <param name="state2">Kistenzustand vom 2. Raum</param>
    /// <param name="stateCalc">Methode zum berechnen der neuen Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="iPortalNew">Neues Portal, wohin die neu erzeugten Varianten gespeichert werden sollen</param>
    static void MergeEndVariants(RoomPortal iPortal1, Room room2, ulong state1, ulong state2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = iPortal1.toRoom;
      ulong newState = stateCalc(state1, state2);
      foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
      {
        var variantData1 = room1.variantList.GetData(variant1);
        if (variantData1.oPortalIndexPlayer == uint.MaxValue) // End-Variante erreicht?
        {
          throw new NotImplementedException();
        }
        var iPortalOld2 = room1.outgoingPortals[variantData1.oPortalIndexPlayer];
        if (ReferenceEquals(iPortalOld2.toRoom, room2)) // zeigt auf das ausgehende Portal in den benachbarten Raum, welcher ebenfalls verschmolzen werden sollen
        {
          foreach (ulong variant2 in iPortalOld2.variantStateDict.GetVariantSpan(state2).AsEnumerable())
          {
            var variantData2 = room2.variantList.GetData(variant2);
            if (variantData1.oPortalIndexPlayer == uint.MaxValue) // End-Variante erreicht?
            {
              throw new NotImplementedException();
            }
            if (ReferenceEquals(room2.outgoingPortals[variantData2.oPortalIndexPlayer].toRoom, room1))
            {
              continue;
              throw new NotImplementedException(); // rekursiver Aufruf
            }
          }
        }
      }
    }
    #endregion
  }
}
