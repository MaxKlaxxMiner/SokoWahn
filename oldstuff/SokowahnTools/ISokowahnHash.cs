#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// Funktionen, welche eine SokowahnHash-Klasse bereit stellen muss
  /// </summary>
  public interface ISokowahnHash
  {
    /// <summary>
    /// erstellt einen neuen Hash-Eintrag (darf noch nicht vorhanden sein)
    /// </summary>
    /// <param name="code">Hash-Code, welcher eintragen werden soll</param>
    /// <param name="tiefe">entsprechende Zugtiefe</param>
    void Add(ulong code, int tiefe);

    /// <summary>
    /// aktualisiert einen Hash-Eintrag (muss vorhanden sein)
    /// </summary>
    /// <param name="code">Code, welcher bearbeitet werden soll</param>
    /// <param name="tiefe">neu zu setzende Zugtiefe</param>
    void Update(ulong code, int tiefe);

    /// <summary>
    /// entfernt wieder einen bestimmten Schlüssel
    /// </summary>
    /// <param name="key">Schlüssel, welcher entfernt werden soll</param>
    void Remove(ulong key);

    /// <summary>
    /// gibt die Zugtiefe eines Hasheintrages zurück (oder 65535, wenn nicht gefunden)
    /// </summary>
    /// <param name="code">Hash-Code, welcher gesucht wird</param>
    /// <returns>entsprechende Zutiefe oder 65535, wenn nicht vorhanden</returns>
    int Get(ulong code);

    /// <summary>
    /// gibt alle gespeidcherten Hasheinträge zurück
    /// </summary>
    /// <returns>Enumerable mit allen Hasheinträgen</returns>
    IEnumerable<KeyValuePair<ulong, ushort>> GetAll();

    /// <summary>
    /// gibt die Anzahl der Hash-Einträge zurück
    /// </summary>
    long HashAnzahl { get; }
  }
}
