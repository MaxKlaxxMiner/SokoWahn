//#define byteModus

#if byteModus
using SatzTyp = System.Byte;
#else
using SatzTyp = System.UInt16;
#endif

#region using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ngMax;
#endregion

namespace Sokosolver.SokowahnTools
{

  #region # public class SokowahnLinearList2 // Klasse zum schreiben und lesen von Stellungen in linearen Listen
  /// <summary>
  /// Klasse zum schreiben und lesen von Stellungen in linearen Listen
  /// </summary>
#if byteModus
  public class SokowahnLinearList2Byte : IDisposable
#else
  public class SokowahnLinearList2 : IDisposable
#endif
  {
    /// <summary>
    /// gibt die maximale Anzahl der Elemente im Buffer an
    /// </summary>
    const int BufferElemente = 32768 / sizeof(SatzTyp); // 32 KByte

    /// <summary>
    /// merkt sich die Größe eines Datensatzes
    /// </summary>
    int satzGröße;

    /// <summary>
    /// merkt sich die maximale Anzahl der Datensätze im Buffer
    /// </summary>
    int bufferMax;

    /// <summary>
    /// Buffer für die schreibenden Daten
    /// </summary>
    SatzTyp[] schreibBuffer;
    /// <summary>
    /// merkt sich die Anzahl der geschriebenen Datensätze
    /// </summary>
    int schreibBufferPos = 0;

    /// <summary>
    /// Buffer für lesenden Daten
    /// </summary>
    SatzTyp[] leseBuffer;
    /// <summary>
    /// gibt die aktuelle Leseposition der Datensätze im Buffer zurück
    /// </summary>
    int leseBufferPos = 0;
    /// <summary>
    /// gibt die Anzahl der Datensätze im Buffer an
    /// </summary>
    int leseBufferGro = 0;

    /// <summary>
    /// merkt sich die temporären Buffer, welche bereits in einer Datei ausgelagert wurden
    /// </summary>
    List<int> tmpBufferBelegt = new List<int>();
    /// <summary>
    /// merkt sich die Position in der Temp-Datei, welche wieder frei geworden sind
    /// </summary>
    List<int> tmpBufferFrei = new List<int>();

    /// <summary>
    /// merkt sich den Temporären Ordner zum Auslagern sehr langer Listen
    /// </summary>
    string tempOrdner;

    /// <summary>
    /// erstellte Temporäre Datei (bleibt leer, wenn die Datei noch nicht benötigt wurde)
    /// </summary>
    string tempDatei;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="satzGröße">Größe eines einzelnen Datensatzes (kistenAnzahl + 1)</param>
    /// <param name="bufferMax">Maximale Größe des Lese-/Schreib-Buffers in Bytes</param>
    /// <param name="tempOrdner">Temporärer Ordner</param>
#if byteModus
    public SokowahnLinearList2Byte(int satzGröße, string tempOrdner)
#else
    public SokowahnLinearList2(int satzGröße, string tempOrdner)
#endif
    {
      this.satzGröße = satzGröße;
      this.bufferMax = BufferElemente / satzGröße;
      this.schreibBuffer = new SatzTyp[BufferElemente];
      this.leseBuffer = new SatzTyp[BufferElemente];
      if (!Directory.Exists(tempOrdner)) throw new Exception("Fehler, temporärer Ordner wurde nicht gefunden: \"" + tempOrdner + "\"");
      this.tempOrdner = tempOrdner;
    }
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="satzGröße">Größe eines einzelnen Datensatzes (kistenAnzahl + 1)</param>
    /// <param name="bufferMax">Maximale Größe des Lese-/Schreib-Buffers in Bytes</param>
    /// <param name="tempOrdner">Temporärer Ordner</param>
    /// <param name="multi32k">Multiplikator für den lese/schreib Buffer </param>
#if byteModus
    public SokowahnLinearList2Byte(int satzGröße, string tempOrdner, int multi32k)
#else
    public SokowahnLinearList2(int satzGröße, string tempOrdner, int multi32k)
#endif
    {
      multi32k = Math.Min(Math.Max(1, multi32k), 16384);
      this.satzGröße = satzGröße;
      this.bufferMax = BufferElemente * multi32k / satzGröße;
      this.schreibBuffer = new SatzTyp[BufferElemente * multi32k];
      this.leseBuffer = new SatzTyp[BufferElemente * multi32k];
      if (!Directory.Exists(tempOrdner)) throw new Exception("Fehler, temporärer Ordner wurde nicht gefunden: \"" + tempOrdner + "\"");
      this.tempOrdner = tempOrdner;
    }
    #endregion

    #region # public void Add(int spielerRaumPos, int[] kistenRaumPos) // fügt eine Stellung in die Liste ein
    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="data">direkte Daten (muss gleichlang sein wie satzGröße</param>
    public void Add(SatzTyp[] data)
    {
      if (schreibBufferPos == bufferMax)
      {
        if (tempDatei == null) tempDatei = tempOrdner + "tmp_" + Zp.TickCount.ToString().Replace(",", "").Replace(".", "") + ".tmp";

#if byteModus
        byte[] wbuf = schreibBuffer;
#else
        byte[] wbuf = schreibBuffer.ToByteArray();
#endif
        schreibBufferPos = 0;

        int wpos;
        if (tmpBufferFrei.Count > 0)
        {
          wpos = tmpBufferFrei[0];
          tmpBufferFrei.RemoveAt(0);
        }
        else
        {
          wpos = tmpBufferBelegt.Count;
        }

        FileStream wdat = new FileStream(tempDatei, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        wdat.Position = (long)wpos * (long)wbuf.Length;
        wdat.Write(wbuf);
        wdat.Close();

        tmpBufferBelegt.Add(wpos);
      }
      int p = schreibBufferPos * satzGröße;
      schreibBufferPos++;
      for (int i = 0; i < data.Length; i++) schreibBuffer[p++] = data[i];
#if DEBUG
      if (p != schreibBufferPos * satzGröße) throw new Exception("autsch!");
#endif
    }

    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition im Raum</param>
    /// <param name="kistenRaumPos">Kistenpositionen im Raum</param>
    public void Add(int spielerRaumPos, int[] kistenRaumPos)
    {
      if (schreibBufferPos == bufferMax)
      {
        if (tempDatei == null) tempDatei = tempOrdner + "tmp_" + Zp.TickCount.ToString().Replace(",", "").Replace(".", "") + ".tmp";

#if byteModus
        byte[] wbuf = schreibBuffer;
#else
        byte[] wbuf = schreibBuffer.ToByteArray();
#endif
        schreibBufferPos = 0;

        int wpos;
        if (tmpBufferFrei.Count > 0)
        {
          wpos = tmpBufferFrei[0];
          tmpBufferFrei.RemoveAt(0);
        }
        else
        {
          wpos = tmpBufferBelegt.Count;
        }

        FileStream wdat = new FileStream(tempDatei, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        wdat.Position = (long)wpos * (long)wbuf.Length;
        wdat.Write(wbuf);
        wdat.Close();

        tmpBufferBelegt.Add(wpos);
      }
      int p = schreibBufferPos * satzGröße;
      schreibBufferPos++;
      schreibBuffer[p++] = (SatzTyp)spielerRaumPos;
      for (int i = 0; i < kistenRaumPos.Length; i++) schreibBuffer[p++] = (SatzTyp)kistenRaumPos[i];
#if DEBUG
      if (p != schreibBufferPos * satzGröße) throw new Exception("autsch!");
#endif
    }

    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition im Raum</param>
    /// <param name="kistenRaumPos">Kistenpositionen im Raum</param>
    /// <param name="punkte">Punkte, welche mit gespeicherten werden sollen</param>
    public void Add(int spielerRaumPos, int[] kistenRaumPos, SokowahnPunkte punkte)
    {
      if (schreibBufferPos == bufferMax)
      {
        if (tempDatei == null) tempDatei = tempOrdner + "tmp_" + Zp.TickCount.ToString().Replace(",", "").Replace(".", "") + ".tmp";

#if byteModus
        throw new Exception("Punkte können im ByteModus nicht gespeichert werden!");
#else
        byte[] wbuf = schreibBuffer.ToByteArray();
#endif
        schreibBufferPos = 0;

        int wpos;
        if (tmpBufferFrei.Count > 0)
        {
          wpos = tmpBufferFrei[0];
          tmpBufferFrei.RemoveAt(0);
        }
        else
        {
          wpos = tmpBufferBelegt.Count;
        }

        FileStream wdat = new FileStream(tempDatei, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        wdat.Position = (long)wpos * (long)wbuf.Length;
        wdat.Write(wbuf);
        wdat.Close();

        tmpBufferBelegt.Add(wpos);
      }
      int p = schreibBufferPos * satzGröße;
      schreibBufferPos++;
      schreibBuffer[p++] = (SatzTyp)spielerRaumPos;
      for (int i = 0; i < kistenRaumPos.Length; i++) schreibBuffer[p++] = (SatzTyp)kistenRaumPos[i];
      schreibBuffer[p++] = (SatzTyp)punkte.tiefeMin;
      schreibBuffer[p++] = (SatzTyp)punkte.tiefeMax;
#if DEBUG
      if (p != schreibBufferPos * satzGröße) throw new Exception("autsch!");
#endif
    }
    #endregion

    /// <summary>
    /// gibt genau eine Stellung aus der Liste zurück
    /// </summary>
    /// <returns>ausgelesene Stellung</returns>
    public SatzTyp[] Pop()
    {
      var ausgabe = new SatzTyp[satzGröße];

      if (leseBufferPos == leseBufferGro)
      {
        if (schreibBufferPos > 0) // SchreibBuffer bevorzugen?
        {
          // Buffer tauschen und Schreibbuffer gleich zum lesen benutzen
          var tmpBuffer = leseBuffer;
          leseBuffer = schreibBuffer;
          schreibBuffer = tmpBuffer;
          leseBufferPos = 0;
          leseBufferGro = schreibBufferPos;
          schreibBufferPos = 0;
        }
        else
        {
          int rpos = tmpBufferBelegt[tmpBufferBelegt.Count - 1];
          tmpBufferBelegt.RemoveAt(tmpBufferBelegt.Count - 1);
          tmpBufferFrei.Add(rpos);

          FileStream rdat = new FileStream(tempDatei, FileMode.Open, FileAccess.ReadWrite);

#if byteModus
          rdat.Position = (long)rpos * leseBuffer.LongLength;
          if (rdat.Read(leseBuffer, 0, leseBuffer.Length) != leseBuffer.Length) throw new Exception("Lesefehler!");
#else
          byte[] rbuf = new byte[leseBuffer.Length * sizeof(SatzTyp)];
          rdat.Position = (long)rpos * (long)rbuf.Length;
          if (rdat.Read(rbuf, 0, rbuf.Length) != rbuf.Length) throw new Exception("Lesefehler!");
          rbuf.ToStructArray<ushort>(0, rbuf.Length, leseBuffer, 0);
#endif

          rdat.Close();

          leseBufferPos = 0;
          leseBufferGro = bufferMax;
        }
      }

      int p = leseBufferPos * ausgabe.Length;
      leseBufferPos++;

      for (int i = 0; i < ausgabe.Length; i++) ausgabe[i] = leseBuffer[p++];

      return ausgabe;
    }

    /// <summary>
    /// gibt mehrere Stellungen aus der Liste zurück
    /// </summary>
    /// <param name="anzahl">Anzahl der zu lesenden Stellungen</param>
    /// <returns>ausgelesene Stellung</returns>
    public SatzTyp[] Pop(int anzahl)
    {
      var ausgabe = new SatzTyp[anzahl * satzGröße];
      int ausgabePos = 0;

      while (ausgabePos < ausgabe.Length)
      {
        if (leseBufferPos == leseBufferGro)
        {
          if (schreibBufferPos > 0) // SchreibBuffer bevorzugen?
          {
            // Buffer tauschen und Schreibbuffer gleich zum lesen benutzen
            var tmpBuffer = leseBuffer;
            leseBuffer = schreibBuffer;
            schreibBuffer = tmpBuffer;
            leseBufferPos = 0;
            leseBufferGro = schreibBufferPos;
            schreibBufferPos = 0;
          }
          else
          {
            int rpos = tmpBufferBelegt[tmpBufferBelegt.Count - 1];
            tmpBufferBelegt.RemoveAt(tmpBufferBelegt.Count - 1);
            tmpBufferFrei.Add(rpos);

            FileStream rdat = new FileStream(tempDatei, FileMode.Open, FileAccess.ReadWrite);

#if byteModus
            rdat.Position = (long)rpos * leseBuffer.LongLength;
            if (rdat.Read(leseBuffer, 0, leseBuffer.Length) != leseBuffer.Length) throw new Exception("Lesefehler!");
#else
            byte[] rbuf = new byte[leseBuffer.Length * sizeof(ushort)];
            rdat.Position = (long)rpos * rbuf.LongLength;
            if (rdat.Read(rbuf, 0, rbuf.Length) != rbuf.Length) throw new Exception("Lesefehler!");
            rbuf.ToStructArray<ushort>(0, rbuf.Length, leseBuffer, 0);
#endif

            rdat.Close();

            leseBufferPos = 0;
            leseBufferGro = bufferMax;
          }
        }

        int copy = Math.Min(leseBufferGro - leseBufferPos, anzahl);
#if byteModus
        leseBuffer.ToByteArray(leseBufferPos * satzGröße, copy * satzGröße, ausgabe, ausgabePos);
#else
        leseBuffer.ToStructArray(leseBufferPos * satzGröße, copy * satzGröße, ausgabe, ausgabePos);
#endif
        ausgabePos += copy * satzGröße;
        leseBufferPos += copy;
        anzahl -= copy;
      }

      return ausgabe;
    }

    /// <summary>
    /// reduziert den Speicherverbrauch (falls ein Multiplikator angegeben wurde) und lagert die Daten in eine Temp-Datei aus
    /// </summary>
    /// <returns>Anzahl der Bytes, welche frei geworden sind</returns>
    public long Refresh()
    {
      if (schreibBuffer == null || schreibBuffer.Length == BufferElemente) return 0; // wurde bereits Refreshed

#if byteModus
      var tmp = new SokowahnLinearList2Byte(satzGröße, tempOrdner);
#else
      var tmp = new SokowahnLinearList2(satzGröße, tempOrdner);
#endif
      long bis = this.SatzAnzahl;
      for (long i = 0; i < bis; i++)
      {
        tmp.Add(this.Pop());
      }

      this.Dispose();
      this.bufferMax = tmp.bufferMax;
      this.leseBuffer = tmp.leseBuffer;
      this.leseBufferGro = tmp.leseBufferGro;
      this.leseBufferPos = tmp.leseBufferPos;
      this.schreibBuffer = tmp.schreibBuffer;
      this.schreibBufferPos = tmp.schreibBufferPos;
      this.tempDatei = tmp.tempDatei;
      tmp.tempDatei = null;
      this.tmpBufferBelegt = tmp.tmpBufferBelegt;
      this.tmpBufferFrei = tmp.tmpBufferFrei;
      tmp.Dispose();

      GC.Collect();
      return SatzAnzahl * (long)sizeof(SatzTyp);
    }

    #region # // --- Rest ---
    /// <summary>
    /// alle Ressourcen wieder frei geben
    /// </summary>
    public void Dispose()
    {
      schreibBuffer = null;
      leseBuffer = null;
      if (tempDatei != null) File.Delete(tempDatei);
    }

    /// <summary>
    /// Destruktor
    /// </summary>
#if byteModus
    ~SokowahnLinearList2Byte()
#else
    ~SokowahnLinearList2()
#endif
    {
      Dispose();
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Datensätze zurück
    /// </summary>
    public long SatzAnzahl
    {
      get
      {
        return (long)tmpBufferBelegt.Count * (long)bufferMax + (long)schreibBufferPos + (long)(leseBufferGro - leseBufferPos);
      }
    }

    /// <summary>
    /// gibt die Größe eines Datensatzes zurück
    /// </summary>
    public int SatzGröße
    {
      get
      {
        return satzGröße;
      }
    }

    /// <summary>
    /// gibt an, wieviel Speicher insgesamt benutzt wird (Ram + Festplatte)
    /// </summary>
    public long BelegtInBytes
    {
      get
      {
        if (tempDatei != null) return SatzAnzahl * (long)(satzGröße * sizeof(SatzTyp));
        return schreibBuffer.Length * sizeof(SatzTyp);
      }
    }

    /// <summary>
    /// gibt den Aufbau als lesbaren String zurück
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      return "Datensätze: " + SatzAnzahl.ToString("#,##0") + " (" + ((double)BelegtInBytes / 1048576.0).ToString("#,##0.0") + " MB)";
    }
    #endregion
  }
  #endregion
}
