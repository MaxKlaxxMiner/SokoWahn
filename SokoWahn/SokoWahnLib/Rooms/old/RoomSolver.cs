
//namespace SokoWahnLib.Rooms
//{
//  /// <summary>
//  /// Klasse zum effizienten lösen von Spielfeldern
//  /// </summary>
//  public class RoomSolver : IDisposable
//  {
//    /// <summary>
//    /// merkt sich alle Räume
//    /// </summary>
//    public Room[] rooms;

//    #region # // --- Konstruktor ---
//    /// <summary>
//    /// Konstruktor
//    /// </summary>
//    /// <param name="field">Spielfeld, welches gelöst werden soll</param>
//    public RoomSolver(ISokoField field)
//    {
//      #region # // --- Räume erstellen ---
//      // --- begehbare Felder ermitteln und Basis-Räume erstellen ---
//      var walkFields = field.GetWalkPosis();
//      rooms = walkFields.OrderBy(pos => pos).Select(pos =>
//      {
//        int portals = (walkFields.Contains(pos - 1) ? 1 : 0) + // eingehendes Portal von der linken Seite
//                      (walkFields.Contains(pos + 1) ? 1 : 0) + // eingegendes Portal von der rechten Seite
//                      (walkFields.Contains(pos - field.Width) ? 1 : 0) + // eingehendes Portal von oben
//                      (walkFields.Contains(pos + field.Width) ? 1 : 0); // eingehendes Portal von unten
//        return new Room(field, pos, new RoomPortal[portals], new RoomPortal[portals]);
//      }).ToArray();
//      #endregion

//      #region # // --- Portale erstellen ---
//      // --- eingehende Portale in den Basis-Räumen erstellen und hinzufügen ---
//      foreach (var room in rooms)
//      {
//        int pos = room.fieldPosis.First();
//        var portals = room.incomingPortals;
//        int pIndex = 0;

//        // eingehendes Portal von der linken Seite
//        if (walkFields.Contains(pos - 1))
//        {
//          portals[pIndex++] = new RoomPortal(pos - 1, pos, rooms.First(r => r.fieldPosis.First() == pos - 1), room); // eingehend
//        }

//        // eingehendes Portal von der rechten Seite
//        if (walkFields.Contains(pos + 1))
//        {
//          portals[pIndex++] = new RoomPortal(pos + 1, pos, rooms.First(r => r.fieldPosis.First() == pos + 1), room);
//        }

//        // eingehendes Portal von oben
//        if (walkFields.Contains(pos - field.Width))
//        {
//          portals[pIndex++] = new RoomPortal(pos - field.Width, pos, rooms.First(r => r.fieldPosis.First() == pos - field.Width), room);
//        }

//        // eingehendes Portal von unten
//        if (walkFields.Contains(pos + field.Width))
//        {
//          portals[pIndex++] = new RoomPortal(pos + field.Width, pos, rooms.First(r => r.fieldPosis.First() == pos + field.Width), room);
//        }

//        Debug.Assert(pIndex == portals.Length);
//      }

//      // --- ausgehende Portale in den Basis-Räumen referenzieren (bestehende verwenden) ---
//      foreach (var room in rooms)
//      {
//        var iPortals = room.incomingPortals;
//        var oPortals = room.outgoingPortals;
//        Debug.Assert(iPortals.Length == oPortals.Length);
//        for (int pIndex = 0; pIndex < iPortals.Length; pIndex++)
//        {
//          oPortals[pIndex] = iPortals[pIndex].roomFrom.incomingPortals.First(p => p.posFrom == iPortals[pIndex].posTo);
//          iPortals[pIndex].oppositePortal = oPortals[pIndex];
//          Debug.Assert(iPortals[pIndex].posFrom == oPortals[pIndex].posTo);
//          Debug.Assert(iPortals[pIndex].posTo == oPortals[pIndex].posFrom);
//          Debug.Assert(iPortals[pIndex].roomFrom == oPortals[pIndex].roomTo);
//          Debug.Assert(iPortals[pIndex].roomTo == oPortals[pIndex].roomFrom);
//        }
//      }
//      #endregion

//      #region # // --- Zustände erstellen ---
//      foreach (var room in rooms)
//      {
//        room.InitStates();
//      }
//      #endregion

//      #region # // --- Varianten erstellen ---
//      foreach (var room in rooms)
//      {
//        room.InitVariants();
//      }
//      #endregion
//    }
//    #endregion

//    #region # // --- Optimizer ---
//    /// <summary>
//    /// entfernt alle Kisten-Varianten eines Raumes
//    /// </summary>
//    /// <param name="room">Room, wo die Kistenvarianten entfernt werden sollen</param>
//    /// <param name="maxCount">maximale Anzahl der Elemente, welche optimiert werden dürfen</param>
//    /// <param name="output">Details der durchgeführten Optimierungen</param>
//    /// <returns>Anzahl der optimierten Elemente</returns>
//    static int OptimizeRemoveBoxes(Room room, int maxCount, List<KeyValuePair<string, int>> output)
//    {
//      int totalCount = 0;

//      // --- eingehende Portale filtern ---
//      foreach (var portal in room.incomingPortals)
//      {
//        if (portal.roomToBoxVariants.Count > 0)
//        {
//          output.Add(new KeyValuePair<string, int>("[Box-Net] remove box-variants from portal " + portal, portal.roomToBoxVariants.Count));
//          totalCount += portal.roomToBoxVariants.Count;
//          portal.roomToBoxVariants.Clear();
//          if (totalCount >= maxCount) return totalCount;
//        }

//        // --- Spieler-Varianten mit Kisten suchen und entfernen ---
//        for (int i = 0; i < portal.roomToPlayerVariants.Count; i++)
//        {
//          var variantStates = room.GetVariantStates(portal.roomToPlayerVariants[i]);
//          if (room.GetStateInfo(variantStates.Key).boxCount + room.GetStateInfo(variantStates.Value).boxCount > 0) // eine Spieler-Variante mit Kisten gefunden?
//          {
//            portal.roomToPlayerVariants.RemoveAt(i);
//            i--;
//            output.Add(new KeyValuePair<string, int>("[Box-Net] remove ply-variants from portal " + portal, 1));
//            totalCount++;
//            if (totalCount >= maxCount) return totalCount;
//          }
//        }
//      }

