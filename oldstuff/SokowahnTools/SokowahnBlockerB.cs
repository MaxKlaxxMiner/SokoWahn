// gibt an, ob der Parallel-Betrieb komplett deaktiviert werden soll (lansamer, übersichtlicher fürs Debuggen)
//#define parallelDeaktivieren

#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using ngMax;
using ngMax.Zip;
#endregion

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// Klasse, welches ein Blocker-System verwaltet
  /// </summary>
  public class SokowahnBlockerB : ISokowahnBlocker
  {
    /// <summary>
    /// merkt sich das Original-Spielfeld
    /// </summary>
    SokowahnRaum basisRaum;

    /// <summary>
    /// merkt sich die Anzahl der begehbaren Felder
    /// </summary>
    int raumAnzahl;

    /// <summary>
    /// gibt an, ob der Blocker gerade erstellt wird
    /// </summary>
    public bool ErstellungsModus
    {
      get
      {
        return status != BlockerStatus.Fertig;
      }
    }

    #region # struct BlockerFeld // Struktur eines Blocker-Feldes (für eine bestimmte Kisten-Anzahl)
    /// <summary>
    /// Struktur eines Blocker-Feldes (für eine bestimmte Kisten-Anzahl)
    /// </summary>
    public struct BlockerFeld
    {
      /// <summary>
      /// merkt sich die Anzahl der berechneten Stellungen, um diesen Blocker zu erstellen
      /// </summary>
      public long geprüfteStellungen;
      /// <summary>
      /// Anzahl der Kisten für diesen Blocker
      /// </summary>
      public int kistenAnzahl;
      /// <summary>
      /// Anzahl der bekannten Blocker-Einträge
      /// </summary>
      public int blockerAnzahl;
      /// <summary>
      /// eigentliche Blockerdaten (Länge: anzahlBlocker * blockerGröße)
      /// </summary>
      public int[] blockerData;

      /// <summary>
      /// Größe eines Blocker-Datensatzes (kistenAnzahl * 2)
      /// </summary>
      public int blockerGröße;
      /// <summary>
      /// gibt an, ob die Blocker sortiert sind
      /// </summary>
      public bool blockerSortiert;
      /// <summary>
      /// Kistennummer, welche angibt wenn das Feld leer ist
      /// </summary>
      public int kistenNummerLeer;

      /// <summary>
      /// fügt einen neuen Blocker in die Liste hinzu
      /// </summary>
      /// <param name="neuKisten">Kisten (mit SokowahnRaum-Positionen), welche hinzugefügt werden sollen</param>
      /// <param name="kistenAnzahl">Anzahl der einzulesenden Kisten im Array</param>
      public void Add(int[] neuKisten, int kistenAnzahl)
      {
        if (this.blockerSortiert) throw new Exception("Kein neuer Eintrag in sortierten Blockern möglich!");

        if (this.kistenAnzahl == 0)
        {
          this.kistenAnzahl = kistenAnzahl;
          this.blockerGröße = kistenAnzahl * 2;
          this.blockerAnzahl = 0;
          this.blockerData = new int[this.blockerGröße]; // Platz für ersten Datensatz schaffen
          this.blockerSortiert = false;
        }

        if (kistenAnzahl != this.kistenAnzahl) throw new Exception("Fehler, Anzahl der Kisten stimmt nicht überein!");

        int p = this.blockerAnzahl * this.blockerGröße; // Position des nächsten Datensatzes berechnen

        if (p == this.blockerData.Length) Array.Resize(ref this.blockerData, this.blockerData.Length * 2);

        for (int i = 0; i < kistenAnzahl; i++)
        {
          this.blockerData[p++] = neuKisten[i];
          this.blockerData[p++] = 0; // Sprung-Marker anfangs leer lassen
        }

        this.blockerAnzahl++;
      }

      /// <summary>
      /// prüft, ob ein Blocker in der Liste bekannt ist, welcher die aktuelle Kistenstellung verbietet
      /// </summary>
      /// <param name="raumKisten">Raumfeld mit den jeweils gesetzten Kisten</param>
      /// <returns>true, wenn die Stellung als verboten erkannt wurde</returns>
      public bool Check(int[] raumKisten)
      {
#if DEBUG
        if (this.blockerAnzahl == 0) return false; // keine Blocker bekannt
        if (!this.blockerSortiert) throw new Exception("Blocker nicht sortiert!");
#endif

        int pBis = blockerAnzahl * blockerGröße;
        for (int p = 0; p < pBis; )
        {
          for (int i = 0; i < kistenAnzahl; i++)
          {
            if (raumKisten[blockerData[p]] >= kistenNummerLeer) // bei nicht passender Kiste zum nächsten Blocker springen
            {
              p += blockerData[p + 1]; // Jumper vewenden
              goto nichtgefunden;
            }
            p += 2;
          }
          return true; // alle Kisten wurde gefunden -> Blocker erkannt

        nichtgefunden: continue;
        }

        return false;
      }

      /// <summary>
      /// sortiert einen Blocker-Bereich
      /// </summary>
      /// <param name="pos">Startposition im Blocker-Array</param>
      /// <param name="kistenIndex">Kisten, welche sortiert werden soll (zeigt auf die Kiste in 2er Schritten)</param>
      /// <param name="anzahl">Anzahl der zu sortierenden Blocker</param>
      void Sortiere(int pos, int kistenIndex, int anzahl)
      {
        int blockerGröße = this.blockerGröße;

        // --- Kisten nach Häufigkeit zählen (alle Kisten ab kistenIndex) ---
        Dictionary<int, int> counter = new Dictionary<int, int>();

        for (int b = 0; b < anzahl; b++)
        {
          for (int ki = kistenIndex; ki < kistenAnzahl * 2; ki += 2)
          {
            int kiste = blockerData[pos + b * blockerGröße + ki];
            if (counter.ContainsKey(kiste)) counter[kiste]++; else counter[kiste] = 1;
          }
        }

        // --- Kopie des Bereiches erstellen ---
        int[] tmpData = new int[anzahl * blockerGröße];
        for (int i = 0; i < tmpData.Length; i++) tmpData[i] = blockerData[pos + i];

        // --- Kisten innerhalb der Blocker nach Häufigkeit sortieren ---
        if (kistenAnzahl * 2 - kistenIndex > 2)
        {
          for (int b = 0; b < anzahl; b++)
          {
            for (int ki = kistenIndex; ki < kistenAnzahl * 2; ki += 2)
            {
              for (int ki2 = ki + 2; ki2 < kistenAnzahl * 2; ki2 += 2)
              {
                int d = counter[tmpData[b * blockerGröße + ki2]] - counter[tmpData[b * blockerGröße + ki]];
                if (d == 0) d = tmpData[b * blockerGröße + ki] - tmpData[b * blockerGröße + ki2];
                if (d > 0)
                {
                  int tmp = tmpData[b * blockerGröße + ki2];
                  tmpData[b * blockerGröße + ki2] = tmpData[b * blockerGröße + ki];
                  tmpData[b * blockerGröße + ki] = tmp;
                }
              }
            }
          }
        }

        // --- Kisten nach Häufigkeit neu zählen (nur die ersten Kisten auf kistenIndex) ---
        foreach (int k in counter.Select(x => x.Key).ToArray()) counter[k] = 0;
        for (int b = 0; b < anzahl; b++)
        {
          int kiste = tmpData[b * blockerGröße + kistenIndex];
          if (counter.ContainsKey(kiste)) counter[kiste]++; else counter[kiste] = 1;
        }

        // --- Kisten innerhalb der Blocker nach Häufigkeit nochmal neu sortieren ---
        if (kistenAnzahl * 2 - kistenIndex > 2)
        {
          for (int b = 0; b < anzahl; b++)
          {
            for (int ki = kistenIndex; ki < kistenAnzahl * 2; ki += 2)
            {
              for (int ki2 = ki + 2; ki2 < kistenAnzahl * 2; ki2 += 2)
              {
                int d = counter[tmpData[b * blockerGröße + ki2]] - counter[tmpData[b * blockerGröße + ki]];
                if (d == 0) d = tmpData[b * blockerGröße + ki] - tmpData[b * blockerGröße + ki2];
                if (d > 0)
                {
                  int tmp = tmpData[b * blockerGröße + ki2];
                  tmpData[b * blockerGröße + ki2] = tmpData[b * blockerGröße + ki];
                  tmpData[b * blockerGröße + ki] = tmp;
                }
              }
            }
          }
        }

        // --- Posis-Index nach Kistenhäufigkeit und Kistennummer sortieren ---
        int[] posis = Enumerable.Range(0, anzahl).Select(i => i * blockerGröße).ToArray();
        Array.Sort(posis, (x, y) =>
        {
          int d = counter[tmpData[y + kistenIndex]] - counter[tmpData[x + kistenIndex]];
          if (d == 0) d = tmpData[x + kistenIndex] - tmpData[y + kistenIndex];
          return d;
        });

        // --- Sortierte Daten zurück schreiben ---
        for (int b = 0; b < anzahl; b++)
        {
          for (int i = 0; i < blockerGröße; i++)
          {
            blockerData[pos + b * blockerGröße + i] = tmpData[posis[b] + i];
          }
        }

        // --- Sprungmarker berechnen und setzen ---
        for (int b = 0; b < anzahl; b++)
        {
          int p = pos + b * blockerGröße + kistenIndex;
          int überspringBlocker = 0;
          while (b + überspringBlocker + 1 < anzahl && blockerData[p] == blockerData[p + (überspringBlocker + 1) * blockerGröße])
          {
            überspringBlocker++;
          }

          if (kistenIndex + 2 < kistenAnzahl * 2)
          {
            Sortiere(pos + b * blockerGröße, kistenIndex + 2, überspringBlocker + 1); // nächste Kistenebene (rekursiv) Sortieren
          }

          b += überspringBlocker;

          // --- Sprungmarker setzen ---
          for (int i = 1; überspringBlocker >= 0; überspringBlocker--, i += blockerGröße)
          {
            blockerData[p + i] = überspringBlocker * blockerGröße + (blockerGröße - kistenIndex);
          }
        }

      }

      /// <summary>
      /// sortiert die Blocker-Daten
      /// </summary>
      public void Sortieren()
      {
        if (blockerSortiert || blockerAnzahl == 0) // nichts zu sortieren bzw. wurde schon sortiert
        {
          blockerSortiert = true;
          return;
        }

        Sortiere(0, 0, blockerAnzahl);

        blockerSortiert = true;
      }

      /// <summary>
      /// gibt den Aufbau als lesbaren String aus
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        if (blockerAnzahl == 0) return " - ";
        return " Kisten: " + kistenAnzahl + ", Blocker: " + blockerAnzahl.ToString("#,##0") + ", Aufwand: " + geprüfteStellungen.ToString("#,##0") + " ";
      }

      #region # public void Speichern(BinaryWriter schreib) // speichert den gesamten Blocker in einen Stream
      /// <summary>
      /// speichert den gesamten Blocker in einen Stream
      /// </summary>
      /// <param name="stream">Stream in den der Blocker gespeichert werden soll</param>
      public void Speichern(BinaryWriter schreib)
      {
        if (!this.blockerSortiert) throw new Exception("Blocker wurde noch nicht sortiert!");

        schreib.Write(geprüfteStellungen);
        schreib.Write(kistenAnzahl);
        schreib.Write(blockerAnzahl);

        int pBis = blockerAnzahl * blockerGröße;

        for (int i = 0; i < kistenAnzahl; i++)
        {
          for (int p = i * 2; p < pBis; )
          {
            int wert = blockerData[p];
            if (wert < 0xff)
            {
              schreib.Write((byte)wert);
            }
            else
            {
              schreib.Write((byte)0xff);
              schreib.Write((ushort)wert);
            }
            p += kistenAnzahl * 2;
          }
        }
      }
      #endregion

      #region # public void Laden(BinaryReader lese) // lädt den gesamten Blocker aus einem Stream
      /// <summary>
      /// lädt den gesamten Blocker aus einem Stream
      /// </summary>
      /// <param name="stream">Stream aus dem der Blocker geladen werden soll</param>
      public void Laden(BinaryReader lese)
      {
        geprüfteStellungen = lese.ReadInt64();
        kistenAnzahl = lese.ReadInt32();
        blockerAnzahl = lese.ReadInt32();

        blockerGröße = kistenAnzahl * 2;
        blockerSortiert = true;

        blockerData = new int[blockerAnzahl * blockerGröße];
        for (int i = 0; i < kistenAnzahl; i++)
        {
          for (int p = i * 2; p < blockerData.Length; )
          {
            int wert = (int)lese.ReadByte();
            if (wert == 255) wert = (int)lese.ReadUInt16();
            blockerData[p] = wert;
            p += kistenAnzahl * 2;
          }
        }

        if (blockerAnzahl > 0)
        {
          blockerSortiert = false;
          Sortieren();
        }
      }

      /// <summary>
      /// lädt den gesamten Blocker aus einem Stream
      /// </summary>
      /// <param name="stream">Stream aus dem der Blocker geladen werden soll</param>
      public void LadenOld(BinaryReader lese, int version)
      {
        geprüfteStellungen = lese.ReadInt64();
        kistenAnzahl = lese.ReadInt32();
        blockerAnzahl = lese.ReadInt32();

        blockerGröße = kistenAnzahl * 2;
        blockerSortiert = true;

        blockerData = new int[blockerAnzahl * blockerGröße];
        if (version == 103)
        {
          for (int p = 0; p < blockerData.Length; )
          {
            for (int i = 0; i < kistenAnzahl; i++)
            {
              blockerData[p++] = (int)lese.ReadUInt16();
              blockerData[p++] = lese.ReadInt32() + 1;
            }
          }
        }
        if (version == 105)
        {
          for (int p = 0; p < blockerData.Length; )
          {
            for (int i = 0; i < kistenAnzahl; i++)
            {
              blockerData[p++] = (int)lese.ReadUInt16();
              blockerData[p++] = lese.ReadInt32();
            }
          }
        }
      }
      #endregion
    }
    #endregion

    #region # enum BlockerStatus // merkt sich den Status des Blockers
    /// <summary>
    /// merkt sich den Status des Blockers
    /// </summary>
    enum BlockerStatus
    {
      /// <summary>
      /// Start einer Blocker-Sucher (eine neue Kistenanzahl wird ausprobiert)
      /// </summary>
      Init,

      /// <summary>
      /// sammelt alle Start-Stellungen mit der entsprechenden Kistenanzahl (sind automatisch auch gleichzeitig Stellungen, mit denen das Ziel erreichbar ist)
      /// </summary>
      SammleStartStellungen,

      /// <summary>
      /// sammelt alle Ziel-Stellungen, wo jede Kiste auf ein Zielfeld steht
      /// </summary>
      SammleZielStellungen,

      /// <summary>
      /// sucht vorwärts alle möglichen Varianten (eventuell bereits vorhandene Blocker werden beachten)
      /// </summary>
      SucheVarianten,

      /// <summary>
      /// ermittelt (anhand der Rückwärts-Suche) welche der ermittelten Stellungen zum Ziel führen können und markiert diese
      /// </summary>
      VerschmelzeZielStellungen,

      /// <summary>
      /// erstellt die Blocker anhand der restlichen Stellungen, welche nicht zum Ziel führten
      /// </summary>
      ErstelleBlocker,

      /// <summary>
      /// Blockersuche wurde beendet, (nur noch der Check-Methode steht bereit)
      /// </summary>
      Fertig,
    }

    /// <summary>
    /// merkt sich den aktuellen Status
    /// </summary>
    BlockerStatus status;
    #endregion

    /// <summary>
    /// merkt sich die aktuelle Anzahl der zu suchenden Kisten
    /// </summary>
    int suchKistenAnzahl;

    /// <summary>
    /// merkt sich alle bereits bekannten Blocker
    /// </summary>
    BlockerFeld[] bekannteBlocker;

    /// <summary>
    /// merkt sich den Namen der Blocker-Datei
    /// </summary>
    string blockerDatei;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="blockerDatei">Pfad zur Datei, worin sich eventuell archivierte Blocker-Daten befinden</param>
    /// <param name="basisRaum">Raum mit dem originalen Spielfeld</param>
    public SokowahnBlockerB(string blockerDatei, SokowahnRaum basisRaum)
    {
      this.basisRaum = basisRaum;
      bekannteBlocker = new BlockerFeld[0];
      status = BlockerStatus.Init;
      suchKistenAnzahl = 0;
      this.blockerDatei = blockerDatei;
      if (File.Exists(blockerDatei))
      {
        LadeAlleBlocker();
      }
    }

    /// <summary>
    /// bricht die Berechnung vorzeitig ab (bereits fertig berechnete Blocker jedoch weiter genutzt werden)
    /// </summary>
    public void Abbruch()
    {
      status = BlockerStatus.Fertig;
      int maxKisten = basisRaum.FeldData.Where(c => c == '$' || c == '*').Count();
      for (int i = 0; i < bekannteBlocker.Length; i++) bekannteBlocker[i].kistenNummerLeer = maxKisten;
    }

    /// <summary>
    /// merkt sich alle bekannten Stellungen (12345 = Prüfung steht noch aus, 60000 = Ziel wurde gefunden bzw. ist in dieser Stellung erreichbar)
    /// </summary>
    ISokowahnHash bekannteStellungen;

    /// <summary>
    /// Maximale Buffer-Größe einer Liste in Bytes
    /// </summary>
    const int listeMax = 16777216; // 16 MByte

    /// <summary>
    /// Prüfliste, welche gerade abgearbeitet wird
    /// </summary>
    SokowahnLinearList2 prüfListe;

    /// <summary>
    /// Prüfliste, zum sammeln der unbekannten Stellungen
    /// </summary>
    SokowahnLinearList2 prüfListeSammler;

    /// <summary>
    /// Prüfliste, zum merken aller möglicherweise Bösen Stellungen
    /// </summary>
    SokowahnLinearList2 prüfListeBöse;

    /// <summary>
    /// Prüfliste mit allen guten Stellungen, welche noch nicht verarbeitet wurden
    /// </summary>
    SokowahnLinearList2 prüfListeGut;

    #region # // --- Sammler ---
    /// <summary>
    /// merkt sich die aktuell prüfenden Kistenpositionen (länge = suchKistenAnzahl)
    /// </summary>
    int[] sammlerCheckKisten;

    /// <summary>
    /// merkt sich alle Kistenpositionen im SokowahnRaum
    /// </summary>
    int[] sammlerCheckKistenRaum;

    /// <summary>
    /// initialisiert die ersten Blocker-Kisten für den SammlerStart
    /// </summary>
    /// <param name="zielVariante">gibt an, ob nur Varianten ermittelt werden sollen, wo alle Kisten bereits auf dem Zielfeld stehen</param>
    void SammleKistenInit(bool zielVariante)
    {
      sammlerCheckKisten = Enumerable.Range(0, suchKistenAnzahl).Select(i => i).ToArray();
      sammlerCheckKisten[suchKistenAnzahl - 1]--; // letzte eins zurück setzen, damit beim ersten sammlerNext() auch die erste Variante gesetzt werden kann

      char[] feldData = basisRaum.FeldData;
      int feldBreite = basisRaum.FeldBreite;

      bool[] spielerRaum = SokowahnStaticTools.SpielfeldRaumScan(feldData, feldBreite);
      raumAnzahl = spielerRaum.Count(x => x);
      int[] raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();
      int[] feldZuRaum = Enumerable.Range(0, feldData.Length).Select(i => spielerRaum[i] ? raumZuFeld.ToList().IndexOf(i) : -1).ToArray();

      if (zielVariante)
      {
        sammlerCheckKistenRaum = raumZuFeld.Select(i => (feldData[i] == '.' || feldData[i] == '*' || feldData[i] == '+') ? feldZuRaum[i] : -1).Where(i => i >= 0).ToArray();
      }
      else
      {
        sammlerCheckKistenRaum = raumZuFeld.Select(i => (feldData[i] == '$' || feldData[i] == '*') ? feldZuRaum[i] : -1).Where(i => i >= 0).ToArray();
      }

      if (!zielVariante) // nur beim ersten mal Initialisieren
      {
        tmpRaum = new SokowahnRaum(feldData, feldBreite);
        tmpRaum.KistenAnzahl = suchKistenAnzahl;
        threadRäume = Enumerable.Range(0, 256).Select(i => new SokowahnRaum(tmpRaum)).ToArray();

        bekannteStellungen = new SokowahnHash_Index24Multi();

        prüfListe = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
        prüfListeSammler = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
        prüfListeGut = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
        prüfListeBöse = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
      }
    }

    /// <summary>
    /// Temp-Raum für die Berechnung
    /// </summary>
    SokowahnRaum tmpRaum;

    /// <summary>
    /// alle Räume für Multi-Threading
    /// </summary>
    SokowahnRaum[] threadRäume;

    /// <summary>
    /// setzt alle Kisten (Anzahl: suchKistenAnzahl) auf die nächste Kisten-Stellung
    /// </summary>
    /// <returns>true, wenn eine weitere Stellung berechnet wurde, false = alle Stellungen gesammelt</returns>
    bool SammleKistenNext()
    {
      #region # // --- nächste Kistenpositionen ermitteln ---
      switch (suchKistenAnzahl)
      {
        #region # case 1:
        case 1:
        {
          sammlerCheckKisten[0]++;
          if (sammlerCheckKisten[0] >= sammlerCheckKistenRaum.Length) return false;
        } break;
        #endregion
        #region # case 2:
        case 2:
        {
          sammlerCheckKisten[1]++;
          if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[0]++;
            sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
            if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 3:
        case 3:
        {
          sammlerCheckKisten[2]++;
          if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[1]++;
            if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[0]++;
              sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
            }
            sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
            if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 4:
        case 4:
        {
          sammlerCheckKisten[3]++;
          if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[2]++;
            if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[1]++;
              if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 2)
              {
                sammlerCheckKisten[0]++;
                sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
              }
              sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
            }
            sammlerCheckKisten[3] = sammlerCheckKisten[2] + 1;
            if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 5:
        case 5:
        {
          sammlerCheckKisten[4]++;
          if (sammlerCheckKisten[4] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[3]++;
            if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[2]++;
              if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length - 2)
              {
                sammlerCheckKisten[1]++;
                if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 3)
                {
                  sammlerCheckKisten[0]++;
                  sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
                }
                sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
              }
              sammlerCheckKisten[3] = sammlerCheckKisten[2] + 1;
            }
            sammlerCheckKisten[4] = sammlerCheckKisten[3] + 1;
            if (sammlerCheckKisten[4] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 6:
        case 6:
        {
          sammlerCheckKisten[5]++;
          if (sammlerCheckKisten[5] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[4]++;
            if (sammlerCheckKisten[4] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[3]++;
              if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length - 2)
              {
                sammlerCheckKisten[2]++;
                if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length - 3)
                {
                  sammlerCheckKisten[1]++;
                  if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 4)
                  {
                    sammlerCheckKisten[0]++;
                    sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
                  }
                  sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
                }
                sammlerCheckKisten[3] = sammlerCheckKisten[2] + 1;
              }
              sammlerCheckKisten[4] = sammlerCheckKisten[3] + 1;
            }
            sammlerCheckKisten[5] = sammlerCheckKisten[4] + 1;
            if (sammlerCheckKisten[5] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 7:
        case 7:
        {
          sammlerCheckKisten[6]++;
          if (sammlerCheckKisten[6] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[5]++;
            if (sammlerCheckKisten[5] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[4]++;
              if (sammlerCheckKisten[4] >= sammlerCheckKistenRaum.Length - 2)
              {
                sammlerCheckKisten[3]++;
                if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length - 3)
                {
                  sammlerCheckKisten[2]++;
                  if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length - 4)
                  {
                    sammlerCheckKisten[1]++;
                    if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 5)
                    {
                      sammlerCheckKisten[0]++;
                      sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
                    }
                    sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
                  }
                  sammlerCheckKisten[3] = sammlerCheckKisten[2] + 1;
                }
                sammlerCheckKisten[4] = sammlerCheckKisten[3] + 1;
              }
              sammlerCheckKisten[5] = sammlerCheckKisten[4] + 1;
            }
            sammlerCheckKisten[6] = sammlerCheckKisten[5] + 1;
            if (sammlerCheckKisten[6] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 8:
        case 8:
        {
          sammlerCheckKisten[7]++;
          if (sammlerCheckKisten[7] >= sammlerCheckKistenRaum.Length)
          {
            sammlerCheckKisten[6]++;
            if (sammlerCheckKisten[6] >= sammlerCheckKistenRaum.Length - 1)
            {
              sammlerCheckKisten[5]++;
              if (sammlerCheckKisten[5] >= sammlerCheckKistenRaum.Length - 2)
              {
                sammlerCheckKisten[4]++;
                if (sammlerCheckKisten[4] >= sammlerCheckKistenRaum.Length - 3)
                {
                  sammlerCheckKisten[3]++;
                  if (sammlerCheckKisten[3] >= sammlerCheckKistenRaum.Length - 4)
                  {
                    sammlerCheckKisten[2]++;
                    if (sammlerCheckKisten[2] >= sammlerCheckKistenRaum.Length - 5)
                    {
                      sammlerCheckKisten[1]++;
                      if (sammlerCheckKisten[1] >= sammlerCheckKistenRaum.Length - 6)
                      {
                        sammlerCheckKisten[0]++;
                        sammlerCheckKisten[1] = sammlerCheckKisten[0] + 1;
                      }
                      sammlerCheckKisten[2] = sammlerCheckKisten[1] + 1;
                    }
                    sammlerCheckKisten[3] = sammlerCheckKisten[2] + 1;
                  }
                  sammlerCheckKisten[4] = sammlerCheckKisten[3] + 1;
                }
                sammlerCheckKisten[5] = sammlerCheckKisten[4] + 1;
              }
              sammlerCheckKisten[6] = sammlerCheckKisten[5] + 1;
            }
            sammlerCheckKisten[7] = sammlerCheckKisten[6] + 1;
            if (sammlerCheckKisten[7] >= sammlerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        default: return false;
      }
      #endregion

      tmpRaum.LadeStellung(sammlerCheckKisten, sammlerCheckKistenRaum);

      if (status == BlockerStatus.SammleStartStellungen)
      {
        ulong crc = tmpRaum.Crc;

        foreach (var stellung in tmpRaum.GetVarianten(this))
        {
          if (bekannteStellungen.Get(stellung.crc64) < 65535) continue; // schon bekannt

          bekannteStellungen.Add(stellung.crc64, 12345);

          prüfListeSammler.Add(stellung.raumSpielerPos, stellung.kistenZuRaum); // unbekannte Stellungen aufnehmen
          prüfListeBöse.Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
        }
      }
      else
      {
        foreach (var stellung in tmpRaum.GetVariantenBlockerZiele())
        {
          int wert = bekannteStellungen.Get(stellung.crc64);

          if (wert < 65535) // schon bekannt?
          {
            if (wert == 60000) continue; // als "Gut-Stellung" bereits bekannt
            bekannteStellungen.Update(stellung.crc64, 60000);
            prüfListeGut.Add(stellung.raumSpielerPos, stellung.kistenZuRaum); // Ziel-Stellungen aufnehmen
            continue;
          }

          bekannteStellungen.Add(stellung.crc64, 60000);

          prüfListeGut.Add(stellung.raumSpielerPos, stellung.kistenZuRaum); // Ziel-Stellungen aufnehmen
          prüfListeSammler.Add(stellung.raumSpielerPos, stellung.kistenZuRaum); // zu den prüfenden Stellungen aufnehmen
        }
      }

      return true;
    }
    #endregion

    /// <summary>
    /// temporäres Blocker-Feld zum sammeln neuer Blocker-Stellungen
    /// </summary>
    BlockerFeld[] tempBlocker;

    /// <summary>
    /// merkt sich die Anzahl der restlichen zu verschmelzenden Stellungen (nur für die Anzeige)
    /// </summary>
    long verschmelzenRest = 0;

    /// <summary>
    /// speichert alle bekannten Blocker
    /// </summary>
    void SpeichereAlleBlocker()
    {
      BinaryWriter schreib = new BinaryWriter(new GZipOutputStream(new FileStream(blockerDatei, FileMode.Create, FileAccess.Write)));

      schreib.Write(106); // Version

      schreib.Write(bekannteBlocker.Length / raumAnzahl + 1);
      schreib.Write(raumAnzahl);
      schreib.Write(bekannteBlocker.Length);

      MemoryStream mem = new MemoryStream();
      BinaryWriter schreib2 = new BinaryWriter(mem);

      foreach (var blocker in bekannteBlocker) blocker.Speichern(schreib2);

      schreib2.Close();
      byte[] buf = mem.ToArray().RlePack();
      schreib.Write(buf.Length);
      schreib.Write(buf);

      schreib.Close();
    }

    /// <summary>
    /// lädt alle bekannten Blocker aus einer GZip-Datei
    /// </summary>
    void LadeAlleBlocker()
    {
      BinaryReader lese = new BinaryReader(new GZipInputStream(new FileStream(blockerDatei, FileMode.Open, FileAccess.Read)));

      int version = lese.ReadInt32();
      if (version != 103 && version != 105 && version != 106) throw new Exception("falsche Version Blocker-Datei: (" + version + ") " + blockerDatei);

      suchKistenAnzahl = lese.ReadInt32() - 1;
      raumAnzahl = lese.ReadInt32();
      bekannteBlocker = new BlockerFeld[lese.ReadInt32()];

      if (version == 106)
      {
        int len = lese.ReadInt32();
        byte[] buf = lese.ReadBytes(len);
        if (len != buf.Length) throw new Exception("Lesefehler!");
        buf = buf.RleUnpack<byte>().ToArray();
        BinaryReader lese2 = new BinaryReader(new RamStream(buf, false));

        for (int b = 0; b < bekannteBlocker.Length; b++)
        {
          bekannteBlocker[b].kistenNummerLeer = suchKistenAnzahl + 1;
          bekannteBlocker[b].Laden(lese2);
        }

        lese2.Dispose();
      }
      else
      {
        for (int b = 0; b < bekannteBlocker.Length; b++)
        {
          bekannteBlocker[b].kistenNummerLeer = suchKistenAnzahl + 1;
          bekannteBlocker[b].LadenOld(lese, version);
        }
      }

      lese.Close();
    }

    /// <summary>
    /// berechnet die nächsten Blocker
    /// </summary>
    /// <param name="limit">maximale Anzahl der Berechnungen, oder 0, wenn die Berechnung beendet werden soll</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      int maxKisten = basisRaum.FeldData.Where(c => c == '$' || c == '*').Count();

      if (limit <= 0) // Marker für Abbruch
      {
        return false;
      }

      switch (status)
      {
        #region # case BlockerStatus.Init: // Start einer Blocker-Sucher (eine neue Kistenanzahl wird ausprobiert)
        case BlockerStatus.Init:
        {
          if (suchKistenAnzahl + 1 >= maxKisten)
          {
            Abbruch();
            return false; // Kisten-Limit erreicht
          }

          suchKistenAnzahl++;

          SammleKistenInit(false);

          status = BlockerStatus.SammleStartStellungen;
          return true;
        }
        #endregion
        #region # case BlockerStatus.SammleStartStellungen: // sammelt alle Start-Stellungen mit der entsprechenden Kistenanzahl (sind automatisch auch gleichzeitig Stellungen, mit denen das Ziel erreichbar ist)
        case BlockerStatus.SammleStartStellungen:
        {
          limit--;
          while (limit > 0 && SammleKistenNext()) limit--;

          if (SammleKistenNext())
          {
            return true;
          }
          else
          {
            SammleKistenInit(true);

            status = BlockerStatus.SammleZielStellungen;
            return true;
          }
        }
        #endregion
        #region # case BlockerStatus.SammleZielStellungen: // sammelt alle Ziel-Stellungen, wo jede Kiste auf ein Zielfeld steht
        case BlockerStatus.SammleZielStellungen:
        {
          limit--;
          while (limit > 0 && SammleKistenNext()) limit--;

          if (!SammleKistenNext()) status = BlockerStatus.SucheVarianten;

          return true;
        }
        #endregion
        #region # case BlockerStatus.SucheVarianten: // sucht vorwärts alle möglichen Varianten (eventuell bereits vorhandene Blocker werden beachten)
        case BlockerStatus.SucheVarianten:
        {
          if (prüfListe.SatzAnzahl == 0)
          {
            prüfListe.Dispose();
            prüfListe = prüfListeSammler;
            prüfListeSammler = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
            if (prüfListe.SatzAnzahl == 0)
            {
              prüfListeSammler.Dispose();
              status = BlockerStatus.VerschmelzeZielStellungen;
              verschmelzenRest = prüfListeBöse.SatzAnzahl;
              return true;
            }
          }

          limit = (int)Math.Min((long)limit, prüfListe.SatzAnzahl);

          var ergebnisse = Enumerable.Range(0, limit).Select(i => prüfListe.Pop())
#if !parallelDeaktivieren
.AsParallel()
#endif
.SelectMany(stellung =>
          {
            SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
            raum.LadeStellung(stellung, 0, 0);
            return raum.GetVarianten(this);
          }).Where(x => bekannteStellungen.Get(x.crc64) == 65535).ToArray();

          foreach (var stellung in ergebnisse)
          {
            int find = bekannteStellungen.Get(stellung.crc64);
            if (find == 65535)
            {
              bekannteStellungen.Add(stellung.crc64, 12345);
              prüfListeSammler.Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
              prüfListeBöse.Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
            }
          }

          return true;
        }
        #endregion
        #region # case BlockerStatus.VerschmelzeZielStellungen: // ermittelt (anhand der Rückwärts-Suche) welche der ermittelten Stellungen zum Ziel führen können und markiert diese
        case BlockerStatus.VerschmelzeZielStellungen:
        {
          if (prüfListe.SatzAnzahl == 0)
          {
            prüfListe.Dispose();
            prüfListe = prüfListeGut;
            prüfListeGut = new SokowahnLinearList2(suchKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", listeMax / 32768);
            if (prüfListe.SatzAnzahl == 0)
            {
              prüfListe.Dispose();
              prüfListeGut.Dispose();
              prüfListe = null;
              prüfListeGut = null;
              status = BlockerStatus.ErstelleBlocker;
              tempBlocker = new BlockerFeld[raumAnzahl];
              for (int i = 0; i < tempBlocker.Length; i++)
              {
                tempBlocker[i].geprüfteStellungen = bekannteStellungen.HashAnzahl;
                tempBlocker[i].kistenNummerLeer = suchKistenAnzahl;
              }
              return true;
            }
          }

          limit = (int)Math.Min((long)limit, prüfListe.SatzAnzahl);
          verschmelzenRest -= (long)limit;

          var ergebnisse = Enumerable.Range(0, limit).Select(i => prüfListe.Pop()).AsParallel().SelectMany(stellung =>
          {
            SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
            raum.LadeStellung(stellung, 0, 0);
            return raum.GetVariantenRückwärts();
          }).Where(x => bekannteStellungen.Get(x.crc64) < 65535).ToArray();

          foreach (var stellung in ergebnisse)
          {
            int find = bekannteStellungen.Get(stellung.crc64);
            if (find == 60000)
            {
              continue;
            }
            else
            {
              prüfListeGut.Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
              bekannteStellungen.Update(stellung.crc64, 60000);
            }
          }

          return true;
        }
        #endregion
        #region # case BlockerStatus.ErstelleBlocker: // erstellt die Blocker anhand der restlichen Stellungen, welche nicht zum Ziel führten
        case BlockerStatus.ErstelleBlocker:
        {
          limit = (int)Math.Min((long)limit, prüfListeBöse.SatzAnzahl);

          var ergebnisse = Enumerable.Range(0, limit).Select(i => prüfListeBöse.Pop()).Select(x =>
          {
            tmpRaum.LadeStellung(x, 0, 0);
            return tmpRaum.GetStellung();
          }).Where(stellung => bekannteStellungen.Get(stellung.crc64) == 12345).ToArray();

          foreach (var stellung in ergebnisse)
          {
            tempBlocker[stellung.raumSpielerPos].Add(stellung.kistenZuRaum, suchKistenAnzahl);
          }

          if (prüfListeBöse.SatzAnzahl == 0)
          {
            int startPos = bekannteBlocker.Length;
            Array.Resize(ref bekannteBlocker, startPos + raumAnzahl);
            for (int i = 0; i < raumAnzahl; i++) bekannteBlocker[startPos + i] = tempBlocker[i];

            for (int i = 0; i < bekannteBlocker.Length; i++) bekannteBlocker[i].kistenNummerLeer = suchKistenAnzahl + 1;

            long geprüfteStellungenGesamt = 0;
            for (int i = 0; i < bekannteBlocker.Length; i += raumAnzahl) geprüfteStellungenGesamt += bekannteBlocker[i].geprüfteStellungen;
            for (int i = 0; i < bekannteBlocker.Length; i++) bekannteBlocker[i].Sortieren();

            if (geprüfteStellungenGesamt > 100000) SpeichereAlleBlocker();

            status = BlockerStatus.Init;
            bekannteStellungen = null;
          }

          return true;
        }
        #endregion
        #region # case BlockerStatus.Fertig: // Blockersuche wurde beendet, (nur noch der Check-Methode steht bereit)
        case BlockerStatus.Fertig: return false;
        #endregion

        default: throw new Exception("Status unbekant: " + status);
      }
    }

    /// <summary>
    /// prüft, ob eine bestimmte Stellung erlaubt ist
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition</param>
    /// <param name="raumZuKiste">zu prüfende Kistenpositionen direkt im Raum</param>
    /// <returns>true, wenn die Stellung erlaubt ist oder false, wenn anhand der Blocker eine verbotene Stellung erkannt wurde</returns>
    public bool CheckErlaubt(int spielerRaumPos, int[] raumZuKiste)
    {
      for (int p = spielerRaumPos; p < bekannteBlocker.Length; p += raumAnzahl) if (bekannteBlocker[p].Check(raumZuKiste)) return false;
      return true; // keine verbotene Stellung gefunden
    }

    /// <summary>
    /// schätzt den Aufwand (Anzahl der Hashes) für den nächsten Steiner
    /// </summary>
    long NextSchätzen
    {
      get
      {
        if (bekannteBlocker == null || bekannteBlocker.Length - raumAnzahl - raumAnzahl < 0) return 0;
        long letzteAnzahl = bekannteBlocker[bekannteBlocker.Length - raumAnzahl].geprüfteStellungen;
        long davorAnzahl = bekannteBlocker[bekannteBlocker.Length - raumAnzahl - raumAnzahl].geprüfteStellungen;
        if (letzteAnzahl == 0 || davorAnzahl == 0 || davorAnzahl > letzteAnzahl) return 0;
        return (long)((double)letzteAnzahl / (double)davorAnzahl * (double)letzteAnzahl);
      }
    }

    /// <summary>
    /// gibt den Inhalt als lesbares Spielfeld aus
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      StringBuilder ausgabe = new StringBuilder();

      for (int p = 0; p < bekannteBlocker.Length; p += raumAnzahl)
      {
        ausgabe.AppendLine("[" + (p / raumAnzahl + 1) + "] - " + Enumerable.Range(p, raumAnzahl).Sum(i => bekannteBlocker[i].blockerAnzahl) + " - " + bekannteBlocker[p].geprüfteStellungen.ToString("#,##0"));
      }

      switch (status)
      {
        case BlockerStatus.Init: if (suchKistenAnzahl > 0) ausgabe.AppendLine("[" + (suchKistenAnzahl + 1) + "] - Init" + (NextSchätzen > 0 ? " - " + NextSchätzen.ToString("#,##0") + " (max)" : "")); break;
        case BlockerStatus.SammleStartStellungen: ausgabe.AppendLine("[" + suchKistenAnzahl + "] - Starter: " + prüfListeSammler.SatzAnzahl.ToString("#,##0") + " / " + bekannteStellungen.HashAnzahl.ToString("#,##0")); break;
        case BlockerStatus.SammleZielStellungen: ausgabe.AppendLine("[" + suchKistenAnzahl + "] - Ziele: " + prüfListeGut.SatzAnzahl.ToString("#,##0") + " / " + bekannteStellungen.HashAnzahl.ToString("#,##0")); break;
        case BlockerStatus.SucheVarianten: ausgabe.AppendLine("[" + suchKistenAnzahl + "] - Suche: " + (prüfListe.SatzAnzahl + prüfListeSammler.SatzAnzahl).ToString("#,##0") + " / " + bekannteStellungen.HashAnzahl.ToString("#,##0") + " (" + ((double)(prüfListe.SatzAnzahl + prüfListeSammler.SatzAnzahl) * 100.0 / (double)bekannteStellungen.HashAnzahl).ToString("0.00") + " %" + (NextSchätzen > 0 ? ", " + NextSchätzen.ToString("#,##0") + " max." : "") + ")"); break;
        case BlockerStatus.VerschmelzeZielStellungen: ausgabe.AppendLine("[" + suchKistenAnzahl + "] - Verschmelzen: " + (prüfListe.SatzAnzahl + prüfListeGut.SatzAnzahl).ToString("#,##0") + " / " + bekannteStellungen.HashAnzahl.ToString("#,##0") + " (Rest: " + verschmelzenRest.ToString("#,##0") + ")"); break;
        case BlockerStatus.ErstelleBlocker: ausgabe.AppendLine("[" + suchKistenAnzahl + "] - Blocker erstellen: " + prüfListeBöse.SatzAnzahl.ToString("#,##0") + " / " + bekannteStellungen.HashAnzahl.ToString("#,##0") + " (" + Enumerable.Range(0, raumAnzahl).Sum(i => tempBlocker[i].blockerAnzahl).ToString("#,##0") + ")"); break;
        default: throw new Exception("?");
      }

      return ausgabe.ToString();
    }
  }
}
