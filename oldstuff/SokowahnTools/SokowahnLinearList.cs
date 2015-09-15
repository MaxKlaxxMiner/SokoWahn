#region using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ngMax;
#endregion

namespace Sokosolver.SokowahnTools
{
  #region # public class SokowahnLinearList // Klasse zum schreiben und lesen von Stellungen in linearen Listen
  /// <summary>
  /// Klasse zum schreiben und lesen von Stellungen in linearen Listen
  /// </summary>
  public class SokowahnLinearList : IDisposable
  {
    /// <summary>
    /// merkt sich die Größe eines einzelnen Datensatzes
    /// </summary>
    int satzGröße;

    /// <summary>
    /// maximale Größe des Buffers (Anzahl der Datensätze)
    /// </summary>
    int bufferMax;

    /// <summary>
    /// aktuelle Größe des Buffers (Anzahl der Datensätze)
    /// </summary>
    int bufferGro;

    /// <summary>
    /// Schreib-/Lese-Position im Buffer (Anzahl der Datensätze)
    /// </summary>
    int bufferPos;

    /// <summary>
    /// Buffer für die zu lesenden oder schreibenden Daten
    /// </summary>
    ushort[] buffer;

    /// <summary>
    /// merkt sich den Temporären Ordner zum Auslagern sehr langer Listen
    /// </summary>
    string tempOrdner;

    /// <summary>
    /// erstellte Temporäre Datei (bleibt leer, wenn die Datei noch nicht benötigt wurde)
    /// </summary>
    string tempDatei;

    /// <summary>
    /// merkt sich die Größe der geschriebenen Temp-Daten
    /// </summary>
    long tempDatenSchreiben;

    /// <summary>
    /// merkt sich die Größe der gelesenen Temp-Daten
    /// </summary>
    long tempDatenLesen;

    /// <summary>
    /// gibt an, ob sich die Liste im Lese-Modus befindet (sonst im Schreib-Modus)
    /// </summary>
    bool leseModus;

    /// <summary>
    /// gibt die Anzahl der gespeicherten Datensätze zurück
    /// </summary>
    public long SatzAnzahl
    {
      get
      {
        return leseModus ? (long)bufferGro - (long)bufferPos + (tempDatenSchreiben - tempDatenLesen) : (long)bufferPos + tempDatenSchreiben;
      }
    }

    /// <summary>
    /// gibt an, wieviel Speicher insgesamt benutzt wird (Ram + Festplatte)
    /// </summary>
    public long BelegtInBytes
    {
      get
      {
        return ((long)bufferGro + tempDatenSchreiben) * (long)satzGröße * 2L;
      }
    }

    #region # public SokowahnLinearList(int satzGröße, int bufferMax, string tempOrdner) // Konstruktor
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="satzGröße">Größe eines einzelnen Datensatzes (kistenAnzahl + 1)</param>
    /// <param name="bufferMax">Maximale Größe des Lese-/Schreib-Buffers in Bytes</param>
    /// <param name="tempOrdner">Temporärer Ordner</param>
    public SokowahnLinearList(int satzGröße, int bufferMax, string tempOrdner)
    {
      this.satzGröße = satzGröße;
      this.bufferMax = bufferMax / 2 / satzGröße;
      if (bufferMax < 2) throw new Exception("Wert für bufferMax war zu niedrig!");
      if (!Directory.Exists(tempOrdner)) throw new Exception("Fehler, temporärer Ordner wurde nicht gefunden: \"" + tempOrdner + "\"");
      this.tempOrdner = tempOrdner;

      this.leseModus = false;
      this.bufferPos = 0;
      this.bufferGro = 1;
      this.buffer = new ushort[bufferGro * satzGröße];
    }
    #endregion

    #region # void TempSpeichern() // speichert den Buffer in eine Temp-Datei und gibt den Buffer zum aufnehmen neuer Daten wieder frei
    /// <summary>
    /// speichert den Buffer in eine Temp-Datei und gibt den Buffer zum aufnehmen neuer Daten wieder frei
    /// </summary>
    void TempSpeichern()
    {
      if (tempDatei == null) tempDatei = tempOrdner + "tmp_" + Zp.TickCount.ToString().Replace(",", "").Replace(".", "") + ".tmp";
      BinaryWriter schreib = new BinaryWriter(new FileStream(tempDatei, FileMode.Append, FileAccess.Write));
      int bis = bufferPos * satzGröße;
      for (int i = 0; i < bis; i++) schreib.Write(buffer[i]);
      schreib.Close();
      tempDatenSchreiben += (long)bufferPos;
      bufferPos = 0;
    }
    #endregion

