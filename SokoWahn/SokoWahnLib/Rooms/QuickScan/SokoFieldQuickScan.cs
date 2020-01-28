#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum schnellen Prüfen eines Spielfeldes (siehe old-solver: SokowahnRaum)
  /// </summary>
  public sealed class SokoFieldQuickScan : ISokoField
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
    public readonly char[] fieldChars;

    #region # // --- statische Werte (welche sich nicht ändern) ---
    /// <summary>
    /// merkt sich die Anzahl der begehbaren Bereiche
    /// </summary>
    readonly int roomCount;

    /// <summary>
    /// wandelt eine konventionelle Feld-Position in eine Room-Position um
    /// </summary>
    readonly int[] fieldToRoom;

    /// <summary>
    /// wandelt eine Room-Position in eine Feld-Position um
    /// </summary>
    readonly int[] roomToField;

    /// <summary>
    /// zeigt auf das linke benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] roomLeft;

    /// <summary>
    /// zeigt auf das rechts benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] roomRight;

    /// <summary>
    /// zeigt auf das obere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] roomUp;

    /// <summary>
    /// zeigt auf das untere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] roomDown;

    /// <summary>
    /// Anzahl der Kisten, welche auf dem Spielfeld liegen (kann bei der Blocker-Suche niedriger sein als die eigentliche Ziele-Anzahl)
    /// </summary>
    int boxesCount;
    #endregion

    #region # // --- dynamische Werte ---
    /// <summary>
    /// aktuelle Spielerposition im SokowahnRaum
    /// </summary>
    int roomPlayerPos;

    /// <summary>
    /// gibt bei den begehbaren Raumbereichen an, welche Kiste sich dort befindet (Wert = kistenAnzahl, keine Kiste steht auf dem Feld)
    /// </summary>
    readonly int[] roomToBoxes;

    /// <summary>
    /// enthält die Kistenpositionen (Wert = raumAnzahl, Kiste steht nirgendwo, ist also nicht vorhanden)
    /// </summary>
    int[] boxesToRoom;

    /// <summary>
    /// merkt sich die aktuelle Zugtiefe
    /// </summary>
    int playerCalcDepth;
    #endregion

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
        fieldChars = new char[width * height]; // Neues kleineres Spielfeld erstellen
        // Spielfeld Inhalt kopieren
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            fieldChars[x + y * width] = field[cutLeft + x + (cutTop + y) * oldWidth];
          }
        }
        // End-Größe erneut prüfen
        if (width * height < 3) throw new SokoFieldException("invalid field-size");
      }
      else
      {
        fieldChars = field; // Spielfeld würde nicht geändert und kann direkt verwendet werden
      }
      #endregion

      #region # // --- Spieler suchen ---
      int playerPos = -1;
      for (int i = 0; i < fieldChars.Length; i++)
      {
        if (fieldChars[i] == '@' || fieldChars[i] == '+') // Feld mit Spieler gefunden?
        {
          if (playerPos >= 0) throw new SokoFieldException("duplicate player found");
          playerPos = i;
        }
      }
      #endregion

      #region # // --- Raum-Logik ---
      var playerRoom = FieldRoomScan(fieldChars, width);

      int roomCount = this.roomCount = playerRoom.Count(x => x);

      roomToField = Enumerable.Range(0, playerRoom.Length).Where(i => playerRoom[i]).ToArray();
      fieldToRoom = Enumerable.Range(0, fieldChars.Length).Select(i => playerRoom[i] ? roomToField.ToList().IndexOf(i) : -1).ToArray();

      roomLeft = roomToField.Select(i => playerRoom[i - 1] ? roomToField.ToList().IndexOf(i - 1) : roomCount).ToArray();
      roomRight = roomToField.Select(i => playerRoom[i + 1] ? roomToField.ToList().IndexOf(i + 1) : roomCount).ToArray();
      roomUp = roomToField.Select(i => playerRoom[i - width] ? roomToField.ToList().IndexOf(i - width) : roomCount).ToArray();
      roomDown = roomToField.Select(i => playerRoom[i + width] ? roomToField.ToList().IndexOf(i + width) : roomCount).ToArray();

      boxesToRoom = Enumerable.Range(0, roomCount).Where(i => playerRoom[roomToField[i]] && (fieldChars[roomToField[i]] == '$' || fieldChars[roomToField[i]] == '*')).ToArray();
      int boxesCount = this.boxesCount = boxesToRoom.Length;
      int counter = 0;
      roomToBoxes = roomToField.Select(i => (fieldChars[i] == '$' || fieldChars[i] == '*') ? counter++ : boxesCount).ToArray();
      Array.Resize(ref roomToBoxes, roomCount + 1);
      roomToBoxes[roomCount] = boxesCount;

      roomPlayerPos = Enumerable.Range(0, roomCount).First(i => fieldChars[roomToField[i]] == '@' || fieldChars[roomToField[i]] == '+');
      playerCalcDepth = 0;

      tmpCheckRoomPosis = new int[roomCount];
      tmpCheckRoomDepth = new int[roomCount];
      tmpCheckRoomReady = new bool[roomCount + 1];
      tmpCheckRoomReady[roomCount] = true; // Ende-Feld schon auf fertig setzen
      #endregion
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Basisdaten anhand eines vorhanden Raumes nutzen</param>
    public SokoFieldQuickScan(SokoFieldQuickScan field)
    {
      fieldChars = field.fieldChars;
      width = field.width;
      height = field.height;
      roomCount = field.roomCount;
      fieldToRoom = field.fieldToRoom;
      roomToField = field.roomToField;
      roomLeft = field.roomLeft;
      roomRight = field.roomRight;
      roomUp = field.roomUp;
      roomDown = field.roomDown;

      roomPlayerPos = field.roomPlayerPos;
      playerCalcDepth = field.playerCalcDepth;
      roomToBoxes = field.roomToBoxes.ToArray(); // Kopie erstellen
      boxesCount = field.boxesCount;
      boxesToRoom = field.boxesToRoom.ToArray(); // Kopie erstellen

      tmpCheckRoomPosis = new int[roomCount];
      tmpCheckRoomDepth = new int[roomCount];
      tmpCheckRoomReady = new bool[roomCount + 1];
      tmpCheckRoomReady[roomCount] = true; // Ende-Feld schon auf fertig setzen
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">bestehende Spielfeld, welches verwendet werden soll</param>
    public SokoFieldQuickScan(ISokoField field) : this(Enumerable.Range(0, field.Width * field.Height).Select(field.GetFieldChar).ToArray(), field.Width) { }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fieldChars">Daten des Spielfeldes</param>
    /// <param name="fieldWidth">Breite des Spielfeldes</param>
    public SokoFieldQuickScan(char[] fieldChars, int fieldWidth)
    {
      width = fieldWidth;
      height = fieldChars.Length / fieldWidth;
      this.fieldChars = fieldChars;

      var playerRoom = FieldRoomScan(fieldChars, fieldWidth);

      int roomCount = this.roomCount = playerRoom.Count(x => x);

      roomToField = Enumerable.Range(0, playerRoom.Length).Where(i => playerRoom[i]).ToArray();
      fieldToRoom = Enumerable.Range(0, fieldChars.Length).Select(i => playerRoom[i] ? roomToField.ToList().IndexOf(i) : -1).ToArray();

      roomLeft = roomToField.Select(i => playerRoom[i - 1] ? roomToField.ToList().IndexOf(i - 1) : roomCount).ToArray();
      roomRight = roomToField.Select(i => playerRoom[i + 1] ? roomToField.ToList().IndexOf(i + 1) : roomCount).ToArray();
      roomUp = roomToField.Select(i => playerRoom[i - fieldWidth] ? roomToField.ToList().IndexOf(i - fieldWidth) : roomCount).ToArray();
      roomDown = roomToField.Select(i => playerRoom[i + fieldWidth] ? roomToField.ToList().IndexOf(i + fieldWidth) : roomCount).ToArray();

      boxesToRoom = Enumerable.Range(0, roomCount).Where(i => playerRoom[roomToField[i]] && (fieldChars[roomToField[i]] == '$' || fieldChars[roomToField[i]] == '*')).ToArray();
      int boxesCount = this.boxesCount = boxesToRoom.Length;
      int counter = 0;
      roomToBoxes = roomToField.Select(i => (fieldChars[i] == '$' || fieldChars[i] == '*') ? counter++ : boxesCount).ToArray();
      Array.Resize(ref roomToBoxes, roomCount + 1);
      roomToBoxes[roomCount] = boxesCount;

      roomPlayerPos = Enumerable.Range(0, roomCount).First(i => fieldChars[roomToField[i]] == '@' || fieldChars[roomToField[i]] == '+');
      playerCalcDepth = 0;

      tmpCheckRoomPosis = new int[roomCount];
      tmpCheckRoomDepth = new int[roomCount];
      tmpCheckRoomReady = new bool[roomCount + 1];
      tmpCheckRoomReady[roomCount] = true; // Ende-Feld schon auf fertig setzen
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
    public int PlayerPos { get { return roomToField[roomPlayerPos]; } }

    /// <summary>
    /// gibt den Inhalt des Spielfeldes an einer bestimmten Position zurück
    /// </summary>
    /// <param name="pos">Position des Spielfeldes, welches abgefragt werden soll (pos: x + y * Width)</param>
    /// <returns>Inhalt des Spielfeldes</returns>
    public char GetFieldChar(int pos)
    {
      if (pos < 0 || pos >= fieldChars.Length) throw new ArgumentOutOfRangeException("pos");
      int p = fieldToRoom[pos];
      if (p < 0) return EmptyFieldChar(fieldChars[pos]);
      if (roomToBoxes[p] < boxesCount) return EmptyFieldChar(fieldChars[pos]) == '.' ? '*' : '$';
      if (p == roomPlayerPos) return EmptyFieldChar(fieldChars[pos]) == '.' ? '+' : '@';
      return EmptyFieldChar(fieldChars[pos]);
    }

    /// <summary>
    /// lässt den Spieler (ungeprüft) einen Spielzug durchführen
    /// </summary>
    /// <param name="move">Spielzug, welcher durchgeführt werden soll</param>
    public void Move(MoveType move)
    {
      throw new NotSupportedException();
    }
    #endregion

    #region # // --- temporäre Werte ---
    /// <summary>
    /// merkt sich temporär, welche Positionen geprüft wurden bzw. noch geprüft werden müssen
    /// </summary>
    readonly int[] tmpCheckRoomPosis;

    /// <summary>
    /// merkt sich temporär, welche Zugtiefen erreicht wurden
    /// </summary>
    readonly int[] tmpCheckRoomDepth;

    /// <summary>
    /// merkt sich temporär, welche Felder im Raum bereits abgelaufen wurden
    /// </summary>
    readonly bool[] tmpCheckRoomReady;
    #endregion

    #region # // --- public Properties ---
    /// <summary>
    /// berechnet den CRC-Schlüssel der aktuellen Stellung
    /// </summary>
    public ulong Crc
    {
      get
      {
        ulong result = (Crc64.Start ^ (ulong)roomPlayerPos) * 0x100000001b3;

        for (int i = 0; i < boxesCount; i++)
        {
          result = (result ^ (ulong)boxesToRoom[i]) * 0x100000001b3;
        }

        return result;
      }
    }

    /// <summary>
    /// gibt die Anzahl der begehbaren Raum-Felder an
    /// </summary>
    public int RoomCount
    {
      get
      {
        return roomCount;
      }
    }

    /// <summary>
    /// gibt die Anzahl der Kisten zurück oder setzt diese (einmaliges Setzen nur im Blocker-Modus erlaubt, die aktuelle Stellung wird dabei unbrauchbar)
    /// </summary>
    public int BoxesCount
    {
      get
      {
        return boxesCount;
      }
      set
      {
        boxesCount = value;

        // alle Kisten entfernen
        for (int i = 0; i < roomCount; i++) roomToBoxes[i] = boxesCount;
        boxesToRoom = Enumerable.Range(0, boxesCount).Select(i => i).ToArray();

        for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = i; // Kisten neu sinnlos auf das Feld setzen
      }
    }

    /// <summary>
    /// gibt die aktuelle Zugtiefe zurück
    /// </summary>
    public int CalcDepth
    {
      get
      {
        if (playerCalcDepth >= 0 && playerCalcDepth < 30000) return playerCalcDepth;
        if (playerCalcDepth >= 30000 & playerCalcDepth < 60000) return playerCalcDepth - 60000;
        return 0;
      }

      set
      {
        playerCalcDepth = value;
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld zurück
    /// </summary>
    public char[] FieldData
    {
      get
      {
        return fieldChars.ToArray();
      }
    }

    /// <summary>
    /// wandelt ein Feld-Zeichen in ein leeres Feld um (beachtet Zielfelder etc.)
    /// </summary>
    /// <param name="c">Zeichen, welches umgewandelt werden soll</param>
    /// <returns>fertig umgewandeltes Zeichen</returns>
    static char EmptyFieldChar(char c)
    {
      switch (c)
      {
        case '#': return '#';
        case ' ': return ' ';
        case '.': return '.';
        case '$': return ' ';
        case '*': return '.';
        case '@': return ' ';
        case '+': return '.';
        default: throw new Exception("ungültiges Zeichen: " + c);
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld zurück, jedoch ohne Spieler und ohne Kisten
    /// </summary>
    public char[] FieldDataEmpty
    {
      get
      {
        return fieldChars.Select(EmptyFieldChar).ToArray();
      }
    }

    /// <summary>
    /// gibt das Spielfeld als lesbaren String aus
    /// </summary>
    /// <returns>lesbarer String</returns>
    public override string ToString()
    {
      var fieldEmpty = FieldDataEmpty;

      var roomToBoxes = this.roomToBoxes.ToArray();
      int boxesCount = this.boxesCount;
      int roomPlayerPos = this.roomPlayerPos;

      return string.Concat(Enumerable.Range(0, height).Select(y => new string(Enumerable.Range(0, width).Select(x =>
      {
        int p = fieldToRoom[x + y * width];
        if (p < 0) return fieldEmpty[x + y * width];
        if (roomToBoxes[p] < boxesCount) return fieldEmpty[x + y * width] == '.' ? '*' : '$';
        if (p == roomPlayerPos) return fieldEmpty[x + y * width] == '.' ? '+' : '@';
        return fieldEmpty[x + y * width];
      }).ToArray()) + "\r\n")).TrimEnd() + (CalcDepth != 0 ? " - Tiefe: " + CalcDepth.ToString("#,##0") : "")
        //   + " - Crc: " + Crc 
      + "\r\n";
    }
    #endregion

    #region # // --- public Methoden ---
    #region # public SokowahnPosition GetState() // gibt die aktuelle Stellung zurück
    /// <summary>
    /// gibt die aktuelle Stellung zurück
    /// </summary>
    /// <returns>aktuelle Stellung</returns>
    public SokowahnState GetState()
    {
      return new SokowahnState { roomPlayerPos = roomPlayerPos, boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, calcDepth = playerCalcDepth };
    }
    #endregion

    #region # public void LoadState(*[] src, int off, int spielerZugTiefe) // lädt eine bestimmte Stellung
    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="src">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="depth">Zugtiefe der Stellung</param>
    public void LoadState(byte[] src, int off, int depth)
    {
      // neue Spielerposition setzen
      roomPlayerPos = src[off++];
      playerCalcDepth = depth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = src[off + i];
        boxesToRoom[i] = p;
        roomToBoxes[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="src">Array mit entsprechenden Daten</param>
    /// <param name="depth">Zugtiefe der Stellung</param>
    public void LoadState(byte[] src, int depth)
    {
      // neue Spielerposition setzen
      roomPlayerPos = src[0];
      playerCalcDepth = depth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = src[i + 1];
        boxesToRoom[i] = p;
        roomToBoxes[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="src">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="depth">Zugtiefe der Stellung</param>
    public void LoadState(ushort[] src, int off, int depth)
    {
      // neue Spielerposition setzen
      roomPlayerPos = src[off++];
      playerCalcDepth = depth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = src[off + i];
        boxesToRoom[i] = p;
        roomToBoxes[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="src">Array mit entsprechenden Daten</param>
    /// <param name="depth">Zugtiefe der Stellung</param>
    public void LoadState(ushort[] src, int depth)
    {
      // neue Spielerposition setzen
      roomPlayerPos = src[0];
      playerCalcDepth = depth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = src[i + 1];
        boxesToRoom[i] = p;
        roomToBoxes[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="state">Stellung, welche geladen werden soll</param>
    public void LoadState(SokowahnState state)
    {
      roomPlayerPos = state.roomPlayerPos;
      playerCalcDepth = state.calcDepth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i] = state.boxesToRoom[i]] = i;
    }

    /// <summary>
    /// Sondervariante für das Blocker-System (ohne setzen der Spielerposition)
    /// </summary>
    /// <param name="boxesIndex">Kisten-Index für sammlerKistenRaum (Länge = sammlerKistenAnzahl)</param>
    /// <param name="boxesRoom">Raumpositionen der einzelnen Kisten (Länge = basisKistenAnzahl)</param>
    public void LoadState(int[] boxesIndex, int[] boxesRoom)
    {
      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i] = boxesRoom[boxesIndex[i]]] = i;
    }
    #endregion
    #region # public void SaveState(*[] dst, int off) // speichert eine bestimmte Stellung
    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="dst">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SaveState(byte[] dst, int off)
    {
      dst[off++] = (byte)roomPlayerPos;
      for (int i = 0; i < boxesCount; i++) dst[off++] = (byte)boxesToRoom[i];
    }

    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="dst">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SaveState(ushort[] dst, int off)
    {
      dst[off++] = (ushort)roomPlayerPos;
      for (int i = 0; i < boxesCount; i++) dst[off++] = (ushort)boxesToRoom[i];
    }
    #endregion

    #region # public string GetSteps(SokoFieldQuickScan next) // berechnet die Lauf-Schritte bis zur nächsten Kisten-Bewegung
    /// <summary>
    /// berechnet die Lauf-Schritte bis zur nächsten Kisten-Bewegung
    /// </summary>
    /// <param name="next">nachfolgendes Spielfeld</param>
    /// <returns>Schritte um das Spielfeld zu erreichen</returns>
    public string GetSteps(SokoFieldQuickScan next)
    {
      string findSteps = "";
      int playerGoal = next.roomPlayerPos;
      int checkRoomFrom = 0;
      int checkRoomTo = 0;

      Array.Clear(tmpCheckRoomReady, 0, roomCount);

      // erste Spielerposition hinzufügen
      tmpCheckRoomReady[roomPlayerPos] = true;
      tmpCheckRoomPosis[checkRoomTo] = roomPlayerPos;
      tmpCheckRoomDepth[checkRoomTo] = playerCalcDepth;
      checkRoomTo++;

      Func<int, int, int> preFields = (pp, tt) =>
      {
        if (roomLeft[pp] < roomCount)
        {
          int index = -1;
          for (int i = 0; i < checkRoomTo; i++) if (tmpCheckRoomPosis[i] == roomLeft[pp]) index = i;
          if (index >= 0 && tmpCheckRoomDepth[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "r" + findSteps;
              return roomLeft[pp];
            }
          }
        }

        if (roomRight[pp] < roomCount)
        {
          int index = -1;
          for (int i = 0; i < checkRoomTo; i++) if (tmpCheckRoomPosis[i] == roomRight[pp]) index = i;
          if (index >= 0 && tmpCheckRoomDepth[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "l" + findSteps;
              return roomRight[pp];
            }
          }
        }

        if (roomUp[pp] < roomCount)
        {
          int index = -1;
          for (int i = 0; i < checkRoomTo; i++) if (tmpCheckRoomPosis[i] == roomUp[pp]) index = i;
          if (index >= 0 && tmpCheckRoomDepth[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "d" + findSteps;
              return roomUp[pp];
            }
          }
        }

        if (roomDown[pp] < roomCount)
        {
          int index = -1;
          for (int i = 0; i < checkRoomTo; i++) if (tmpCheckRoomPosis[i] == roomDown[pp]) index = i;
          if (index >= 0 && tmpCheckRoomDepth[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "u" + findSteps;
              return roomDown[pp];
            }
          }
        }

        return tmpCheckRoomPosis[0];
      };

      // alle möglichen Spielerposition berechnen
      while (checkRoomFrom < checkRoomTo)
      {
        roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];
        int pDepth = tmpCheckRoomDepth[checkRoomFrom] + 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpCheckRoomReady[p = roomLeft[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomLeft[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // linke Kiste weiter nach links schieben
              roomPlayerPos = p;                                                                    // Spieler nach links bewegen

              if (roomPlayerPos == playerGoal && Enumerable.Range(0, boxesCount).All(i => boxesToRoom[i] == next.boxesToRoom[i]))
              {
                int pp = roomRight[p];
                int tt = pDepth - 1;
                findSteps = "L";
                while (pp != tmpCheckRoomPosis[0])
                {
                  tt--;
                  pp = preFields(pp, tt);
                }
              }

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach rechts bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpCheckRoomReady[p = roomRight[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomRight[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // rechte Kiste weiter nach rechts schieben
              roomPlayerPos = p;                                                                    // Spieler nach rechts bewegen

              if (roomPlayerPos == playerGoal && Enumerable.Range(0, boxesCount).All(i => boxesToRoom[i] == next.boxesToRoom[i]))
              {
                int pp = roomLeft[p];
                int tt = pDepth - 1;
                findSteps = "R";
                while (pp != tmpCheckRoomPosis[0])
                {
                  tt--;
                  pp = preFields(pp, tt);
                }
              }

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach links bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpCheckRoomReady[p = roomUp[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomUp[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // obere Kiste weiter nach oben schieben
              roomPlayerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (roomToBoxes[p2] > 0 && boxesToRoom[roomToBoxes[p2] - 1] > p2 && roomToBoxes[p2] < boxesCount)
              {
                int tmp = boxesToRoom[roomToBoxes[p2] - 1];
                boxesToRoom[roomToBoxes[p2]--] = tmp;
                boxesToRoom[roomToBoxes[tmp]++] = p2;
              }
              #endregion

              if (roomPlayerPos == playerGoal && Enumerable.Range(0, boxesCount).All(i => boxesToRoom[i] == next.boxesToRoom[i]))
              {
                int pp = roomDown[p];
                int tt = pDepth - 1;
                findSteps = "U";
                while (pp != tmpCheckRoomPosis[0])
                {
                  tt--;
                  pp = preFields(pp, tt);
                }
              }

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach unten bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (roomToBoxes[p] < boxesCount - 1 && boxesToRoom[roomToBoxes[p] + 1] < p)
              {
                int tmp = boxesToRoom[roomToBoxes[p] + 1];
                boxesToRoom[roomToBoxes[p]++] = tmp;
                boxesToRoom[roomToBoxes[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpCheckRoomReady[p = roomDown[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomDown[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // untere Kiste weiter nach unten schieben
              roomPlayerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (roomToBoxes[p2] < boxesCount - 1 && boxesToRoom[roomToBoxes[p2] + 1] < p2)
              {
                int tmp = boxesToRoom[roomToBoxes[p2] + 1];
                boxesToRoom[roomToBoxes[p2]++] = tmp;
                boxesToRoom[roomToBoxes[tmp]--] = p2;
              }
              #endregion

              if (roomPlayerPos == playerGoal && Enumerable.Range(0, boxesCount).All(i => boxesToRoom[i] == next.boxesToRoom[i]))
              {
                int pp = roomUp[p];
                int tt = pDepth - 1;
                findSteps = "D";
                while (pp != tmpCheckRoomPosis[0])
                {
                  tt--;
                  pp = preFields(pp, tt);
                }
              }

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach oben bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (roomToBoxes[p] > 0 && boxesToRoom[roomToBoxes[p] - 1] > p && roomToBoxes[p] < boxesCount)
              {
                int tmp = boxesToRoom[roomToBoxes[p] - 1];
                boxesToRoom[roomToBoxes[p]--] = tmp;
                boxesToRoom[roomToBoxes[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        checkRoomFrom++;
      }

      roomPlayerPos = tmpCheckRoomPosis[0]; // alte Spielerposition wieder herstellen

      return findSteps;
    }
    #endregion

    #region # public IEnumerable<SokowahnStellung> GetVariants() // ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// <summary>
    /// ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// </summary>
    /// <returns>Enumerable der gefundenen Stellungen</returns>
    public IEnumerable<SokowahnState> GetVariants()
    {
      int checkRoomFrom = 0;
      int checkRoomTo = 0;

      Array.Clear(tmpCheckRoomReady, 0, roomCount);

      // erste Spielerposition hinzufügen
      tmpCheckRoomReady[roomPlayerPos] = true;
      tmpCheckRoomPosis[checkRoomTo] = roomPlayerPos;
      tmpCheckRoomDepth[checkRoomTo] = playerCalcDepth;
      checkRoomTo++;

      // alle möglichen Spielerposition berechnen
      while (checkRoomFrom < checkRoomTo)
      {
        roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];
        int pDepth = tmpCheckRoomDepth[checkRoomFrom] + 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpCheckRoomReady[p = roomLeft[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomLeft[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // linke Kiste weiter nach links schieben
              roomPlayerPos = p;                                                                    // Spieler nach links bewegen

              yield return new SokowahnState { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach rechts bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpCheckRoomReady[p = roomRight[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomRight[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // rechte Kiste weiter nach rechts schieben
              roomPlayerPos = p;                                                                    // Spieler nach rechts bewegen

              yield return new SokowahnState { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach links bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpCheckRoomReady[p = roomUp[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomUp[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // obere Kiste weiter nach oben schieben
              roomPlayerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (roomToBoxes[p2] > 0 && boxesToRoom[roomToBoxes[p2] - 1] > p2 && roomToBoxes[p2] < boxesCount)
              {
                int tmp = boxesToRoom[roomToBoxes[p2] - 1];
                boxesToRoom[roomToBoxes[p2]--] = tmp;
                boxesToRoom[roomToBoxes[tmp]++] = p2;
              }
              #endregion

              yield return new SokowahnState { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach unten bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (roomToBoxes[p] < boxesCount - 1 && boxesToRoom[roomToBoxes[p] + 1] < p)
              {
                int tmp = boxesToRoom[roomToBoxes[p] + 1];
                boxesToRoom[roomToBoxes[p]++] = tmp;
                boxesToRoom[roomToBoxes[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpCheckRoomReady[p = roomDown[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (roomToBoxes[p2 = roomDown[p]] == boxesCount && p2 < roomCount) // Feld hinter der Kiste frei?
            {
              boxesToRoom[roomToBoxes[p2] = roomToBoxes[p]] = p2; roomToBoxes[p] = boxesCount; // untere Kiste weiter nach unten schieben
              roomPlayerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (roomToBoxes[p2] < boxesCount - 1 && boxesToRoom[roomToBoxes[p2] + 1] < p2)
              {
                int tmp = boxesToRoom[roomToBoxes[p2] + 1];
                boxesToRoom[roomToBoxes[p2]++] = tmp;
                boxesToRoom[roomToBoxes[tmp]--] = p2;
              }
              #endregion

              yield return new SokowahnState { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

              roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];                                      // Spieler zurück nach oben bewegen
              boxesToRoom[roomToBoxes[p] = roomToBoxes[p2]] = p; roomToBoxes[p2] = boxesCount; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (roomToBoxes[p] > 0 && boxesToRoom[roomToBoxes[p] - 1] > p && roomToBoxes[p] < boxesCount)
              {
                int tmp = boxesToRoom[roomToBoxes[p] - 1];
                boxesToRoom[roomToBoxes[p]--] = tmp;
                boxesToRoom[roomToBoxes[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        checkRoomFrom++;
      }

      roomPlayerPos = tmpCheckRoomPosis[0]; // alte Spielerposition wieder herstellen
    }
    #endregion

    #region # public IEnumerable<SokowahnPosition> GetVariantsBackward() // ermittelt alle möglichen Zugvarianten, welche vor dieser Stellung existiert haben könnten und gibt deren Stellungen zurück
    /// <summary>
    /// ermittelt alle möglichen Zugvarianten, welche vor dieser Stellung existiert haben könnten und gibt deren Stellungen zurück
    /// </summary>
    /// <returns>alle möglichen Vorgänge-Zugvarianten</returns>
    public IEnumerable<SokowahnState> GetVariantsBackward()
    {
      int pMiddle = roomPlayerPos;
      int pLeft = roomLeft[pMiddle];
      int pRight = roomRight[pMiddle];
      int pUp = roomUp[pMiddle];
      int pDown = roomDown[pMiddle];

      #region # // --- Links-Vermutung: Kiste wurde das letztes mal nach links geschoben ---
      if (roomToBoxes[pLeft] < boxesCount && pRight < roomCount && roomToBoxes[pRight] == boxesCount)
      {
        roomPlayerPos = pRight; // Spieler zurück nach rechts bewegen
        boxesToRoom[roomToBoxes[pMiddle] = roomToBoxes[pLeft]] = pMiddle; roomToBoxes[pLeft] = boxesCount; // linke Kiste eins zurück nach rechts schieben

        foreach (var v in GetVariantsBackwardStep()) yield return v;

        boxesToRoom[roomToBoxes[pLeft] = roomToBoxes[pMiddle]] = pLeft; roomToBoxes[pMiddle] = boxesCount; // linke Kiste weiter nach links schieben
        roomPlayerPos = pMiddle; // Spieler nach links bewegen
      }
      #endregion

      #region # // --- Rechts-Vermutung: Kiste wurde das letztes mal nach rechts geschoben ---
      if (roomToBoxes[pRight] < boxesCount && pLeft < roomCount && roomToBoxes[pLeft] == boxesCount)
      {
        roomPlayerPos = pLeft; // Spieler zurück nach links bewegen
        boxesToRoom[roomToBoxes[pMiddle] = roomToBoxes[pRight]] = pMiddle; roomToBoxes[pRight] = boxesCount; // rechte Kiste eins zurück nach links schieben

        foreach (var v in GetVariantsBackwardStep()) yield return v;

        boxesToRoom[roomToBoxes[pRight] = roomToBoxes[pMiddle]] = pRight; roomToBoxes[pMiddle] = boxesCount; // rechte Kiste weiter nach rechts schieben
        roomPlayerPos = pMiddle; // Spieler nach rechts bewegen
      }
      #endregion

      #region # // --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
      if (roomToBoxes[pUp] < boxesCount && pDown < roomCount && roomToBoxes[pDown] == boxesCount)
      {
        roomPlayerPos = pDown; // Spieler zurück nach unten bewegen
        boxesToRoom[roomToBoxes[pMiddle] = roomToBoxes[pUp]] = pMiddle; roomToBoxes[pUp] = boxesCount; // obere Kiste eins zurück nach unten schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (roomToBoxes[pMiddle] < boxesCount - 1 && boxesToRoom[roomToBoxes[pMiddle] + 1] < pMiddle)
        {
          int tmp = boxesToRoom[roomToBoxes[pMiddle] + 1];
          boxesToRoom[roomToBoxes[pMiddle]++] = tmp;
          boxesToRoom[roomToBoxes[tmp]--] = pMiddle;
        }
        #endregion

        foreach (var v in GetVariantsBackwardStep()) yield return v;

        boxesToRoom[roomToBoxes[pUp] = roomToBoxes[pMiddle]] = pUp; roomToBoxes[pMiddle] = boxesCount; // obere Kiste weiter nach oben schieben
        roomPlayerPos = pMiddle; // Spieler nach oben bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (roomToBoxes[pUp] > 0 && boxesToRoom[roomToBoxes[pUp] - 1] > pUp && roomToBoxes[pUp] < boxesCount)
        {
          int tmp = boxesToRoom[roomToBoxes[pUp] - 1];
          boxesToRoom[roomToBoxes[pUp]--] = tmp;
          boxesToRoom[roomToBoxes[tmp]++] = pUp;
        }
        #endregion
      }
      #endregion

      #region # // --- Unten-Vermutung: Kiste wurde das letztes mal nach unten geschoben ---
      if (roomToBoxes[pDown] < boxesCount && pUp < roomCount && roomToBoxes[pUp] == boxesCount)
      {
        roomPlayerPos = pUp; // Spieler zurück nach oben bewegen
        boxesToRoom[roomToBoxes[pMiddle] = roomToBoxes[pDown]] = pMiddle; roomToBoxes[pDown] = boxesCount; // untere Kiste eins zurück nach oben schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (roomToBoxes[pMiddle] > 0 && boxesToRoom[roomToBoxes[pMiddle] - 1] > pMiddle && roomToBoxes[pMiddle] < boxesCount)
        {
          int tmp = boxesToRoom[roomToBoxes[pMiddle] - 1];
          boxesToRoom[roomToBoxes[pMiddle]--] = tmp;
          boxesToRoom[roomToBoxes[tmp]++] = pMiddle;
        }
        #endregion

        foreach (var v in GetVariantsBackwardStep()) yield return v;

        boxesToRoom[roomToBoxes[pDown] = roomToBoxes[pMiddle]] = pDown; roomToBoxes[pMiddle] = boxesCount; // untere Kiste weiter nach unten schieben
        roomPlayerPos = pMiddle; // Spieler nach unten bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (roomToBoxes[pDown] < boxesCount - 1 && boxesToRoom[roomToBoxes[pDown] + 1] < pDown)
        {
          int tmp = boxesToRoom[roomToBoxes[pDown] + 1];
          boxesToRoom[roomToBoxes[pDown]++] = tmp;
          boxesToRoom[roomToBoxes[tmp]--] = pDown;
        }
        #endregion
      }
      #endregion
    }
    #endregion
    #region # IEnumerable<SokowahnPosition> GetVariantsBackwardStep() // Hilfsmethode für GetVariantenRückwärts(), berechnet eine bestimmte Richtung
    /// <summary>
    /// Hilfsmethode für GetVariantenRückwärts(), berechnet eine bestimmte Richtung
    /// </summary>
    /// <returns>gefundene gültige Stellungen</returns>
    IEnumerable<SokowahnState> GetVariantsBackwardStep()
    {
      int checkRoomFrom = 0;
      int checkRoomTo = 0;

      Array.Clear(tmpCheckRoomReady, 0, roomCount);

      // erste Spielerposition hinzufügen
      tmpCheckRoomReady[roomPlayerPos] = true;
      tmpCheckRoomPosis[checkRoomTo] = roomPlayerPos;
      tmpCheckRoomDepth[checkRoomTo] = playerCalcDepth;
      checkRoomTo++;

      // alle möglichen Spielerposition berechnen
      while (checkRoomFrom < checkRoomTo)
      {
        roomPlayerPos = tmpCheckRoomPosis[checkRoomFrom];
        int pDepth = tmpCheckRoomDepth[checkRoomFrom] - 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpCheckRoomReady[p = roomLeft[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount)
          {
            if ((p2 = roomRight[roomPlayerPos]) < roomCount && roomToBoxes[p2] == boxesCount)
            {
              yield return new SokowahnState { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpCheckRoomReady[p = roomRight[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount)
          {
            if ((p2 = roomLeft[roomPlayerPos]) < roomCount && roomToBoxes[p2] == boxesCount)
            {
              yield return new SokowahnState { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpCheckRoomReady[p = roomUp[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount)
          {
            if ((p2 = roomDown[roomPlayerPos]) < roomCount && roomToBoxes[p2] == boxesCount)
            {
              yield return new SokowahnState { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpCheckRoomReady[p = roomDown[roomPlayerPos]])
        {
          if (roomToBoxes[p] < boxesCount)
          {
            if ((p2 = roomUp[roomPlayerPos]) < roomCount && roomToBoxes[p2] == boxesCount)
            {
              yield return new SokowahnState { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
            }
          }
          else
          {
            tmpCheckRoomReady[p] = true;
            tmpCheckRoomPosis[checkRoomTo] = p;
            tmpCheckRoomDepth[checkRoomTo] = pDepth;
            checkRoomTo++;
          }
        }
        #endregion

        checkRoomFrom++;
      }
    }
    #endregion

    #region # public IEnumerable<SokowahnPosition> GetVariantsBlockerGoals() // ermittelt alle Ziel-Varianten, wo der Spieler stehen kann
    /// <summary>
    /// ermittelt alle Ziel-Varianten, wo der Spieler stehen kann
    /// </summary>
    /// <returns>gefundene mögliche Stellungen</returns>
    public IEnumerable<SokowahnState> GetVariantsBlockerGoals()
    {
      for (int box = 0; box < boxesCount; box++)
      {
        int pBox = boxesToRoom[box];
        int pPlayer;

        if ((pPlayer = roomLeft[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetState();
        }

        if ((pPlayer = roomRight[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetState();
        }

        if ((pPlayer = roomUp[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetState();
        }

        if ((pPlayer = roomDown[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetState();
        }
      }
    }
    #endregion

    #region # // --- Debug-Methoden ---
    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="state">Stellung, welche ausgelesen werden soll</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(SokowahnState state)
    {
      var tmpData = new ushort[boxesCount + 1];
      state.SavePosition(tmpData, 0);
      return Debug(tmpData, 0, state.calcDepth);
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(byte[] data, int offset)
    {
      var tmp = new SokoFieldQuickScan(this);

      tmp.LoadState(data, offset, 0);

      return tmp.ToString();
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(ushort[] data, int offset)
    {
      var tmp = new SokoFieldQuickScan(this);

      tmp.LoadState(data, offset, 0);

      return tmp.ToString();
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="zugTiefe">erwartete Zugtiefe</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(byte[] data, int offset, int zugTiefe)
    {
      var tmp = new SokoFieldQuickScan(this);

      tmp.LoadState(data, offset, zugTiefe);

      return tmp.ToString();
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="zugTiefe">erwartete Zugtiefe</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(ushort[] data, int offset, int zugTiefe)
    {
      var tmp = new SokoFieldQuickScan(this);

      tmp.LoadState(data, offset, zugTiefe);

      return tmp.ToString();
    }
    #endregion
    #endregion

    #region # // --- SokowahnStaticTools ---
    /// <summary>
    /// ermittelt die Felder, wo der Spieler sich aufhalten darf
    /// </summary>
    /// <param name="feldData">Felddaten des Spielfeldes als char-Array</param>
    /// <param name="feldBreite">Breite des Feldes (Höhe wird automatisch ermittelt)</param>
    /// <returns>Bool-Array mit gleicher Größe wie feldData, gibt an, wo der Spieler sich aufhalten darf</returns>
    public static bool[] FieldRoomScan(char[] feldData, int feldBreite)
    {
      int fieldHeight = feldData.Length / feldBreite;

      var playerRoom = feldData.Select(c => c == '@' || c == '+').ToArray();

      bool find = true;
      while (find)
      {
        find = false;
        for (int y = 1; y < fieldHeight - 1; y++)
        {
          for (int x = 1; x < feldBreite - 1; x++)
          {
            if (playerRoom[x + y * feldBreite])
            {
              int p = x + y * feldBreite - feldBreite;
              if (!playerRoom[p] && " .$*".Any(c => feldData[p] == c)) find = playerRoom[p] = true;
              p += feldBreite - 1;
              if (!playerRoom[p] && " .$*".Any(c => feldData[p] == c)) find = playerRoom[p] = true;
              p += 2;
              if (!playerRoom[p] && " .$*".Any(c => feldData[p] == c)) find = playerRoom[p] = true;
              p += feldBreite - 1;
              if (!playerRoom[p] && " .$*".Any(c => feldData[p] == c)) find = playerRoom[p] = true;
            }
          }
        }
      }

      return playerRoom;
    }

    /// <summary>
    /// erstellt eine gekürzte Kopie eines Arrays
    /// </summary>
    /// <param name="array">Array mit den entsprechenden Daten</param>
    /// <param name="count">Anzahl der Datensätze (darf maximale so lang sein wie das Array selbst)</param>
    /// <returns>neues Array mit den entsprechenden Daten</returns>
    public static T[] CopyArray<T>(T[] array, int count) where T : struct
    {
      var result = new T[count];

      for (int i = 0; i < count; i++) result[i] = array[i];

      return result;
    }
    #endregion
  }
}
