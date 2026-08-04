// Konsolen-Frontend fuer den alten C#-Solver (4th generation)
// Dient als Referenz-Orakel fuer den Go-Nachbau: deterministische Ausgaben
// (kompiliert mit -define:parallelDeaktivieren, siehe build.sh)

using System;
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

      string level = File.ReadAllText(args[0]);

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
  }
}
