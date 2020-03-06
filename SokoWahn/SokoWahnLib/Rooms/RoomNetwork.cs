#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using SokoWahnLib.Rooms.Merger;

// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable LoopCanBePartlyConvertedToQuery
// ReSharper disable RedundantIfElseBlock
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Ein auf Räume basierende Netzwerk
  /// </summary>
  public sealed class RoomNetwork : IDisposable
  {
    /// <summary>
    /// Spielfeld, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;

    /// <summary>
    /// alle Räume, welche zum Netzwerk gehören
    /// </summary>
    public Room[] rooms;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    public RoomNetwork(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      var boxScan = SokoBoxScanner.ScanSingleBoxPushes(field);
      //boxScan = null; // Test ohne Scanner

      #region # // --- Räume erstellen ---
      // --- begehbare Felder abfragen und daraus Basis-Räume erstellen ---
      var walkPosis = field.GetWalkPosis();

      uint roomIndex = 0;
      uint maxBoxes = (uint)walkPosis.Sum(pos => field.IsBox(pos) ? 1 : 0);
      rooms = walkPosis.OrderBy(pos => pos).ToArray().Select(pos =>
      {
        int portalCount = (walkPosis.Contains(pos - 1) ? 1 : 0) + // eingehendes Portal von der linken Seite
                          (walkPosis.Contains(pos + 1) ? 1 : 0) + // eingegendes Portal von der rechten Seite
                          (walkPosis.Contains(pos - field.Width) ? 1 : 0) + // eingehendes Portal von oben
                          (walkPosis.Contains(pos + field.Width) ? 1 : 0); // eingehendes Portal von unten
        return new Room(roomIndex++, field, new[] { pos }, new RoomPortal[portalCount], new RoomPortal[portalCount], maxBoxes);
      }).ToArray();

      if (rooms.Sum(room => room.goalPosis.Length) != rooms.Sum(room => room.startBoxPosis.Length)) throw new SokoFieldException("goal count != box count");
      #endregion

      #region # // --- Portale erstellen ---
      // --- eingehende Portale in den Räumen erstellen und setzen ---
      foreach (var room in rooms)
      {
        int pos = room.fieldPosis.First();
        var portals = room.incomingPortals;
        int portalIndex = 0;

        // eingehendes Portal von der linken Seite
        if (walkPosis.Contains(pos - 1))
        {
          bool boxBlocked = boxScan.All(push => push.Key != pos - 1 || push.Value != pos) || boxScan.All(push => push.Key != pos || push.Value != pos + 1);
          portals[portalIndex] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos - 1), pos - 1, room, pos, (uint)portalIndex, boxBlocked);
          portalIndex++;
        }

        // eingehendes Portal von der rechten Seite
        if (walkPosis.Contains(pos + 1))
        {
          bool boxBlocked = boxScan.All(push => push.Key != pos + 1 || push.Value != pos) || boxScan.All(push => push.Key != pos || push.Value != pos - 1);
          portals[portalIndex] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos + 1), pos + 1, room, pos, (uint)portalIndex, boxBlocked);
          portalIndex++;
        }

        // eingehendes Portal von der oberen Seite
        if (walkPosis.Contains(pos - field.Width))
        {
          bool boxBlocked = boxScan.All(push => push.Key != pos - field.Width || push.Value != pos) || boxScan.All(push => push.Key != pos || push.Value != pos + field.Width);
          portals[portalIndex] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos - field.Width), pos - field.Width, room, pos, (uint)portalIndex, boxBlocked);
          portalIndex++;
        }

        // eingehendes Portal von der unteren Seite
        if (walkPosis.Contains(pos + field.Width))
        {
          bool boxBlocked = boxScan.All(push => push.Key != pos + field.Width || push.Value != pos) || boxScan.All(push => push.Key != pos || push.Value != pos - field.Width);
          portals[portalIndex] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos + field.Width), pos + field.Width, room, pos, (uint)portalIndex, boxBlocked);
          portalIndex++;
        }

        Debug.Assert(portalIndex == portals.Length);
      }

      // --- ausgehende Portale in den Basis-Räumen setzen und verlinken ---
      foreach (var room in rooms)
      {
        var iPortals = room.incomingPortals;
        var oPortals = room.outgoingPortals;
        Debug.Assert(iPortals.Length == oPortals.Length);
        for (int pIndex = 0; pIndex < iPortals.Length; pIndex++)
        {
          oPortals[pIndex] = iPortals[pIndex].fromRoom.incomingPortals.First(p => p.fromPos == iPortals[pIndex].toPos);
          iPortals[pIndex].oppositePortal = oPortals[pIndex];
          Debug.Assert(iPortals[pIndex].fromPos == oPortals[pIndex].toPos);
          Debug.Assert(iPortals[pIndex].toPos == oPortals[pIndex].fromPos);
          Debug.Assert(iPortals[pIndex].fromRoom == oPortals[pIndex].toRoom);
          Debug.Assert(iPortals[pIndex].toRoom == oPortals[pIndex].fromRoom);
        }
      }
      #endregion

      #region # // --- Raumzustände erstellen ---
      foreach (var room in rooms)
      {
        room.InitStates(boxScan);
      }
      #endregion

      #region # // --- Varianten erstellen ---
      foreach (var room in rooms)
      {
        room.InitVariants(boxScan);
      }
      #endregion

      // --- zum Schluss prüfen ---
      Validate(true);
    }
    #endregion

    #region # // --- MergeRooms ---
    /// <summary>
    /// verschmilz zwei Räume
    /// </summary>
    /// <param name="room1">erster Raum</param>
    /// <param name="room2">zweiter Raum</param>
    /// <param name="mergeInfo">optionale Status-Meldung zum Ablauf des Merge-Vorganges</param>
    public void MergeRooms(Room room1, Room room2, Func<string, bool> mergeInfo = null)
    {
      if (mergeInfo == null) mergeInfo = x => true;

      var roomMerger = new RoomMerger(this, room1, room2);

      if (Tools.TickRefresh() && !mergeInfo("Merge: mix states")) return;
      roomMerger.Step1_MixStates();

      if (Tools.TickRefresh() && !mergeInfo("Merge: start variants")) return;
      roomMerger.Step2_StartVariants();

      if (!roomMerger.Step3_PortalVariants(txt => mergeInfo("Merge: portal variants - " + txt))) return;

      if (Tools.TickRefresh() && !mergeInfo("Merge: update portals")) return;
      roomMerger.Step4_UpdatePortals();

      if (Tools.TickRefresh() && !mergeInfo("Merge: optimize states")) return;
      roomMerger.Step5_OptimizeStates();

      if (Tools.TickRefresh() && !mergeInfo("Merge: update rooms")) return;
      roomMerger.Step6_UpdateRooms();

      // Ressourcen der alten Räume wieder freigeben
      room1.Dispose();
      room2.Dispose();

      if (Tools.TickRefresh() && !mergeInfo("Merge: validate")) return;
      Validate(); // einfache Validierung der Räume und Portale

      //if (!mergeInfo("Merge: deadlock")) return;

      //var scanner = new RoomDeadlockScanner(roomMerger.newRoom);
      //scanner.Step1_CreateReverseMap();
      //scanner.Step2_ScanForward();
      //scanner.Step3_ScanBackward();
      //scanner.Step4_RemoveUnusedVariants();

      mergeInfo("Merge: ok, remain: " + rooms.Length);
    }
    #endregion

    #region # // --- Validate ---
    /// <summary>
    /// Methode zum Prüfen der Konsistenz aller Daten und Verlinkungen
    /// </summary>
    /// <param name="checkVariants">gibt an, ob auch alle Varianten geprüft werden sollen (kann sehr lange dauern)</param>
    public void Validate(bool checkVariants = false)
    {
      #region # // --- Räume auf Doppler prüfen und Basis-Check der Portale ---
      var roomsHash = new HashSet<Room>();
      var posToRoom = new Dictionary<int, Room>();
      int startRoom = -1;
      for (uint roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
      {
        var room = rooms[roomIndex];
        if (room.roomIndex != roomIndex) throw new Exception("fehlerhafter Room-Index: " + room.roomIndex + " != " + roomIndex);
        if (roomsHash.Contains(room)) throw new Exception("doppelten Raum erkannt: " + room);

        if (room.incomingPortals.Length != room.outgoingPortals.Length)
        {
          throw new Exception("Anzahl der ein- und ausgehenden Portale stimmen nicht: " + room);
        }

        foreach (var pos in room.fieldPosis)
        {
          if (posToRoom.ContainsKey(pos)) throw new Exception("Feld " + pos + " wird in zwei Räumen gleichzeitig verwendet: " + posToRoom[pos] + " und " + room);
          posToRoom.Add(pos, room);
        }

        for (int p = 0; p < room.incomingPortals.Length; p++)
        {
          if (room.incomingPortals[p] == null) throw new Exception("eingehendes Portal = null [" + p + "]: " + room);
          if (room.outgoingPortals[p] == null) throw new Exception("ausgehendes Portal = null [" + p + "]: " + room);
        }

        if (room.startVariantCount > 0)
        {
          if (startRoom >= 0) throw new Exception("doppelte Start-Räume gefunden: " + startRoom + " und " + roomIndex);
          startRoom = (int)roomIndex;
        }

        roomsHash.Add(room);
      }
      if (startRoom < 0) throw new Exception("es wurde kein Start-Raum gefunden");
      if (field.GetWalkPosis().Count != posToRoom.Count) throw new Exception("nicht alle begehbaren Felder werden von allen Räumen abgedeckt");
      #endregion

      #region # // --- eingehende Portale aller Räume prüfen ---
      var portals = new HashSet<RoomPortal>();
      foreach (var room in rooms)
      {
        for (int i = 0; i < room.incomingPortals.Length; i++)
        {
          var iPortal = room.incomingPortals[i];
          if (iPortal.iPortalIndex != i) throw new Exception("iPortalIndex ist fehlerhaft " + iPortal.iPortalIndex + " != " + i);

          if (portals.Contains(iPortal)) throw new Exception("Portal wird doppelt benutzt: " + iPortal);
          portals.Add(iPortal);

          if (iPortal.toRoom != room) throw new Exception("eingehendes Portal [" + i + "] verlinkt nicht zum eigenen Raum, bei: " + room);
          if (!roomsHash.Contains(iPortal.fromRoom)) throw new Exception("eingehendes Portal [" + i + "] hat einen unbekannten Quell-Raum verlinkt, bei: " + room);

          if (posToRoom[iPortal.fromPos] != iPortal.fromRoom) throw new Exception("posFrom passt nicht zu roomFrom, bei: " + room);
          if (posToRoom[iPortal.toPos] != iPortal.toRoom) throw new Exception("posTo passt nicht zu roomTo, bei: " + room);
        }
      }
      #endregion

      #region # // --- ausgehende Portale alle Räume prüfen inkl. Rückverweise ---
      var outPortals = new HashSet<RoomPortal>();
      foreach (var room in rooms)
      {
        for (int i = 0; i < room.outgoingPortals.Length; i++)
        {
          var oPortal = room.outgoingPortals[i];
          if (!portals.Contains(oPortal)) throw new Exception("Out-Portal wurde nicht bei den eingehenden Portalen gefunden: " + oPortal);
          if (outPortals.Contains(oPortal)) throw new Exception("Out-Portal wird doppelt benutzt: " + oPortal);
          outPortals.Add(oPortal);

          if (oPortal.oppositePortal != room.incomingPortals[i]) throw new Exception("Rückverweis des Portals passt nicht: " + oPortal);
          if (oPortal.oppositePortal.oppositePortal != oPortal) throw new Exception("doppelter Rückverweis des Portals passt nicht: " + oPortal);
        }
      }
      #endregion

      #region # // --- Zustände und Varianten prüfen ---
      if (checkVariants)
      {
        int currentRoomIndex = -1;
        int currentPortalIndex = -1;
        ulong currentState = ulong.MaxValue;
        ulong currentVariant = ulong.MaxValue;

        try
        {
          for (int roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
          {
            currentRoomIndex = roomIndex;
            currentPortalIndex = -1;
            currentState = ulong.MaxValue;
            currentVariant = ulong.MaxValue;

            var room = rooms[roomIndex];
            var stateList = room.stateList;
            if (stateList.Count < 1) throw new IndexOutOfRangeException();
            if (room.startState >= stateList.Count) throw new IndexOutOfRangeException();
            var variantList = room.variantList;
            using (var usingStates = new Bitter(stateList.Count))
            using (var usingVariants = new Bitter(variantList.Count))
            {
              usingStates.SetBit(0); // 0-Zustand immer pauschal markieren (End-Zustand)
              usingStates.SetBit(room.startState); // Start-Zustand immer markieren

              bool pushVariant = false;
              // --- Start-Varianten prüfen ---
              for (ulong variant = 0; variant < room.startVariantCount; variant++)
              {
                currentVariant = variant;
                if (variant >= usingVariants.Length) throw new IndexOutOfRangeException();
                usingVariants.SetBit(variant);

                var v = variantList.GetData(variant);
                if (v.pushes == 0)
                {
                  if (pushVariant) throw new Exception("Start-Varianten: Push vor Move-Variante erkannt");
                }
                else
                {
                  pushVariant = true;
                }

                if (v.oldState >= usingStates.Length) throw new IndexOutOfRangeException();
                usingStates.SetBit(v.oldState);

                if (v.newState >= usingStates.Length) throw new IndexOutOfRangeException();
                usingStates.SetBit(v.newState);
              }

              // --- Portal-Varianten prüfen ---
              currentPortalIndex = 0;
              foreach (var portal in room.incomingPortals)
              {
                foreach (var state in portal.variantStateDict.GetAllStates())
                {
                  currentState = state;
                  if (state >= usingStates.Length) throw new IndexOutOfRangeException();
                  pushVariant = false;
                  foreach (var variant in portal.variantStateDict.GetVariantSpan(state).AsEnumerable())
                  {
                    if (variant != currentVariant + 1) throw new Exception("Varianten nicht Lückenfrei! erwartet: " + (currentVariant + 1) + ", vorhanden: " + variant);
                    currentVariant = variant;
                    if (variant >= usingVariants.Length) throw new IndexOutOfRangeException();
                    if (usingVariants.GetBit(variant)) throw new Exception("mehrfach benutzte Varianten erkannt");
                    usingVariants.SetBit(variant);
                    usingStates.SetBit(state);

                    var v = variantList.GetData(variant);
                    if (v.pushes == 0)
                    {
                      if (pushVariant) throw new Exception("Portal-Varianten: Push vor Move-Variante erkannt");
                    }
                    else
                    {
                      pushVariant = true;
                    }
                  }
                }
                currentPortalIndex++;
              }
              currentState = ulong.MaxValue;

              // --- Zustandänderungen durch eingehende Kisten prüfen ---
              currentPortalIndex = 0;
              foreach (var portal in room.incomingPortals)
              {
                foreach (var boxSwap in portal.stateBoxSwap)
                {
                  if (boxSwap.Key >= usingStates.Length) throw new IndexOutOfRangeException();
                  if (boxSwap.Value >= usingStates.Length) throw new IndexOutOfRangeException();
                  if (boxSwap.Key == boxSwap.Value) throw new Exception("unnötige BoxSwap erkannt");
                  usingStates.SetBit(boxSwap.Key);
                  //usingStates.SetBit(boxSwap.Value); // -> wird doch ignoriert, da der Ziel-Zustand aus dem eventuell erkannten Zustand nicht mehr erreichbar ist
                }
                currentPortalIndex++;
              }
              currentPortalIndex = -1;

              if (usingStates.CountMarkedBits(0) != usingStates.Length)
              {
                currentState = usingStates.CountMarkedBits(0);
                currentVariant = ulong.MaxValue;
                throw new Exception("nicht alle Zustände werden verwendet");
              }
              if (usingVariants.CountMarkedBits(0) != usingVariants.Length)
              {
                currentState = ulong.MaxValue;
                currentVariant = usingVariants.CountMarkedBits(0);
                throw new Exception("nicht alle Varianten werden verwendet");
              }
            }
          }
        }
        catch (Exception exc)
        {
          string txt = exc.Message + "\r\n\r\nRoom-Index: " + currentRoomIndex;
          if (currentState < ulong.MaxValue) txt += "\r\nState: " + (currentState == 0 ? "finish" : currentState.ToString());
          if (currentPortalIndex >= 0) txt += "\r\nPortal-Index: " + currentPortalIndex;
          if (currentVariant < ulong.MaxValue) txt += "\r\nVariant: " + currentVariant;
          throw new Exception(txt + "\r\n");
        }
      }
      #endregion
    }
    #endregion

    #region # // --- Effort ---
    /// <summary>
    /// gibt den theoretischen Rechenaufwand als Zeichenkettenzahl zurück
    /// </summary>
    /// <param name="rooms">optional: Räume, welche speziell zusammengerechnet werden soll: default: alle</param>
    /// <returns>Rechenaufwand als Zeichenkette</returns>
    public string Effort(Room[] rooms = null)
    {
      if (rooms == null || rooms.Length == 0) rooms = this.rooms;
      return MulNumberStr(rooms.Select(room => room.variantList.Count));
    }

    /// <summary>
    /// multipliziert mehrere Nummern und gibt das Ergebnis als BigInteger zurück
    /// </summary>
    /// <param name="values">Werte, welche miteinander multipliziert werden sollen</param>
    /// <returns>fertiges Ergebnis</returns>
    public static BigInteger MulNumber(IEnumerable<ulong> values)
    {
      var mul = new BigInteger(1);
      ulong mulTmp = 1;
      foreach (var val in values)
      {
        if (val == 0) continue;
        if (val > uint.MaxValue) { mul *= val; continue; }
        mulTmp *= val;
        if (mulTmp < uint.MaxValue) continue;
        mul *= mulTmp;
        mulTmp = 1;
      }
      if (mulTmp > 1) mul *= mulTmp;

      return mul;
    }

    /// <summary>
    /// multipliziert mehrere Nummern und gibt das Ergebnis als lesbare Zeichenkette zurück
    /// </summary>
    /// <param name="values">Werte, welche miteinander multipliziert werden sollen</param>
    /// <param name="maxLen">maximale Länge der Ergebnis-Zeichenkette</param>
    /// <returns>fertiges Ergebnis</returns>
    public static string MulNumberStr(IEnumerable<ulong> values, int maxLen = 16777216)
    {
      var mul = MulNumber(values);

      string tmp = "";
      var txt = mul.ToString();
      if (txt.Length > 12)
      {
        tmp = txt.Substring(0, 4);
        if (int.Parse(txt.Substring(4, 5)) >= 50000) // Nachkommastelle aufrunden?
        {
          tmp = (int.Parse(txt.Substring(0, 4)) + 1).ToString();
        }
        tmp = tmp.Insert(1, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + "e" + (txt.Length - 1);
      }

      string separator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
      int c = 0;
      while (txt.Length > c + 3)
      {
        txt = txt.Insert(txt.Length - c - 3, separator);
        c += 3 + separator.Length;
      }

      if (tmp != "")
      {
        int max = maxLen - 16;
        if (txt.Length > max) txt = txt.Substring(0, max - 4) + " ...";
        txt = tmp + " (" + txt + ")";
      }

      return txt;
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (rooms != null)
      {
        for (int r = 0; r < rooms.Length; r++)
        {
          if (rooms[r] != null) rooms[r].Dispose();
          rooms[r] = null;
        }
        rooms = null;
      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~RoomNetwork()
    {
      Dispose();
    }
    #endregion
  }
}