    #region # void TempLaden() // lädt die nächsten Daten von der Temp-Datei wieder in den Buffer
    /// <summary>
    /// lädt die nächsten Daten von der Temp-Datei wieder in den Buffer
    /// </summary>
    void TempLaden()
    {
      if (tempDatenLesen == tempDatenSchreiben)
      {
        if (File.Exists(tempDatei)) File.Delete(tempDatei);
        return;
      }

      BinaryReader lese = new BinaryReader(new FileStream(tempDatei, FileMode.Open, FileAccess.Read));
      lese.BaseStream.Position = tempDatenLesen * (long)satzGröße * 2L;

      bufferGro = (int)Math.Min((long)bufferGro, tempDatenSchreiben - tempDatenLesen);

      int bis = bufferGro * satzGröße;
      for (int i = 0; i < bis; i++) buffer[i] = lese.ReadUInt16();

      lese.Close();

      tempDatenLesen += (long)bufferGro;
      if (tempDatenLesen == tempDatenSchreiben)
      {
        if (File.Exists(tempDatei)) File.Delete(tempDatei);
      }

      bufferPos = 0;
    }
    #endregion

    #region # public void Add(int spielerRaumPos, int[] kistenRaumPos) // fügt eine Stellung in die Liste ein
    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition im Raum</param>
    /// <param name="kistenRaumPos">Kistenpositionen im Raum</param>
    public void Add(int spielerRaumPos, int[] kistenRaumPos)
    {
      if (leseModus) throw new Exception("Fehler beim hinzufügen, Liste befindet siche bereits im Lese-Modus!");

      if (bufferPos == bufferGro)
      {
        if (bufferGro < bufferMax)
        {
          bufferGro = Math.Min(bufferGro * 2, bufferMax);
          Array.Resize(ref buffer, bufferGro * satzGröße);
        }
        else
        {
          TempSpeichern();
        }
      }

      int off = bufferPos * satzGröße;
      buffer[off] = (ushort)spielerRaumPos;
      for (int i = 1; i < satzGröße; i++) buffer[off + i] = (ushort)kistenRaumPos[i - 1];

      bufferPos++;
    }

    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="raum">Raum mit den entsprechenden Daten</param>
    public void Add(SokowahnRaum raum)
    {
      if (leseModus) throw new Exception("Fehler beim hinzufügen, Liste befindet siche bereits im Lese-Modus!");

      if (bufferPos == bufferGro)
      {
        if (bufferGro < bufferMax)
        {
          bufferGro = Math.Min(bufferGro * 2, bufferMax);
          Array.Resize(ref buffer, bufferGro * satzGröße);
        }
        else
        {
          TempSpeichern();
        }
      }

      int off = bufferPos * satzGröße;
      buffer[off] = (ushort)raum.raumSpielerPos;
      for (int i = 1; i < satzGröße; i++) buffer[off + i] = (ushort)raum.kistenZuRaum[i - 1];

      bufferPos++;
    }
    #endregion

    #region # public SokowahnStellungMinimal Pop() // gibt genau eine Stellung von der Liste zurück (nach dem ersten Lesevorgang, können keinen Daten mehr geschrieben werden)
    /// <summary>
    /// gibt genau eine Stellung von der Liste zurück (nach dem ersten Lesevorgang, können keinen Daten mehr geschrieben werden)
    /// </summary>
    /// <returns>ausgelesene Stellung</returns>
    public ushort[] Pop()
    {
      if (!leseModus)
      {
        if (tempDatei != null)
        {
          TempSpeichern();
          TempLaden();
        }
        else
        {
          bufferGro = bufferPos;
          bufferPos = 0;
        }
        leseModus = true;
      }

      if (bufferPos == bufferGro && tempDatei != null) TempLaden();

      int off = bufferPos * satzGröße;
      bufferPos++;

      return Enumerable.Range(off, satzGröße).Select(i => buffer[i]).ToArray();
    }
    #endregion

    /// <summary>
    /// alle Ressourcen wieder frei geben
    /// </summary>
    public void Dispose()
    {
      buffer = null;
      leseModus = false;
      bufferPos = 0;
      if (tempDatei != null && File.Exists(tempDatei)) File.Delete(tempDatei);
      tempDatenLesen = 0;
      tempDatenSchreiben = 0;
    }

    /// <summary>
    /// Destruktor
    /// </summary>
    ~SokowahnLinearList()
    {
      Dispose();
    }

