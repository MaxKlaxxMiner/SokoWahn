// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnWin
{
  /// <summary>
  /// ein einzelnes Element innerhalb einer Variante
  /// </summary>
  public struct VariantPathElement
  {
    /// <summary>
    /// merkt sich die aktuelle Spielerposition
    /// </summary>
    public readonly int playerPos;
    /// <summary>
    /// merkt sich die aktuellen Kisten
    /// </summary>
    public readonly int[] boxes;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="playerPos">Spieler-Position</param>
    /// <param name="boxes">Kisten, welche zur Variante gehören</param>
    public VariantPathElement(int playerPos, int[] boxes)
    {
      this.playerPos = playerPos;
      this.boxes = boxes;
    }
  }
}
