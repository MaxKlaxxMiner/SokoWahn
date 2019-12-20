// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnWin
{
  /// <summary>
  /// Item-Element für die Varianten-Liste
  /// </summary>
  public struct VariantListItem
  {
    /// <summary>
    /// merkt sich die Textzeile für die Listbox
    /// </summary>
    public readonly string txt;

    /// <summary>
    /// Pfad der Varianten
    /// </summary>
    public readonly VariantPathElement[] variantPath;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="txt">Listbox-Zeile, welche angezeigt wird</param>
    /// <param name="variantPath">vollständiger Pfad in Moves</param>
    public VariantListItem(string txt, VariantPathElement[] variantPath)
    {
      this.txt = txt;
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
