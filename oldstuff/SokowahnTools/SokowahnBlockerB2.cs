#define parallelDeaktivieren
//#define parallelGeordnet

#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Compression;
using ngMax;
using ngMax.Zip;
using System.Windows.Forms;
#endregion

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// Klasse, welches ein Blocker-System verwaltet
  /// </summary>
  public class SokowahnBlockerB2 : ISokowahnBlocker
  {
    const int multi32k = 16;
    const int maxVorschau = 256;

    /// <summary>
    /// merkt sich den Basisraum
    /// </summary>
    SokowahnRaum basisRaum;

    /// <summary>
    /// merkt sich den Temporären Raum für die Kistensuche
    /// </summary>
    SokowahnRaum tempRaum;

    /// <summary>
    /// merkt sich den Namen der zugehörigen Blocker-Datei
    /// </summary>
    string blockerDatei;

    /// <summary>
    /// merkt sich zu den jeweils bekannten Blockern die Punktewertungen aller erlaubten Stellungen
    /// </summary>
    public ISokowahnHash[] bekannteBlockerHashes = new ISokowahnHash[0];

    SokowahnBlockerB.BlockerFeld[] bekannteBlocker = new SokowahnBlockerB.BlockerFeld[0];

    /// <summary>
    /// merkt sich die Statistiken aller Kisten-Stellungen
    /// </summary>
    public long[][] bekannteSammlerStats = new long[0][];

    #region # // --- BlockerStatus ---
    /// <summary>
    /// merkt sich den Status des Blockers
    /// </summary>
    enum BlockerStatus
    {
      /// <summary>
      /// Initialisierung
      /// </summary>
      init,
      /// <summary>
      /// alle möglichen Zielstellungen werden ermittelt
      /// </summary>
      sammleZiele,
      /// <summary>
      /// aktiver Suchmodus (rückwärts)
      /// </summary>
      suchModus,
      /// <summary>
      /// Blockersuche nach verbotenen Stellungen (vorwärts)
      /// </summary>
      blockerSuche,
      /// <summary>
      /// gibt an, dass der Blocker bereit zu lesen ist
      /// </summary>
      bereit,
    }

    /// <summary>
    /// merkt sich den aktuellen Blocker Aufbau-Status
    /// </summary>
    BlockerStatus blockerStatus = BlockerStatus.init;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="blockerDatei">Pfad zur Datei, worin sich eventuell archivierte Blocker-Daten befinden</param>
    /// <param name="basisRaum">Raum mit dem originalen Spielfeld</param>
    public SokowahnBlockerB2(string blockerDatei, SokowahnRaum basisRaum)
    {
      this.basisRaum = basisRaum;
      this.blockerDatei = blockerDatei;
      this.blockerStatus = BlockerStatus.init;
      //if (file.exists(blockerdatei))
      //{
      //  ladealleblocker();
      //}
    }
    #endregion

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
    /// gibt an, ob der Blocker sicher gerade im Erstellungsmodus befindet
    /// </summary>
    public bool ErstellungsModus
    {
      get
      {
        return blockerStatus != BlockerStatus.bereit;
      }
    }

    /// <summary>
    /// Anzahl der Kisten, welche momentan gesucht werden
    /// </summary>
    int sammlerKistenAnzahl = 0;

    /// <summary>
    /// merkt sich alle aktuell bekannten Sammler-Stellungen
    /// </summary>
    ISokowahnHash sammlerHash = null;

    /// <summary>
    /// Enumerator, welcher eine Abfrage bereit hält
    /// </summary>
    IEnumerator<SokowahnStellung> sammlerAbfrage = null;

    /// <summary>
    /// merkt sich die aktuellen Sammler-Statistiken
    /// </summary>
    long[] sammlerStats = null;

    #region # IEnumerable<SokowahnStellung> SammlerBerechneZielStellungen() // ermittelt alle Zielstellungen, welche mit der entsprechenden Anzahl der Kisten erreichbar sind
    /// <summary>
    /// ermittelt alle Zielstellungen, welche mit der entsprechenden Anzahl der Kisten erreichbar sind
    /// </summary>
    /// <returns>Enumerable aller möglichen Zielstellungen</returns>
    IEnumerable<SokowahnStellung> SammlerBerechneZielStellungen()
    {
      SokowahnRaum raum = new SokowahnRaum(basisRaum);
      raum.KistenAnzahl = sammlerKistenAnzahl;

      char[] feldData = basisRaum.FeldData;
      int feldBreite = basisRaum.FeldBreite;

      bool[] spielerRaum = SokowahnStaticTools.SpielfeldRaumScan(feldData, feldBreite);
      int[] raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();
      int[] feldZuRaum = Enumerable.Range(0, feldData.Length).Select(i => spielerRaum[i] ? raumZuFeld.ToList().IndexOf(i) : -1).ToArray();

      var sammlerKistenRaum = raumZuFeld.Select(i => (feldData[i] == '.' || feldData[i] == '*' || feldData[i] == '+') ? feldZuRaum[i] : -1).Where(i => i >= 0).ToArray();

      foreach (var variante in Tools.BerechneElementeVarianten(basisRaum.KistenAnzahl, sammlerKistenAnzahl, false))
      {
        raum.LadeStellung(variante, sammlerKistenRaum);
        foreach (var stellung in raum.GetVariantenBlockerZiele())
        {
          yield return stellung;
        }
      }
      yield break;
    }
    #endregion

    #region # bool SucheRückwärts(int limit) // Rückwärtssuche beginnend beim Ziel
    /// <summary>
    /// merkt sich die noch abzuarbeitenden Stellungen bei der Rückwärtssuche
    /// </summary>
    SokowahnLinearList2[] rückwärtsSucher = null;

    /// <summary>
    /// merkt sich die aktuelle Tiefe der Rückwärtssuche
    /// </summary>
    int rückwärtsTiefe = 0;

    /// <summary>
    /// merkt sich alle Räume für MultiThreading
    /// </summary>
    SokowahnRaum[] threadRäume = null;

    /// <summary>
    /// Rückwärtssuche beginnend beim Ziel
    /// </summary>
    /// <param name="limit">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    bool SucheRückwärts(int limit)
    {
      if (rückwärtsTiefe >= rückwärtsSucher.Length)
      {
        return false;
      }

      var liste = rückwärtsSucher[rückwärtsTiefe];

      limit = (int)Math.Min((long)limit, liste.SatzAnzahl);

      var stellungen = liste.Pop(limit);
      int satzGröße = liste.SatzGröße;

      var ergebnisse = Enumerable.Range(0, limit)
#if !parallelDeaktivieren
.AsParallel()
#if parallelGeordnet
.AsOrdered()
#endif
#endif
.SelectMany(stellung =>
{
  SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
  raum.LadeStellung(stellungen, stellung * satzGröße, 60000 - rückwärtsTiefe);
  return raum.GetVariantenRückwärts();
}).Where(x => { int find = sammlerHash.Get(x.crc64); return find == 65535 || find < x.zugTiefe; }).ToArray();

      foreach (var variante in ergebnisse)
      {
        int findZiel = sammlerHash.Get(variante.crc64);

        if (findZiel == 65535) // neue Stellung gefunden
        {
          sammlerHash.Add(variante.crc64, variante.zugTiefe);
          rückwärtsSucher[60000 - variante.zugTiefe].Add(variante.raumSpielerPos, variante.kistenZuRaum);
          alleStellungen.Add(variante.raumSpielerPos, variante.kistenZuRaum);
          sammlerStats[variante.zugTiefe]++;
        }
        else
        {
          if (variante.zugTiefe > findZiel) // wurde eine kürzere Variante zur einer bereits bekannten Stellung gefunden?
          {
            sammlerHash.Update(variante.crc64, variante.zugTiefe);
            rückwärtsSucher[60000 - variante.zugTiefe].Add(variante.raumSpielerPos, variante.kistenZuRaum);
            sammlerStats[variante.zugTiefe]++;
            sammlerStats[findZiel]--;
          }
          else continue;
        }
      }

      if (liste.SatzAnzahl == 0)
      {
        liste.Dispose();
        rückwärtsTiefe++;
        while (rückwärtsTiefe < rückwärtsSucher.Length && rückwärtsSucher[rückwärtsTiefe].SatzAnzahl == 0)
        {
          rückwärtsSucher[rückwärtsTiefe].Dispose();
          rückwärtsTiefe++;
        }
        if (rückwärtsTiefe < rückwärtsSucher.Length)
        {
          while (rückwärtsTiefe > rückwärtsSucher.Length - maxVorschau)
          {
            Array.Resize(ref rückwärtsSucher, rückwärtsSucher.Length + 1);
            rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2(sammlerKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", multi32k);
          }
        }
      }

      return true;
    }
    #endregion

    /// <summary>
    /// merkt sich die Anzahl der begehbaren Räume
    /// </summary>
    int raumAnzahl = 0;

    /// <summary>
    /// merkt sich bei der Rückwärtssuche alle Stellungen, welche gefunden wurden
    /// </summary>
    SokowahnLinearList2 alleStellungen = null;

    /// <summary>
    /// merkt sich alle bekannten Blocker-Hashes
    /// </summary>
    HashSet<ulong> alleBlockerHash = null;

    /// <summary>
    /// temporäres Blocker-Feld zum sammeln neuer Blocker-Stellungen
    /// </summary>
    SokowahnBlockerB.BlockerFeld[] tempBlocker;

    /// <summary>
    /// berechnet die nächsten Blocker
    /// </summary>
    /// <param name="limit">maximale Anzahl der Berechnungen, oder 0, wenn die Berechnung beendet werden soll</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      switch (blockerStatus)
      {
        #region # case BlockerStatus.init:
        case BlockerStatus.init:
        {
          sammlerKistenAnzahl++;
          if (sammlerKistenAnzahl == basisRaum.KistenAnzahl)
          {
            Abbruch();
            return false;
          }

          sammlerAbfrage = SammlerBerechneZielStellungen().GetEnumerator();
          sammlerHash = new SokowahnHash_Index16Multi();
          sammlerStats = new long[60001];
          rückwärtsSucher = Enumerable.Range(0, maxVorschau).Select(x => new SokowahnLinearList2(sammlerKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", multi32k)).ToArray();
          rückwärtsTiefe = 0;

          alleStellungen = new SokowahnLinearList2(sammlerKistenAnzahl + 1, Environment.CurrentDirectory + "\\temp\\", multi32k * 4);
          alleBlockerHash = new HashSet<ulong>();

          char[] feldData = basisRaum.FeldData;
          int feldBreite = basisRaum.FeldBreite;

          bool[] spielerRaum = SokowahnStaticTools.SpielfeldRaumScan(feldData, feldBreite);
          raumAnzahl = spielerRaum.Count(x => x);

          tempBlocker = new SokowahnBlockerB.BlockerFeld[raumAnzahl];
          for (int i = 0; i < tempBlocker.Length; i++) tempBlocker[i].kistenNummerLeer = sammlerKistenAnzahl;

          blockerStatus = BlockerStatus.sammleZiele;
          return true;
        }
        #endregion

        #region # case BlockerStatus.sammleZiele:
        case BlockerStatus.sammleZiele:
        {
          while (limit-- > 0 && sammlerAbfrage.MoveNext())
          {
            var satz = sammlerAbfrage.Current;
            if (sammlerHash.Get(satz.crc64) == 65535)
            {
              sammlerHash.Add(satz.crc64, 60000);
              sammlerStats[60000]++;
              rückwärtsSucher[0].Add(satz.raumSpielerPos, satz.kistenZuRaum);
              alleStellungen.Add(satz.raumSpielerPos, satz.kistenZuRaum);
            }
          }

          if (limit >= 0)
          {
            tempRaum = new SokowahnRaum(basisRaum);
            tempRaum.KistenAnzahl = sammlerKistenAnzahl;
            threadRäume = Enumerable.Range(0, 256).Select(i => new SokowahnRaum(tempRaum)).ToArray();
            blockerStatus = BlockerStatus.suchModus;
          }

          return true;
        }
        #endregion

        #region # case BlockerStatus.suchModus:
        case BlockerStatus.suchModus:
        {
          if (SucheRückwärts(limit)) return true;

          Array.Resize(ref bekannteBlockerHashes, sammlerKistenAnzahl);
          bekannteBlockerHashes[sammlerKistenAnzahl - 1] = sammlerHash;
          Array.Resize(ref bekannteSammlerStats, sammlerKistenAnzahl);
          var tmp = sammlerStats.Reverse().ToList();
          while (tmp[tmp.Count - 1] == 0) tmp.RemoveAt(tmp.Count - 1);
          bekannteSammlerStats[sammlerKistenAnzahl - 1] = tmp.ToArray();
          for (int i = 0; i < rückwärtsSucher.Length; i++) rückwärtsSucher[i].Dispose();
          blockerStatus = BlockerStatus.blockerSuche;

          return true;
        }
        #endregion

        #region # case BlockerStatus.blockerSuche:
        case BlockerStatus.blockerSuche:
        {
          limit = (int)Math.Min((long)limit, alleStellungen.SatzAnzahl);

          if (limit == 0)
          {
            int startPos = bekannteBlocker.Length;
            Array.Resize(ref bekannteBlocker, startPos + raumAnzahl);
            for (int i = 0; i < raumAnzahl; i++) bekannteBlocker[startPos + i] = tempBlocker[i];

            for (int i = 0; i < bekannteBlocker.Length; i++)
            {
              bekannteBlocker[i].geprüfteStellungen = sammlerHash.HashAnzahl;
              bekannteBlocker[i].kistenNummerLeer = sammlerKistenAnzahl + 1;
            }

            long geprüfteStellungenGesamt = 0;
            for (int i = 0; i < bekannteBlocker.Length; i += raumAnzahl) geprüfteStellungenGesamt += bekannteBlocker[i].geprüfteStellungen;
            for (int i = 0; i < bekannteBlocker.Length; i++) bekannteBlocker[i].Sortieren();

            blockerStatus = BlockerStatus.init;
            return true;
          }

          var stellungen = alleStellungen.Pop(limit);
          int satzGröße = alleStellungen.SatzGröße;

          var ergebnisse = Enumerable.Range(0, limit)
#if !parallelDeaktivieren
.AsParallel()
#if parallelGeordnet
.AsOrdered()
#endif
#endif
.SelectMany(stellung =>
          {
            SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
            raum.LadeStellung(stellungen, stellung * satzGröße, 0);
            //            return raum.GetVarianten(this).Where(x => sammlerHash.Get(x.crc64) == 65535);
            var ausgabe = raum.GetVarianten(this).Where(x =>
              {
                if (sammlerHash.Get(x.crc64) == 65535) return true;
                x.zugTiefe = 60000 - sammlerHash.Get(x.crc64);
                //string dbg = x.Debug(basisRaum);
                return false;
              }
              );
            return ausgabe;
          }).ToArray();

          foreach (var stellung in ergebnisse)
          {
            if (alleBlockerHash.Contains(stellung.crc64)) continue;
            alleBlockerHash.Add(stellung.crc64);
            tempBlocker[stellung.raumSpielerPos].Add(stellung.kistenZuRaum, sammlerKistenAnzahl);
          }

          return true;
        }
        #endregion

        #region # case BlockerStatus.bereit:
        case BlockerStatus.bereit:
        {
          return false;
        }
        #endregion

        default: throw new NotImplementedException();
      }
    }

    /// <summary>
    /// bricht die Berechnung vorzeitig ab (bereits fertig berechnete Blocker jedoch weiter genutzt werden)
    /// </summary>
    public void Abbruch()
    {
      blockerStatus = BlockerStatus.bereit;
      int maxKisten = basisRaum.FeldData.Where(c => c == '$' || c == '*').Count();
      for (int i = 0; i < bekannteBlocker.Length; i++) bekannteBlocker[i].kistenNummerLeer = maxKisten;
    }

    long merkStatAnzahl = 0;
    long merkStatSumme = 0;
    int merkStatCounter = 0;

    /// <summary>
    /// gibt den Inhalt des Blockers als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette, welche zurückgegeben wird</returns>
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      for (int s = 0; s < bekannteBlockerHashes.Length; s++)
      {
        long anzahl = bekannteSammlerStats[s].Sum(x => x);
        long summe = bekannteSammlerStats[s].Select((x, i) => new { x, i }).Sum(x => (long)x.i * x.x);

        sb.Append("[")
          .Append(s + 1)
          .Append("] ")
          .Append(bekannteBlockerHashes[s].HashAnzahl.ToString("#,##0") + " (" + (bekannteBlockerHashes[s].HashAnzahl / 1048576.0).ToString("#,##0.0") + " MB) - ")
          .Append("Tiefe: " + (summe / (double)anzahl).ToString("0.000"))
          .AppendLine();
      }

      switch (blockerStatus)
      {
        case BlockerStatus.init: sb.Append("[").Append(sammlerKistenAnzahl + 1).Append("] Init... ").AppendLine(); break;
        case BlockerStatus.sammleZiele: sb.Append("[").Append(sammlerKistenAnzahl).Append("] Ziele: " + rückwärtsSucher[0].SatzAnzahl.ToString("#,##0")).AppendLine(); break;
        case BlockerStatus.suchModus:
        {
          if (--merkStatCounter <= 0)
          {
            merkStatAnzahl = sammlerStats.Sum(x => x);
            merkStatSumme = sammlerStats.Select((x, i) => new { x, i }).Sum(x => (long)(60000 - x.i) * x.x);
            merkStatCounter = 10;
          }
          long anzahl = merkStatAnzahl;
          long summe = merkStatSumme;
          long satzAnzahl = rückwärtsSucher.Sum(x => x.SatzAnzahl);
          sb.Append("[")
            .Append(sammlerKistenAnzahl)
            .Append("] ")
            .Append("Suche: " + satzAnzahl.ToString("#,##0") + " / " + sammlerHash.HashAnzahl.ToString("#,##0") + " (" + (satzAnzahl * 100L / (double)sammlerHash.HashAnzahl).ToString("0.00") + " %) - ")
            .Append("Tiefe: " + (summe / (double)anzahl).ToString("0.000"))
            .AppendLine();
          if (sammlerKistenAnzahl > 2)
          {
            sb.AppendLine(new string(' ', satzAnzahl.ToString("#,##0").Length + 9) + "Max: " + ((double)bekannteBlockerHashes[sammlerKistenAnzahl - 2].HashAnzahl / (double)bekannteBlockerHashes[sammlerKistenAnzahl - 3].HashAnzahl * (double)bekannteBlockerHashes[sammlerKistenAnzahl - 2].HashAnzahl).ToString("#,##0"));
          }

          sb.AppendLine();
          foreach (string zeile in Enumerable.Range(0, rückwärtsSucher.Length).Where(i => rückwärtsSucher[i].SatzAnzahl > 0).Select(i => "    [" + i + "] " + rückwärtsSucher[i].ToString().Replace("Datensätze: ", "")))
          {
            sb.AppendLine(zeile);
          }
        } break;
        case BlockerStatus.blockerSuche:
        {
          long anzahl = sammlerStats.Sum(x => x);
          long summe = sammlerStats.Select((x, i) => new { x, i }).Sum(x => (long)(60000 - x.i) * x.x);
          long satzAnzahl = rückwärtsSucher.Sum(x => x.SatzAnzahl);
          sb.Append("[")
            .Append(sammlerKistenAnzahl)
            .Append("] ")
            .Append("BlockerSuche: " + alleStellungen.SatzAnzahl.ToString("#,##0"))
            .AppendLine();
        } break;
        case BlockerStatus.bereit: break;
      }

      return sb.ToString();
    }
  }
}
