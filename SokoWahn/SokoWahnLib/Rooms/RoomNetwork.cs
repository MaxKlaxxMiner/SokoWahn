#region # using *.*
using System;
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
        return new Room(field, new[] { (uint)pos }, new RoomPortal[portals], new RoomPortal[portals]);
      }).ToArray();
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
    #endregion
  }
}
