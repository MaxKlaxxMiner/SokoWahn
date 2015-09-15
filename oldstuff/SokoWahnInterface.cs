using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sokosolver
{
  /// <summary>
  /// Interface für ein SokoBan-Lösungsungsystem
  /// </summary>
  public interface SokoWahnInterface
  {
    /// <summary>
    /// berechnet den nächsten Schritt
    /// </summary>
    /// <param name="limit">Anzahl der zu berechnenden (kleinen) Arbeitsschritte, negativer Wert = optionale Vorbereitungsschritte)</param>
    /// <returns>gibt an, ob noch weitere Berechnungen anstehen</returns>
    bool Next(int limit);

    /// <summary>
    /// entfernt unnötige Einträge aus der Hashtabelle (sofern möglich)
    /// </summary>
    /// <returns>Anzahl der Einträge, welche entfernt werden konnten</returns>
    long Refresh();

    /// <summary>
    /// gibt den kompletten Lösungweg des Spiels zurück (nur sinnvoll, nachdem die Methode Next() ein false zurück geliefert hat = was "fertig" bedeutet)
    /// </summary>
    /// <returns>Lösungsweg als einzelne Spielfelder</returns>
    IEnumerable<string> GetLösungsweg();

    /// <summary>
    /// gibt die aktuelle Suchtiefe zurück
    /// </summary>
    int SuchTiefe { get; }

    /// <summary>
    /// gibt die Anzahl der bekannten Stellungen zurück
    /// </summary>
    long KnotenAnzahl { get; }

    /// <summary>
    /// gibt die Anzahl der noch zu prüfenden Knoten zurück
    /// </summary>
    long KnotenRest { get; }

    /// <summary>
    /// gibt das gesamte Spielfeld als lesbaren (genormten) Inhalt aus (Format siehe: <see cref="http://de.wikipedia.org/wiki/Sokoban#Levelnotation">Wikipedia</see> )
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    string ToString();
  }
}
