
namespace JSoko.Optimizer_
{
  /// <summary>
  /// Constants that represent the method used for optimizing a solution.
  /// </summary>
  public enum OptimizationMethod
  {
    /// <summary>
    /// Optimize for moves, then for pushes as a tie-break.
    /// </summary>
    MovesPushes,

    /// <summary>
    /// Optimize for pushes, then for moves as a tie-break.
    /// </summary>
    PushesMoves,

    /// <summary>
    /// Optimize for pushes, then for moves and then for the secondary metrics.
    /// </summary>
    PushesMovesBoxlinesBoxchangesPushingsessions,

    /// <summary>
    /// Optimize for moves, then for pushes and then for the secondary metrics,
    /// </summary>
    MovesPushesBoxlinesBoxchangesPushingsessions,

    /// <summary>
    /// Optimize for box lines.
    /// </summary>
    Boxlines,

    /// <summary>
    /// Optimize for box changes.
    /// </summary>
    Boxchanges,

    /// <summary>
    /// DEBUG: internal use: find good vicinity settings.
    /// </summary>
    FindVicinitySettings
  }
}
