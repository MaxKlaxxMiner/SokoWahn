using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sokosolver
{
  public static class Zp
  {
    /// <summary>
    /// Abfrage der sehr präzisen Systemticks (Geschwindigkeit meist abhängig vom Prozessortakt, kann über QueryPerformanceFrequency() ermittelt werden)
    /// </summary>
    /// <param name="performanceCount">Rückgabewert als Zeiger auf einen ulong</param>
    /// <returns>true wenn erfolgreich</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool QueryPerformanceCounter(out ulong performanceCount);

    /// <summary>
    /// Abfrage, wie schnell die Funktion QueryPerformanceCounter() pro Sekunde zählt
    /// </summary>
    /// <param name="frequency">Frequenz in Hz (meist genau der GHz-Takt des Prozessors)</param>
    /// <returns>true wenn erfolgreich</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool QueryPerformanceFrequency(out ulong frequency);

    /// <summary>
    /// merkt sich den Multiplikator zum berechnen der Exakten Millisekunden
    /// </summary>
    static double frequency_merker;

    /// <summary>
    /// gibt einen sehr genauen Tick-Counter in Millisekunden zurück
    /// </summary>
    public static double TickCount
    {
      get
      {
        if (frequency_merker <= 0.0)
        {
          if (frequency_merker == 0.0)
          {
            try
            {
              ulong frequency;
              QueryPerformanceFrequency(out frequency);
              frequency_merker = 1000.0 / frequency;
              QueryPerformanceCounter(out frequency); // Dummy-Zeile, um beim ersten Aufruf die Genauigkeit zu erhöhen
              ulong counter;
              QueryPerformanceCounter(out counter);
              return counter * frequency_merker;
            }
            catch
            {
              frequency_merker = -1000.0 / Stopwatch.Frequency;
              return Stopwatch.GetTimestamp() * -frequency_merker;
            }
          }
          return Stopwatch.GetTimestamp() * -frequency_merker;
        }
        // ReSharper disable once RedundantIfElseBlock
        else
        {
          ulong counter;
          QueryPerformanceCounter(out counter);
          return counter * frequency_merker;
        }
      }
    }
  }
}
