
#region # using *.*

using System;

#endregion

namespace JSoko.Utilities
{
  public class Debug
  {
    public static void CheckParameters(string[] argv)
    {
      throw new NotImplementedException(string.Join(" ", argv));
    }
  }
}
