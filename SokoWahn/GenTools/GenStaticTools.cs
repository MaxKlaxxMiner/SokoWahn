
#region # using *.*

// ReSharper disable ConvertToLambdaExpressionWhenPossible

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum generieren der Quellcodes statischen Cs-Dateien
  /// </summary>
  static class GenStaticTools
  {
    #region # public static CsFile GenSokoTools(string projectName) // generiert die allgemeinen SokoTools
    /// <summary>
    /// generiert die allgemeinen SokoTools
    /// </summary>
    /// <param name="projectName">Name/Namespace des Projektes</param>
    /// <returns>fertig zusammengestellte Cs-Datei</returns>
    public static CsFile GenSokoTools(string projectName)
    {
      var csSokoTools = new CsFile();

      #region # // --- using *.* ---
      csSokoTools.Write();
      csSokoTools.Write();
      csSokoTools.Write("#region # using *.*");
      csSokoTools.Write();
      csSokoTools.Write("using System.Text;");
      csSokoTools.Write("using System.Linq;");
      csSokoTools.Write("using System.Collections.Generic;");
      csSokoTools.Write("using System.Runtime.CompilerServices;");
      csSokoTools.Write();
      csSokoTools.Write("#endregion");
      csSokoTools.Write();
      csSokoTools.Write();
      #endregion

      csSokoTools.Write("namespace " + projectName, ns =>
      {
        ns.Write("static class SokoTools", cl =>
        {
          #region # // --- IEnumerable<int[]> FieldBoxesVariants(int fieldCount, int boxesCount) ---
          cl.Write("public static IEnumerable<int[]> FieldBoxesVariants(int fieldCount, int boxesCount)", m =>
          {
            m.Write("int dif = fieldCount - boxesCount;");
            m.Write("int end = boxesCount - 1;");
            m.Write();
            m.Write("var boxesVariant = new int[boxesCount];");
            m.Write();
            m.Write("for (int box = 0; ; )", f =>
            {
              f.Write("while (box < end) boxesVariant[box + 1] = boxesVariant[box++] + 1;");
              f.Write("yield return boxesVariant;");
              f.Write("while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;");
            });
          });
          cl.Write();
          #endregion

          #region # // --- Crc64-Tools ---
          cl.Write("public const ulong CrcStart = 0xcbf29ce484222325u;");
          cl.Write("const ulong CrcMul = 0x100000001b3;");
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("public static ulong CrcCompute(ulong crc64, int value)", crcf =>
          {
            cl.Write("return (crc64 ^ (uint)value) * CrcMul;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("public static ulong CrcCompute(ulong crc64, ushort[] buffer, int ofs, int len)", crcf =>
          {
            crcf.Write("crc64 ^= buffer[ofs];");
            crcf.Write("for (int i = 1; i < len; i++) crc64 = crc64 * CrcMul ^ buffer[i + ofs];");
            crcf.Write("return crc64 * CrcMul;");
          });
          cl.Write();
          #endregion

          #region # // --- TxtView() ---
          cl.Write("public static string TxtView(char[] fieldData, int fieldWidth, ushort[] fieldTargets, int playerPos)", tx =>
          {
            tx.Write("var output = new StringBuilder();");
            tx.Write("for (int i = 0; i < fieldData.Length - 1; i++)", f =>
            {
              f.Write("bool target = fieldTargets.Any(x => x == i);");
              f.Write("bool player = playerPos == i;");
              f.Write("switch (fieldData[i])", sw =>
              {
                sw.Write("case ' ': output.Append(target ? (player ? '+' : '.') : (player ? '@' : ' ')); break;");
                sw.Write("case '#': output.Append('#'); break;");
                sw.Write("default: output.Append(target ? '*' : '$'); break;");
              });
              f.Write("if (i % fieldWidth == fieldWidth - 1) output.AppendLine();");
            });
            tx.Write("output.Append(fieldData[fieldData.Length - 2]).AppendLine().AppendLine();");
            tx.Write("return output.ToString();");
          });
          #endregion
        });
      });

      return csSokoTools;
    }
    #endregion

    #region # public static CsFile GenDictionaryFastCrc(string projectName) // generiert den Code vom schnelleren Dictionary-Ersatz
    /// <summary>
    /// generiert den Code vom schnelleren Dictionary-Ersatz
    /// </summary>
    /// <param name="projectName">Name/Namespace des Projektes</param>
    /// <returns>fertig zusammengestellte Cs-Datei</returns>
    public static CsFile GenDictionaryFastCrc(string projectName)
    {
      var csDictFast = new CsFile();

      #region # // --- using *.* ---
      csDictFast.Write();
      csDictFast.Write();
      csDictFast.Write("#region # using *.*");
      csDictFast.Write();
      csDictFast.Write("using System;");
      csDictFast.Write("using System.Runtime.CompilerServices;");
      csDictFast.Write();
      csDictFast.Write("// ReSharper disable UnusedMember.Global");
      csDictFast.Write();
      csDictFast.Write("#endregion");
      csDictFast.Write();
      csDictFast.Write();
      #endregion

      csDictFast.Write("namespace " + projectName, ns =>
      {
        ns.Write("sealed class DictionaryFastCrc<TValue> where TValue : struct", cl =>
        {
          cl.Write("private int[] buckets = new int[1];");
          cl.Write("private ulong bucketsMask;");
          cl.Write("private Entry[] entries = new Entry[1];");
          cl.Write("private int count;");
          cl.Write("private int freeList;");
          cl.Write("private int freeCount;");
          cl.Write("internal int Count { get { return count - freeCount; } }");
          cl.Write("public TValue this[ulong key]", th =>
          {
            th.Write("get", g =>
            {
              g.Write("int entry = FindEntry(key);");
              g.Write("if (entry >= 0) return entries[entry].value;");
              g.Write("throw new Exception(\"key not found\");");
            });
            th.Write("set { Insert(key, value, false); }");
          });
          cl.Write();
          cl.Write("internal DictionaryFastCrc(int capacity = 1) { Initialize(Math.Max(1, capacity)); }");
          cl.Write();
          cl.Write("internal void Add(ulong key, TValue value) { Insert(key, value, true); }");
          cl.Write();
          cl.Write("public void Clear()", f =>
          {
            f.Write("if (count <= 0) return;");
            f.Write("Array.Clear(buckets, 0, buckets.Length);");
            f.Write("Array.Clear(entries, 0, count);");
            f.Write("freeList = -1;");
            f.Write("count = 0;");
            f.Write("freeCount = 0;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("internal bool ContainsKey(ulong key)", f =>
          {
            f.Write("for (int index = buckets[key & bucketsMask]; index != 0; index = entries[index].next) if (entries[index].key == key) return true;");
            f.Write("return false;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("private int FindEntry(ulong key)", f =>
          {
            f.Write("for (int index = buckets[key & bucketsMask]; index != 0; index = entries[index].next) if (entries[index].key == key) return index;");
            f.Write("return -1;");
          });
          cl.Write();
          cl.Write("static int GetDouble(int min)", f =>
          {
            f.Write("int dub = 1;");
            f.Write("while (dub < min) dub *= 2;");
            f.Write("return dub;");
          });
          cl.Write();
          cl.Write("private void Initialize(int capacity)", f =>
          {
            f.Write("int prime = GetDouble(capacity);");
            f.Write("buckets = new int[prime];");
            f.Write("bucketsMask = (ulong)(buckets.Length - 1);");
            f.Write("entries = new Entry[prime];");
            f.Write("freeList = -1;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("private void Insert(ulong key, TValue value, bool add)", f =>
          {
            f.Write("var index1 = key & bucketsMask;");
            f.Write("int num2 = 0;");
            f.Write("for (int index2 = buckets[index1]; index2 != 0; index2 = entries[index2].next)", fr =>
            {
              fr.Write("if (entries[index2].key == key)", i =>
              {
                i.Write("if (add) throw new ArgumentException();");
                i.Write("entries[index2].value = value;");
                i.Write("return;");
              });
              fr.Write("++num2;");
            });
            f.Write("int index3;");
            f.Write("if (freeCount > 0)", i =>
            {
              i.Write("index3 = freeList;");
              i.Write("freeList = entries[index3].next;");
              i.Write("--freeCount;");
            });
            f.Write("else", e =>
            {
              e.Write("if (count == entries.Length)", i =>
              {
                i.Write("Resize(GetDouble(count + 1));");
                i.Write("index1 = key & bucketsMask;");
              });
              e.Write("index3 = count;");
              e.Write("++count;");
            });
            f.Write("entries[index3].next = buckets[index1];");
            f.Write("entries[index3].key = key;");
            f.Write("entries[index3].value = value;");
            f.Write("buckets[index1] = index3;");
            f.Write("if (num2 <= 100) return;");
            f.Write("Resize(entries.Length);");
          });
          cl.Write();
          cl.Write("private void Resize(int newSize)", f =>
          {
            f.Write("var numArray = new int[newSize];");
            f.Write("var entryArray = new Entry[newSize];");
            f.Write("Array.Copy(entries, 0, entryArray, 0, count);");
            f.Write("for (int index1 = 0; index1 < count; ++index1)", fr =>
            {
              fr.Write("var index2 = entryArray[index1].key & ((ulong)newSize - 1);");
              fr.Write("entryArray[index1].next = numArray[index2];");
              fr.Write("numArray[index2] = index1;");
            });
            f.Write("buckets = numArray;");
            f.Write("bucketsMask = (ulong)(buckets.Length - 1);");
            f.Write("entries = entryArray;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("public bool TryGetValue(ulong key, out TValue value)", f =>
          {
            f.Write("int entry = FindEntry(key);");
            f.Write("if (entry >= 0)", i =>
            {
              i.Write("value = entries[entry].value;");
              i.Write("return true;");
            });
            f.Write("value = default(TValue);");
            f.Write("return false;");
          });
          cl.Write();
          cl.Write("private struct Entry", f =>
          {
            f.Write("public ulong key;");
            f.Write("public int next;");
            f.Write("public TValue value;");
          });
        });
      });

      return csDictFast;
    }
    #endregion
  }
}
