// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Struktur einer kompletten Stellung (inkl. Zugtiefe und Crc64-Schlüssel)
  /// </summary>
  public struct SokowahnPosition
  {
    /// <summary>
    /// Spielerposition im Raum
    /// </summary>
    public int roomPlayerPos;

    /// <summary>
    /// Kistenpositionen im Raum
    /// </summary>
    public int[] boxesToRoom;

    /// <summary>
    /// Zugtiefe, bei welcher diese Stellung erreicht wurde
    /// </summary>
    public int calcDepth;

    /// <summary>
    /// Crc64-Schlüssel der gesammten Stellung
    /// </summary>
    public ulong crc64;

    /// <summary>
    /// gibt die Stellung als Array zurück (ushort-Typ)
    /// </summary>
    /// <param name="dst">Array in dem die Stellung direkt gespeichert werden soll</param>
    /// <param name="offset">Start-Position im Array</param>
    /// <returns>Anzahl der getätigten Einträge</returns>
    public void SavePosition(ushort[] dst, int offset)
    {
      dst[offset++] = (ushort)roomPlayerPos;
      foreach (int box in boxesToRoom) dst[offset++] = (ushort)box;
    }

    /// <summary>
    /// gibt die Stellung als Array zurück (byte-Typ)
    /// </summary>
    /// <param name="dst">Array in dem die Stellung direkt gespeichert werden soll</param>
    /// <param name="offset">Start-Position im Array</param>
    /// <returns>Anzahl der getätigten Einträge</returns>
    public void SavePosition(byte[] dst, int offset)
    {
      dst[offset++] = (byte)roomPlayerPos;
      foreach (int box in boxesToRoom) dst[offset++] = (byte)box;
    }

    /// <summary>
    /// gibt die eigene Stellung als lesbares Feld zurück
    /// </summary>
    /// <param name="field">Sokowahn-Raum mit den jeweiligen Grunddaten</param>
    /// <returns>lesbares Spielfeld</returns>
    public string Debug(SokoFieldQuickScan field)
    {
      var temp = new SokoFieldQuickScan(field)
      {
        BoxesCount = boxesToRoom.Length
      };
      return temp.Debug(this);
    }

    /// <summary>
    /// gibt die eigene Stellung als lesbares Feld zurück
    /// </summary>
    /// <param name="field">Sokowahn-Raum mit den jeweiligen Grunddaten</param>
    /// <returns>lesbares Spielfeld</returns>
    public string ToString(SokoFieldQuickScan field)
    {
      return Debug(field);
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichen aus
    /// </summary>
    /// <returns>Zeichenkette, welche ausgegeben werden soll</returns>
    public override string ToString()
    {
      return "[" + calcDepth + "] - (" + roomPlayerPos + ") " + string.Join(", ", boxesToRoom) + " - " + crc64;
    }
  }
}
