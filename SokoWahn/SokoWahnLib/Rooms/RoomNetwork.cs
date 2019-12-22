#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
// ReSharper disable PossibleUnintendedReferenceComparison
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

      #region # // --- Varianten erstellen ---
      foreach (var room in rooms)
      {
        room.InitVariants();
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

      // --- eingehende Portale aller Räume prüfen ---
      var portals = new HashSet<RoomPortal>();
      foreach (var room in rooms)
      {
        for (int p = 0; p < room.incomingPortals.Length; p++)
        {
          var portal = room.incomingPortals[p];
          if (portals.Contains(portal)) throw new Exception("Portal wird doppelt benutzt: " + portal);
          portals.Add(portal);

          if (portal.toRoom != room) throw new Exception("eingehendes Portal [" + p + "] verlinkt nicht zum eigenen Raum, bei: " + room);
          if (!roomsHash.Contains(portal.fromRoom)) throw new Exception("eingehendes Portal [" + p + "] hat einen unbekannten Quell-Raum verlinkt, bei: " + room);

          if (posToRoom[portal.fromPos] != portal.fromRoom) throw new Exception("posFrom passt nicht zu roomFrom, bei: " + room);
          if (posToRoom[portal.toPos] != portal.toRoom) throw new Exception("posTo passt nicht zu roomTo, bei: " + room);
        }
      }

      // --- ausgehende Portale alle Räume prüfen inkl. Rückverweise ---
      var outPortals = new HashSet<RoomPortal>();
      foreach (var room in rooms)
      {
        for (int p = 0; p < room.outgoingPortals.Length; p++)
        {
          var portal = room.outgoingPortals[p];
          if (!portals.Contains(portal)) throw new Exception("Out-Portal wurde nicht bei den eingehenden Portalen gefunden: " + portal);
          if (outPortals.Contains(portal)) throw new Exception("Out-Portal wird doppelt benutzt: " + portal);
          outPortals.Add(portal);

          if (portal.oppositePortal != room.incomingPortals[p]) throw new Exception("Rückverweis des Portals passt nicht: " + portal);
          if (portal.oppositePortal.oppositePortal != portal) throw new Exception("doppelter Rückverweis des Portals passt nicht: " + portal);
        }
      }
    }
    #endregion

    #region # // --- Effort ---
    /// <summary>
    /// gibt den theoretischen Rechenaufwand als Zeichenkettenzahl zurück
    /// </summary>
    /// <returns>Rechenaufwand als Zeichenkette</returns>
    public string Effort(int maxLen = 16777216)
    {
      return MulNumber(rooms.Select(room => room.variantList.Count), maxLen);
    }

    /// <summary>
    /// multipliziert mehrere Nummern und gibt das Ergebnis als lesbare Zeichenkette zurück
    /// </summary>
    /// <param name="values">Werte, welche miteinander multipliziert werden sollen</param>
    /// <param name="maxLen">maximale Länge der Ergebnis-Zeichenkette</param>
    /// <returns>fertiges Ergebnis</returns>
    static string MulNumber(IEnumerable<ulong> values, int maxLen = 16777216)
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
