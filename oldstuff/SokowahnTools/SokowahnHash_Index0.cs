// --- nur ein Index-Typ darf bzw. muss gesetzt sein ---
#define Index0
//#define Index16
//#define Index24

// --- multiHash nur bei Index16 oder Index24 verfügbar ---
//#define multiHash

#region # using *.*
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ngMax;
#endregion

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// Hashsystem um sich bekannte Stellungen zu merken (Crc64)
  /// </summary>
#if Index0
#if multiHash
  Not Implemented!
#else
  public unsafe class SokowahnHash_Index0 : ISokowahnHash
#endif
#endif
#if Index16
#if multiHash
  public unsafe class SokowahnHash_Index16Multi : ISokowahnHash
#else
  public unsafe class SokowahnHash_Index16 : ISokowahnHash
#endif
#endif
#if Index24
#if multiHash
  public unsafe class SokowahnHash_Index24Multi : ISokowahnHash
#else
  public unsafe class SokowahnHash_Index24 : ISokowahnHash
#endif
#endif
  {
    #region # // --- Variablen ---
#if multiHash
    /// <summary>
    /// merkt sich die schnellen Hash-Einträge
    /// </summary>
    Dictionary<ulong, ushort>[] hashes;

    /// <summary>
    /// Anzahl der Hasheinträge
    /// </summary>
    const int hashesAnzahl = 4;

    /// <summary>
    /// Bits, welche benutzt werden müssen
    /// </summary>
    ulong hashesBits = (ulong)(hashesAnzahl - 1);
#else
    /// <summary>
    /// merkt sich die schnellen Hash-Einträge
    /// </summary>
    Dictionary<ulong, ushort> hash;
#endif

    /// <summary>
    /// merkt sich die Grenze, wieviel Einträge in einem Dictionary passen
    /// </summary>
    const int dictionaryLimit = 47400000;

#if Index0
    /// <summary>
    /// merkt sich weitere Hash-Einträge
    /// </summary>
    Dictionary<ulong, ushort>[] weitere = new Dictionary<ulong, ushort>[0];

#endif

#if Index16 || Index24
    /// <summary>
    /// merkt sich die Gesamtzahl der Archivierten Einträge
    /// </summary>
    long archivAnzahl = 0;

    /// <summary>
    /// Zeiger für das Array
    /// </summary>
    IntPtr archivDataPointer = IntPtr.Zero;

    /// <summary>
    /// merkt sich den Index auf das Archiv
    /// </summary>
    uint* archivIndex;

    /// <summary>
    /// Zeiger für das Array
    /// </summary>
    IntPtr archivIndexPointer = IntPtr.Zero;

#if Index16
    /// <summary>
    /// merkt sich die eigentlichen Archiv-Daten (Rest-Schlüssel und short-Wert)
    /// </summary>
    ulong* archivData;
#endif
#if Index24
    /// <summary>
    /// merkt sich die eigentlichen Archiv-Daten (Rest-Schlüssel und short-Wert)
    /// </summary>
    long archivData;
#endif
#endif

    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
#if Index0
    public SokowahnHash_Index0()
#endif
#if Index16
#if multiHash
    public SokowahnHash_Index16Multi()
#else
    public SokowahnHash_Index16()
#endif
#endif
#if Index24
#if multiHash
    public SokowahnHash_Index24Multi()
#else
    public SokowahnHash_Index24()
#endif
#endif
    {
#if multiHash
      hashes = new Dictionary<ulong, ushort>[hashesAnzahl];
      for (int i = 0; i < hashes.Length; i++) hashes[i] = new Dictionary<ulong, ushort>();
#else
      hash = new Dictionary<ulong, ushort>();
#endif

#if Index16
      archivIndexPointer = Marshal.AllocHGlobal((1 << 16) * 2 * 4);
      if (archivIndexPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((1 << 16) * 2 * 4) / 1048576L) + " MB)");
      archivIndex = (uint*)archivIndexPointer.ToPointer();
      for (int i = 0; i < (1 << 16) * 2; i++) archivIndex[i] = 0;
#endif

#if Index24
      archivIndexPointer = Marshal.AllocHGlobal((1 << 24) * 2 * 4);
      if (archivIndexPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((1 << 24) * 2 * 4) / 1048576L) + " MB)");
      archivIndex = (uint*)archivIndexPointer.ToPointer();
      for (int i = 0; i < (1 << 24) * 2; i++) archivIndex[i] = 0;
