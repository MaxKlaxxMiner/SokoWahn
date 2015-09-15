// gibt an, ob der Parallel-Betrieb komplett deaktiviert werden soll (lansamer, übersichtlicher fürs Debuggen)
#define parallelDeaktivieren

#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sokosolver.SokowahnTools;
using System.IO;
using System.Threading;
using ngMax;
using System.Windows.Forms;
#endregion

namespace Sokosolver
{
  public class SokoWahn_5th : SokoWahnInterface
  {
    int maxZüge = 30000;
    Dictionary<ulong, int> gefundenCrcs = new Dictionary<ulong, int>();

    #region # // --- statische Variablen ---
    /// <summary>
    /// Breite des Spielfeldes in Zeichen
    /// </summary>
    int feldBreite;

    /// <summary>
    /// Höhe des Spielfeldes in Zeichen
    /// </summary>
    int feldHöhe;

    /// <summary>
    /// Startposition des Spieler auf dem Spielfeld
    /// </summary>
    int feldSpielerStartPos;

    /// <summary>
    /// Inhalt des Spielfeldes (Größe: feldBreite * feldHöhe)
    /// </summary>
    char[] feldData;

    /// <summary>
    /// Inhalt des Spielfeldes, ohne Kisten und ohne Spieler (Größe: feldBreite * feldHöhe)
    /// </summary>
    char[] feldDataLeer;

    /// <summary>
    /// merkt sich die Basis-Daten und Startaufstellung der Kisten
    /// </summary>
    SokowahnRaum raumBasis;
    #endregion

    #region # // --- dynamische Variablen ---
    /// <summary>
    /// Hashtabelle aller bekannten Stellungen
    /// </summary>
    ISokowahnHash bekannteStellungen;

    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die vorwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2[] vorwärtsSucher;
    /// <summary>
    /// merkt sich die Punkte der vorwärtsSucher
    /// </summary>
    Dictionary<int, int>[] vorwärtsSucherPunkte;

    /// <summary>
    /// bereits vorwärts berechnete Schritte in die Tiefe
    /// </summary>
    int vorwärtsTiefe;

    /// <summary>
    /// Knoten, wo die Vorwärtssuche aktuell tatsächlich durchgeführt wird
    /// </summary>
    int vorwärtsTiefeAktuell;

    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die rückwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2[] rückwärtsSucher;

    /// <summary>
    /// bereits rückwärts berechnete Schritte in die Tiefe
    /// </summary>
    int rückwärtsTiefe;

    /// <summary>
    /// Hashtabelle der direkten Zielstellungen (wird durch die Rückwärts-Suche erweitert)
    /// </summary>
    ISokowahnHash zielStellungen;

    /// <summary>
    /// gibt an, ob die Endlösung gefunden wurde
    /// </summary>
    bool lösungGefunden = false;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="spielFeld">Spielfeld als Textzeilen</param>
    public SokoWahn_5th(string spielFeld)
    {
      SokowahnStaticTools.SpielfeldEinlesen(spielFeld, out feldBreite, out feldHöhe, out feldSpielerStartPos, out feldData, out feldDataLeer);

      raumBasis = new SokowahnRaum(feldData, feldBreite);

      Directory.CreateDirectory(TempOrdner);

      // --- Vorwärtssuche initialisieren ---
      bekannteStellungen = new SokowahnHash_Index24Multi();
      bekannteStellungen.Add(raumBasis.Crc, 0);
      vorwärtsTiefe = 0;
      vorwärtsTiefeAktuell = 0;
      vorwärtsSucher = new SokowahnLinearList2[0];
      vorwärtsSucherPunkte = new Dictionary<int, int>[0];
      VorwärtsAdd(raumBasis.GetStellung(), new SokowahnPunkte());

      // --- Zielstellungen und Rückwärtssuche initialisieren ---
      zielStellungen = new SokowahnHash_Index24Multi();
      rückwärtsTiefe = 0;
      rückwärtsSucher = new SokowahnLinearList2[0];

      foreach (SokowahnStellung stellung in SokowahnStaticTools.SucheZielStellungen(raumBasis))
      {
        zielStellungen.Add(stellung.crc64, 60000);
        RückwärtsAdd(stellung);
      }

      ulong spielFeldCrc = Crc64.Start.Crc64Update(raumBasis.FeldBreite, raumBasis.FeldHöhe, raumBasis.FeldData);

      blocker = new SokowahnBlockerB2(Environment.CurrentDirectory + "\\temp\\blocker2_x" + spielFeldCrc.ToString("x").PadLeft(16, '0') + ".gz", raumBasis);
    }
    #endregion

