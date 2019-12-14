#region # using *.*
using System;
using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Raum (Teil von RoomNetwork)
  /// </summary>
  public class Room : IDisposable
  {
    /// <summary>
    /// Spielfeld, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// Positionen der Spielfelder, welche dem Raum zugeordnet sind
    /// </summary>
    public readonly int[] fieldPosis;
    /// <summary>
    /// eingehende Portale, welche zum eigenen Raum gehören
    /// </summary>
    public readonly RoomPortal[] incomingPortals;
    /// <summary>
    /// ausgehende Portale, welche zu anderen Räumen gehören
    /// </summary>
    public readonly RoomPortal[] outgoingPortals;
    /// <summary>
    /// Liste mit allen Zuständen, welche der Raum annehmen kann
    /// </summary>
    public readonly StateList stateList;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    /// <param name="fieldPosis">Positionen der Spielfelder, welche dem Raum zugeordnet werden</param>
    /// <param name="incomingPortals">eingehende Portale, welche zum eigenen Raum gehören</param>
    /// <param name="outgoingPortals">ausgehende Portale, welche zu anderen Räumen gehören</param>
    public Room(ISokoField field, int[] fieldPosis, RoomPortal[] incomingPortals, RoomPortal[] outgoingPortals)
    {
      #region # // --- Parameter prüfen ---
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      if (fieldPosis == null || fieldPosis.Length == 0) throw new ArgumentNullException("fieldPosis");
      var walkPosis = field.GetWalkPosis();
      if (fieldPosis.Any(pos => !walkPosis.Contains(pos))) throw new ArgumentOutOfRangeException("fieldPosis");
      this.fieldPosis = fieldPosis;

      if (incomingPortals == null || incomingPortals.Length == 0) throw new ArgumentNullException("incomingPortals");
      this.incomingPortals = incomingPortals;
      if (outgoingPortals == null || outgoingPortals.Length == 0) throw new ArgumentNullException("outgoingPortals");
      this.outgoingPortals = outgoingPortals;
      if (incomingPortals.Length != outgoingPortals.Length) throw new ArgumentException("iPortals.Length != oPortals.Length");
      #endregion

      stateList = new StateListNormal();
    }
    #endregion

    #region # // --- States ---
    /// <summary>
    /// erstellt die ersten Zustände
    /// </summary>
    public void InitStates()
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(stateList.Count == 0);
      //Debug.Assert( todo: check variant lists

      int pos = fieldPosis.First();

      switch (field.GetField(pos))
      {
        //        case '@': // Spieler auf einem leeren Feld
        //        {
        //          AddPlayerState(pos, 0, 0, false);   // | Start | Ende | Spieler auf einem Feld
        //          AddBoxState(0, 0, false);           // |     - | Ende | leeres Feld
        //          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
        //          {
        //            AddBoxState(1, 0, true);          // |     - |    - | Kiste auf einem Feld
        //          }
        //        } break;

        //        case '+': // Spieler auf einem Zielfeld
        //        {
        //          AddPlayerState(pos, 0, 0, false);   // | Start |    - | Spieler auf einem Zielfeld
        //          AddBoxState(0, 0, false);           // |     - |    - | leeres Zielfeld
        //          AddBoxState(1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
        //        } break;

        //        case ' ': // leeres Feld
        //        {
        //          AddBoxState(0, 0, false);           // | Start | Ende | leeres Feld
        //          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
        //          {
        //            AddPlayerState(pos, 0, 0, false); // |     - | Ende | Spieler auf einem Feld
        //            AddBoxState(1, 0, true);          // |     - |    - | Kiste auf einem Feld
        //          }
        //        } break;

        //        case '.': // Zielfeld
        //        {
        //          AddBoxState(0, 0, false);           // | Start |    - | leeres Zielfeld
        //          AddBoxState(1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
        //          if (!field.CheckCorner(pos)) // Spieler kann sich nur auf dem Feld befinden, wenn die Kiste rausgeschoben wurde (was bei einer Ecke nicht möglich ist)
        //          {
        //            AddPlayerState(pos, 0, 0, false); // |     - |    - | Spieler auf einem Zielfeld
        //          }
        //        } break;

        //        case '$': // Feld mit Kiste
        //        {
        //          AddBoxState(1, 0, true);            // | Start |    - | Kiste auf einem Feld
        //          AddBoxState(0, 0, false);           // |     - | Ende | leeres Feld
        //          AddPlayerState(pos, 0, 0, false);   // |     - | Ende | Spieler auf einem leeren Feld
        //          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
        //        } break;

        //        case '*': // Kiste auf einem Zielfeld
        //        {
        //          AddBoxState(1, 1, true);            // | Start | Ende | Kiste auf einem Zielfeld
        //          if (!field.CheckCorner(pos)) // Kiste kann weggeschoben werden?
        //          {
        //            AddBoxState(0, 0, false);         // |     - |    - | leeres Feld
        //            AddPlayerState(pos, 0, 0, false); // |     - |    - | Spieler auf einem leeren Feld
        //          }
        //        } break;

        default: throw new NotSupportedException("char: " + field.GetField(pos));
      }
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      // --- nur die eingehenden Portale auflösen, welche zum eigenen Raum gehören ---
      if (incomingPortals != null)
      {
        for (int p = 0; p < incomingPortals.Length; p++)
        {
          if (incomingPortals[p] != null) incomingPortals[p].Dispose();
          incomingPortals[p] = null;
        }
      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~Room()
    {
      Dispose();
    }
    #endregion
  }
}