#endif
    }
    #endregion

    #region # public void Add(ulong code, int tiefe) // erstellt einen neuen Hash-Eintrag (darf noch nicht vorhanden sein)
    /// <summary>
    /// erstellt einen neuen Hash-Eintrag (darf noch nicht vorhanden sein)
    /// </summary>
    /// <param name="code">Hash-Code, welcher eintragen werden soll</param>
    /// <param name="tiefe">entsprechende Zugtiefe</param>
    public void Add(ulong code, int tiefe)
    {
#if multiHash
      var hash = hashes[code & hashesBits];
#endif
      hash.Add(code, (ushort)tiefe);

      if (hash.Count > dictionaryLimit)
      {
#if Index0
        Array.Resize(ref weitere, weitere.Length + 1);
        for (int i = weitere.Length - 1; i > 0; i--) weitere[i] = weitere[i - 1];
        weitere[0] = hash;
        hash = new Dictionary<ulong, ushort>();
#endif

#if Index16
        if (archivAnzahl == 0)
        {
          #region # // --- Archiv das erste mal erstellen ---
          // --- Archiv vorbereiten ---
#if multiHash
          archivAnzahl = hashes.Sum(x => x.Count);
#else
          archivAnzahl = hash.Count;
#endif

          archivDataPointer = Marshal.AllocHGlobal((IntPtr)((long)archivAnzahl * 8L));
          if (archivDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((long)archivAnzahl * 8L) / 1048576L) + " MB)");
          archivData = (ulong*)archivDataPointer.ToPointer();

          uint[] zähler = new uint[1 << 16];
#if multiHash
          foreach (var h in hashes)
          {
            h.Select(x => zähler[x.Key & 0xffff]++).Count();
          }
#else
          hash.Select(x => zähler[x.Key & 0xffff]++).Count();
#endif
          uint[] posis = new uint[1 << 16];
          uint pos = 0;
          for (int i = 1; i < (1 << 16); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Archiv befüllen ---
#if multiHash
          foreach (var h in hashes) foreach (var satz in h)
#else
          foreach (var satz in hash)
#endif
            {
              int indexPos = (int)(satz.Key & 0xffff);
              archivData[posis[indexPos]++] = (satz.Key & 0xffffffffffff0000) | (ulong)satz.Value;
            }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 16); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Archiv sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, 1 << 16).Select(i =>
          {
            Sort.Quick(&archivData[posis[i]], (int)zähler[i]);
            return i;
          }).Count();

          // --- 16 Bit-Index erstellen ---
          uint[] indexTemp = Enumerable.Range(0, (1 << 16) * 2).Select(i => (i & 1) == 0 ? zähler[i >> 1] : posis[i >> 1]).ToArray();

          for (int i = 0; i < indexTemp.Length; i++) archivIndex[i] = indexTemp[i];
          #endregion
        }
        else
        {
          #region # // --- Archiv erweitern ---
          // --- Neue Daten vorbereiten ---
          int dazuAnzahl = hash.Count;
          ulong[] dazuData = new ulong[dazuAnzahl];
          uint[] zähler = new uint[1 << 16];
          hash.Select(x => zähler[x.Key & 0xffff]++).Count();
          uint[] posis = new uint[1 << 16];
          uint pos = 0;
          for (int i = 1; i < (1 << 16); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten befüllen ---
          foreach (var satz in hash)
          {
            int indexPos = (int)(satz.Key & 0xffff);
            dazuData[posis[indexPos]++] = (satz.Key & 0xffffffffffff0000) | (ulong)satz.Value;
          }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 16); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, (1 << 16)).Select(i =>
          {
            Array.Sort(dazuData, (int)posis[i], (int)zähler[i]);
            return i;
          }).Count();

          // --- Neues Array erstellen ---
          ulong* altData = archivData;
          IntPtr altPointer = archivDataPointer;
          archivDataPointer = Marshal.AllocHGlobal((IntPtr)((long)(archivAnzahl + dazuAnzahl) * 8L));
          if (archivDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((long)(archivAnzahl + dazuAnzahl) * 8L) / 1048576L) + " MB)");
          archivData = (ulong*)archivDataPointer.ToPointer();

          // --- alte Archiv-Daten kopieren und zusammen mit den neuen Daten verschmelzen ---
          pos = 0;
          for (int index = 0; index < (1 << 16); index++)
          {
            uint posAlt = archivIndex[(index << 1) + 1];
            uint countAlt = archivIndex[index << 1];
            uint posNeu = posis[index];
            uint countNeu = zähler[index];
            uint dazu = 0;

            while (countAlt > 0 && countNeu > 0) // beide Tabellen verzahnen und ins neue Archiv speichern
            {
              if (altData[posAlt] < dazuData[posNeu])
              {
                archivData[pos] = altData[posAlt++];
                countAlt--;
              }
              else
              {
                archivData[pos] = dazuData[posNeu++];
                countNeu--;
              }
              pos++;
              dazu++;
            }
            while (countAlt > 0) // Reste der alten Tabelle übertragen
            {
              archivData[pos++] = altData[posAlt++];
              dazu++;
              countAlt--;
            }
            while (countNeu > 0) // Reste der neuen Tabelle übertragen
            {
              archivData[pos++] = dazuData[posNeu++];
              dazu++;
              countNeu--;
            }

            // Index anpassen
            archivIndex[index << 1] = dazu;
            archivIndex[(index << 1) + 1] = pos - dazu;
          }

          archivAnzahl = pos;

          Marshal.FreeHGlobal(altPointer);

          dazuData = null;
          #endregion
        }
