
#region # using *.*

using System;

#endregion

namespace JSoko.ResourceHandling
{
  public class Settings
  {
    public static void LoadSettings(JSoko jSoko)
    {
      // todo: throw new NotImplementedException();
    }

    public static int GetInt(string name, int alternate)
    {
      // todo
      return alternate;
    }

    public static string Get(string name)
    {
      if (name == "iconFolder") return "resources/graphics/icons/";

      return null;
    }
  }
}
