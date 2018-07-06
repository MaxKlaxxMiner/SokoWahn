﻿#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse für ein komplettes SokoWahn-Spielfeld
  /// </summary>
  public class SokoField
  {
    /// <summary>
    /// Breite des Spielfeldes in Spalten
    /// </summary>
    public readonly int width;
    /// <summary>
    /// Höhe des Spielfeldes in Zeilen
    /// </summary>
    public readonly int height;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txtField">komplettes Spielfeld im Textformat</param>
    public SokoField(string txtField)
    {
      // --- Spielfeld einlesen ---
      var lines = txtField.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
      width = lines.Max(line => line.Length);
      height = lines.Length;
      var field = new char[width * height];
      for (int y = 0; y < height; y++)
      {
        string line = lines[y]; // Zeile abfragen
        for (int x = 0; x < width; x++)
        {
          char c = x < line.Length ? line[x] : ' '; // Zeichen in der Zeile Abfragen (außerhalb der Zeile = Leerzeichen)
          switch (c)
          {
            case '@': // Spieler
            case '+': // Spieler auf einem Zielfeld
            case '$': // verschiebare Kiste
            case '.': // Zielfeld
            case '*': // Kiste auf einem Zielfeld
            case '#': // Mauer
            case ' ': // leeres Spielfeld
            field[x + y * width] = c; break; // den Wert übertragen

            default: c = ' '; goto case ' '; // bei unbekannten Zeichen ein leeres Spielfeld verwenden
          }
        }
      }
    }
  }
}