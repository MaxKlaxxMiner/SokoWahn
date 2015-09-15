using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// gibt die ermittelten Punkte einer Stellung zurück
  /// </summary>
  public struct SokowahnPunkte
  {
    /// <summary>
    /// gibt die absolute Mindestdauer an, welche das Spielfeld zum lösen noch benötigt (kürzere Varianten sind unmöglich)
    /// </summary>
    public int tiefeMin;
    /// <summary>
    /// maximale Lösungdauer, welche das Spielfeld zum lösen benötigt
    /// </summary>
    public int tiefeMax;

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return (new { tiefeMin, tiefeMax }).ToString();
    }

    /// <summary>
    /// liest die Punkte anhand von Stellungsdaten ein
    /// </summary>
    /// <param name="stellung">Stellungsdaten, welche eingelesen werden sollen</param>
    public SokowahnPunkte(ushort[] stellung)
    {
      tiefeMin = stellung[stellung.Length - 2];
      tiefeMax = stellung[stellung.Length - 1];
    }
  }
}
