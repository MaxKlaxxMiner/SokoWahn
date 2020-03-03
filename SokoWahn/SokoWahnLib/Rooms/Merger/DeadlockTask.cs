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
    /// gibt an, ob der Spieler zusammen mit einer Kiste durch das Portal gelaufen ist
    /// </summary>
    public readonly bool leaveWithBox;

    /// <summary>
    /// Kistenzustand beim verlassen des Raumes
    /// </summary>
    public readonly ulong state;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="oPortalIndexPlayer">Portalnummer, worüber der Spieler den Raum verlassen hat</param>
    /// <param name="leaveWithBox">gibt an, ob der Spieler zusammen mit einer Kiste durch das Portal gelaufen ist</param>
    /// <param name="state">Kistenzustand beim verlassen des Raumes</param>
    public DeadlockTask(uint oPortalIndexPlayer, bool leaveWithBox, ulong state)
    {
      this.oPortalIndexPlayer = oPortalIndexPlayer;
      this.leaveWithBox = leaveWithBox;
      this.state = state;
    }
  }
}
