// Konsolen-Frontend fuer den alten C#-Solver (4th generation)
// Dient als Referenz-Orakel fuer den Go-Nachbau: deterministische Ausgaben
// (kompiliert mit -define:parallelDeaktivieren, siehe build.sh)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sokosolver.SokowahnTools;

namespace Sokosolver
{
  internal static class RefCli
  {
    private static int Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Aufruf: refcli <levelDatei> [batchGroesse] [prepBatches] [-v]");
        Console.WriteLine("  batchGroesse: Stellungen pro Next()-Aufruf (Standard: 1000000000 = ganze Tiefenstufe)");
        Console.WriteLine("  prepBatches:  Anzahl Blocker-Vorbereitungs-Batches (Standard: 0 = Blocker sofort abbrechen)");
        Console.WriteLine("  -v:           nach jedem Batch die komplette Tiefenstatistik ausgeben");
        return 1;
      }

      string level = FreezeGoalBoxesToWalls(File.ReadAllText(args[0]));

      // Blocker-Vergleichsmodus: nur die Blocker-Stufen bis maxK berechnen und ausgeben
      if (args.Length >= 3 && (args[1] == "blocker" || args[1] == "blockerbx"))
      {
        return BlockerOnly(level, int.Parse(args[2]), args[1]);
      }

      int batch = args.Length >= 2 ? int.Parse(args[1]) : 1000000000;
      int prep = args.Length >= 3 ? int.Parse(args[2]) : 0;
      bool verbose = args.Contains("-v");

      // Hinweis: der Solver legt temp\blocker_x<hex>.gz im Arbeitsverzeichnis an -
      // fuer reproduzierbare Laeufe ohne Blocker vorher den temp-Ordner leeren
      var solver = new SokoWahn_4th(level);

      Console.WriteLine(solver.ToString());

      // --- optionale Blocker-Vorbereitung (entspricht negativem Limit der alten GUI) ---
      string letzterBlockerStand = null;
      for (int p = 0; p < prep; p++)
      {
        if (!solver.Next(-batch)) break;
        letzterBlockerStand = solver.ToString(); // letzten Stand merken (nach Abschluss verschwinden die Blocker-Zeilen)
      }
      if (prep > 0)
      {
        Console.WriteLine("--- Blocker-Stand nach Vorbereitung ---");
        Console.WriteLine(letzterBlockerStand ?? solver.ToString());
      }

      // --- eigentliche Suche ---
      long batches = 0;
      int letzteTiefe = -1;

      while (solver.Next(batch))
      {
        batches++;
        if (verbose)
        {
          Console.WriteLine("--- Batch " + batches + " ---");
          Console.WriteLine(solver.ToString());
        }
        else if (solver.SuchTiefe != letzteTiefe)
        {
          letzteTiefe = solver.SuchTiefe;
          // Tausender-Punkte wie in der Go-Ausgabe (Diff-Vergleiche bleiben damit byte-genau)
          Console.WriteLine("Tiefe " + letzteTiefe.ToString().PadLeft(4) + ": Knoten=" + solver.KnotenAnzahl.ToString("#,##0") + " Rest=" + solver.KnotenRest.ToString("#,##0"));
        }
      }

      Console.WriteLine();
      Console.WriteLine("Fertig nach " + batches + " Batches: SuchTiefe=" + solver.SuchTiefe + " Knoten=" + solver.KnotenAnzahl.ToString("#,##0"));

      var weg = solver.GetLösungsweg().ToArray();
      Console.WriteLine("Loesungsweg-Stellungen: " + weg.Length + " (Zuege: " + (weg.Length - 1) + ")");
      Console.WriteLine(SokowahnStaticTools.LösungswegZuSteps(weg));

