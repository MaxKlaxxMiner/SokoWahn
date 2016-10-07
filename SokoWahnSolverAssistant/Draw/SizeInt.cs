
namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// 2D-Größe mit Integer-Angaben
  /// </summary>
  public struct SizeInt
  {
    /// <summary>
    /// Breite
    /// </summary>
    public int w;

    /// <summary>
    /// Höhe
    /// </summary>
    public int h;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="w">Breite</param>
    /// <param name="h">Höhe</param>
    public SizeInt(int w, int h)
    {
      this.w = w;
      this.h = h;
    }
  }
}
