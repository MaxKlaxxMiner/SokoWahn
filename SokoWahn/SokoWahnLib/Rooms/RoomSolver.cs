#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum effizienten lösen von Spielfeldern
  /// </summary>
  public class RoomSolver : IDisposable
  {
    /// <summary>
    /// merkt sich das zu lösende Spielfeld
    /// </summary>
    public readonly ISokoField field;

    /// <summary>
    /// merkt sich alle Räume
    /// </summary>
    public Room[] rooms;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches gelöst werden soll</param>
    public RoomSolver(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      // --- begehbare Felder ermitteln und Basis-Räume erstellen ---
      var walkFields = field.GetWalkPosis();
      rooms = walkFields.OrderBy(pos => pos).Select(pos =>
      {
        int portals = (walkFields.Contains(pos - 1) ? 1 : 0) + // eingehendes Portal von der linken Seite
                      (walkFields.Contains(pos + 1) ? 1 : 0) + // eingegendes Portal von der rechten Seite
                      (walkFields.Contains(pos - field.Width) ? 1 : 0) + // eingehendes Portal von oben
                      (walkFields.Contains(pos + field.Width) ? 1 : 0); // eingehendes Portal von unten
        return new Room(field, pos, new RoomPortal[portals], new RoomPortal[portals]);
      }).ToArray();

      // --- eingehende Portale in den Basis-Räumen erstellen und hinzufügen ---
      foreach (var room in rooms)
      {
        int pos = room.fieldPosis.First();
        var portals = room.incomingPortals;
        int pIndex = 0;

        // eingehendes Portal von der linken Seite
        if (walkFields.Contains(pos - 1))
        {
          portals[pIndex++] = new RoomPortal(pos - 1, pos, rooms.First(r => r.fieldPosis.First() == pos - 1), room); // eingehend
        }

        // eingehendes Portal von der rechten Seite
        if (walkFields.Contains(pos + 1))
        {
          portals[pIndex++] = new RoomPortal(pos + 1, pos, rooms.First(r => r.fieldPosis.First() == pos + 1), room);
        }

        // eingehendes Portal von oben
        if (walkFields.Contains(pos - field.Width))
        {
          portals[pIndex++] = new RoomPortal(pos - field.Width, pos, rooms.First(r => r.fieldPosis.First() == pos - field.Width), room);
        }

        // eingehendes Portal von unten
        if (walkFields.Contains(pos + field.Width))
        {
          portals[pIndex++] = new RoomPortal(pos + field.Width, pos, rooms.First(r => r.fieldPosis.First() == pos + field.Width), room);
        }

        Debug.Assert(pIndex == portals.Length);
      }

      // --- ausgehende Portale in den Basis-Räumen referenzieren (bestehende verwenden) ---
      foreach (var room in rooms)
      {
        var iPortals = room.incomingPortals;
        var oPortals = room.outgoingPortals;
        Debug.Assert(iPortals.Length == oPortals.Length);
        for (int pIndex = 0; pIndex < iPortals.Length; pIndex++)
        {
          oPortals[pIndex] = iPortals[pIndex].roomFrom.incomingPortals.First(p => p.posFrom == iPortals[pIndex].posTo);
          iPortals[pIndex].oppositePortal = oPortals[pIndex];
          Debug.Assert(iPortals[pIndex].posFrom == oPortals[pIndex].posTo);
          Debug.Assert(iPortals[pIndex].posTo == oPortals[pIndex].posFrom);
          Debug.Assert(iPortals[pIndex].roomFrom == oPortals[pIndex].roomTo);
          Debug.Assert(iPortals[pIndex].roomTo == oPortals[pIndex].roomFrom);
        }
      }

    }

    /// <summary>
    /// gibt das Spielfeld in der Konsole aus
    /// </summary>
    /// <param name="selectRoom">optional: Raum, welcher dargestellt werden soll</param>
    /// <param name="selectState">optional: Status des Raums, welcher dargestellt werden soll (Konflikt mit selectPortal)</param>
    /// <param name="selectPortal">optional: Portal des Raums, welches dargestellt werden soll (Konflikt mit selectState)</param>
    /// <param name="displayIndent">optional: gibt an wie weit die Anzeige eingerückt sein soll (Default: 2)</param>
    public void DisplayConsole(int selectRoom = -1, int selectState = -1, int selectPortal = -1, int displayIndent = 2)
    {
      if (selectRoom >= rooms.Length) throw new ArgumentOutOfRangeException("selectRoom");
      if (selectState >= 0 && selectPortal >= 0) throw new ArgumentException("conflicted: selectState and selectPortal");

      if (selectState >= 0 && selectRoom < 0) throw new ArgumentOutOfRangeException("selectState");
      if (selectRoom >= 0 && selectState >= rooms[selectRoom].stateDataUsed) throw new ArgumentOutOfRangeException("selectState");

      if (selectPortal >= 0 && selectRoom < 0) throw new ArgumentOutOfRangeException("selectPortal");
      //      if (selectRoom >= 0 && selectPortal >= rooms

      if (displayIndent < 0) throw new ArgumentOutOfRangeException("displayIndent");
      string indent = new string(' ', displayIndent);

      if (displayIndent + field.Width >= Console.BufferWidth) throw new IndexOutOfRangeException("Console.BufferWidth too small");
      if (field.Height >= Console.BufferHeight) throw new IndexOutOfRangeException("Console.BufferHeight too small");

      // --- Spielfeld anzeigen ---
      string fieldTxt = ("\r\n" + field.GetText()).Replace("\r\n", "\r\n" + indent); // Spielfeld (mit Indent) berechnen
      //if (selectRoom >= 0) fieldTxt = new string(fieldTxt.Select(SokoFieldHelper.ClearChar).ToArray()); // Spielfeld im Select-Modus leeren

      int cTop = Console.CursorTop + 1; // Anfangs-Position des Spielfeldes merken
      Console.WriteLine(fieldTxt);            // Spielfeld ausgeben

      // --- Spielfeld wieder mit Inhalt befüllen ---
      if (selectRoom >= 0)
      {
        int oldTop = Console.CursorTop; // alte Cursor-Position merken
        var room = rooms[selectRoom];
        Console.ForegroundColor = ConsoleColor.White;
        // --- alle Felder des Raumes markieren ---
        if (selectState >= 0)
        {
          var stateInfo = room.GetStateInfo((uint)selectState);
          var boxes = new HashSet<int>(stateInfo.boxPosis);
          foreach (int pos in room.fieldPosis)
          {
            Console.CursorTop = cTop + pos / field.Width;
            Console.CursorLeft = indent.Length + pos % field.Width;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            if (room.goalPosis.Contains(pos))
            {
              Console.Write(boxes.Contains(pos) ? '*' : (stateInfo.playerPos == pos ? '+' : '.'));
            }
            else
            {
              Console.Write(boxes.Contains(pos) ? '$' : (stateInfo.playerPos == pos ? '@' : ' '));
            }
          }
        }
        else
        {
          foreach (int pos in room.fieldPosis)
          {
            Console.CursorTop = cTop + pos / field.Width;
            Console.CursorLeft = indent.Length + pos % field.Width;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.Write(field.GetField(pos));
          }
        }
        // --- alle äußeren Portale des Raumes markieren --
        for (int i = 0; i < room.incomingPortals.Length; i++)
        {
          var portal = room.incomingPortals[i];
          int pos = portal.posFrom;
          Console.CursorTop = cTop + pos/field.Width;
          Console.CursorLeft = indent.Length + pos%field.Width;
          if (selectPortal == i)
          {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
          }
          else
          {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.DarkRed;
          }
          Console.Write(field.GetField(pos));
        }

        Console.CursorTop = oldTop;     // alte Cursor-Position zurück setzen
        Console.CursorLeft = 0;
        Console.ForegroundColor = ConsoleColor.Gray;  // Standard-Farben setzen
        Console.BackgroundColor = ConsoleColor.Black;
      }

      // --- Allgemeine Details anzeigen ---
      if (selectRoom >= 0)
      {
        var room = rooms[selectRoom];
        Console.WriteLine(indent + "    Room: {0:N0} / {1:N0}", selectRoom + 1, rooms.Length);
        Console.WriteLine();
        Console.WriteLine(indent + "   Posis: {0}", string.Join(", ", room.fieldPosis));
        Console.WriteLine();
        if (selectState >= 0)
        {
          Console.WriteLine(indent + "   State: {0:N0} / {1:N0}", selectState + 1, room.stateDataUsed);
        }
        else
        {
          Console.WriteLine(indent + "  States: {0:N0}", room.stateDataUsed);
        }
        Console.WriteLine();
        for (int i = 0; i < room.incomingPortals.Length; i++)
        {
          if (i == selectPortal)
          {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(indent + "Portal {0}: {1}" + indent, i, room.incomingPortals[i]);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
          }
          else
          {
            Console.WriteLine(indent + "Portal {0}: {1}" + indent, i, room.incomingPortals[i]);
          }
        }
      }
      else
      {
        Console.WriteLine(indent + "   Rooms: {0:N0}", rooms.Length);
        Console.WriteLine();
        Console.WriteLine(indent + "  Fields: {0:N0}", rooms.Sum(x => x.fieldPosis.Length));
        Console.WriteLine();
        Console.WriteLine(indent + "  States: {0:N0}", rooms.Sum(x => x.stateDataUsed));
        Console.WriteLine();
        Console.WriteLine(indent + " Portals: {0:N0}", rooms.Sum(x => x.incomingPortals.Length));
      }
      Console.WriteLine();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (rooms == null) return;
      foreach (var room in rooms)
      {
        room.Dispose();
      }
      rooms = null;
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~RoomSolver()
    {
      Dispose();
    }
  }
}
