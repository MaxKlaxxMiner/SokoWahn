#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum effizienten lösen von Spielfeldern
  /// </summary>
  public class RoomSolver
  {
    /// <summary>
    /// merkt sich das zu lösende Spielfeld
    /// </summary>
    readonly ISokoField field;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches gelöst werden soll</param>
    public RoomSolver(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      // --- begehbare Felder ermitteln ---
      var walkFields = new HashSet<int>();
      var todo = new Stack<int>();
      todo.Push(field.PlayerPos);
      while (todo.Count > 0)
      {
        int pos = todo.Pop();
        if (field.GetField(pos) == '#') continue; // Feld ist nie begehbar
        if (walkFields.Contains(pos)) continue;   // Feld schon bekannt
        walkFields.Add(pos);                      // bekannte Felder merken
        todo.Push(pos - 1); // links hinzufügen
        todo.Push(pos + 1); // rechts hinzufügen
        todo.Push(pos - field.Width); // oben hinzufügen
        todo.Push(pos + field.Width); // unten hinzufügen
      }
    }

    /// <summary>
    /// gibt das Spielfeld in der Konsole aus
    /// </summary>
    public void DisplayConsole()
    {
      Console.WriteLine(("\r\n" + field.GetText()).Replace("\r\n", "\r\n  ")); // Spielfeld (mit Indent) ausgeben
    }
  }
}
