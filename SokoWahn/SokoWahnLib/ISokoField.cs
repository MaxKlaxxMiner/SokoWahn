#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// allgemeines Interface für ein Spielfeld
  /// </summary>
  public interface ISokoField
  {
    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    int Width { get; }
    /// <summary>
    /// gibt die Höhe des Spielfeldes zurück
    /// </summary>
    int Height { get; }
    /// <summary>
    /// gibt die aktuelle Spielerposition zurück (pos: x + y * Width)
    /// </summary>
    int PlayerPos { get; }
    /// <summary>
    /// gibt den Inhalt des Spielfeldes an einer bestimmten Position zurück
    /// </summary>
    /// <param name="pos">Position des Spielfeldes, welches abgefragt werden soll (pos: x + y * Width)</param>
    /// <returns>Inhalt des Spielfeldes</returns>
    char GetField(int pos);
    /// <summary>
    /// lässt den Spieler (ungeprüft) einen Spielzug durchführen
    /// </summary>
    /// <param name="move">Spielzug, welcher durchgeführt werden soll</param>
    void Move(MoveType move);
  }
}