//      // --- ausgehende Portale filtern ---
//      foreach (var portal in room.outgoingPortals)
//      {
//        // --- ausgehende Kisten-Varianten alle direkt entfernen ---
//        if (portal.roomToBoxVariants.Count > 0)
//        {
//          output.Add(new KeyValuePair<string, int>("[Box-Net] remove box-variants from portal " + portal, portal.roomToBoxVariants.Count));
//          totalCount += portal.roomToBoxVariants.Count;
//          portal.roomToBoxVariants.Clear();
//          if (totalCount >= maxCount) return totalCount;
//        }

//        var roomTo = portal.roomTo;

//        // --- Spieler-Varianten mit Kisten suchen und entfernen ---
//        for (int i = 0; i < portal.roomToPlayerVariants.Count; i++)
//        {
//          var variantStates = roomTo.GetVariantStates(portal.roomToPlayerVariants[i]);
//          if (roomTo.GetStateInfo(variantStates.Key).boxCount + roomTo.GetStateInfo(variantStates.Value).boxCount > 0) // eine Spieler-Variante mit Kisten gefunden?
//          {
//            portal.roomToPlayerVariants.RemoveAt(i);
//            i--;
//            output.Add(new KeyValuePair<string, int>("[Box-Net] remove ply-variants from portal " + portal, 1));
//            totalCount++;
//            if (totalCount >= maxCount) return totalCount;
//          }
//        }
//      }

//      // --- Start-Varianten entfernen, welche ohne Kisten keine Bedeutung mehr haben ---
//      if (room.startBoxVariants.Count > 0 && !room.fieldPosis.All(pos => room.field.IsPlayer(pos)))
//      {
//        output.Add(new KeyValuePair<string, int>("[Box-Net] remove box-variants from room " + room.fieldPosis.First(), room.startBoxVariants.Count));
//        totalCount += room.startBoxVariants.Count;
//        room.startBoxVariants.Clear();
//        if (totalCount >= maxCount) return totalCount;
//      }
//      if (room.startPlayerVariants.Count > 0 && !room.fieldPosis.All(pos => room.field.IsPlayer(pos)))
//      {
//        output.Add(new KeyValuePair<string, int>("[Box-Net] remove ply-variants from room " + room.fieldPosis.First(), room.startPlayerVariants.Count));
//        totalCount += room.startPlayerVariants.Count;
//        room.startPlayerVariants.Clear();
//        if (totalCount >= maxCount) return totalCount;
//      }

//      return totalCount;
//    }

//    /// <summary>
//    /// optimiert das Spielfeld und gibt die optimierten Element als lesbare Liste zurück 
//    /// </summary>
//    /// <param name="maxCount">maximale Anzahl der Elemente, welche optimiert werden dürfen</param>
//    /// <param name="output">Details der durchgeführten Optimierungen</param>
//    /// <returns>Anzahl der optimierten Elemente</returns>
//    public int Optimize(int maxCount, List<KeyValuePair<string, int>> output)
//    {
//      return 0;

//      //int totalCount = 0;

//      //#region # // --- Kisten-Netzwerke erstellen ---
//      //var boxNetworks = new List<HashSet<Room>>();
//      //foreach (var room in rooms)
//      //{
//      //  if (boxNetworks.Any(network => network.Contains(room))) continue; // Raum schon in eins der Netzwerke vorhanden?
//      //  if (!room.HasBoxStates()) continue; // Raum kann nie Kisten enthalten?

//      //  var net = new HashSet<Room>();
//      //  boxNetworks.Add(net);
//      //  var checkRooms = new Stack<Room>();
//      //  checkRooms.Push(room);
//      //  while (checkRooms.Count > 0)
//      //  {
//      //    var checkRoom = checkRooms.Pop();
//      //    if (net.Contains(checkRoom)) continue; // Raum schon im Netzwerk vorhanden?
//      //    net.Add(checkRoom); // Raum in das Netzwerk aufnehmen

//      //    // --- ausgehende Portale prüfen ---
//      //    foreach (var portal in checkRoom.outgoingPortals)
//      //    {
//      //      if (portal.roomToBoxVariants.Count == 0) continue; // ausgehendes Portal hat keine Kisten-Varianten
//      //      Debug.Assert(portal.roomTo.HasBoxStates());
//      //      checkRooms.Push(portal.roomTo); // benachbarter Raum kann aufgenommen werden
//      //    }

//      //    // --- eingehende Portale prüfen ---
//      //    foreach (var portal in checkRoom.incomingPortals)
//      //    {
//      //      if (portal.roomToBoxVariants.Count == 0) continue; // eingehendes Portal hat keine Kisten-Varianten
//      //      if (!portal.roomFrom.HasBoxStates()) continue;
//      //      checkRooms.Push(portal.roomFrom); // benachbarter Raum kann aufgenommen werden
//      //    }
//      //  }
//      //}
//      //#endregion

//      //#region # // --- Kisten-Netzwerke prüfen ob diese eventuell kistenfrei sein könnten ---
//      //foreach (var net in boxNetworks)
//      //{
//      //  if (net.Any(r => r.HasStartBoxes())) continue;

//      //  // --- komplettes Kisten-Netzwerk ohne Kisten gefunden? ---
//      //  foreach (var room in net)
//      //  {
//      //    totalCount += OptimizeRemoveBoxes(room, maxCount - totalCount, output);
//      //    if (totalCount >= maxCount) return totalCount;
//      //  }
//      //}
//      //#endregion

//      //#region # // --- Kisten-Einbahnstraßen mit Sackgassen finden ---
//      //foreach (var room in rooms)
//      //{
//      //  if (room.fieldPosis.Any(pos => field.IsGoal(pos))) continue; // Zielfelder im Raum vorhanden: hier können Kisten ohne Rückkehr rein geschoben werden

//      //  // --- prüfen, ob es nur eingehende Kisten-Varianten gibt aber keine ausgehenden ---
//      //  if (room.incomingPortals.Any(x => x.roomToBoxVariants.Count > 0) && room.outgoingPortals.All(x => x.roomToBoxVariants.Count == 0))
//      //  {
//      //    totalCount += OptimizeRemoveBoxes(room, maxCount - totalCount, output);
//      //    if (totalCount >= maxCount) return totalCount;
//      //  }
//      //}
//      //#endregion

