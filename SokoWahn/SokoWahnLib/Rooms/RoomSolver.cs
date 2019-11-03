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
      rooms = walkFields.OrderBy(pos => pos).Select(pos => new Room(field, pos, new RoomPortal[ // Größe des Arrays vorher bestimmen
        (walkFields.Contains(pos - 1) ? 1 : 0) +            // Portal zur linken Seite
        (walkFields.Contains(pos + 1) ? 1 : 0) +            // Portal zur rechten Seite
        (walkFields.Contains(pos - field.Width) ? 1 : 0) +  // Portal nach oben
        (walkFields.Contains(pos + field.Width) ? 1 : 0)    // Portal nach unten
       ])).ToArray();

      // --- Basis-Räume miteinander über Portale verknüpfen ---
      foreach (var room in rooms)
      {
        int pos = room.fieldPosis.First();
        var portals = room.portals;
        int pIndex = 0;
        if (walkFields.Contains(pos - 1)) // Portal zur linken Seite erstellen
        {
          portals[pIndex++] = new RoomPortal(pos, pos - 1, rooms.First(r => r.fieldPosis.First() == pos - 1));
        }
        if (walkFields.Contains(pos + 1)) // Portal zur rechten Seite erstellen
        {
          portals[pIndex++] = new RoomPortal(pos, pos + 1, rooms.First(r => r.fieldPosis.First() == pos + 1));
        }
        if (walkFields.Contains(pos - field.Width)) // Portal nach oben erstellen
        {
          portals[pIndex++] = new RoomPortal(pos, pos - field.Width, rooms.First(r => r.fieldPosis.First() == pos - field.Width));
        }
        if (walkFields.Contains(pos + field.Width)) // Portal nach unten erstellen
        {
          portals[pIndex++] = new RoomPortal(pos, pos + field.Width, rooms.First(r => r.fieldPosis.First() == pos + field.Width));
        }
        Debug.Assert(pIndex == portals.Length);
      }

    }

    /// <summary>
    /// gibt das Spielfeld in der Konsole aus
    /// </summary>
    /// <param name="selectRoom">optional: Raum, welcher dargestellt werden soll</param>
    /// <param name="displayIndent">optional: gibt an wie weit die Anzeige eingerückt sein soll (Default: 2)</param>
    public void DisplayConsole(int selectRoom = -1, int displayIndent = 2)
    {
      if (selectRoom >= rooms.Length) throw new ArgumentOutOfRangeException("selectRoom");
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
        foreach (int pos in room.fieldPosis)
        {
          Console.CursorTop = cTop + pos / field.Width;
          Console.CursorLeft = indent.Length + pos % field.Width;
          Console.BackgroundColor = ConsoleColor.DarkGray;
          Console.Write(field.GetField(pos));
        }
        // --- alle Portale des Raumes markieren --
        foreach (var portal in room.portals)
        {
          int pos = portal.posFrom;
          Console.CursorTop = cTop + pos / field.Width;
          Console.CursorLeft = indent.Length + pos % field.Width;
          Console.BackgroundColor = ConsoleColor.DarkGreen;
          Console.Write(field.GetField(pos));
          pos = portal.posTo;
          Console.CursorTop = cTop + pos / field.Width;
          Console.CursorLeft = indent.Length + pos % field.Width;
          Console.BackgroundColor = ConsoleColor.DarkRed;
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
        Console.WriteLine(indent + "   Posis: {0:N0}", string.Join(", ", room.fieldPosis));
        Console.WriteLine();
        for (int i = 0; i < room.portals.Length; i++)
        {
          Console.WriteLine(indent + "Portal {0}: {1}", i, room.portals[i]);
        }
      }
      else
      {
        Console.WriteLine(indent + "   Rooms: {0:N0}", rooms.Length);
        Console.WriteLine();
        Console.WriteLine(indent + "  Fields: {0:N0}", rooms.Sum(x => x.fieldPosis.Count));
        Console.WriteLine();
        Console.WriteLine(indent + " Portals: {0:N0}", rooms.Sum(x => x.portals.Length));
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
