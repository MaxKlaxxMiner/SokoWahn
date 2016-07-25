
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Local

#endregion

namespace SokoWahnCore
{
  public static partial class SokoTools
  {
    /// <summary>
    /// berechnet die Entfernung der am weitesten entfernten Kisten
    /// </summary>
    /// <param name="boxes">Array mit den Kisten-Position, welche geprüft werden sollen</param>
    /// <param name="offset">Startposition im Array</param>
    /// <param name="count">Anzahl der prüfenden Kisten</param>
    /// <param name="fieldWidth">Breite des Spielfeldes (Länge einer Zeile)</param>
    /// <returns>Entfernung der weitesten entfernten Kisten</returns>
    public static int MaxBoxDistance(int[] boxes, int offset, int count, int fieldWidth)
    {
      if (count <= 1) return 0;

      int minX = int.MaxValue;
      int maxX = int.MinValue;
      int minY = int.MaxValue;
      int maxY = int.MinValue;

      for (int i = 0; i < count; i++)
      {
        int box = boxes[offset + i];
        int x = box % fieldWidth;
        int y = box / fieldWidth;
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
      }

      return Math.Max(maxX - minX, maxY - minY);
    }
  }
}
