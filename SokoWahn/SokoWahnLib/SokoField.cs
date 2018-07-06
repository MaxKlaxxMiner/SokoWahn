#region # using *.*
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
    }
  }
}
