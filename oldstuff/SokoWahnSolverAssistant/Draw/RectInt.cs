
namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// speichert ein Rechteck
  /// </summary>
  public struct RectInt
  {
    /// <summary>
    /// X-Position vom Rechteck
    /// </summary>
    public int x;

    /// <summary>
    /// Y-Position vom Rechteck
    /// </summary>
    public int y;

    /// <summary>
    /// Breite des Rechteckes
    /// </summary>
    public int w;

    /// <summary>
    /// Höhe des Rechteckes
    /// </summary>
    public int h;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="x">X-Position vom Rechteck</param>
    /// <param name="y">Y-Position vom Rechteck</param>
    /// <param name="w">Breite des Rechteckes</param>
    /// <param name="h">Höhe des Rechteckes</param>
    public RectInt(int x, int y, int w, int h)
    {
      this.x = x;
      this.y = y;
      this.w = w;
      this.h = h;
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="p">Position des Rechteckes</param>
    /// <param name="s">Größe des Rechteckes</param>
    public RectInt(PointInt p, SizeInt s)
    {
      x = p.x;
      y = p.y;
      w = s.w;
      h = s.h;
    }
  }
}