//      //#region # // --- Varianten aufräumen ---
//      //// --- nicht mehr benutzte Varianten entfernen ---
//      //foreach (var room in rooms)
//      //{
//      //  int vCount = 0;
//      //  vCount += room.startBoxVariants.Count;
//      //  vCount += room.startPlayerVariants.Count;
//      //  foreach (var portal in room.incomingPortals)
//      //  {
//      //    vCount += portal.roomToBoxVariants.Count;
//      //    vCount += portal.roomToPlayerVariants.Count;
//      //  }
//      //  Debug.Assert(vCount <= room.variantsDataUsed);
//      //  while (vCount < room.variantsDataUsed)
//      //  {
//      //    for (uint v = room.variantsDataUsed - 1; v < room.variantsDataUsed; v--)
//      //    {
//      //      if (room.startBoxVariants.Contains(v)) continue;
//      //      if (room.startPlayerVariants.Contains(v)) continue;
//      //      if (room.incomingPortals.Any(p => p.roomToBoxVariants.Contains(v) || p.roomToPlayerVariants.Contains(v))) continue;

//      //      // --- Variante entfernen und Bits zusammenschieben ---
//      //      room.variantsDataUsed--;
//      //      room.variantsData.MoveBits(v * room.variantsDataElement, (v + 1) * room.variantsDataElement, (room.variantsDataUsed - v) * room.variantsDataElement);

//      //      // --- nachfolgende Varianten-IDs angleichen ---
//      //      for (int i = 0; i < room.startBoxVariants.Count; i++)
//      //      {
//      //        if (room.startBoxVariants[i] > v) room.startBoxVariants[i]--;
//      //      }
//      //      for (int i = 0; i < room.startPlayerVariants.Count; i++)
//      //      {
//      //        if (room.startPlayerVariants[i] > v) room.startPlayerVariants[i]--;
//      //      }
//      //      foreach (var portal in room.incomingPortals)
//      //      {
//      //        var bb = portal.roomToBoxVariants;
//      //        var bp = portal.roomToPlayerVariants;
//      //        for (int i = 0; i < bb.Count; i++)
//      //        {
//      //          if (bb[i] > v) bb[i]--;
//      //        }
//      //        for (int i = 0; i < bp.Count; i++)
//      //        {
//      //          if (bp[i] > v) bp[i]--;
//      //        }
//      //      }

//      //      output.Add(new KeyValuePair<string, int>("[Variants] remove variant " + v + " from room " + room.fieldPosis[0], 1));
//      //      totalCount++;
//      //      if (totalCount >= maxCount) return totalCount;
//      //    }
//      //  }
//      //}
//      //#endregion

//      //#region # // --- Zustände aufräumen ---
//      //foreach (var room in rooms)
//      //{
//      //  var mapStates = new uint[room.statePlayerUsed + room.stateBoxUsed];
//      //  for (int i = 0; i < mapStates.Length; i++) mapStates[i] = uint.MaxValue;
//      //  for (uint v = 0; v < room.variantsDataUsed; v++)
//      //  {
//      //    var st = room.GetVariantStates(v);
//      //    mapStates[st.Key] = st.Key;
//      //    mapStates[st.Value] = st.Value;
//      //  }
//      //  bool find = false;
//      //  for (int i = 0; i < mapStates.Length; i++)
//      //  {
//      //    if (mapStates[i] == uint.MaxValue)
//      //    {
//      //      find = true;
//      //      break;
//      //    }
//      //  }
//      //  if (find) // können die Zustände optimiert werden?
//      //  {
//      //    uint count = 0;
//      //    uint countPlayers = 0;
//      //    for (int i = 0; i < mapStates.Length; i++)
//      //    {
//      //      if (mapStates[i] < uint.MaxValue)
//      //      {
//      //        mapStates[i] = count++;
//      //        if (i < room.statePlayerUsed) countPlayers++;
//      //      }
//      //    }

//      //    // --- statePlayer verschieben ---
//      //    uint oldOffset = room.statePlayerUsed;
//      //    room.statePlayerUsed = countPlayers;
//      //    for (uint i = 0, c = 0; c < room.statePlayerUsed; i++)
//      //    {
//      //      if (mapStates[i] == uint.MaxValue) continue;
//      //      room.statePlayerData.MoveBits(c * room.statePlayerElement, i * room.statePlayerElement, room.statePlayerElement);
//      //      c++;
//      //    }

//      //    // --- stateBox verschieben ---
//      //    room.stateBoxUsed = count - countPlayers;
//      //    for (uint i = 0, c = 0; c < room.stateBoxUsed; i++)
//      //    {
//      //      if (mapStates[i + oldOffset] == uint.MaxValue) continue;
//      //      room.stateBoxData.MoveBits(c * room.stateBoxElement, i * room.stateBoxElement, room.stateBoxElement);
//      //      c++;
//      //    }

//      //    // --- Zustände in den Varianten neu mappen ---
//      //    for (uint v = 0; v < room.variantsDataUsed; v++)
//      //    {
//      //      var st = room.GetVariantStates(v);
//      //      room.SetVariantStates(v, mapStates[st.Key], mapStates[st.Value]);
//      //    }

//      //    output.Add(new KeyValuePair<string, int>("[States] remove states from room " + room.fieldPosis.First(), mapStates.Length - (int)count));
//      //    totalCount += mapStates.Length - (int)count;
//      //    if (totalCount >= maxCount) return totalCount;
//      //  }
//      //}
//      //#endregion

//      //return totalCount;
//    }
//    #endregion

//    #region # // --- Merger ---
//    /// <summary>
//    /// verschmelzt zwei Räume zu einen größeren Raum
//    /// </summary>
//    /// <param name="roomIndex1">erster Raum</param>
//    /// <param name="roomIndex2">zweiter Raum</param>
//    public void Merge(int roomIndex1 = -1, int roomIndex2 = -1)
//    {
//      //if (roomIndex1 < 0 || roomIndex2 < 0) throw new NotImplementedException(); // todo: automatische Suche passender Räume durchführen
//      //Debug.Assert(roomIndex1 < rooms.Length);
//      //Debug.Assert(roomIndex2 < rooms.Length);
//      //Debug.Assert(roomIndex1 < roomIndex2);
//      //// --- nächsten 2. Raum suchen, wenn zwischen den beiden Räumen keine Verbindung besteht ---
//      //while (rooms[roomIndex1].outgoingPortals.All(portal => !rooms[roomIndex2].fieldPosis.Contains(portal.posTo))) roomIndex2++;

