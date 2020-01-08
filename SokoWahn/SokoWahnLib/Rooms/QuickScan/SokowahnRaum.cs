#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable AccessToModifiedClosure
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// enthält ein eigenes Spielfeld-System und mehrere Methoden zum schnellen suchen von Zugmöglichkeiten in beide Richtungen
  /// </summary>
  public class SokowahnRaum
  {
    #region # // --- statische Werte (welche sich nicht ändern) ---
    /// <summary>
    /// merkt sich die Grunddaten des Spielfeldes
    /// </summary>
    char[] feldData;

    /// <summary>
    /// Breite des Spielfeldes
    /// </summary>
    int feldBreite;

    /// <summary>
    /// merkt sich die Anzahl der begehbaren Bereiche
    /// </summary>
    int raumAnzahl;

    /// <summary>
    /// zeigt auf das linke benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    public int[] raumLinks;

    /// <summary>
    /// zeigt auf das rechts benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    public int[] raumRechts;

    /// <summary>
    /// zeigt auf das obere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    public int[] raumOben;

    /// <summary>
    /// zeigt auf das untere benachbarte Raum-Feld (raumAnzahl = wenn das Feld nicht begehbar ist)
    /// </summary>
    public int[] raumUnten;

    /// <summary>
    /// Anzahl der Kisten, welche auf dem Spielfeld liegen (kann bei der Blocker-Suche niedriger sein als die eigentliche Ziele-Anzahl)
    /// </summary>
    int kistenAnzahl;
    #endregion

    #region # // --- dynamische Werte ---
    /// <summary>
    /// aktuelle Spielerposition im SokowahnRaum
    /// </summary>
    public int raumSpielerPos;

    /// <summary>
    /// gibt bei den begehbaren Raumbereichen an, welche Kiste sich dort befindet (Wert = kistenAnzahl, keine Kiste steht auf dem Feld)
    /// </summary>
    int[] raumZuKisten;

    /// <summary>
    /// enthält die Kistenpositionen (Wert = raumAnzahl, Kiste steht nirgendwo, ist also nicht vorhanden)
    /// </summary>
    public int[] kistenZuRaum;

    /// <summary>
    /// merkt sich die aktuelle Zugtiefe
    /// </summary>
    public int spielerZugTiefe;
    #endregion

    #region # // --- temporäre Werte ---
    /// <summary>
    /// merkt sich temporär, welche Positionen geprüft wurden bzw. noch geprüft werden müssen
    /// </summary>
    int[] tmpCheckRaumPosis;

    /// <summary>
    /// merkt sich temporär, welche Zugtiefen erreicht wurden
    /// </summary>
    int[] tmpCheckRaumTiefe;

    /// <summary>
    /// merkt sich temporär, welche Felder im Raum bereits abgelaufen wurden
    /// </summary>
    bool[] tmpRaumCheckFertig;
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

        for (int i = 0; i < kistenAnzahl; i++)
        {
          ergebnis = (ergebnis ^ (ulong)kistenZuRaum[i]) * 0x100000001b3;
        }

        return ergebnis;
      }
    }

    /// <summary>
    /// gibt an, ob der kompakte Byte-Modus erlaubt ist (sonst nur der ushort-Modus, sofern mehr als 254 begehbare Felder vorhanden sind)
    /// </summary>
    public bool ByteModusErlaubt
    {
      get
      {
        return raumAnzahl < 255;
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
    public int KistenAnzahl
    {
      get
      {
        return kistenAnzahl;
      }
      set
      {
        kistenAnzahl = value;

        // alle Kisten entfernen
        for (int i = 0; i < raumAnzahl; i++) raumZuKisten[i] = kistenAnzahl;
        kistenZuRaum = Enumerable.Range(0, kistenAnzahl).Select(i => i).ToArray();

        for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = i; // Kisten neu sinnlos auf das Feld setzen
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
      char[] feldLeer = FeldDataLeer;

      int feldHöhe = feldData.Length / feldBreite;

      bool[] spielerRaum = SpielfeldRaumScan(feldData, feldBreite);
      int raumAnzahl = spielerRaum.Count(x => x);
      int[] raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();
      int[] feldZuRaum = Enumerable.Range(0, feldData.Length).Select(i => spielerRaum[i] ? raumZuFeld.ToList().IndexOf(i) : -1).ToArray();

      int[] raumZuKisten = this.raumZuKisten.ToArray();
      int kistenAnzahl = this.kistenAnzahl;
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
    public SokowahnStellung GetStellung()
    {
      return new SokowahnStellung { raumSpielerPos = raumSpielerPos, kistenZuRaum = kistenZuRaum.ToArray(), crc64 = Crc, zugTiefe = spielerZugTiefe };
    }
    #endregion

    #region # public void LadeStellung(*[] datenArray, int off, int spielerZugTiefe) // lädt eine bestimmte Stellung
    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array mit entsprechenden Daten</param>
    /// <param name="off">Position im Array, wo die Daten liegen</param>
    /// <param name="spielerZugTiefe">Zugtiefe der Stellung</param>
    public void LadeStellung(byte[] datenArray, int off, int zugTiefe)
    {
      // neue Spielerposition setzen
      raumSpielerPos = (int)datenArray[off++];
      this.spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++)
      {
        int p = (int)datenArray[off + i];
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
      raumSpielerPos = (int)datenArray[0];
      this.spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++)
      {
        int p = (int)datenArray[i + 1];
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
      raumSpielerPos = (int)datenArray[off++];
      this.spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++)
      {
        int p = (int)datenArray[off + i];
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
      raumSpielerPos = (int)datenArray[0];
      this.spielerZugTiefe = zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++)
      {
        int p = (int)datenArray[i + 1];
        kistenZuRaum[i] = p;
        raumZuKisten[p] = i;
      }
    }

    /// <summary>
    /// lädt eine bestimmte Stellung
    /// </summary>
    /// <param name="stellung">Stellung, welche geladen werden soll</param>
    public void LadeStellung(SokowahnStellung stellung)
    {
      raumSpielerPos = stellung.raumSpielerPos;
      spielerZugTiefe = stellung.zugTiefe;

      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i] = stellung.kistenZuRaum[i]] = i;
    }

    /// <summary>
    /// Sondervariante für das Blocker-System (ohne setzen der Spielerposition)
    /// </summary>
    /// <param name="sammlerKistenIndex">Kisten-Index für sammlerKistenRaum (Länge = sammlerKistenAnzahl)</param>
    /// <param name="sammlerKistenRaum">Raumpositionen der einzelnen Kisten (Länge = basisKistenAnzahl)</param>
    public void LadeStellung(int[] sammlerKistenIndex, int[] sammlerKistenRaum)
    {
      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i]] = kistenAnzahl;

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKisten[kistenZuRaum[i] = sammlerKistenRaum[sammlerKistenIndex[i]]] = i;
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
      for (int i = 0; i < kistenAnzahl; i++) datenArray[off++] = (byte)kistenZuRaum[i];
    }

    /// <summary>
    /// speichert eine bestimmte Stellung
    /// </summary>
    /// <param name="datenArray">Array, in dem die Daten gespeichert werden sollen</param>
    /// <param name="off">Position im Array, wo die Daten gespeichert werden sollen</param>
    public void SpeichereStellung(ushort[] datenArray, int off)
    {
      datenArray[off++] = (ushort)raumSpielerPos;
      for (int i = 0; i < kistenAnzahl; i++) datenArray[off++] = (ushort)kistenZuRaum[i];
    }
    #endregion

    /// <summary>
    /// berechnet die Lauf-Schritte bis zur nächsten Kisten-Bewegung
    /// </summary>
    /// <param name="next">nachfolgendes Spielfeld</param>
    /// <returns>Schritte um das Spielfeld zu erreichen</returns>
    public string GetSteps(SokowahnRaum next)
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumLinks[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                                    // Spieler nach links bewegen

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, kistenAnzahl).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
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
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // linke Kiste eins zurück nach rechts schieben
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumRechts[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                                    // Spieler nach rechts bewegen

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, kistenAnzahl).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
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
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // rechte Kiste eins zurück nach links schieben
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumOben[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] > 0 && kistenZuRaum[raumZuKisten[p2] - 1] > p2 && raumZuKisten[p2] < kistenAnzahl)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] - 1];
                kistenZuRaum[raumZuKisten[p2]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p2;
              }
              #endregion

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, kistenAnzahl).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
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
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[p] + 1] < p)
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumUnten[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] + 1];
                kistenZuRaum[raumZuKisten[p2]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p2;
              }
              #endregion

              if (raumSpielerPos == spielerZiel && Enumerable.Range(0, kistenAnzahl).All(i => kistenZuRaum[i] == next.kistenZuRaum[i]))
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
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] > 0 && kistenZuRaum[raumZuKisten[p] - 1] > p && raumZuKisten[p] < kistenAnzahl)
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
    public IEnumerable<SokowahnStellung> GetVarianten()
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumLinks[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                                    // Spieler nach links bewegen

              //       if (!IstBlocker())
              {
                yield return new SokowahnStellung { raumSpielerPos = raumSpielerPos, kistenZuRaum = TeilArray(kistenZuRaum, kistenAnzahl), crc64 = Crc, zugTiefe = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach rechts bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // linke Kiste eins zurück nach rechts schieben
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumRechts[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                                    // Spieler nach rechts bewegen

              //       if (!IstBlocker())
              {
                yield return new SokowahnStellung { raumSpielerPos = raumSpielerPos, kistenZuRaum = TeilArray(kistenZuRaum, kistenAnzahl), crc64 = Crc, zugTiefe = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach links bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // rechte Kiste eins zurück nach links schieben
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumOben[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                                    // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] > 0 && kistenZuRaum[raumZuKisten[p2] - 1] > p2 && raumZuKisten[p2] < kistenAnzahl)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] - 1];
                kistenZuRaum[raumZuKisten[p2]--] = tmp;
                kistenZuRaum[raumZuKisten[tmp]++] = p2;
              }
              #endregion

              //       if (!IstBlocker())
              {
                yield return new SokowahnStellung { raumSpielerPos = raumSpielerPos, kistenZuRaum = TeilArray(kistenZuRaum, kistenAnzahl), crc64 = Crc, zugTiefe = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach unten bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[p] + 1] < p)
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
          if (raumZuKisten[p] < kistenAnzahl) // steht eine Kiste auf den benachbarten Feld?
          {
            if (raumZuKisten[p2 = raumUnten[p]] == kistenAnzahl && p2 < raumAnzahl) // Feld hinter der Kiste frei?
            {
              kistenZuRaum[raumZuKisten[p2] = raumZuKisten[p]] = p2; raumZuKisten[p] = kistenAnzahl; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                                    // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKisten[p2] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKisten[p2] + 1];
                kistenZuRaum[raumZuKisten[p2]++] = tmp;
                kistenZuRaum[raumZuKisten[tmp]--] = p2;
              }
              #endregion

              //       if (!IstBlocker())
              {
                yield return new SokowahnStellung { raumSpielerPos = raumSpielerPos, kistenZuRaum = TeilArray(kistenZuRaum, kistenAnzahl), crc64 = Crc, zugTiefe = pTiefe };
              }

              raumSpielerPos = tmpCheckRaumPosis[checkRaumVon];                                      // Spieler zurück nach oben bewegen
              kistenZuRaum[raumZuKisten[p] = raumZuKisten[p2]] = p; raumZuKisten[p2] = kistenAnzahl; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKisten[p] > 0 && kistenZuRaum[raumZuKisten[p] - 1] > p && raumZuKisten[p] < kistenAnzahl)
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
    public IEnumerable<SokowahnStellung> GetVariantenRückwärts()
    {
      int pMitte = raumSpielerPos;
      int pLinks = raumLinks[pMitte];
      int pRechts = raumRechts[pMitte];
      int pOben = raumOben[pMitte];
      int pUnten = raumUnten[pMitte];

      #region # // --- Links-Vermutung: Kiste wurde das letztes mal nach links geschoben ---
      if (raumZuKisten[pLinks] < kistenAnzahl && pRechts < raumAnzahl && raumZuKisten[pRechts] == kistenAnzahl)
      {
        raumSpielerPos = pRechts; // Spieler zurück nach rechts bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pLinks]] = pMitte; raumZuKisten[pLinks] = kistenAnzahl; // linke Kiste eins zurück nach rechts schieben

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pLinks] = raumZuKisten[pMitte]] = pLinks; raumZuKisten[pMitte] = kistenAnzahl; // linke Kiste weiter nach links schieben
        raumSpielerPos = pMitte; // Spieler nach links bewegen
      }
      #endregion

      #region # // --- Rechts-Vermutung: Kiste wurde das letztes mal nach rechts geschoben ---
      if (raumZuKisten[pRechts] < kistenAnzahl && pLinks < raumAnzahl && raumZuKisten[pLinks] == kistenAnzahl)
      {
        raumSpielerPos = pLinks; // Spieler zurück nach links bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pRechts]] = pMitte; raumZuKisten[pRechts] = kistenAnzahl; // rechte Kiste eins zurück nach links schieben

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pRechts] = raumZuKisten[pMitte]] = pRechts; raumZuKisten[pMitte] = kistenAnzahl; // rechte Kiste weiter nach rechts schieben
        raumSpielerPos = pMitte; // Spieler nach rechts bewegen
      }
      #endregion

      #region # // --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
      if (raumZuKisten[pOben] < kistenAnzahl && pUnten < raumAnzahl && raumZuKisten[pUnten] == kistenAnzahl)
      {
        raumSpielerPos = pUnten; // Spieler zurück nach unten bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pOben]] = pMitte; raumZuKisten[pOben] = kistenAnzahl; // obere Kiste eins zurück nach unten schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKisten[pMitte] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[pMitte] + 1] < pMitte)
        {
          int tmp = kistenZuRaum[raumZuKisten[pMitte] + 1];
          kistenZuRaum[raumZuKisten[pMitte]++] = tmp;
          kistenZuRaum[raumZuKisten[tmp]--] = pMitte;
        }
        #endregion

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pOben] = raumZuKisten[pMitte]] = pOben; raumZuKisten[pMitte] = kistenAnzahl; // obere Kiste weiter nach oben schieben
        raumSpielerPos = pMitte; // Spieler nach oben bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKisten[pOben] > 0 && kistenZuRaum[raumZuKisten[pOben] - 1] > pOben && raumZuKisten[pOben] < kistenAnzahl)
        {
          int tmp = kistenZuRaum[raumZuKisten[pOben] - 1];
          kistenZuRaum[raumZuKisten[pOben]--] = tmp;
          kistenZuRaum[raumZuKisten[tmp]++] = pOben;
        }
        #endregion
      }
      #endregion

      #region # // --- Unten-Vermutung: Kiste wurde das letztes mal nach unten geschoben ---
      if (raumZuKisten[pUnten] < kistenAnzahl && pOben < raumAnzahl && raumZuKisten[pOben] == kistenAnzahl)
      {
        raumSpielerPos = pOben; // Spieler zurück nach oben bewegen
        kistenZuRaum[raumZuKisten[pMitte] = raumZuKisten[pUnten]] = pMitte; raumZuKisten[pUnten] = kistenAnzahl; // untere Kiste eins zurück nach oben schieben
        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKisten[pMitte] > 0 && kistenZuRaum[raumZuKisten[pMitte] - 1] > pMitte && raumZuKisten[pMitte] < kistenAnzahl)
        {
          int tmp = kistenZuRaum[raumZuKisten[pMitte] - 1];
          kistenZuRaum[raumZuKisten[pMitte]--] = tmp;
          kistenZuRaum[raumZuKisten[tmp]++] = pMitte;
        }
        #endregion

        foreach (var variante in GetVariantenRückwärtsTeil()) yield return variante;

        kistenZuRaum[raumZuKisten[pUnten] = raumZuKisten[pMitte]] = pUnten; raumZuKisten[pMitte] = kistenAnzahl; // untere Kiste weiter nach unten schieben
        raumSpielerPos = pMitte; // Spieler nach unten bewegen
        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKisten[pUnten] < kistenAnzahl - 1 && kistenZuRaum[raumZuKisten[pUnten] + 1] < pUnten)
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
    IEnumerable<SokowahnStellung> GetVariantenRückwärtsTeil()
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
          if (raumZuKisten[p] < kistenAnzahl)
          {
            if ((p2 = raumRechts[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == kistenAnzahl)
            {
              yield return new SokowahnStellung { kistenZuRaum = kistenZuRaum.ToArray(), crc64 = Crc, raumSpielerPos = raumSpielerPos, zugTiefe = pTiefe };
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
          if (raumZuKisten[p] < kistenAnzahl)
          {
            if ((p2 = raumLinks[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == kistenAnzahl)
            {
              yield return new SokowahnStellung { kistenZuRaum = kistenZuRaum.ToArray(), crc64 = Crc, raumSpielerPos = raumSpielerPos, zugTiefe = pTiefe };
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
          if (raumZuKisten[p] < kistenAnzahl)
          {
            if ((p2 = raumUnten[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == kistenAnzahl)
            {
              yield return new SokowahnStellung { kistenZuRaum = kistenZuRaum.ToArray(), crc64 = Crc, raumSpielerPos = raumSpielerPos, zugTiefe = pTiefe };
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
          if (raumZuKisten[p] < kistenAnzahl)
          {
            if ((p2 = raumOben[raumSpielerPos]) < raumAnzahl && raumZuKisten[p2] == kistenAnzahl)
            {
              yield return new SokowahnStellung { kistenZuRaum = kistenZuRaum.ToArray(), crc64 = Crc, raumSpielerPos = raumSpielerPos, zugTiefe = pTiefe };
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
    public IEnumerable<SokowahnStellung> GetVariantenBlockerZiele()
    {
      for (int kiste = 0; kiste < kistenAnzahl; kiste++)
      {
        int pKiste = kistenZuRaum[kiste];
        int pSpieler;

        if ((pSpieler = raumLinks[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == kistenAnzahl)
        {
          raumSpielerPos = pSpieler;
          //     if (GetVariantenRückwärts().Count() > 0) 
          yield return GetStellung();
        }

        if ((pSpieler = raumRechts[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == kistenAnzahl)
        {
          raumSpielerPos = pSpieler;
          yield return GetStellung();
        }

        if ((pSpieler = raumOben[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == kistenAnzahl)
        {
          raumSpielerPos = pSpieler;
          yield return GetStellung();
        }

        if ((pSpieler = raumUnten[pKiste]) < raumAnzahl && raumZuKisten[pSpieler] == kistenAnzahl)
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
    /// <param name="stellung">Stellung, welche ausgelesen werden soll</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(SokowahnStellung stellung)
    {
      ushort[] tmpData = new ushort[kistenAnzahl + 1];
      stellung.SpeichereStellung(tmpData, 0);
      return Debug(tmpData, 0, stellung.zugTiefe);
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(byte[] data, int offset)
    {
      SokowahnRaum tmp = new SokowahnRaum(this);

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
      SokowahnRaum tmp = new SokowahnRaum(this);

      tmp.LadeStellung(data, offset, 0);

      return tmp.ToString();
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(byte[] data, int offset, int zugTiefe)
    {
      SokowahnRaum tmp = new SokowahnRaum(this);

      tmp.LadeStellung(data, offset, zugTiefe);

      return tmp.ToString();
    }

    /// <summary>
    /// gibt eine bestimmte Stellung direkt als sichtbaren String aus (ohne die eigene Stellung zu beeinflussen)
    /// </summary>
    /// <param name="data">Stellungsdaten, welche ausgelesen werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <returns>lesbare Stellung</returns>
    public string Debug(ushort[] data, int offset, int zugTiefe)
    {
      SokowahnRaum tmp = new SokowahnRaum(this);

      tmp.LadeStellung(data, offset, zugTiefe);

      return tmp.ToString();
    }
    #endregion
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="raum">Basisdaten anhand eines vorhanden Raumes nutzen</param>
    public SokowahnRaum(SokowahnRaum raum)
    {
      this.feldData = raum.feldData;
      this.feldBreite = raum.feldBreite;
      this.raumAnzahl = raum.raumAnzahl;
      this.raumLinks = raum.raumLinks;
      this.raumRechts = raum.raumRechts;
      this.raumOben = raum.raumOben;
      this.raumUnten = raum.raumUnten;

      this.raumSpielerPos = raum.raumSpielerPos;
      this.spielerZugTiefe = raum.spielerZugTiefe;
      this.raumZuKisten = raum.raumZuKisten.ToArray(); // Kopie erstellen
      this.kistenAnzahl = raum.kistenAnzahl;
      this.kistenZuRaum = raum.kistenZuRaum.ToArray(); // Kopie erstellen

      this.tmpCheckRaumPosis = new int[raumAnzahl];
      this.tmpCheckRaumTiefe = new int[raumAnzahl];
      this.tmpRaumCheckFertig = new bool[raumAnzahl + 1];
      this.tmpRaumCheckFertig[raumAnzahl] = true; // Ende-Feld schon auf fertig setzen
    }

    /// <summary>
    /// merkt sich die Hashtable für den Run
    /// </summary>
    SokowahnHash merkStartHash = null;

    /// <summary>
    /// merkt sich die Hashtable für den Run
    /// </summary>
    SokowahnHash merkZielHash = null;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="feldData">Daten des Spielfeldes</param>
    /// <param name="feldBreite">Breite des Spielfeldes</param>
    public SokowahnRaum(char[] feldData, int feldBreite)
    {
      this.feldData = feldData;
      this.feldBreite = feldBreite;

      bool[] spielerRaum = SpielfeldRaumScan(feldData, feldBreite);

      int raumAnzahl = this.raumAnzahl = spielerRaum.Count(x => x);

      int[] raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();

      this.raumLinks = raumZuFeld.Select(i => spielerRaum[i - 1] ? raumZuFeld.ToList().IndexOf(i - 1) : raumAnzahl).ToArray();
      this.raumRechts = raumZuFeld.Select(i => spielerRaum[i + 1] ? raumZuFeld.ToList().IndexOf(i + 1) : raumAnzahl).ToArray();
      this.raumOben = raumZuFeld.Select(i => spielerRaum[i - feldBreite] ? raumZuFeld.ToList().IndexOf(i - feldBreite) : raumAnzahl).ToArray();
      this.raumUnten = raumZuFeld.Select(i => spielerRaum[i + feldBreite] ? raumZuFeld.ToList().IndexOf(i + feldBreite) : raumAnzahl).ToArray();

      this.kistenZuRaum = Enumerable.Range(0, raumAnzahl).Where(i => spielerRaum[raumZuFeld[i]] && (feldData[raumZuFeld[i]] == '$' || feldData[raumZuFeld[i]] == '*')).ToArray();
      int kistenAnzahl = this.kistenAnzahl = kistenZuRaum.Length;
      int counter = 0;
      this.raumZuKisten = raumZuFeld.Select(i => (feldData[i] == '$' || feldData[i] == '*') ? counter++ : kistenAnzahl).ToArray();
      Array.Resize(ref this.raumZuKisten, raumAnzahl + 1);
      this.raumZuKisten[raumAnzahl] = kistenAnzahl;

      this.raumSpielerPos = Enumerable.Range(0, raumAnzahl).Where(i => feldData[raumZuFeld[i]] == '@' || feldData[raumZuFeld[i]] == '+').First();
      this.spielerZugTiefe = 0;

      this.tmpCheckRaumPosis = new int[raumAnzahl];
      this.tmpCheckRaumTiefe = new int[raumAnzahl];
      this.tmpRaumCheckFertig = new bool[raumAnzahl + 1];
      this.tmpRaumCheckFertig[raumAnzahl] = true; // Ende-Feld schon auf fertig setzen
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

      bool[] spielerRaum = feldData.Select(c => c == '@' || c == '+').ToArray();

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
      T[] ausgabe = new T[anzahl];

      for (int i = 0; i < anzahl; i++) ausgabe[i] = array[i];

      return ausgabe;
    }
    #endregion
  }
}
