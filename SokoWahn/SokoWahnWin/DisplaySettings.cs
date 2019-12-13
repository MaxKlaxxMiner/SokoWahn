using SokoWahnLib;
// ReSharper disable MemberCanBePrivate.Global

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
    public int playerPos = -1;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public DisplaySettings()
    {
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Standard-Spielfeld, welches gezeichnet werden soll</param>
    public DisplaySettings(ISokoField field)
    {
      playerPos = field.PlayerPos;
    }

    /// <summary>
    /// erstellt eine Kopie der Einstellungen und gibt diese zurück
    /// </summary>
    /// <returns>erstellte Kopie</returns>
    public DisplaySettings Clone()
    {
      return new DisplaySettings
      {
        playerPos = playerPos
      };
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new
      {
        playerPos
      }.ToString();
    }
  }
}
