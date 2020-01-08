#region # using *.*
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum schnellen Prüfen eines Spielfeldes (siehe old-solver: SokowahnRaum)
  /// </summary>
  public class SokoFieldQuickScan : ISokoField
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



    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txtField">komplettes Spielfeld im Textformat</param>
    public SokoFieldQuickScan(string txtField)
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
      int playerPos = -1;
      for (int i = 0; i < this.field.Length; i++)
      {
        if (this.field[i] == '@' || this.field[i] == '+') // Feld mit Spieler gefunden?
        {
          if (playerPos >= 0) throw new SokoFieldException("duplicate player found");
          playerPos = i;
        }
      }
      #endregion

      //#region # // --- Ziele und Kisten zählen und vergleichen ---
      //goalPositions = field.Select((c, i) => new { c, i }).Where(x => x.c == '.' || x.c == '*' || x.c == '+').Select(x => x.i).ToArray(); // Zielfelder: leer, mit Kiste, mit Spieler
      //boxPositions = field.Select((c, i) => new { c, i }).Where(x => x.c == '$' || x.c == '*').Select(x => x.i).ToArray(); // Kisten: nur Kiste, auf einem Zielfeld
      //if (boxPositions.Length == 0) throw new SokoFieldException("no boxes found");
      //if (goalPositions.Length == 0) throw new SokoFieldException("no goals found");
      //if (boxPositions.Length < goalPositions.Length) throw new SokoFieldException("less boxes than goals (" + boxPositions.Length + " < " + goalPositions.Length + ")");
      //if (boxPositions.Length > goalPositions.Length) throw new SokoFieldException("more boxes than goals (" + boxPositions.Length + " > " + goalPositions.Length + ")");
      //#endregion
    }
    #endregion

    #region # // --- ISokoField ---
    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// gibt die Höhe des Spielfeldes zurück
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// gibt die aktuelle Spielerposition zurück (pos: x + y * Width)
    /// </summary>
    public int PlayerPos { get; private set; }

    /// <summary>
    /// gibt den Inhalt des Spielfeldes an einer bestimmten Position zurück
    /// </summary>
    /// <param name="pos">Position des Spielfeldes, welches abgefragt werden soll (pos: x + y * Width)</param>
    /// <returns>Inhalt des Spielfeldes</returns>
    public char GetField(int pos)
    {
      return '\0';
    }

    /// <summary>
    /// lässt den Spieler (ungeprüft) einen Spielzug durchführen
    /// </summary>
    /// <param name="move">Spielzug, welcher durchgeführt werden soll</param>
    public void Move(MoveType move)
    {
    }
    #endregion
  }
}
