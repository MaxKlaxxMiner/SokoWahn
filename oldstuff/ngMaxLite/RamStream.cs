#region # using *.*

using System;
using System.IO;

#endregion

namespace Sokosolver
{
  #region # public class RamStream : Stream // Stream, welcher die gesamten Daten im Arbeitsspeicher hält (ähnlich wie MemoryStream, jedoch kann auch ein bereits vorhandenes ByteArray genutzt werden und verbraucht so keinen zusätzlichen Speicher)
  /// <summary>
  /// Stream, welcher die gesamten Daten im Arbeitsspeicher hält (ähnlich wie MemoryStream, jedoch kann die gesamte Größe jederzeit angepasst werden)
  /// </summary>
  internal sealed class RamStream : Stream
  {
    /// <summary>
    /// merkt sich die Daten des Streams
    /// </summary>
    byte[] data;

    /// <summary>
    /// merkt sich die aktuelle Lese- / Schreibposition
    /// </summary>
    int pos;

    /// <summary>
    /// merkt sich die reale Größe des Streams
    /// </summary>
    int gro;

    /// <summary>
    /// gibt an, ob aus dem Stream gelesen werden kann (immer true)
    /// </summary>
    public override bool CanRead
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// gibt an, ob die Lese- / Schreibposition verändert werden kann (immer true)
    /// </summary>
    public override bool CanSeek
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// gibt an, ob in den Stream etwas geschrieben werden darf (immer true)
    /// </summary>
    public override bool CanWrite
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// räumt den Stream auf (reduziert den Speicherverbrauch, sofern möglich)
    /// </summary>
    public override void Flush()
    {
      if (gro < data.Length) Array.Resize(ref data, gro);
    }

    /// <summary>
    /// gibt die Größe des Streams zurück
    /// </summary>
    public override long Length
    {
      get
      {
        return data.Length;
      }
    }

    /// <summary>
    /// gibt die aktuelle Lese- / Schreibposition zurück oder setzt diese
    /// </summary>
    public override long Position
    {
      get
      {
        return pos;
      }
      set
      {
        if (value < 0)
        {
          pos = 0; return;
        }
        pos = (int)value;
      }
    }

    /// <summary>
    /// liest einen Bereich aus den Stream
    /// </summary>
    /// <param name="buffer">Ausgabe-Buffer</param>
    /// <param name="offset">Anfangsposition im Ausgabe-Buffer</param>
    /// <param name="count">max. Anzahl der zu lesenden Bytes</param>
    /// <returns>Anzahl der wirklich gelesenen Bytes</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
      int merkCount = count;
      while (count > 0 && pos < gro)
      {
        buffer[offset++] = data[pos++];
        count--;
      }
      return merkCount - count;
    }

    /// <summary>
    /// setzt die Lese- / Schreibposition im Stream
    /// </summary>
    /// <param name="offset">Positionsangabe</param>
    /// <param name="origin">Ausrichtung der Positionsangabe</param>
    /// <returns>die neue Position im Stream</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
      switch (origin)
      {
        case SeekOrigin.Begin: Position = offset; break;
        case SeekOrigin.Current: Position += offset; break;
        case SeekOrigin.End: Position = gro + offset; break;
      }
      return Position;
    }

    /// <summary>
    /// setzt die Größe des Streams neu
    /// </summary>
    /// <param name="value">neue Größe in Bytes</param>
    public override void SetLength(long value)
    {
      gro = (int)value;
      if (gro > data.Length) Array.Resize(ref data, gro);
    }

    /// <summary>
    /// schreibt Daten in den Stream
    /// </summary>
    /// <param name="buffer">Input-Buffer</param>
    /// <param name="offset">Lese-Position im Input-Buffer</param>
    /// <param name="count">Anzahl der zu schreibenden Bytes</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
      if (count + pos > gro)
      {
        gro = count + pos;
        while (gro > data.Length) Array.Resize(ref data, data.Length * 2);
      }
      while (count > 0)
      {
        data[pos++] = buffer[offset++];
        count--;
      }
    }

    /// <summary>
    /// schließt den Stream bzw. muss nicht machen
    /// </summary>
    public override void Close() { }

    /// <summary>
    /// liest ein einzelnes Byte aus dem Stream
    /// </summary>
    /// <returns>gelesene Byte oder -1 wenn Streamende erreicht</returns>
    public override int ReadByte()
    {
      if (pos >= gro) return -1;
      return data[pos++];
    }

    /// <summary>
    /// schreib ein Byte in den Stream
    /// </summary>
    /// <param name="value">Byte, welches in den Stream geschrieben werden soll</param>
    public override void WriteByte(byte value)
    {
      if (pos >= gro)
      {
        gro = pos + 1;
        while (gro > data.Length) Array.Resize(ref data, data.Length * 2);
      }
      data[pos++] = value;
    }

    /// <summary>
    /// gibt den Speicher wieder frei
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
      data = null;
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="data">ByteArray, welches benutzt werden soll</param>
    /// <param name="kopieErstellen">gibt an, ob eine Kopie vom Array erstellt werden soll (deafult: false = das Array wird direkt verwendet)</param>
    public RamStream(byte[] data, bool kopieErstellen)
    {
      if (kopieErstellen)
      {
        this.data = new byte[data.Length];
        pos = 0;
        gro = data.Length;
        for (int i = 0; i < gro; i++) this.data[i] = data[i];
      }
      else
      {
        this.data = data;
        pos = 0;
        gro = data.Length;
      }
    }
  }
  #endregion
}
