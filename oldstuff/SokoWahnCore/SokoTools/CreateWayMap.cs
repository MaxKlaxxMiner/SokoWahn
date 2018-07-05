
#region # using *.*

using System.Collections.Generic;
// ReSharper disable UnusedMember.Global

#endregion

namespace SokoWahnCore
{
  public static partial class SokoTools
  {
    /// <summary>
    /// berechnet die Laufwege und gibt eine Map zurück, wo sich der Spieler aufhalten kann
    /// </summary>
    /// <param name="field">Daten des Spielfeldes</param>
    /// <param name="width">Breite des Spielfeldes</param>
    /// <param name="playerStart">Startposition des Spielers</param>
    /// <returns>fertige Wege-Map</returns>
    public static bool[] CreateWayMap(char[] field, int width, int playerStart)
    {
      var way = new bool[field.Length];

      var check = new Stack<int>();
      check.Push(playerStart);

      while (check.Count > 0)
      {
        int pos = check.Pop();
        if (way[pos]) continue;
        if (field[pos] == '#') continue;
        way[pos] = true;
        check.Push(pos - 1);
        check.Push(pos + 1);
        check.Push(pos - width);
        check.Push(pos + width);
      }

      return way;
    }
  }
}
