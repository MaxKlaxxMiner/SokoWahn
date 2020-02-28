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
    /// merkt sich, ob es sich um den Startzustand handelt
    /// </summary>
    readonly bool startState;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomIndex">Raum-Index, wohin der Zustand gehört</param>
    /// <param name="state">Zustand-ID des Raumes</param>
    /// <param name="startState">gibt an, ob es sich um den Startzustand handelt</param>
    public StateListItem(int roomIndex, ulong state, bool startState)
    {
      this.roomIndex = roomIndex;
      this.state = state;
      this.startState = startState;
    }

    /// <summary>
    /// gibt den lesbaren Inhalt für die Listbox zurück
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      if (state == 0) return "State finish";
      if (startState) return "State " + state + " (start)";
      return "State " + state;
    }
  }
}