//      //#region # // --- Portale der beiden Räume suchen ---
//      //var room1 = rooms[roomIndex1];
//      //var room2 = rooms[roomIndex2];
//      //var incomingMergePortals1 = new HashSet<RoomPortal>(); // Portale des 1. Raumes, welche verschmolzen werden (und nachher verschwinden)
//      //var incomingMergePortals2 = new HashSet<RoomPortal>(); // Portale des 2. Raumes, welche verschmolzen werden (und nachher verschwinden)
//      //var incomingOuterPortals1 = new HashSet<RoomPortal>(); // Portale des 1. Raumes, welche erhalten bleiben
//      //var incomingOuterPortals2 = new HashSet<RoomPortal>(); // Portale des 2. Raumes, welche erhalten bleiben
//      //foreach (var portal in room1.incomingPortals)
//      //{
//      //  if (portal.roomFrom == room2) incomingMergePortals1.Add(portal); else incomingOuterPortals1.Add(portal);
//      //}
//      //foreach (var portal in room2.incomingPortals)
//      //{
//      //  if (portal.roomFrom == room1) incomingMergePortals2.Add(portal); else incomingOuterPortals2.Add(portal);
//      //}
//      //Debug.Assert(incomingMergePortals1.Count > 0);
//      //Debug.Assert(incomingMergePortals2.Count > 0);
//      //Debug.Assert(incomingMergePortals1.Count == incomingMergePortals2.Count);
//      //Debug.Assert(incomingMergePortals1.All(p => incomingMergePortals2.Contains(p.oppositePortal)));
//      //Debug.Assert(incomingMergePortals2.All(p => incomingMergePortals1.Contains(p.oppositePortal)));
//      //#endregion

//      //var incomingNewPortals = new RoomPortal[incomingOuterPortals1.Count + incomingOuterPortals2.Count];
//      //var outgoingNewPortals = new RoomPortal[incomingNewPortals.Length];
//      //uint maxPlayerStates = checked(room1.statePlayerUsed * room2.stateBoxUsed + room2.statePlayerUsed * room1.stateBoxUsed);
//      //uint maxBoxStates = checked(room1.stateBoxUsed * room2.stateBoxUsed);
//      //uint maxVariants = checked(room1.variantsDataUsed * room2.variantsDataUsed);
//      //var newRoom = new Room(field, room1.fieldPosis.Concat(room2.fieldPosis), maxPlayerStates, maxBoxStates, maxVariants, incomingNewPortals, outgoingNewPortals);

//      //// --- neue Zustände erstellen (Zustände Raum 1 * Zustände Raum 2) ---
//      //var statesDict = newRoom.AddMergeStates(room1, room2);

//      //// --- neue eingehende Portale erstellen und die alten ausgehenden Portale verlinken ---
//      //int portalCount = 0;
//      //foreach (var portal in incomingOuterPortals1.Concat(incomingOuterPortals2))
//      //{
//      //  incomingNewPortals[portalCount] = new RoomPortal(portal.posFrom, portal.posTo, portal.roomFrom, newRoom);
//      //  outgoingNewPortals[portalCount] = portal.oppositePortal;
//      //  incomingNewPortals[portalCount].oppositePortal = outgoingNewPortals[portalCount];
//      //  outgoingNewPortals[portalCount].oppositePortal = incomingNewPortals[portalCount];
//      //  portalCount++;
//      //}

//      //// --- Start-Varianten verschmelzen ---
//      //foreach (var vp1 in room1.startPlayerVariants)
//      //{
//      //  throw new NotImplementedException();
//      //}
//      //foreach (var vp2 in room2.startPlayerVariants)
//      //{
//      //  throw new NotImplementedException();
//      //}
//      //foreach (var vb1 in room1.startBoxVariants)
//      //{
//      //  throw new NotImplementedException();
//      //}
//      //foreach (var vb2 in room2.startBoxVariants)
//      //{
//      //  throw new NotImplementedException();
//      //}

//      //// --- Portal-Varianten verschmelzen ---
//      //var knownOutgoingPortals = new HashSet<RoomPortal>(outgoingNewPortals);
//      //var mapOutgoingPortals = new Dictionary<RoomPortal, int>();
//      //for (int i = 0; i < outgoingNewPortals.Length; i++) mapOutgoingPortals.Add(outgoingNewPortals[i], i);
//      //var mapIncomingPortals = new Dictionary<RoomPortal, RoomPortal>();
//      //foreach (var portal in incomingOuterPortals1) mapIncomingPortals.Add(portal, incomingNewPortals.First(p => p.posFrom == portal.posFrom && p.posTo == portal.posTo));
//      //foreach (var portal in incomingOuterPortals2) mapIncomingPortals.Add(portal, incomingNewPortals.First(p => p.posFrom == portal.posFrom && p.posTo == portal.posTo));

//      //// --- Eingänge in Raum 1 ---
//      //foreach (var portal1 in incomingOuterPortals1)
//      //{
//      //  foreach (uint ppv1 in portal1.roomToPlayerVariants)
//      //  {
//      //    var v1 = room1.GetVariantInfo(ppv1);
//      //    if (incomingMergePortals2.Contains(v1.outgoingPortal)) // R1 Variante führt durch ein Portal in den benachbarten Raum?
//      //    {
//      //      foreach (uint ppv2 in v1.outgoingPortal.roomToPlayerVariants)
//      //      {
//      //        var v2 = room2.GetVariantInfo(ppv2);
//      //        if (knownOutgoingPortals.Contains(v2.outgoingPortal)) // R2 Variante führt aus dem Raum heraus?
//      //        {
//      //          uint incomingState = statesDict[(ulong)v1.incomingState << 32 | v2.incomingState];
//      //          int outgoingPortal = mapOutgoingPortals[v2.outgoingPortal];
//      //          uint outgoingState = statesDict[(ulong)v1.outgoingState << 32 | v2.outgoingState];
//      //          uint moves = v1.moves + v2.moves;
//      //          uint pushes = v1.pushes + v2.pushes;
//      //          mapIncomingPortals[portal1].roomToPlayerVariants.Add(newRoom.variantsDataUsed);
//      //          newRoom.AddVariant(incomingState, outgoingPortal, outgoingState, moves, pushes);
//      //        }
//      //        else
//      //        {
//      //          throw new NotImplementedException();
//      //        }
//      //      }
//      //      foreach (uint pbv2 in v1.outgoingPortal.roomToBoxVariants)
//      //      {
//      //        throw new NotImplementedException();
//      //      }
//      //    }
//      //    else
//      //    {
//      //      throw new NotImplementedException();
//      //    }
//      //  }
//      //  foreach (uint pbv1 in portal1.roomToBoxVariants)
//      //  {
//      //    throw new NotImplementedException();
//      //  }
//      //}