    #region # string TempOrdner // gibt den temporären Ordner zurück
    /// <summary>
    /// gibt den temporären Ordner zurück
    /// </summary>
    string TempOrdner
    {
      get
      {
        return Environment.CurrentDirectory + "\\temp\\";
      }
    }
    #endregion

    #region # // --- Sucher-Listen (vorwärts und rückwärts) ---
    /// <summary>
    /// trägt eine noch zu prüfende Stellung in die Vorwärtssuche ein
    /// </summary>
    /// <param name="stellung">Stellung, welche eingetragen werden soll</param>
    void VorwärtsAdd(SokowahnStellung stellung, SokowahnPunkte punkte)
    {
      int tiefePos = stellung.zugTiefe;

      while (tiefePos >= vorwärtsSucher.Length)
      {
        Array.Resize(ref vorwärtsSucher, vorwärtsSucher.Length + 1);
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1 + 2, TempOrdner);
        Array.Resize(ref vorwärtsSucherPunkte, vorwärtsSucherPunkte.Length + 1);
        vorwärtsSucherPunkte[vorwärtsSucherPunkte.Length - 1] = new Dictionary<int, int>();
      }

      vorwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum, punkte);
      vorwärtsSucherPunkte[tiefePos][punkte.tiefeMax] = vorwärtsSucherPunkte[tiefePos].TryGetValue(punkte.tiefeMax, 0) + 1;
    }

    /// <summary>
    /// trägt eine noch zu prüfende Stellung in die Vorwärtssuche ein
    /// </summary>
    /// <param name="stellung">Stellung, welche eingetragen werden soll</param>
    void VorwärtsAdd(SokowahnStellungRun stellung, SokowahnPunkte punkte)
    {
      int tiefePos = stellung.zugTiefe;

      while (tiefePos >= vorwärtsSucher.Length)
      {
        Array.Resize(ref vorwärtsSucher, vorwärtsSucher.Length + 1);
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1 + 2, TempOrdner);
        Array.Resize(ref vorwärtsSucherPunkte, vorwärtsSucherPunkte.Length + 1);
        vorwärtsSucherPunkte[vorwärtsSucherPunkte.Length - 1] = new Dictionary<int, int>();
      }

      vorwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum, punkte);
      vorwärtsSucherPunkte[tiefePos][punkte.tiefeMax] = vorwärtsSucherPunkte[tiefePos].TryGetValue(punkte.tiefeMax, 0) + 1;
    }

    /// <summary>
    /// trägt eine noch zu prüfende Stellung in die Rückwärtssuche ein
    /// </summary>
    /// <param name="stellung">Stellung, welche eingetragen werden soll</param>
    void RückwärtsAdd(SokowahnStellung stellung)
    {
      int tiefePos = 60000 - stellung.zugTiefe;

      while (tiefePos >= rückwärtsSucher.Length)
      {
        Array.Resize(ref rückwärtsSucher, rückwärtsSucher.Length + 1);
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1, TempOrdner, 16);
      }

      rückwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
    }
    #endregion

    /// <summary>
    /// merkt sich alle Blocker-Stellungen (um bei der Vorwärtssuche sinnlose Stellungen auszulassen)
    /// </summary>
    ISokowahnBlocker blocker;

    /// <summary>
    /// merkt sich alle Räume für die gleichzeitige Berechnung in mehreren Threads
    /// </summary>
    SokowahnRaum[] raumPool;

    #region # static int RechneMinZügeKiste(SokowahnRaum raum) // berechnet die Anzahl der Schritte um eine einzelne Kiste zum Ziel zu bewegen
    /// <summary>
    /// Rekursive Suchmethode einer einzelnen Kiste
    /// </summary>
    /// <param name="raum">Raum mit der einzelnen Kiste und Spielerstellung</param>
    /// <returns>Mindestzahl der Schritte zum Ziel (oder 60000 = wenn kein Ziel möglich)</returns>
    int RechneMindestZüge(SokowahnRaum raum)
    {
      List<List<SokowahnStellung>> list = new List<List<SokowahnStellung>>();
      list.Add(new List<SokowahnStellung>(raum.GetStellung().SelfEnumerable()));
      int listTiefe = 0;

      SokowahnHash_Index0 hash = new SokowahnHash_Index0();

      while (listTiefe < list.Count)
      {
        foreach (var stellung in list[listTiefe])
        {
          raum.LadeStellung(stellung);
          if (raumZiele[stellung.kistenZuRaum[0]]) return listTiefe;

          foreach (var variante in raum.GetVarianten())
          {
            int find = hash.Get(variante.crc64);
            if (find < 65535)
            {
              if (find <= variante.zugTiefe) continue;
              hash.Update(variante.crc64, variante.zugTiefe);
            }
            else
            {
              hash.Add(variante.crc64, variante.zugTiefe);
            }
            while (list.Count <= variante.zugTiefe)
            {
              list.Add(new List<SokowahnStellung>());
            }
            list[variante.zugTiefe].Add(variante);
          }
        }
        listTiefe++;
      }

      return 60000;
    }

    /// <summary>
    /// berechnet die Anzahl der Schritte um eine einzelne Kiste zum Ziel zu bewegen
    /// </summary>
    /// <param name="raum">Raum mit der einzelnen Kiste</param>
    /// <returns>Mindestzahl der Schritte zum Ziel (oder 60000 = wenn kein Ziel möglich)</returns>
    int RechneMinZügeKiste(SokowahnRaum raum)
    {
      var varianten = raum.GetVariantenBlockerZiele().ToArray();

      int mindest = 60000;

      foreach (var variante in varianten)
      {
        raum.LadeStellung(variante);
        raum.spielerZugTiefe = 0;

        int i = RechneMindestZüge(raum);

        if (i < mindest) mindest = i;
      }

      return mindest;
    }
    #endregion

    #region # bool SucheVorwärts(int limit) // Normale Suche nach vorne (von der Startstellung aus beginnend)
    /// <summary>
    /// merkt sich die minimale Anzahl der Züge bei den einzelnen Kisten um ein Ziel zu erreichen (60000 = keine Lösung möglich)
    /// </summary>
    int[] einzelKistenDauer = null;

    /// <summary>
    /// merkt sich die Lauffelder
    /// </summary>
    ushort[] laufFelder = null;

    /// <summary>
    /// merkt sich die Zielfelder im Raum
    /// </summary>
    bool[] raumZiele = null;

    /// <summary>
    /// Normale Suche nach vorne (von der Startstellung aus beginnend)
    /// </summary>
    /// <param name="limit">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    bool SucheVorwärts(int limit)
    {
      if (limit == 0)
      {
        vorwärtsTiefeAktuell = vorwärtsTiefe;
        return true;
      }

      if (vorwärtsTiefe >= vorwärtsSucher.Length) return false;

      if (limit > 1111100000)
      {
        maxZüge = limit - 1111100000 + 1;
        for (int i = maxZüge; i < vorwärtsSucher.Length; i++)
        {
          vorwärtsSucher[i].Dispose();
          vorwärtsSucher[i] = null;
          vorwärtsSucherPunkte[i].Clear();
          vorwärtsSucherPunkte[i] = null;
        }
        Array.Resize(ref vorwärtsSucher, Math.Min(vorwärtsSucher.Length, maxZüge));
        Array.Resize(ref vorwärtsSucherPunkte, vorwärtsSucher.Length);
        MessageBox.Show("Maxzüge gesetzt auf: " + (maxZüge - 1));
        vorwärtsTiefeAktuell = vorwärtsTiefe;
        return true;
      }

      bool schnell = limit >= 100000;

      var liste = vorwärtsSucher[vorwärtsTiefeAktuell];
      var listePunkte = vorwärtsSucherPunkte[vorwärtsTiefeAktuell];

      //if (liste.SatzAnzahl * 2L > (long)limit)
      //{
      //  if (liste.SatzAnzahl < (long)limit * 10L)
      //  {
      //    limit = (int)(liste.SatzAnzahl / 2L);
      //  }
      //}

      limit = (int)Math.Min((long)limit, liste.SatzAnzahl);

      #region # // --- raumPool abfragen bzw. erstellen ---
      if (raumPool == null)
      {
        raumPool = Enumerable.Range(0, 256).Select(x => new SokowahnRaum(raumBasis, blocker, bekannteStellungen, zielStellungen)).ToArray();

        SokowahnRaum r = new SokowahnRaum(raumPool[0]);

        r.KistenAnzahl = 1;

        int[] kistePos = new int[1];
        int[] kisteIndex = new int[1];
        int raumAnzahl = r.RaumAnzahl;
        einzelKistenDauer = new int[raumAnzahl];

        char[] feldLeer = raumPool[0].FeldDataLeer;
        bool[] spielerRaum = SokowahnStaticTools.SpielfeldRaumScan(feldData, feldBreite);
        int[] raumZuFeld = Enumerable.Range(0, spielerRaum.Length).Where(i => spielerRaum[i]).ToArray();
        raumZiele = new bool[raumAnzahl];
        for (int i = 0; i < raumAnzahl; i++) raumZiele[i] = feldLeer[raumZuFeld[i]] == '.';

        for (int k = 0; k < raumAnzahl; k++)
        {
          kistePos[0] = k;
          r.LadeStellung(kisteIndex, kistePos);
          einzelKistenDauer[k] = RechneMinZügeKiste(r);
        }

        laufFelder = new ushort[raumAnzahl * raumAnzahl];

        ushort[] laufPosis = new ushort[raumAnzahl + 1];
        for (int y = 0; y < laufFelder.Length; y += raumAnzahl)
        {
          int pp = 0;
          laufPosis[pp++] = (ushort)(y / raumAnzahl);
          for (int x = 0; x < raumAnzahl; x++)
          {
            if (x > pp) throw new Exception("Fatal!");
            ushort p = laufPosis[x];
            laufFelder[y + x] = p;
            if ((laufPosis[pp] = (ushort)r.raumOben[p]) < raumAnzahl && !laufPosis.Take(pp).Any(i => i == laufPosis[pp])) pp++;
            if ((laufPosis[pp] = (ushort)r.raumRechts[p]) < raumAnzahl && !laufPosis.Take(pp).Any(i => i == laufPosis[pp])) pp++;
            if ((laufPosis[pp] = (ushort)r.raumUnten[p]) < raumAnzahl && !laufPosis.Take(pp).Any(i => i == laufPosis[pp])) pp++;
            if ((laufPosis[pp] = (ushort)r.raumLinks[p]) < raumAnzahl && !laufPosis.Take(pp).Any(i => i == laufPosis[pp])) pp++;
          }
        }

      }

      #endregion

      #region # // --- Teilliste mit den besten Stellungen erzeugen (falls notwendig) ---
      if (liste.SatzAnzahl > limit)
      {
        int punkteOk = 0;
        int findAnzahl = 0;
        int gutDazu = 0;
        foreach (var satz in listePunkte.OrderBy(x => x.Key))
        {
          punkteOk = satz.Key;
          findAnzahl += satz.Value;
          if (findAnzahl >= limit)
          {
            gutDazu = limit - (findAnzahl - satz.Value);
            break;
          }
        }

        var listeOk = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1 + 2, TempOrdner);
        var listeAufheben = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1 + 2, TempOrdner);

        long bis = liste.SatzAnzahl;
        for (long i = 0; i < bis; i++)
        {
          var stellung = liste.Pop();
          SokowahnPunkte punkte = new SokowahnPunkte(stellung);
          if (punkte.tiefeMax <= punkteOk)
          {
            if (punkte.tiefeMax == punkteOk)
            {
              if (gutDazu > 0)
              {
                gutDazu--;
              }
              else
              {
                listeAufheben.Add(stellung);
                continue;
              }
            }
            listeOk.Add(stellung);
            if (listeOk.SatzAnzahl == limit) break;
          }
          else
          {
            listeAufheben.Add(stellung);
          }
        }

        if (liste.SatzAnzahl < listeAufheben.SatzAnzahl)
        {
          bis = liste.SatzAnzahl;
          for (long i = 0; i < bis; i++) listeAufheben.Add(liste.Pop());
          vorwärtsSucher[vorwärtsTiefeAktuell] = listeAufheben;
          liste.Dispose();
        }
        else
        {
          bis = listeAufheben.SatzAnzahl;
          for (long i = 0; i < bis; i++) liste.Add(listeAufheben.Pop());
          listeAufheben.Dispose();
        }

        liste = listeOk;
      }

