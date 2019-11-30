#region # using *.*
using System;
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Ein auf Räume basierende Netzwerk
  /// </summary>
  public class RoomNetwork: IDisposable
  {
    /// <summary>
    /// Spielfeld, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    public RoomNetwork(ISokoField field)
    {
      if (field == null) throw new ArgumentNullException("field");
      this.field = field;
    }

    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
    }
  }
}