//      //// --- Eingänge in Raum 2 ---
//      //foreach (var portal2 in incomingOuterPortals2)
//      //{
//      //  foreach (uint ppv2 in portal2.roomToPlayerVariants)
//      //  {
//      //    var v2 = room2.GetVariantInfo(ppv2);
//      //    if (knownOutgoingPortals.Contains(v2.outgoingPortal)) // R2 Variante führt aus dem Raum heraus?
//      //    {
//      //      for (uint st1 = 0; st1 < room1.stateBoxUsed; st1++)
//      //      {
//      //        uint incomingState = statesDict[(ulong)st1 << 32 | v2.incomingState];
//      //        int outgoingPortal = mapOutgoingPortals[v2.outgoingPortal];
//      //        uint outgoingState = statesDict[(ulong)st1 << 32 | v2.outgoingState];
//      //        uint moves = v2.moves;
//      //        uint pushes = v2.pushes;
//      //        mapIncomingPortals[portal2].roomToPlayerVariants.Add(newRoom.variantsDataUsed);
//      //        newRoom.AddVariant(incomingState, outgoingPortal, outgoingState, moves, pushes);
//      //      }
//      //    }
//      //    else if (incomingMergePortals1.Contains(v2.outgoingPortal)) // R2 Variante führt durch ein Portal in den benachbarten Raum?
//      //    {
//      //      foreach (uint ppv1 in v2.outgoingPortal.roomToPlayerVariants)
//      //      {
//      //        var v1 = room1.GetVariantInfo(ppv1);
//      //        if (knownOutgoingPortals.Contains(v1.outgoingPortal)) // R1 Variante führt aus dem Raum heraus?
//      //        {
//      //          uint incomingState = statesDict[(ulong)v1.incomingState << 32 | v2.incomingState];
//      //          int outgoingPortal = mapOutgoingPortals[v1.outgoingPortal];
//      //          uint outgoingState = statesDict[(ulong)v1.outgoingState << 32 | v2.outgoingState];
//      //          uint moves = v1.moves + v2.moves;
//      //          uint pushes = v1.pushes + v2.pushes;
//      //          mapIncomingPortals[portal2].roomToPlayerVariants.Add(newRoom.variantsDataUsed);
//      //          newRoom.AddVariant(incomingState, outgoingPortal, outgoingState, moves, pushes);
//      //        }
//      //        else
//      //        {
//      //          throw new NotImplementedException();
//      //        }
//      //      }
//      //      foreach (uint pbv1 in v2.outgoingPortal.roomToBoxVariants)
//      //      {
//      //        throw new NotImplementedException();
//      //      }
//      //    }
//      //    else
//      //    {
//      //      throw new NotImplementedException();
//      //    }
//      //  }
//      //  foreach (uint pbv2 in portal2.roomToBoxVariants)
//      //  {
//      //    throw new NotImplementedException();
//      //  }
//      //}

//      //// --- Räume-Index aktualisieren ---
//      //rooms = rooms.Where(r => r != room1 && r != room2).Concat(Enumerable.Repeat(newRoom, 1)).OrderBy(r => r.fieldPosis.First()).ToArray();

//      //// --- Räume der eingehende Portale von den benachbarten Räume anpassen ---
//      //foreach (var room in rooms)
//      //{
//      //  if (room == newRoom) continue; // eigener Raum muss nicht angefasst werden
//      //  for (int p = 0; p < room.incomingPortals.Length; p++)
//      //  {
//      //    var portal = room.incomingPortals[p];
//      //    if (newRoom.fieldPosis.Contains(portal.posFrom))
//      //    {
//      //      var newPortal = new RoomPortal(portal.posFrom, portal.posTo, newRoom, room);           // neues Portal erstellen
//      //      newPortal.roomToPlayerVariants.AddRange(room.incomingPortals[p].roomToPlayerVariants); // alle Spieler-Varianten übertragen
//      //      newPortal.roomToBoxVariants.AddRange(room.incomingPortals[p].roomToBoxVariants);       // alle Kisten-Varianten übertragen
//      //      room.incomingPortals[p] = newPortal;                                                   // neues eingehendes Portal setzen
//      //      newPortal.oppositePortal = newRoom.incomingPortals.First(x => x.posFrom == newPortal.posTo && x.posTo == newPortal.posFrom); // Rückverweis setzen
//      //      newPortal.oppositePortal.oppositePortal = room.incomingPortals[p];                     // doppelten Rückverweis setzen
//      //    }
//      //  }
//      //}

//      //// --- alle ausgehenden Portal neu verlinken ---
//      //foreach (var r in rooms)
//      //{
//      //  for (int p = 0; p < r.outgoingPortals.Length; p++)
//      //  {
//      //    var oPortal = r.outgoingPortals[p];
//      //    foreach (var r2 in rooms)
//      //    {
//      //      foreach (var p2 in r2.incomingPortals)
//      //      {
//      //        if (p2.posTo == oPortal.posTo && p2.posFrom == oPortal.posFrom)
//      //        {
//      //          r.outgoingPortals[p] = p2;
//      //        }
//      //      }
//      //    }
//      //  }
//      //}

//      //// --- Prüfung der Portale durchführen ---
//      //FullPortalCheck();
//    }
//    #endregion

//    #region # // --- Checks ---
//    /// <summary>
//    /// prüft alle Portale, ob diese untereinander richtig verknüpft sind
//    /// </summary>
//    public void FullPortalCheck()
//    {
//      //// --- Räume auf Doppler prüfen und Basis-Check der Portale ---
//      //var roomsHash = new HashSet<Room>();
//      //var posToRoom = new Dictionary<int, Room>();
//      //foreach (var room in rooms)
//      //{
//      //  if (roomsHash.Contains(room)) throw new Exception("Raum doppelt vorhanden: " + room);