#endif

#if Index24
        if (archivAnzahl == 0)
        {
        #region # // --- Archiv das erste mal erstellen ---
          // --- Archiv vorbereiten ---
#if multiHash
          archivAnzahl = hashes.Sum(x => (long)x.Count);
#else
          archivAnzahl = hash.Count;
#endif

          archivDataPointer = Marshal.AllocHGlobal((IntPtr)((long)archivAnzahl * 7L + 1L));
          if (archivDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((long)archivAnzahl * 7L + 1L) / 1048576L) + " MB)");
          archivData = (long)archivDataPointer.ToPointer();
#if DEBUG
          byte* zerofill = (byte*)archivData;
          for (int i = 0; i < archivAnzahl * 7 + 1; i++) zerofill[i] = 0x00;
#endif

          uint[] zähler = new uint[1 << 24];
#if multiHash
          foreach (var h in hashes)
          {
            h.Select(x => zähler[x.Key & 0xffffff]++).Count();
          }
#else
          hash.Select(x => zähler[x.Key & 0xffffff]++).Count();
#endif
          uint[] posis = new uint[1 << 24];
          uint pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Archiv befüllen ---
#if multiHash
          foreach (var h in hashes) foreach (var satz in h)
#else
          foreach (var satz in hash)
#endif
            {
              int indexPos = (int)(satz.Key & 0xffffff);
              ulong* p = (ulong*)(archivData + (long)(posis[indexPos]++) * 7L);
              *p = (*p & 0xff00000000000000) | ((satz.Key >> 8) & 0x00ffffffffff0000) | (ulong)satz.Value;
            }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Archiv sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, 1 << 24).Select(i =>
          {
            ulong[] tmp = new ulong[zähler[i]];
            long offset = (long)posis[i] * 7L + archivData;
            for (int x = 0; x < tmp.Length; x++)
            {
              tmp[x] = *(ulong*)(offset + (long)(x * 7)) & 0x00ffffffffffffff;
            }

            Sort.Quick(tmp);

            for (int x = 0; x < tmp.Length; x++)
            {
              ulong w = *(ulong*)(offset + (long)(x * 7)) & 0xff00000000000000;
              *(ulong*)(offset + (long)(x * 7)) = w | tmp[x];
            }
            return i;
          }).Count();

          // --- 24 Bit-Index erstellen ---
          uint[] indexTemp = Enumerable.Range(0, (1 << 24) * 2).Select(i => (i & 1) == 0 ? zähler[i >> 1] : posis[i >> 1]).ToArray();

          for (int i = 0; i < indexTemp.Length; i++) archivIndex[i] = indexTemp[i];
          #endregion
        }
        else
        {
        #region # // --- Archiv erweitern ---
          // --- Neue Daten vorbereiten ---
          int dazuAnzahl = hash.Count;
          ulong[] dazuData = new ulong[dazuAnzahl];
          uint[] zähler = new uint[1 << 24];
          hash.Select(x => zähler[x.Key & 0xffffff]++).Count();
          uint[] posis = new uint[1 << 24];
          uint pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten befüllen ---
          foreach (var satz in hash)
          {
            int indexPos = (int)(satz.Key & 0xffffff);
            dazuData[posis[indexPos]++] = ((satz.Key >> 8) & 0x00ffffffffff0000) | (ulong)satz.Value;
          }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += zähler[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, (1 << 24)).Select(i =>
          {
            Array.Sort(dazuData, (int)posis[i], (int)zähler[i]);
            return i;
          }).Count();

          // --- Neues Array erstellen ---
          long altData = archivData;
          IntPtr altPointer = archivDataPointer;
          archivDataPointer = Marshal.AllocHGlobal((IntPtr)((long)(archivAnzahl + dazuAnzahl) * 7L + 1L));
          if (archivDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((long)(archivAnzahl + dazuAnzahl) * 7L + 1L) / 1048576L) + " MB)");
          archivData = (long)archivDataPointer.ToPointer();

          // --- alte Archiv-Daten kopieren und zusammen mit den neuen Daten verschmelzen ---
          pos = 0;
          for (int index = 0; index < (1 << 24); index++)
          {
            uint posAlt = archivIndex[(index << 1) + 1];
            uint countAlt = archivIndex[index << 1];
            uint posNeu = posis[index];
            uint countNeu = zähler[index];
            uint dazu = 0;

            while (countAlt > 0 && countNeu > 0) // beide Tabellen verzahnen und ins neue Archiv speichern
            {
              if (((*(ulong*)(altData + (long)posAlt * 7L)) & 0x00ffffffffffffff) < dazuData[posNeu])
              {
                *(ulong*)(archivData + (long)pos * 7L) = ((*(ulong*)(archivData + (long)pos * 7L)) & 0xff00000000000000) | ((*(ulong*)(altData + (long)posAlt * 7L)) & 0x00ffffffffffffff);
                posAlt++;
                countAlt--;
              }
              else
              {
                *(ulong*)(archivData + (long)pos * 7L) = ((*(ulong*)(archivData + (long)pos * 7L)) & 0xff00000000000000) | dazuData[posNeu++];
                countNeu--;
              }
              pos++;
              dazu++;
            }
            while (countAlt > 0) // Reste der alten Tabelle übertragen
            {
              *(ulong*)(archivData + (long)pos * 7L) = ((*(ulong*)(archivData + (long)pos * 7L)) & 0xff00000000000000) | ((*(ulong*)(altData + (long)posAlt * 7L)) & 0x00ffffffffffffff);
              pos++;
              posAlt++;
              dazu++;
              countAlt--;
            }
            while (countNeu > 0) // Reste der neuen Tabelle übertragen
            {
              *(ulong*)(archivData + (long)pos * 7L) = ((*(ulong*)(archivData + (long)pos * 7L)) & 0xff00000000000000) | dazuData[posNeu++];
              pos++;
              dazu++;
              countNeu--;
            }

            // Index anpassen
            archivIndex[index << 1] = dazu;
            archivIndex[(index << 1) + 1] = pos - dazu;
          }

          archivAnzahl = pos;

          Marshal.FreeHGlobal(altPointer);

          dazuData = null;
          #endregion
        }