#if DEBUG
      if (limit != liste.SatzAnzahl) throw new Exception("aua?");
#endif
      #endregion

      SokowahnRaum raum = raumPool[Thread.CurrentThread.ManagedThreadId];

      int mx = maxZüge - rückwärtsTiefe;
      var ergebnisse = Enumerable.Range(0, limit).Select(i => liste.Pop()).SelectMany(stellung =>
      {
        raum.LadeStellung(stellung, vorwärtsTiefeAktuell);
        SokowahnPunkte punkte = new SokowahnPunkte(stellung);

        listePunkte[punkte.tiefeMax]--;
        if (bekannteStellungen.Get(raum.Crc) < vorwärtsTiefeAktuell) return Enumerable.Empty<SokowahnStellungRun>();

        return raum.GetVariantenRun().Where(v => v.zugTiefe <= mx && v.zugTiefe < bekannteStellungen.Get(v.crc64));
      }).ToArray();

#if !parallelDeaktivieren
      var punkteListe = new SokowahnPunkte[ergebnisse.Length];

      if (schnell)
      {
        ParallelEnumerable.Range(0, ergebnisse.Length).Select(v =>
        {
          SokowahnRaum r = raumPool[Thread.CurrentThread.ManagedThreadId];
          r.LadeStellung(ergebnisse[v]);
          punkteListe[v] = r.BerechnePunkte2(einzelKistenDauer, laufFelder);
          //punkteListe[v] = r.BerechnePunkteSchnell(einzelKistenDauer);
          return true;
        }).Count();
      }
      else
      {
        ParallelEnumerable.Range(0, ergebnisse.Length).Select(v =>
        {
          SokowahnRaum r = raumPool[Thread.CurrentThread.ManagedThreadId];
          r.LadeStellung(ergebnisse[v]);
          punkteListe[v] = r.BerechnePunkte(einzelKistenDauer);
          return true;
        }).Count();
      }
