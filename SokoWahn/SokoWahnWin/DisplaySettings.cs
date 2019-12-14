using System.Linq;
using SokoWahnLib;
using SokoWahnLib.Rooms;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable LoopCanBeConvertedToQuery

namespace SokoWahnWin
{
  /// <summary>
  /// Klasse zum merken der Display-Einstellungen
  /// </summary>
  public class DisplaySettings
  {
    /// <summary>
    /// merkt sich die Spielerposition auf dem Spielfeld (-1 = wird nicht angezeigt)
    /// </summary>
    public int playerPos;

    /// <summary>
    /// merkt sich die Positionen der anzuzeigenden Kisten
    /// </summary>
    public int[] boxes;

    /// <summary>
    /// aufleuchtende Farben im Vordergrund
    /// </summary>
    public Highlight[] hFront;
    /// <summary>
    /// aufleuchtende Farbe im Hintergrund
    /// </summary>
    public Highlight[] hBack;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public DisplaySettings()
    {
      playerPos = -1;
      boxes = new int[0];
      hFront = new Highlight[0];
      hBack = new Highlight[0];
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Standard-Spielfeld, welches gezeichnet werden soll</param>
    public DisplaySettings(ISokoField field)
    {
      playerPos = field.PlayerPos;
      boxes = Enumerable.Range(0, field.Width * field.Height).Where(field.IsBox).ToArray();
      hFront = new Highlight[0];
      hBack = new Highlight[0];
    }

    /// <summary>
    /// erstellt eine Kopie der Einstellungen und gibt diese zurück
    /// </summary>
    /// <returns>erstellte Kopie</returns>
    public DisplaySettings Clone()
    {
      return new DisplaySettings
      {
        playerPos = playerPos,
        boxes = boxes.ToArray(),
        hFront = hFront.ToArray(),
        hBack = hBack.ToArray()
      };
    }

    /// <summary>
    /// berechnet die CRC-Prüfsumme für Vordergrund-Änderungen der Anzeige
    /// </summary>
    /// <returns>CRC-Prüfsumme</returns>
    public ulong CrcFront()
    {
      ulong crc = Crc64.Start
        .Crc64Update(playerPos)
        .Crc64Update(boxes);

      foreach (var h in hFront) crc = h.Crc(crc);

      return crc;
    }

    /// <summary>
    /// berechnet die CRC-Prüfsumme für Hintergund-Änderungen der Anzeige
    /// </summary>
    /// <returns>CRC-Prüfsumme</returns>
    public ulong CrcBack()
    {
      ulong crc = Crc64.Start;

      foreach (var h in hBack) crc = h.Crc(crc);

      return crc;
    }
  }
}
