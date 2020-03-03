// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Aufgabe zum Suchen nach unlösbaren Varianten
  /// </summary>
  public struct DeadlockTask
  {
    /// <summary>
    /// Portalnummer, worüber der Spieler den Raum verlassen hat
    /// </summary>
    public readonly uint oPortalIndexPlayer;

    /// <summary>
    /// gibt an, ob der Spieler eine Kiste aus den Raum geschoben hat
    /// </summary>
    public readonly bool exportedBox;

    /// <summary>
    /// Kistenzustand beim verlassen des Raumes
    /// </summary>
    public readonly ulong state;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="oPortalIndexPlayer">Portalnummer, worüber der Spieler den Raum verlassen hat</param>
    /// <param name="exportedBox">gibt an, ob der Spieler eine Kiste aus den Raum geschoben hat</param>
    /// <param name="state">Kistenzustand beim verlassen des Raumes</param>
    public DeadlockTask(uint oPortalIndexPlayer, bool exportedBox, ulong state)
    {
      this.oPortalIndexPlayer = oPortalIndexPlayer;
      this.exportedBox = exportedBox;
      this.state = state;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { oPortalIndexPlayer, exportBox = exportedBox, state }.ToString();
    }
  }
}