#endif

      for (int v = 0; v < ergebnisse.Length; v++)
      {
        var variante = ergebnisse[v];
        int findQuelle = bekannteStellungen.Get(variante.crc64);

#if parallelDeaktivieren
        raum.LadeStellung(variante);
        //        SokowahnPunkte punkte = raum.BerechnePunkte(einzelKistenDauer);
        SokowahnPunkte punkte = raum.BerechnePunkte2(einzelKistenDauer, laufFelder);
        //        SokowahnPunkte punkte = raum.BerechnePunkte3(einzelKistenDauer, laufFelder);
#else
        var punkte = punkteListe[v];
#endif

        if (variante.zugTiefe < findQuelle) // neue Stellung oder bessere Variante gefunden
        {
          int findZiel = zielStellungen.Get(variante.crc64);
          if (findZiel < 65535)
          {
            if (variante.zugTiefe + 60000 - findZiel < maxZüge)
            {
              #region // --- neue (bessere) Variante gefunden ---
              maxZüge = variante.zugTiefe + 60000 - findZiel;
              gefundenCrc = variante.crc64;

              for (int i = maxZüge; i < vorwärtsSucher.Length; i++)
              {
                vorwärtsSucher[i].Dispose();
                vorwärtsSucher[i] = null;
                vorwärtsSucherPunkte[i].Clear();
                vorwärtsSucherPunkte[i] = null;
              }
              if (maxZüge < vorwärtsSucher.Length)
              {
                Array.Resize(ref vorwärtsSucher, maxZüge);
                Array.Resize(ref vorwärtsSucherPunkte, maxZüge);
              }
              #endregion
            }
            continue;
          }
          //if (punkte.tiefeMin + variante.zugTiefe < maxZüge)
          //if (variante.zugTiefe + rückwärtsTiefe < maxZüge)
          if (variante.zugTiefe + rückwärtsTiefe < maxZüge && punkte.tiefeMin + variante.zugTiefe < maxZüge)
          {
            if (findQuelle < 65535) bekannteStellungen.Update(variante.crc64, variante.zugTiefe); else bekannteStellungen.Add(variante.crc64, variante.zugTiefe);
            VorwärtsAdd(variante, punkte);
          }
        }
      }

      vorwärtsTiefeAktuell++;

      while (vorwärtsTiefe < vorwärtsSucher.Length && vorwärtsSucher[vorwärtsTiefe].SatzAnzahl == 0)
      {
        vorwärtsTiefe++;
        vorwärtsTiefeAktuell = vorwärtsTiefe;
      }

      if (vorwärtsTiefeAktuell == vorwärtsSucher.Length)
      {
        vorwärtsTiefeAktuell = vorwärtsTiefe;
      }

      return true;
    }
    #endregion

    #region # bool SucheRückwärts(int limit) // Rückwärtssuche beginnend beim Ziel
    /// <summary>
    /// Rückwärtssuche beginnend beim Ziel
    /// </summary>
    /// <param name="limit">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    bool SucheRückwärts(int limit)
    {
      if (rückwärtsTiefe == rückwärtsSucher.Length)
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
#endif
.SelectMany(stellung =>
{
  SokowahnRaum raum = raumPool[Thread.CurrentThread.ManagedThreadId];
  raum.LadeStellung(stellungen, stellung * satzGröße, 60000 - rückwärtsTiefe);
  return raum.GetVariantenRückwärts();
}).Where(x => { int find = zielStellungen.Get(x.crc64); return find == 65535 || find < x.zugTiefe; }).ToArray();

      foreach (var variante in ergebnisse)
      {
        int findZiel = zielStellungen.Get(variante.crc64);

        if (findZiel == 65535) // neue Stellung gefunden
        {
          zielStellungen.Add(variante.crc64, variante.zugTiefe);
          RückwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
        }
        else
        {
          if (variante.zugTiefe > findZiel) // wurde eine kürzere Variante zur einer bereits bekannten Stellung gefunden?
          {
            zielStellungen.Update(variante.crc64, variante.zugTiefe);
            int findQuelle = bekannteStellungen.Get(variante.crc64);
            if (findQuelle < 65535 && 60000 - variante.zugTiefe + findQuelle < maxZüge)
            {
              int stop = 0;
            }
            //if (variante.crc64 == gefundenStellung.crc64)
            //{
            //  gefundenTiefe -= variante.zugTiefe - findZiel;
            //  //       gefundenStellung.zugTiefe = variante.zugTiefe;
            //}
            //if (60000 - variante.zugTiefe + vorwärtsTiefe + 1 < gefundenTiefe)
            RückwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
          else continue;
        }

        //int findTiefe = 60000 - variante.zugTiefe + variante.findHash; // aktuelle Gesamttiefe der Lösung ermitteln

        //if (findTiefe < gefundenTiefe) // bessere Lösung gefunden?
        //{
        //  gefundenTiefe = findTiefe;
        //  gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
        //  gefundenStellung.zugTiefe = variante.findHash;
        //}
      }

      if (liste.SatzAnzahl == 0)
      {
        liste.Dispose();
        rückwärtsTiefe++;
        rückwärtsAktiv = false;
      }

      return true;
    }
    #endregion

    #region # // --- Public Methoden ---
    /// <summary>
    /// merkt sich, ob gerade die nächste Rückwärts-Stufe berechnet werden soll
    /// </summary>
    bool rückwärtsAktiv = false;

    /// <summary>
    /// berechnet den nächsten Schritt
    /// </summary>
    /// <param name="limit">Anzahl der zu berechnenden (kleinen) Arbeitsschritte, negativer Wert = optionale Vorbereitungsschritte)</param>
    /// <returns>gibt an, ob noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      if (limit >= 0)
      {
        if (blocker.ErstellungsModus) blocker.Abbruch();

        if (zielStellungen.HashAnzahl >= bekannteStellungen.HashAnzahl && !rückwärtsAktiv) return SucheVorwärts(limit);

        rückwärtsAktiv = true;
        return SucheRückwärts(limit); // Rückwärts
      }
      else
      {
        return blocker.Next(-limit);
      }
    }

    /// <summary>
    /// entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// </summary>
    /// <returns>Anzahl der Einträge, welche entfernt werden konnten</returns>
    public long Refresh()
    {
      return 0;
    }

    /// <summary>
    /// gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// </summary>
    /// <returns>Lösungsweg als einzelne Spielfelder</returns>
    public IEnumerable<string> GetLösungsweg()
    {
      if (lösungGefunden) // Lösung gefunden?
      {
        yield break;
      }
    }

    /// <summary>
    /// gibt die aktuelle Suchtiefe zurück
    /// </summary>
    public int SuchTiefe
    {
      get
      {
        return 0;
      }
    }
    #endregion

    #region # // --- Public Properties ----
    /// <summary>
    /// gibt die Anzahl der bekannten Stellungen zurück
    /// </summary>
    public long KnotenAnzahl
    {
      get
      {
        return bekannteStellungen.HashAnzahl;
      }
    }

    /// <summary>
    /// gibt die Anzahl der noch zu berechnenden Knoten zurück (kann sich nach einer Berechnung erhöhen)
    /// </summary>
    /// <returns>Anzahl der noch zu berechnenden Knoten</returns>
    public long KnotenRest
    {
      get
      {
        return vorwärtsSucher.Sum(x => x.SatzAnzahl);
      }
    }

    /// <summary>
    /// gibt das gesamte Spielfeld als lesbaren (genormten) Inhalt aus (Format siehe: <see cref="http://de.wikipedia.org/wiki/Sokoban#Levelnotation">Wikipedia</see> )
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      StringBuilder ausgabe = new StringBuilder(raumBasis.ToString());

      if (blocker.ErstellungsModus)
      {
        ausgabe.AppendLine();

        ausgabe.AppendLine(blocker.ToString());

        return ausgabe.ToString();
      }
      else
      {
        ausgabe.AppendLine();
        ausgabe.Append("    [" + rückwärtsTiefe.ToString("#,##0") + "]" + rückwärtsSucher[rückwärtsTiefe].ToString().Replace("Datensätze: ", "").PadLeft(25 - rückwärtsTiefe.ToString().Length) + " --- Lösung: ");
        if (maxZüge < 30000)
        {
          ausgabe.Append(maxZüge.ToString("#,##0") + " --- (");
          if (maxZüge - rückwärtsTiefe - vorwärtsTiefe >= 0)
          {
            ausgabe.AppendLine(vorwärtsTiefe.ToString("#,##0") + " - " + (maxZüge - rückwärtsTiefe).ToString("#,##0") + " = " + (maxZüge - rückwärtsTiefe - vorwärtsTiefe).ToString("#,##0") + ") ---");
          }
          else
          {
            ausgabe.AppendLine("Perfekt - " + (bekannteStellungen.HashAnzahl / 1000.0).ToString("#,##0") + " k) ---");
          }
        }
        else
        {
          ausgabe.AppendLine("keine ---");
        }
        ausgabe.AppendLine();
        for (int i = Math.Max(vorwärtsTiefe, vorwärtsTiefeAktuell - 30); i < vorwärtsSucher.Length; i++)
        {
          if (i >= maxZüge) break;
          if (vorwärtsSucher[i] == null) continue;
          ausgabe.AppendLine((i == vorwärtsTiefeAktuell ? " -> " : "    ") + "[" + i.ToString("#,##0") + "]" + vorwärtsSucher[i].ToString().Replace("Datensätze: ", "").PadLeft(25 - i.ToString().Length) + (vorwärtsSucher[i].SatzAnzahl > 0 ? " - <" + (vorwärtsSucherPunkte[i].Min(x => x.Key) + i) + "> " + string.Join(" ", vorwärtsSucherPunkte[i].Where(x => x.Value > 0).OrderBy(x => x.Key).Take(10).Select(x => x.Key + "[" + x.Value + "]")) : ""));
          if (ausgabe.Length > 20000) break;
        }
      }


      return ausgabe.ToString();
    }
    #endregion
  }
}
