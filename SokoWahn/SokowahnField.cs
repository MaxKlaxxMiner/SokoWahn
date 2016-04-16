#region # using *.*

using System;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum einlesen und verarbeiten von Spielfeldern
  /// </summary>
  internal unsafe sealed class SokowahnField
  {
    #region # // --- Variablen ---
    /// <summary>
    /// gibt die Breite des Spielfeldes an
    /// </summary>
    public readonly int width;

    /// <summary>
    /// gibt die Höhe des Spielfeldes an
    /// </summary>
    public readonly int height;

    /// <summary>
    /// merkt sich die gesamte Länge des Spielfeldes
    /// </summary>
    public readonly int fieldLength;

    /// <summary>
    /// merkt sich das eigentliche Spielfeld
    /// </summary>
    public readonly char[] fieldData;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fieldText">Daten des Spielfeldes als Textdaten</param>
    public SokowahnField(string fieldText)
    {
      // --- Zeilen einlesen ---
      var lines = fieldText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", "        ").Split('\n');

      lines = NormalizeLines(lines);
      if (lines.Length < 3) throw new ArgumentException("kein sinnvolles Spielfeld gefunden");

      width = lines.First().Length;
      height = lines.Length;
      fieldLength = width * height;
      fieldData = string.Concat(lines).ToCharArray();
      if (fieldData.Length != fieldLength) throw new InvalidOperationException();
    }
    #endregion

    #region # static string[] NormalizeLines(string[] lines) // normalisiert die Zeilen
    /// <summary>
    /// normalisiert die Zeilen
    /// </summary>
    /// <param name="lines">Zeilen, welche normalisiert werden sollen</param>
    /// <returns>fertig normalisierte Zeilen</returns>
    static string[] NormalizeLines(string[] lines)
    {
      var tmp = lines.ToArray();

      // --- Kommentare am Ende wegschneiden ---
      for (int i = 0; i < tmp.Length; i++)
      {
        int pos = tmp[i].IndexOfAny(new[] { ';', '/' });
        if (pos < 0) continue;
        tmp[i] = tmp[i].Substring(0, pos);
      }

      // --- ungültige Zeichen filtern und durch Leerzeichen ersetzen ---
      foreach (string line in tmp)
      {
        int len = line.Length;
        fixed (char* chars = line)
        {
          for (int i = 0; i < len; i++)
          {
            switch (chars[i])
            {
              case '#': // solide Wand

              case ' ': // leeres Spielfeld
              case '.': // offenes Zielfeld

              case '$': // Box im freien Raum
              case '*': // Box auf einem Zielfeld

              case '@': // Spieler im freien Raum
              case '+': // Spieler auf einem Zielfeld

              break;

              default: chars[i] = ' '; break; // andere Zeichen durch Leerezeichen ersetzen
            }
          }
        }
      }

      // --- erste Spielfeld-Zeile suchen ---
      int firstLine = 0;
      while (firstLine < tmp.Length - 1 && tmp[firstLine].Trim().Length == 0) firstLine++;

      // --- letzte Spielfeld-Zeile suchen ---
      int lastLine = firstLine;
      while (lastLine < tmp.Length - 1 && tmp[lastLine + 1].Trim().Length > 0) lastLine++;

      // --- Zeilen zurecht schneiden ---
      tmp = tmp.Skip(firstLine).Take(Math.Max(0, lastLine - firstLine + 1)).ToArray();

      // --- Länge der Zeilen nach dem Ende ausrichten ---
      int maxLenEnd = tmp.Max(line => line.TrimEnd().Length);
      for (int i = 0; i < tmp.Length; i++) tmp[i] = tmp[i].PadRight(maxLenEnd).Substring(0, maxLenEnd);

      // --- vorderen Einschub der Zeilen entfernen ---
      int maxLenStart = tmp.Max(line => line.TrimStart().Length);
      for (int i = 0; i < tmp.Length; i++) tmp[i] = tmp[i].Remove(0, tmp[i].Length - maxLenStart);

      return tmp;
    }
    #endregion

    #region # public override string ToString() // gibt den Inhalt des gesamten Spielfeldes aus
    /// <summary>
    /// gibt den Inhalt des gesamten Spielfeldes aus
    /// </summary>
    /// <returns>Inhalt des Spielfeldes</returns>
    public override string ToString()
    {
      return string.Join(Environment.NewLine, Enumerable.Range(0, height).Select(line => new string(fieldData.Skip(line * width).Take(width).ToArray())));
    }
    #endregion
  }
}
