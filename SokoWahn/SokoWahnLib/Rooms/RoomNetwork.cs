#region # using *.*
using System;
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
    }
    #endregion

    #region # // --- DisplayConsole ---
    /// <summary>
    /// gibt das Spielfeld mit bestimmten Auswahl-Kriterien in der Console aus
    /// </summary>
    public void DisplayConsole()
    {
      const string Indent = "  ";

      if (Indent.Length * 2 + field.Width >= Console.BufferWidth || field.Height + 10 >= Console.BufferHeight) // Console zur klein für die Ausgabe?
      {
        Console.WriteLine("Console-Size problem");
        return;
      }

      // --- Basis-Spielfeld anzeigen ---
      string fieldTxt = ("\r\n" + field.GetText()).Replace("\r\n", "\r\n" + Indent); // Spielfeld (mit Indent) einrücken

      int cTop = Console.CursorTop + 1; // Anfangs-Position des Spielfeldes merken
      Console.WriteLine(fieldTxt);      // Basis-Spielfeld ausgeben
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
