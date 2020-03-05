// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnWin
{
  /// <summary>
  /// Item-Element für die Varianten-Liste
  /// </summary>
  public sealed class VariantListItem
  {
    /// <summary>
    /// merkt sich die Textzeile für die Listbox
    /// </summary>
    public readonly string txt;

    /// <summary>
    /// merkt sich den nächsten Kistenzustand, welcher durch diese Variante erreicht werden kann
    /// </summary>
    public readonly ulong nextState;

    /// <summary>
    /// Pfad der Varianten
    /// </summary>
    public readonly VariantPathElement[] variantPath;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txt">Listbox-Zeile, welche angezeigt wird</param>
    /// <param name="nextState">nächster Kistenzustand, welcher durch diese Variante erreicht werden kann</param>
    /// <param name="variantPath">vollständiger Pfad in Moves</param>
    public VariantListItem(string txt, ulong nextState, VariantPathElement[] variantPath)
    {
      this.txt = txt;
      this.nextState = nextState;
      this.variantPath = variantPath;
    }

    /// <summary>
    /// gibt den Textinhalt für die Listbox aus
    /// </summary>
    /// <returns>Zeile, welche angezeigt werden soll</returns>
    public override string ToString()
    {
      return txt;
    }
  }
}
