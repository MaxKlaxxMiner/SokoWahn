
using System.IO;

namespace JSoko.java.io
{
  public sealed class BufferedReader
  {
    BinaryReader binaryReader;

    public BufferedReader(Stream inputStream)
    {
      binaryReader = new BinaryReader(inputStream);
    }

    public void Close()
    {
      if (binaryReader == null) return;
      binaryReader.Close();
      binaryReader = null;
    }
  }
}
