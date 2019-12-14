using SokoWahnLib.Rooms;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnWin
{
  /// <summary>
  /// Struktur eines aufleuchtenden Elementes
  /// </summary>
  public struct Highlight
  {
    /// <summary>
    /// Farbe des Elementes
    /// </summary>
    public readonly int color;
    /// <summary>
    /// Größe eines Feldes
    /// </summary>
    public readonly float size;
    /// <summary>
    /// zugehörige Spielfelder, welche aufleuchten sollen
    /// </summary>
    public readonly int[] fields;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="color">Farbe des Elementes</param>
    /// <param name="size">Größe eines Feldes</param>
    /// <param name="fields">zugehörige Spielfelder</param>
    public Highlight(int color, float size, int[] fields)
    {
      this.color = color;
      this.size = size;
      this.fields = fields;
    }

    /// <summary>
    /// berechnet die Prüfsumme des Inhaltes
    /// </summary>
    /// <param name="crc">CRC-Prüfsumme, welche zum berechnen verwendet werden soll</param>
    /// <returns>berechnete CRC-Prüfsumme</returns>
    public ulong Crc(ulong crc)
    {
      return crc.Crc64Update(color).Crc64Update(size).Crc64Update(fields);
    }
  }
}
