#region # using *.*

using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace Sokosolver
{
  internal static class Zp
  {
    /// <summary>
    /// Abfrage der sehr präzisen Systemticks (Geschwindigkeit meist abhängig vom Prozessortakt, kann über QueryPerformanceFrequency() ermittelt werden)
    /// </summary>
    /// <param name="performanceCount">Rückgabewert als Zeiger auf einen ulong</param>
    /// <returns>true wenn erfolgreich</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryPerformanceCounter(out ulong performanceCount);

    /// <summary>
    /// Abfrage, wie schnell die Funktion QueryPerformanceCounter() pro Sekunde zählt
    /// </summary>
    /// <param name="frequency">Frequenz in Hz (meist genau der GHz-Takt des Prozessors)</param>
    /// <returns>true wenn erfolgreich</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryPerformanceFrequency(out ulong frequency);

    /// <summary>
    /// merkt sich den Multiplikator zum berechnen der Exakten Millisekunden
    /// </summary>
    static double frequencyMerker;

    /// <summary>
    /// gibt einen sehr genauen Tick-Counter in Millisekunden zurück
    /// </summary>
    public static double TickCount
    {
      get
      {
        if (frequencyMerker <= 0.0)
        {
          if (frequencyMerker == 0.0)
          {
            try
            {
              ulong frequency;
              QueryPerformanceFrequency(out frequency);
              frequencyMerker = 1000.0 / frequency;
              QueryPerformanceCounter(out frequency); // Dummy-Zeile, um beim ersten Aufruf die Genauigkeit zu erhöhen
              ulong counter;
              QueryPerformanceCounter(out counter);
              return counter * frequencyMerker;
            }
            catch
            {
              frequencyMerker = -1000.0 / Stopwatch.Frequency;
              return Stopwatch.GetTimestamp() * -frequencyMerker;
            }
          }
          return Stopwatch.GetTimestamp() * -frequencyMerker;
        }
        // ReSharper disable once RedundantIfElseBlock
        else
        {
          ulong counter;
          QueryPerformanceCounter(out counter);
          return counter * frequencyMerker;
        }
      }
    }
  }
}
