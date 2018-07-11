#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// allgemeines Interface für ein Spielfeld
  /// </summary>
  public interface ISokoField
  {
    /// <summary>
    /// gibt die Breite des Spielfeldes zurück
    /// </summary>
    int Width { get; }
  }
}
