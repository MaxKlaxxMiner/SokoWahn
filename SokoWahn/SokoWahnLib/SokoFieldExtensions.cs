#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Hilfmethoden für SokoWahn-Spielfeld
  /// </summary>
  public static class SokoFieldExtensions
  {
    /// <summary>
    /// gibt das gesamte Spielfeld im Text-Format aus
    /// </summary>
    /// <param name="field">Spielfeld, welches ausgegeben werden soll</param>
    /// <returns>fertiges Spielfeld im Textformat</returns>
    public static unsafe string GetText(this ISokoField field)
    {
      int w = field.Width;
      int h = field.Height;
      string result = UnsafeHelper.FastAllocateString((w + 2) * h); // Ausgabe Größe berechnen (pro Zeile 2 Zeichen extra für die Zeilenumbrüche)

      fixed (char* resultP = result)
      {
        for (int resultPos = 0, fieldPos = 0; resultPos < result.Length; )
        {
          for (int x = 0; x < w; x++)
          {
            resultP[resultPos++] = field.GetField(fieldPos++);
          }
          resultP[resultPos++] = '\r'; // Zeilenumbruch hinzufügen
          resultP[resultPos++] = '\n';
        }
      }

      return result;
    }
  }
}
