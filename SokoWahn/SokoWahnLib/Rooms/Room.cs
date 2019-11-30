#region # using *.*
using System;
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
    public readonly uint[] fieldPosis;
    /// <summary>
    /// eingehende Portale, welche zum eigenen Raum gehören
    /// </summary>
    public readonly RoomPortal[] inPortals;
    /// <summary>
    /// ausgehende Portale, welche zu anderen Räumen gehören
    /// </summary>
    public readonly RoomPortal[] outPortals;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    /// <param name="fieldPosis">Positionen der Spielfelder, welche dem Raum zugeordnet werden</param>
    /// <param name="inPortals">eingehende Portale, welche zum eigenen Raum gehören</param>
    /// <param name="outPortals">ausgehende Portale, welche zu anderen Räumen gehören</param>
    public Room(ISokoField field, uint[] fieldPosis, RoomPortal[] inPortals, RoomPortal[] outPortals)
    {
      #region # // --- Parameter prüfen ---
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      if (fieldPosis == null || fieldPosis.Length == 0) throw new ArgumentNullException("fieldPosis");
      var walkPosis = field.GetWalkPosis();
      if (fieldPosis.Any(pos => !walkPosis.Contains((int)pos))) throw new ArgumentOutOfRangeException("fieldPosis");
      this.fieldPosis = fieldPosis;

      if (inPortals == null || inPortals.Length == 0) throw new ArgumentNullException("inPortals");
      this.inPortals = inPortals;
      if (outPortals == null || outPortals.Length == 0) throw new ArgumentNullException("outPortals");
      this.outPortals = outPortals;
      if (inPortals.Length != outPortals.Length) throw new ArgumentException("inPortals.Length != outPortals.Length");
      #endregion
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (inPortals != null)
      {
        for (int p = 0; p < inPortals.Length; p++)
        {
          if (inPortals[p] != null) inPortals[p].Dispose();
          inPortals[p] = null;
        }
      }
    }
    #endregion
  }
}
