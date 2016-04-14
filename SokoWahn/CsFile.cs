#region # using *.*

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
// ReSharper disable UnusedMember.Global

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum einfachen erstellen einer C#-Datei
  /// </summary>
  internal sealed class CsFile
  {
    /// <summary>
    /// merkt sich die Daten der Datei
    /// </summary>
    readonly StringBuilder data;

    /// <summary>
    /// gibt an, ob die Zeilen vereinfacht geschrieben werden dürfen (z.B. statt " ein ' und statt ' ein ´)
    /// </summary>
    readonly bool simplify;

    /// <summary>
    /// merkt sich Tab-Größe, wieviel Leerzeichen eingerückt werden sollen
    /// </summary>
    readonly int indentSize;

    /// <summary>
    /// merkt sich die aktuelle Einrückungs-Größe
    /// </summary>
    int indentPos;


    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="simplifyQuote">gibt an, ob die Zeilen vereinfacht geschrieben werden dürfen (z.B. statt " ein ' und statt ' ein ´)</param>
    /// <param name="indentTabSize">gibt die Tab-Größe an, wieviel Leerzeichen immer eingerückt werden sollen</param>
    public CsFile(bool simplifyQuote = false, int indentTabSize = 2)
    {
      data = new StringBuilder();
      simplify = simplifyQuote;
      indentSize = indentTabSize;
      indentPos = 0;
    }

    /// <summary>
    /// schreibt eine Zeile mit optionale, weiteren Block in die Datei
    /// </summary>
    /// <param name="line">Zeile, welche geschrieben werden soll</param>
    /// <param name="block">optionale Block-Methode für weitere Zeilen in einem {}-Block</param>
    public void Write(string line, Action<CsFile> block = null)
    {
      if (simplify) line = line.Replace('\'', '\"').Replace('´', '\'');

      if (!string.IsNullOrWhiteSpace(line)) data.Append(' ', indentPos).Append(line);
      data.AppendLine();

      if (block == null) return;

      data.Append(' ', indentPos).Append('{').AppendLine();

      indentPos += indentSize;
      block(this);
      indentPos -= indentSize;

      data.Append(' ', indentPos).Append('}').AppendLine();
    }

    /// <summary>
    /// schreibt eine oder mehrere Zeilen in die Datei (leer = Leerzeile)
    /// </summary>
    /// <param name="lines">Zeilen, welche geschrieben werden sollen</param>
    public void Write(params string[] lines)
    {
      if (lines.Length == 0) Write("");
      foreach (var line in lines)
      {
        Write(line);
      }
    }

    /// <summary>
    /// schreibt mehrere Zeilen in die Datei
    /// </summary>
    /// <param name="lines">Enumerable der Zeilen, welche geschrieben werden sollen</param>
    public void Write(IEnumerable<string> lines)
    {
      foreach (var line in lines)
      {
        Write(line);
      }
    }

    /// <summary>
    /// speichert die gesamte C#-Datei auf die Festplatte
    /// </summary>
    /// <param name="filename"></param>
    public void SaveToFile(string filename)
    {
      File.WriteAllText(filename, data.ToString());
    }

    /// <summary>
    /// gibt den gesamten Inhalt der C#-Datei zurück
    /// </summary>
    /// <returns>Inhalt des C#-Datei</returns>
    public override string ToString()
    {
      return data.ToString();
    }
  }
}
