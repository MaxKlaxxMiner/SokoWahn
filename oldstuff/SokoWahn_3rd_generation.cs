#region using *.*

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sokosolver.SokowahnTools;

#endregion

namespace Sokosolver
{
  // ReSharper disable once InconsistentNaming
  internal sealed class SokoWahn_3rd : SokoWahnInterface
  {
    #region # // --- allgemeine statische Variablen ---
    /// <summary>
    /// merkt sich das eigentliche komplette Spielfeld (mit Spieler und allen Steinen)
    /// </summary>
    readonly char[] feldData;

    /// <summary>
    /// merkt sich das Spielfeld, jedoch ohne Spieler und ohne Kisten (nur Struktur und Zielfelder vorhanden)
    /// </summary>
    readonly char[] feldLeer;

    /// <summary>
    /// merkt sich die Breite des Spielfeldes
    /// </summary>
    readonly int feldBreite;

    /// <summary>
    /// merkt sich die Höhe des Spielfeldes
    /// </summary>
    readonly int feldHöhe;

    /// <summary>
    /// Startposition des Spielers
    /// </summary>
    readonly int startSpielerPos;

    /// <summary>
    /// Anzahl der SokowahnRaum-Felder
    /// </summary>
    readonly int raumAnzahl;

    /// <summary>
    /// merkt sich nur die begehbaren Felder und zeigt auf die realen Spielfelder
    /// </summary>
    readonly int[] raumZuFeld;

    /// <summary>
    /// zeigt direkt auf die Index-Positionen, -1 = wenn davon kein Index-Eintrag vorhanden ist
    /// </summary>
    readonly int[] feldZuRaum;

    /// <summary>
    /// zeigt auf das linke benachbarte SokowahnRaum-Feld (-1 = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumLinks;

    /// <summary>
    /// zeigt auf das rechts benachbarte SokowahnRaum-Feld (-1 = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumRechts;

    /// <summary>
    /// zeigt auf das obere benachbarte SokowahnRaum-Feld (-1 = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumOben;

    /// <summary>
    /// zeigt auf das untere benachbarte SokowahnRaum-Feld (-1 = wenn das Feld nicht begehbar ist)
    /// </summary>
    readonly int[] raumUnten;

    /// <summary>
    /// gibt an, ob das Feld ein Zeilfeld für Kisten ist (1 = ja, 0 = nein)
    /// </summary>
    readonly int[] raumZiel;

    /// <summary>
    /// Anzahl der vorhandenen Kisten
    /// </summary>
    int kistenAnzahl;
    #endregion

    #region # // --- aktuelle Variablen für den Suchmodus ---
    /// <summary>
    /// Modus-Varianten, was genau momentan gemacht wird
    /// </summary>
    enum Modus
    {
      /// <summary>
      /// unbekannter Modus (nur am Anfang)
      /// </summary>
      Unbekannt,

      /// <summary>
      /// initialisiert und startet den End-Steiner (nur für eine bestimmte Steinanzahl)
      /// </summary>
      SteinerInit,
      /// <summary>
      /// ermittelt alle erreichbaren Stellungen für den End-Steiner
      /// </summary>
      SteinerVarianten,
      /// <summary>
      /// prüft alle Varianten und entfernt alle Stellungen, welche lösbar sind
      /// </summary>
      SteinerLösen,

      /// <summary>
      /// gibt an, dass der eigentliche Suchmodus initialisiert werden soll
      /// </summary>
      SucheInit,
      /// <summary>
      /// gibt an, ob der Suchmodus gerade aktiv arbeitet
      /// </summary>
      SucheRechne,
      /// <summary>
      /// gibt an, ob das Ziel bereits gefunden wurde und der Lösungsweg bereit steht
      /// </summary>
      SucheGefunden
    }

    /// <summary>
    /// merkt sich den aktuellen Modus, was momentan genau gemacht wird
    /// </summary>
    Modus modus = Modus.Unbekannt;

    /// <summary>
    /// gibt an, ob sich auf dem SokowahnRaum-Feld momentan eine Kiste befindet (-1 = keine Kiste, sonst Index auf die entsprechende Kiste)
    /// </summary>
    int[] raumZuKiste;

    /// <summary>
    /// merkt sich die Anzahl der Kisten, welche auf ein Zielfeld stehen
    /// </summary>
    int kistenMitZiel;

    /// <summary>
    /// aktuelle SokowahnRaum-Positionen aller Kisten
    /// </summary>
    int[] kistenZuRaum;

    /// <summary>
    /// gibt die momentane Spielerposition an
    /// </summary>
    int raumSpielerPos;

    /// <summary>
    /// merkt sich die aktuelle Zugtiefe, welche momentan abgearbeitet wird
    /// </summary>
    int suchTiefe;

    /// <summary>
    /// merkt sich alle bekannten Stellungen
    /// </summary>
    Dictionary<ulong, ushort> bekannteStellungen;

    /// <summary>
    /// gibt die Größe eines Suchlisten-Satzes an
    /// </summary>
    int suchListenSatz;

    /// <summary>
    /// Suchposition, um das nächste freie Feld zu finden
    /// </summary>
    int suchListenSuchFrei;

    /// <summary>
    /// Suchposition, um den nächsten Suchknoten zu finden
    /// </summary>
    int suchListenSuchNext;

    /// <summary>
    /// merkt sich die letzte Tiefe
    /// </summary>
    int suchListenTiefe;

    /// <summary>
    /// speichert sich die Datensätze der Suchlisten
    /// </summary>
    ushort[] suchListenDaten;

    /// <summary>
    /// merkt sich die Anzahl der gespeicherten Einträge pro Suchtiefe in der Suchliste
    /// </summary>
    int[] suchListenAnzahl;
    #endregion

    #region # public SokoWahn_3rd(string spielFeld) // Konstruktor
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="spielFeld">Spielfeld als Textzeilen</param>
    public SokoWahn_3rd(string spielFeld)
    {
      SokowahnStaticTools.SpielfeldEinlesen(spielFeld, out feldBreite, out feldHöhe, out startSpielerPos, out feldData, out feldLeer);

      var spielerRaum = SokowahnStaticTools.SpielfeldRaumScan(feldData, feldBreite);

      #region # // --- SokowahnRaum-Indexe und deren Felder erstellen ---
      raumAnzahl = spielerRaum.Where(x => x).Count(); // Anzahl der SokowahnRaum-Felder zählen (Felder, welche theoretisch begehbar sind)

      raumZuFeld = spielerRaum.Select((c, i) => new { c, i }).Where(x => x.c).Select(x => x.i).ToArray();

      raumLinks = raumZuFeld.Select(i => spielerRaum[i - 1] ? raumZuFeld.ToList().IndexOf(i - 1) : -1).ToArray();
      raumRechts = raumZuFeld.Select(i => spielerRaum[i + 1] ? raumZuFeld.ToList().IndexOf(i + 1) : -1).ToArray();
      raumOben = raumZuFeld.Select(i => spielerRaum[i - feldBreite] ? raumZuFeld.ToList().IndexOf(i - feldBreite) : -1).ToArray();
      raumUnten = raumZuFeld.Select(i => spielerRaum[i + feldBreite] ? raumZuFeld.ToList().IndexOf(i + feldBreite) : -1).ToArray();

      feldZuRaum = Enumerable.Range(0, feldBreite * feldHöhe).Select(i => spielerRaum[i] ? raumZuFeld.ToList().IndexOf(i) : -1).ToArray();

      raumZiel = raumZuFeld.Select(i => feldLeer[i] == '.' ? 1 : 0).ToArray();

      raumBlocker = new Blocker[0];
      #endregion
    }
    #endregion

    #region # ushort[] GetStellung() // gibt die aktuelle Stellung als kompakten String zurück (Länge: Anzahl der Kisten + 1 Spielerposition)
    /// <summary>
    /// gibt die aktuelle Stellung als kompakten String zurück (Länge: Anzahl der Kisten + 1 Spielerposition)
    /// </summary>
    /// <returns>nicht lesbare Zeichenkette mit Länge = kistenAnzahl + 1</returns>
    ushort[] GetStellung()
    {
      var ausgabe = new ushort[kistenAnzahl + 1];
      for (int i = 0; i < kistenAnzahl; i++) ausgabe[i] = (ushort)kistenZuRaum[i];
      ausgabe[kistenAnzahl] = (ushort)raumSpielerPos;
      return ausgabe;
    }
    #endregion

