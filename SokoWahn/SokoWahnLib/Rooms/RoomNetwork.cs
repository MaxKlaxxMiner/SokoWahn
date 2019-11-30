#region # using *.*
using System;
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
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    public RoomNetwork(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;
    }

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

    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
    }
  }
}