//      //  if (room.incomingPortals.Length != room.outgoingPortals.Length)
//      //  {
//      //    throw new Exception("Anzahl der ein- und ausgehenden Portale stimmen nicht: " + room.incomingPortals.Length + " != " + room.outgoingPortals.Length + " bei: " + room);
//      //  }

//      //  foreach (var pos in room.fieldPosis)
//      //  {
//      //    if (posToRoom.ContainsKey(pos)) throw new Exception("Feld " + pos + " wird in zwei Räumen gleichzeitig verwendet: " + posToRoom[pos] + " und " + room);
//      //    posToRoom.Add(pos, room);
//      //  }

//      //  for (int p = 0; p < room.incomingPortals.Length; p++)
//      //  {
//      //    if (room.incomingPortals[p] == null) throw new Exception("eingehendes Portal = null [" + p + "] bei: " + room);
//      //    if (room.outgoingPortals[p] == null) throw new Exception("ausgehendes Portal = null [" + p + "] bei: " + room);
//      //  }

//      //  roomsHash.Add(room);
//      //}

//      //// --- eingehende Portale aller Räume prüfen ---
//      //var portals = new HashSet<RoomPortal>();
//      //foreach (var room in rooms)
//      //{
//      //  for (int p = 0; p < room.incomingPortals.Length; p++)
//      //  {
//      //    var portal = room.incomingPortals[p];
//      //    if (portals.Contains(portal)) throw new Exception("Portal wird doppelt benutzt: " + portal);
//      //    portals.Add(portal);

//      //    if (portal.roomTo != room) throw new Exception("eingehendes Portal [" + p + "] verlinkt nicht zum eigenen Raum, bei: " + room);
//      //    if (!roomsHash.Contains(portal.roomFrom)) throw new Exception("eingehendes Portal [" + p + "] hat einen unbekannten Quell-Raum verlinkt, bei: " + room);

//      //    if (posToRoom[portal.posFrom] != portal.roomFrom) throw new Exception("posFrom passt nicht zu roomFrom, bei: " + room);
//      //    if (posToRoom[portal.posTo] != portal.roomTo) throw new Exception("posTo passt nicht zu roomTo, bei: " + room);
//      //  }
//      //}

//      //// --- ausgehende Portale alle Räume prüfen inkl. Rückverweise ---
//      //var outPortals = new HashSet<RoomPortal>();
//      //foreach (var room in rooms)
//      //{
//      //  for (int p = 0; p < room.outgoingPortals.Length; p++)
//      //  {
//      //    var portal = room.outgoingPortals[p];
//      //    if (!portals.Contains(portal)) throw new Exception("Out-Portal wurde nicht bei den eingehenden Portalen gefunden: " + portal);
//      //    if (outPortals.Contains(portal)) throw new Exception("Out-Portal wird doppelt benutzt: " + portal);
//      //    outPortals.Add(portal);

//      //    if (portal.oppositePortal != room.incomingPortals[p]) throw new Exception("Rückverweis des Portals passt nicht: " + portal);
//      //    if (portal.oppositePortal.oppositePortal != portal) throw new Exception("doppelter Rückverweis des Portals passt nicht: " + portal);
//      //  }
//      //}
//    }
//    #endregion

//    #region # // --- Display ---
//    /// <summary>
//    /// multipliziert mehrere Nummern und gibt das Ergebnis als lesbare Zeichenkette zurück
//    /// </summary>
//    /// <param name="values">Werte, welche miteinander multipliziert werden sollen</param>
//    /// <returns>fertiges Ergebnis</returns>
//    static string MulNumber(IEnumerable<ulong> values)
//    {
//      var mul = new BigInteger(1);
//      ulong mulTmp = 1;
//      foreach (var val in values)
//      {
//        if (val > uint.MaxValue)
//        {
//          mul *= val;
//          continue;
//        }
//        mulTmp *= val;
//        if (mulTmp > uint.MaxValue)
//        {
//          mul *= mulTmp;
//          mulTmp = 1;
//        }
//      }
//      if (mulTmp > 1) mul *= mulTmp;

//      string tmp = "";
//      var txt = mul.ToString();
//      if (txt.Length > 12)
//      {
//        tmp = txt.Substring(0, 4);
//        if (int.Parse(txt.Substring(4, 5)) >= 50000) // Nachkommastelle aufrunden?
//        {
//          tmp = (int.Parse(txt.Substring(0, 4)) + 1).ToString();
//        }
//        tmp = tmp.Insert(1, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + "e" + (txt.Length - 1);
//      }

//      string separator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
//      int c = 0;
//      while (txt.Length > c + 3)
//      {
//        txt = txt.Insert(txt.Length - c - 3, separator);
//        c += 3 + separator.Length;
//      }

//      if (tmp != "")
//      {
//        int max = Console.BufferWidth - tmp.Length - 16;
//        if (txt.Length > max) txt = txt.Substring(0, max - 4) + " ...";
//        txt = tmp + " (" + txt + ")";
//      }

//      return txt;
//    }

//    /// <summary>
//    /// zeigt den maximalen Aufwand zum lösen des gesamten Feldes an
//    /// </summary>
//    /// <param name="indent">Leerzeichen zum einrücken der Zeilen</param>
//    void DisplayEffort(string indent)
//    {
//      Console.WriteLine(indent + "  Effort: " + MulNumber(rooms.Select(x => (ulong)(x.variantsDataUsed))));
//      Console.WriteLine();
//    }

//    /// <summary>
//    /// gibt das Spielfeld in der Konsole aus
//    /// </summary>
//    /// <param name="selectRoom">optional: Raum, welcher dargestellt werden soll</param>
//    /// <param name="selectState">optional: Status des Raums, welcher dargestellt werden soll (Konflikt mit selectPortal)</param>
//    /// <param name="selectPortal">optional: Portal des Raums, welches dargestellt werden soll (Konflikt mit selectState)</param>
//    /// <param name="selectVariant">optionsl: Portal-Variante, welche drargestellt werden soll</param>
//    /// <param name="displayIndent">optional: gibt an wie weit die Anzeige eingerückt sein soll (Default: 2)</param>
//    public void DisplayConsole(int selectRoom = -1, int selectState = -1, int selectPortal = -1, int selectVariant = -1, int displayIndent = 2)
//    {
//      if (selectRoom >= rooms.Length) throw new ArgumentOutOfRangeException("selectRoom");
//      if (selectState >= 0 && selectPortal >= 0) throw new ArgumentException("conflicted: selectState and selectPortal");

