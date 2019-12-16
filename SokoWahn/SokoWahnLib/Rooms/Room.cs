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

      stateList = new StateListNormal(fieldPosis, fieldPosis.Where(field.IsGoal).ToArray());
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
        case '@': // Spieler auf einem leeren Feld
        case ' ': // leeres Feld
        {
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          if (!field.CheckCorner(pos)) stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen (sofern diese nicht in einer Ecke steht)
        } break;

        case '+': // Spieler auf einem Zielfeld
        case '.': // Zielfeld
        {
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
          stateList.Add(new int[0]); // Zwischen-Zustand: leeres Zielfeld
        } break;

        case '$': // Feld mit Kiste
        {
          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen
        } break;

        case '*': // Kiste auf einem Zielfeld
        {
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
          if (!field.CheckCorner(pos)) stateList.Add(new int[0]); // Zustand ohne Kiste hinzufügen (nur wenn die Kiste herausgeschoben werden kann)
        } break;

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
