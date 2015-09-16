#region # using *.*

using System;
using System.Diagnostics;
using System.Linq;
// ReSharper disable ForCanBeConvertedToForeach

#endregion

namespace SokoWahnCore.SpeedTest
{
  static class Program
  {
    const int Repeats = 5;

    #region # // --- CrcTest() ---
    static void CrcTest()
    {
      Console.WriteLine();
      Console.WriteLine(" --- CrcTest() ---");
      Console.WriteLine();

      int loop = 100;

      var buffer = new ulong[1048576 * 4];
      var rndBuffer = new byte[buffer.Length * sizeof(ulong)];

      new Random(12345).NextBytes(rndBuffer);
      Buffer.BlockCopy(rndBuffer, 0, buffer, 0, buffer.Length);

      Test("GetHashCode() - ForEach", () =>
      {
        int result = 0;
        for (int l = 0; l < loop; l++)
        {
          foreach (var b in buffer)
          {
            result += b.GetHashCode();
          }
        }
        return result;
      }, 67378320);

      Test("GetHashCode() - For(i) ", () =>
      {
        int result = 0;
        for (int l = 0; l < loop; l++)
        {
          for (int i = 0; i < buffer.Length; i++)
          {
            result += buffer[i].GetHashCode();
          }
        }
        return result;
      }, 67378320);
    }
    #endregion

    #region # // --- Test(...) ---
    /// <summary>
    /// Hilfsmethode zum testen der Gewindigkeit
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
        Console.WriteLine(", {0} ms", stop.ElapsedMilliseconds.ToString("#,##0"));
        Console.ForegroundColor = ConsoleColor.Gray;
      }
      Console.WriteLine();
    }
    #endregion

    static void Main(string[] args)
    {
      if (args.Length == 0) args = new[] { "crc" };

      foreach (var speedTest in args)
      {
        switch (speedTest.ToLower())
        {
          case "crc": CrcTest(); break;
          default: throw new Exception("Speed-Test unknown: \"" + speedTest + "\"");
        }
      }

      if (Environment.CommandLine.Contains(".vshost.exe")) Console.ReadLine();
    }
  }
}
