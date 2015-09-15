#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace Sokosolver
{
  public class SokoWahn_2nd : SokoWahnInterface
  {
    #region # // --- Feld-Daten ---
    /// <summary>
    /// merkt sich die Breite des Spielfeldes
    /// </summary>
    int feldBreite;
    /// <summary>
    /// merkt sich die Höhe des Spielfeldes
    /// </summary>
    int feldHöhe;
    /// <summary>
    /// merkt sich die Anzahl der Spielfelder
    /// </summary>
    int feldAnzahl;
    /// <summary>
    /// merkt sich das Spielfeld: ' ' = leer, '#' = Wand, '@' = Spieler, '$' = Kiste, '.' = Zielfeld, '*' = Kisten auf Zielfeld, '+' = Spieler auf Zielfeld
    /// </summary>
    char[] feld;
    /// <summary>
    /// gleiche wie "feld" nur ohne Kisten und ohne Spieler
    /// </summary>
    char[] feldLeer;
    #endregion

    #region # // --- Spieler-Daten ---
    /// <summary>
    /// Spielerposition
    /// </summary>
    int spielerPos;
    /// <summary>
    /// gleiche Größe wie das Feld, gibt an, ob dort jeweils der Spieler sich aufhalten darf
    /// </summary>
    bool[] spielerRaum;
    /// <summary>
    /// merkt sich alle begehbaren Adressen direkt
    /// </summary>
    short[] spielerRaumIndex;
    /// <summary>
    /// merkt sich die eigenen Index-Einträge
    /// </summary>
    int[] spielerRaumReIndex;
    #endregion

    #region # // --- Kisten-Daten ---
    /// <summary>
    /// merkt sich die Startaufstellung am Anfang des Spiels
    /// </summary>
    string kistenStartAufstellung;
    /// <summary>
    /// merkt sich, wieviele Kisten allgemein auf dem Spielfeld sich befinden
    /// </summary>
    int kistenAnzahl;
    /// <summary>
    /// gibt an, ob sich auf dem Feld eine Kiste befinden darf
    /// </summary>
    bool[] kistenRaum;
    /// <summary>
    /// merk sich die Anzahl der Kisten-Felder
    /// </summary>
    int kistenRaumAnzahl;
    /// <summary>
    /// merkt sich die Positionen der Kistenfelder
    /// </summary>
    int[] kistenRaumIndex;
    /// <summary>
    /// merkt sich, ob das System sich noch im Kisten-Aufbau-Modus befindet (-1 = kein Aufbau-Modus, 0 = Startaufstellung, 1..x = Tiefe vom Aufbau-Modus
    /// </summary>
    int kistenAufbauModus;
    /// <summary>
    /// merkt sich die aktuell zu prüfende Kistenpositionen im kistenRaumIndex (Länge = Anzahl der Kisten)
    /// </summary>
    int[] kistenPositionen;
    /// <summary>
    /// zeigt auf Array kistenSpielerCheckListe, welche Variante gerade geprüft wird
    /// </summary>
    int kistenSpielerCheck;
    /// <summary>
    /// merkt sich die Spielerpositionen mit Kiste, welche geprüft werden (kistenIndex * 4 + richtung, richtung: 0 = links, 1 = rechts, 2 = oben, 3 = unten)
    /// </summary>
    int[] kistenSpielerCheckListe;
    /// <summary>
    /// gibt an, ob der Spieler gerade aktiv geprüft wird
    /// </summary>
    bool kistenSpielerCheckAktiv;
    /// <summary>
    /// merkt sich die aktuelle Spielerposition, welche geprüft wird
    /// </summary>
    int kistenSpielerPos;
    /// <summary>
    /// merkt sich die Offset-Position der Richtungen des Spielers (-1, +1, -feldBreite, +feldBreite)
    /// </summary>
    int[] kistenSpielerOffset;

    #endregion

    bool ohneBlocker = false; // default: false für bessere Leistung

    #region # // --- Hash-Daten ---
    /// <summary>
    /// merkt sich alle bekannten Stellungen
    /// </summary>
    Dictionary<string, string> bekannteStellungen;
    /// <summary>
    /// merkt sich temporär alle Kisten-Konstellationen, welche zum Ziel geführt werden können
    /// </summary>
    Dictionary<string, bool> kistenStellungenWertung;
    /// <summary>
    /// merkt sich die Liste der noch zu prüfenden Stellungen
    /// </summary>
    List<string> prüfStellungen;

    /// <summary>
    /// merkt sich die Kistenstellungen, welche nicht zum Ziel führen können
    /// </summary>
    char[] kistenStopper;
    /// <summary>
    /// aktuelle Schreibposition im Array kistenStopper
    /// </summary>
    int kistenStopperPos;
    /// <summary>
    /// Index der der Kistenpositionen * 4, welche der Spieler gerade verschoben hat
    /// </summary>
    int[] kistenStopperIndex;

    #endregion

    #region # public SokoWahn(string quellFeld) // Konstruktor
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="quellFeld">Quellfelder als Textzeilen</param>
    public SokoWahn_2nd(string quellFeld)
    {
      #region # // --- Feld erstellen und zurecht schneiden ---
      feldBreite = quellFeld.IndexOf('\r');
      feld = quellFeld.Replace("\r", "").Replace("\n", "").ToCharArray();
      feldHöhe = feld.Length / feldBreite;
      if (feld.Length != feldBreite * feldHöhe) throw new Exception("ungültiges Feld!");

      // Links weg schneiden
      while (Enumerable.Range(0, feldHöhe).All(i => feld[i * feldBreite] == ' '))
      {
        for (int y = 0; y < feldHöhe; y++) for (int x = 1; x < feldBreite; x++) feld[x - 1 + y * (feldBreite - 1)] = feld[x + y * feldBreite];
        feldBreite--;
      }

      // Oben weg schneiden
      while (Enumerable.Range(0, feldBreite).All(i => feld[i] == ' '))
      {
        for (int y = 1; y < feldHöhe; y++) for (int x = 0; x < feldBreite; x++) feld[x + (y - 1) * feldBreite] = feld[x + y * feldBreite];
        feldHöhe--;
      }

      // Rechts weg schneiden
      while (Enumerable.Range(0, feldHöhe).All(i => feld[i * feldBreite + feldBreite - 1] == ' '))
      {
        for (int y = 0; y < feldHöhe; y++) for (int x = 0; x < feldBreite - 1; x++) feld[x + y * (feldBreite - 1)] = feld[x + y * feldBreite];
        feldBreite--;
      }

      // Unten weg schneiden
      while (Enumerable.Range(0, feldBreite).All(i => feld[i + (feldHöhe - 1) * feldBreite] == ' '))
      {
        // muss nix verschoben werden
        feldHöhe--;
      }

      feldAnzahl = feldBreite * feldHöhe;
      Array.Resize(ref feld, feldAnzahl);

      feldLeer = feld.Select(c =>
      {
        switch (c)
        {
          case '#': return '#';
          case ' ': return ' ';
          case 'x': return ' ';
          case '$': return ' ';
          case '@': return ' ';
          case '+': return '.';
          case '.': return '.';
          case '*': return '.';
          default: throw new Exception("Fehlerhaftes Zeichen: " + c);
        }
      }).ToArray();
      #endregion

      #region # // --- Spieler suchen ---
      spielerPos = -1;

      for (int y = 0; y < feldHöhe; y++)
      {
        for (int x = 0; x < feldBreite; x++)
        {
          if (feld[x + y * feldBreite] == '@' || feld[x + y * feldBreite] == '+')
          {
            if (spielerPos >= 0) throw new Exception("Fehler, mehrere Spieler vorhanden");
            spielerPos = x + y * feldBreite;
          }
        }
      }

      if (spielerPos < 0) throw new Exception("Fehler, Spieler wurde nicht gefunden");
      #endregion

      #region # // --- begehbare Felder ermitteln ---
      spielerRaum = new bool[feldAnzahl];
      spielerRaum[spielerPos] = true;

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
              if (!spielerRaum[p] && " .$*x".Any(c => feld[p] == c)) { find = spielerRaum[p] = true; }
              p += feldBreite - 1;
              if (!spielerRaum[p] && " .$*x".Any(c => feld[p] == c)) { find = spielerRaum[p] = true; }
              p += 2;
              if (!spielerRaum[p] && " .$*x".Any(c => feld[p] == c)) { find = spielerRaum[p] = true; }
              p += feldBreite - 1;
              if (!spielerRaum[p] && " .$*x".Any(c => feld[p] == c)) { find = spielerRaum[p] = true; }
            }
          }
        }
      }

      // unsinnige Lauf-Felder entfernen (nur welche in Sackgassen enden)
      find = true;
      while (find)
      {
        find = false;
        for (int y = 1; y < feldHöhe - 1; y++)
        {
          for (int x = 1; x < feldBreite - 1; x++)
          {
            int p = x + y * feldBreite;
            if (spielerRaum[p] && p != spielerPos)
            {
              if (!spielerRaum[p - 1] && !spielerRaum[p - feldBreite] && !spielerRaum[p + 1]) { spielerRaum[p] = false; find = true; }
              if (!spielerRaum[p - 1] && !spielerRaum[p + feldBreite] && !spielerRaum[p + 1]) { spielerRaum[p] = false; find = true; }
              if (!spielerRaum[p - feldBreite] && !spielerRaum[p - 1] && !spielerRaum[p + feldBreite]) { spielerRaum[p] = false; find = true; }
              if (!spielerRaum[p - feldBreite] && !spielerRaum[p + 1] && !spielerRaum[p + feldBreite]) { spielerRaum[p] = false; find = true; }
            }
          }
        }
      }

      // Index der begehbaren Bereiche berechnen
      spielerRaumIndex = spielerRaum.Select((x, i) => new { x, i }).Where(x => x.x).Select(x => (short)x.i).ToArray();
      spielerRaumReIndex = new int[char.MaxValue];
      spielerRaumIndex.Select((x, i) => spielerRaumReIndex[x] = (byte)i).Count();


      #endregion

      #region # // --- einfache Kistenfelder ermitteln ---
      kistenRaum = new bool[feldAnzahl];
      for (int y = 1; y < feldHöhe - 1; y++)
      {
        for (int x = 1; x < feldBreite - 1; x++)
        {
          int p = x + y * feldBreite;
          if (feld[p] == '*' || feld[p] == '.' || feld[p] == '$' || feld[p] == '@' || feld[p] == '+')
          {
            kistenRaum[p] = true;
            continue;
          }
          if (feld[p] == 'x')
          {
            feld[p] = ' ';
            continue;
          }
          if (ohneBlocker && feld[p] == ' ')
          {
            kistenRaum[p] = true;
            continue;
          }
          if (spielerRaum[p])
          {
            if (spielerRaum[p - 1] && spielerRaum[p + 1])
            {
              kistenRaum[p] = true;
              continue;
            }
            if (spielerRaum[p - feldBreite] && spielerRaum[p + feldBreite])
            {
              kistenRaum[p] = true;
              continue;
            }
          }
        }
      }

      kistenRaumAnzahl = kistenRaum.Where(x => x).Count();
      kistenRaumIndex = kistenRaum.Select((x, i) => new { x, i }).Where(x => x.x).Select(x => x.i).ToArray();
      kistenAnzahl = feld.Where(x => x == '$' || x == '*').Count();
      #endregion

      kistenAufbauModus = 0;
      kistenStartAufstellung = ToString();

      kistenStopper = new char[65536];
      kistenStopper[0] = (char)0;
      kistenStopperPos = 1;
      kistenStopperIndex = new int[feldAnzahl * 4];

      prüfStellungen = new List<string>();
      bekannteStellungen = new Dictionary<string, string>();
      kistenStellungenWertung = new Dictionary<string, bool>();
    }
    #endregion

    #region # bool LadeStellung(string stellung) // lädt eine bekannte Stellung (der Grundaufbau muss mit der vorhandenen identisch schein)
    /// <summary>
    /// lädt eine bekannte Stellung (der Grundaufbau muss mit der vorhandenen identisch schein)
    /// </summary>
    /// <param name="stellung">Stellung, welche geladen werden soll</param>
    bool LadeStellung(string stellung)
    {
      bool fertig = true;
      if (stellung.Length == kistenAnzahl + 1)
      {
        Array.Copy(feldLeer, feld, feld.Length);
        spielerPos = (int)stellung[0];
        feld[spielerPos] = feld[spielerPos] == '.' ? '+' : '@';
        stellung.Skip(1).Select(x =>
        {
          if (feld[x] == '.')
          {
            feld[x] = '*';
          }
          else
          {
            fertig = false;
            feld[x] = '$';
          }
          return 0;
        }).Count();
        return !fertig;
      }
      for (int y = 0; y < feldHöhe; y++)
      {
        for (int x = 0; x < feldBreite; x++)
        {
          char c = feld[x + y * feldBreite] = stellung[x + y * (feldBreite + 2)];
          if (c == '@' || c == '+')
          {
            spielerPos = x + y * feldBreite;
            if (c == '+') fertig = false;
          }
          if (c == '.') fertig = false;
        }
      }
      return !fertig;
    }
    #endregion

    #region # bool StellungBekannt(string stellung) // prüft, ob eine bestimmte Stellung bereits bekannt ist
    /// <summary>
    /// prüft, ob eine bestimmte Stellung bereits bekannt ist
    /// </summary>
    /// <param name="stellung">Stellung, welche geprüft werden soll</param>
    /// <returns>true, wenn die Stellung bereits bekannt war</returns>
    bool StellungBekannt(string stellung)
    {
      if (bekannteStellungen.ContainsKey(stellung)) return true;

      if (archivSatzPos == 0) return false;

      int von = 0;
      int bis = archivSatzPos / archivSatzGröße;
      int mit = 0;

      byte[] such = stellung.Select(x => (byte)spielerRaumReIndex[x]).ToArray();

      do
      {
        mit = (von + bis) >> 1;
        if (Enumerable.Range(0, archivSatzGröße).Where(i => archivData[i + mit * archivSatzGröße] != such[i]).Select(i => such[i] - archivData[i + mit * archivSatzGröße]).FirstOrDefault() < 0) { bis = mit; } else { von = mit; }
      } while (bis - von > 1);

      if (Enumerable.Range(0, archivSatzGröße).All(i => archivData[i + von * archivSatzGröße] == such[i])) return true; // von - Position 

      return false;
    }
    #endregion

    #region # bool KannLauf(int richtung) // prüft, ob der Spieler in eine bestimmte Richtung laufen kann (nicht schieben einer Kiste)
    /// <summary>
    /// prüft, ob der Spieler in eine bestimmte Richtung laufen kann (nicht schieben einer Kiste)
    /// </summary>
    /// <param name="richtung">Richtung (erlaubte Werte: -1 , +1, -feldBreite, +feldBreite)</param>
    /// <returns>true, wenn in die entsprechende Richtung gelaufen werden kann</returns>
    bool KannLauf(int richtung)
    {
      return spielerRaum[spielerPos + richtung] && (feld[spielerPos + richtung] == ' ' || feld[spielerPos + richtung] == '.');
    }
    #endregion
    #region # void LosLauf(int richtung) // lässt den Spieler in eine bestimmte Richung laufen (nicht schieben einer Kiste)
    /// <summary>
    /// lässt den Spieler in eine bestimmte Richung laufen (nicht schieben einer Kiste)
    /// </summary>
    /// <param name="richtung">Richtung (erlaubte Werte: -1 , +1, -feldBreite, +feldBreite)</param>
    void LosLauf(int richtung)
    {
      feld[spielerPos] = feld[spielerPos] == '+' ? '.' : ' ';
      spielerPos += richtung;
      feld[spielerPos] = feld[spielerPos] == '.' ? '+' : '@';
    }
    #endregion

    #region # bool KannSchieben(int richtung) // prüft, ob der Spieler eine Kiste in eine bestimmt Richtung schieben kann
    /// <summary>
    /// prüft, ob der Spieler eine Kiste in eine bestimmt Richtung schieben kann
    /// </summary>
    /// <param name="richtung">Richtung (erlaubte Werte: -1 , +1, -feldBreite, +feldBreite)</param>
    /// <returns>true, wenn eine Kiste verschiebbar ist</returns>
    bool KannSchieben(int richtung)
    {
      return (feld[spielerPos + richtung] == '$' || feld[spielerPos + richtung] == '*') && (feld[spielerPos + richtung + richtung] == '.' || (feld[spielerPos + richtung + richtung] == ' ' && kistenRaum[spielerPos + richtung + richtung]));
    }
    #endregion
    #region # void LosSchieb(int richtung) // verschiebt eine Kiste in eine bestimmte Richtung
    /// <summary>
    /// verschiebt eine Kiste in eine bestimmte Richtung
    /// </summary>
    /// <param name="richtung">Richtung (erlaubte Werte: -1 , +1, -feldBreite, +feldBreite)</param>
    void LosSchieb(int richtung)
    {
      feld[spielerPos] = feld[spielerPos] == '+' ? '.' : ' ';
      spielerPos += richtung;
      feld[spielerPos] = feld[spielerPos] == '*' ? '+' : '@';
      feld[spielerPos + richtung] = feld[spielerPos + richtung] == '.' ? '*' : '$';
    }
    #endregion
    #region # void ZurückSchieb(int richtung) // verschiebt eine Kiste zurück
    /// <summary>
    /// verschiebt eine Kiste zurück
    /// </summary>
    /// <param name="richtung">Richtung (erlaubte Werte: -1 , +1, -feldBreite, +feldBreite)</param>
    void ZurückSchieb(int richtung)
    {
      feld[spielerPos] = feld[spielerPos] == '+' ? '*' : '$';
      feld[spielerPos - richtung] = feld[spielerPos - richtung] == '*' ? '.' : ' ';
      spielerPos += richtung;
      feld[spielerPos] = feld[spielerPos] == '.' ? '+' : '@';
    }
    #endregion

    #region # bool KistenPositionenIncrement() // sucht die nächste Kisten-Variante
    /// <summary>
    /// sucht die nächste Kisten-Variante
    /// </summary>
    /// <returns>true, wenn eine weitere Variante gefunden wurde sonst false</returns>
    bool KistenPositionenIncrement()
    {
      // bisher sichtbare Kisten entfernen
      kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feldLeer[kistenRaumIndex[x]]).Count();

      switch (kistenPositionen.Length)
      {
        case 1:
        {
          kistenPositionen[0]++;
          if (kistenPositionen[0] >= kistenRaumAnzahl) return false;
        } break;
        case 2:
        {
          kistenPositionen[1]++;
          if (kistenPositionen[1] >= kistenRaumAnzahl)
          {
            kistenPositionen[0]++;
            if (kistenPositionen[0] >= kistenRaumAnzahl - 1) return false;
            kistenPositionen[1] = kistenPositionen[0] + 1;
          }
        } break;
        case 3:
        {
          kistenPositionen[2]++;
          if (kistenPositionen[2] >= kistenRaumAnzahl)
          {
            kistenPositionen[1]++;
            if (kistenPositionen[1] >= kistenRaumAnzahl - 1)
            {
              kistenPositionen[0]++;
              if (kistenPositionen[0] >= kistenRaumAnzahl - 2) return false;
              kistenPositionen[1] = kistenPositionen[0] + 1;
            }
            kistenPositionen[2] = kistenPositionen[1] + 1;
          }
        } break;
        case 4:
        {
          kistenPositionen[3]++;
          if (kistenPositionen[3] >= kistenRaumAnzahl)
          {
            kistenPositionen[2]++;
            if (kistenPositionen[2] >= kistenRaumAnzahl - 1)
            {
              kistenPositionen[1]++;
              if (kistenPositionen[1] >= kistenRaumAnzahl - 2)
              {
                kistenPositionen[0]++;
                if (kistenPositionen[0] >= kistenRaumAnzahl - 3) return false;
                kistenPositionen[1] = kistenPositionen[0] + 1;
              }
              kistenPositionen[2] = kistenPositionen[1] + 1;
            }
            kistenPositionen[3] = kistenPositionen[2] + 1;
          }
        } break;
        case 5:
        {
          kistenPositionen[4]++;
          if (kistenPositionen[4] >= kistenRaumAnzahl)
          {
            kistenPositionen[3]++;
            if (kistenPositionen[3] >= kistenRaumAnzahl - 1)
            {
              kistenPositionen[2]++;
              if (kistenPositionen[2] >= kistenRaumAnzahl - 2)
              {
                kistenPositionen[1]++;
                if (kistenPositionen[1] >= kistenRaumAnzahl - 3)
                {
                  kistenPositionen[0]++;
                  if (kistenPositionen[0] >= kistenRaumAnzahl - 4) return false;
                  kistenPositionen[1] = kistenPositionen[0] + 1;
                }
                kistenPositionen[2] = kistenPositionen[1] + 1;
              }
              kistenPositionen[3] = kistenPositionen[2] + 1;
            }
            kistenPositionen[4] = kistenPositionen[3] + 1;
          }
        } break;
        default: throw new Exception("todo");
      }

      // sichtbare Kisten neu setzen
      kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feld[kistenRaumIndex[x]] == '.' ? '*' : '$').Count();

      return true;
    }
    #endregion

    #region # void KistenPositionUpdate(int pos, int offset) // erneuert die Position einer virtuellen Kiste
    /// <summary>
    /// erneuert die Position einer virtuellen Kiste
    /// </summary>
    /// <param name="pos">aktuelle Position der Kiste</param>
    /// <param name="offset">offset für die neue Position</param>
    void KistenPositionUpdate(int pos, int offset)
    {
      for (int i = 0; i < kistenPositionen.Length; i++)
      {
        if (kistenRaumIndex[kistenPositionen[i]] == pos)
        {
          pos += offset;
          // Position neu einsortieren (sofern notwendig)
          if (offset < 0)
          {
            while (i > 0 && kistenRaumIndex[kistenPositionen[i - 1]] > pos) { kistenPositionen[i] = kistenPositionen[i - 1]; i--; }
          }
          else
          {
            while (i < kistenPositionen.Length - 1 && kistenRaumIndex[kistenPositionen[i + 1]] < pos) { kistenPositionen[i] = kistenPositionen[i + 1]; i++; }
          }
          kistenPositionen[i] = kistenRaumIndex.Select((x, p) => new { x, p }).Where(x => x.x == pos).First().p;
          return;
        }
      }
      throw new Exception("Kiste auf Position nicht gefunden: " + pos);
    }
    #endregion

    #region # IEnumerable<string> KistenSucheSpielzüge(string stellung) // gibt alle möglichen Spielzüge einer Stellung zurück
    /// <summary>
    /// gibt alle möglichen Spielzüge einer Stellung zurück
    /// </summary>
    /// <param name="stellung">Stellung, welche geprüft werden soll</param>
    /// <returns>einzelne Stellungen welche die Zugmöglichkeiten darstellen</returns>
    IEnumerable<string> KistenSucheSpielzüge(string stellung)
    {
      string cache;
      if (bekannteStellungen.TryGetValue(stellung, out cache))
      {
        int länge = kistenPositionen.Length + 1;
        foreach (var ausgabe in Enumerable.Range(0, cache.Length / länge).Select(x => cache.Substring(x * länge, länge))) yield return ausgabe;
        yield break;
      }

      // kistenSpielerPos = stellung[0];
      // for (int i = 0; i < kistenPositionen.Length; i++) kistenPositionen[i] = stellung[i + 1]; ;
      // kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feld[kistenRaumIndex[x]] == '.' ? '*' : '$').Count();
      // feld[kistenSpielerPos] = feld[kistenSpielerPos] == '.' ? '+' : '@'; // test

      bool[] spielerGewesen = new bool[feld.Length];
      List<int> posis = new List<int>();
      posis.Add(kistenSpielerPos);

      List<string> merker = new List<string>();

      while (posis.Count > 0)
      {
        int bis = posis.Count;
        for (int i = 0; i < bis; i++)
        {
          int pos = posis[i];
          if (spielerGewesen[pos]) continue;
          spielerGewesen[pos] = true;

          pos -= feldBreite; // pos oben
          if (!spielerGewesen[pos])
          {
            if (feld[pos] == '$' || feld[pos] == '*')
            {
              if (feld[pos - feldBreite] == '.' || (feld[pos - feldBreite] == ' ' && kistenRaum[pos - feldBreite]))
              {
                KistenPositionUpdate(pos, -feldBreite);
                merker.Add((char)pos + new string(kistenPositionen.Select(x => (char)x).ToArray()));
                KistenPositionUpdate(pos - feldBreite, +feldBreite);
              }
            }
            else if (feld[pos] == ' ' || feld[pos] == '.') posis.Add(pos);
          }

          pos += feldBreite - 1; // pos links
          if (!spielerGewesen[pos])
          {
            if (feld[pos] == '$' || feld[pos] == '*')
            {
              if (feld[pos - 1] == '.' || (feld[pos - 1] == ' ' && kistenRaum[pos - 1]))
              {
                KistenPositionUpdate(pos, -1);
                merker.Add((char)pos + new string(kistenPositionen.Select(x => (char)x).ToArray()));
                KistenPositionUpdate(pos - 1, +1);
              }
            }
            else if (feld[pos] == ' ' || feld[pos] == '.') posis.Add(pos);
          }

          pos += 2; // pos rechts
          if (!spielerGewesen[pos])
          {
            if (feld[pos] == '$' || feld[pos] == '*')
            {
              if (feld[pos + 1] == '.' || (feld[pos + 1] == ' ' && kistenRaum[pos + 1]))
              {
                KistenPositionUpdate(pos, +1);
                merker.Add((char)pos + new string(kistenPositionen.Select(x => (char)x).ToArray()));
                KistenPositionUpdate(pos + 1, -1);
              }
            }
            else if (feld[pos] == ' ' || feld[pos] == '.') posis.Add(pos);
          }

          pos += feldBreite - 1; // pos unten
          if (!spielerGewesen[pos])
          {
            if (feld[pos] == '$' || feld[pos] == '*')
            {
              if (feld[pos + feldBreite] == '.' || (feld[pos + feldBreite] == ' ' && kistenRaum[pos + feldBreite]))
              {
                KistenPositionUpdate(pos, +feldBreite);
                merker.Add((char)pos + new string(kistenPositionen.Select(x => (char)x).ToArray()));
                KistenPositionUpdate(pos + feldBreite, -feldBreite);
              }
            }
            else if (feld[pos] == ' ' || feld[pos] == '.') posis.Add(pos);
          }
        }
        posis.RemoveRange(0, bis);
      }

      // kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feldLeer[kistenRaumIndex[x]]).Count();

      foreach (var satz in merker) yield return satz;

      bekannteStellungen[stellung] = string.Concat(merker);
    }
    #endregion

    /// <summary>
    /// gibt an, ob die aktuelle Stellung erlaubt ist
    /// </summary>
    /// <param name="richtungsOff">Richtung: 0 = links, 1 = rechts, 2 = oben, 3 = unten</param>
    /// <returns>true, wenn die Stellung erlaubt ist, sonst false</returns>
    bool KistenStopperErlaubt(int richtungsOff)
    {
      int off;
      switch (richtungsOff)
      {
        case 0: off = feldAnzahl + spielerPos - 1; break;
        case 1: off = spielerPos + 1; break;
        case 2: off = feldAnzahl * 3 + spielerPos - feldBreite; break;
        case 3: off = feldAnzahl * 2 + spielerPos + feldBreite; break;
        default: throw new Exception("?");
      }

      if (kistenStopperIndex[off] > 0) return false;

      int tiefe = 1;
      for (int zugIndex = off + feldAnzahl * 4; zugIndex < kistenStopperIndex.Length; zugIndex += feldAnzahl * 4)
      {
        if (kistenStopperIndex[zugIndex] > 0)
        {
          int pp = kistenStopperIndex[zugIndex];
          while (kistenStopper[pp] != (char)9999)
          {
            int gg = 0;
            for (int i = 0; i < tiefe; i++)
            {
              int p = kistenRaumIndex[kistenStopper[pp + i]];
              if (feld[p] == '$' || feld[p] == '*') gg++; else break;
            }
            if (gg == tiefe) return false;
            pp += tiefe;
          }
        }
        tiefe++;
      }

      return true;
    }

    #region # public bool Next(int limit) // berechnet den nächsten Schritt
    /// <summary>
    /// berechnet den nächsten Schritt
    /// </summary>
    /// <param name="limit">Anzahl der zu berechnenden Schritte</param>
    /// <returns>gibt an, ob noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      if (bekannteStellungen.Count > 47950000 - limit) // "Out of Memory Error" abfangen, da dieser Fehler ab 48000000-Datensätzen auftritt (egal wieviel Speicher tatsächlich verfügbar ist)
      {
        return true;
      }

      #region # // --- Kisten-Aufbau-Modus (negativer limit-Wert) ---
      if (limit < 0)
      {
        #region # // --- Initialisierung, falls die Anzahl der Kisten sich ändert ---
        if (kistenAufbauModus < 0) return false; // kein Aufbaumodus mehr?
        limit = -limit;
        if (kistenAufbauModus == 0) // aller erste Initialisierung 
        {
          kistenSpielerOffset = new[] { -1, +1, -feldBreite, +feldBreite };
          feldLeer.Select((c, i) => feld[i] = c).Count();

          kistenAufbauModus++;

          if (kistenAufbauModus > kistenAnzahl || kistenAufbauModus > 5) return false; // weitere Vorbereitung nicht mehr möglich

          kistenPositionen = Enumerable.Range(0, kistenAufbauModus).ToArray();
          kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feld[kistenRaumIndex[x]] == '.' ? '*' : '$').Count();
          kistenSpielerCheck = -1;
          kistenSpielerCheckAktiv = false;
          bekannteStellungen = new Dictionary<string, string>();
          kistenStellungenWertung = new Dictionary<string, bool>();
          prüfStellungen = new List<string>();
          return true;
        }
        #endregion

        while (limit-- > 0)
        {
          #region # if (kistenSpielerCheckAktiv) // eine Spielerposition wird aktuell geprüft?
          if (kistenSpielerCheckAktiv) // eine Spielerposition wird aktuell geprüft?
          {
            string stellung = (char)kistenSpielerPos + new string(kistenPositionen.Select(x => (char)x).ToArray());

            if (kistenPositionen.All(pos => feld[kistenRaumIndex[pos]] == '*'))
            {
              kistenStellungenWertung[stellung] = true; // Zielstellung gefunden
            }
            else
            {
              kistenStellungenWertung[stellung] = false; // ungelöste Stellung gefunden
            }
            feld[kistenSpielerPos] = feldLeer[kistenSpielerPos];

            KistenSucheSpielzüge(stellung).Count();
            kistenSpielerCheckAktiv = false;
            kistenSpielerCheck++;
            limit++;
            continue;
          }
          #endregion

          #region # if (kistenSpielerCheck == -1) // Liste muss erst erstellt werden?
          if (kistenSpielerCheck == -1) // Liste muss erst erstellt werden?
          {
            List<int> neu = new List<int>();
            for (int y = 0; y < kistenPositionen.Length; y++)
            {
              for (int r = 0; r < 4; r++)
              {
                int pos = kistenRaumIndex[kistenPositionen[y]] + kistenSpielerOffset[r];
                if (!spielerRaum[pos] || (feld[pos] != ' ' && feld[pos] != '.')) continue; // Spieler darf hier nicht stehen
                neu.Add((kistenPositionen[y] << 2) + r);
              }
            }
            kistenSpielerCheckListe = neu.ToArray();
            kistenSpielerCheck = 0;
            kistenSpielerCheckAktiv = false;
          }
          #endregion

          if (kistenSpielerCheck == kistenSpielerCheckListe.Length) // Ende erreicht? -> neue Kistenvariante prüfen
          {
            if (prüfStellungen.Count == 0 && KistenPositionenIncrement()) // nächste Kistenvariante verfügbar? (wenn Daten in der Prüfliste stehen, dann muss weiter gefiltert werden)
            {
              kistenSpielerCheck = -1;
              limit++;
              continue;
            }
            else
            {
              // filtern nach gültigen und ungültigen Zugvarianten
              prüfStellungen = new List<string>();
              foreach (string check in kistenStellungenWertung.Where(x => !x.Value).Select(x => x.Key))
              {
                //int find = KistenSucheSpielzüge(check).Where(x => kistenStellungenWertung[x]).Count();
                //if (find > 0) prüfStellungen.Add(check);
                if (KistenSucheSpielzüge(check).Any(x => kistenStellungenWertung[x])) prüfStellungen.Add(check);
              }
              if (prüfStellungen.Select(x => kistenStellungenWertung[x] = true).Count() > 0) return true;

              // ungültige Stellungen Sammeln
              prüfStellungen.AddRange(kistenStellungenWertung.Where(x => !x.Value).Select(x => x.Key));

              if (kistenAufbauModus == 1)
              {
                LadeStellung(kistenStartAufstellung);
                int zusatzKill = prüfStellungen.Where(x => !kistenStellungenWertung.Where(i => i.Value).Any(i => i.Key[1] == x[1])).Select(x =>
                 {
                   int pos = kistenRaumIndex[x[1]];
                   if (feld[pos] == ' ') feld[pos] = 'x';
                   kistenRaum[pos] = false;
                   return 0;
                 }).Count();

                if (zusatzKill > 0) // wurden neue Verbotsfelder für Kisten gefunden?
                {
                  kistenRaumAnzahl = kistenRaum.Where(x => x).Count();
                  kistenRaumIndex = kistenRaum.Select((x, i) => new { x, i }).Where(x => x.x).Select(x => x.i).ToArray();
                  kistenAufbauModus = 0;
                  return true;
                }
                Array.Copy(feldLeer, feld, feld.Length);
              }

              // --- zusätzlichen Stop-Index berechnen ---
              var tempListe = Enumerable.Range(0, feldAnzahl * 4).Select(i => { List<char> dummy = null; return dummy; }).ToArray();

              kistenStellungenWertung.Where(x => !x.Value).Select(x =>
              {
                int spielerPos = x.Key[0];
                char[] kistenPosis = x.Key.Skip(1).ToArray();
                for (int r = 0; r < kistenPosis.Length; r++)
                {
                  int kistenPos = kistenPosis[r];
                  int off = spielerPos - kistenRaumIndex[kistenPos];
                  if (off == -1) off = 0; else if (off == +1) off = feldAnzahl; else if (off == -feldBreite) off = feldAnzahl * 2; else if (off == +feldBreite) off = feldAnzahl * 3; else continue;
                  off += kistenRaumIndex[kistenPos];
                  if (tempListe[off] == null) tempListe[off] = new List<char>();
                  int dazu = kistenPosis.Where(i => (int)i != kistenPos).Select(i => { tempListe[off].Add(i); return 0; }).Count();
                  if (dazu != kistenAufbauModus - 1) throw new Exception("Stop?");
                }

                return 0;
              }).Count();

              // --- temporären Stop-Index in die Haupttabellen eintragen ---
              Array.Resize(ref kistenStopperIndex, feldAnzahl * kistenAufbauModus * 4);

              for (int y = 0; y < feldAnzahl * 4; y++)
              {
                if (tempListe[y] != null)
                {
                  kistenStopperIndex[feldAnzahl * (kistenAufbauModus - 1) * 4 + y] = kistenStopperPos;
                  if (tempListe[y].Count + 1 >= kistenStopper.Length - kistenStopperPos) Array.Resize(ref kistenStopper, kistenStopper.Length * 2 + tempListe[y].Count);
                  tempListe[y].Select(x => kistenStopper[kistenStopperPos++] = x).Count();
                  kistenStopper[kistenStopperPos++] = (char)9999;
                }
              }

              // --- zum nächsten Aufbaumodus wecheln ---
              kistenAufbauModus++;
              kistenPositionen = Enumerable.Range(0, kistenAufbauModus).ToArray();
              kistenPositionen.Select(x => feld[kistenRaumIndex[x]] = feld[kistenRaumIndex[x]] == '.' ? '*' : '$').Count();
              kistenSpielerCheck = -1;
              kistenSpielerCheckAktiv = false;
              bekannteStellungen = new Dictionary<string, string>();
              kistenStellungenWertung = new Dictionary<string, bool>();
              prüfStellungen = new List<string>();
              return true;
            }
          }

          kistenSpielerPos = kistenRaumIndex[kistenSpielerCheckListe[kistenSpielerCheck] >> 2] + kistenSpielerOffset[kistenSpielerCheckListe[kistenSpielerCheck] & 3];
          feld[kistenSpielerPos] = feldLeer[kistenSpielerPos] == '.' ? '+' : '@';
          kistenSpielerCheckAktiv = true;

        }

        return true;
      }

      if (kistenAufbauModus >= 0) // noch im Kisten-Aufbau-Modus?
      {
        kistenAufbauModus = -1;
        bekannteStellungen = new Dictionary<string, string>();
        kistenStellungenWertung = new Dictionary<string, bool>();
        prüfStellungen = new List<string>();
        LadeStellung(kistenStartAufstellung);
        bekannteStellungen.Add(StringToKurz(kistenStartAufstellung), "");
        prüfStellungen.Add(StringToKurz(kistenStartAufstellung));
        GC.Collect();
        return true;
      }
      #endregion

      #region # // --- Normaler Modus (positiver limit-Wert) ---
      limit = Math.Min(prüfStellungen.Count, limit);
      for (int z = 0; z < limit; z++)
      {
        if (!LadeStellung(prüfStellungen[z]))
        {
          if (z > 0) prüfStellungen.RemoveRange(0, z);
          return false;
        }

        #region # // --- laufen ---
        if (KannLauf(-feldBreite))
        {
          LosLauf(-feldBreite); string stellung = ToKurzKette(); LosLauf(+feldBreite);
          if (!StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannLauf(+feldBreite))
        {
          LosLauf(+feldBreite); string stellung = ToKurzKette(); LosLauf(-feldBreite);
          if (!StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannLauf(-1))
        {
          LosLauf(-1); string stellung = ToKurzKette(); LosLauf(+1);
          if (!StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannLauf(+1))
        {
          LosLauf(+1); string stellung = ToKurzKette(); LosLauf(-1);
          if (!StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }
        #endregion

        #region # // --- schieben ---
        if (KannSchieben(-feldBreite))
        {
          LosSchieb(-feldBreite);
          string stellung = ToKurzKette();
          bool stellungErlaubt = KistenStopperErlaubt(2);
          ZurückSchieb(+feldBreite);
          if (stellungErlaubt && !StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannSchieben(+feldBreite))
        {
          LosSchieb(+feldBreite);
          string stellung = ToKurzKette();
          bool stellungErlaubt = KistenStopperErlaubt(3);
          ZurückSchieb(-feldBreite);
          if (stellungErlaubt && !StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannSchieben(-1))
        {
          LosSchieb(-1);
          string stellung = ToKurzKette();
          bool stellungErlaubt = KistenStopperErlaubt(0);
          ZurückSchieb(+1);
          if (stellungErlaubt && !StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }

        if (KannSchieben(+1))
        {
          LosSchieb(+1);
          string stellung = ToKurzKette();
          bool stellungErlaubt = KistenStopperErlaubt(1);
          ZurückSchieb(-1);
          if (stellungErlaubt && !StellungBekannt(stellung))
          {
            bekannteStellungen.Add(stellung, ToKurzKette());
            prüfStellungen.Add(stellung);
          }
        }
        #endregion
      }

      prüfStellungen.RemoveRange(0, limit);
      return true;
      #endregion
    }
    #endregion

    int archivSatzGröße = 0;
    int archivSatzPos = 0;
    byte[] archivData = null;
    //  int[] archivCounter = null;

    void ArchivEintrag(string[] neues)
    {
      if (neues.Length == 0) return;

      if (archivData == null) // neuaufbau?
      {
        archivSatzGröße = neues[0].Length;
        if (spielerRaumIndex.Length > 256) throw new Exception("todo");
        archivData = new byte[neues.Length * archivSatzGröße];
        foreach (string neu in neues)
        {
          neu.Select(x => archivData[archivSatzPos++] = (byte)spielerRaumReIndex[x]).Count();
        }
        return;
      }

      int neuPos = neues.Length;
      int altPos = archivSatzPos / archivSatzGröße;
      int zielPos = neuPos + altPos;
      archivSatzPos = zielPos * archivSatzGröße;
      neuPos--; altPos--; zielPos--;
      if (archivSatzPos > archivData.Length) Array.Resize(ref archivData, archivSatzPos);

      byte[] such = neues[neuPos].Select(x => (byte)spielerRaumReIndex[x]).ToArray();

      while (zielPos >= 0)
      {
        if (such != null && (altPos == 0 || Enumerable.Range(0, archivSatzGröße).Where(i => archivData[i + altPos * archivSatzGröße] != such[i]).Select(i => such[i] - archivData[i + altPos * archivSatzGröße]).FirstOrDefault() > 0))
        {
          for (int i = 0; i < archivSatzGröße; i++) archivData[i + zielPos * archivSatzGröße] = such[i];
          neuPos--;
          if (neuPos >= 0) such = neues[neuPos].Select(x => (byte)spielerRaumReIndex[x]).ToArray(); else such = null;
        }
        else
        {
          for (int i = 0; i < archivSatzGröße; i++) archivData[i + zielPos * archivSatzGröße] = archivData[i + altPos * archivSatzGröße];
          altPos--;
        }
        zielPos--;
      }
    }

    #region # public long Refresh() // entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// <summary>
    /// entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// </summary>
    /// <returns>Anzahl der Einträge, welche entfernt werden konnten</returns>
    public long Refresh()
    {
      var alt = bekannteStellungen;
      int vorher = alt.Count;
      bekannteStellungen = new Dictionary<string, string>();
      List<string> nochZuÜbertragen = prüfStellungen.ToList();

      while (nochZuÜbertragen.Count > 0)
      {
        nochZuÜbertragen.Sort((x, y) => string.CompareOrdinal(x, y));
        string davor = "?";
        nochZuÜbertragen = nochZuÜbertragen.Where(x => x != davor).Select(x => davor = x).ToList();

        int bis = nochZuÜbertragen.Count;
        for (int i = 0; i < bis; i++)
        {
          string such = nochZuÜbertragen[i];
          string tmp;
          if (!alt.TryGetValue(such, out tmp)) throw new Exception("?");
          bekannteStellungen[such] = tmp;
          if (tmp != "") nochZuÜbertragen.Add(tmp);
        }
        nochZuÜbertragen.RemoveRange(0, bis);
      }

      // gelöschte Stellungen merken
      string[] gelöschte = alt.Where(x => !bekannteStellungen.ContainsKey(x.Key)).Select(x => x.Key).ToArray();
      Array.Sort(gelöschte, (x, y) => string.CompareOrdinal(x, y));
      ArchivEintrag(gelöschte);
      alt = null;
      gelöschte = null;
      GC.Collect();

      return vorher - bekannteStellungen.Count;
    }
    #endregion

    #region # string KurzToString(string kurzKette) // wandelt eine Kurzschreibweise in ein vollständiges Spielfeld um
    /// <summary>
    /// wandelt eine Kurzschreibweise in ein vollständiges Spielfeld um
    /// </summary>
    /// <param name="kurzKette">String mit der Kurzschreibweise (enthält Spielerposition und Kistenpositionen kodiert)</param>
    /// <returns>fertig lesbares Spielfeld (inkl. Enter-Zeichen)</returns>
    string KurzToString(string kurzKette)
    {
      char[] ausgabe = feldLeer.ToArray();
      int pos = (int)kurzKette[0];
      ausgabe[pos] = ausgabe[pos] == '.' ? '+' : '@';
      kurzKette.Skip(1).Select(x => ausgabe[x] = ausgabe[x] == '.' ? '*' : '$').Count();
      return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x => ausgabe[x + y * feldBreite]).ToArray()) + "\r\n"));
    }
    #endregion
    #region # string StringToKurz(string langKette) // wandelt ein vollständiges Spielfeld in eine Kurzschreibweise um
    /// <summary>
    /// wandelt ein vollständiges Spielfeld in eine Kurzschreibweise um
    /// </summary>
    /// <param name="langKette">vollständiges Spielfeld inkl. Enterzeichen</param>
    /// <returns>Kurzschreibweise (enthält Spielerposition und Kistenpositionen kodiert)</returns>
    string StringToKurz(string langKette)
    {
      char[] ausgabe = new char[kistenAnzahl + 1];
      int p = 1;
      char[] lang = langKette.Replace("\r\n", "").ToCharArray();
      for (int i = 0; i < lang.Length; i++)
      {
        switch (lang[i])
        {
          case ' ': break;
          case '.': break;
          case '#': break;
          case 'x': break;
          case '*': ausgabe[p++] = (char)i; break;
          case '$': ausgabe[p++] = (char)i; break;
          case '@': ausgabe[0] = (char)i; break;
          case '+': ausgabe[0] = (char)i; break;
        }
      }
      return new string(ausgabe);
    }
    #endregion

    #region # string ToKurzKette() // gibt das eigene Spielfeld als kodierte Kurzschreibweise zurück
    /// <summary>
    /// gibt das eigene Spielfeld als kodierte Kurzschreibweise zurück
    /// </summary>
    /// <returns>fertige Kurzschreibweise</returns>
    string ToKurzKette()
    {
      char[] ausgabe = new char[kistenAnzahl + 1];
      int p = 1;
      for (int i = 0; i < feld.Length; i++)
      {
        switch (feld[i])
        {
          case ' ': break;
          case '.': break;
          case '#': break;
          case 'x': break;
          case '*': ausgabe[p++] = (char)i; break;
          case '$': ausgabe[p++] = (char)i; break;
          case '@': ausgabe[0] = (char)i; break;
          case '+': ausgabe[0] = (char)i; break;
        }
      }
      return new string(ausgabe);
    }
    #endregion
    #region # public override string ToString() // gibt das Spielfeld als lesbaren Inhalt aus
    /// <summary>
    /// gibt das gesamte Spielfeld als lesbaren (genormten) Inhalt aus (Format siehe: <see cref="http://de.wikipedia.org/wiki/Sokoban#Levelnotation">Wikipedia</see> )
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x => feld[x + y * feldBreite]).ToArray()) + "\r\n"));
    }
    #endregion

    #region # public IEnumerable<string> GetLösungsweg() // gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, wenn die Methode Next() ein false zurück liefert
    /// <summary>
    /// gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// </summary>
    /// <returns>Lösungsweg als einzelne Spielfelder</returns>
    public IEnumerable<string> GetLösungsweg()
    {
      string stellung = ToKurzKette();
      yield return KurzToString(stellung);
      while (bekannteStellungen.TryGetValue(stellung, out stellung))
      {
        if (stellung != "") yield return KurzToString(stellung);
      }
    }

    /// <summary>
    /// gibt die aktuelle Suchtiefe zurück
    /// </summary>
    public int SuchTiefe
    {
      get
      {
        return GetLösungsweg().Count() - 1;
      }
    }
    #endregion

    #region # public long KnotenAnzahl // gibt die Anzahl der bekannten Stellungen zurück
    /// <summary>
    /// gibt die Anzahl der bekannten Stellungen zurück
    /// </summary>
    public long KnotenAnzahl
    {
      get
      {
        if (archivSatzGröße > 0) return bekannteStellungen.Count + (archivSatzPos / archivSatzGröße);
        return bekannteStellungen.Count;
      }
    }
    #endregion
    #region # public int KnotenRest // gibt die Anzahl der noch zu prüfenden Knoten zurück
    /// <summary>
    /// gibt die Anzahl der noch zu prüfenden Knoten zurück
    /// </summary>
    public long KnotenRest
    {
      get
      {
        return prüfStellungen.Count;
      }
    }
    #endregion
  }
}
