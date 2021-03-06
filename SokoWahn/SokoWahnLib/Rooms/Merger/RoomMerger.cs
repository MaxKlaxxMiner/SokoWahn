﻿#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedField.Local
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable RedundantIfElseBlock
#endregion

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse zum verschmelzen von zwei Räumen
  /// </summary>
  public sealed class RoomMerger
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
    public readonly Room newRoom;
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
      newRoom = new Room(uint.MaxValue, network.field, srcRoom1.fieldPosis.Concat(srcRoom2.fieldPosis).OrderBy(x => x).ToArray(), newIncomingPortals, newOutgoingPortals, srcRoom1.maxBoxes);

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
        newIncomingPortals[iPortalIndex] = new RoomPortal(iPortalOld.fromRoom, iPortalOld.fromPos, newRoom, iPortalOld.toPos, iPortalIndex, iPortalOld.blockedBox)
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
    public void Step1_MixStates()
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
    /// <param name="mergeInfo">Status-Meldung</param>
    public bool Step2_StartVariants(Func<string, bool> mergeInfo)
    {
      var room1 = srcRoom1;
      var room2 = srcRoom2;
      ulong state1Mul = srcRoom2.stateList.Count;

      if (room1.startVariantCount + room2.startVariantCount == 0) return true; // keine Startvarianten vorhanden?
      Debug.Assert(room1.startVariantCount == 0 || room2.startVariantCount == 0);

      if (room1.startVariantCount > 0) // im ersten Raum wird gestartet
      {
        return MergeStartVariants(room1, room2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, newRoom, mergeInfo);
      }
      else // im zweiten Raum wird gestartet
      {
        return MergeStartVariants(room2, room1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, newRoom, mergeInfo);
      }
    }
    #endregion

    #region # // Step3_PortalVariants() // Portale mit allen Varianten veschmelzen
    /// <summary>
    /// Portale mit allen Varianten veschmelzen
    /// </summary>
    /// <param name="mergeInfo">Status-Meldung</param>
    public bool Step3_PortalVariants(Func<string, bool> mergeInfo)
    {
      var room1 = srcRoom1;
      var room2 = srcRoom2;
      var newIncomingPortals = newRoom.incomingPortals;
      ulong state1Mul = srcRoom2.stateList.Count;

      ulong maxStepCount = (uint)newIncomingPortals.Length * room1.stateList.Count * room2.stateList.Count;
      ulong finishedStepCount = 0;
      ulong calcMoves = 0;

      for (uint iPortalIndex = 0; iPortalIndex < newIncomingPortals.Length; iPortalIndex++)
      {
        string txtLine = "portals " + (iPortalIndex + 1) + " / " + newIncomingPortals.Length + " - ";

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
              if (newRoom.stateList.Get(newStateTo).Length > newRoom.maxBoxes) continue; // werden mehr Kisten in den Raum geschoben als insgesamt vorhanden sind?
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
              if (newRoom.stateList.Get(newStateTo).Length > newRoom.maxBoxes) continue; // werden mehr Kisten in den Raum geschoben als insgesamt vorhanden sind?
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
              if (Tools.TickRefresh())
              {
                if (!mergeInfo(txtLine + "states " + (state1 * room2.stateList.Count + state2 + finishedStepCount).ToString("N0") + " / " + maxStepCount.ToString("N0") + " - (" + calcMoves.ToString("N0") + " moves)")) return false;
              }
              calcMoves += MergePortalVariants(iPortalOld, room2, state1, state2, (s1, s2) => s1 * state1Mul + s2, mapPortalIndex1, mapPortalIndex2, iPortalNew);
            }
          }
        }
        else
        {
          for (ulong state2 = 0; state2 < room2.stateList.Count; state2++)
          {
            for (ulong state1 = 0; state1 < room1.stateList.Count; state1++)
            {
              if (Tools.TickRefresh())
              {
                if (!mergeInfo(txtLine + "states " + (state2 * room1.stateList.Count + state1 + finishedStepCount).ToString("N0") + " / " + maxStepCount.ToString("N0") + " - (" + calcMoves.ToString("N0") + " moves)")) return false;
              }
              calcMoves += MergePortalVariants(iPortalOld, room1, state2, state1, (s1, s2) => s2 * state1Mul + s1, mapPortalIndex2, mapPortalIndex1, iPortalNew);
            }
          }
        }
        finishedStepCount += room1.stateList.Count * room2.stateList.Count;
        #endregion
      }

      return true;
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

    #region # // Step5_OptimizeStates() // entfernt unbenutzte Kisten-Zustände im neuen Raum
    /// <summary>
    /// entfernt unbenutzte Kisten-Zustände im neuen Raum
    /// </summary>
    public void Step5_OptimizeStates()
    {
      OptimizeTools.RemoveUnusedStates(newRoom);

      Debug.Assert(newRoom.startVariantCount > 0 || (newRoom.startVariantCount + srcRoom1.startVariantCount + srcRoom2.startVariantCount == 0));
    }
    #endregion

    #region # // Step6_UpdateRooms() // aktualisiert die Räume und deren Indizierungen
    /// <summary>
    /// aktualisiert die Räume und deren Indizierungen
    /// </summary>
    public void Step6_UpdateRooms()
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

    #region # // MergeStartVariants(...) // verschmilzt alle Varianten, welche beim Start beginnen
    /// <summary>
    /// verschmilzt alle Varianten, welche beim Start beginnen
    /// </summary>
    /// <param name="room1">erster Raum, wo gestartet wird</param>
    /// <param name="room2">zweiter Raum, welcher verschmolzen wird</param>
    /// <param name="stateCalc">Funktion zum Berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="roomNew">neuer Raum, wohin die neuen Startvarianten gespeichert werden sollen</param>
    /// <param name="mergeInfo">Status-Meldung</param>
    static bool MergeStartVariants(Room room1, Room room2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, Room roomNew, Func<string, bool> mergeInfo)
    {
      ulong startState = stateCalc(room1.startState, room2.startState);
      var dict = new Dictionary<ulong, ulong>();
      var tasks = new List<MergeTask>();
      var moveTasks = new List<int>();
      var pushTasks = new List<int>();
      var endTasks = new List<int>();

      #region # // --- erste Aufgaben sammeln ---
      for (ulong variant1 = 0; variant1 < room1.startVariantCount; variant1++)
      {
        if (Tools.TickRefresh() && !mergeInfo("Variants " + variant1.ToString("N0") + " / " + room1.startVariantCount.ToString("N0"))) return false;
        var variantData1 = room1.variantList.GetData(variant1);

        ulong nextState1 = room1.startState;
        ulong nextState2 = room2.startState;
        var oPortalBoxes = ResolveBoxes(new uint[0], ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
        if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

        var newTask = new MergeTask
        (
          nextState1,              // Kistenzustand des ersten Raumes
          nextState2,              // Kistenzustand des zweiten Raumes
          oPortalBoxes.ToArray(),  // rausgeschobene Kisten
          variantData1.moves,      // Anzahl der Laufschritte insgesamt
          variantData1.pushes,     // Anzahl der Kistenverschiebungen insgesamt
          variantData1.path,       // zurückgelegter Pfad
          true,                    // Main-Room1 setzen
          variantData1             // die zu verarbeitende Variante
        );

        ulong crc = newTask.GetCrc();
        ulong bestMoves;
        if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
        if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

        dict[crc] = newTask.moves;
        tasks.Add(newTask);
      }
      #endregion

      #region # // --- Aufgaben abarbeiten und alle Varianten ermitteln, welche zwischen den beiden Räumen möglich sind ---
      ulong calcMoves = 0;
      for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
      {
        if (Tools.TickRefresh() && !mergeInfo("Tasks remain: " + (tasks.Count - taskIndex).ToString("N0") + (endTasks.Count > 0 ? " - ends found: " + endTasks.Count.ToString("N0") : "") + " (" + calcMoves.ToString("N0") + " moves)")) return false;
        var task = tasks[taskIndex];
        ulong checkCrc = task.GetCrc();
        ulong moves = dict[checkCrc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?
        calcMoves += task.moves;

        if (task.variantData.pushes > 0) // Variante mit Kistenverschiebungen erkannt?
        {
          if (task.variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung erreicht?
          {
            if (task.state1 == 0 && task.state2 == 0) endTasks.Add(taskIndex);
          }
          else
          {
            uint oPortalIndex = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
            if (oPortalIndex == uint.MaxValue) // Weg in den benachbarten Raum erkannt?
            {
              if (task.main1)
              {
                var iPortal2 = room1.outgoingPortals[task.variantData.oPortalIndexPlayer];
                foreach (ulong variant2 in iPortal2.variantStateDict.GetVariantSpan(task.state2).AsEnumerable())
                {
                  var variantData2 = room2.variantList.GetData(variant2);

                  ulong nextState1 = task.state1;
                  ulong nextState2 = task.state2;
                  var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState2, ref nextState1, variantData2, mapPortalIndex2, room2, room1);
                  if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                  var newTask = new MergeTask
                  (
                    nextState1,                         // Kistenzustand des ersten Raumes
                    nextState2,                         // Kistenzustand des zweiten Raumes
                    oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                    task.moves + variantData2.moves,    // Anzahl der Laufschritte insgesamt
                    task.pushes + variantData2.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                    task.path + variantData2.path,      // zurückgelegter Pfad
                    false,                              // Main-Room1 setzen
                    variantData2                        // die zu verarbeitende Variante
                  );

                  ulong crc = newTask.GetCrc();
                  ulong bestMoves;
                  if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                  if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                  dict[crc] = newTask.moves;
                  tasks.Add(newTask);
                }
              }
              else
              {
                var iPortal1 = room2.outgoingPortals[task.variantData.oPortalIndexPlayer];
                foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(task.state1).AsEnumerable())
                {
                  var variantData1 = room1.variantList.GetData(variant1);

                  ulong nextState1 = task.state1;
                  ulong nextState2 = task.state2;
                  var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
                  if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                  var newTask = new MergeTask
                  (
                    nextState1,                         // Kistenzustand des ersten Raumes
                    nextState2,                         // Kistenzustand des zweiten Raumes
                    oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                    task.moves + variantData1.moves,    // Anzahl der Laufschritte insgesamt
                    task.pushes + variantData1.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                    task.path + variantData1.path,      // zurückgelegter Pfad
                    true,                               // Main-Room1 setzen
                    variantData1                        // die zu verarbeitende Variante
                  );

                  ulong crc = newTask.GetCrc();
                  ulong bestMoves;
                  if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                  if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                  dict[crc] = newTask.moves;
                  tasks.Add(newTask);
                }
              }
            }
            else
            {
              pushTasks.Add(taskIndex); // Kisten-Variante merken
            }
          }
        }
        else // Variante nur mit Laufweg
        {
          uint oPortalIndex = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
          if (oPortalIndex == uint.MaxValue) // Laufweg in den benachbarten Raum erkannt?
          {
            if (task.main1)
            {
              var iPortal2 = room1.outgoingPortals[task.variantData.oPortalIndexPlayer];
              foreach (ulong variant2 in iPortal2.variantStateDict.GetVariantSpan(task.state2).AsEnumerable())
              {
                var variantData2 = room2.variantList.GetData(variant2);

                ulong nextState1 = task.state1;
                ulong nextState2 = task.state2;
                var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState2, ref nextState1, variantData2, mapPortalIndex2, room2, room1);
                if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                var newTask = new MergeTask
                (
                  nextState1,                         // Kistenzustand des ersten Raumes
                  nextState2,                         // Kistenzustand des zweiten Raumes
                  oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                  task.moves + variantData2.moves,    // Anzahl der Laufschritte insgesamt
                  task.pushes + variantData2.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                  task.path + variantData2.path,      // zurückgelegter Pfad
                  false,                              // Main-Room1 setzen
                  variantData2                        // die zu verarbeitende Variante
                );

                ulong crc = newTask.GetCrc();
                ulong bestMoves;
                if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                dict[crc] = newTask.moves;
                tasks.Add(newTask);
              }
            }
            else
            {
              var iPortal1 = room2.outgoingPortals[task.variantData.oPortalIndexPlayer];
              foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(task.state1).AsEnumerable())
              {
                var variantData1 = room1.variantList.GetData(variant1);

                ulong nextState1 = task.state1;
                ulong nextState2 = task.state2;
                var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
                if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                var newTask = new MergeTask
                (
                  nextState1,                         // Kistenzustand des ersten Raumes
                  nextState2,                         // Kistenzustand des zweiten Raumes
                  oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                  task.moves + variantData1.moves,    // Anzahl der Laufschritte insgesamt
                  task.pushes + variantData1.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                  task.path + variantData1.path,      // zurückgelegter Pfad
                  true,                               // Main-Room1 setzen
                  variantData1                        // die zu verarbeitende Variante
                );

                ulong crc = newTask.GetCrc();
                ulong bestMoves;
                if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                dict[crc] = newTask.moves;
                tasks.Add(newTask);
              }
            }
          }
          else
          {
            if (task.pushes > 0)
            {
              pushTasks.Add(taskIndex); // als Kisten-Variante merken
            }
            else
            {
              moveTasks.Add(taskIndex); // reine Laufvariante merken
            }
          }
        }
      }
      #endregion

      #region # // --- reine Laufwege abarbeiten ---
      foreach (var taskIndex in moveTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        uint oPortalIndexPlayer = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
        Debug.Assert(oPortalIndexPlayer < roomNew.outgoingPortals.Length);
        Debug.Assert(stateCalc(task.state1, task.state2) == startState);
        Debug.Assert(task.pushes == 0);
        Debug.Assert(task.oPortalBoxes.Length == 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        var newVariant = roomNew.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          oPortalIndexPlayer,   // Portal, worüber der Spieler den Raum verlässt
          startState,           // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        Debug.Assert(roomNew.startVariantCount == newVariant);
        roomNew.startVariantCount++;
        Debug.Assert(roomNew.startVariantCount == roomNew.variantList.Count);
      }
      #endregion

      #region # // --- Varianten mit Kistenverschiebungen abarbeiten ---
      foreach (var taskIndex in pushTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        uint oPortalIndexPlayer = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
        ulong endState = stateCalc(task.state1, task.state2);
        Debug.Assert(oPortalIndexPlayer < roomNew.outgoingPortals.Length);
        Debug.Assert(task.pushes > 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        var newVariant = roomNew.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          oPortalIndexPlayer,   // Portal, worüber der Spieler den Raum verlässt
          endState,             // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        Debug.Assert(roomNew.startVariantCount == newVariant);
        roomNew.startVariantCount++;
        Debug.Assert(roomNew.startVariantCount == roomNew.variantList.Count);
      }
      #endregion

      #region # // --- End-Varianten abarbeiten ---
      foreach (var taskIndex in endTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        Debug.Assert(task.variantData.oPortalIndexPlayer == uint.MaxValue);
        ulong endState = stateCalc(task.state1, task.state2);
        Debug.Assert(endState == 0);
        Debug.Assert(task.pushes > 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        var newVariant = roomNew.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          uint.MaxValue,        // Portal, worüber der Spieler den Raum verlässt
          endState,             // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        Debug.Assert(roomNew.startVariantCount == newVariant);
        roomNew.startVariantCount++;
        Debug.Assert(roomNew.startVariantCount == roomNew.variantList.Count);
      }
      #endregion

      return true;
    }
    #endregion

    #region # // MergePortalVariants(...) // verschmilzt alle Varianten eines Portales
    /// <summary>
    /// verschmilzt alle Varianten eines Portales
    /// </summary>
    /// <param name="inPortal1">eingehendes altes Portal vom 1. Raum</param>
    /// <param name="room2">2. Raum</param>
    /// <param name="state1">Kistenzustand vom 1. Raum</param>
    /// <param name="state2">Kistenzustand vom 2. Raum</param>
    /// <param name="stateCalc">Funktion zum Berechnen der kombinierten Zustandsnummer</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="mapPortalIndex2">Mapping der Portalnummern vom 2. Raum</param>
    /// <param name="iPortalNew">neues Portal, wohin die neu erzeugten Varianten gespeichert werden sollen</param>
    static ulong MergePortalVariants(RoomPortal inPortal1, Room room2, ulong state1, ulong state2, Func<ulong, ulong, ulong> stateCalc, uint[] mapPortalIndex1, uint[] mapPortalIndex2, RoomPortal iPortalNew)
    {
      var room1 = inPortal1.toRoom;
      ulong startState = stateCalc(state1, state2);
      if (iPortalNew.toRoom.stateList.Get(startState).Length > iPortalNew.toRoom.maxBoxes) return 0; // zuviele Kisten sind im Raum
      var dict = new Dictionary<ulong, ulong>();
      var tasks = new List<MergeTask>();
      var moveTasks = new List<int>();
      var pushTasks = new List<int>();
      var endTasks = new List<int>();
      ulong calcMoves = 0;

      #region # // --- erste Aufgaben sammeln ---
      foreach (ulong variant1 in inPortal1.variantStateDict.GetVariantSpan(state1).AsEnumerable())
      {
        var variantData1 = room1.variantList.GetData(variant1);

        ulong nextState1 = state1;
        ulong nextState2 = state2;
        var oPortalBoxes = ResolveBoxes(new uint[0], ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
        if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

        var newTask = new MergeTask
        (
          nextState1,              // Kistenzustand des ersten Raumes
          nextState2,              // Kistenzustand des zweiten Raumes
          oPortalBoxes.ToArray(),  // rausgeschobene Kisten
          variantData1.moves,      // Anzahl der Laufschritte insgesamt
          variantData1.pushes,     // Anzahl der Kistenverschiebungen insgesamt
          variantData1.path,       // zurückgelegter Pfad
          true,                    // Main-Room1 setzen
          variantData1             // die zu verarbeitende Variante
        );

        ulong crc = newTask.GetCrc();
        ulong bestMoves;
        if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
        if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

        dict[crc] = newTask.moves;
        tasks.Add(newTask);
      }
      #endregion

      #region # // --- Aufgaben abarbeiten und alle Varianten ermitteln, welche zwischen den beiden Räumen möglich sind ---
      for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
      {
        var task = tasks[taskIndex];
        ulong checkCrc = task.GetCrc();
        ulong moves = dict[checkCrc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        if (task.variantData.pushes > 0) // Variante mit Kistenverschiebungen erkannt?
        {
          if (task.variantData.oPortalIndexPlayer == uint.MaxValue) // End-Stellung erreicht?
          {
            if (task.state1 == 0 && task.state2 == 0) endTasks.Add(taskIndex);
          }
          else
          {
            uint oPortalIndex = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
            if (oPortalIndex == uint.MaxValue) // Weg in den benachbarten Raum erkannt?
            {
              if (task.main1)
              {
                var iPortal2 = room1.outgoingPortals[task.variantData.oPortalIndexPlayer];
                foreach (ulong variant2 in iPortal2.variantStateDict.GetVariantSpan(task.state2).AsEnumerable())
                {
                  var variantData2 = room2.variantList.GetData(variant2);

                  ulong nextState1 = task.state1;
                  ulong nextState2 = task.state2;
                  var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState2, ref nextState1, variantData2, mapPortalIndex2, room2, room1);
                  if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                  var newTask = new MergeTask
                  (
                    nextState1,                         // Kistenzustand des ersten Raumes
                    nextState2,                         // Kistenzustand des zweiten Raumes
                    oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                    task.moves + variantData2.moves,    // Anzahl der Laufschritte insgesamt
                    task.pushes + variantData2.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                    task.path + variantData2.path,      // zurückgelegter Pfad
                    false,                              // Main-Room1 setzen
                    variantData2                        // die zu verarbeitende Variante
                  );

                  ulong crc = newTask.GetCrc();
                  ulong bestMoves;
                  if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                  if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                  dict[crc] = newTask.moves;
                  tasks.Add(newTask);
                }
              }
              else
              {
                var iPortal1 = room2.outgoingPortals[task.variantData.oPortalIndexPlayer];
                foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(task.state1).AsEnumerable())
                {
                  var variantData1 = room1.variantList.GetData(variant1);

                  ulong nextState1 = task.state1;
                  ulong nextState2 = task.state2;
                  var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
                  if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                  var newTask = new MergeTask
                  (
                    nextState1,                         // Kistenzustand des ersten Raumes
                    nextState2,                         // Kistenzustand des zweiten Raumes
                    oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                    task.moves + variantData1.moves,    // Anzahl der Laufschritte insgesamt
                    task.pushes + variantData1.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                    task.path + variantData1.path,      // zurückgelegter Pfad
                    true,                               // Main-Room1 setzen
                    variantData1                        // die zu verarbeitende Variante
                  );

                  ulong crc = newTask.GetCrc();
                  ulong bestMoves;
                  if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                  if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                  dict[crc] = newTask.moves;
                  tasks.Add(newTask);
                }
              }
            }
            else
            {
              pushTasks.Add(taskIndex); // Kisten-Variante merken
            }
          }
        }
        else // Variante nur mit Laufweg
        {
          uint oPortalIndex = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
          if (oPortalIndex == uint.MaxValue) // Laufweg in den benachbarten Raum erkannt?
          {
            if (task.main1)
            {
              var iPortal2 = room1.outgoingPortals[task.variantData.oPortalIndexPlayer];
              foreach (ulong variant2 in iPortal2.variantStateDict.GetVariantSpan(task.state2).AsEnumerable())
              {
                var variantData2 = room2.variantList.GetData(variant2);

                ulong nextState1 = task.state1;
                ulong nextState2 = task.state2;
                var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState2, ref nextState1, variantData2, mapPortalIndex2, room2, room1);
                if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                var newTask = new MergeTask
                (
                  nextState1,                         // Kistenzustand des ersten Raumes
                  nextState2,                         // Kistenzustand des zweiten Raumes
                  oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                  task.moves + variantData2.moves,    // Anzahl der Laufschritte insgesamt
                  task.pushes + variantData2.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                  task.path + variantData2.path,      // zurückgelegter Pfad
                  false,                              // Main-Room1 setzen
                  variantData2                        // die zu verarbeitende Variante
                );

                ulong crc = newTask.GetCrc();
                ulong bestMoves;
                if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                dict[crc] = newTask.moves;
                tasks.Add(newTask);
              }
            }
            else
            {
              var iPortal1 = room2.outgoingPortals[task.variantData.oPortalIndexPlayer];
              foreach (ulong variant1 in iPortal1.variantStateDict.GetVariantSpan(task.state1).AsEnumerable())
              {
                var variantData1 = room1.variantList.GetData(variant1);

                ulong nextState1 = task.state1;
                ulong nextState2 = task.state2;
                var oPortalBoxes = ResolveBoxes(task.oPortalBoxes, ref nextState1, ref nextState2, variantData1, mapPortalIndex1, room1, room2);
                if (oPortalBoxes == null) continue; // ungültige Variante erkannt?

                var newTask = new MergeTask
                (
                  nextState1,                         // Kistenzustand des ersten Raumes
                  nextState2,                         // Kistenzustand des zweiten Raumes
                  oPortalBoxes.ToArray(),             // rausgeschobene Kisten
                  task.moves + variantData1.moves,    // Anzahl der Laufschritte insgesamt
                  task.pushes + variantData1.pushes,  // Anzahl der Kistenverschiebungen insgesamt
                  task.path + variantData1.path,      // zurückgelegter Pfad
                  true,                               // Main-Room1 setzen
                  variantData1                        // die zu verarbeitende Variante
                );

                ulong crc = newTask.GetCrc();
                ulong bestMoves;
                if (!dict.TryGetValue(crc, out bestMoves)) bestMoves = ulong.MaxValue;
                if (bestMoves <= newTask.moves) continue; // war eine bessere Variante bereits bekannt?

                dict[crc] = newTask.moves;
                tasks.Add(newTask);
              }
            }
          }
          else
          {
            if (task.pushes > 0)
            {
              pushTasks.Add(taskIndex); // als Kisten-Variante merken
            }
            else
            {
              moveTasks.Add(taskIndex); // reine Laufvariante merken
            }
          }
        }
      }
      #endregion

      #region # // --- reine Laufwege abarbeiten ---
      foreach (var taskIndex in moveTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        uint oPortalIndexPlayer = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
        Debug.Assert(oPortalIndexPlayer < iPortalNew.toRoom.outgoingPortals.Length);
        Debug.Assert(stateCalc(task.state1, task.state2) == startState);
        Debug.Assert(task.pushes == 0);
        Debug.Assert(task.oPortalBoxes.Length == 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        if (task.main1 && task.variantData.oPortalIndexPlayer == inPortal1.iPortalIndex) continue; // unnötige Laufwege ignorieren
        var newVariant = iPortalNew.toRoom.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          oPortalIndexPlayer,   // Portal, worüber der Spieler den Raum verlässt
          startState,           // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        iPortalNew.variantStateDict.Add(startState, newVariant);
        calcMoves += task.moves;
      }
      #endregion

      #region # // --- Varianten mit Kistenverschiebungen abarbeiten ---
      foreach (var taskIndex in pushTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        uint oPortalIndexPlayer = task.main1 ? mapPortalIndex1[task.variantData.oPortalIndexPlayer] : mapPortalIndex2[task.variantData.oPortalIndexPlayer];
        ulong endState = stateCalc(task.state1, task.state2);
        Debug.Assert(oPortalIndexPlayer < iPortalNew.toRoom.outgoingPortals.Length);
        Debug.Assert(task.pushes > 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        if (task.main1 && task.variantData.oPortalIndexPlayer == inPortal1.iPortalIndex && startState == endState) continue; // unnötige Laufwege (mit überflüssigen Kistenverschiebungen) ignorieren
        var newVariant = iPortalNew.toRoom.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          oPortalIndexPlayer,   // Portal, worüber der Spieler den Raum verlässt
          endState,             // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        iPortalNew.variantStateDict.Add(startState, newVariant);
        calcMoves += task.moves;
      }
      #endregion

      #region # // --- End-Varianten abarbeiten ---
      foreach (var taskIndex in endTasks)
      {
        var task = tasks[taskIndex];
        ulong crc = task.GetCrc();
        ulong moves = dict[crc];
        Debug.Assert(moves <= task.moves);
        if (moves != task.moves) continue; // nicht die beste Variante dieser Aufgabe erkannt?

        Debug.Assert(task.variantData.oPortalIndexPlayer == uint.MaxValue);
        ulong endState = stateCalc(task.state1, task.state2);
        Debug.Assert(endState == 0);
        Debug.Assert(task.pushes > 0);
        Debug.Assert((uint)task.path.Length == task.moves);

        var newVariant = iPortalNew.toRoom.variantList.Add
        (
          startState,           // vorheriger Kistenzustand
          task.moves,           // Anzahl der Laufschritte
          task.pushes,          // Anzahl der Kistenverschiebungen
          task.oPortalBoxes,    // rausgeschobene Kisten
          uint.MaxValue,        // Portal, worüber der Spieler den Raum verlässt
          endState,             // nachfolgender Kistenzustand
          task.path             // zurückgelegter Pfad
        );
        iPortalNew.variantStateDict.Add(startState, newVariant);
        calcMoves += task.moves;
      }
      #endregion

      return calcMoves;
    }
    #endregion

    #region # // ResolveBoxes(...) // Methode zum Auflösen der Kisten und Prüfen der Variante
    /// <summary>
    /// Methode zum Auflösen der Kisten und Prüfen der Variante
    /// </summary>
    /// <param name="oldPortalBoxes">bisherige Kisten, welche bereits rausgeschoben wurden</param>
    /// <param name="state1">Zustand des ersten Raumes</param>
    /// <param name="state2">Zustand des zweiten Raumes</param>
    /// <param name="variantData1">Variante, welche verwendet wird</param>
    /// <param name="mapPortalIndex1">Mapping der Portalnummern vom 1. Raum</param>
    /// <param name="room1">Referenz auf den ersten Raum</param>
    /// <param name="room2">Referenz auf den zweiten Raum</param>
    /// <returns>neue Liste mit Kisten, oder null, wenn die Variante ungültig ist</returns>
    static List<uint> ResolveBoxes(uint[] oldPortalBoxes, ref ulong state1, ref ulong state2, VariantData variantData1, uint[] mapPortalIndex1, Room room1, Room room2)
    {
      if (variantData1.pushes == 0 && oldPortalBoxes.Length == 0) return oldPortalBoxes.ToList(); // keine Kistenverschiebungen vorhanden?

      var oPortalBoxes = new List<uint>(oldPortalBoxes.Length + variantData1.oPortalIndexBoxes.Length);
      oPortalBoxes.AddRange(oldPortalBoxes);

      Debug.Assert(state1 == variantData1.oldState);

      foreach (uint oPortalBox1 in variantData1.oPortalIndexBoxes)
      {
        uint oPortalBox = mapPortalIndex1[oPortalBox1];
        if (oPortalBox == uint.MaxValue) // Kiste wurde in den benachbarten Raum geschoben?
        {
          var iPortal2 = room1.outgoingPortals[oPortalBox1];
          ulong newState2 = iPortal2.stateBoxSwap.Get(state2);
          if (newState2 == state2) return null; // ungültig: Kiste kann der benachbarte Raum nicht aufnehmen
          state2 = newState2;
        }
        else // Kiste wurde aus dem Raum heraus geschoben 
        {
          if (oPortalBoxes.Contains(oPortalBox)) return null; // ungültig: Kiste wurde doppelt durch das gleiche Portal rausgeschoben
          oPortalBoxes.Add(oPortalBox);
        }
      }

      if (variantData1.oPortalIndexPlayer < uint.MaxValue
       && mapPortalIndex1[variantData1.oPortalIndexPlayer] < uint.MaxValue
       && room1.outgoingPortals[variantData1.oPortalIndexPlayer].blockedBox
       && oPortalBoxes.Contains(mapPortalIndex1[variantData1.oPortalIndexPlayer]))
      {
        return null;
      }

      state1 = variantData1.newState;

      oPortalBoxes.Sort((x, y) => x.CompareTo(y));
      return oPortalBoxes;
    }
    #endregion
  }
}
