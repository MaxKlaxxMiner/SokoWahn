#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnLib;
#endregion

namespace SokoWahnTool
{
  static class Program
  {
    static void Main(string[] args)
    {
      var field = new SokoField(@"
            #####
            #   #
            #$  #
          ###  $##
          #  $ $ #
        ### # ## #   ######
        #   #@## #####  ..#
        # $  $          ..#
        ##### ### # ##  ..#
            #     #########
            #######
      ");

      ISokoField iField = field; // nur Interface abfragen

      for (; ; )
      {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("    Size: {0} x {1}", iField.Width, iField.Height);
        Console.WriteLine();
        Console.WriteLine("  Player: {0}, {1}", iField.PlayerPos % iField.Width, iField.PlayerPos / iField.Width);
        Console.WriteLine();
        Console.WriteLine(iField.GetText()); // Spielfeld ausgeben

        // --- Spieler zusätzlich markieren ---
        Console.SetCursorPosition(iField.PlayerPos % iField.Width, Console.CursorTop - iField.Height + iField.PlayerPos / iField.Width - 1); // Cursor auf den Spieler setzen
        Console.ForegroundColor = ConsoleColor.Green; // Farbe Grün setzen
        Console.Write(iField.GetField(iField.PlayerPos)); // Spieler neu mit grüner Farbe ausgeben
        Console.Write('\b'); // ein Zeichen zurück springen, damit der blinkende Cursor wieder auf den Spieler zeigt
        Console.ForegroundColor = ConsoleColor.Gray; // Farbe zurück auf Default (grau) setzen

        var moves = iField.GetMoveTypes(); // fragt die Bewegungsmöglichkeiten des Spieler ab

        switch (Console.ReadKey(true).Key)
        {
          case ConsoleKey.A:
          case ConsoleKey.LeftArrow:
          case ConsoleKey.NumPad4: break; // links

          case ConsoleKey.D:
          case ConsoleKey.RightArrow:
          case ConsoleKey.NumPad6: break; // rechts

          case ConsoleKey.W:
          case ConsoleKey.UpArrow:
          case ConsoleKey.NumPad8: break; // hoch

          case ConsoleKey.S:
          case ConsoleKey.DownArrow:
          case ConsoleKey.NumPad2: break; // runter

          case ConsoleKey.Delete:
          case ConsoleKey.Backspace: break; // Schritt zurück

          case ConsoleKey.Escape: return; // Programm beenden
        }
      }
    }
  }
}
