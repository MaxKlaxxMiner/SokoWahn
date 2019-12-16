#region # using *.*
using System;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  public class RoomPortal : IDisposable
  {
    /// <summary>
    /// Spielfeld, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// Quell-Raum, wo das Portal startet
    /// </summary>
    public readonly Room fromRoom;
    /// <summary>
    /// genaue Position, wo das Portal startet
    /// </summary>
    public readonly int fromPos;
    /// <summary>
    /// Ziel-Raum, wohin das Portal führt
    /// </summary>
    public readonly Room toRoom;
    /// <summary>
    /// genaue Position, wohin das Portal führt
    /// </summary>
    public readonly int toPos;
    /// <summary>
    /// das gegenüberliegende/zurückführende Portal
    /// </summary>
    public RoomPortal oppositePortal;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="fromRoom">Quell-Raum, wo das Portal startet</param>
    /// <param name="fromPos">genaue Position im Quell-Raum, wo das Portal startet</param>
    /// <param name="toRoom">Ziel-Raum, wohin das Portal führt</param>
    /// <param name="toPos">genaue Position im Ziel-Raum, wohin das Portal führt</param>
    public RoomPortal(Room fromRoom, int fromPos, Room toRoom, int toPos)
    {
      #region # // --- Parameter prüfen ---
      if (fromRoom == null) throw new ArgumentNullException("fromRoom");
      this.fromRoom = fromRoom;

      field = fromRoom.field;

      if (!field.ValidPos(fromPos) || !fromRoom.fieldPosis.Contains(fromPos)) throw new ArgumentOutOfRangeException("fromPos");
      this.fromPos = fromPos;

      if (toRoom == null) throw new ArgumentNullException("toRoom");
      this.toRoom = toRoom;

      if (fromRoom.field != toRoom.field) throw new ArgumentException("fromRoom.field != toRoom.field");

      if (!field.ValidPos(toPos) || !toRoom.fieldPosis.Contains(toPos)) throw new ArgumentOutOfRangeException("toPos");
      this.toPos = toPos;
      #endregion
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~RoomPortal()
    {
      Dispose();
    }
    #endregion

    #region # // --- Compare ---
    /// <summary>
    /// Hash-Code für einfachen Vergleich
    /// </summary>
    /// <returns>eindeutiger Hash-Code</returns>
    public override int GetHashCode()
    {
      return fromPos << 16 | toPos;
    }
    /// <summary>
    /// direkter Vergleich der Objekte
    /// </summary>
    /// <param name="obj">Objekt, welches verglichen werden soll</param>
    /// <returns>true, wenn Beide identisch sind</returns>
    public override bool Equals(object obj)
    {
      return ReferenceEquals(obj, this);
    }
    #endregion
  }
}
