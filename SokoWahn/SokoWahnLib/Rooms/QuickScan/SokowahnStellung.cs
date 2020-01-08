namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Struktur einer kompletten Stellung (inkl. Zugtiefe und Crc64-Schlüssel)
  /// </summary>
  public struct SokowahnStellung
  {
    /// <summary>
    /// Spielerposition im Raum
    /// </summary>
    public int raumSpielerPos;

    /// <summary>
    /// Kistenpositionen im Raum
    /// </summary>
    public int[] kistenZuRaum;

    /// <summary>
    /// Zugtiefe, bei welcher diese Stellung erreicht wurde
    /// </summary>
    public int zugTiefe;

    /// <summary>
    /// Crc64-Schlüssel der gesammten Stellung
    /// </summary>
    public ulong crc64;

    /// <summary>
    /// gibt die Stellung als Array zurück (ushort-Typ)
    /// </summary>
    /// <param name="zielArray">Array in dem die Stellung direkt gespeichert werden soll</param>
    /// <param name="offset">Start-Position im Array</param>
    /// <returns>Anzahl der getätigten Einträge</returns>
    public void SpeichereStellung(ushort[] zielArray, int offset)
    {
      zielArray[offset++] = (ushort)raumSpielerPos;
      foreach (int kiste in kistenZuRaum) zielArray[offset++] = (ushort)kiste;
    }

    /// <summary>
    /// gibt die Stellung als Array zurück (byte-Typ)
    /// </summary>
    /// <param name="zielArray">Array in dem die Stellung direkt gespeichert werden soll</param>
    /// <param name="offset">Start-Position im Array</param>
    /// <returns>Anzahl der getätigten Einträge</returns>
    public void SpeichereStellung(byte[] zielArray, int offset)
    {
      zielArray[offset++] = (byte)raumSpielerPos;
      foreach (int kiste in kistenZuRaum) zielArray[offset++] = (byte)kiste;
    }

    /// <summary>
    /// gibt die eigene Stellung als lesbares Feld zurück
    /// </summary>
    /// <param name="tmpRaum">Sokowahn-Raum mit den jeweiligen Grunddaten</param>
    /// <returns>lesbares Spielfeld</returns>
    public string Debug(SokowahnRaum raum)
    {
      SokowahnRaum temp = new SokowahnRaum(raum);
      temp.KistenAnzahl = kistenZuRaum.Length;
      return temp.Debug(this);
    }

    /// <summary>
    /// gibt die eigene Stellung als lesbares Feld zurück
    /// </summary>
    /// <param name="tmpRaum">Sokowahn-Raum mit den jeweiligen Grunddaten</param>
    /// <returns>lesbares Spielfeld</returns>
    public string ToString(SokowahnRaum raum)
    {
      return Debug(raum);
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichen aus
    /// </summary>
    /// <returns>Zeichenkette, welche ausgegeben werden soll</returns>
    public override string ToString()
    {
      return "[" + zugTiefe + "] - (" + raumSpielerPos + ") " + string.Join(", ", kistenZuRaum) + " - " + crc64;
    }
  }
}
