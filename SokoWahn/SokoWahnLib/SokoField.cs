#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse für ein komplettes SokoWahn-Spielfeld
  /// </summary>
  public class SokoField
  {
    /// <summary>
    /// Breite des Spielfeldes in Spalten
    /// </summary>
    public readonly int width;
    /// <summary>
    /// Höhe des Spielfeldes in Zeilen
    /// </summary>
    public readonly int height;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txtField">komplettes Spielfeld im Textformat</param>
    public SokoField(string txtField)
    {
      if (string.IsNullOrWhiteSpace(txtField)) throw new SokoFieldException("empty field");

      // --- Spielfeld einlesen ---
      var lines = txtField.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
      width = lines.Max(line => line.Length);
      height = lines.Length;
      if (width * height < 3) throw new SokoFieldException("invalid field-size");

      var field = new char[width * height];
      for (int y = 0; y < height; y++)
      {
        string line = lines[y]; // Zeile abfragen
        for (int x = 0; x < width; x++)
        {
          char c = x < line.Length ? line[x] : ' '; // Zeichen in der Zeile Abfragen (außerhalb der Zeile = Leerzeichen)
          switch (c)
          {
            case '@': // Spieler
            case '+': // Spieler auf einem Zielfeld
            case '$': // verschiebare Kiste
            case '.': // Zielfeld
            case '*': // Kiste auf einem Zielfeld
            case '#': // Mauer
            case ' ': // leeres Spielfeld
            field[x + y * width] = c; break; // den Wert übertragen

            default: c = ' '; goto case ' '; // bei unbekannten Zeichen ein leeres Spielfeld verwenden
          }
        }
      }

      // --- Spielfeld trimmen (sofern möglich) ---
      int cutLeft = -1;
      for (int x = width - 1; x >= 0; x--)
      {
        for (int y = 0; y < height; y++)
        {
          if (field[x + y * width] != ' ') cutLeft = x; // weitere linke Spalte mit Inhalt gefunden -> maximal erst ab dort abschneiden
        }
      }

      int cutRight = -1;
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          if (field[x + y * width] != ' ') cutRight = width - x - 1; // weitere rechte Spalte mit Inhalt gefunden -> maximal erst ab dort abschneiden
        }
      }

      int cutTop = -1;
      for (int y = height - 1; y >= 0; y--)
      {
        for (int x = 0; x < width; x++)
        {
          if (field[x + y * width] != ' ') cutTop = y; // weitere obere Zeile mit Inhalt gefunden -> maximal erst ab dort abschneiden
        }
      }

      int cutBottom = -1;
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          if (field[x + y * width] != ' ') cutBottom = height - y - 1; // weitere untere Zeile mit Inhalt gefunden -> maximal erst ab dort abschneiden
        }
      }

      if (cutLeft < 0 || cutRight < 0 || cutTop < 0 || cutBottom < 0) throw new SokoFieldException("empty field");
    }
  }
}
