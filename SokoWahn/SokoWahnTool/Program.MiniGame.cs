#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnLib;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
// ReSharper disable UnusedMember.Local
#endregion

namespace SokoWahnTool
{
  static partial class Program
  {
    /// <summary>
    /// startet ein bestimmtes Spielfeld als Mini-Game
    /// </summary>
    /// <param name="iField">Spielfeld, welches verwendet werden soll</param>
    /// <param name="indent">optionaler Abstand zum linken Rand (Default: 2)</param>
    static void MiniGame(ISokoField iField, int indent = 2)
    {
      for (; ; )
      {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(new string(' ', indent) + "  Size: {0} x {1}", iField.Width, iField.Height);
        Console.WriteLine();
        Console.WriteLine(new string(' ', indent) + "Player: {0}, {1}", iField.PlayerPos % iField.Width, iField.PlayerPos / iField.Width);
        Console.WriteLine(("\r\n" + iField.GetText()).Replace("\r\n", "\r\n" + new string(' ', indent))); // Spielfeld ausgeben

        // --- Spieler zusätzlich markieren ---
        Console.SetCursorPosition(iField.PlayerPos % iField.Width + indent, Console.CursorTop - iField.Height + iField.PlayerPos / iField.Width - 1); // Cursor auf den Spieler setzen
        Console.ForegroundColor = ConsoleColor.Green; // Farbe Grün setzen
        Console.Write(iField.GetField(iField.PlayerPos)); // Spieler neu mit grüner Farbe ausgeben
        Console.Write('\b'); // ein Zeichen zurück springen, damit der blinkende Cursor wieder auf den Spieler zeigt
        Console.ForegroundColor = ConsoleColor.Gray; // Farbe zurück auf Default (grau) setzen

        var moves = iField.GetMoveTypes(); // fragt die Bewegungsmöglichkeiten des Spieler ab

        switch (Console.ReadKey(true).Key)
        {
          case ConsoleKey.A:
          case ConsoleKey.LeftArrow:
          case ConsoleKey.NumPad4:
          {
            if ((moves & MoveType.Left) != MoveType.None) iField.Move(moves & MoveType.LeftPush);
          } break; // links

          case ConsoleKey.D:
          case ConsoleKey.RightArrow:
          case ConsoleKey.NumPad6:
          {
            if ((moves & MoveType.Right) != MoveType.None) iField.Move(moves & MoveType.RightPush);
          } break; // rechts

          case ConsoleKey.W:
          case ConsoleKey.UpArrow:
          case ConsoleKey.NumPad8:
          {
            if ((moves & MoveType.Up) != MoveType.None) iField.Move(moves & MoveType.UpPush);
          } break; // hoch

          case ConsoleKey.S:
          case ConsoleKey.DownArrow:
          case ConsoleKey.NumPad2:
          {
            if ((moves & MoveType.Down) != MoveType.None) iField.Move(moves & MoveType.DownPush);
          } break; // runter

          case ConsoleKey.Delete:
          case ConsoleKey.Backspace: break; // Schritt zurück

          case ConsoleKey.Escape: return; // Programm beenden
        }
      }
    }
  }
}
