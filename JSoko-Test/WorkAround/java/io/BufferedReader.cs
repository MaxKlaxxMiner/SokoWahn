
using System.IO;

namespace JSoko.java.io
{
  public class BufferedReader
  {
    Stream baseStream;
    BinaryReader binaryReader;

    public BufferedReader(Stream baseStream)
    {
      this.baseStream = baseStream;
      binaryReader = new BinaryReader(baseStream);
    }

    public void Close()
    {
      if (binaryReader == null) return;

      binaryReader.Close();
      binaryReader = null;
      baseStream = null;
    }
  }
}