//      if (selectState >= 0 && selectRoom < 0) throw new ArgumentOutOfRangeException("selectState");
//      if (selectRoom >= 0 && selectState >= rooms[selectRoom].StateUsed) throw new ArgumentOutOfRangeException("selectState");

//      if (selectPortal >= 0 && selectRoom < 0) throw new ArgumentOutOfRangeException("selectPortal");
//      //      if (selectRoom >= 0 && selectPortal >= rooms

//      if (displayIndent < 0) throw new ArgumentOutOfRangeException("displayIndent");
//      string indent = new string(' ', displayIndent);

//      if (displayIndent + field.Width >= Console.BufferWidth) throw new IndexOutOfRangeException("Console.BufferWidth too small");
//      if (field.Height >= Console.BufferHeight) throw new IndexOutOfRangeException("Console.BufferHeight too small");

//      // --- Spielfeld anzeigen ---
//      string fieldTxt = ("\r\n" + field.GetText()).Replace("\r\n", "\r\n" + indent); // Spielfeld (mit Indent) berechnen

//      int cTop = Console.CursorTop + 1; // Anfangs-Position des Spielfeldes merken
//      Console.WriteLine(fieldTxt);      // Spielfeld ausgeben

//      DisplayEffort(indent);

//      // --- Spielfeld wieder mit Inhalt befüllen ---
//      if (selectRoom >= 0)
//      {
//        int oldTop = Console.CursorTop; // alte Cursor-Position merken
//        var room = rooms[selectRoom];
//        Console.ForegroundColor = ConsoleColor.White;
//        // --- alle Felder des Raumes markieren ---
//        if (selectState >= 0)
//        {
//          var stateInfo = room.GetStateInfo((uint)selectState);
//          var boxes = new HashSet<int>(stateInfo.boxPosis);
//          foreach (int pos in room.fieldPosis)
//          {
//            Console.CursorTop = cTop + pos / field.Width;
//            Console.CursorLeft = indent.Length + pos % field.Width;
//            Console.BackgroundColor = ConsoleColor.DarkGreen;
//            if (room.goalPosis.Contains(pos))
//            {
//              Console.Write(boxes.Contains(pos) ? '*' : (stateInfo.playerPos == pos ? '+' : '.'));
//            }
//            else
//            {
//              Console.Write(boxes.Contains(pos) ? '$' : (stateInfo.playerPos == pos ? '@' : ' '));
//            }
//          }
//        }
//        else
//        {
//          foreach (int pos in room.fieldPosis)
//          {
//            Console.CursorTop = cTop + pos / field.Width;
//            Console.CursorLeft = indent.Length + pos % field.Width;
//            Console.BackgroundColor = ConsoleColor.DarkGreen;
//            Console.Write(field.GetField(pos));
//          }
//        }
//        // --- alle äußeren Portale des Raumes markieren --
//        for (int i = 0; i < room.incomingPortals.Length; i++)
//        {
//          var portal = room.incomingPortals[i];
//          int pos = portal.posFrom;
//          Console.CursorTop = cTop + pos / field.Width;
//          Console.CursorLeft = indent.Length + pos % field.Width;
//          if (selectPortal == i)
//          {
//            Console.ForegroundColor = ConsoleColor.Black;
//            Console.BackgroundColor = ConsoleColor.White;
//          }
//          else
//          {
//            Console.ForegroundColor = ConsoleColor.Gray;
//            Console.BackgroundColor = ConsoleColor.DarkRed;
//          }
//          Console.Write(field.GetField(pos));
//        }

//        Console.CursorTop = oldTop;     // alte Cursor-Position zurück setzen
//        Console.CursorLeft = 0;
//        Console.ForegroundColor = ConsoleColor.Gray;  // Standard-Farben setzen
//        Console.BackgroundColor = ConsoleColor.Black;
//      }

