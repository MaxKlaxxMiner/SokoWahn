#region # using *.*

using System;
using System.Linq;
using SokoWahnCore.CoreTools;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum einlesen und verarbeiten von Spielfeldern
  /// </summary>
  public unsafe sealed class SokowahnField
  {
    #region # // --- Statische Werte ---
    /// <summary>
    /// gibt die Breite des Spielfeldes an
    /// </summary>
    public readonly int width;

    /// <summary>
    /// gibt die Höhe des Spielfeldes an
    /// </summary>
    public readonly int height;

    /// <summary>
    /// merkt sich das eigentliche Spielfeld
    /// </summary>
    public readonly char[] fieldData;

    /// <summary>
    /// gibt die Anzahl der vorhandenen Boxen an
    /// </summary>
    public readonly int boxesCount;
    #endregion

    #region # // --- interne Status-Variablen ---
    /// <summary>
    /// merkt sich die aktuelle Position vom Spieler (posis[0]) und die Positionen aller Boxen
    /// </summary>
    internal ushort[] posis;

    /// <summary>
    /// merkt sich die Anzahl der Boxen, welche sich noch nicht auf einem Zielfeld befinden
    /// </summary>
    internal int boxesRemain;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fieldText">Daten des Spielfeldes als Textdaten</param>
    public SokowahnField(string fieldText)
    {
      // --- Zeilen einlesen ---
      var lines = fieldText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", "        ").Split('\n');

      lines = NormalizeLines(lines);
      if (lines.Length < 3) throw new ArgumentException("kein sinnvolles Spielfeld gefunden");

      // --- Spielfeld zuordnen ---
      width = lines.First().Length;
      height = lines.Length;
      int fieldLength = width * height;
      if (fieldLength > ushort.MaxValue) throw new IndexOutOfRangeException("Spielfeld ist zu größ");

      fieldData = string.Concat(lines).ToCharArray();
      if (fieldData.Length != fieldLength) throw new InvalidOperationException();

      boxesCount = fieldData.Count(c => c == '$' || c == '*');

      // --- Spielstatus scannen ---
      ScanGameState();

      // --- Spielfeld-Logik prüft um eine Reihe von möglichen Fehlern zu erkennen
      ValidateFieldLogic();
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="sokowahnField">vorhandenes Sokowahn-Spielfeld, kopiert werden soll</param>
    /// <param name="gameState">optionaler Spielstatus, welcher stattdessen verwendet werden soll</param>
    public SokowahnField(SokowahnField sokowahnField, ushort[] gameState = null)
    {
      width = sokowahnField.width;
      height = sokowahnField.height;
      fieldData = sokowahnField.fieldData.ToArray();
      boxesCount = sokowahnField.boxesCount;

      boxesRemain = sokowahnField.boxesRemain;
      posis = sokowahnField.posis.ToArray();

      if (gameState != null) SetGameState(gameState);
    }
    #endregion

    #region # // --- public Properties ---
    /// <summary>
    /// gibt die aktuelle Spielerposition zurück
    /// </summary>
    /// <returns>Spieler Position</returns>
    public int PlayerPos { get { return posis[0]; } }
    #endregion

    #region # // --- public Methoden ---
    /// <summary>
    /// gibt den aktuellen Spielstatus mit allen Positionen zurück
    /// </summary>
    /// <param name="clone">gibt an, ob die Werte im neuen Array kopiert werden sollen, sonst wird nur die Referenz weiter gegeben (default: true)</param>
    /// <returns>Spielstatus</returns>
    public ushort[] GetGameState(bool clone = true)
    {
      return clone ? posis.ToArray() : posis;
    }

    /// <summary>
    /// gibt den Spielstatus zurück, speichert diesen in ein Array und gibt die Anzahl der gespeicherten Elemente zurück
    /// </summary>
    /// <param name="destBuffer">Buffer, wohin der Spielstatus geschrieben werden</param>
    /// <param name="index">Startposition im Array, wohin der Spielstatus geschrieben werden soll</param>
    /// <returns>Anzahl der geschriebenen Elemente</returns>
    public int GetGameState(ushort[] destBuffer, int index)
    {
      for (int i = 0; i < posis.Length; i++)
      {
        destBuffer[index + i] = posis[i];
      }
      return posis.Length;
    }

    /// <summary>
    /// berechnet die Crc64-Prüfsumme vom Spielstatus
    /// </summary>
    /// <returns>berechneter Crc64-Wert</returns>
    public ulong GetGameStateCrc()
    {
      return Crc64.Start.Crc64Update(posis, 0, posis.Length);
    }

    /// <summary>
    /// scannt nach allen Zugmöglichkeiten und gibt diese zurück
    /// </summary>
    /// <param name="output">Ausgabe Buffer für alle Zugmöglichkeiten</param>
    /// <returns>Anzahl der erkannten Zugmöglichkeiten</returns>
    public int ScanMoves(ushort[] output)
    {
      var stateBackup = GetGameState();
      int outputCount = 0;

      bool* scannedFields = stackalloc bool[fieldData.Length];
      ushort* scanTodo = stackalloc ushort[fieldData.Length];

      int scanTodoPos = 0;
      int scanTodoLen = 0;
      scanTodo[scanTodoLen++] = stateBackup[0];
      scannedFields[stateBackup[0]] = true;

      while (scanTodoPos < scanTodoLen)
      {
        int scan = scanTodo[scanTodoPos++];

        #region # // --- links ---
        switch (fieldData[scan - 1])
        {
          case ' ':
          case '.': if (!scannedFields[scan - 1]) { scannedFields[scan - 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan - 1); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MovePlayer(-1))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- rechts ---
        switch (fieldData[scan + 1])
        {
          case ' ':
          case '.': if (!scannedFields[scan + 1]) { scannedFields[scan + 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan + 1); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MovePlayer(+1))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- oben ---
        switch (fieldData[scan - width])
        {
          case ' ':
          case '.': if (!scannedFields[scan - width]) { scannedFields[scan - width] = true; scanTodo[scanTodoLen++] = (ushort)(scan - width); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MovePlayer(-width))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- unten ---
        switch (fieldData[scan + width])
        {
          case ' ':
          case '.': if (!scannedFields[scan + width]) { scannedFields[scan + width] = true; scanTodo[scanTodoLen++] = (ushort)(scan + width); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MovePlayer(+width))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion
      }

      return outputCount;
    }

    /// <summary>
    /// scannt nach allen Rückwärts-Zugmöglichkeiten und gibt diese zurück
    /// </summary>
    /// <param name="output">Ausgabe Buffer für alle Zugmöglichkeiten</param>
    /// <returns>Anzahl der erkannten Zugmöglichkeiten</returns>
    public int ScanReverseMoves(ushort[] output)
    {
      var stateBackup = GetGameState();
      int outputCount = 0;

      bool* scannedFields = stackalloc bool[fieldData.Length];
      ushort* scanTodo = stackalloc ushort[fieldData.Length];

      int scanTodoPos = 0;
      int scanTodoLen = 0;
      scanTodo[scanTodoLen++] = stateBackup[0];
      scannedFields[stateBackup[0]] = true;

      while (scanTodoPos < scanTodoLen)
      {
        int scan = scanTodo[scanTodoPos++];

        #region # // --- links (zurück nach rechts) ---
        switch (fieldData[scan - 1])
        {
          case ' ':
          case '.': if (!scannedFields[scan - 1]) { scannedFields[scan - 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan - 1); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MoveReversePlayer(-1, true))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- rechts (zurück nach links) ---
        switch (fieldData[scan + 1])
        {
          case ' ':
          case '.': if (!scannedFields[scan + 1]) { scannedFields[scan + 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan + 1); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MoveReversePlayer(+1, true))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- oben (zurück nach unten) ---
        switch (fieldData[scan - width])
        {
          case ' ':
          case '.': if (!scannedFields[scan - width]) { scannedFields[scan - width] = true; scanTodo[scanTodoLen++] = (ushort)(scan - width); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MoveReversePlayer(-width, true))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion

        #region # // --- unten (zurück nach oben) ---
        switch (fieldData[scan + width])
        {
          case ' ':
          case '.': if (!scannedFields[scan + width]) { scannedFields[scan + width] = true; scanTodo[scanTodoLen++] = (ushort)(scan + width); } break;

          case '$':
          case '*':
          {
            SetPlayerPos(scan);
            if (MoveReversePlayer(+width, true))
            {
              int outputOffset = outputCount * posis.Length;
              for (int i = 0; i < posis.Length; i++) output[outputOffset + i] = posis[i];
              outputCount++;
              SetGameState(stateBackup);
            }
            else SetPlayerPos(stateBackup[0]);
          } break;
        }
        #endregion
      }

      return outputCount;
    }

    /// <summary>
    /// setzt die Spielerposition
    /// </summary>
    /// <param name="newPlayerPos">neue Spielerposition</param>
    public void SetPlayerPos(int newPlayerPos)
    {
      int oldPlayerPos = posis[0];
      if (oldPlayerPos == newPlayerPos) return;
      fieldData[oldPlayerPos] = fieldData[oldPlayerPos] == '+' ? '.' : ' ';
      fieldData[newPlayerPos] = fieldData[newPlayerPos] == '.' ? '+' : '@';
      posis[0] = (ushort)newPlayerPos;
    }

    /// <summary>
    /// setzt einen bestimmten Spielstatus
    /// </summary>
    /// <param name="gameState">Spielstatus mit allen Positionen, welcher gesetzt werden soll</param>
    /// <param name="ofs">optionaler Offset</param>
    public void SetGameState(ushort[] gameState, int ofs = 0)
    {
      // --- altes Spiefeld räumen ---
      int playerPos = posis[0];
      fieldData[playerPos] = fieldData[playerPos] == '+' ? '.' : ' ';
      for (int box = 1; box < posis.Length; box++)
      {
        int boxPos = posis[box];
        fieldData[boxPos] = fieldData[boxPos] == '*' ? '.' : ' ';
      }

      // --- neues Spielfeld setzen ---
      playerPos = gameState[ofs];
      fieldData[playerPos] = fieldData[playerPos] == '.' ? '+' : '@';
      int newRemain = 0;
      for (int box = 1; box < posis.Length; box++)
      {
        int boxPos = gameState[ofs + box];
        if (fieldData[boxPos] == '.')
        {
          fieldData[boxPos] = '*';
        }
        else
        {
          fieldData[boxPos] = '$';
          newRemain++;
        }
      }
      boxesRemain = newRemain;

      Array.Copy(gameState, ofs, posis, 0, posis.Length);
    }

    /// <summary>
    /// ändert die Kisten-Positionen und kann ebenfalls die Anzahl der Kisten ändern
    /// </summary>
    public void SetBoxes(ushort[] newBoxes)
    {
      // --- alte Kisten entfernen ---
      for (int box = 1; box < posis.Length; box++)
      {
        int boxPos = posis[box];
        fieldData[boxPos] = fieldData[boxPos] == '*' ? '.' : ' ';
      }

      if (newBoxes.Length != posis.Length - 1) // Anzahl der Kisten hat sich geändert?
      {
        if (newBoxes.Length > boxesCount) throw new ArgumentException("zuviele Kisten");
        Array.Resize(ref posis, newBoxes.Length + 1);
      }

      // --- neue Kisten setzen ---
      int newRemain = 0;
      for (int box = 0; box < newBoxes.Length; box++)
      {
        ushort boxPos = newBoxes[box];
        posis[box + 1] = boxPos;
        if (fieldData[boxPos] == '.')
        {
          fieldData[boxPos] = '*';
        }
        else
        {
          fieldData[boxPos] = '$';
          newRemain++;
        }
      }
      boxesRemain = newRemain;
    }

    /// <summary>
    /// bewegt den Spieler in eine bestimmte Richtung
    /// </summary>
    /// <param name="moveDirection">Richtung, in welcher der Spieler bewegt werden soll (-1 = links, +1 = rechts, -width = hoch, +width = runter)</param>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    internal bool MovePlayer(int moveDirection)
    {
      int oldPlayerPos = posis[0];
      int newPlayerPos = oldPlayerPos + moveDirection;
      switch (fieldData[newPlayerPos])
      {
        // --- keine Box verschieben ---
        case ' ':
        case '.':
        {
          fieldData[oldPlayerPos] = fieldData[oldPlayerPos] == '+' ? '.' : ' ';
          fieldData[newPlayerPos] = fieldData[newPlayerPos] == '.' ? '+' : '@';
          posis[0] = (ushort)newPlayerPos;
        } return true;

        // --- mit Box verschieben
        case '$':
        case '*':
        {
          int newBoxPos = newPlayerPos + moveDirection;
          if (fieldData[newBoxPos] != ' ' && fieldData[newBoxPos] != '.') return false; // Weg blockiert

          #region # // --- alte Kiste entfernen und Spieler setzen ---
          fieldData[oldPlayerPos] = fieldData[oldPlayerPos] == '+' ? '.' : ' ';
          if (fieldData[newPlayerPos] == '*')
          {
            fieldData[newPlayerPos] = '+';
            boxesRemain++;
          }
          else
          {
            fieldData[newPlayerPos] = '@';
          }
          #endregion

          #region # // --- neue Kiste setzen ---
          if (fieldData[newBoxPos] == '.')
          {
            fieldData[newBoxPos] = '*';
            boxesRemain--;
          }
          else
          {
            fieldData[newBoxPos] = '$';
          }
          #endregion

          #region # // --- Index korrigieren ---
          posis[0] = (ushort)newPlayerPos;
          for (int i = 1; i < posis.Length; i++)
          {
            if (posis[i] != newPlayerPos) continue;
            if (moveDirection < 0)
            {
              while (i > 1 && posis[i - 1] > newBoxPos)
              {
                posis[i] = posis[i - 1];
                i--;
              }
            }
            else
            {
              while (i < posis.Length - 1 && posis[i + 1] < newBoxPos)
              {
                posis[i] = posis[i + 1];
                i++;
              }
            }
            posis[i] = (ushort)newBoxPos;
            break;
          }
          #endregion
        } return true;

        default: return false;
      }
    }

    /// <summary>
    /// bewegt den Spieler wieder einen Schritt rückwärts
    /// </summary>
    /// <param name="moveDirection">Richtung, in welcher der Spieler vorher bewegt wurde (-1 = links, +1 = rechts, -width = hoch, +width = runter)</param>
    /// <param name="moveBox">gibt an, ob eine Box ebenfalls zurück "gezogen" werden soll</param>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    internal bool MoveReversePlayer(int moveDirection, bool moveBox)
    {
      int oldPlayerPos = posis[0];
      int newPlayerPos = oldPlayerPos - moveDirection;

      switch (fieldData[newPlayerPos])
      {
        // --- freier Platz ---
        case ' ':
        case '.':
        {
          if (moveBox)
          {
            int oldBoxPos = oldPlayerPos + moveDirection;

            #region # // --- Kiste vom alten Feld weg ziehen ---
            if (fieldData[oldBoxPos] == '$')
            {
              fieldData[oldBoxPos] = ' ';
            }
            else if (fieldData[oldBoxPos] == '*')
            {
              fieldData[oldBoxPos] = '.';
              boxesRemain++;
            }
            else return false; // kein gültiger Rückwärts-Zug
            #endregion

            #region # // --- Spieler weg ziehen und Kiste dort hin platzieren ---
            if (fieldData[oldPlayerPos] == '+')
            {
              fieldData[oldPlayerPos] = '*';
              boxesRemain--;
            }
            else
            {
              fieldData[oldPlayerPos] = '$';
            }
            #endregion

            #region # // --- Index korrigieren ---
            for (int i = 1; i < posis.Length; i++)
            {
              if (posis[i] != oldBoxPos) continue;
              if (moveDirection > 0)
              {
                while (i > 1 && posis[i - 1] > oldPlayerPos)
                {
                  posis[i] = posis[i - 1];
                  i--;
                }
              }
              else
              {
                while (i < posis.Length - 1 && posis[i + 1] < oldPlayerPos)
                {
                  posis[i] = posis[i + 1];
                  i++;
                }
              }
              posis[i] = (ushort)oldPlayerPos;
              break;
            }
            #endregion
          }
          else
          {
            fieldData[oldPlayerPos] = fieldData[oldPlayerPos] == '+' ? '.' : ' ';
          }
          fieldData[newPlayerPos] = fieldData[newPlayerPos] == '.' ? '+' : '@';
          posis[0] = (ushort)newPlayerPos;
        } return true;

        default: return false;
      }
    }

    /// <summary>
    /// bewegt den Spieler um eins nach links
    /// </summary>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    public bool MoveLeft()
    {
      return MovePlayer(-1);
    }

    /// <summary>
    /// bewegt den Spieler um eins nach rechts
    /// </summary>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    public bool MoveRight()
    {
      return MovePlayer(+1);
    }

    /// <summary>
    /// bewegt den Spieler um eins nach oben
    /// </summary>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    public bool MoveUp()
    {
      return MovePlayer(-width);
    }

    /// <summary>
    /// bewegt den Spieler um eins nach oben
    /// </summary>
    /// <returns>true, wenn der Schritt erfolgreich war</returns>
    public bool MoveDown()
    {
      return MovePlayer(+width);
    }
    #endregion

    #region # // --- private Methoden ---

    #region # void ScanGameStatus() // scannt das Spielfeld und merkt sich die Positionen der Boxen und die Position vom Spieler
    /// <summary>
    /// scannt das Spielfeld und merkt sich die Positionen der Boxen und die Position vom Spieler
    /// </summary>
    void ScanGameState()
    {
      boxesRemain = fieldData.Count(c => c == '$');
      int freeFields = fieldData.Count(c => c == '.' || c == '+');
      if (boxesRemain != freeFields) throw new ArgumentException("Anzahl der Boxen stimmt nicht mit der Anzahl der Spielfelder überein");

      // --- Spieler suchen ---
      int playerPos = -1;
      for (int i = 0; i < fieldData.Length; i++)
      {
        if (fieldData[i] != '@' && fieldData[i] != '+') continue;
        if (playerPos >= 0) throw new ArgumentException("Mehr als ein Spieler auf dem Spielfeld vorhanden");
        playerPos = i;
      }
      if (playerPos < 0) throw new ArgumentException("Spieler wurde nicht gefunden");

      // --- Boxen-Positionen ermitteln ---
      posis = new ushort[1 + boxesCount];
      posis[0] = (ushort)playerPos;

      int posOffset = 1;
      for (int i = 0; i < fieldData.Length; i++)
      {
        if (fieldData[i] == '$' || fieldData[i] == '*')
        {
          posis[posOffset++] = (ushort)i;
        }
      }
      if (posOffset != posis.Length) throw new InvalidOperationException();
    }
    #endregion

    #region # void ValidateFieldLogic() // prüft, ob das Spielfeld gültig ist und (theoretisch) gelöst werden kann
    /// <summary>
    /// prüft, ob das Spielfeld gültig ist und (theoretisch) gelöst werden kann
    /// </summary>
    static void ValidateFieldLogic()
    {
      // todo
    }
    #endregion

    #region # static string[] NormalizeLines(string[] lines) // normalisiert die Zeilen
    /// <summary>
    /// normalisiert die Zeilen
    /// </summary>
    /// <param name="lines">Zeilen, welche normalisiert werden sollen</param>
    /// <returns>fertig normalisierte Zeilen</returns>
    static string[] NormalizeLines(string[] lines)
    {
      var tmp = lines.ToArray();

      // --- Kommentare am Ende wegschneiden ---
      for (int i = 0; i < tmp.Length; i++)
      {
        int pos = tmp[i].IndexOfAny(new[] { ';', '/' });
        if (pos < 0) continue;
        tmp[i] = tmp[i].Substring(0, pos);
      }

      // --- ungültige Zeichen filtern und durch Leerzeichen ersetzen ---
      foreach (string line in tmp)
      {
        int len = line.Length;
        fixed (char* chars = line)
        {
          for (int i = 0; i < len; i++)
          {
            switch (chars[i])
            {
              case '#': // solide Wand

              case ' ': // leeres Spielfeld
              case '.': // offenes Zielfeld

              case '$': // Box im freien Raum
              case '*': // Box auf einem Zielfeld

              case '@': // Spieler im freien Raum
              case '+': // Spieler auf einem Zielfeld

              break;

              default: chars[i] = ' '; break; // andere Zeichen durch Leerezeichen ersetzen
            }
          }
        }
      }

      // --- erste Spielfeld-Zeile suchen ---
      int firstLine = 0;
      while (firstLine < tmp.Length - 1 && tmp[firstLine].Trim().Length == 0) firstLine++;

      // --- letzte Spielfeld-Zeile suchen ---
      int lastLine = firstLine;
      while (lastLine < tmp.Length - 1 && tmp[lastLine + 1].Trim().Length > 0) lastLine++;

      // --- Zeilen zurecht schneiden ---
      tmp = tmp.Skip(firstLine).Take(Math.Max(0, lastLine - firstLine + 1)).ToArray();

      // --- Länge der Zeilen nach dem Ende ausrichten ---
      int maxLenEnd = tmp.Max(line => line.TrimEnd().Length);
      for (int i = 0; i < tmp.Length; i++) tmp[i] = tmp[i].PadRight(maxLenEnd).Substring(0, maxLenEnd);

      // --- vorderen Einschub der Zeilen entfernen ---
      int maxLenStart = tmp.Max(line => line.TrimStart().Length);
      for (int i = 0; i < tmp.Length; i++) tmp[i] = tmp[i].Remove(0, tmp[i].Length - maxLenStart);

      return tmp;
    }
    #endregion

    #endregion

    #region # public override string ToString() // gibt den Inhalt des gesamten Spielfeldes aus
    /// <summary>
    /// gibt den Inhalt des gesamten Spielfeldes aus
    /// </summary>
    /// <returns>Inhalt des Spielfeldes</returns>
    public override string ToString()
    {
      return string.Join(Environment.NewLine, Enumerable.Range(0, height).Select(line => new string(fieldData.Skip(line * width).Take(width).ToArray())));
    }
    #endregion
  }
}
