
namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Typen der Bildelemente
  /// </summary>
  public enum SpriteType
  {
    /// <summary>
    /// leeres Feld
    /// </summary>
    Empty = 0,

    /// <summary>
    /// leeres Zielfeld
    /// </summary>
    EmptyFinish = 7,

    /// <summary>
    /// Spieler
    /// </summary>
    Player = 1,

    /// <summary>
    /// Kiste
    /// </summary>
    Box = 2,

    /// <summary>
    /// Kiste auf einem Zielfeld
    /// </summary>
    BoxFinish = 9,

    /// <summary>
    /// innere Ecken
    /// </summary>
    WallCorner = 14,

    /// <summary>
    /// waagerechte Wände
    /// </summary>
    WallHorizon = 15,

    /// <summary>
    /// ausgefüllte Wände
    /// </summary>
    WallFill = 16,

    /// <summary>
    /// senkrechte Wände
    /// </summary>
    WallVertical = 21,

    /// <summary>
    /// äußere Kanten
    /// </summary>
    WallEdge = 22
  };
}
