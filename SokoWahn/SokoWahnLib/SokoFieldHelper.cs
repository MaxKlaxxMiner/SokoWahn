// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
namespace SokoWahnLib
{
  /// <summary>
  /// Klasse mit Hilfsfunktionen
  /// </summary>
  public static class SokoFieldHelper
  {
    /// <summary>
    /// prüft, ob es ein gültiges Spielfeld-Zeichen ist
    /// </summary>
    /// <param name="c">Zeichen, welches geprüft werden soll</param>
    /// <returns>true, wenn es ein gültiges Spielfeld-Zeichen ist, sonst false</returns>
    public static bool ValidChar(char c)
    {
      switch (c)
      {
        case ' ':  // leeres Spielfeld
        case '#':  // Mauer
        case '.':  // Zielfeld für Kisten
        case '@':  // Spieler
        case '+':  // Spieler auf einem Zielfeld
        case '$':  // Kiste
        case '*':  // Kiste auf einem Zielfeld
        case '\r': // Linebreak: CR - Carry Return (Wagenrücklauf)
        case '\n': // Linebreak: LF - Line Feed (Zeilvorschub)
        return true;
        default: return false;
      }
    }

    /// <summary>
    /// filtert das Spielfeld-Zeichen und ersetzt ungültige Zeichen durch Leerzeichen
    /// </summary>
    /// <param name="c">Zeichen, welches gefiltert werden soll</param>
    /// <returns>gefiltertes Zeichen</returns>
    public static char FilterChar(char c)
    {
      return ValidChar(c) ? c : ' ';
    }

    /// <summary>
    /// leert das Zeichen eines Spielfeldes von Kisten und Spielern (andere Zeichen bleiben erhalten)
    /// </summary>
    /// <param name="c">Zeichen, welches ersetzt werden soll</param>
    /// <returns>ersetztes Zeichen</returns>
    public static char ClearChar(char c)
    {
      switch (c)
      {
        case '@': return ' '; // Spieler entfernen
        case '+': return '.'; // Spieler auf einem Zielfeld entfernen
        case '$': return ' '; // Kiste entfernen
        case '*': return '.'; // Kiste auf einem Zielfeld entfernen
        default: return c;    // andere Zeichen behalten
      }
    }
  }
}
