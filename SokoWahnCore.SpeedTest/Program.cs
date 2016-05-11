#region # using *.*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable ForCanBeConvertedToForeach

#endregion

namespace SokoWahnCore.SpeedTest
{
  static unsafe class Program
  {
    const int Repeats = 3;

    #region # // --- Test(...) - Base ---
    /// <summary>
    /// Hilfsmethode zum testen der Geschwindigkeit
    /// </summary>
    /// <param name="name">Name des Tests</param>
    /// <param name="testMethod">Methode, welche den Test durchführt (sendet einen eindeutigen Rückgabe-Wert zurück)</param>
    /// <param name="targetResult">Rückgabe-Wert, welcher erwartet wird</param>
    static void Test(string name, Func<int> testMethod, int targetResult)
    {
      var stop = new Stopwatch();
      for (int r = 0; r < Repeats; r++)
      {
        Console.Write(" ({0} / {1}) {2}: ", (r + 1), Repeats, name);
        stop.Restart();
        long result = testMethod();
        stop.Stop();
        if (result == targetResult)
        {
          Console.ForegroundColor = ConsoleColor.Green;
          Console.Write("[ok]");
        }
        else
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.Write("[error] ");
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.Write("{0} != {1}", targetResult, result);
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(", {0} ms", stop.ElapsedMilliseconds.ToString("N0"));
        Console.ForegroundColor = ConsoleColor.Gray;
      }
      Console.WriteLine();
    }
    #endregion

    #region # // --- CrcTest() ---
    static void CrcTest()
    {
      Console.WriteLine();
      Console.WriteLine(" --- CrcTest() ---");
      Console.WriteLine();

      const int Div = 1048576;

      const int LoopCount = 100 * Div;

      var buffer = new ushort[1048576 * 4 / Div];
      var rndBuffer = new byte[buffer.Length * sizeof(ushort)];

      new Random(12345).NextBytes(rndBuffer);
      Buffer.BlockCopy(rndBuffer, 0, buffer, 0, buffer.Length);

      Test("Crc64 Default              ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          result = Crc64.Start;
          for (int i = 0; i < buffer.Length; i++)
          {
            result = result.Crc64Update(buffer[i]);
          }
        }
        return (int)(uint)result;
      }, -1695961532);

      Test("Crc64 Inline               ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          result = Crc64.Start;
          for (int i = 0; i < buffer.Length; i++)
          {
            result = (result ^ buffer[i]) * Crc64.Mul;
          }
        }
        return (int)(uint)result;
      }, -1695961532);

      Test("Crc64 Unsafe               ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          fixed (ushort* buf = &buffer[0])
          {
            int len = buffer.Length;
            result = Crc64.Start;
            for (int i = 0; i < len; i++)
            {
              result = (result ^ buf[i]) * Crc64.Mul;
            }
          }
        }
        return (int)(uint)result;
      }, -1695961532);

      Test("Crc64 Re-Order             ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          result = Crc64.Start ^ buffer[0];
          for (int i = 1; i < buffer.Length; i++)
          {
            result = result * Crc64.Mul ^ buffer[i];
          }
          result *= Crc64.Mul;
        }
        return (int)(uint)result;
      }, -1695961532);

      Test("Crc64 Re-Order Unsafe      ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          fixed (ushort* buf = &buffer[0])
          {
            int len = buffer.Length;
            result = Crc64.Start ^ buf[0];
            for (int i = 1; i < len; i++)
            {
              result = result * Crc64.Mul ^ buf[i];
            }
            result *= Crc64.Mul;
          }
        }
        return (int)(uint)result;
      }, -1695961532);

      Test("Crc64 Final (Best)         ", () =>
      {
        ulong result = 0;
        for (int l = 0; l < LoopCount; l++)
        {
          result = Crc64.Start.Crc64Update(buffer, 0, buffer.Length);
        }
        return (int)(uint)result;
      }, -1695961532);
    }
    #endregion

    const string TestLevel =
      "        ######## \n" +
      "        #     @# \n" +
      "        # $#$ ## \n" +
      "        # $  $#  \n" +
      "        ##$ $ #  \n" +
      "######### $ # ###\n" +
      "#....  ## $  $  #\n" +
      "##...    $  $   #\n" +
      "#....  ##########\n" +
      "########         \n";

    #region # // --- WalkTest() ---
    static void WalkTest(string level)
    {
      // --- Basis-Feld einlesen ---
      int width = TestLevel.IndexOf('\n');
      int height = TestLevel.Count(c => c == '\n');
      var fieldsBase = TestLevel.Where(c => c != '\n').ToArray();
      if (fieldsBase.Length != width * height) throw new ArgumentException("level size");
      var fieldsFree = new bool[fieldsBase.Length];

      // --- Spieler suchen ---
      var posis = fieldsBase.WhereGetIndices(c => c == '@' || c == '+').ToArray();

      if (posis.Length < 1) throw new ArgumentException("player not found");
      if (posis.Length > 1) throw new ArgumentException("duplicate players");

      int playerStart = posis[0];

      // --- begehbare Bereiche suchen ---
      var search = new Stack<int>();
      search.Push(playerStart);
      while (search.Count > 0)
      {
        int next = search.Pop();
        if (fieldsBase[next] == '#' || fieldsFree[next]) continue;
        fieldsFree[next] = true;

        if (next % width > 0) search.Push(next - 1);                     // links
        if (next % width < width - 1) search.Push(next + 1);             // rechts
        if (next - width >= 0) search.Push(next - width);                // hoch
        if (next + width < fieldsBase.Length) search.Push(next + width); // runter
      }

      var testPosis = fieldsFree.WhereGetIndices(b => b).ToArray();



    }
    #endregion


    static void Main(string[] args)
    {
      if (args.Length == 0) args = new[] { "walk" };

      foreach (var speedTest in args)
      {
        switch (speedTest.ToLower())
        {
          case "crc": CrcTest(); break;
          case "walk": WalkTest(TestLevel); break;

          default: throw new Exception("Speed-Test unknown: \"" + speedTest + "\"");
        }
      }

      if (Environment.CommandLine.Contains(".vshost.exe")) Console.ReadLine();
    }
  }
}
