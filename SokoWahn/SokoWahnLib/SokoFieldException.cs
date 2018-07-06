#region # using *.*
using System;
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Exception, wenn ein Fehler im Spielfeld gefunden wurde
  /// </summary>
  public class SokoFieldException : Exception
  {
    public SokoFieldException(string message) : base(message) { }
  }
}
