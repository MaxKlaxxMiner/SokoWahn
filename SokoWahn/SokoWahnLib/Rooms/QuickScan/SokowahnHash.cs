#region # using *.*
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Hashsystem um sich bekannte Stellungen zu merken (Crc64)
  /// </summary>
  public unsafe class SokowahnHash
  {
    #region # // --- Variablen ---
    /// <summary>
    /// merkt sich die schnellen Hash-Einträge
    /// </summary>
    Dictionary<ulong, ushort>[] hashDicts;

    /// <summary>
    /// Anzahl der Hasheinträge
    /// </summary>
    const int HashDictCount = 4;

    /// <summary>
    /// Bits, welche benutzt werden müssen
    /// </summary>
    ulong hashBits = HashDictCount - 1;

    /// <summary>
    /// merkt sich die Grenze, wieviel Einträge in einem Dictionary passen
    /// </summary>
    const int DictLimit = 47400000;

    /// <summary>
    /// merkt sich die Gesamtzahl der Archivierten Einträge
    /// </summary>
    long archiveCount;

    /// <summary>
    /// Zeiger für das Array
    /// </summary>
    IntPtr archiveDataPointer = IntPtr.Zero;

    /// <summary>
    /// merkt sich den Index auf das Archiv
    /// </summary>
    readonly uint* archiveIndex;

    /// <summary>
    /// Zeiger für das Array
    /// </summary>
    IntPtr archiveIndexPointer;

    /// <summary>
    /// merkt sich die eigentlichen Archiv-Daten (Rest-Schlüssel und short-Wert)
    /// </summary>
    long archiveData;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    public SokowahnHash()
    {
      hashDicts = new Dictionary<ulong, ushort>[HashDictCount];
      for (int i = 0; i < hashDicts.Length; i++) hashDicts[i] = new Dictionary<ulong, ushort>();

      archiveIndexPointer = Marshal.AllocHGlobal((1 << 24) * 2 * 4);
      if (archiveIndexPointer == IntPtr.Zero) throw new OutOfMemoryException("Speicher konnte nicht reserviert werden (" + (((1 << 24) * 2 * 4) / 1048576L) + " MB)");
      archiveIndex = (uint*)archiveIndexPointer.ToPointer();
      for (int i = 0; i < (1 << 24) * 2; i++) archiveIndex[i] = 0;
    }
    #endregion

    #region # public void Add(ulong code, int depth) // erstellt einen neuen Hash-Eintrag (darf noch nicht vorhanden sein)
    /// <summary>
    /// erstellt einen neuen Hash-Eintrag (darf noch nicht vorhanden sein)
    /// </summary>
    /// <param name="code">Hash-Code, welcher eintragen werden soll</param>
    /// <param name="depth">entsprechende Zugtiefe</param>
    public void Add(ulong code, int depth)
    {
      var hash = hashDicts[code & hashBits];
      hash.Add(code, (ushort)depth);

      if (hash.Count > DictLimit)
      {
        if (archiveCount == 0)
        {
          #region # // --- Archiv das erste mal erstellen ---
          // --- Archiv vorbereiten ---
          archiveCount = hashDicts.Sum(x => (long)x.Count);

          archiveDataPointer = Marshal.AllocHGlobal((IntPtr)(archiveCount * 7L + 1L));
          if (archiveDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + ((archiveCount * 7L + 1L) / 1048576L) + " MB)");
          archiveData = (long)archiveDataPointer.ToPointer();

          var counter = new uint[1 << 24];
          foreach (var h in hashDicts)
          {
            foreach (var x in h) counter[x.Key & 0xffffff]++;
          }
          var posis = new uint[1 << 24];
          uint pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += counter[i - 1];
            posis[i] = pos;
          }

          // --- Archiv befüllen ---
          foreach (var h in hashDicts)
          {
            foreach (var satz in h)
            {
              int indexPos = (int)(satz.Key & 0xffffff);
              var p = (ulong*)(archiveData + posis[indexPos]++ * 7L);
              *p = (*p & 0xff00000000000000) | ((satz.Key >> 8) & 0x00ffffffffff0000) | satz.Value;
            }
          }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += counter[i - 1];
            posis[i] = pos;
          }

          // --- Archiv sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, 1 << 24).Select(i =>
          {
            var tmp = new ulong[counter[i]];
            long offset = posis[i] * 7L + archiveData;
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
          var indexTemp = Enumerable.Range(0, (1 << 24) * 2).Select(i => (i & 1) == 0 ? counter[i >> 1] : posis[i >> 1]).ToArray();

          for (int i = 0; i < indexTemp.Length; i++) archiveIndex[i] = indexTemp[i];
          #endregion
        }
        else
        {
          #region # // --- Archiv erweitern ---
          // --- Neue Daten vorbereiten ---
          int newCount = hash.Count;
          var newData = new ulong[newCount];
          var counter = new uint[1 << 24];
          foreach (var x in hash) counter[x.Key & 0xffffff]++;
          var posis = new uint[1 << 24];
          uint pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += counter[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten befüllen ---
          foreach (var satz in hash)
          {
            int indexPos = (int)(satz.Key & 0xffffff);
            newData[posis[indexPos]++] = ((satz.Key >> 8) & 0x00ffffffffff0000) | satz.Value;
          }

          // --- Positionen neu berechnen ---
          posis[0] = pos = 0;
          for (int i = 1; i < (1 << 24); i++)
          {
            pos += counter[i - 1];
            posis[i] = pos;
          }

          // --- Neue Daten sortieren (Multi-Threaded) ---
          ParallelEnumerable.Range(0, (1 << 24)).Select(i =>
          {
            Array.Sort(newData, (int)posis[i], (int)counter[i]);
            return i;
          }).Count();

          // --- Neues Array erstellen ---
          long oldData = archiveData;
          var oldPointer = archiveDataPointer;
          archiveDataPointer = Marshal.AllocHGlobal((IntPtr)((archiveCount + newCount) * 7L + 1L));
          if (archiveDataPointer == IntPtr.Zero) throw new Exception("Speicher konnte nicht reserviert werden (" + (((archiveCount + newCount) * 7L + 1L) / 1048576L) + " MB)");
          archiveData = (long)archiveDataPointer.ToPointer();

          // --- alte Archiv-Daten kopieren und zusammen mit den neuen Daten verschmelzen ---
          pos = 0;
          for (int index = 0; index < (1 << 24); index++)
          {
            uint posOld = archiveIndex[(index << 1) + 1];
            uint countOld = archiveIndex[index << 1];
            uint posNew = posis[index];
            uint countNew = counter[index];
            uint add = 0;

            while (countOld > 0 && countNew > 0) // beide Tabellen verzahnen und ins neue Archiv speichern
            {
              if (((*(ulong*)(oldData + (long)posOld * 7L)) & 0x00ffffffffffffff) < newData[posNew])
              {
                *(ulong*)(archiveData + (long)pos * 7L) = ((*(ulong*)(archiveData + (long)pos * 7L)) & 0xff00000000000000) | ((*(ulong*)(oldData + (long)posOld * 7L)) & 0x00ffffffffffffff);
                posOld++;
                countOld--;
              }
              else
              {
                *(ulong*)(archiveData + (long)pos * 7L) = ((*(ulong*)(archiveData + (long)pos * 7L)) & 0xff00000000000000) | newData[posNew++];
                countNew--;
              }
              pos++;
              add++;
            }
            while (countOld > 0) // Reste der alten Tabelle übertragen
            {
              *(ulong*)(archiveData + (long)pos * 7L) = ((*(ulong*)(archiveData + (long)pos * 7L)) & 0xff00000000000000) | ((*(ulong*)(oldData + (long)posOld * 7L)) & 0x00ffffffffffffff);
              pos++;
              posOld++;
              add++;
              countOld--;
            }
            while (countNew > 0) // Reste der neuen Tabelle übertragen
            {
              *(ulong*)(archiveData + (long)pos * 7L) = ((*(ulong*)(archiveData + (long)pos * 7L)) & 0xff00000000000000) | newData[posNew++];
              pos++;
              add++;
              countNew--;
            }

            // Index anpassen
            archiveIndex[index << 1] = add;
            archiveIndex[(index << 1) + 1] = pos - add;
          }

          archiveCount = pos;

          Marshal.FreeHGlobal(oldPointer);

          newData = null;
          #endregion
        }

        for (int i = 0; i < hashDicts.Length; i++)
        {
          hashDicts[i] = null; // Hash aufräumen
        }
        hashDicts = new[] { new Dictionary<ulong, ushort>() };
        hashBits = 0;

        GC.Collect();
      }
    }
    #endregion

    #region # public int Get(ulong code) // gibt die Zugtiefe eines Hasheintrages zurück (oder 65535, wenn nicht gefunden)
    /// <summary>
    /// gibt die Zugtiefe eines Hasheintrages zurück (oder 65535, wenn nicht gefunden)
    /// </summary>
    /// <param name="code">Hash-Code, welcher gesucht wird</param>
    /// <returns>entsprechende Zutiefe oder 65535, wenn nicht vorhanden</returns>
    public int Get(ulong code)
    {
      ushort result;

      if (archiveCount == 0)
      {
        var hash = hashDicts[code & hashBits];

        return hash.TryGetValue(code, out result) ? result : 65535;
      }
      if (hashDicts[0].TryGetValue(code, out result)) return result;

      int index = (int)(code & 0xffffff) << 1;
      long indexLen = archiveIndex[index];
      if (indexLen > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long start = archiveIndex[index + 1];
        long end = start + indexLen;

        code = (code >> 8) & 0x00ffffffffff0000;

        do
        {
          long middle = (start + end) >> 1;
          if (((*(ulong*)(archiveData + middle * 7L)) & 0x00ffffffffff0000) > code) end = middle; else start = middle;
        } while (end - start > 1);

        if (((*(ulong*)(archiveData + start * 7L)) & 0x00ffffffffff0000) == code) // Eintrag gefunden?
        {
          return *(ushort*)(archiveData + start * 7L);
        }
      }

      return 65535; // nicht gefunden
    }
    #endregion

    #region # public void Update(ulong code, int depth) // aktualisiert einen Hash-Eintrag (muss vorhanden sein)
    /// <summary>
    /// aktualisiert einen Hash-Eintrag (muss vorhanden sein)
    /// </summary>
    /// <param name="code">Code, welcher bearbeitet werden soll</param>
    /// <param name="depth">neu zu setzende Zugtiefe</param>
    public void Update(ulong code, int depth)
    {
      var hash = hashDicts[code & hashBits];

      if (hash.ContainsKey(code))
      {
        hash[code] = (ushort)depth;
        return;
      }

      int index = (int)(code & 0xffffff) << 1;
      long indexLen = archiveIndex[index];
      if (indexLen > 0)
      {
        // binäre Suche in der kleinen Liste durchführen
        long start = archiveIndex[index + 1];
        long end = start + indexLen;

        code = (code >> 8) & 0x00ffffffffff0000;

        do
        {
          long middle = (start + end) >> 1;
          if (((*(ulong*)(archiveData + middle * 7L)) & 0x00ffffffffff0000) > code) end = middle; else start = middle;
        } while (end - start > 1);

        if (((*(ulong*)(archiveData + start * 7L)) & 0x00ffffffffff0000) == code) // Eintrag gefunden?
        {
          *(ushort*)(archiveData + start * 7L) = (ushort)(uint)depth; // neue Tiefe setzen
          return;
        }
      }

      throw new Exception("Hash-Eintrag wurde nicht gefunden!");
    }
    #endregion

    #region # public long HashCount // gibt die Anzahl der Hash-Einträge zurück
    /// <summary>
    /// gibt die Anzahl der Hash-Einträge zurück
    /// </summary>
    public long HashCount
    {
      get
      {
        return hashDicts.Sum(h => h.Count) + archiveCount;
      }
    }
    #endregion

    #region # // --- Destruktor ---
    /// <summary>
    /// Destruktor
    /// </summary>
    ~SokowahnHash()
    {
      if (archiveIndexPointer != IntPtr.Zero)
      {
        Marshal.FreeHGlobal(archiveIndexPointer);
        archiveIndexPointer = IntPtr.Zero;
      }

      if (archiveDataPointer != IntPtr.Zero)
      {
        Marshal.FreeHGlobal(archiveDataPointer);
        archiveDataPointer = IntPtr.Zero;
      }
    }
    #endregion

    #region # public override string ToString() // gibt grob den eigentlichen Inhalt der Hashtabelle aus
    /// <summary>
    /// gibt grob den eigentlichen Inhalt der Hashtabelle aus
    /// </summary>
    /// <returns>lesbare Übersicht des Inhaltes</returns>
    public override string ToString()
    {
      return "Hash-Einträge: " + HashCount.ToString("#,##0") + " (davon im Archiv: " + archiveCount.ToString("#,##0") + ")";
    }
    #endregion

    /// <summary>
    /// gibt alle gespeicherten Hasheinträge zurück
    /// </summary>
    /// <returns>Enumerable mit allen Hasheinträgen</returns>
    public IEnumerable<KeyValuePair<ulong, ushort>> GetAll()
    {
      foreach (var hash in hashDicts) foreach (var h in hash) yield return h;

      if (archiveCount > 0) throw new Exception("todo");
    }

    /// <summary>
    /// entfernt wieder einen bestimmten Schlüssel
    /// </summary>
    /// <param name="key">Schlüssel, welcher entfernt werden soll</param>
    public void Remove(ulong key)
    {
      if (archiveCount > 0) throw new Exception("todo");

      var hash = hashDicts[key & hashBits];

      hash.Remove(key);
    }
  }
}