#endif

#if (Index16 || Index24)
#if multiHash
        for (int i = 0; i < hashes.Length; i++)
        {
          hashes[i] = null; // Hash aufräumen
        }
        hashes = new[] { new Dictionary<ulong, ushort>() };
        hashesBits = 0;
#else
        hash = new Dictionary<ulong, ushort>(); // Hash aufräumen
#endif

        GC.Collect();
#endif
      }
    }
    #endregion

    #region # public int Get(ulong code) // gibt die Zugtiefe eines Hasheintrages zurück (oder 65535, wenn nicht gefunden)
    /// <summary>
    /// gibt die Zugtiefe eines Hasheintrages zurück (oder 65535, wenn nicht gefunden)
    /// </summary>
    /// <param name="code">Hash-Code, welcher gesucht wird</param>
    /// <returns>entsprechende Zutiefe oder 65535, wenn nicht vorhanden</returns>
    public unsafe int Get(ulong code)
    {
      ushort ausgabe;

#if multiHash
      if (archivAnzahl == 0)
      {
        var hash = hashes[code & hashesBits];

        if (hash.TryGetValue(code, out ausgabe)) return (int)ausgabe; else return 65535;
      }
      else
      {
        if (hashes[0].TryGetValue(code, out ausgabe)) return (int)ausgabe;
      }
#else
      if (hash.TryGetValue(code, out ausgabe)) return (int)ausgabe;
#endif


#if Index0
      foreach (var w in weitere)
      {
        if (w.TryGetValue(code, out ausgabe)) return (int)ausgabe;
      }
#endif

#if Index16
      int index = (int)(code & 0xffff) << 1;
      long satzLänge = archivIndex[index];
      if (satzLänge > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long von = archivIndex[index + 1];
        long bis = von + satzLänge;
        long mit = 0;

        code = code & 0xffffffffffff0000;

        do
        {
          mit = (von + bis) >> 1;
          if ((archivData[mit] & 0xffffffffffff0000) > code) bis = mit; else von = mit;
        } while (bis - von > 1);

        if ((archivData[von] & 0xffffffffffff0000) == code) // Eintrag gefunden?
        {
          return (int)(archivData[von] & 0xffff);
        }
      }
#endif

#if Index24
      int index = (int)(code & 0xffffff) << 1;
      long satzLänge = archivIndex[index];
      if (satzLänge > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long von = (long)archivIndex[index + 1];
        long bis = von + satzLänge;
        long mit = 0;

        code = (code >> 8) & 0x00ffffffffff0000;

        do
        {
          mit = (von + bis) >> 1;
          if (((*(ulong*)(archivData + mit * 7L)) & 0x00ffffffffff0000) > code) bis = mit; else von = mit;
        } while (bis - von > 1);

        if (((*(ulong*)(archivData + von * 7L)) & 0x00ffffffffff0000) == code) // Eintrag gefunden?
        {
          return (int)*(ushort*)(archivData + von * 7L);
        }
      }
#endif

      return 65535; // nicht gefunden
    }
    #endregion

    #region # public void Update(ulong code, int tiefe) // aktualisiert einen Hash-Eintrag (muss vorhanden sein)
    /// <summary>
    /// aktualisiert einen Hash-Eintrag (muss vorhanden sein)
    /// </summary>
    /// <param name="code">Code, welcher bearbeitet werden soll</param>
    /// <param name="tiefe">neu zu setzende Zugtiefe</param>
    public unsafe void Update(ulong code, int tiefe)
    {
#if multiHash
      var hash = hashes[code & hashesBits];
#endif

      if (hash.ContainsKey(code))
      {
        hash[code] = (ushort)tiefe;
        return;
      }

#if Index0
      foreach (var w in weitere)
      {
        if (w.ContainsKey(code))
        {
          w[code] = (ushort)tiefe;
          return;
        }
      }
#endif

#if Index16
      int index = (int)(code & 0xffff) << 1;
      long satzLänge = archivIndex[index];
      if (satzLänge > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long von = archivIndex[index + 1];
        long bis = von + satzLänge;
        long mit = 0;

        code = code & 0xffffffffffff0000;

        do
        {
          mit = (von + bis) >> 1;
          if ((archivData[mit] & 0xffffffffffff0000) > code) bis = mit; else von = mit;
        } while (bis - von > 1);

        if ((archivData[von] & 0xffffffffffff0000) == code) // Eintrag gefunden?
        {
          archivData[von] = code | (ulong)(uint)tiefe; // neue Tiefe setzen
          return;
        }
      }
#endif

#if Index24
      int index = (int)(code & 0xffffff) << 1;
      long satzLänge = archivIndex[index];
      if (satzLänge > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long von = archivIndex[index + 1];
        long bis = von + satzLänge;
        long mit = 0;

        code = (code >> 8) & 0x00ffffffffff0000;

        do
        {
          mit = (von + bis) >> 1;
          if (((*(ulong*)(archivData + mit * 7L)) & 0x00ffffffffff0000) > code) bis = mit; else von = mit;
        } while (bis - von > 1);

        if (((*(ulong*)(archivData + von * 7L)) & 0x00ffffffffff0000) == code) // Eintrag gefunden?
        {
          *(ushort*)(archivData + von * 7L) = (ushort)(uint)tiefe; // neue Tiefe setzen
          return;
        }
      }
#endif

      throw new Exception("Hash-Eintrag wurde nicht gefunden!");
    }
    #endregion

    #region # public long HashAnzahl // gibt die Anzahl der Hash-Einträge zurück
    /// <summary>
    /// gibt die Anzahl der Hash-Einträge zurück
    /// </summary>
    public long HashAnzahl
    {
      get
      {
#if Index0
        return (long)hash.Count + weitere.Sum(w => (long)w.Count);
#endif

#if Index16 || Index24
#if multiHash
        return (long)hashes.Sum(h => h.Count) + (long)archivAnzahl;
#else
        return (long)hash.Count + (long)archivAnzahl;
#endif
#endif
      }
    }
    #endregion

    #region # // --- Destruktor ---
    /// <summary>
    /// Destruktor
    /// </summary>
