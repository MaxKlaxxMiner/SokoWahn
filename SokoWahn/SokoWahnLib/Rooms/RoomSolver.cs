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
    public void DisplayConsole()
    {
      Console.WriteLine(("\r\n" + field.GetText()).Replace("\r\n", "\r\n  ")); // Spielfeld (mit Indent) ausgeben
      Console.WriteLine("  Rooms: {0:N0}", rooms.Count);
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
