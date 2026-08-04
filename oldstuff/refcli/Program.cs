// Konsolen-Frontend fuer den alten C#-Solver (4th generation)
// Dient als Referenz-Orakel fuer den Go-Nachbau: deterministische Ausgaben
// (kompiliert mit -define:parallelDeaktivieren, siehe build.sh)

using System;
using System.IO;
using System.Linq;
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
      int batch = args.Length >= 2 ? int.Parse(args[1]) : 1000000000;
      int prep = args.Length >= 3 ? int.Parse(args[2]) : 0;
      bool verbose = args.Contains("-v");

      // Hinweis: der Solver legt temp\blocker_x<hex>.gz im Arbeitsverzeichnis an -
      // fuer reproduzierbare Laeufe ohne Blocker vorher den temp-Ordner leeren
      var solver = new SokoWahn_4th(level);

      Console.WriteLine(solver.ToString());

      // --- optionale Blocker-Vorbereitung (entspricht negativem Limit der alten GUI) ---
      for (int p = 0; p < prep; p++)
      {
        if (!solver.Next(-batch)) break;
      }
      if (prep > 0)
      {
        Console.WriteLine("--- Blocker-Stand nach Vorbereitung ---");
        Console.WriteLine(solver.ToString());
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
          Console.WriteLine("Tiefe " + letzteTiefe.ToString().PadLeft(4) + ": Knoten=" + solver.KnotenAnzahl + " Rest=" + solver.KnotenRest);
        }
      }

      Console.WriteLine();
      Console.WriteLine("Fertig nach " + batches + " Batches: SuchTiefe=" + solver.SuchTiefe + " Knoten=" + solver.KnotenAnzahl);

      var weg = solver.GetLösungsweg().ToArray();
      Console.WriteLine("Loesungsweg-Stellungen: " + weg.Length + " (Zuege: " + (weg.Length - 1) + ")");
      Console.WriteLine(SokowahnStaticTools.LösungswegZuSteps(weg));

      return 0;
    }
  }
}
