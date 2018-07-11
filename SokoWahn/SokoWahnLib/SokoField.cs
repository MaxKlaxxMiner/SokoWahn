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
  public class SokoField : ISokoField
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
    /// die Daten des Spielfeldes (Größe: width * height)
    /// </summary>
    public readonly char[] field;
    /// <summary>
    /// merkt sich die Positionen der Ziele
    /// </summary>
    public readonly int[] targetPositions;
    /// <summary>
    /// merkt sich die Positionen der Kisten
    /// </summary>
    public readonly int[] boxPositions;
    /// <summary>
    /// die aktuelle Spielerposition
    /// </summary>
    public int playerPos;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txtField">komplettes Spielfeld im Textformat</param>
    public SokoField(string txtField)
    {
      if (string.IsNullOrWhiteSpace(txtField)) throw new SokoFieldException("empty field");

      #region # // --- Spielfeld einlesen ---
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
      #endregion

      #region # // --- Spielfeld trimmen (sofern möglich) ---
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

      if (cutLeft + cutRight + cutTop + cutBottom > 0) // Trim durchführen (sofern notwendig)
      {
        int oldWidth = width;
        width -= cutLeft;
        width -= cutRight;
        height -= cutTop;
        height -= cutBottom;
        this.field = new char[width * height]; // Neues kleineres Spielfeld erstellen
        // Spielfeld Inhalt kopieren
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            this.field[x + y * width] = field[cutLeft + x + (cutTop + y) * oldWidth];
          }
        }
        // End-Größe erneut prüfen
        if (width * height < 3) throw new SokoFieldException("invalid field-size");
      }
      else
      {
        this.field = field; // Spielfeld würde nicht geändert und kann direkt verwendet werden
      }
      #endregion

      #region # // --- Spieler suchen ---
      playerPos = -1;
      for (int i = 0; i < field.Length; i++)
      {
        if (field[i] == '@' || field[i] == '+') // Feld mit Spieler gefunden?
        {
          if (playerPos >= 0) throw new SokoFieldException("duplicate player found");
          playerPos = i;
        }
      }
      #endregion

      #region # // --- Ziele und Kisten zählen und vergleichen ---
      targetPositions = field.Select((c, i) => new { c, i }).Where(x => x.c == '.' || x.c == '*' || x.c == '+').Select(x => x.i).ToArray(); // Zielfelder: leer, mit Kiste, mit Spieler
      boxPositions = field.Select((c, i) => new { c, i }).Where(x => x.c == '$' || x.c == '*').Select(x => x.i).ToArray(); // Kisten: nur Kiste, auf einem Zielfeld
      if (boxPositions.Length == 0) throw new SokoFieldException("no boxes found");
      if (targetPositions.Length == 0) throw new SokoFieldException("no targets found");
      if (boxPositions.Length < targetPositions.Length) throw new SokoFieldException("less boxes than targets (" + boxPositions.Length + " < " + targetPositions.Length + ")");
      if (boxPositions.Length > targetPositions.Length) throw new SokoFieldException("more boxes than targets (" + boxPositions.Length + " > " + targetPositions.Length + ")");
      #endregion
    }
    #endregion

    #region # // --- ISokoField ---
    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    public int Width { get { return width; } }
    #endregion
  }
}