//      // --- Allgemeine Details anzeigen ---
//      if (selectRoom >= 0)
//      {
//        var room = rooms[selectRoom];
//        Console.WriteLine(indent + "    Room: {0:N0} / {1:N0}", selectRoom + 1, rooms.Length);
//        Console.WriteLine();
//        Console.WriteLine(indent + "   Posis: {0}", string.Join(", ", room.fieldPosis));
//        Console.WriteLine();
//        if (selectState >= 0)
//        {
//          Console.WriteLine(indent + "   State: {0:N0} / {1:N0}", selectState + 1, room.StateUsed);
//        }
//        else
//        {
//          Console.WriteLine(indent + "  States: {0:N0}", room.StateUsed);
//          if (room.startPlayerVariants.Count + room.startBoxVariants.Count > 0) Console.WriteLine();
//          for (int v = 0; v < room.startPlayerVariants.Count; v++)
//          {
//            Console.WriteLine(indent + "Ply-Variant {0}: {1}", v + 1, room.GetVariantInfo(room.startPlayerVariants[v]));
//          }
//          for (int v = 0; v < room.startBoxVariants.Count; v++)
//          {
//            Console.WriteLine(indent + "Box-Variant {0}: {1}", v + 1, room.GetVariantInfo(room.startBoxVariants[v]));
//          }
//        }
//        Console.WriteLine();
//        for (int i = 0; i < room.incomingPortals.Length; i++)
//        {
//          var incomingPortal = room.incomingPortals[i];
//          if (i == selectPortal)
//          {
//            Console.ForegroundColor = ConsoleColor.Black;
//            Console.BackgroundColor = ConsoleColor.White;
//            Console.WriteLine(indent + "Portal {0}: {1} (Variants: {2})" + indent, i, incomingPortal, incomingPortal.roomToPlayerVariants.Count + incomingPortal.roomToBoxVariants.Count);
//            Console.ForegroundColor = ConsoleColor.Gray;
//            Console.BackgroundColor = ConsoleColor.Black;
//            if (selectVariant >= 0)
//            {
//              for (int v = 0; v < incomingPortal.roomToPlayerVariants.Count; v++)
//              {
//                if (selectVariant == v)
//                {
//                  Console.ForegroundColor = ConsoleColor.Black;
//                  Console.BackgroundColor = ConsoleColor.Gray;
//                }
//                Console.WriteLine(indent + indent + "Ply-Variant {0}: {1}" + indent, v + 1, incomingPortal.roomTo.GetVariantInfo(incomingPortal.roomToPlayerVariants[v]));
//                if (selectVariant == v)
//                {
//                  Console.ForegroundColor = ConsoleColor.Gray;
//                  Console.BackgroundColor = ConsoleColor.Black;
//                }
//              }
//              for (int v = 0; v < incomingPortal.roomToBoxVariants.Count; v++)
//              {
//                if (selectVariant == v + incomingPortal.roomToPlayerVariants.Count)
//                {
//                  Console.ForegroundColor = ConsoleColor.Black;
//                  Console.BackgroundColor = ConsoleColor.Gray;
//                }
//                Console.WriteLine(indent + indent + "Box-Variant {0}: {1}" + indent, v + 1, incomingPortal.roomTo.GetVariantInfo(incomingPortal.roomToBoxVariants[v]));
//                if (selectVariant == v + incomingPortal.roomToPlayerVariants.Count)
//                {
//                  Console.ForegroundColor = ConsoleColor.Gray;
//                  Console.BackgroundColor = ConsoleColor.Black;
//                }
//              }
//            }
//          }
//          else
//          {
//            Console.WriteLine(indent + "Portal {0}: {1} (Variants: {2})" + indent, i, incomingPortal, incomingPortal.roomToPlayerVariants.Count + incomingPortal.roomToBoxVariants.Count);
//          }
//        }
//      }
//      else
//      {
//        Console.WriteLine(indent + "   Rooms: {0:N0}", rooms.Length);
//        Console.WriteLine();
//        Console.WriteLine(indent + "  Fields: {0:N0}", rooms.Sum(x => x.fieldPosis.Length));
//        Console.WriteLine();
//        Console.WriteLine(indent + "  States: {0:N0}", rooms.Sum(x => x.StateUsed));
//        Console.WriteLine();
//        Console.WriteLine(indent + " Portals: {0:N0}", rooms.Sum(x => x.incomingPortals.Length));
//      }
//      Console.WriteLine();
//    }

//    /// <summary>
//    /// gibt das Spielfeld in der Konsole aus (mit bestimmten Raum-Zuständen)
//    /// </summary>
//    /// <param name="states">Zustände, welche angezeigt werden sollen</param>
//    /// <param name="fixPlayerPos">optional: Angabe einer festen Spieler-Position</param>
//    /// <param name="displayIndent">optional: gibt an wie weit die Anzeige eingerückt sein soll (Default: 2)</param>
//    public void DisplayRoomStates(uint[] states, int fixPlayerPos = -1, int displayIndent = 2)
//    {
//      if (displayIndent < 0) throw new ArgumentOutOfRangeException("displayIndent");
//      string indent = new string(' ', displayIndent);

//      if (displayIndent + field.Width >= Console.BufferWidth) throw new IndexOutOfRangeException("Console.BufferWidth too small");
//      if (field.Height >= Console.BufferHeight) throw new IndexOutOfRangeException("Console.BufferHeight too small");

//      if (states == null) throw new ArgumentNullException("states");
//      if (states.Length != rooms.Length) throw new ArgumentOutOfRangeException("states");

//      // --- Spielfeld anzeigen ---
//      string fieldTxt = ("\r\n" + field.GetText()).Replace("\r\n", "\r\n" + indent); // Spielfeld (mit Indent) berechnen

//      int cTop = Console.CursorTop + 1; // Anfangs-Position des Spielfeldes merken
//      Console.WriteLine(fieldTxt);      // Spielfeld ausgeben

//      int oldTop = Console.CursorTop; // alte Cursor-Position merken

//      for (int r = 0; r < rooms.Length; r++)
//      {
//        var room = rooms[r];
//        if (states[r] >= room.StateUsed) throw new Exception("states[" + r + "] > room[" + r + "].states");
//        var state = room.GetStateInfo(states[r]);

//        // --- alle Felder leeren ---
//        foreach (int pos in room.fieldPosis)
//        {
//          Console.CursorTop = cTop + pos / field.Width;
//          Console.CursorLeft = indent.Length + pos % field.Width;
//          Console.Write(field.IsGoal(pos) ? '.' : ' ');
//        }

//        // --- Spieler hinzufügen ---
//        if (state.playerPos > 0 && fixPlayerPos < 0)
//        {
//          Console.CursorTop = cTop + state.playerPos / field.Width;
//          Console.CursorLeft = indent.Length + state.playerPos % field.Width;
//          Console.Write(field.IsGoal(state.playerPos) ? '+' : '@');
//        }

//        // --- Kisten hinzufügen ---
//        foreach (int pos in state.boxPosis)
//        {
//          Console.CursorTop = cTop + pos / field.Width;
//          Console.CursorLeft = indent.Length + pos % field.Width;
//          Console.Write(field.IsGoal(pos) ? '*' : '$');
//        }
//      }

//      if (fixPlayerPos > 0) // fixe Spieler-Position überschreiben
//      {
//        Console.CursorTop = cTop + fixPlayerPos / field.Width;
//        Console.CursorLeft = indent.Length + fixPlayerPos % field.Width;
//        Console.Write(field.IsGoal(fixPlayerPos) ? '+' : '@');
//      }

//      Console.CursorTop = oldTop;     // alte Cursor-Position zurück setzen
//      Console.CursorLeft = 0;
//      Console.ForegroundColor = ConsoleColor.Gray;  // Standard-Farben setzen
//      Console.BackgroundColor = ConsoleColor.Black;
//    }
//    #endregion

//    #region # // --- Dispose ---
//    /// <summary>
//    /// gibt alle Ressourcen wieder frei
//    /// </summary>
//    public void Dispose()
//    {
//      if (rooms == null) return;
//      foreach (var room in rooms)
//      {
//        room.Dispose();
//      }
//      rooms = null;
//    }

//    /// <summary>
//    /// Destructor
//    /// </summary>
//    ~RoomSolver()
//    {
//      Dispose();
//    }
//    #endregion
//  }
//}
