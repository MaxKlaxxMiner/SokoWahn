
#region # using *.*

using System.Collections.Generic;

#endregion

namespace SokoWahnCore
{
  /// <summary>
  /// Klasse zum scannen von blockierten Stellungen, wo sich keine Kisten aufhalten dürfen
  /// </summary>
  public sealed class DeadlockBlocker
  {
    /// <summary>
    /// merkt sich das aktuelle Spielfeld
    /// </summary>
    SokowahnField field;

    /// <summary>
    /// merkt sich die Wege-Felder, wo sich der Spieler aufhalten darf
    /// </summary>
    public readonly bool[] wayMap;

    /// <summary>
    /// merkt sich die direkt blockierten Spielfelder, wo sich keine Kisten aufhalten dürfen
    /// </summary>
    public readonly bool[] blockerSingle;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches betroffen ist</param>
    public DeadlockBlocker(SokowahnField field)
    {
      this.field = new SokowahnField(field);

      wayMap = SokoTools.CreateWayMap(field.fieldData, field.width, field.PlayerPos);

      blockerSingle = wayMap.SelectArray(b => !b);
    }
  }
}
