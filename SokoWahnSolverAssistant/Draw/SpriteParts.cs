
namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Teile der Bildelemente
  /// </summary>
  public enum SpriteParts
  {
    /// <summary>
    /// Ecke linksoben
    /// </summary>
    TopLeft = 1,

    /// <summary>
    /// Ecke rechtsoben
    /// </summary>
    TopRight = 2,

    /// <summary>
    /// Ecke linksunten
    /// </summary>
    BottomLeft = 4,

    /// <summary>
    /// Ecke rechtsunten
    /// </summary>
    BottomRight = 8,

    /// <summary>
    /// linke Seite
    /// </summary>
    Left = TopLeft | BottomLeft,

    /// <summary>
    /// rechts Seite
    /// </summary>
    Right = TopRight | BottomRight,

    /// <summary>
    /// obere Seite
    /// </summary>
    Top = TopLeft | TopRight,

    /// <summary>
    /// untere Seite
    /// </summary>
    Bottom = BottomLeft | BottomRight,

    /// <summary>
    /// alle Teile
    /// </summary>
    All = TopLeft | TopRight | BottomLeft | BottomRight
  }
}
