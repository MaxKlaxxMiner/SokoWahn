// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global
using System.Collections.Generic;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Struktur zum speichern einer Varianten-Kette
  /// </summary>
  public struct VariantSpan
  {
    /// <summary>
    /// erste Variante der Kette
    /// </summary>
    public readonly ulong variantStart;
    /// <summary>
    /// Anzahl der fortlaufenden Varianten
    /// </summary>
    public readonly ulong variantCount;

    /// <summary>
    /// zeigt auf das Ende der Kette
    /// </summary>
    public ulong VariantEnd { get { return variantStart + variantCount; } }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="variantStart">erste Variante der Kette</param>
    /// <param name="variantCount">Anzahl der fortlaufenden Varianten</param>
    public VariantSpan(ulong variantStart, ulong variantCount)
    {
      this.variantStart = variantStart;
      this.variantCount = variantCount;
    }

    /// <summary>
    /// gibt alle Varianten der Kette einzeln als Enumerable zurück
    /// </summary>
    public IEnumerable<ulong> AsEnumerable()
    {
      for (ulong i = 0; i < variantCount; i++) yield return variantStart + i;
    }
  }
}
