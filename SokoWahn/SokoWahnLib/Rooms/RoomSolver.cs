#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Global
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
    public readonly List<Room> rooms;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches gelöst werden soll</param>
    public RoomSolver(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      // --- begehbare Felder ermitteln ---
      var walkFields = field.GetWalkPosis();
      rooms = walkFields.OrderBy(pos => pos).Select(pos => new Room(field, pos, new RoomPortal[0])).ToList();
    }

    /// <summary>
    /// gibt das Spielfeld in der Konsole aus
    /// </summary>
    /// <param name="selectRoom">optional: Raum, welcher dargestellt werden soll</param>
    /// <param name="displayIndent">optional: gibt an wie weit die Anzeige eingerückt sein soll (Default: 2)</param>
    public void DisplayConsole(int selectRoom = -1, int displayIndent = 2)
    {
      if (selectRoom >= rooms.Count) throw new ArgumentOutOfRangeException("selectRoom");
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
        foreach (int pos in room.fieldPosis)
        {
          Console.CursorTop = cTop + pos / field.Width;
          Console.CursorLeft = indent.Length + pos % field.Width;
          Console.BackgroundColor = ConsoleColor.DarkGray;
          Console.ForegroundColor = ConsoleColor.White;
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
        Console.WriteLine(indent + "Room : {0:N0} / {1:N0}", selectRoom + 1, rooms.Count);
      }
      else
      {
        Console.WriteLine(indent + "Rooms: {0:N0}", rooms.Count);
      }
      Console.WriteLine();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      foreach (var room in rooms)
      {
        room.Dispose();
      }
      rooms.Clear();
    }
  }
}