    /// <summary>
    /// gibt den Aufbau als lesbaren String zurück
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      return "Datensätze: " + SatzAnzahl.ToString("#,##0") + " (" + ((double)BelegtInBytes / 1048576.0).ToString("#,##0.0") + " MB)";
    }
  }
  #endregion

  #region # public class SokowahnLinearListByte // Klasse zum schreiben und lesen von Stellungen in linearen Listen (Byte-Version)
  /// <summary>
  /// Klasse zum schreiben und lesen von Stellungen in linearen Listen (Byte-Version)
  /// </summary>
  public class SokowahnLinearListByte : IDisposable
  {
    /// <summary>
    /// merkt sich die Größe eines einzelnen Datensatzes
    /// </summary>
    int satzGröße;

    /// <summary>
    /// maximale Größe des Buffers (Anzahl der Datensätze)
    /// </summary>
    int bufferMax;

    /// <summary>
    /// aktuelle Größe des Buffers (Anzahl der Datensätze)
    /// </summary>
    int bufferGro;

    /// <summary>
    /// Schreib-/Lese-Position im Buffer (Anzahl der Datensätze)
    /// </summary>
    int bufferPos;

    /// <summary>
    /// Buffer für die zu lesenden oder schreibenden Daten
    /// </summary>
    byte[] buffer;

    /// <summary>
    /// merkt sich den Temporären Ordner zum Auslagern sehr langer Listen
    /// </summary>
    string tempOrdner;

    /// <summary>
    /// erstellte Temporäre Datei (bleibt leer, wenn die Datei noch nicht benötigt wurde)
    /// </summary>
    string tempDatei;

    /// <summary>
    /// merkt sich die Größe der geschriebenen Temp-Daten
    /// </summary>
    long tempDatenSchreiben;

    /// <summary>
    /// merkt sich die Größe der gelesenen Temp-Daten
    /// </summary>
    long tempDatenLesen;

    /// <summary>
    /// gibt an, ob sich die Liste im Lese-Modus befindet (sonst im Schreib-Modus)
    /// </summary>
    bool leseModus;

    /// <summary>
    /// gibt die Anzahl der gespeicherten Datensätze zurück
    /// </summary>
    public long SatzAnzahl
    {
      get
      {
        return leseModus ? (long)bufferGro - (long)bufferPos + (tempDatenSchreiben - tempDatenLesen) : (long)bufferPos + tempDatenSchreiben;
      }
    }

    /// <summary>
    /// gibt an, wieviel Speicher insgesamt benutzt wird (Ram + Festplatte)
    /// </summary>
    public long BelegtInBytes
    {
      get
      {
        return ((long)bufferGro + tempDatenSchreiben) * (long)satzGröße;
      }
    }

    #region # public SokowahnLinearListByte(int satzGröße, int bufferMax, string tempOrdner) // Konstruktor
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="satzGröße">Größe eines einzelnen Datensatzes (kistenAnzahl + 1)</param>
    /// <param name="bufferMax">Maximale Größe des Lese-/Schreib-Buffers in Bytes</param>
    /// <param name="tempOrdner">Temporärer Ordner</param>
    public SokowahnLinearListByte(int satzGröße, int bufferMax, string tempOrdner)
    {
      this.satzGröße = satzGröße;
      this.bufferMax = bufferMax / satzGröße;
      if (bufferMax < 2) throw new Exception("Wert für bufferMax war zu niedrig!");
      if (!Directory.Exists(tempOrdner)) throw new Exception("Fehler, temporärer Ordner wurde nicht gefunden: \"" + tempOrdner + "\"");
      this.tempOrdner = tempOrdner;

      this.leseModus = false;
      this.bufferPos = 0;
      this.bufferGro = 1;
      this.buffer = new byte[bufferGro * satzGröße];
    }
    #endregion

    #region # void TempSpeichern() // speichert den Buffer in eine Temp-Datei und gibt den Buffer zum aufnehmen neuer Daten wieder frei
    /// <summary>
    /// speichert den Buffer in eine Temp-Datei und gibt den Buffer zum aufnehmen neuer Daten wieder frei
    /// </summary>
    void TempSpeichern()
    {
      if (tempDatei == null) tempDatei = tempOrdner + "tmp_" + Zp.TickCount.ToString().Replace(",", "").Replace(".", "") + ".tmp";
      FileStream schreib = new FileStream(tempDatei, FileMode.Append, FileAccess.Write);
      schreib.Write(buffer, 0, bufferPos * satzGröße);
      schreib.Close();
      tempDatenSchreiben += (long)bufferPos;
      bufferPos = 0;
    }
    #endregion

    #region # void TempLaden() // lädt die nächsten Daten von der Temp-Datei wieder in den Buffer
    /// <summary>
    /// lädt die nächsten Daten von der Temp-Datei wieder in den Buffer
    /// </summary>
    void TempLaden()
    {
      if (tempDatenLesen == tempDatenSchreiben)
      {
        if (File.Exists(tempDatei)) File.Delete(tempDatei);
        return;
      }

      FileStream lese = new FileStream(tempDatei, FileMode.Open, FileAccess.Read);
      lese.Position = tempDatenLesen * (long)satzGröße;

      bufferGro = (int)Math.Min((long)bufferGro, tempDatenSchreiben - tempDatenLesen);

      lese.Read(buffer, 0, bufferGro * satzGröße);
      lese.Close();

      tempDatenLesen += (long)bufferGro;
      if (tempDatenLesen == tempDatenSchreiben)
      {
        if (File.Exists(tempDatei)) File.Delete(tempDatei);
      }

      bufferPos = 0;
    }
    #endregion

    #region # public void Add(int spielerRaumPos, int[] kistenRaumPos) // fügt eine Stellung in die Liste ein
    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition im Raum</param>
    /// <param name="kistenRaumPos">Kistenpositionen im Raum</param>
    public void Add(int spielerRaumPos, int[] kistenRaumPos)
    {
      if (leseModus) throw new Exception("Fehler beim hinzufügen, Liste befindet siche bereits im Lese-Modus!");

      if (bufferPos == bufferGro)
      {
        if (bufferGro < bufferMax)
        {
          bufferGro = Math.Min(bufferGro * 2, bufferMax);
          Array.Resize(ref buffer, bufferGro * satzGröße);
        }
        else
        {
          TempSpeichern();
        }
      }

      int off = bufferPos * satzGröße;
      buffer[off] = (byte)spielerRaumPos;
      for (int i = 1; i < satzGröße; i++) buffer[off + i] = (byte)kistenRaumPos[i - 1];

      bufferPos++;
    }

    /// <summary>
    /// fügt eine Stellung in die Liste ein
    /// </summary>
    /// <param name="raum">Raum mit den entsprechenden Daten</param>
    public void Add(SokowahnRaum raum)
    {
      if (leseModus) throw new Exception("Fehler beim hinzufügen, Liste befindet siche bereits im Lese-Modus!");

      if (bufferPos == bufferGro)
      {
        if (bufferGro < bufferMax)
        {
          bufferGro = Math.Min(bufferGro * 2, bufferMax);
          Array.Resize(ref buffer, bufferGro * satzGröße);
        }
        else
        {
          TempSpeichern();
        }
      }

      int off = bufferPos * satzGröße;
      buffer[off] = (byte)raum.raumSpielerPos;
      for (int i = 1; i < satzGröße; i++) buffer[off + i] = (byte)raum.kistenZuRaum[i - 1];

      bufferPos++;
    }
    #endregion

    #region # public SokowahnStellungMinimal Pop() // gibt genau eine Stellung von der Liste zurück (nach dem ersten Lesevorgang, können keinen Daten mehr geschrieben werden)
    /// <summary>
    /// gibt genau eine Stellung von der Liste zurück (nach dem ersten Lesevorgang, können keinen Daten mehr geschrieben werden)
    /// </summary>
    /// <returns>ausgelesene Stellung</returns>
    public byte[] Pop()
    {
      if (!leseModus)
      {
        if (tempDatei != null)
        {
          TempSpeichern();
          TempLaden();
        }
        else
        {
          bufferGro = bufferPos;
          bufferPos = 0;
        }
        leseModus = true;
      }

      if (bufferPos == bufferGro && tempDatei != null) TempLaden();

      int off = bufferPos * satzGröße;
      bufferPos++;

      return Enumerable.Range(off, satzGröße).Select(i => buffer[i]).ToArray();
    }
    #endregion

    /// <summary>
    /// alle Ressourcen wieder frei geben
    /// </summary>
    public void Dispose()
    {
      buffer = null;
      leseModus = false;
      bufferPos = 0;
      if (tempDatei != null && File.Exists(tempDatei)) File.Delete(tempDatei);
      tempDatenLesen = 0;
      tempDatenSchreiben = 0;
    }

    /// <summary>
    /// Destruktor
    /// </summary>
    ~SokowahnLinearListByte()
    {
      Dispose();
    }

    /// <summary>
    /// gibt den Aufbau als lesbaren String zurück
    /// </summary>
    /// <returns>lesbarer Inhalt</returns>
    public override string ToString()
    {
      return "Datensätze: " + SatzAnzahl.ToString("#,##0") + " (" + ((double)BelegtInBytes / 1048576.0).ToString("#,##0.0") + " MB)";
    }
  }
  #endregion
}
