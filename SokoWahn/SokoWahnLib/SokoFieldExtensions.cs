#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Hilfmethoden für SokoWahn-Spielfeld
  /// </summary>
  public static class SokoFieldExtensions
  {
    #region # public static unsafe string GetText(this ISokoField field)
    /// <summary>
    /// gibt das gesamte Spielfeld im Text-Format aus
    /// </summary>
    /// <param name="field">Spielfeld, welches ausgegeben werden soll</param>
    /// <returns>fertiges Spielfeld im Textformat</returns>
    public static unsafe string GetText(this ISokoField field)
    {
      int w = field.Width;
      int h = field.Height;
      string result = UnsafeHelper.FastAllocateString((w + 2) * h); // Ausgabe Größe berechnen (pro Zeile 2 Zeichen extra für die Zeilenumbrüche)

      fixed (char* resultP = result)
      {
        for (int resultPos = 0, fieldPos = 0; resultPos < result.Length; )
        {
          for (int x = 0; x < w; x++)
          {
            resultP[resultPos++] = field.GetField(fieldPos++);
          }
          resultP[resultPos++] = '\r'; // Zeilenumbruch hinzufügen
          resultP[resultPos++] = '\n';
        }
      }

      return result;
    }
    #endregion

    #region # public static MoveType GetMoveTypes(this ISokoField field) // gibt alle Bewegungsmöglichkeiten des Spielers zurück
    /// <summary>
    /// gibt alle Bewegungsmöglichkeiten des Spielers zurück
    /// </summary>
    /// <param name="field">Spielfeld, welches geprüft werden soll</param>
    /// <returns>Flags vom Typ MoveType</returns>
    public static MoveType GetMoveTypes(this ISokoField field)
    {
      var result = MoveType.None;

      int w = field.Width;
      int h = field.Height;
      int p = field.PlayerPos;
      int px = p % w;
      int py = p / w;

      // --- links prüfen ---
      if (px > 0)
      {
        char f = field.GetField(p - 1);
        if (f == ' ' || f == '.') result |= MoveType.Left; // Spielfeld leer?
        else if ((f == '$' || f == '*') && px > 1) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetField(p - 2);
          if (f == ' ' || f == '.') result |= MoveType.LeftPush; // Feld hinter der Kiste leer?
        }
      }

      // --- rechts prüfen ---
      if (px < w - 1)
      {
        char f = field.GetField(p + 1);
        if (f == ' ' || f == '.') result |= MoveType.Right; // Spielfeld leer?
        else if ((f == '$' || f == '*') && px < w - 2) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetField(p + 2);
          if (f == ' ' || f == '.') result |= MoveType.RightPush; // Feld hinter der Kiste leer?
        }
      }

      // --- oben prüfen ---
      if (py > 0)
      {
        char f = field.GetField(p - w);
        if (f == ' ' || f == '.') result |= MoveType.Up; // Spielfeld leer?
        else if ((f == '$' || f == '*') && py > 1) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetField(p - w * 2);
          if (f == ' ' || f == '.') result |= MoveType.UpPush; // Feld hinter der Kiste leer?
        }
      }

      // --- unten prüfen ---
      if (py < h - 1)
      {
        char f = field.GetField(p + w);
        if (f == ' ' || f == '.') result |= MoveType.Down; // Spielfeld leer?
        else if ((f == '$' || f == '*') && py < h - 2) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetField(p + w * 2);
          if (f == ' ' || f == '.') result |= MoveType.DownPush; // Feld hinter der Kiste leer?
        }
      }

      // --- gefundene Ergebnisse zurück geben ---
      return result;
    }
    #endregion
  }
}
