#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public readonly int[] goalPosis;
    /// <summary>
    /// merkt sich die Positionen der Kisten
    /// </summary>
    public readonly int[] boxPosis;
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
          field[x + y * width] = SokoFieldHelper.FilterChar(x < line.Length ? line[x] : ' '); // Zeichen in der Zeile Abfragen (außerhalb der Zeile = Leerzeichen)
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
      for (int i = 0; i < this.field.Length; i++)
      {
        if (this.field[i] == '@' || this.field[i] == '+') // Feld mit Spieler gefunden?
        {
          if (playerPos >= 0) throw new SokoFieldException("duplicate player found");
          playerPos = i;
        }
      }
      #endregion

      #region # // --- Ziele und Kisten zählen und vergleichen ---
      goalPosis = field.Select((c, i) => new { c, i }).Where(x => x.c == '.' || x.c == '*' || x.c == '+').Select(x => x.i).ToArray(); // Zielfelder: leer, mit Kiste, mit Spieler
      boxPosis = field.Select((c, i) => new { c, i }).Where(x => x.c == '$' || x.c == '*').Select(x => x.i).ToArray(); // Kisten: nur Kiste, auf einem Zielfeld
      if (boxPosis.Length == 0) throw new SokoFieldException("no boxes found");
      if (goalPosis.Length == 0) throw new SokoFieldException("no goals found");
      if (boxPosis.Length < goalPosis.Length) throw new SokoFieldException("less boxes than goals (" + boxPosis.Length + " < " + goalPosis.Length + ")");
      if (boxPosis.Length > goalPosis.Length) throw new SokoFieldException("more boxes than goals (" + boxPosis.Length + " > " + goalPosis.Length + ")");
      #endregion
    }
    #endregion

    #region # // --- ISokoField ---
    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    public int Width { get { return width; } }

    /// <summary>
    /// gibt die Höhe des Spielfeldes zurück
    /// </summary>
    public int Height { get { return height; } }

    /// <summary>
    /// gibt die aktuelle Spielerposition zurück (pos: x + y * Width)
    /// </summary>
    public int PlayerPos { get { return playerPos; } }

    /// <summary>
    /// gibt den Inhalt des Spielfeldes an einer bestimmten Position zurück
    /// </summary>
    /// <param name="pos">Position des Spielfeldes, welches abgefragt werden soll (pos: x + y * Width)</param>
    /// <returns>Inhalt des Spielfeldes</returns>
    public char GetFieldChar(int pos)
    {
      return field[pos];
    }

    /// <summary>
    /// lässt den Spieler (ungeprüft) einen Spielzug durchführen
    /// </summary>
    /// <param name="move">Spielzug, welcher durchgeführt werden soll</param>
    public void Move(MoveType move)
    {
      // --- Spieler an der alten Position entfernen ---
      Debug.Assert(field[playerPos] == '@' || field[playerPos] == '+');
      field[playerPos] = field[playerPos] == '@' ? ' ' : '.';

      switch (move)
      {
        // --- normales laufen auf leere Felder ---

        case MoveType.Left:
        {
          Debug.Assert(playerPos % width > 0);
          playerPos--;
        } break;

        case MoveType.Right:
        {
          Debug.Assert(playerPos % width < width - 1);
          playerPos++;
        } break;

        case MoveType.Up:
        {
          Debug.Assert(playerPos > width - 1);
          playerPos -= width;
        } break;

        case MoveType.Down:
        {
          Debug.Assert(playerPos / width < height - 1);
          playerPos += width;
        } break;


        // --- mit Kisten schieben ---

        case MoveType.LeftPush:
        {
          Debug.Assert(playerPos % width > 1);
          playerPos--;

          Debug.Assert(field[playerPos] == '$' || field[playerPos] == '*');
          field[playerPos] = field[playerPos] == '$' ? ' ' : '.';

          Debug.Assert(field[playerPos - 1] == ' ' || field[playerPos - 1] == '.');
          field[playerPos - 1] = field[playerPos - 1] == ' ' ? '$' : '*';
        } break;

        case MoveType.RightPush:
        {
          Debug.Assert(playerPos % width < width - 2);
          playerPos++;

          Debug.Assert(field[playerPos] == '$' || field[playerPos] == '*');
          field[playerPos] = field[playerPos] == '$' ? ' ' : '.';

          Debug.Assert(field[playerPos + 1] == ' ' || field[playerPos + 1] == '.');
          field[playerPos + 1] = field[playerPos + 1] == ' ' ? '$' : '*';
        } break;

        case MoveType.UpPush:
        {
          Debug.Assert(playerPos > width - 1);
          playerPos -= width;

          Debug.Assert(field[playerPos] == '$' || field[playerPos] == '*');
          field[playerPos] = field[playerPos] == '$' ? ' ' : '.';

          Debug.Assert(field[playerPos - width] == ' ' || field[playerPos - width] == '.');
          field[playerPos - width] = field[playerPos - width] == ' ' ? '$' : '*';
        } break;

        case MoveType.DownPush:
        {
          Debug.Assert(playerPos / width < height - 2);
          playerPos += width;

          Debug.Assert(field[playerPos] == '$' || field[playerPos] == '*');
          field[playerPos] = field[playerPos] == '$' ? ' ' : '.';

          Debug.Assert(field[playerPos + width] == ' ' || field[playerPos + width] == '.');
          field[playerPos + width] = field[playerPos + width] == ' ' ? '$' : '*';
        } break;

        default: throw new Exception("unknown Move-Type: " + move);
      }

      // --- Spieler an die neue Position setzen ---
      Debug.Assert(field[playerPos] == ' ' || field[playerPos] == '.');
      field[playerPos] = field[playerPos] == ' ' ? '@' : '+';
    }
    #endregion
  }
}