      return 0;
    }

    /// <summary>
    /// berechnet nur die Blocker-Stufen bis einschliesslich maxK und gibt sie aus (ohne Cache-Datei)
    /// </summary>
    /// <param name="variante">"blocker" = SokowahnBlocker (4th plain), "blockerbx" = SokowahnBlockerBx (4th List2)</param>
    private static int BlockerOnly(string level, int maxK, string variante)
    {
      int feldBreite, feldHöhe, spielerPos;
      char[] feldData, feldDataLeer;
      SokowahnStaticTools.SpielfeldEinlesen(level, out feldBreite, out feldHöhe, out spielerPos, out feldData, out feldDataLeer);

      var raum = new SokowahnRaum(feldData, feldBreite);

      Directory.CreateDirectory("temp");
      string datei = "temp\\refblocker_only.gz";
      if (File.Exists(datei)) File.Delete(datei); // immer frisch rechnen

      ISokowahnBlocker blocker;
      if (variante == "blockerbx") blocker = new SokowahnBlockerBx(datei, raum);
      else blocker = new SokowahnBlocker(datei, raum);

      var fertigeStufe = new Regex(@"^\[\d+\] - [\d.,]+ - [\d.,]+\s*$");

      while (blocker.Next(int.MaxValue))
      {
        int fertig = blocker.ToString().Split('\n').Count(zeile => fertigeStufe.IsMatch(zeile.TrimEnd()));
        if (fertig >= maxK) break;
      }

      Console.WriteLine(blocker.ToString());
      if (File.Exists(datei)) File.Delete(datei);
      return 0;
    }

    #region # // --- FreezeGoalBoxesToWalls: eingefrorene Kisten auf Zielen durch Waende ersetzen ---
    /// <summary>
    /// ersetzt eingefrorene Kisten auf Zielfeldern durch Waende (JSoko-Verhalten,
    /// gleiche Logik wie freeze.go in den Go-Ports): eine Kiste, die nie mehr bewegt
    /// werden kann und ihr Ziel bereits bedient, ist von einer Wand nicht zu
    /// unterscheiden - Kiste und Ziel entfallen ersatzlos. Erkennung per klassischer
    /// Freeze-Analyse (Waende, rekursiv auch gegenseitig blockierte Zielfeld-Kisten
    /// wie 2x2-Bloecke), kaskadierend bis zum Fixpunkt. Kisten abseits der Ziele
    /// zaehlen konservativ nie als Blockade, Felder ausserhalb des Rasters nicht als Wand.
    /// </summary>
    private static string FreezeGoalBoxesToWalls(string level)
    {
      var lines = level.Replace("\r", "").Split('\n');
      int width = lines.Max(zeile => zeile.Length);
      int height = lines.Length;
      if (width == 0) return level;
      var raw = lines.Select(zeile => zeile.PadRight(width).ToCharArray()).ToArray();

      bool geaendert = true;
      while (geaendert) // Fixpunkt: neue Waende koennen weitere Kisten einfrieren
      {
        geaendert = false;
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            if (raw[y][x] != '*') continue;
            if (FrozenBox(raw, width, height, x, y, new HashSet<int>()))
            {
              raw[y][x] = '#';
              geaendert = true;
            }
          }
        }
      }

      return string.Join("\n", raw.Select(zeile => new string(zeile)));
    }

    /// <summary>
    /// prueft, ob die Zielfeld-Kiste auf (x,y) eingefroren ist; treatAsWall enthaelt
    /// die im aktuellen Pruefpfad besuchten Kisten (Positionen als x + y*width)
    /// </summary>
    private static bool FrozenBox(char[][] raw, int width, int height, int x, int y, HashSet<int> treatAsWall)
    {
      treatAsWall.Add(x + y * width);
      return FrozenAxis(raw, width, height, x, y, 1, 0, treatAsWall)
          && FrozenAxis(raw, width, height, x, y, 0, 1, treatAsWall);
    }

    /// <summary>
    /// prueft, ob die Kiste auf (x,y) entlang einer Achse blockiert ist
    /// </summary>
    private static bool FrozenAxis(char[][] raw, int width, int height, int x, int y, int dx, int dy, HashSet<int> treatAsWall)
    {
      if (WallLike(raw, width, height, x - dx, y - dy, treatAsWall)
       || WallLike(raw, width, height, x + dx, y + dy, treatAsWall)) return true;

      // Nachbar-Kiste auf Ziel, die ihrerseits eingefroren ist?
      // (Set pro Zweig kopieren, damit gescheiterte Pruefpfade nicht als Wand nachwirken)
      for (int seite = -1; seite <= 1; seite += 2)
      {
        int nx = x + dx * seite, ny = y + dy * seite;
        if (nx < 0 || nx >= width || ny < 0 || ny >= height || raw[ny][nx] != '*') continue;
        if (FrozenBox(raw, width, height, nx, ny, new HashSet<int>(treatAsWall))) return true;
      }
      return false;
    }

    /// <summary>
    /// gibt an, ob das Feld fuer die Freeze-Analyse als Wand zaehlt
    /// (ausserhalb des Rasters: konservativ keine Wand)
    /// </summary>
    private static bool WallLike(char[][] raw, int width, int height, int x, int y, HashSet<int> treatAsWall)
    {
      if (x < 0 || x >= width || y < 0 || y >= height) return false;
      return raw[y][x] == '#' || treatAsWall.Contains(x + y * width);
    }
    #endregion
  }
}
