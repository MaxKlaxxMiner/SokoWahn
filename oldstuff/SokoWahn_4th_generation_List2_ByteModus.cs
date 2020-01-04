#define byteModus

// gibt an, ob der Parallel-Betrieb komplett deaktiviert werden soll (lansamer, übersichtlicher fürs Debuggen)
//#define parallelDeaktivieren

// gibt an, ob der Parallel-Betrieb geordnet ablaufen soll (= stabilere Zugverteilungen für Vergleiche, jedoch etwas langsamer)
//#define parallelGeordnet

#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sokosolver.SokowahnTools;
using System.IO;
using System.Threading;
#endregion

namespace Sokosolver
{
#if byteModus
  public class SokoWahn_4th_List2_ByteModus : SokoWahnInterface
#else
  public class SokoWahn_4th_List2 : SokoWahnInterface
#endif
  {
    #region # // --- statische Variablen ---
    /// <summary>
    /// Breite des Spielfeldes in Zeichen
    /// </summary>
    readonly int feldBreite;

    /// <summary>
    /// Höhe des Spielfeldes in Zeichen
    /// </summary>
    readonly int feldHöhe;

    /// <summary>
    /// Startposition des Spieler auf dem Spielfeld
    /// </summary>
    readonly int feldSpielerStartPos;

    /// <summary>
    /// Inhalt des Spielfeldes (Größe: feldBreite * feldHöhe)
    /// </summary>
    readonly char[] feldData;

    /// <summary>
    /// Inhalt des Spielfeldes, ohne Kisten und ohne Spieler (Größe: feldBreite * feldHöhe)
    /// </summary>
    readonly char[] feldDataLeer;

    /// <summary>
    /// merkt sich die Basis-Daten und Startaufstellung der Kisten
    /// </summary>
    readonly SokowahnRaum raumBasis;
    #endregion

    #region # // --- dynamische Variablen ---
    /// <summary>
    /// Hashtabelle aller bekannten Stellungen
    /// </summary>
    readonly ISokowahnHash bekannteStellungen;

#if byteModus
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die vorwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2Byte[] vorwärtsSucher;
#else
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die vorwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2[] vorwärtsSucher;
#endif

    /// <summary>
    /// bereits vorwärts berechnete Schritte in die Tiefe
    /// </summary>
    int vorwärtsTiefe;


    /// <summary>
    /// Hashtabelle aller bekannten Zielstellungen
    /// </summary>
    readonly ISokowahnHash zielStellungen;

#if byteModus
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die rückwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2Byte[] rückwärtsSucher;
#else
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die rückwärts gerichtete Suche
    /// </summary>
    SokowahnLinearList2[] rückwärtsSucher;
#endif

    /// <summary>
    /// bereits rückwärts berechnete Schritte in die Tiefe
    /// </summary>
    int rückwärtsTiefe;

    /// <summary>
    /// Buffer Multiplikator für die Suchlisten
    /// </summary>
    int list2Multi = 512;

    /// <summary>
    /// beste gefundene Tiefe 
    /// </summary>
    int gefundenTiefe;
    /// <summary>
    /// zugehörige Pushes
    /// </summary>
    int gefundenPushes;

    /// <summary>
    /// merkt sich die Knoten-Stellung der gefundenen Variante
    /// </summary>
    SokowahnStellung gefundenStellung;

    /// <summary>
    /// gibt an, ob die Endlösung gefunden wurde
    /// </summary>
    bool lösungGefunden;
    #endregion

    #region # // --- Konstruktor ---
#if byteModus
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="spielFeld">Spielfeld als Textzeilen</param>
    public SokoWahn_4th_List2_ByteModus(string spielFeld)
#else
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="spielFeld">Spielfeld als Textzeilen</param>
    public SokoWahn_4th_List2(string spielFeld)
#endif
    {
      SokowahnStaticTools.SpielfeldEinlesen(spielFeld, out feldBreite, out feldHöhe, out feldSpielerStartPos, out feldData, out feldDataLeer);

      raumBasis = new SokowahnRaum(feldData, feldBreite);

#if byteModus
      if (!raumBasis.ByteModusErlaubt) throw new Exception("Fehler zu viele Felder für den Byte-Modus (" + raumBasis.RaumAnzahl + " > 254)");
#endif

      Directory.CreateDirectory(TempOrdner);

      // --- Vorwärtssuche initialisieren ---
      bekannteStellungen = new SokowahnHash_Index24Multi();
      bekannteStellungen.Add(raumBasis.Crc, 0);
      vorwärtsTiefe = 0;
#if byteModus
      vorwärtsSucher = new SokowahnLinearList2Byte[0];
#else
      vorwärtsSucher = new SokowahnLinearList2[0];
#endif
      VorwärtsAdd(raumBasis.GetStellung());

      // --- Rückwärtssuche initialisieren ---
      zielStellungen = new SokowahnHash_Index24Multi();
      rückwärtsTiefe = 0;
#if byteModus
      rückwärtsSucher = new SokowahnLinearList2Byte[0];
#else
      rückwärtsSucher = new SokowahnLinearList2[0];
#endif

      foreach (SokowahnStellung stellung in SokowahnStaticTools.SucheZielStellungen(raumBasis))
      {
        int find = zielStellungen.Get(stellung.crc64);
        if (find == 65535) // noch unbekannt
        {
          zielStellungen.Add(stellung.crc64, stellung.zugTiefe);
          RückwärtsAdd(stellung);
        }
      }

      gefundenTiefe = 65535;
      gefundenPushes = 65535;
      gefundenStellung = new SokowahnStellung { crc64 = 123 };


      ulong spielFeldCrc = 0xcbf29ce484222325u;

      spielFeldCrc = (spielFeldCrc ^ (ulong)raumBasis.FeldBreite) * 0x100000001b3;
      spielFeldCrc = (spielFeldCrc ^ (ulong)raumBasis.FeldHöhe) * 0x100000001b3;

      foreach (char zeichen in raumBasis.FeldData)
      {
        spielFeldCrc = (spielFeldCrc ^ zeichen) * 0x100000001b3;
      }

      string blockerPath = Environment.CurrentDirectory + "\\blocker\\";
      Directory.CreateDirectory(blockerPath);
      blocker = new SokowahnBlockerBx(blockerPath + "blockerB_x" + spielFeldCrc.ToString("x").PadLeft(16, '0') + ".gz", raumBasis);
    }
    #endregion

    #region # string TempOrdner // gibt den temporären Ordner zurück
    /// <summary>
    /// gibt den temporären Ordner zurück
    /// </summary>
    static string TempOrdner
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
    void VorwärtsAdd(SokowahnStellung stellung)
    {
      int tiefePos = stellung.zugTiefe;

      while (tiefePos >= vorwärtsSucher.Length)
      {
        Array.Resize(ref vorwärtsSucher, vorwärtsSucher.Length + 1);
#if byteModus
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2Byte(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#else
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#endif
      }

      vorwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
    }

    /// <summary>
    /// trägt eine noch zu prüfende Stellung in die Vorwärtssuche ein
    /// </summary>
    /// <param name="stellung">Stellung, welche eingetragen werden soll</param>
    void VorwärtsAdd(SokowahnStellungRun stellung)
    {
      int tiefePos = stellung.zugTiefe;

      while (tiefePos >= vorwärtsSucher.Length)
      {
        Array.Resize(ref vorwärtsSucher, vorwärtsSucher.Length + 1);
#if byteModus
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2Byte(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#else
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#endif
      }

      vorwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
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
#if byteModus
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2Byte(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#else
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#endif
      }

      rückwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
    }

    /// <summary>
    /// trägt eine noch zu prüfende Stellung in die Rückwärtssuche ein
    /// </summary>
    /// <param name="stellung">Stellung, welche eingetragen werden soll</param>
    void RückwärtsAdd(SokowahnStellungRun stellung)
    {
      int tiefePos = 60000 - stellung.zugTiefe;

      while (tiefePos >= rückwärtsSucher.Length)
      {
        Array.Resize(ref rückwärtsSucher, rückwärtsSucher.Length + 1);
#if byteModus
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2Byte(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#else
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList2(raumBasis.KistenAnzahl + 1, TempOrdner, list2Multi);
#endif
      }

      rückwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
    }
    #endregion

    /// <summary>
    /// merkt sich alle Blocker-Stellungen (um bei der Vorwärtssuche sinnlose Stellungen auszulassen)
    /// </summary>
    readonly SokowahnBlockerBx blocker;

    /// <summary>
    /// merkt sich alle Thread-Räume (für Multi-Threading)
    /// </summary>
    SokowahnRaum[] threadRäume;

    #region # int CountPushes(SokowahnStellung stellung, int tiefe) // ermittelt die Anzahl der Pushes bei einer gefundenen Lösung
    /// <summary>
    /// ermittelt die Anzahl der Pushes bei einer gefundenen Lösung
    /// </summary>
    /// <param name="stellung">mittlere Stellung, welche gefunden wurde</param>
    /// <param name="tiefe">gesamte gefundene Spieltiefe in Moves</param>
    /// <returns>Anzahl der durchgeführten Pushes</returns>
    int CountPushes(SokowahnStellung stellung, int tiefe)
    {
      int pushes = 0;
      var tmpRaum = new SokowahnRaum(raumBasis);

      if (stellung.zugTiefe > 30000) stellung.zugTiefe = tiefe - (60000 - stellung.zugTiefe);
      int tmpTiefe = stellung.zugTiefe;
      tmpRaum.LadeStellung(stellung);

      while (tmpTiefe < tiefe)
      {
        var alleNachfolger = tmpRaum.GetVarianten().ToArray();
        var nachfolger = alleNachfolger.Where(x => zielStellungen.Get(x.crc64) == 60000 - (tiefe - x.zugTiefe)).OrderBy(x => x.zugTiefe).FirstOrDefault();

        pushes++;
        tmpRaum.LadeStellung(nachfolger);
        tmpTiefe = nachfolger.zugTiefe;
      }

      tmpTiefe = stellung.zugTiefe;
      tmpRaum.LadeStellung(stellung);

      if (tmpTiefe > 0)
      {
        for (; ; )
        {
          pushes++;
          var alleVorgänger = tmpRaum.GetVariantenRückwärts().ToArray();

          var vorgänger = alleVorgänger.Where(x => bekannteStellungen.Get(x.crc64) == x.zugTiefe).OrderBy(x => x.zugTiefe).FirstOrDefault();

          if (vorgänger.zugTiefe == 0) return pushes;

          tmpRaum.LadeStellung(vorgänger);
        }
      }

      return pushes;
    }
    #endregion

    #region # bool SucheVorwärts(int limit) // Normale Suche nach vorne (von der Startstellung aus beginnend)
    /// <summary>
    /// Normale Suche nach vorne (von der Startstellung aus beginnend)
    /// </summary>
    /// <param name="limit">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    bool SucheVorwärts(int limit)
    {
      if (vorwärtsTiefe == vorwärtsSucher.Length)
      {
        lösungGefunden = true;
        return false;
      }

      var liste = vorwärtsSucher[vorwärtsTiefe];

      limit = (int)Math.Min(limit, liste.SatzAnzahl);

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
  raum.LadeStellung(stellungen, stellung * satzGröße, vorwärtsTiefe);
  if (bekannteStellungen.Get(raum.Crc) < vorwärtsTiefe) return Enumerable.Empty<SokowahnStellungRun>();
  return raum.GetVariantenRun();
}).ToArray();

      foreach (var variante in ergebnisse)
      {
        int findQuelle = bekannteStellungen.Get(variante.crc64);

        if (findQuelle == 65535) // neue Stellung gefunden
        {
          if (gefundenTiefe == 65535 || variante.zugTiefe + rückwärtsTiefe + 1 < gefundenTiefe)
          {
            bekannteStellungen.Add(variante.crc64, variante.zugTiefe);
            VorwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
        }
        else
        {
          if (variante.zugTiefe < findQuelle) // wurde eine kürzere Variante zur einer bereits bekannten Stellung gefunden?
          {
            bekannteStellungen.Update(variante.crc64, variante.zugTiefe);
            if (variante.crc64 == gefundenStellung.crc64)
            {
              gefundenTiefe -= findQuelle - variante.zugTiefe;
              gefundenStellung.zugTiefe = variante.zugTiefe;
              gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
            }
            if (variante.zugTiefe + rückwärtsTiefe + 1 < gefundenTiefe) VorwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
          else continue;
        }

        if (variante.findHash == 65535) continue; // Ziel-Verbindung noch unbekannt -> normal vorwärts weiter suchen

        int findTiefe = 60000 - variante.findHash + variante.zugTiefe; // aktuelle Gesamttiefe der Lösung ermitteln

        if (findTiefe <= gefundenTiefe) // eventuell bessere Lösung gefunden?
        {
          if (findTiefe < gefundenTiefe) // definitiv bessere Lösung gefunden?
          {
            gefundenTiefe = findTiefe;
            gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
            gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
          }
          else
          {
            int newPushes = CountPushes(new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe }, findTiefe);
            if (newPushes < gefundenPushes) // gleiche Lösung mit weniger Pushes gefunden?
            {
              gefundenTiefe = findTiefe;
              gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
              gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
            }
          }
        }
      }

      if (liste.SatzAnzahl == 0)
      {
        liste.Dispose();
        vorwärtsTiefe++;
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
        lösungGefunden = true;
        return false;
      }

      var liste = rückwärtsSucher[rückwärtsTiefe];

      limit = (int)Math.Min(limit, liste.SatzAnzahl);

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
  return raum.GetVariantenRückwärtsRun2();
}).Where(x => { int find = zielStellungen.Get(x.crc64); return find == 65535 || find < x.zugTiefe; }).ToArray();

      foreach (var variante in ergebnisse)
      {
        int findZiel = zielStellungen.Get(variante.crc64);

        if (findZiel == 65535) // neue Stellung gefunden
        {
          if (gefundenTiefe == 65535 || 60000 - variante.zugTiefe + vorwärtsTiefe + 1 < gefundenTiefe)
          {
            zielStellungen.Add(variante.crc64, variante.zugTiefe);
            RückwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
        }
        else
        {
          if (variante.zugTiefe > findZiel) // wurde eine kürzere Variante zur einer bereits bekannten Stellung gefunden?
          {
            zielStellungen.Update(variante.crc64, variante.zugTiefe);
            if (variante.crc64 == gefundenStellung.crc64)
            {
              gefundenTiefe -= variante.zugTiefe - findZiel;
              gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
            }
            if (60000 - variante.zugTiefe + vorwärtsTiefe + 1 < gefundenTiefe) RückwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
          else continue;
        }

        if (variante.findHash == 65535) continue; // Verbindung zur Quelle noch unbekannt -> normal rückwärts weiter suchen

        int findTiefe = 60000 - variante.zugTiefe + variante.findHash; // aktuelle Gesamttiefe der Lösung ermitteln

        if (findTiefe <= gefundenTiefe) // eventuell bessere Lösung gefunden?
        {
          if (findTiefe < gefundenTiefe) // definitiv bessere Lösung gefunden?
          {
            gefundenTiefe = findTiefe;
            gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
            gefundenStellung.zugTiefe = variante.findHash;
            gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
          }
          else
          {
            int newPushes = CountPushes(new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe }, findTiefe);
            if (newPushes < gefundenPushes) // gleiche Lösung mit weniger Pushes gefunden?
            {
              gefundenTiefe = findTiefe;
              gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
              gefundenStellung.zugTiefe = variante.findHash;
              gefundenPushes = CountPushes(gefundenStellung, gefundenTiefe);
            }
          }
        }
      }

      if (liste.SatzAnzahl == 0)
      {
        liste.Dispose();
        rückwärtsTiefe++;
      }

      return true;
    }
    #endregion

    #region # // --- Public Methoden ---

    int aktuelleZugwahlTiefe = -1;
    bool aktuelleZugwahl;
    readonly List<long> hashNutzung = new List<long>();
    readonly List<long> hashVorwärtsNutzung = new List<long>();
    readonly List<long> hashRückwärtsNutzung = new List<long>();

    /// <summary>
    /// berechnet den nächsten Schritt
    /// </summary>
    /// <param name="limit">Anzahl der zu berechnenden (kleinen) Arbeitsschritte, negativer Wert = optionale Vorbereitungsschritte)</param>
    /// <returns>gibt an, ob noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      if (limit > 0)
      {
        if (blocker.ErstellungsModus) blocker.Abbruch();

        if (threadRäume == null)
        {
          threadRäume = Enumerable.Range(0, 256).Select(i => new SokowahnRaum(raumBasis, blocker, bekannteStellungen, zielStellungen)).ToArray();
        }

        if (gefundenTiefe < 65535)
        {
          int pos = gefundenStellung.zugTiefe;
          if (pos > 30000) pos = gefundenTiefe - (60000 - pos);

          if (pos > vorwärtsTiefe && pos - vorwärtsTiefe < gefundenTiefe - vorwärtsTiefe - rückwärtsTiefe)
          {
            if (vorwärtsSucher[vorwärtsTiefe].SatzAnzahl <= rückwärtsSucher[rückwärtsTiefe].SatzAnzahl)
            {
              return SucheVorwärts(limit);
            }
            else
            {
              return SucheRückwärts(limit);
            }
          }

          if (pos > vorwärtsTiefe) return SucheVorwärts(limit);
          if (vorwärtsTiefe + rückwärtsTiefe < gefundenTiefe) return SucheRückwärts(limit);
        }

        if (vorwärtsTiefe + rückwärtsTiefe >= gefundenTiefe || vorwärtsTiefe == vorwärtsSucher.Length || rückwärtsTiefe == rückwärtsSucher.Length)
        {
          lösungGefunden = true;
          return false;
        }

        if (SuchTiefe != aktuelleZugwahlTiefe)
        {
          aktuelleZugwahlTiefe = SuchTiefe;
          aktuelleZugwahl = bekannteStellungen.HashAnzahl < zielStellungen.HashAnzahl;

          if (vorwärtsTiefe > hashVorwärtsNutzung.Count) hashVorwärtsNutzung.Add(bekannteStellungen.HashAnzahl);
          if (rückwärtsTiefe > hashRückwärtsNutzung.Count) hashRückwärtsNutzung.Add(zielStellungen.HashAnzahl);
          hashNutzung.Add(bekannteStellungen.HashAnzahl + zielStellungen.HashAnzahl);

          if (vorwärtsTiefe > 10 && rückwärtsTiefe > 10)
          {
            long letzteVorwärts10 = hashVorwärtsNutzung[hashVorwärtsNutzung.Count - 1] - hashVorwärtsNutzung[hashVorwärtsNutzung.Count - 11];
            long letzteRückwärts10 = hashRückwärtsNutzung[hashRückwärtsNutzung.Count - 1] - hashRückwärtsNutzung[hashRückwärtsNutzung.Count - 11];
            aktuelleZugwahl = letzteVorwärts10 < letzteRückwärts10;
          }
        }

        //if (vorwärtsSucher[vorwärtsTiefe].SatzAnzahl <= rückwärtsSucher[rückwärtsTiefe].SatzAnzahl)
        //if (bekannteStellungen.HashAnzahl < zielStellungen.HashAnzahl)
        if (aktuelleZugwahl)
        {
          return SucheVorwärts(limit);
        }
        else
        {
          return SucheRückwärts(limit);
        }
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
      long sum = 0;
      double mess = Zp.TickCount;

      foreach (var satz in vorwärtsSucher)
      {
        if (satz == null) continue;
        sum += satz.Refresh();
        if (Zp.TickCount - mess > 3000.0) break;
      }

      if (sum == 0)
      {
        foreach (var satz in rückwärtsSucher)
        {
          if (satz == null) continue;
          sum += satz.Refresh();
          if (Zp.TickCount - mess > 3000.0) break;
        }
      }

      if (sum == 0)
      {
        if (list2Multi > 1)
        {
          list2Multi /= 2;
          return list2Multi;
        }
      }

      if (sum > 0)
      {
        GC.Collect();
      }

      return sum;
    }

    /// <summary>
    /// gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// </summary>
    /// <returns>Lösungsweg als einzelne Spielfelder</returns>
    public IEnumerable<string> GetLösungsweg()
    {
      if (lösungGefunden || gefundenTiefe < 65535) // Lösung gefunden?
      {
        if (lösungGefunden)
        {
          if (vorwärtsSucher != null) for (int i = 0; i < vorwärtsSucher.Length; i++) if (vorwärtsSucher[i] != null) vorwärtsSucher[i].Dispose();
          if (rückwärtsSucher != null) for (int i = 0; i < rückwärtsSucher.Length; i++) if (rückwärtsSucher[i] != null) rückwärtsSucher[i].Dispose();
        }

        var tmpRaum = new SokowahnRaum(raumBasis);

        if (gefundenStellung.zugTiefe > 30000) gefundenStellung.zugTiefe = gefundenTiefe - (60000 - gefundenStellung.zugTiefe);
        int tmpTiefe = gefundenStellung.zugTiefe;
        tmpRaum.LadeStellung(gefundenStellung);

        var merkListe = new List<SokowahnStellung>();

        while (tmpTiefe < gefundenTiefe)
        {
          var alleNachfolger = tmpRaum.GetVarianten().ToArray();
          var nachfolger = alleNachfolger.Where(x => zielStellungen.Get(x.crc64) == 60000 - (gefundenTiefe - x.zugTiefe)).OrderBy(x => x.zugTiefe).FirstOrDefault();

          merkListe.Add(nachfolger);
          tmpRaum.LadeStellung(nachfolger);
          tmpTiefe = nachfolger.zugTiefe;
        }

        foreach (var satz in merkListe.AsEnumerable().Reverse()) yield return satz.ToString(raumBasis);

        tmpTiefe = gefundenStellung.zugTiefe;
        tmpRaum.LadeStellung(gefundenStellung);

        while (tmpTiefe > 0)
        {
          yield return tmpRaum.ToString();
          var alleVorgänger = tmpRaum.GetVariantenRückwärts().ToArray();

          var vorgänger = alleVorgänger.Where(x => bekannteStellungen.Get(x.crc64) == x.zugTiefe).OrderBy(x => x.zugTiefe).FirstOrDefault();

          if (vorgänger.zugTiefe == 0)
          {
            yield return raumBasis.ToString();
            yield break;
          }

          tmpRaum.LadeStellung(vorgänger);
        }

        yield break;
      }

      int suchTiefe = vorwärtsTiefe + rückwärtsTiefe;
      for (int i = 0; i < suchTiefe; i++) yield return "dummy" + i;
      yield return "Ende";
    }

    /// <summary>
    /// gibt die aktuelle Suchtiefe zurück
    /// </summary>
    public int SuchTiefe
    {
      get
      {
        return vorwärtsTiefe + rückwärtsTiefe;
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
        return bekannteStellungen.HashAnzahl + zielStellungen.HashAnzahl;
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
        return vorwärtsSucher.Sum(x => x.SatzAnzahl) + rückwärtsSucher.Sum(x => x.SatzAnzahl);
      }
    }

    /// <summary>
    /// merkt sich den temporären Lösungsweg
    /// </summary>
    string tmpLösung = "";
    /// <summary>
    /// merkt sich die Tiefe der temporären Lösung
    /// </summary>
    int tmpLösungTiefe;
    /// <summary>
    /// merkt sich die Pushes der temporären Lösung
    /// </summary>
    int tmpLösungPushes;

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

      if (vorwärtsSucher != null && vorwärtsSucher.Length > 0 && rückwärtsSucher != null && rückwärtsSucher.Length > 0 && vorwärtsTiefe + rückwärtsTiefe > 0)
      {
        ausgabe.AppendLine();

        if (gefundenTiefe < 65535)
        {
          ausgabe.Clear();
          if (tmpLösungTiefe != gefundenTiefe || tmpLösungPushes != gefundenPushes)
          {
            tmpLösung = SokowahnStaticTools.LösungswegZuSteps(GetLösungsweg());
            tmpLösungTiefe = gefundenTiefe;
            tmpLösungPushes = gefundenPushes;
          }
          ausgabe.AppendLine(tmpLösung);

          int pos = gefundenStellung.zugTiefe;
          if (pos > 30000) pos = gefundenTiefe - (60000 - pos);
          ausgabe.AppendLine("Gefunden: " + gefundenTiefe.ToString("#,##0") + " (" + (pos - vorwärtsTiefe).ToString("#,##0") + " / " + (gefundenTiefe - vorwärtsTiefe - rückwärtsTiefe) + "), Pushes: " + gefundenPushes).AppendLine();
        }
        else
        {
          long sumVorwärts = vorwärtsSucher.Sum(x => x != null ? x.SatzAnzahl : 0L) / 2L;
          long sumRückwärts = rückwärtsSucher.Sum(x => x != null ? x.SatzAnzahl : 0L) / 2L;
          double tiefeVorwärts = 0.0;
          double tiefeRückwärts = 0.0;
          for (int i = 0; i < vorwärtsSucher.Length; i++)
          {
            if (vorwärtsSucher[i] == null) continue;
            if (vorwärtsSucher[i].SatzAnzahl > sumVorwärts)
            {
              tiefeVorwärts = i + (sumVorwärts / (double)vorwärtsSucher[i].SatzAnzahl);
              break;
            }
            sumVorwärts -= vorwärtsSucher[i].SatzAnzahl;
          }
          for (int i = 0; i < rückwärtsSucher.Length; i++)
          {
            if (rückwärtsSucher[i] == null) continue;
            if (rückwärtsSucher[i].SatzAnzahl > sumRückwärts)
            {
              tiefeRückwärts = i + (sumRückwärts / (double)rückwärtsSucher[i].SatzAnzahl);
              break;
            }
            sumRückwärts -= rückwärtsSucher[i].SatzAnzahl;
          }
          ausgabe.Append("Tiefe: " + (vorwärtsTiefe + rückwärtsTiefe).ToString("#,##0") + " - " + (vorwärtsSucher.Length + rückwärtsSucher.Length).ToString("#,##0") + " (" + (tiefeVorwärts + tiefeRückwärts).ToString("#,##0.00") + ")");
          if (hashNutzung.Count > 20 && hashNutzung.Last() > 1000000 && hashNutzung.Last() < 3000000000)
          {
            double anstiegLetzte = hashNutzung[hashNutzung.Count - 1] - hashNutzung[hashNutzung.Count - 11];
            double anstiegDavor = hashNutzung[hashNutzung.Count - 11] - hashNutzung[hashNutzung.Count - 21];
            double mulProTiefe = Math.Max(1, Math.Pow(anstiegLetzte / anstiegDavor, 1 / 10.0));
            int tiefe1 = hashNutzung.Count;
            int tiefe2 = hashNutzung.Count;
            int tiefe3 = hashNutzung.Count;
            double hashErwartung = hashNutzung[hashNutzung.Count - 1];
            double hashAnstieg = anstiegLetzte * 0.1;
            while (hashErwartung < 3000000000 && tiefe3 < 9999)
            {
              if (hashErwartung < 100000000) tiefe1++;
              if (hashErwartung < 1000000000) tiefe2++;
              tiefe3++;
              hashErwartung += hashAnstieg;
              hashAnstieg *= mulProTiefe;
            }
            ausgabe.Append(" - max: " + tiefe1.ToString("N0") + " / " + tiefe2.ToString("N0") + " / " + tiefe3.ToString("N0") + " (100M, 1G, 3G)");
          }
          ausgabe.AppendLine().AppendLine();
        }

        var vorwärtsZeilen = Enumerable.Range(0, vorwärtsSucher.Length).Where(i => vorwärtsSucher[i].SatzAnzahl > 0 || i >= vorwärtsTiefe).Select(i => "[" + i + "] " + vorwärtsSucher[i].ToString()).ToArray();
        var rückwärtsZeilen = Enumerable.Range(0, rückwärtsSucher.Length).Where(i => rückwärtsSucher[i].SatzAnzahl > 0 || i >= rückwärtsTiefe).Select(i => "[" + i + "] " + rückwärtsSucher[i].ToString()).ToArray();

        int bis = Math.Max(vorwärtsZeilen.Length, rückwärtsZeilen.Length);

        for (int i = 0; i < bis; i++)
        {
          if (i < vorwärtsZeilen.Length) ausgabe.Append(vorwärtsZeilen[i].Replace("Datensätze: ", "").PadRight(30)); else ausgabe.Append("".PadRight(30));
          if (i < rückwärtsZeilen.Length) ausgabe.Append(rückwärtsZeilen[i].Replace("Datensätze: ", "").PadRight(30)); else ausgabe.Append("".PadRight(30));
          ausgabe.AppendLine();
        }
      }

      return ausgabe.ToString();
    }
    #endregion
  }
}
