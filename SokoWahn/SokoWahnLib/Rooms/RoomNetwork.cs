#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Ein auf Räume basierende Netzwerk
  /// </summary>
  public class RoomNetwork : IDisposable
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

      #region # // --- Räume erstellen ---
      // --- begehbare Felder abfragen und daraus Basis-Räume erstellen ---
      var walkFields = field.GetWalkPosis();
      rooms = walkFields.OrderBy(pos => pos).Select(pos =>
      {
        int portals = (walkFields.Contains(pos - 1) ? 1 : 0) + // eingehendes Portal von der linken Seite
                      (walkFields.Contains(pos + 1) ? 1 : 0) + // eingegendes Portal von der rechten Seite
                      (walkFields.Contains(pos - field.Width) ? 1 : 0) + // eingehendes Portal von oben
                      (walkFields.Contains(pos + field.Width) ? 1 : 0); // eingehendes Portal von unten
        return new Room(field, new[] { pos }, new RoomPortal[portals], new RoomPortal[portals]);
      }).ToArray();
      #endregion

      #region # // --- Portale erstellen ---
      // --- eingehende Portale in den Räumen erstellen und setzen ---
      foreach (var room in rooms)
      {
        int pos = room.fieldPosis.First();
        var portals = room.incomingPortals;
        int pIndex = 0;

        // eingehendes Portal von der linken Seite
        if (walkFields.Contains(pos - 1))
        {
          portals[pIndex++] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos - 1), pos - 1, room, pos);
        }

        // eingehendes Portal von der rechten Seite
        if (walkFields.Contains(pos + 1))
        {
          portals[pIndex++] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos + 1), pos + 1, room, pos);
        }

        // eingehendes Portal von der oberen Seite
        if (walkFields.Contains(pos - field.Width))
        {
          portals[pIndex++] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos - field.Width), pos - field.Width, room, pos);
        }

        // eingehendes Portal von der unteren Seite
        if (walkFields.Contains(pos + field.Width))
        {
          portals[pIndex++] = new RoomPortal(rooms.First(r => r.fieldPosis[0] == pos + field.Width), pos + field.Width, room, pos);
        }

        Debug.Assert(pIndex == portals.Length);
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
        room.InitStates();
      }
      #endregion

      // --- zum Schluss alles überprüfen ---
      Validate(true);
    }
    #endregion

    #region # // --- Validate ---
    /// <summary>
    /// Methode zum Prüfen der Konsistenz aller Daten und Verlinkungen
    /// </summary>
    /// <param name="checkVariants">gibt an, ob auch alle Varianten geprüft werden sollen (kann sehr lange dauern)</param>
    public void Validate(bool checkVariants = false)
    {
      // --- Räume auf Doppler prüfen und Basis-Check der Portale ---
      var roomsHash = new HashSet<Room>();
      var posToRoom = new Dictionary<int, Room>();
      foreach (var room in rooms)
      {
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

        roomsHash.Add(room);
      }
      if (field.GetWalkPosis().Count != posToRoom.Count) throw new Exception("nicht alle begehbaren Felder werden von allen Räumen abgedeckt");

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
