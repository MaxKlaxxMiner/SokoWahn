
namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse, welche eine Liste mit Raum-Zuständen speichern kann
  /// </summary>
  public abstract class StateList
  {
    /// <summary>
    /// gibt die Anzahl der gespeicherten Zustände zurück
    /// </summary>
    public abstract long Count { get; }
  }
}
