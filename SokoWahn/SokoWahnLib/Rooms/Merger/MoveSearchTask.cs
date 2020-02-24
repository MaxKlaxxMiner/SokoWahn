
namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Suchaufgabe für reine Laufwege
  /// </summary>
  public struct MoveSearchTask
  {
    /// <summary>
    /// ausgehendes Portal
    /// </summary>
    public uint oPortalIndexPlayer;

    /// <summary>
    /// Anzahl der Laufschritte
    /// </summary>
    public ulong moves;
    /// <summary>
    /// zurückgelegter Pfad
    /// </summary>
    public string path;

    /// <summary>
    /// gibt die Prüfsumme der Aufgabe zurück (um Doppler zu filtern)
    /// </summary>
    /// <returns></returns>
    public ulong GetCrc()
    {
      return Crc64.Start.Crc64Update(oPortalIndexPlayer);
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return path;
    }
  }
}
