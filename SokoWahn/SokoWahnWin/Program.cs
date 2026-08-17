using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SokoWahnLib;

namespace SokoWahnWin
{
  static class Program
  {
    /// <summary>
    /// Der Haupteinstiegspunkt für die Anwendung.
    /// Optionales Argument: Pfad zu einer Level-Datei (z.B. aus dem levelcache
    /// des Go-Projekts) - ohne Argument starten die einkompilierten Test-Räume.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      SokoField levelField = null;
      if (args.Length > 0)
      {
        try
        {
          levelField = LoadLevelFile(args[0]);
        }
        catch (Exception exc)
        {
          MessageBox.Show("Level-Datei konnte nicht geladen werden:\r\n" + args[0] + "\r\n\r\n" + exc.Message,
                          "SokoWahn Rooms", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }
      }

      Application.Run(new FormDebugger(levelField));
    }

    /// <summary>
    /// lädt ein Level aus einer Textdatei; Meta-Zeilen nach dem Level-Block
    /// (z.B. "url: ..." im levelcache-Format) werden abgeschnitten
    /// </summary>
    /// <param name="path">Pfad zur Level-Datei</param>
    /// <returns>fertig geparstes Spielfeld</returns>
    static SokoField LoadLevelFile(string path)
    {
      var sb = new System.Text.StringBuilder();
      bool levelSeen = false;
      foreach (var line in System.IO.File.ReadAllLines(path))
      {
        bool levelLine = line.Trim().Length > 0 && line.All(c => "# .$*@+".IndexOf(c) >= 0);
        if (levelLine)
        {
          sb.AppendLine(line);
          levelSeen = true;
        }
        else if (levelSeen)
        {
          break; // Ende des Level-Blocks erreicht (Leerzeile oder Meta-Daten)
        }
      }
      return new SokoField(sb.ToString());
    }
  }
}
