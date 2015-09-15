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
  public class SokoWahn_4th_ByteModus : SokoWahnInterface
#else
 public class SokoWahn_4th : SokoWahnInterface
#endif
  {
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

#if byteModus
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die vorwärts gerichtete Suche
    /// </summary>
    SokowahnLinearListByte[] vorwärtsSucher;
#else
  /// <summary>
  /// Listen mit den noch zu prüfenden Stellungen für die vorwärts gerichtete Suche
  /// </summary>
  SokowahnLinearList[] vorwärtsSucher;
#endif

    /// <summary>
    /// bereits vorwärts berechnete Schritte in die Tiefe
    /// </summary>
    int vorwärtsTiefe;


    /// <summary>
    /// Hashtabelle aller bekannten Zielstellungen
    /// </summary>
    ISokowahnHash zielStellungen;

#if byteModus
    /// <summary>
    /// Listen mit den noch zu prüfenden Stellungen für die rückwärts gerichtete Suche
    /// </summary>
    SokowahnLinearListByte[] rückwärtsSucher;
#else
  /// <summary>
  /// Listen mit den noch zu prüfenden Stellungen für die rückwärts gerichtete Suche
  /// </summary>
  SokowahnLinearList[] rückwärtsSucher;
#endif

    /// <summary>
    /// bereits rückwärts berechnete Schritte in die Tiefe
    /// </summary>
    int rückwärtsTiefe;

    /// <summary>
    /// beste gefundene Tiefe 
    /// </summary>
    int gefundenTiefe;

    /// <summary>
    /// merkt sich die Knoten-Stellung der gefundenen Variante
    /// </summary>
    SokowahnStellung gefundenStellung;

    /// <summary>
    /// gibt an, ob die Endlösung gefunden wurde
    /// </summary>
    bool lösungGefunden = false;
    #endregion

    #region # // --- Konstruktor ---
#if byteModus
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="spielFeld">Spielfeld als Textzeilen</param>
    public SokoWahn_4th_ByteModus(string spielFeld)
#else
  /// <summary>
  /// Konstruktor
  /// </summary>
  /// <param name="spielFeld">Spielfeld als Textzeilen</param>
  public SokoWahn_4th(string spielFeld)
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
      vorwärtsSucher = new SokowahnLinearListByte[0];
#else
   vorwärtsSucher = new SokowahnLinearList[0];
#endif
      VorwärtsAdd(raumBasis.GetStellung());

      // --- Rückwärtssuche initialisieren ---
      zielStellungen = new SokowahnHash_Index24Multi();
      rückwärtsTiefe = 0;
#if byteModus
      rückwärtsSucher = new SokowahnLinearListByte[0];
#else
   rückwärtsSucher = new SokowahnLinearList[0];
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
      gefundenStellung = new SokowahnStellung();
      gefundenStellung.crc64 = 123;


      ulong spielFeldCrc = 0xcbf29ce484222325u;

      spielFeldCrc = (spielFeldCrc ^ (ulong)raumBasis.FeldBreite) * 0x100000001b3;
      spielFeldCrc = (spielFeldCrc ^ (ulong)raumBasis.FeldHöhe) * 0x100000001b3;

      foreach (char zeichen in raumBasis.FeldData)
      {
        spielFeldCrc = (spielFeldCrc ^ (ulong)zeichen) * 0x100000001b3;
      }

      blocker = new SokowahnBlocker(Environment.CurrentDirectory + "\\temp\\blocker_x" + spielFeldCrc.ToString("x").PadLeft(16, '0') + ".gz", raumBasis);
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
    /// einzelne Buffergröße in Bytes pro aktiver ZugTiefe, größere Daten werden auf der Festplatte ausgelagert
    /// </summary>
    const int ListBufferGröße = 1048576 * 16;

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
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearListByte(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
#else
    vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
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
        vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearListByte(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
#else
    vorwärtsSucher[vorwärtsSucher.Length - 1] = new SokowahnLinearList(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
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
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearListByte(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
#else
    rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
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
        rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearListByte(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
#else
    rückwärtsSucher[rückwärtsSucher.Length - 1] = new SokowahnLinearList(raumBasis.KistenAnzahl + 1, ListBufferGröße, TempOrdner);
#endif
      }

      rückwärtsSucher[tiefePos].Add(stellung.raumSpielerPos, stellung.kistenZuRaum);
    }
    #endregion

    /// <summary>
    /// merkt sich alle Blocker-Stellungen (um bei der Vorwärtssuche sinnlose Stellungen auszulassen)
    /// </summary>
    SokowahnBlocker blocker;

    /// <summary>
    /// merkt sich alle Thread-Räume (für Multi-Threading)
    /// </summary>
    SokowahnRaum[] threadRäume = null;

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

      limit = (int)Math.Min((long)limit, liste.SatzAnzahl);

      var ergebnisse = Enumerable.Range(0, limit).Select(i => liste.Pop())
#if !parallelDeaktivieren
.AsParallel()
#if parallelGeordnet
.AsOrdered()
#endif
#endif
.SelectMany(stellung =>
   {
     SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
     raum.LadeStellung(stellung, vorwärtsTiefe);
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
            }
            if (variante.zugTiefe + rückwärtsTiefe + 1 < gefundenTiefe) VorwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
          else continue;
        }

        if (variante.findHash == 65535) continue; // Ziel-Verbindung noch unbekannt -> normal vorwärts weiter suchen

        int findTiefe = 60000 - variante.findHash + variante.zugTiefe; // aktuelle Gesamttiefe der Lösung ermitteln

        if (findTiefe < gefundenTiefe) // bessere Lösung gefunden?
        {
          gefundenTiefe = findTiefe;
          gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
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

      limit = (int)Math.Min((long)limit, liste.SatzAnzahl);

      var ergebnisse = Enumerable.Range(0, limit).Select(i => liste.Pop())
#if !parallelDeaktivieren
.AsParallel()
#if parallelGeordnet
.AsOrdered()
#endif
#endif
.SelectMany(stellung =>
   {
     SokowahnRaum raum = threadRäume[Thread.CurrentThread.ManagedThreadId];
     raum.LadeStellung(stellung, 60000 - rückwärtsTiefe);
     return raum.GetVariantenRückwärtsRun();
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
              //       gefundenStellung.zugTiefe = variante.zugTiefe;
            }
            if (60000 - variante.zugTiefe + vorwärtsTiefe + 1 < gefundenTiefe) RückwärtsAdd(variante); // zum weiteren Durchsuchen hinzufügen
          }
          else continue;
        }

        if (variante.findHash == 65535) continue; // Verbindung zur Quelle noch unbekannt -> normal rückwärts weiter suchen

        int findTiefe = 60000 - variante.zugTiefe + variante.findHash; // aktuelle Gesamttiefe der Lösung ermitteln

        if (findTiefe < gefundenTiefe) // bessere Lösung gefunden?
        {
          gefundenTiefe = findTiefe;
          gefundenStellung = new SokowahnStellung { raumSpielerPos = variante.raumSpielerPos, kistenZuRaum = variante.kistenZuRaum, crc64 = variante.crc64, zugTiefe = variante.zugTiefe };
          gefundenStellung.zugTiefe = variante.findHash;
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

        if (vorwärtsSucher[vorwärtsTiefe].SatzAnzahl <= rückwärtsSucher[rückwärtsTiefe].SatzAnzahl)
        //if (bekannteStellungen.HashAnzahl < zielStellungen.HashAnzahl)
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
        if (vorwärtsSucher != null) for (int i = 0; i < vorwärtsSucher.Length; i++) if (vorwärtsSucher[i] != null) vorwärtsSucher[i].Dispose();
        if (rückwärtsSucher != null) for (int i = 0; i < rückwärtsSucher.Length; i++) if (rückwärtsSucher[i] != null) rückwärtsSucher[i].Dispose();

        SokowahnRaum tmpRaum = new SokowahnRaum(raumBasis);

        if (gefundenStellung.zugTiefe > 30000) gefundenStellung.zugTiefe = gefundenTiefe - (60000 - gefundenStellung.zugTiefe);
        int tmpTiefe = gefundenStellung.zugTiefe;
        tmpRaum.LadeStellung(gefundenStellung);

        List<SokowahnStellung> merkListe = new List<SokowahnStellung>();

        while (tmpTiefe < gefundenTiefe)
        {
          var alleNachfolger = tmpRaum.GetVarianten().ToArray();
          var nachfolger = alleNachfolger.Where(x => zielStellungen.Get(x.crc64) == 60000 - (gefundenTiefe - x.zugTiefe)).FirstOrDefault();

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

          var vorgänger = alleVorgänger.Where(x => bekannteStellungen.Get(x.crc64) == x.zugTiefe).FirstOrDefault();

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
          int pos = gefundenStellung.zugTiefe;
          if (pos > 30000) pos = gefundenTiefe - (60000 - pos);
          ausgabe.AppendLine("Gefunden: " + gefundenTiefe.ToString("#,##0") + " (" + (pos - vorwärtsTiefe).ToString("#,##0") + " / " + (gefundenTiefe - vorwärtsTiefe - rückwärtsTiefe) + ")").AppendLine();
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
              tiefeVorwärts = (double)i + ((double)sumVorwärts / (double)vorwärtsSucher[i].SatzAnzahl);
              break;
            }
            sumVorwärts -= vorwärtsSucher[i].SatzAnzahl;
          }
          for (int i = 0; i < rückwärtsSucher.Length; i++)
          {
            if (rückwärtsSucher[i] == null) continue;
            if (rückwärtsSucher[i].SatzAnzahl > sumRückwärts)
            {
              tiefeRückwärts = (double)i + ((double)sumRückwärts / (double)rückwärtsSucher[i].SatzAnzahl);
              break;
            }
            sumRückwärts -= rückwärtsSucher[i].SatzAnzahl;
          }
          ausgabe.AppendLine("Tiefe: " + (vorwärtsTiefe + rückwärtsTiefe).ToString("#,##0") + " - " + (vorwärtsSucher.Length + rückwärtsSucher.Length).ToString("#,##0") + " (" + (tiefeVorwärts + tiefeRückwärts).ToString("#,##0.00") + ")").AppendLine();
        }

        string[] vorwärtsZeilen = Enumerable.Range(0, vorwärtsSucher.Length).Where(i => vorwärtsSucher[i].SatzAnzahl > 0 || i >= vorwärtsTiefe).Select(i => "[" + i + "] " + vorwärtsSucher[i].ToString()).ToArray();
        string[] rückwärtsZeilen = Enumerable.Range(0, rückwärtsSucher.Length).Where(i => rückwärtsSucher[i].SatzAnzahl > 0 || i >= rückwärtsTiefe).Select(i => "[" + i + "] " + rückwärtsSucher[i].ToString()).ToArray();

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
