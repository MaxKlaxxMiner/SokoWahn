// ReSharper disable UnusedMember.Global
using System.Collections.Generic;
using System.Diagnostics;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Implementierung einer Hash-Tabelle
  /// </summary>
  public sealed class HashCrcNormal : HashCrc
  {
    /// <summary>
    /// merkt sich die Daten der Hashtable
    /// </summary>
    readonly Dictionary<ulong, ulong> data;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public HashCrcNormal()
    {
      data = new Dictionary<ulong, ulong>();
    }

    /// <summary>
    /// fügt einen Wert in die Hashtable hinzu
    /// </summary>
    /// <param name="crc">Prüfsumme, welche gespeichert werden soll</param>
    /// <param name="value">Wert, welcher gesetzt werden soll</param>
    public override void Add(ulong crc, ulong value)
    {
      Debug.Assert(!data.ContainsKey(crc));
      data.Add(crc, value);
    }

    /// <summary>
    /// aktualisiert einen bestehenden Wert in der Hashtable
    /// </summary>
    /// <param name="crc">Prüfsumme, welche verwendet werden soll</param>
    /// <param name="value">neuer Wert, welcher gesetzt werden soll</param>
    public override void Update(ulong crc, ulong value)
    {
      Debug.Assert(data.ContainsKey(crc));
      data[crc] = value;
    }

    /// <summary>
    /// gibt den entsprechenden Wert zurück oder "notFoundValue", wenn diese nicht gefunden wurde
    /// </summary>
    /// <param name="crc">Prüfsumme, welche abgefragt werden soll</param>
    /// <param name="notFoundValue">alternativer Rückgabewert, falls der Schlüssel nicht gefunden wurde</param>
    /// <returns>ausgelesener Wert oder "notFoundValue" wenn nicht gefunden</returns>
    public override ulong Get(ulong crc, ulong notFoundValue = 0)
    {
      ulong val;
      return data.TryGetValue(crc, out val) ? val : notFoundValue;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Einträge zurück
    /// </summary>
    public override ulong Count { get { return (uint)data.Count; } }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose() { }
  }
}
