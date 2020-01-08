// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse zum neu Mappen von zu überspringenden Werten
  /// </summary>
  public sealed class SkipMapper
  {
    /// <summary>
    /// merkt sich die gemappten Daten
    /// </summary>
    public readonly ulong[] map;
    /// <summary>
    /// gibt die Anzahl an, wieviele Werte weiterhin in Benutzung sind
    /// </summary>
    public readonly ulong usedCount;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="bits">Bits, welche die benutzen bzw. unbenutzen Werten markiert</param>
    /// <param name="usedBits">gibt an, dass die gesetzten Bits die Werte sind, welche erhalten bleiben</param>
    public SkipMapper(Bitter bits, bool usedBits = true)
    {
      map = new ulong[bits.Length];

      ulong pos = 0;
      ulong ofs = 0;
      if (usedBits)
      {
        while (pos < bits.Length)
        {
          // --- benutzte Werte mappen ---
          ulong c = bits.CountMarkedBits(pos);
          for (ulong i = 0; i < c; i++) map[pos++] = ofs++;

          // --- unbenutzte Werte markieren ---
          c = bits.CountFreeBits(pos);
          for (ulong i = 0; i < c; i++) map[pos++] = ulong.MaxValue;
        }
      }
      else
      {
        while (pos < bits.Length)
        {
          // --- benutzte Werte mappen ---
          ulong c = bits.CountFreeBits(pos);
          for (ulong i = 0; i < c; i++) map[pos++] = ofs++;

          // --- unbenutzte Werte markieren ---
          c = bits.CountMarkedBits(pos);
          for (ulong i = 0; i < c; i++) map[pos++] = ulong.MaxValue;
        }
      }
      usedCount = ofs;
    }
  }
}
