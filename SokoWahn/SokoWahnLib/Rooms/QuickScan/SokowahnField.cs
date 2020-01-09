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
    readonly char[] feldData;

    /// <summary>
    /// Breite des Spielfeldes
    /// </summary>
    readonly int feldBreite;

    /// <summary>
    /// merkt sich die Anzahl der begehbaren Bereiche
    /// </summary>
    readonly int raumAnzahl;

    /// <summary>
    /// zeigt auf das linke benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumLinks;

    /// <summary>
    /// zeigt auf das rechts benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumRechts;

    /// <summary>
    /// zeigt auf das obere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumOben;

    /// <summary>
    /// zeigt auf das untere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumUnten;

    /// <summary>
    /// Anzahl der Kisten, welche auf dem Spielfeld liegen (kann bei der Blocker-Suche niedriger sein als die eigentliche Ziele-Anzahl)
    /// </summary>
    int boxesCount;
    #endregion

    #region # // --- dynamische Werte ---
    /// <summary>
    /// aktuelle Spielerposition im SokowahnRaum
    /// </summary>
    int raumSpielerPos;

    /// <summary>
    /// gibt bei den begehbaren Raumbereichen an, welche Kiste sich dort befindet (Wert = kistenAnzahl, keine Kiste steht auf dem Feld)
    /// </summary>
    readonly int[] raumZuKisten;

    /// <summary>
    /// enthält die Kistenpositionen (Wert = raumAnzahl, Kiste steht nirgendwo, ist also nicht vorhanden)
    /// </summary>
    int[] kistenZuRaum;

    /// <summary>
    /// merkt sich die aktuelle Zugtiefe
    /// </summary>
    int spielerZugTiefe;
    #endregion

    #region # // --- temporäre Werte ---
    /// <summary>
    /// merkt sich temporär, welche Positionen geprüft wurden bzw. noch geprüft werden müssen
    /// </summary>
    readonly int[] tmpCheckRaumPosis;

    /// <summary>
    /// merkt sich temporär, welche Zugtiefen erreicht wurden
    /// </summary>
    readonly int[] tmpCheckRaumTiefe;

    /// <summary>
    /// merkt sich temporär, welche Felder im Raum bereits abgelaufen wurden
    /// </summary>
    readonly bool[] tmpRaumCheckFertig;
    #endregion

    #region # // --- public Properties ---
    /// <summary>
    /// berechnet den CRC-Schlüssel der aktuellen Stellung
    /// </summary>
    public ulong Crc
    {
      get
      {
        ulong ergebnis = (Crc64.Start ^ (ulong)raumSpielerPos) * 0x100000001b3;

        for (int i = 0; i < boxesCount; i++)
        {
          ergebnis = (ergebnis ^ (ulong)kistenZuRaum[i]) * 0x100000001b3;
        }

        return ergebnis;
      }
    }

    /// <summary>
    /// gibt die Anzahl der begehbaren Raum-Felder an
    /// </summary>
    public int RaumAnzahl
    {
      get
      {
        return raumAnzahl;
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
        for (int i = 0; i < raumAnzahl; i++) raumZuKisten[i] = boxesCount;
        kistenZuRaum = Enumerable.Range(0, boxesCount).Select(i => i).ToArray();

        for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = i; // Kisten neu sinnlos auf das Feld setzen
      }
    }

    /// <summary>
    /// gibt die aktuelle Zugtiefe zurück
    /// </summary>
    public int SpielerZugTiefe
    {
      get
      {
        if (spielerZugTiefe >= 0 && spielerZugTiefe < 30000) return spielerZugTiefe;
        if (spielerZugTiefe >= 30000 & spielerZugTiefe < 60000) return spielerZugTiefe - 60000;
        return 0;
      }

      set
      {
        spielerZugTiefe = value;
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld zurück
    /// </summary>
    public char[] FeldData
    {
      get
      {
        return feldData.ToArray();
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld zurück, jedoch ohne Spieler und ohne Kisten
    /// </summary>
    public char[] FeldDataLeer
    {
      get
      {
        return feldData.Select(z =>
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
    public int FeldBreite
    {
      get
      {
        return feldBreite;
      }
    }

    /// <summary>
    /// gibt die Höhe des Spielfeldes zurück
    /// </summary>
    public int FeldHöhe
    {
      get
      {
        return feldData.Length / feldBreite;
      }
    }

    /// <summary>
    /// gibt das Spielfeld als lesbaren String aus
    /// </summary>
    /// <returns>lesbarer String</returns>
    public override string ToString()
    {
      var feldLeer = FeldDataLeer;

      int feldHöhe = feldData.Length / feldBreite;

      var spielerRaum = SpielfeldRaumScan(feldData, feldBreite);
      var raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();
      var feldZuRaum = Enumerable.Range(0, feldData.Length).Select(i => spielerRaum[i] ? raumZuFeld.ToList().IndexOf(i) : -1).ToArray();

      var raumZuKisten = this.raumZuKisten.ToArray();
      int kistenAnzahl = boxesCount;
      int raumSpielerPos = this.raumSpielerPos;

      return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x =>
      {
        int p = feldZuRaum[x + y * feldBreite];
        if (p < 0) return feldLeer[x + y * feldBreite];
        if (raumZuKisten[p] < kistenAnzahl) return feldLeer[x + y * feldBreite] == '.' ? '*' : '$';
        if (p == raumSpielerPos) return feldLeer[x + y * feldBreite] == '.' ? '+' : '@';
        return feldLeer[x + y * feldBreite];
      }).ToArray()) + "\r\n")).TrimEnd() + (SpielerZugTiefe != 0 ? " - Tiefe: " + SpielerZugTiefe.ToString("#,##0") : "")
        //   + " - Crc: " + Crc 
      + "\r\n";
    }
    #endregion

    #region # // --- public Methoden ---
    #region # public SokowahnStellung GetStellung() // gibt die aktuelle Stellung zurück
    /// <summary>
    /// gibt die aktuelle Stellung zurück
    /// </summary>
    /// <returns>aktuelle Stellung</returns>
    public SokowahnPosition GetStellung()
    {
      return new SokowahnPosition { roomPlayerPos = raumSpielerPos, boxesToRoom = kistenZuRaum.ToArray(), crc64 = Crc, calcDepth = spielerZugTiefe };
    }
    #endregion

    #region # public void LadeStellung(*[] datenArray, int off, int spielerZugTiefe) // lädt eine bestimmte Stellung
    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="zugTiefe">Zugtiefe der Stellung</param>
    public void LadeStellung(byte[] datenArray, int off, int zugTiefe)
    {
      // neue Spielerposition setzen
      raumSpielerPos = datenArray[off++];
      spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = datenArray[off + i];
        kistenZuRaum[i] = p;
        raumZuKisten[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array mit entsprechenden Daten</param>
    /// <param name="zugTiefe">Zugtiefe der Stellung</param>
    public void LadeStellung(byte[] datenArray, int zugTiefe)
    {
      // neue Spielerposition setzen
      raumSpielerPos = datenArray[0];
      spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = datenArray[i + 1];
        kistenZuRaum[i] = p;
        raumZuKisten[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="zugTiefe">Zugtiefe der Stellung</param>
    public void LadeStellung(ushort[] datenArray, int off, int zugTiefe)
    {
      // neue Spielerposition setzen
      raumSpielerPos = datenArray[off++];
      spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = datenArray[off + i];
        kistenZuRaum[i] = p;
        raumZuKisten[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array mit entsprechenden Daten</param>
    /// <param name="zugTiefe">Zugtiefe der Stellung</param>
    public void LadeStellung(ushort[] datenArray, int zugTiefe)
    {
      // neue Spielerposition setzen
      raumSpielerPos = datenArray[0];
      spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++)
      {
        int p = datenArray[i + 1];
        kistenZuRaum[i] = p;
        raumZuKisten[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="position">Stellung, welche geladen werden soll</param>
    public void LadeStellung(SokowahnPosition position)
    {
      raumSpielerPos = position.roomPlayerPos;
      spielerZugTiefe = position.calcDepth;

      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i] = position.boxesToRoom[i]] = i;
    }

    /// <summary>
    /// Sondervariante für das Blocker-System (ohne setzen der Spielerposition)
    /// </summary>
    /// <param name="sammlerKistenIndex">Kisten-Index für sammlerKistenRaum (Länge = sammlerKistenAnzahl)</param>
    /// <param name="sammlerKistenRaum">Raumpositionen der einzelnen Kisten (Länge = basisKistenAnzahl)</param>
    public void LadeStellung(int[] sammlerKistenIndex, int[] sammlerKistenRaum)
    {
      // alte Kisten entfernen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i]] = boxesCount;

      // neue Kisten setzen
      for (int i = 0; i < boxesCount; i++) raumZuKisten[kistenZuRaum[i] = sammlerKistenRaum[sammlerKistenIndex[i]]] = i;
    }
    #endregion
    #region # public void SpeichereStellung(*[] datenArray, int off) // speichert eine bestimmte Stellung
    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SpeichereStellung(byte[] datenArray, int off)
    {
      datenArray[off++] = (byte)raumSpielerPos;
      for (int i = 0; i < boxesCount; i++) datenArray[off++] = (byte)kistenZuRaum[i];
    }

    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SpeichereStellung(ushort[] datenArray, int off)
    {
      datenArray[off++] = (ushort)raumSpielerPos;
      for (int i = 0; i < boxesCount; i++) datenArray[off++] = (ushort)kistenZuRaum[i];
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
      int spielerZiel = next.raumSpielerPos;
      int checkRaumVon = 0;
      int checkRaumBis = 0;

      Array.Clear(tmpRaumCheckFertig, 0, raumAnzahl);

      // erste Spielerposition hinzufügen
      tmpRaumCheckFertig[raumSpielerPos] = true;
      tmpCheckRaumPosis[checkRaumBis] = raumSpielerPos;
      tmpCheckRaumTiefe[checkRaumBis] = spielerZugTiefe;
      checkRaumBis++;

      Func<int, int, int> vorherFeld = (pp, tt) =>
      {
        if (raumLinks[pp] < raumAnzahl)
        {
          int index = -1;
          for (int i = 0; i < checkRaumBis; i++) if (tmpCheckRaumPosis[i] == raumLinks[pp]) index = i;
          if (index >= 0 && tmpCheckRaumTiefe[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "r" + findSteps;
              return raumLinks[pp];
            }
          }
        }

        if (raumRechts[pp] < raumAnzahl)
        {
          int index = -1;
          for (int i = 0; i < checkRaumBis; i++) if (tmpCheckRaumPosis[i] == raumRechts[pp]) index = i;
          if (index >= 0 && tmpCheckRaumTiefe[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "l" + findSteps;
              return raumRechts[pp];
            }
          }
        }

        if (raumOben[pp] < raumAnzahl)
        {
          int index = -1;
          for (int i = 0; i < checkRaumBis; i++) if (tmpCheckRaumPosis[i] == raumOben[pp]) index = i;
          if (index >= 0 && tmpCheckRaumTiefe[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "d" + findSteps;
              return raumOben[pp];
            }
          }
        }

        if (raumUnten[pp] < raumAnzahl)
        {
          int index = -1;
          for (int i = 0; i < checkRaumBis; i++) if (tmpCheckRaumPosis[i] == raumUnten[pp]) index = i;
          if (index >= 0 && tmpCheckRaumTiefe[index] == tt)
          {
            if (tt > 0 || index == 0)
            {
              findSteps = "u" + findSteps;
              return raumUnten[pp];
            }
          }
        }

        return tmpCheckRaumPosis[0];
      };

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];
        int pTiefe = tmpCheckRaumTiefe[checkRaumVon] + 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpRaumCheckFertig[p = raumLinks[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumLinks[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                                    // Spieler nach links bewegen

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, boxesCount).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
              {
                int pp = raumRechts[p];
                int tt = pTiefe - 1;
                findSteps = "L";
                while (pp != tmpCheckRaumPosis[0])
                {
                  tt--;
                  pp = vorherFeld(pp, tt);
                }
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach rechts bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpRaumCheckFertig[p = raumRechts[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumRechts[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                                    // Spieler nach rechts bewegen

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, boxesCount).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
              {
                int pp = raumLinks[p];
                int tt = pTiefe - 1;
                findSteps = "R";
                while (pp != tmpCheckRaumPosis[0])
                {
                  tt--;
                  pp = vorherFeld(pp, tt);
                }
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach links bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpRaumCheckFertig[p = raumOben[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumOben[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] > 0 && kistenZuRaum[raumZuKisten[p2] - 1] > p2 && raumZuKisten[p2] < boxesCount)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] - 1];
                kistenZuRaum[raumZuKisten[p2]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p2;
              }
              #endregion

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, boxesCount).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
              {
                int pp = raumUnten[p];
                int tt = pTiefe - 1;
                findSteps = "U";
                while (pp != tmpCheckRaumPosis[0])
                {
                  tt--;
                  pp = vorherFeld(pp, tt);
                }
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach unten bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] < boxesCount - 1 && kistenZuRaum[raumZuKisten[p] + 1] < p)
              {
                int tmp = kistenZuRaum[raumZuKisten[p] + 1];
                kistenZuRaum[raumZuKisten[p]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpRaumCheckFertig[p = raumUnten[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumUnten[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] < boxesCount - 1 && kistenZuRaum[raumZuKisten[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] + 1];
                kistenZuRaum[raumZuKisten[p2]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p2;
              }
              #endregion

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, boxesCount).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
              {
                int pp = raumOben[p];
                int tt = pTiefe - 1;
                findSteps = "D";
                while (pp != tmpCheckRaumPosis[0])
                {
                  tt--;
                  pp = vorherFeld(pp, tt);
                }
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach oben bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] > 0 && kistenZuRaum[raumZuKisten[p] - 1] > p && raumZuKisten[p] < boxesCount)
              {
                int tmp = kistenZuRaum[raumZuKisten[p] - 1];
                kistenZuRaum[raumZuKisten[p]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }

      raumSpielerPos = tmpCheckRaumPosis[0]; // alte Spielerposition wieder herstellen

      return findSteps;
    }

    #region # public IEnumerable<SokowahnStellung> GetVarianten() // ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// <summary>
    /// ermittelt alle möglichen Zugvarianten und gibt deren Stellungen zurück
    /// </summary>
    /// <returns>Enumerable der gefundenen Stellungen</returns>
    public IEnumerable<SokowahnPosition> GetVarianten()
    {
      int checkRaumVon = 0;
      int checkRaumBis = 0;

      Array.Clear(tmpRaumCheckFertig, 0, raumAnzahl);

      // erste Spielerposition hinzufügen
      tmpRaumCheckFertig[raumSpielerPos] = true;
      tmpCheckRaumPosis[checkRaumBis] = raumSpielerPos;
      tmpCheckRaumTiefe[checkRaumBis] = spielerZugTiefe;
      checkRaumBis++;

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];
        int pTiefe = tmpCheckRaumTiefe[checkRaumVon] + 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpRaumCheckFertig[p = raumLinks[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumLinks[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                                    // Spieler nach links bewegen

              //       if (!IstBlocker())
              {
                yield return new SokowahnPosition { roomPlayerPos = raumSpielerPos, boxesToRoom = TeilArray(kistenZuRaum, boxesCount), crc64 = Crc, calcDepth = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach rechts bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpRaumCheckFertig[p = raumRechts[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumRechts[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                                    // Spieler nach rechts bewegen

              //       if (!IstBlocker())
              {
                yield return new SokowahnPosition { roomPlayerPos = raumSpielerPos, boxesToRoom = TeilArray(kistenZuRaum, boxesCount), crc64 = Crc, calcDepth = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach links bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpRaumCheckFertig[p = raumOben[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumOben[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] > 0 && kistenZuRaum[raumZuKisten[p2] - 1] > p2 && raumZuKisten[p2] < boxesCount)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] - 1];
                kistenZuRaum[raumZuKisten[p2]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p2;
              }
              #endregion

              //       if (!IstBlocker())
              {
                yield return new SokowahnPosition { roomPlayerPos = raumSpielerPos, boxesToRoom = TeilArray(kistenZuRaum, boxesCount), crc64 = Crc, calcDepth = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach unten bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] < boxesCount - 1 && kistenZuRaum[raumZuKisten[p] + 1] < p)
              {
                int tmp = kistenZuRaum[raumZuKisten[p] + 1];
                kistenZuRaum[raumZuKisten[p]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpRaumCheckFertig[p = raumUnten[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumUnten[p]] == boxesCount && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = boxesCount; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] < boxesCount - 1 && kistenZuRaum[raumZuKisten[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] + 1];
                kistenZuRaum[raumZuKisten[p2]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p2;
              }
              #endregion

              //       if (!IstBlocker())
              {
                yield return new SokowahnPosition { roomPlayerPos = raumSpielerPos, boxesToRoom = TeilArray(kistenZuRaum, boxesCount), crc64 = Crc, calcDepth = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach oben bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = boxesCount; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] > 0 && kistenZuRaum[raumZuKisten[p] - 1] > p && raumZuKisten[p] < boxesCount)
              {
                int tmp = kistenZuRaum[raumZuKisten[p] - 1];
                kistenZuRaum[raumZuKisten[p]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }

      raumSpielerPos = tmpCheckRaumPosis[0]; // alte Spielerposition wieder herstellen
    }
    #endregion

    #region # public IEnumerable<SokowahnStellung> GetVariantenRückwärts() // ermittelt alle möglichen Zugvarianten, welche vor dieser Stellung existiert haben könnten und gibt deren Stellungen zurück
    /// <summary>
    /// ermittelt alle möglichen Zugvarianten, welche vor dieser Stellung existiert haben könnten und gibt deren Stellungen zurück
    /// </summary>
    /// <returns>alle möglichen Vorgänge-Zugvarianten</returns>
    public IEnumerable<SokowahnPosition> GetVariantenRückwärts()
    {
      int pMitte = raumSpielerPos;
      int pLinks = raumLinks[pMitte];
      int pRechts = raumRechts[pMitte];
      int pOben = raumOben[pMitte];
      int pUnten = raumUnten[pMitte];

      #region # // --- Links-Vermutung: Kiste wurde das letztes mal nach links geschoben ---
      if (raumZuKisten[pLinks] < boxesCount && pRechts < raumAnzahl && raumZuKisten[pRechts] == boxesCount)
      {
        raumSpielerPos = pRechts; // Spieler zurück nach rechts bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pLinks]] = pMitte; raumZuKisten[pLinks] = boxesCount; // linke Kiste eins zurück nach rechts schieben

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pLinks] = raumZuKisten[pMitte]] = pLinks; raumZuKisten[pMitte] = boxesCount; // linke Kiste weiter nach links schieben
        raumSpielerPos = pMitte; // Spieler nach links bewegen
      }
      #endregion

      #region # // --- Rechts-Vermutung: Kiste wurde das letztes mal nach rechts geschoben ---
      if (raumZuKisten[pRechts] < boxesCount && pLinks < raumAnzahl && raumZuKisten[pLinks] == boxesCount)
      {
        raumSpielerPos = pLinks; // Spieler zurück nach links bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pRechts]] = pMitte; raumZuKisten[pRechts] = boxesCount; // rechte Kiste eins zurück nach links schieben

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pRechts] = raumZuKisten[pMitte]] = pRechts; raumZuKisten[pMitte] = boxesCount; // rechte Kiste weiter nach rechts schieben
        raumSpielerPos = pMitte; // Spieler nach rechts bewegen
      }
      #endregion

      #region # // --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
      if (raumZuKisten[pOben] < boxesCount && pUnten < raumAnzahl && raumZuKisten[pUnten] == boxesCount)
      {
        raumSpielerPos = pUnten; // Spieler zurück nach unten bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pOben]] = pMitte; raumZuKisten[pOben] = boxesCount; // obere Kiste eins zurück nach unten schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKisten[pMitte] < boxesCount - 1 && kistenZuRaum[raumZuKisten[pMitte] + 1] < pMitte)
        {
          int tmp = kistenZuRaum[raumZuKisten[pMitte] + 1];
          kistenZuRaum[raumZuKisten[pMitte]++] = tmp;
          kistenZuRaum[raumZuKisten[tmp]--] = pMitte;
        }
        #endregion

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pOben] = raumZuKisten[pMitte]] = pOben; raumZuKisten[pMitte] = boxesCount; // obere Kiste weiter nach oben schieben
        raumSpielerPos = pMitte; // Spieler nach oben bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKisten[pOben] > 0 && kistenZuRaum[raumZuKisten[pOben] - 1] > pOben && raumZuKisten[pOben] < boxesCount)
        {
          int tmp = kistenZuRaum[raumZuKisten[pOben] - 1];
          kistenZuRaum[raumZuKisten[pOben]--] = tmp;
          kistenZuRaum[raumZuKisten[tmp]++] = pOben;
        }
        #endregion
      }
      #endregion

      #region # // --- Unten-Vermutung: Kiste wurde das letztes mal nach unten geschoben ---
      if (raumZuKisten[pUnten] < boxesCount && pOben < raumAnzahl && raumZuKisten[pOben] == boxesCount)
      {
        raumSpielerPos = pOben; // Spieler zurück nach oben bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pUnten]] = pMitte; raumZuKisten[pUnten] = boxesCount; // untere Kiste eins zurück nach oben schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKisten[pMitte] > 0 && kistenZuRaum[raumZuKisten[pMitte] - 1] > pMitte && raumZuKisten[pMitte] < boxesCount)
        {
          int tmp = kistenZuRaum[raumZuKisten[pMitte] - 1];
          kistenZuRaum[raumZuKisten[pMitte]--] = tmp;
          kistenZuRaum[raumZuKisten[tmp]++] = pMitte;
        }
        #endregion

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pUnten] = raumZuKisten[pMitte]] = pUnten; raumZuKisten[pMitte] = boxesCount; // untere Kiste weiter nach unten schieben
        raumSpielerPos = pMitte; // Spieler nach unten bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKisten[pUnten] < boxesCount - 1 && kistenZuRaum[raumZuKisten[pUnten] + 1] < pUnten)
        {
          int tmp = kistenZuRaum[raumZuKisten[pUnten] + 1];
          kistenZuRaum[raumZuKisten[pUnten]++] = tmp;
          kistenZuRaum[raumZuKisten[tmp]--] = pUnten;
        }
        #endregion
      }
      #endregion
    }
    #endregion
    #region # IEnumerable<SokowahnStellung> GetVariantenRückwärtsTeil() // Hilfsmethode für GetVariantenRückwärts(), berechnet eine bestimmte Richtung
    /// <summary>
    /// Hilfsmethode für GetVariantenRückwärts(), berechnet eine bestimmte Richtung
    /// </summary>
    /// <returns>gefundene gültige Stellungen</returns>
    IEnumerable<SokowahnPosition> GetVariantenRückwärtsTeil()
    {
      int checkRaumVon = 0;
      int checkRaumBis = 0;

      Array.Clear(tmpRaumCheckFertig, 0, raumAnzahl);

      // erste Spielerposition hinzufügen
      tmpRaumCheckFertig[raumSpielerPos] = true;
      tmpCheckRaumPosis[checkRaumBis] = raumSpielerPos;
      tmpCheckRaumTiefe[checkRaumBis] = spielerZugTiefe;
      checkRaumBis++;

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];
        int pTiefe = tmpCheckRaumTiefe[checkRaumVon] - 1;

        int p, p2;

        #region # // --- links ---
        if (!tmpRaumCheckFertig[p = raumLinks[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount)
          {
            if ((p2 = raumRechts[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == boxesCount)
            {
              yield return new SokowahnPosition { boxesToRoom = kistenZuRaum.ToArray(), crc64 = Crc, roomPlayerPos = raumSpielerPos, calcDepth = pTiefe };
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if (!tmpRaumCheckFertig[p = raumRechts[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount)
          {
            if ((p2 = raumLinks[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == boxesCount)
            {
              yield return new SokowahnPosition { boxesToRoom = kistenZuRaum.ToArray(), crc64 = Crc, roomPlayerPos = raumSpielerPos, calcDepth = pTiefe };
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if (!tmpRaumCheckFertig[p = raumOben[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount)
          {
            if ((p2 = raumUnten[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == boxesCount)
            {
              yield return new SokowahnPosition { boxesToRoom = kistenZuRaum.ToArray(), crc64 = Crc, roomPlayerPos = raumSpielerPos, calcDepth = pTiefe };
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if (!tmpRaumCheckFertig[p = raumUnten[raumSpielerPos]])
        {
          if (raumZuKisten[p] < boxesCount)
          {
            if ((p2 = raumOben[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == boxesCount)
            {
              yield return new SokowahnPosition { boxesToRoom = kistenZuRaum.ToArray(), crc64 = Crc, roomPlayerPos = raumSpielerPos, calcDepth = pTiefe };
            }
          }
          else
          {
            tmpRaumCheckFertig[p] = true;
            tmpCheckRaumPosis[checkRaumBis] = p;
            tmpCheckRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }
    }
    #endregion

    #region # public IEnumerable<SokowahnStellung> GetVariantenBlockerZiele() // ermittelt alle Ziel-Varianten, wo der Spieler stehen kann
    /// <summary>
    /// ermittelt alle Ziel-Varianten, wo der Spieler stehen kann
    /// </summary>
    /// <returns>gefundene mögliche Stellungen</returns>
    public IEnumerable<SokowahnPosition> GetVariantenBlockerZiele()
    {
      for (int kiste = 0; kiste < boxesCount; kiste++)
      {
        int pKiste = kistenZuRaum[kiste];
        int pSpieler;

        if ((pSpieler = raumLinks[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == boxesCount)
        {
          raumSpielerPos = pSpieler;
          //     if (GetVariantenRückwärts().Count() > 0) 
          yield return GetStellung();
        }

        if ((pSpieler = raumRechts[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == boxesCount)
        {
          raumSpielerPos = pSpieler;
          yield return GetStellung();
        }

        if ((pSpieler = raumOben[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == boxesCount)
        {
          raumSpielerPos = pSpieler;
          yield return GetStellung();
        }

        if ((pSpieler = raumUnten[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == boxesCount)
        {
          raumSpielerPos = pSpieler;
          yield return GetStellung();
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
      SokowahnField tmp = new SokowahnField(this);

      tmp.LadeStellung(data, offset, 0);

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
      SokowahnField tmp = new SokowahnField(this);

      tmp.LadeStellung(data, offset, 0);

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

      tmp.LadeStellung(data, offset, zugTiefe);

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

      tmp.LadeStellung(data, offset, zugTiefe);

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
      feldData = field.feldData;
      feldBreite = field.feldBreite;
      raumAnzahl = field.raumAnzahl;
      raumLinks = field.raumLinks;
      raumRechts = field.raumRechts;
      raumOben = field.raumOben;
      raumUnten = field.raumUnten;

      raumSpielerPos = field.raumSpielerPos;
      spielerZugTiefe = field.spielerZugTiefe;
      raumZuKisten = field.raumZuKisten.ToArray(); // Kopie erstellen
      boxesCount = field.boxesCount;
      kistenZuRaum = field.kistenZuRaum.ToArray(); // Kopie erstellen

      tmpCheckRaumPosis = new int[raumAnzahl];
      tmpCheckRaumTiefe = new int[raumAnzahl];
      tmpRaumCheckFertig = new bool[raumAnzahl + 1];
      tmpRaumCheckFertig[raumAnzahl] = true; // Ende-Feld schon auf fertig setzen
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="feldData">Daten des Spielfeldes</param>
    /// <param name="feldBreite">Breite des Spielfeldes</param>
    public SokowahnField(char[] feldData, int feldBreite)
    {
      this.feldData = feldData;
      this.feldBreite = feldBreite;

      var spielerRaum = SpielfeldRaumScan(feldData, feldBreite);

      int raumAnzahl = this.raumAnzahl = spielerRaum.Count(x => x);

      var raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();

      raumLinks = raumZuFeld.Select(i => spielerRaum[i - 1] ? raumZuFeld.ToList().IndexOf(i - 1) : raumAnzahl).ToArray();
      raumRechts = raumZuFeld.Select(i => spielerRaum[i + 1] ? raumZuFeld.ToList().IndexOf(i + 1) : raumAnzahl).ToArray();
      raumOben = raumZuFeld.Select(i => spielerRaum[i - feldBreite] ? raumZuFeld.ToList().IndexOf(i - feldBreite) : raumAnzahl).ToArray();
      raumUnten = raumZuFeld.Select(i => spielerRaum[i + feldBreite] ? raumZuFeld.ToList().IndexOf(i + feldBreite) : raumAnzahl).ToArray();

      kistenZuRaum = Enumerable.Range(0, raumAnzahl).Where(i => spielerRaum[raumZuFeld[i]] && (feldData[raumZuFeld[i]] == '$' || feldData[raumZuFeld[i]] == '*')).ToArray();
      int kistenAnzahl = boxesCount = kistenZuRaum.Length;
      int counter = 0;
      raumZuKisten = raumZuFeld.Select(i => (feldData[i] == '$' || feldData[i] == '*') ? counter++ : kistenAnzahl).ToArray();
      Array.Resize(ref raumZuKisten, raumAnzahl + 1);
      raumZuKisten[raumAnzahl] = kistenAnzahl;

      raumSpielerPos = Enumerable.Range(0, raumAnzahl).First(i => feldData[raumZuFeld[i]] == '@' || feldData[raumZuFeld[i]] == '+');
      spielerZugTiefe = 0;

      tmpCheckRaumPosis = new int[raumAnzahl];
      tmpCheckRaumTiefe = new int[raumAnzahl];
      tmpRaumCheckFertig = new bool[raumAnzahl + 1];
      tmpRaumCheckFertig[raumAnzahl] = true; // Ende-Feld schon auf fertig setzen
    }
    #endregion

    #region # // --- SokowahnStaticTools ---
    /// <summary>
    /// ermittelt die Felder, wo der Spieler sich aufhalten darf
    /// </summary>
    /// <param name="feldData">Felddaten des Spielfeldes als char-Array</param>
    /// <param name="feldBreite">Breite des Feldes (Höhe wird automatisch ermittelt)</param>
    /// <returns>Bool-Array mit gleicher Größe wie feldData, gibt an, wo der Spieler sich aufhalten darf</returns>
    public static bool[] SpielfeldRaumScan(char[] feldData, int feldBreite)
    {
      int feldHöhe = feldData.Length / feldBreite;

      var spielerRaum = feldData.Select(c => c == '@' || c == '+').ToArray();

      bool find = true;
      while (find)
      {
        find = false;
        for (int y = 1; y < feldHöhe - 1; y++)
        {
          for (int x = 1; x < feldBreite - 1; x++)
          {
            if (spielerRaum[x + y * feldBreite])
            {
              int p = x + y * feldBreite - feldBreite;
              if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
              p += feldBreite - 1;
              if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
              p += 2;
              if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
              p += feldBreite - 1;
              if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
            }
          }
        }
      }

      return spielerRaum;
    }
    #endregion

    #region # // --- Extensions ---
    /// <summary>
    /// erstellt eine gekürzte Kopie eines Arrays
    /// </summary>
    /// <param name="array">Array mit den entsprechenden Daten</param>
    /// <param name="anzahl">Anzahl der Datensätze (darf maximale so lang sein wie das Array selbst)</param>
    /// <returns>neues Array mit den entsprechenden Daten</returns>
    public static T[] TeilArray<T>(T[] array, int anzahl) where T : struct
    {
      var ausgabe = new T[anzahl];

      for (int i = 0; i < anzahl; i++) ausgabe[i] = array[i];

      return ausgabe;
    }
    #endregion
  }
}
