
using System;
using JSoko.java.io;

namespace JSoko.java.util
{
  public class Properties_
  {
    public void Load(BufferedReader propertyInputStream)
    {
      // todo
    }

    public void PutAll(Properties_ defaultSettings)
    {
      throw new NotImplementedException();
      // todo
    }

    public string GetProperty(string key)
    {
      if (key == "iconFolder") return "resources/graphics/icons/";

      return null;
    }

    public void SetProperty(string key, string value)
    {
    }
  }
}
