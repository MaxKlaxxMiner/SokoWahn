// ReSharper disable NotAccessedField.Global
using System.Linq;
using System.Text;
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Struktur eines kompletten Varianten-Datensatzes
  /// </summary>
  public struct VariantData
  {
    /// <summary>
    /// vorheriger Raum-Zustand
    /// </summary>
    public ulong oldState;
    /// <summary>
    /// Anzahl der Bewegungsschritte (nur Bewegungen innerhalb des Raumes und beim Verlassen des Raumes werden gezählt)
    /// </summary>
    public ulong moves;
    /// <summary>
    /// Anzahl der Kistenverschiebungen (nur Verschiebungen innerhalb des Raumes oder beim Verlassen des Raumes werden gezählt)
    /// </summary>
    public ulong pushes;
    /// <summary>
    /// alle Portale, wohin eine Kiste rausgeschoben wurde
    /// </summary>
    public uint[] oPortalIndexBoxes;
    /// <summary>
    /// Portal, wo der Spieler den Raum zum Schluss verlassen hat (uint.MaxValue: Spieler verbleibt irgendwo im Raum = Zielstellung erreicht)
    /// </summary>
    public uint oPortalIndexPlayer;
    /// <summary>
    /// nachfolgender Raum-Zustand
    /// </summary>
    public ulong newState;
    /// <summary>
    /// optionaler Pfad in XSB-Schreibweise (lrudLRUD bzw. auch RLE komprimiert erlaubt)
    /// </summary>
    public string path;

    /// <summary>
    /// komprimiert einen Pfad mit RLE-Code
    /// </summary>
    /// <param name="path">Pfad, welcher komprimiert werden soll</param>
    /// <returns>fertig komprimierter Pfad</returns>
    public static string CompressPath(string path)
    {
      if (string.IsNullOrEmpty(path) || path.Any(char.IsDigit)) return path; // der Pfad ist leer oder wurde bereits komprimiert

      var output = new StringBuilder();

      for (int pos = 0; pos < path.Length; )
      {
        int len = 1;
        while (pos + len < path.Length && path[pos + len] == path[pos]) len++;
        if (len < 3)
        {
          output.Append(path[pos++]);
        }
        else
        {
          output.Append(len).Append(path[pos]);
          pos += len;
        }
      }

      return output.ToString();
    }

    /// <summary>
    /// dekomprimiert einen RLE-komprimierten Pfad wieder
    /// </summary>
    /// <param name="path">Pfad, welcher dekomprimiert werden soll</param>
    /// <returns>fertig dekomprimierter Pfad</returns>
    public static string UncompressPath(string path)
    {
      var output = new StringBuilder();

      for (int p = 0; p < path.Length; p++)
      {
        int count = 0;

        while (char.IsDigit(path[p]))
        {
          count *= 10;
          count += path[p] - '0';
          p++;
        }

        if (count == 0) count = 1;

        output.Append(path[p], count);
      }

      return output.ToString();
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { moves, pushes, newState, boxPortalsIndices = "int[" + oPortalIndexBoxes.Length + "]", path }.ToString();
    }
  }
}
