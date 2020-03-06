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
    /// Portalnummer, worüber der Spieler den Raum verlassen bzw. betreten hat
    /// </summary>
    public readonly uint portalIndexPlayer;

    /// <summary>
    /// gibt an, ob der Spieler eine Kiste aus dem Raum geschoben bzw. zurück gezogen hat
    /// </summary>
    public readonly bool exportedBox;

    /// <summary>
    /// Kistenzustand beim Verlassen des Raumes, bzw. beim Betreten des Raumes
    /// </summary>
    public readonly ulong state;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="portalIndexPlayer">Portalnummer, worüber der Spieler den Raum verlassen bzw. betreten hat</param>
    /// <param name="exportedBox">gibt an, ob der Spieler eine Kiste aus dem Raum geschoben bzw. zurück gezogen hat</param>
    /// <param name="state">Kistenzustand beim Verlassen des Raumes, bzw. beim Betreten des Raumes</param>
    public DeadlockTask(uint portalIndexPlayer, bool exportedBox, ulong state)
    {
      this.portalIndexPlayer = portalIndexPlayer;
      this.exportedBox = exportedBox;
      this.state = state;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { portalIndexPlayer, exportedBox, state }.ToString();
    }
  }
}
