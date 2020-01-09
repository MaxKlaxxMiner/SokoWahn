#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable AccessToModifiedClosure
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// enthält ein eigenes Spielfeld-System und mehrere Methoden zum schnellen suchen von Zugmöglichkeiten in beide Richtungen
  /// </summary>
  public sealed class SokowahnField
  {
    #region # // --- statische Werte (welche sich nicht ändern) ---
    /// <summary>
    /// merkt sich die Grunddaten des Spielfeldes
    /// </summary>
    readonly char[] fieldData;

    /// <summary>
    /// Breite des Spielfeldes
    /// </summary>
    readonly int fieldWidth;

    /// <summary>
    /// merkt sich die Anzahl der begehbaren Bereiche
    /// </summary>
    readonly int roomCount;

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
        return fieldData.ToArray();
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld zurück, jedoch ohne Spieler und ohne Kisten
    /// </summary>
    public char[] FieldDataEmpty
    {
      get
      {
        return fieldData.Select(z =>
        {
          switch (z)
          {
            case '#': return '#';
            case ' ': return ' ';
            case '.': return '.';
            case '$': return ' ';
            case '*': return '.';
            case '@': return ' ';
            case '+': return '.';
            default: throw new Exception("ungültiges Zeichen: " + z);
          }
        }).ToArray();
      }
    }

    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    public int FieldWidth
    {
      get
      {
        return fieldWidth;
      }
    }

    /// <summary>
    /// gibt die Höhe des Spielfeldes zurück
    /// </summary>
    public int FieldHeight
    {
      get
      {
        return fieldData.Length / fieldWidth;
      }
    }

    /// <summary>
    /// gibt das Spielfeld als lesbaren String aus
    /// </summary>
    /// <returns>lesbarer String</returns>
    public override string ToString()
    {
      var fieldEmpty = FieldDataEmpty;

      int fieldHeight = fieldData.Length / fieldWidth;

      var playerRoom = FieldRoomScan(fieldData, fieldWidth);
      var roomToField = Enumerable.Range(0, playerRoom.Length).Where(i => playerRoom[i]).ToArray();
      var fieldToRoom = Enumerable.Range(0, fieldData.Length).Select(i => playerRoom[i] ? roomToField.ToList().IndexOf(i) : -1).ToArray();

      var roomToBoxes = this.roomToBoxes.ToArray();
      int boxesCount = this.boxesCount;
      int roomPlayerPos = this.roomPlayerPos;

      return string.Concat(Enumerable.Range(0, fieldHeight).Select(y => new string(Enumerable.Range(0, fieldWidth).Select(x =>
      {
        int p = fieldToRoom[x + y * fieldWidth];
        if (p < 0) return fieldEmpty[x + y * fieldWidth];
        if (roomToBoxes[p] < boxesCount) return fieldEmpty[x + y * fieldWidth] == '.' ? '*' : '$';
        if (p == roomPlayerPos) return fieldEmpty[x + y * fieldWidth] == '.' ? '+' : '@';
        return fieldEmpty[x + y * fieldWidth];
      }).ToArray()) + "\r\n")).TrimEnd() + (CalcDepth != 0 ? " - Tiefe: " + CalcDepth.ToString("#,##0") : "")
        //   + " - Crc: " + Crc 
      + "\r\n";
    }
    #endregion

    #region # // --- public Methoden ---
    #region # public SokowahnPosition GetPosition() // gibt die aktuelle Stellung zurück
    /// <summary>
    /// gibt die aktuelle Stellung zurück
    /// </summary>
    /// <returns>aktuelle Stellung</returns>
    public SokowahnPosition GetPosition()
    {
      return new SokowahnPosition { roomPlayerPos = roomPlayerPos, boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, calcDepth = playerCalcDepth };
    }
    #endregion

    #region # public void LoadPosition(*[] src, int off, int spielerZugTiefe) // lädt eine bestimmte Stellung
    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="src">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="depth">Zugtiefe der Stellung</param>
    public void LoadPosition(byte[] src, int off, int depth)
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
    public void LoadPosition(byte[] src, int depth)
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
    public void LoadPosition(ushort[] src, int off, int depth)
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
    public void LoadPosition(ushort[] src, int depth)
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
    /// <param name="position">Stellung, welche geladen werden soll</param>
    public void LoadPosition(SokowahnPosition position)
    {
      roomPlayerPos = position.roomPlayerPos;
      playerCalcDepth = position.calcDepth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i] = position.boxesToRoom[i]] = i;
    }

    /// <summary>
    /// Sondervariante für das Blocker-System (ohne setzen der Spielerposition)
    /// </summary>
    /// <param name="boxesIndex">Kisten-Index für sammlerKistenRaum (Länge = sammlerKistenAnzahl)</param>
    /// <param name="boxesRoom">Raumpositionen der einzelnen Kisten (Länge = basisKistenAnzahl)</param>
    public void LoadPosition(int[] boxesIndex, int[] boxesRoom)
    {
      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) roomToBoxes[boxesToRoom[i] = boxesRoom[boxesIndex[i]]] = i;
    }
    #endregion
    #region # public void SavePosition(*[] dst, int off) // speichert eine bestimmte Stellung
    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="dst">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SavePosition(byte[] dst, int off)
    {
      dst[off++] = (byte)roomPlayerPos;
      for (int i = 0; i < boxesCount; i++) dst[off++] = (byte)boxesToRoom[i];
    }

    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="dst">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SavePosition(ushort[] dst, int off)
    {
      dst[off++] = (ushort)roomPlayerPos;
      for (int i = 0; i < boxesCount; i++) dst[off++] = (ushort)boxesToRoom[i];
    }
    #endregion

    /// <summary>
    /// berechnet die Lauf-Schritte bis zur nächsten Kisten-Bewegung
    /// </summary>
    /// <param name="next">nachfolgendes Spielfeld</param>
    /// <returns>Schritte um das Spielfeld zu erreichen</returns>
    public string GetSteps(SokowahnField next)
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

    #region # public IEnumerable<SokowahnStellung> GetVariants() // ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// <summary>
    /// ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// </summary>
    /// <returns>Enumerable der gefundenen Stellungen</returns>
    public IEnumerable<SokowahnPosition> GetVariants()
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

              yield return new SokowahnPosition { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

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

              yield return new SokowahnPosition { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

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

              yield return new SokowahnPosition { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

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

              yield return new SokowahnPosition { roomPlayerPos = roomPlayerPos, boxesToRoom = CopyArray(boxesToRoom, boxesCount), crc64 = Crc, calcDepth = pDepth };

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
    public IEnumerable<SokowahnPosition> GetVariantsBackward()
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
    IEnumerable<SokowahnPosition> GetVariantsBackwardStep()
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
              yield return new SokowahnPosition { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
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
              yield return new SokowahnPosition { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
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
              yield return new SokowahnPosition { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
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
              yield return new SokowahnPosition { boxesToRoom = boxesToRoom.ToArray(), crc64 = Crc, roomPlayerPos = roomPlayerPos, calcDepth = pDepth };
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
    public IEnumerable<SokowahnPosition> GetVariantsBlockerGoals()
    {
      for (int box = 0; box < boxesCount; box++)
      {
        int pBox = boxesToRoom[box];
        int pPlayer;

        if ((pPlayer = roomLeft[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetPosition();
        }

        if ((pPlayer = roomRight[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetPosition();
        }

        if ((pPlayer = roomUp[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetPosition();
        }

        if ((pPlayer = roomDown[pBox]) < roomCount && roomToBoxes[pPlayer] == boxesCount)
        {
          roomPlayerPos = pPlayer;
          yield return GetPosition();
        }
      }
    }
    #endregion

    #region # // --- Debug-Methoden ---
    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="position">Stellung, welche ausgelesen werden soll</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(SokowahnPosition position)
    {
      var tmpData = new ushort[boxesCount + 1];
      position.SavePosition(tmpData, 0);
      return Debug(tmpData, 0, position.calcDepth);
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(byte[] data, int offset)
    {
      var tmp = new SokowahnField(this);

      tmp.LoadPosition(data, offset, 0);

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
      var tmp = new SokowahnField(this);

      tmp.LoadPosition(data, offset, 0);

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
      var tmp = new SokowahnField(this);

      tmp.LoadPosition(data, offset, zugTiefe);

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
      var tmp = new SokowahnField(this);

      tmp.LoadPosition(data, offset, zugTiefe);

      return tmp.ToString();
    }
    #endregion
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Basisdaten anhand eines vorhanden Raumes nutzen</param>
    public SokowahnField(SokowahnField field)
    {
      fieldData = field.fieldData;
      fieldWidth = field.fieldWidth;
      roomCount = field.roomCount;
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
    /// <param name="fieldData">Daten des Spielfeldes</param>
    /// <param name="fieldWidth">Breite des Spielfeldes</param>
    public SokowahnField(char[] fieldData, int fieldWidth)
    {
      this.fieldData = fieldData;
      this.fieldWidth = fieldWidth;

      var spielerRaum = FieldRoomScan(fieldData, fieldWidth);

      int roomCount = this.roomCount = spielerRaum.Count(x => x);

      var roomToField = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();

      roomLeft = roomToField.Select(i => spielerRaum[i - 1] ? roomToField.ToList().IndexOf(i - 1) : roomCount).ToArray();
      roomRight = roomToField.Select(i => spielerRaum[i + 1] ? roomToField.ToList().IndexOf(i + 1) : roomCount).ToArray();
      roomUp = roomToField.Select(i => spielerRaum[i - fieldWidth] ? roomToField.ToList().IndexOf(i - fieldWidth) : roomCount).ToArray();
      roomDown = roomToField.Select(i => spielerRaum[i + fieldWidth] ? roomToField.ToList().IndexOf(i + fieldWidth) : roomCount).ToArray();

      boxesToRoom = Enumerable.Range(0, roomCount).Where(i => spielerRaum[roomToField[i]] && (fieldData[roomToField[i]] == '$' || fieldData[roomToField[i]] == '*')).ToArray();
      int boxesCount = this.boxesCount = boxesToRoom.Length;
      int counter = 0;
      roomToBoxes = roomToField.Select(i => (fieldData[i] == '$' || fieldData[i] == '*') ? counter++ : boxesCount).ToArray();
      Array.Resize(ref roomToBoxes, roomCount + 1);
      roomToBoxes[roomCount] = boxesCount;

      roomPlayerPos = Enumerable.Range(0, roomCount).First(i => fieldData[roomToField[i]] == '@' || fieldData[roomToField[i]] == '+');
      playerCalcDepth = 0;

      tmpCheckRoomPosis = new int[roomCount];
      tmpCheckRoomDepth = new int[roomCount];
      tmpCheckRoomReady = new bool[roomCount + 1];
      tmpCheckRoomReady[roomCount] = true; // Ende-Feld schon auf fertig setzen
    }
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
    #endregion

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
  }
}