    #region # void LadeStellung(string stellung) // lädt die aktuelle Stellung
    /// <summary>
    /// lädt die aktuelle Stellung
    /// </summary>
    /// <param name="stellung">Stellung, welche geladen werden soll</param>
    void LadeStellung(ushort[] stellung)
    {
      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKiste[kistenZuRaum[i]] = -1;

      // neue Kisten setzen (und Ziele zählen)
      kistenMitZiel = 0;
      for (int i = 0; i < kistenAnzahl; i++)
      {
        int p = stellung[i];
        kistenZuRaum[i] = p;
        kistenMitZiel += raumZiel[p];
        raumZuKiste[p] = i;
      }

      // neue Spielerposition setzen
      raumSpielerPos = stellung[kistenAnzahl];
    }
    #endregion

    #region # // --- Varianten-Sucher ---
    #region # struct Variante // Struktur einer Stellungs-Variante
    /// <summary>
    /// Struktur einer Stellungs-Variante
    /// </summary>
    struct Variante
    {
      /// <summary>
      /// gibt die hashfähige Stellung des Spielfeldes an. siehe: LadeStellung() / GetStellung()
      /// </summary>
      public ushort[] stellung;

      /// <summary>
      /// bereits berechneter Crc-Schlüssel der Stellung
      /// </summary>
      public ulong stellungCrc;

      /// <summary>
      /// gibt die gefundene Tiefe (Anzahl der gelaufenen Schritte) an, womit diese Stellung erreicht wurde
      /// </summary>
      public int tiefe;

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return "[" + tiefe + "] " + stellungCrc;
      }
    }
    #endregion

