
namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// 2D-Position mit Integer-Angaben
  /// </summary>
  public struct PointInt
  {
    /// <summary>
    /// X-Position (von links nach rechts)
    /// </summary>
    public int x;

    /// <summary>
    /// Y-Position (von oben nach unten)
    /// </summary>
    public int y;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="x">X-Position (von links nach rechts)</param>
    /// <param name="y">Y-Position (von oben nach unten)</param>
    public PointInt(int x, int y)
    {
      this.x = x;
      this.y = y;
    }
  }
}