#if Index0
    ~SokowahnHash_Index0()
#endif
#if Index16
#if multiHash
    ~SokowahnHash_Index16Multi()
#else
    ~SokowahnHash_Index16()
#endif
#endif
#if Index24
#if multiHash
    ~SokowahnHash_Index24Multi()
#else
    ~SokowahnHash_Index24()
#endif
#endif
    {
#if Index16 || Index24
      if (archivIndexPointer != IntPtr.Zero)
      {
        Marshal.FreeHGlobal(archivIndexPointer);
        archivIndexPointer = IntPtr.Zero;
      }

      if (archivDataPointer != IntPtr.Zero)
      {
        Marshal.FreeHGlobal(archivDataPointer);
        archivDataPointer = IntPtr.Zero;
      }
#endif
    }
    #endregion

    #region # public override string ToString() // gibt grob den eigentlichen Inhalt der Hashtabelle aus
    /// <summary>
    /// gibt grob den eigentlichen Inhalt der Hashtabelle aus
    /// </summary>
    /// <returns>lesbare Übersicht des Inhaltes</returns>
    public override string ToString()
    {
#if Index0
      return "Hash-Einträge: " + HashAnzahl.ToString("#,##0");
#endif

#if Index16 || Index24
      return "Hash-Einträge: " + HashAnzahl.ToString("#,##0") + " (davon im Archiv: " + archivAnzahl.ToString("#,##0") + ")";
#endif
    }
    #endregion

    /// <summary>
    /// gibt alle gespeidcherten Hasheinträge zurück
    /// </summary>
    /// <returns>Enumerable mit allen Hasheinträgen</returns>
    public IEnumerable<KeyValuePair<ulong, ushort>> GetAll()
    {
#if multiHash
      foreach (var hash in hashes) foreach (var satz in hash) yield return satz;
#else
      foreach (var satz in hash) yield return satz;
#endif
#if Index0
      foreach (var h in weitere) foreach (var satz in h) yield return satz;
#endif

#if Index16 || Index24
      if (archivAnzahl > 0) throw new Exception("todo");
#endif
    }

    public void Remove(ulong key)
    {
#if Index0
      foreach (var h in weitere) h.Remove(key);
#endif

#if Index16 || Index24
      if (archivAnzahl > 0) throw new Exception("todo");
#endif

#if multiHash
      var hash = hashes[key & hashesBits];
#endif

      hash.Remove(key);
    }
  }
}
