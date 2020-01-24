using System;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Hashtable zum speichern von Prüsummen und Spieltiefen
  /// </summary>
  public abstract class HashCrc : IDisposable
  {
    /// <summary>
    /// fügt einen Wert in die Hashtable hinzu
    /// </summary>
    /// <param name="crc">Prüfsumme, welche gespeichert werden soll</param>
    /// <param name="value">Wert, welcher gesetzt werden soll</param>
    public abstract void Add(ulong crc, ulong value);

    /// <summary>
    /// aktualisiert einen bestehenden Wert in der Hashtable
    /// </summary>
    /// <param name="crc">Prüfsumme, welche verwendet werden soll</param>
    /// <param name="value">neuer Wert, welcher gesetzt werden soll</param>
    public abstract void Update(ulong crc, ulong value);

    /// <summary>
    /// gibt den entsprechenden Wert zurück oder "notFoundValue", wenn diese nicht gefunden wurde
    /// </summary>
    /// <param name="crc">Prüfsumme, welche abgefragt werden soll</param>
    /// <param name="notFoundValue">alternativer Rückgabewert, falls der Schlüssel nicht gefunden wurde</param>
    /// <returns>ausgelesener Wert oder "notFoundValue" wenn nicht gefunden</returns>
    public abstract ulong Get(ulong crc, ulong notFoundValue = 0);

    /// <summary>
    /// gibt die Anzahl der gespeicherten Einträge zurück
    /// </summary>
    public abstract ulong Count { get; }

    #region # // --- Dispose ---
    /// <summary>
    /// Destructor
    /// </summary>
    ~HashCrc()
    {
      Dispose();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public abstract void Dispose();
    #endregion
  }
}
