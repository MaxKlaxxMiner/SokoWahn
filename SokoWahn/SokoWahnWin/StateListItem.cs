// ReSharper disable MemberCanBePrivate.Global

namespace SokoWahnWin
{
  /// <summary>
  /// Item-Element für die Zustand-Liste
  /// </summary>
  public struct StateListItem
  {
    /// <summary>
    /// Raum-Index, wohin der Zustand gehört
    /// </summary>
    public readonly int roomIndex;
    /// <summary>
    /// Zustand-ID des Raumes
    /// </summary>
    public readonly ulong state;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomIndex">Raum-Index, wohin der Zustand gehört</param>
    /// <param name="state">Zustand-ID des Raumes</param>
    public StateListItem(int roomIndex, ulong state)
    {
      this.roomIndex = roomIndex;
      this.state = state;
    }

    /// <summary>
    /// gibt den lesbaren Inhalt für die Listbox zurück
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      if (state == 0) return "State finish";
      return "State " + state;
    }
  }
}