    #region # IEnumerable<Variante> SucheVariantenHashcheck() // gibt alle möglichen Zugvarianten (nur die Kisten-Verschiebungen) zurück, welche auf dem aktuellen Spielfeld möglich sind (bekannte Stellungen in der Hashtable werden nicht mehr geliefert)
    /// <summary>
    /// gibt alle möglichen Zugvarianten (nur die Kisten-Verschiebungen) zurück, welche auf dem aktuellen Spielfeld möglich sind (bekannte Stellungen in der Hashtable werden nicht mehr geliefert)
    /// </summary>
    /// <returns>Enumerable mit allen möglichen Varianten</returns>
    IEnumerable<Variante> SucheVariantenHashcheck()
    {
      // temporäre Werte speichern
      int checkRaumVon = 0;
      int checkRaumBis = 0;
      var checkRaumPosis = new int[raumAnzahl];
      var checkRaumTiefe = new int[raumAnzahl];
      var raumGecheckt = new bool[raumAnzahl];

      // erste Spielerposition hinzufügen
      raumGecheckt[raumSpielerPos] = true;
      checkRaumPosis[checkRaumBis] = raumSpielerPos;
      checkRaumTiefe[checkRaumBis] = suchTiefe;
      checkRaumBis++;

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = checkRaumPosis[checkRaumVon];
        int pTiefe = checkRaumTiefe[checkRaumVon] + 1;

        int p, p2;

        #region # // --- links ---
        if ((p = raumLinks[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumLinks[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                       // Spieler nach links bewegen

              #region # // Stellung auslesen und wenn gültig zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              int bekannt = StellungBekannt(stellungCrc);
              if ((bekannt < 0 || bekannt > pTiefe) && !IstBlocker()) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach rechts bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if ((p = raumRechts[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumRechts[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                       // Spieler nach rechts bewegen

              #region # // Stellung auslesen und wenn gültig zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              int bekannt = StellungBekannt(stellungCrc);
              if ((bekannt < 0 || bekannt > pTiefe) && !IstBlocker()) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach links bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if ((p = raumOben[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumOben[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                       // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKiste[p2] > 0 && kistenZuRaum[raumZuKiste[p2] - 1] > p2)
              {
                int tmp = kistenZuRaum[raumZuKiste[p2] - 1];
                kistenZuRaum[raumZuKiste[p2]--] = tmp;
                kistenZuRaum[raumZuKiste[tmp]++] = p2;
              }
              #endregion

              #region # // Stellung auslesen und wenn gültig zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              int bekannt = StellungBekannt(stellungCrc);
              if ((bekannt < 0 || bekannt > pTiefe) && !IstBlocker()) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach unten bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKiste[p] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[p] + 1] < p)
              {
                int tmp = kistenZuRaum[raumZuKiste[p] + 1];
                kistenZuRaum[raumZuKiste[p]++] = tmp;
                kistenZuRaum[raumZuKiste[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if ((p = raumUnten[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumUnten[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                       // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKiste[p2] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKiste[p2] + 1];
                kistenZuRaum[raumZuKiste[p2]++] = tmp;
                kistenZuRaum[raumZuKiste[tmp]--] = p2;
              }
              #endregion

              #region # // Stellung auslesen und wenn gültig zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              int bekannt = StellungBekannt(stellungCrc);
              if ((bekannt < 0 || bekannt > pTiefe) && !IstBlocker()) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach oben bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKiste[p] > 0 && kistenZuRaum[raumZuKiste[p] - 1] > p)
              {
                int tmp = kistenZuRaum[raumZuKiste[p] - 1];
                kistenZuRaum[raumZuKiste[p]--] = tmp;
                kistenZuRaum[raumZuKiste[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }

      raumSpielerPos = checkRaumPosis[0]; // alte Spielerposition wieder herstellen
    }
    #endregion

    #region # IEnumerable<Variante> SucheVariantenSteiner() // gibt alle möglichen Zugvarianten (nur die Kisten-Verschiebungen) zurück, welche auf dem aktuellen Spielfeld möglich sind (bekannte Stellungen in der Hashtable werden nicht mehr geliefert, Tiefe wird ignoriert)
    /// <summary>
    /// gibt alle möglichen Zugvarianten (nur die Kisten-Verschiebungen) zurück, welche auf dem aktuellen Spielfeld möglich sind (bekannte Stellungen in der Hashtable werden nicht mehr geliefert, Tiefe wird ignoriert)
    /// </summary>
    /// <returns>Enumerable mit allen möglichen Varianten</returns>
    IEnumerable<Variante> SucheVariantenSteiner()
    {
      // temporäre Werte speichern
      int checkRaumVon = 0;
      int checkRaumBis = 0;
      var checkRaumPosis = new int[raumAnzahl];
      var raumGecheckt = new bool[raumAnzahl];

      // erste Spielerposition hinzufügen
      raumGecheckt[raumSpielerPos] = true;
      checkRaumPosis[checkRaumBis] = raumSpielerPos;
      checkRaumBis++;

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = checkRaumPosis[checkRaumVon];

        int p, p2;

        #region # // --- links ---
        if ((p = raumLinks[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumLinks[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // linke Kiste weiter nach links schieben
              raumSpielerPos = p;                                                       // Spieler nach links bewegen

              #region # // Stellung auslesen und wenn gültig zurück senden
              if (!IstBlocker())
              {
                var stellung = GetStellung();
                ulong stellungCrc = StellungCrc(stellung);
                if (!bekannteStellungen.ContainsKey(stellungCrc)) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = 1 };
              }
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach rechts bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // linke Kiste eins zurück nach rechts schieben
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if ((p = raumRechts[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumRechts[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // rechte Kiste weiter nach rechts schieben
              raumSpielerPos = p;                                                       // Spieler nach rechts bewegen

              #region # // Stellung auslesen und wenn gültig zurück senden
              if (!IstBlocker())
              {
                var stellung = GetStellung();
                ulong stellungCrc = StellungCrc(stellung);
                if (!bekannteStellungen.ContainsKey(stellungCrc)) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = 1 };
              }
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach links bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // rechte Kiste eins zurück nach links schieben
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if ((p = raumOben[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumOben[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // obere Kiste weiter nach oben schieben
              raumSpielerPos = p;                                                       // Spieler nach oben bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKiste[p2] > 0 && kistenZuRaum[raumZuKiste[p2] - 1] > p2)
              {
                int tmp = kistenZuRaum[raumZuKiste[p2] - 1];
                kistenZuRaum[raumZuKiste[p2]--] = tmp;
                kistenZuRaum[raumZuKiste[tmp]++] = p2;
              }
              #endregion

              #region # // Stellung auslesen und wenn gültig zurück senden
              if (!IstBlocker())
              {
                var stellung = GetStellung();
                ulong stellungCrc = StellungCrc(stellung);
                if (!bekannteStellungen.ContainsKey(stellungCrc)) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = 1 };
              }
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach unten bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // obere Kiste eins zurück nach unten schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKiste[p] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[p] + 1] < p)
              {
                int tmp = kistenZuRaum[raumZuKiste[p] + 1];
                kistenZuRaum[raumZuKiste[p]++] = tmp;
                kistenZuRaum[raumZuKiste[tmp]--] = p;
              }
              #endregion
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if ((p = raumUnten[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumUnten[p]) >= 0 && raumZuKiste[p2] < 0)
            {
              kistenZuRaum[raumZuKiste[p2] = raumZuKiste[p]] = p2; raumZuKiste[p] = -1; // untere Kiste weiter nach unten schieben
              raumSpielerPos = p;                                                       // Spieler nach unten bewegen

              #region # // Kisten sortieren (sofern notwendig)
              while (raumZuKiste[p2] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[p2] + 1] < p2)
              {
                int tmp = kistenZuRaum[raumZuKiste[p2] + 1];
                kistenZuRaum[raumZuKiste[p2]++] = tmp;
                kistenZuRaum[raumZuKiste[tmp]--] = p2;
              }
              #endregion

              #region # // Stellung auslesen und wenn gültig zurück senden
              if (!IstBlocker())
              {
                var stellung = GetStellung();
                ulong stellungCrc = StellungCrc(stellung);
                if (!bekannteStellungen.ContainsKey(stellungCrc)) yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = 1 };
              }
              #endregion

              raumSpielerPos = checkRaumPosis[checkRaumVon];                            // Spieler zurück nach oben bewegen
              kistenZuRaum[raumZuKiste[p] = raumZuKiste[p2]] = p; raumZuKiste[p2] = -1; // untere Kiste eins zurück nach oben schieben

              #region # // Kisten zurück sortieren (sofern notwendig)
              while (raumZuKiste[p] > 0 && kistenZuRaum[raumZuKiste[p] - 1] > p)
              {
                int tmp = kistenZuRaum[raumZuKiste[p] - 1];
                kistenZuRaum[raumZuKiste[p]--] = tmp;
                kistenZuRaum[raumZuKiste[tmp]++] = p;
              }
              #endregion
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }

      raumSpielerPos = checkRaumPosis[0]; // alte Spielerposition wieder herstellen
    }
    #endregion

    #region # IEnumerable<Variante> SucheVariantenVorgängerTeil() // sucht die direkten Vorgänger, Teilberechnung von SucheVariantenVorgänger()
    /// <summary>
    /// sucht die direkten Vorgänger, Teilberechnung von SucheVariantenVorgänger()
    /// </summary>
    /// <returns>Enumerable mit allen möglichen Varianten</returns>
    IEnumerable<Variante> SucheVariantenVorgängerTeil()
    {
      // temporäre Werte speichern
      int checkRaumVon = 0;
      int checkRaumBis = 0;
      var checkRaumPosis = new int[raumAnzahl];
      var checkRaumTiefe = new int[raumAnzahl];
      var raumGecheckt = new bool[raumAnzahl];

      // erste Spielerposition hinzufügen
      raumGecheckt[raumSpielerPos] = true;
      checkRaumPosis[checkRaumBis] = raumSpielerPos;
      checkRaumTiefe[checkRaumBis] = suchTiefe;
      checkRaumBis++;

      // alle möglichen Spielerposition berechnen
      while (checkRaumVon < checkRaumBis)
      {
        raumSpielerPos = checkRaumPosis[checkRaumVon];
        int pTiefe = checkRaumTiefe[checkRaumVon] - 1;
        if (pTiefe < 1) yield break; // weitere Rückwärtssuche unnötig

        int p, p2;

        #region # // --- links ---
        if ((p = raumLinks[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumRechts[raumSpielerPos]) >= 0 && raumZuKiste[p2] < 0)
            {
              // erreichbare Stellung auslesen und zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- rechts ---
        if ((p = raumRechts[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumLinks[raumSpielerPos]) >= 0 && raumZuKiste[p2] < 0)
            {
              // erreichbare Stellung auslesen und zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- oben ---
        if ((p = raumOben[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumUnten[raumSpielerPos]) >= 0 && raumZuKiste[p2] < 0)
            {
              // erreichbare Stellung auslesen und zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        #region # // --- unten ---
        if ((p = raumUnten[raumSpielerPos]) >= 0 && !raumGecheckt[p])
        {
          if (raumZuKiste[p] >= 0)
          {
            if ((p2 = raumOben[raumSpielerPos]) >= 0 && raumZuKiste[p2] < 0)
            {
              // erreichbare Stellung auslesen und zurück senden
              var stellung = GetStellung();
              ulong stellungCrc = StellungCrc(stellung);
              yield return new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = pTiefe };
            }
          }
          else
          {
            raumGecheckt[p] = true;
            checkRaumPosis[checkRaumBis] = p;
            checkRaumTiefe[checkRaumBis] = pTiefe;
            checkRaumBis++;
          }
        }
        #endregion

        checkRaumVon++;
      }
    }
    #endregion

    #region # IEnumerable<Variante> SucheVariantenVorgänger() // gibt alle möglichen Vorgänger-Varianten zurück, welche diese aktuelle Stellung erzeugt haben könnten
    /// <summary>
    /// gibt alle möglichen Vorgänger-Varianten zurück, welche diese aktuelle Stellung erzeugt haben könnten
    /// </summary>
    /// <returns>Enumerable mit allen möglichen Varianten</returns>
    IEnumerable<Variante> SucheVariantenVorgänger()
    {
      int p = raumSpielerPos;
      int pl = raumLinks[p];
      int pr = raumRechts[p];
      int po = raumOben[p];
      int pu = raumUnten[p];

      #region # // --- Links-Vermutung: Kiste wurde das letztes mal nach links geschoben ---
      if (pl >= 0 && raumZuKiste[pl] >= 0 && pr >= 0 && raumZuKiste[pr] < 0)
      {
        raumSpielerPos = pr;                                                      // Spieler zurück nach rechts bewegen
        kistenZuRaum[raumZuKiste[p] = raumZuKiste[pl]] = p; raumZuKiste[pl] = -1; // linke Kiste eins zurück nach rechts schieben

        foreach (var variante in SucheVariantenVorgängerTeil()) yield return variante;

        kistenZuRaum[raumZuKiste[pl] = raumZuKiste[p]] = pl; raumZuKiste[p] = -1; // linke Kiste weiter nach links schieben
        raumSpielerPos = p;                                                       // Spieler nach links bewegen
      }
      #endregion

      #region # // --- Rechts-Vermutung: Kiste wurde das letztes mal nach rechts geschoben ---
      if (pr >= 0 && raumZuKiste[pr] >= 0 && pl >= 0 && raumZuKiste[pl] < 0)
      {
        raumSpielerPos = pl;                                                      // Spieler zurück nach links bewegen
        kistenZuRaum[raumZuKiste[p] = raumZuKiste[pr]] = p; raumZuKiste[pr] = -1; // rechte Kiste eins zurück nach links schieben

        foreach (var variante in SucheVariantenVorgängerTeil()) yield return variante;

        kistenZuRaum[raumZuKiste[pr] = raumZuKiste[p]] = pr; raumZuKiste[p] = -1; // rechte Kiste weiter nach rechts schieben
        raumSpielerPos = p;                                                       // Spieler nach rechts bewegen
      }
      #endregion

      #region # // --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
      // --- Oben-Vermutung: Kiste wurde das letztes mal nach oben geschoben ---
      if (po >= 0 && raumZuKiste[po] >= 0 && pu >= 0 && raumZuKiste[pu] < 0)
      {
        raumSpielerPos = pu;                                                      // Spieler zurück nach unten bewegen
        kistenZuRaum[raumZuKiste[p] = raumZuKiste[po]] = p; raumZuKiste[po] = -1; // obere Kiste eins zurück nach unten schieben

        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKiste[p] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[p] + 1] < p)
        {
          int tmp = kistenZuRaum[raumZuKiste[p] + 1];
          kistenZuRaum[raumZuKiste[p]++] = tmp;
          kistenZuRaum[raumZuKiste[tmp]--] = p;
        }
        #endregion

        foreach (var variante in SucheVariantenVorgängerTeil()) yield return variante;

        kistenZuRaum[raumZuKiste[po] = raumZuKiste[p]] = po; raumZuKiste[p] = -1; // obere Kiste weiter nach oben schieben
        raumSpielerPos = p;                                                       // Spieler nach oben bewegen

        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKiste[po] > 0 && kistenZuRaum[raumZuKiste[po] - 1] > po)
        {
          int tmp = kistenZuRaum[raumZuKiste[po] - 1];
          kistenZuRaum[raumZuKiste[po]--] = tmp;
          kistenZuRaum[raumZuKiste[tmp]++] = po;
        }
        #endregion
      }
      #endregion

      #region # // --- Unten-Vermutung: Kiste wurde das letztes mal nach unten geschoben ---
      if (pu >= 0 && raumZuKiste[pu] >= 0 && po >= 0 && raumZuKiste[po] < 0)
      {
        raumSpielerPos = po;                                                      // Spieler zurück nach oben bewegen
        kistenZuRaum[raumZuKiste[p] = raumZuKiste[pu]] = p; raumZuKiste[pu] = -1; // untere Kiste eins zurück nach oben schieben

        #region # // Kisten zurück sortieren (sofern notwendig)
        while (raumZuKiste[p] > 0 && kistenZuRaum[raumZuKiste[p] - 1] > p)
        {
          int tmp = kistenZuRaum[raumZuKiste[p] - 1];
          kistenZuRaum[raumZuKiste[p]--] = tmp;
          kistenZuRaum[raumZuKiste[tmp]++] = p;
        }
        #endregion

        foreach (var variante in SucheVariantenVorgängerTeil()) yield return variante;

        kistenZuRaum[raumZuKiste[pu] = raumZuKiste[p]] = pu; raumZuKiste[p] = -1; // untere Kiste weiter nach unten schieben
        raumSpielerPos = p;                                                       // Spieler nach unten bewegen

        #region # // Kisten sortieren (sofern notwendig)
        while (raumZuKiste[pu] < kistenAnzahl - 1 && kistenZuRaum[raumZuKiste[pu] + 1] < pu)
        {
          int tmp = kistenZuRaum[raumZuKiste[pu] + 1];
          kistenZuRaum[raumZuKiste[pu]++] = tmp;
          kistenZuRaum[raumZuKiste[tmp]--] = pu;
        }
        #endregion
      }
      #endregion
    }
    #endregion
    #endregion

    #region # void SuchListeInsert(ushort[] stellung, int tiefe) // fügt einen neuen Eintrag in die Suchliste hinzu
    /// <summary>
    /// fügt einen neuen Eintrag in die Suchliste hinzu
    /// </summary>
    /// <param name="stellung">Stellung</param>
    /// <param name="tiefe">Suchtiefe</param>
    void SuchListeInsert(ushort[] stellung, int tiefe)
    {
      while (suchListenSuchFrei < suchListenDaten.Length && suchListenDaten[suchListenSuchFrei] < 32000) suchListenSuchFrei += suchListenSatz;
      if (suchListenSuchFrei == suchListenDaten.Length) // kein freien Platz gefunden
      {
        suchListenSuchFrei = 0;
        while (suchListenSuchFrei < suchListenDaten.Length && suchListenDaten[suchListenSuchFrei] < 32000) suchListenSuchFrei += suchListenSatz;
        if (suchListenSuchFrei == suchListenDaten.Length)
        {
          if (suchListenDaten.Length * 2 > 1070000000)
          {
            if (suchListenDaten.Length == 1070000000 / suchListenSatz * suchListenSatz) throw new Exception("MEM-LIMIT");
            Array.Resize(ref suchListenDaten, 1070000000 / suchListenSatz * suchListenSatz);
          }
          else
          {
            Array.Resize(ref suchListenDaten, suchListenDaten.Length * 2);
          }
          for (int i = suchListenSuchFrei; i < suchListenDaten.Length; i += suchListenSatz) suchListenDaten[i] = 32000; // alle neuen Felder als frei kennzeichnen
        }
      }
      suchListenAnzahl[tiefe]++;
      suchListenDaten[suchListenSuchFrei++] = (ushort)tiefe;
      stellung.Select(x => suchListenDaten[suchListenSuchFrei++] = x).Count();
    }
    #endregion

    #region # string SuchListeNext(int tiefe) // gibt den nächsten Datensatz einer bestimmten Suchtiefe zurück und löscht diesen gleichzeitig
    /// <summary>
    /// gibt den nächsten Datensatz einer bestimmten Suchtiefe zurück und löscht diesen gleichzeitig
    /// </summary>
    /// <param name="tiefe">Tiefe, welche abgefragt werden soll</param>
    /// <returns>gefundene Stellung</returns>
    ushort[] SuchListeNext(int tiefe)
    {
      if (suchListenTiefe != tiefe)
      {
        suchListenSuchNext = 0;
        suchListenTiefe = tiefe;
      }
      while (suchListenSuchNext < suchListenDaten.Length && suchListenDaten[suchListenSuchNext] != (ushort)tiefe) suchListenSuchNext += suchListenSatz;
      if (suchListenSuchNext == suchListenDaten.Length)
      {
        suchListenSuchNext = 0;
        while (suchListenSuchNext < suchListenDaten.Length && suchListenDaten[suchListenSuchNext] != (ushort)tiefe) suchListenSuchNext += suchListenSatz;
      }
      if (suchListenSuchNext == suchListenDaten.Length) throw new Exception("böse");
      suchListenAnzahl[tiefe]--;
      suchListenDaten[suchListenSuchNext] = 32000;
      suchListenSuchNext += suchListenSatz;
      return Enumerable.Range(suchListenSuchNext - suchListenSatz + 1, suchListenSatz - 1).Select(i => suchListenDaten[i]).ToArray();
      //   return new string(suchListenDaten, suchListenSuchNext - suchListenSatz + 1, suchListenSatz - 1);
    }
    #endregion

    #region # // --- Hashsystem ---
    #region # void ArchivEintrag(List<StellungsSatz> neuesSortiert) // Speichert eine breits sortierte Liste ins Archiv, um insgesamt Platz zu sparen
    /// <summary>
    /// Größe des momentanen Archives in Datensätzen
    /// </summary>
    int archivGro;
    /// <summary>
    /// enthält alle ArchivDaten
    /// </summary>
    StellungsSatz[] archivData;
    /// <summary>
    /// Speichert eine breits sortierte Liste ins Archiv, um insgesamt Platz zu sparen
    /// </summary>
    /// <param name="neuesSortiert">Liste mit den bereits sortierten Stellungen</param>
    void ArchivEintrag(List<StellungsSatz> neuesSortiert)
    {
      if (neuesSortiert.Count == 0) return;

      if (archivData == null) // neuaufbau?
      {
        archivData = neuesSortiert.ToArray(); // einfach direkt kopieren
        archivGro = archivData.Length;
        return;
      }

      // --- beide Listen verschmelzen ---

      int neuPos = neuesSortiert.Count;
      int altPos = archivGro;
      int zielPos = neuPos + altPos;
      archivGro = zielPos;
      neuPos--; altPos--; zielPos--;
      Array.Resize(ref archivData, archivGro);

      while (zielPos >= 0)
      {
        if (altPos < 0)
        {
          while (zielPos >= 0) archivData[zielPos--] = neuesSortiert[neuPos--];
          return;
        }
        if (neuPos < 0)
        {
          while (zielPos >= 0) archivData[zielPos--] = archivData[altPos--];
          return;
        }
        if (neuesSortiert[neuPos].stellung > archivData[altPos].stellung)
        {
          archivData[zielPos--] = neuesSortiert[neuPos--];
        }
        else
        {
          archivData[zielPos--] = archivData[altPos--];
        }
      }

    }
    #endregion

    #region # bool StellungBekannt(ulong stellungCrc) // prüft, ob eine bestimmte Stellung bereits bekannt ist
    /// <summary>
    /// prüft, ob eine bestimmte Stellung bereits bekannt ist
    /// </summary>
    /// <param name="stellungCrc">CRC-Schlüssel der Stellung, welche geprüft werden soll</param>
    /// <returns>größer gleich 0, wenn die Stellung bereits bekannt war oder -1 wenn unbekannt</returns>
    int StellungBekannt(ulong stellungCrc)
    {
      ushort tmp = 0;

      if (bekannteStellungen.TryGetValue(stellungCrc, out tmp))
      {
        return tmp;
      }

      if (archivGro == 0) return -1;

      int von = 0;
      int bis = archivGro;

      do
      {
        var mit = (von + bis) >> 1;
        if (archivData[mit].stellung > stellungCrc) bis = mit; else von = mit;
      } while (bis - von > 1);

      if (archivData[von].stellung == stellungCrc) return archivData[von].tiefe;

      return -1;
    }
    #endregion

    #region # ulong StellungCrc(string stellung) // berechnet den CRC-Schlüssel einer kompletten Stellung
    /// <summary>
    /// berechnet den CRC-Schlüssel einer kompletten Stellung
    /// </summary>
    /// <param name="stellung">Stellung, wovon der CRC Schlüssel berechnet werden soll</param>
    /// <returns>fertig berechneter CRC Schlüssel</returns>
    static ulong StellungCrc(ushort[] stellung)
    {
      ulong ergebnis = 0xcbf29ce484222325u; //init prime
      int hashSatzGro = stellung.Length;
      for (int i = 0; i < hashSatzGro; i++)
      {
        ergebnis = (ergebnis ^ stellung[i]) * 0x100000001b3; //xor with new and mul with prime
      }
      return ergebnis;
    }
    #endregion
    #endregion

    #region # // --- Kisten-Helfer Methoden ---
    #region # void KistenStandardInit() // initialisiert die Kistenfelder und deren Indexe aus den Standard-Feld
    /// <summary>
    /// initialisiert die Kistenfelder und deren Indexe aus den Standard-Feld
    /// </summary>
    void KistenStandardInit()
    {
      kistenAnzahl = 0;
      raumZuKiste = raumZuFeld.Select(i => (feldData[i] == '$' || feldData[i] == '*') ? kistenAnzahl++ : -1).ToArray();
      kistenMitZiel = raumZuKiste.Where((x, i) => x >= 0 && raumZiel[i] == 1).Count();
      kistenZuRaum = Enumerable.Range(0, kistenAnzahl).Select(i => raumZuKiste.ToList().IndexOf(i)).ToArray();
    }
    #endregion

    /// <summary>
    /// merkt sich die aktuell prüfenden Kistenpositionen (länge = kistenAnzahl)
    /// </summary>
    int[] steinerCheckKisten;

    /// <summary>
    /// merkt sich alle Kistenpositionen im SokowahnRaum
    /// </summary>
    int[] steinerCheckKistenRaum;

    /// <summary>
    /// initialisiert die ersten Steiner-Kisten
    /// </summary>
    void KistenSteinerInit()
    {
      raumZuKiste = raumZuFeld.Select(i => -1).ToArray();
      kistenZuRaum = new int[kistenAnzahl];

      steinerCheckKisten = Enumerable.Range(0, kistenAnzahl).Select(i => i).ToArray();
      steinerCheckKisten[kistenAnzahl - 1]--; // letzte eins zurück setzen, damit beim ersten steinerNext() auch die erste Variante gesetzt werden kann

      steinerCheckKistenRaum = raumZuFeld.Select(i => (feldData[i] == '$' || feldData[i] == '*') ? feldZuRaum[i] : -1).Where(i => i >= 0).ToArray();

      bekannteStellungen = new Dictionary<ulong, ushort>();
    }

    /// <summary>
    /// merkt sich die Stellungen, welche noch geprüft werden müssen
    /// </summary>
    List<Variante> steinerPrüfStellungen;

    /// <summary>
    /// momentane Leseposition in den Prüfstellungen
    /// </summary>
    int steinerPrüfstellungenPos;


    /// <summary>
    /// merkt sich alle bösen Stellungen
    /// </summary>
    Variante[] steinerBöseStellungen;

    /// <summary>
    /// setzt alle Kisten auf die nächste Stellung (nur Kisten-Startpositionen werden verwendet und der Spieler auch auf das Startfeld gesetzt
    /// </summary>
    /// <returns>true, wenn eine zu prüfende Stellung erzeugt wurde</returns>
    bool KistenSteinerNext()
    {
      // alte Kisten entfernen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKiste[kistenZuRaum[i]] = -1;

      #region # // --- nächste Kistenpositionen ermitteln ---
      switch (kistenAnzahl)
      {
        #region # case 1:
        case 1:
        {
          steinerCheckKisten[0]++;
          if (steinerCheckKisten[0] >= steinerCheckKistenRaum.Length) return false;
        } break;
        #endregion
        #region # case 2:
        case 2:
        {
          steinerCheckKisten[1]++;
          if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[0]++;
            steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
            if (steinerCheckKisten[1] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 3:
        case 3:
        {
          steinerCheckKisten[2]++;
          if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[1]++;
            if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[0]++;
              steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
            }
            steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
            if (steinerCheckKisten[2] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 4:
        case 4:
        {
          steinerCheckKisten[3]++;
          if (steinerCheckKisten[3] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[2]++;
            if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[1]++;
              if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 2)
              {
                steinerCheckKisten[0]++;
                steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
              }
              steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
            }
            steinerCheckKisten[3] = steinerCheckKisten[2] + 1;
            if (steinerCheckKisten[3] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 5:
        case 5:
        {
          steinerCheckKisten[4]++;
          if (steinerCheckKisten[4] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[3]++;
            if (steinerCheckKisten[3] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[2]++;
              if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length - 2)
              {
                steinerCheckKisten[1]++;
                if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 3)
                {
                  steinerCheckKisten[0]++;
                  steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
                }
                steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
              }
              steinerCheckKisten[3] = steinerCheckKisten[2] + 1;
            }
            steinerCheckKisten[4] = steinerCheckKisten[3] + 1;
            if (steinerCheckKisten[4] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 6:
        case 6:
        {
          steinerCheckKisten[5]++;
          if (steinerCheckKisten[5] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[4]++;
            if (steinerCheckKisten[4] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[3]++;
              if (steinerCheckKisten[3] == steinerCheckKistenRaum.Length - 2)
              {
                steinerCheckKisten[2]++;
                if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length - 3)
                {
                  steinerCheckKisten[1]++;
                  if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 4)
                  {
                    steinerCheckKisten[0]++;
                    steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
                  }
                  steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
                }
                steinerCheckKisten[3] = steinerCheckKisten[2] + 1;
              }
              steinerCheckKisten[4] = steinerCheckKisten[3] + 1;
            }
            steinerCheckKisten[5] = steinerCheckKisten[4] + 1;
            if (steinerCheckKisten[5] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 7:
        case 7:
        {
          steinerCheckKisten[6]++;
          if (steinerCheckKisten[6] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[5]++;
            if (steinerCheckKisten[5] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[4]++;
              if (steinerCheckKisten[4] == steinerCheckKistenRaum.Length - 2)
              {
                steinerCheckKisten[3]++;
                if (steinerCheckKisten[3] == steinerCheckKistenRaum.Length - 3)
                {
                  steinerCheckKisten[2]++;
                  if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length - 4)
                  {
                    steinerCheckKisten[1]++;
                    if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 5)
                    {
                      steinerCheckKisten[0]++;
                      steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
                    }
                    steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
                  }
                  steinerCheckKisten[3] = steinerCheckKisten[2] + 1;
                }
                steinerCheckKisten[4] = steinerCheckKisten[3] + 1;
              }
              steinerCheckKisten[5] = steinerCheckKisten[4] + 1;
            }
            steinerCheckKisten[6] = steinerCheckKisten[5] + 1;
            if (steinerCheckKisten[6] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        #region # case 8:
        case 8:
        {
          steinerCheckKisten[7]++;
          if (steinerCheckKisten[7] == steinerCheckKistenRaum.Length)
          {
            steinerCheckKisten[6]++;
            if (steinerCheckKisten[6] == steinerCheckKistenRaum.Length - 1)
            {
              steinerCheckKisten[5]++;
              if (steinerCheckKisten[5] == steinerCheckKistenRaum.Length - 2)
              {
                steinerCheckKisten[4]++;
                if (steinerCheckKisten[4] == steinerCheckKistenRaum.Length - 3)
                {
                  steinerCheckKisten[3]++;
                  if (steinerCheckKisten[3] == steinerCheckKistenRaum.Length - 4)
                  {
                    steinerCheckKisten[2]++;
                    if (steinerCheckKisten[2] == steinerCheckKistenRaum.Length - 5)
                    {
                      steinerCheckKisten[1]++;
                      if (steinerCheckKisten[1] == steinerCheckKistenRaum.Length - 6)
                      {
                        steinerCheckKisten[0]++;
                        steinerCheckKisten[1] = steinerCheckKisten[0] + 1;
                      }
                      steinerCheckKisten[2] = steinerCheckKisten[1] + 1;
                    }
                    steinerCheckKisten[3] = steinerCheckKisten[2] + 1;
                  }
                  steinerCheckKisten[4] = steinerCheckKisten[3] + 1;
                }
                steinerCheckKisten[5] = steinerCheckKisten[4] + 1;
              }
              steinerCheckKisten[6] = steinerCheckKisten[5] + 1;
            }
            steinerCheckKisten[7] = steinerCheckKisten[6] + 1;
            if (steinerCheckKisten[7] >= steinerCheckKistenRaum.Length) return false;
          }
        } break;
        #endregion
        default: throw new NotImplementedException(kistenAnzahl + "-Steiner noch nicht implementiert");
      }
      #endregion

      // neue Kisten setzen
      for (int i = 0; i < kistenAnzahl; i++) raumZuKiste[kistenZuRaum[i] = steinerCheckKistenRaum[steinerCheckKisten[i]]] = i;

      // Start-Spielerposition setzen
      raumSpielerPos = feldZuRaum[startSpielerPos];

      var stellung = GetStellung();
      ulong stellungCrc = StellungCrc(stellung);

      if (bekannteStellungen.ContainsKey(stellungCrc)) return true;

      steinerPrüfStellungen.Add(new Variante { stellung = stellung, stellungCrc = stellungCrc, tiefe = 1 });

      // bekannteStellungen.Add(stellungCrc, 1);

      return true;
    }

    #endregion

    #region # // --- Blocker-System ---

    /// <summary>
    /// merkt sich alle Blocker-Stellungen
    /// </summary>
    Blocker[] raumBlocker;

    /// <summary>
    /// Struktur eines Blocker-Feldes
    /// </summary>
    struct Blocker
    {
      /// <summary>
      /// Anzahl der Kisten für diesen Blocker
      /// </summary>
      private int anzahlKisten;
      /// <summary>
      /// Anzahl der bekannten Blocker-Einträge
      /// </summary>
      public int anzahlBlocker;
      /// <summary>
      /// eigentliche Blockerdaten (Länge: anzahlKisten * anzahlBlocker)
      /// </summary>
      private int[] blockerRaumKisten;
      /// <summary>
      /// Feld zum zusätzlichen merken der Stellungen, welche diesen Blocker erstellt haben (zum nachprüfen)
      /// </summary>
      private string[] blockerStellungen;

      /// <summary>
      /// fügt einen neuen Blocker in die Liste hinzu
      /// </summary>
      /// <param name="neuKisten">Kisten (mit SokowahnRaum-Positionen), welche hinzugefügt werden sollen</param>
      /// <param name="ustellung">zusätzlicher Stellungsaufbau (zum späteren nachprüfen)</param>
      public void Dazu(int[] neuKisten, ushort[] ustellung)
      {
        string stellung = new string(ustellung.Select(c => (char)c).ToArray());

        if (anzahlKisten == 0)
        {
          anzahlKisten = neuKisten.Length;
          anzahlBlocker = 0;
          blockerRaumKisten = new int[0];
          blockerStellungen = new string[0];
        }

        if (blockerStellungen.Any(x => x == stellung)) return; // Blocker-Stellung schon bekannt

        if (anzahlKisten != neuKisten.Length) throw new Exception("Fehler, Anzahl der Kisten stimmt nicht überein!");

        Array.Resize(ref blockerRaumKisten, blockerRaumKisten.Length + anzahlKisten);
        Array.Resize(ref blockerStellungen, blockerStellungen.Length + 1);

        int p = anzahlBlocker * anzahlKisten;
        blockerStellungen[anzahlBlocker++] = stellung;
        for (int i = 0; i < anzahlKisten; i++) blockerRaumKisten[p++] = neuKisten[i];
      }

      /// <summary>
      /// prüft, ob ein Blocker in der Liste bekannt ist, welcher die aktuelle Kistenstellung verbietet
      /// </summary>
      /// <param name="raumKisten">Raumfeld mit den jeweils gesetzten Kisten</param>
      /// <returns>true, wenn die Stellung als verboten erkannt wurde</returns>
      public bool Check(int[] raumKisten)
      {
        for (int checkBlocker = 0; checkBlocker < anzahlBlocker; checkBlocker++)
        {
          int p = checkBlocker * anzahlKisten;
          for (int i = 0; i < anzahlKisten; i++)
          {
            if (raumKisten[blockerRaumKisten[p + i]] < 0) break; // bei der ersten nicht passenden Kiste gleich zum nächsten Blocker springen
            if (i == anzahlKisten - 1) return true; // war bereits die letzte übereinstimmende Kiste -> Blocker zutreffend
          }
        }
        return false;
      }

      /// <summary>
      /// gibt den Aufbau als lesbaren String aus
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        if (anzahlBlocker == 0) return " - ";
        return " Kisten: " + anzahlKisten + ", Blocker: " + anzahlBlocker.ToString("#,##0") + " ";
      }
    }


    /// <summary>
    /// fügt die aktuelle Stellung in das Blocker-System ein
    /// </summary>
    void SteinerBlockerDazu(ushort[] stellung)
    {
      LadeStellung(stellung);
      int pl = raumLinks[raumSpielerPos] >= 0 ? raumZuKiste[raumLinks[raumSpielerPos]] : -1;
      int pr = raumRechts[raumSpielerPos] >= 0 ? raumZuKiste[raumRechts[raumSpielerPos]] : -1;
      int po = raumOben[raumSpielerPos] >= 0 ? raumZuKiste[raumOben[raumSpielerPos]] : -1;
      int pu = raumUnten[raumSpielerPos] >= 0 ? raumZuKiste[raumUnten[raumSpielerPos]] : -1;
      if (pl + pr + po + pu == -4) throw new Exception(); // es wurde keine Kiste direkt am Spieler gefunden

      if (raumBlocker.Length < kistenAnzahl * raumAnzahl) // zu klein?
      {
        Array.Resize(ref raumBlocker, kistenAnzahl * raumAnzahl);
      }

      raumBlocker[(kistenAnzahl - 1) * raumAnzahl + raumSpielerPos].Dazu(kistenZuRaum, stellung);
    }

    /// <summary>
    /// prüft, ob ein Blocker eventuell die aktuelle Stellung verbietet
    /// </summary>
    /// <returns>true wenn ein Blocker gefunden wurde, daher die aktuelle Stellung nicht lösbar ist</returns>
    bool IstBlocker()
    {
      for (int p = raumSpielerPos; p < raumBlocker.Length; p += raumAnzahl) if (raumBlocker[p].Check(raumZuKiste)) return true;
      return false;
    }
    #endregion

    #region # // --- Public Methoden und Properties ---
    #region # public bool Next(int limit) // berechnet den nächsten Schritt
    /// <summary>
    /// berechnet den nächsten Schritt
    /// </summary>
    /// <param name="limit">Anzahl der zu berechnenden (kleinen) Arbeitsschritte, negativer Wert = optionale Vorbereitungsschritte)</param>
    /// <returns>gibt an, ob noch weitere Berechnungen anstehen</returns>
    public bool Next(int limit)
    {
      if (limit == 0) return false;
      switch (modus)
      {
        #region # case Modus.unbekannt:
        case Modus.Unbekannt:
        {
          if (limit > 0)
          {
            modus = Modus.SucheInit; // Initialisierung für die Suche direkt starten
          }
          else
          {
            modus = Modus.SteinerInit; // Initialisierung der Vorbereitung starten
          }
          return true;
        }
        #endregion
        #region # case Modus.steinerInit:
        case Modus.SteinerInit:
        {
          if (limit > 0)
          {
            modus = Modus.SucheInit; // Vorbereitung abbrechen und Suche direkt starten
            return true;
          }

          int maxKisten = feldData.Where(c => c == '$' || c == '*').Count();

          kistenAnzahl++;

          if (kistenAnzahl >= maxKisten)
          {
            return false; // Maximale Kistenanzahl erreicht, weitere Berechnungen sind nicht mehr möglich
          }

          modus = Modus.SteinerVarianten; // neue Aufgabe: alle Varianten ermitteln

          KistenSteinerInit();
          steinerPrüfStellungen = new List<Variante>();
          steinerPrüfstellungenPos = 0;

          return true;
        }
        #endregion
        #region # case Modus.steinerVarianten:
        case Modus.SteinerVarianten:
        {
          if (limit > 0)
          {
            modus = Modus.SucheInit; // Vorbereitung abbrechen und Suche direkt starten
            return true;
          }

          limit = -limit;

          while (limit > 0)
          {
            if (steinerPrüfstellungenPos == steinerPrüfStellungen.Count)
            {
              limit--;
              if (!KistenSteinerNext())
              {
                modus = Modus.SteinerLösen;
                steinerBöseStellungen = steinerPrüfStellungen.Where(x => bekannteStellungen.ContainsKey(x.stellungCrc) && bekannteStellungen[x.stellungCrc] == 1).ToArray();
                steinerPrüfStellungen = steinerPrüfStellungen.Where(x => bekannteStellungen.ContainsKey(x.stellungCrc) && bekannteStellungen[x.stellungCrc] == 2).ToList();
                bekannteStellungen = bekannteStellungen.Where(x => x.Value == 1).ToDictionary(x => x.Key, x => x.Value);
                steinerPrüfstellungenPos = 0;
                return true;
              }
            }
            else
            {
              var prüf = steinerPrüfStellungen[steinerPrüfstellungenPos++];
              LadeStellung(prüf.stellung); limit--;
              if (kistenMitZiel == kistenAnzahl) bekannteStellungen[prüf.stellungCrc] = 2;
              foreach (var neuPrüf in SucheVariantenSteiner())
              {
                steinerPrüfStellungen.Add(neuPrüf);
                bekannteStellungen.Add(neuPrüf.stellungCrc, 1);
              }
            }
          }

          return true;
        }
        #endregion
        #region # case Modus.steinerLösen:
        case Modus.SteinerLösen:
        {
          if (limit > 0)
          {
            modus = Modus.SucheInit; // Vorbereitung abbrechen und Suche direkt starten
            return true;
          }

          limit = -limit;

          while (limit > 0)
          {
            if (steinerPrüfstellungenPos == steinerPrüfStellungen.Count)
            {
              // var dummy = steinerBöseStellungen.Where(x => bekannteStellungen.ContainsKey(x.stellungCrc)).ToArray();

              foreach (var satz in steinerBöseStellungen.Where(x => bekannteStellungen.ContainsKey(x.stellungCrc)))
              {
                SteinerBlockerDazu(satz.stellung);
              }

              modus = Modus.SteinerInit;

              return true;
            }

            var prüf = steinerPrüfStellungen[steinerPrüfstellungenPos++];
            LadeStellung(prüf.stellung); limit--;
            suchTiefe = 99999;

            var vorgänger = SucheVariantenVorgänger().ToArray();

            if (vorgänger.Length > 0)
            {
              foreach (var check in vorgänger)
              {
                if (bekannteStellungen.ContainsKey(check.stellungCrc))
                {
                  steinerPrüfStellungen.Add(new Variante { stellung = check.stellung, stellungCrc = check.stellungCrc, tiefe = 1 });
                  bekannteStellungen.Remove(check.stellungCrc);
                }
              }
            }
            else // gute Stellung, da keine Vorgänger möglich sind
            {
              if (bekannteStellungen.ContainsKey(prüf.stellungCrc)) bekannteStellungen.Remove(prüf.stellungCrc);
            }

          }

          return true;
        }
        #endregion
        #region # case Modus.sucheInit:
        case Modus.SucheInit:
        {
          raumSpielerPos = feldZuRaum[startSpielerPos];

          KistenStandardInit();

          suchListenAnzahl = new int[2048]; // maximal Spielzüge in die Tiefe berechenbar (kann jedoch problemlos vergrößert werden)
          suchListenSatz = kistenAnzahl + 1 + 1;

          suchListenDaten = new ushort[suchListenSatz];
          for (int i = 0; i < suchListenDaten.Length; i += suchListenSatz) suchListenDaten[i] = 32000; // alle Felder als frei kennzeichnen
          suchListenSuchFrei = 0;
          suchListenSuchNext = 0;

          suchTiefe = 0;
          SuchListeInsert(GetStellung(), suchTiefe);
          bekannteStellungen = new Dictionary<ulong, ushort>();
          bekannteStellungen[StellungCrc(GetStellung())] = 0; // Startstellung als bekannte Stellung hinzufügen

          modus = Modus.SucheRechne;
          return true;
        }
        #endregion
        #region # case Modus.sucheRechne:
        case Modus.SucheRechne:
        {
          limit = Math.Min(limit, suchListenAnzahl[suchTiefe]);
          for (int listenPos = 0; listenPos < limit; listenPos++)
          {
            LadeStellung(SuchListeNext(suchTiefe));
            if (kistenMitZiel == kistenAnzahl)
            {
              modus = Modus.SucheGefunden;
              return false;
            }

            if (StellungBekannt(StellungCrc(GetStellung())) < suchTiefe) continue; // zu prüfende Stellung wurde schon früher (mit weniger Tiefe) berechnet

            foreach (var variante in SucheVariantenHashcheck())
            {
              var tmpTiefe = StellungBekannt(variante.stellungCrc);
              if (tmpTiefe >= 0)
              {
                if (tmpTiefe <= variante.tiefe) continue; // Vorschlag ignorieren
                bekannteStellungen[variante.stellungCrc] = (ushort)variante.tiefe; // neue Tiefe setzen
                SuchListeInsert(variante.stellung, variante.tiefe);
              }
              else
              {
                bekannteStellungen.Add(variante.stellungCrc, (ushort)variante.tiefe);
                SuchListeInsert(variante.stellung, variante.tiefe);
              }
            }
          }
          if (suchListenAnzahl[suchTiefe] == 0) suchTiefe++; // keine weiteren Stellungen bei dieser Zugtiefe vorhanden, dann zur nächsten Zugtiefe springen
          return true;
        }
        #endregion
        case Modus.SucheGefunden: return false; // Ergebnis gefunden, keine weiteren Berechnungen notwendig
        default: throw new Exception("? " + modus);
      }

    }
    #endregion

    #region # public struct StellungsSatz // speichert die Struktur eines Hash-Datensatzes
    /// <summary>
    /// speichert die Struktur eines Hash-Datensatzes
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StellungsSatz
    {
      /// <summary>
      /// Stellung als Crc64 gespeichert
      /// </summary>
      public ulong stellung;
      /// <summary>
      /// Tiefe bei dieser Stellung
      /// </summary>
      public ushort tiefe;
      /// <summary>
      /// gibt den Inhalt lesbar aus
      /// </summary>
      /// <returns>lesbarer Inhalt als String</returns>
      public override string ToString()
      {
        return (new { stellung, tiefe }).ToString();
      }
    }
    #endregion

    #region # public long Refresh() // entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// <summary>
    /// entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// </summary>
    /// <returns>Anzahl der Einträge, welche entfernt werden konnten</returns>
    public long Refresh()
    {
      // 1. suchliste aufräumen
      for (int suchPos = 0; suchPos < suchListenDaten.Length; suchPos += suchListenSatz)
      {
        int tiefe = suchListenDaten[suchPos];
        if (tiefe < 32000)
        {
          ulong crc = StellungCrc(Enumerable.Range(suchPos + 1, suchListenSatz - 1).Select(i => suchListenDaten[i]).ToArray());
          int bekanntTiefe = StellungBekannt(crc);
          if (bekanntTiefe > 0 && bekanntTiefe < tiefe)
          {
            suchListenDaten[suchPos] = 32000;
            suchListenAnzahl[tiefe]--;
          }
        }
      }

      // 2. hash aufräumen
      var old = bekannteStellungen;
      bekannteStellungen = new Dictionary<ulong, ushort>();
      var übertrag = new List<StellungsSatz>();

      foreach (var satz in old)
      {
        if (satz.Value <= suchTiefe)
        {
          übertrag.Add(new StellungsSatz { stellung = satz.Key, tiefe = satz.Value });
        }
        else
        {
          bekannteStellungen.Add(satz.Key, satz.Value);
        }
      }

      übertrag.Sort((x, y) => x.stellung.CompareTo(y.stellung));

      ArchivEintrag(übertrag);

      return archivGro + (long)48000000;
    }
    #endregion

    #region # public IEnumerable<string> GetLösungsweg() // gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// <summary>
    /// gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// </summary>
    /// <returns>Lösungsweg als einzelne Spielfelder</returns>
    public IEnumerable<string> GetLösungsweg()
    {
      switch (modus)
      {
        case Modus.SucheRechne:
        {
          for (int i = 0; i < suchTiefe; i++) yield return "dummy" + i;
          yield return ToString();
          yield break;
        }
        case Modus.SucheGefunden:
        {
          int tmpTiefe = suchTiefe;
          var tmpStellung = GetStellung();
          while (suchTiefe > 0)
          {
            yield return ToString();
            var alleVorgänger = SucheVariantenVorgänger().ToArray();
            var vorgänger = alleVorgänger.Where(x => StellungBekannt(x.stellungCrc) == x.tiefe).FirstOrDefault();
            if (vorgänger.tiefe == 0)
            {
              yield return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x => feldData[x + y * feldBreite]).ToArray()) + "\r\n"));
              LadeStellung(tmpStellung); suchTiefe = tmpTiefe; // Endstand wieder herstellen
              yield break;
            }
            LadeStellung(vorgänger.stellung);
            suchTiefe = vorgänger.tiefe;
          }
          LadeStellung(tmpStellung); suchTiefe = tmpTiefe; // Endstand wieder herstellen
          yield break;
        }
        default:
        {
          yield return ToString();
          yield break;
        }
      }
    }

    /// <summary>
    /// gibt die aktuelle Suchtiefe zurück
    /// </summary>
    public int SuchTiefe
    {
      get
      {
        return modus == Modus.SucheRechne ? suchTiefe : GetLösungsweg().Count() - 1;
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
        switch (modus)
        {
          case Modus.SucheRechne: return bekannteStellungen.Count + (long)archivGro;
          case Modus.SteinerVarianten: return bekannteStellungen.Count;
          case Modus.SteinerLösen: return bekannteStellungen.Count;
          default: return 0;
        }
      }
    }
    #endregion

    #region # public int KnotenRest // gibt die Anzahl der noch zu berechnenden Knoten zurück (kann sich nach einer Berechnung erhöhen)
    /// <summary>
    /// gibt die Anzahl der noch zu berechnenden Knoten zurück (kann sich nach einer Berechnung erhöhen)
    /// </summary>
    /// <returns>Anzahl der noch zu berechnenden Knoten</returns>
    public long KnotenRest
    {
      get
      {
        switch (modus)
        {
          case Modus.SucheRechne: return suchListenAnzahl.Sum(anzahl => (long)anzahl);
          case Modus.SteinerVarianten: return steinerPrüfStellungen.Count - steinerPrüfstellungenPos;
          case Modus.SteinerLösen: return steinerPrüfStellungen.Count - steinerPrüfstellungenPos;
          default: return 0;
        }
      }
    }
    #endregion

    #region # public override string ToString() // gibt das gesamte Spielfeld als lesbaren (genormten) Inhalt aus (Format siehe: <see cref="http://de.wikipedia.org/wiki/Sokoban#Levelnotation">Wikipedia</see> )
    /// <summary>
    /// lädt eine Stellung und zeigt diese direkt an
    /// </summary>
    /// <param name="stellung">Stellung, welche geladen werden soll</param>
    /// <returns>fertig sichtbare Stellung</returns>
    // ReSharper disable once UnusedMember.Local
    string ToString(ushort[] stellung)
    {
      LadeStellung(stellung);

      return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x =>
      {
        int p = feldZuRaum[x + y * feldBreite];
        if (p < 0) return feldLeer[x + y * feldBreite];
        if (raumZuKiste[p] >= 0) return raumZiel[p] == 1 ? '*' : '$';
        if (p == raumSpielerPos) return raumZiel[p] == 1 ? '+' : '@';
        return raumZiel[p] == 1 ? '.' : ' ';
      }).ToArray()) + "\r\n"));
    }
    /// <summary>
    /// gibt das gesamte Spielfeld als lesbaren (genormten) Inhalt aus (Format siehe: <see cref="http://de.wikipedia.org/wiki/Sokoban#Levelnotation">Wikipedia</see> )
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      switch (modus)
      {
        case Modus.Unbekannt: return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x => feldData[x + y * feldBreite]).ToArray()) + "\r\n"));

        case Modus.SucheInit: goto case Modus.Unbekannt;

        case Modus.SucheRechne: return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x =>
        {
          int p = feldZuRaum[x + y * feldBreite];
          if (p < 0) return feldLeer[x + y * feldBreite];
          if (raumZuKiste[p] >= 0) return raumZiel[p] == 1 ? '*' : '$';
          if (p == raumSpielerPos) return raumZiel[p] == 1 ? '+' : '@';
          return raumZiel[p] == 1 ? '.' : ' ';
        }).ToArray()) + "\r\n")) + "\r\n" + string.Concat(Enumerable.Range(0, suchListenAnzahl.Length).Where(i => suchListenAnzahl[i] > 0).Select(i => i + ": " + suchListenAnzahl[i].ToString("#,##0") + "\r\n"));

        case Modus.SucheGefunden: return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x =>
        {
          int p = feldZuRaum[x + y * feldBreite];
          if (p < 0) return feldLeer[x + y * feldBreite];
          if (raumZuKiste[p] >= 0) return raumZiel[p] == 1 ? '*' : '$';
          if (p == raumSpielerPos) return raumZiel[p] == 1 ? '+' : '@';
          return raumZiel[p] == 1 ? '.' : ' ';
        }).ToArray()) + "\r\n"));

        case Modus.SteinerInit: return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x => feldData[x + y * feldBreite]).ToArray()) + "\r\n"));
        case Modus.SteinerVarianten:
        {
          if (steinerPrüfStellungen.Count == 0) goto case Modus.Unbekannt;
          return string.Concat(Enumerable.Range(0, feldHöhe).Select(y => new string(Enumerable.Range(0, feldBreite).Select(x =>
          {
            int p = feldZuRaum[x + y * feldBreite];
            if (p < 0) return feldLeer[x + y * feldBreite];
            if (raumZuKiste[p] >= 0) return raumZiel[p] == 1 ? '*' : '$';
            if (p == raumSpielerPos) return raumZiel[p] == 1 ? '+' : '@';
            return raumZiel[p] == 1 ? '.' : ' ';
          }).ToArray()) + "\r\n")) + "\r\n" + string.Concat(Enumerable.Range(1, kistenAnzahl - 1).Select(k => k + "-Steiner: " + Enumerable.Range(raumAnzahl * (k - 1), raumAnzahl).Select(i => i < raumBlocker.Length ? raumBlocker[i].anzahlBlocker : 0).Sum().ToString("#,##0") + "\r\n")) + kistenAnzahl + "-Steiner: suche...";
        }
        case Modus.SteinerLösen: goto case Modus.SteinerVarianten;

        default: return "";
      }
    }
    #endregion
    #endregion
  }
}
