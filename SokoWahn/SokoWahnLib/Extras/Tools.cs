using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
// ReSharper disable UnusedMember.Global

/* * * * * * * *  * *
 *  Quelle: ngMax   *
 * * * * * * * *  * */

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse mit Hilfsmethoden
  /// </summary>
  public static class Tools
  {
    #region # // --- TickRefresh() ---
    /// <summary>
    /// merkt sich alle Ticks pro Thread-ID
    /// </summary>
    static readonly int[] RefreshedThreadTicks = Enumerable.Range(0, 256).Select(x => Environment.TickCount).ToArray();

    /// <summary>
    /// gibt beim nächsten erwarteten Tick ein "true" zurück sonst ein "false" (default: Tickdauer des Betriebssystems, meist 60 Hz)
    /// </summary>
    /// <param name="tickMs">optional: Tick in Millisekunden (Default: Tickdauer des Betriebssystems, meist 60 Hz)</param>
    /// <param name="longTickCut">optional: gibt an, ob zu langsame Ticks geschnitten werden sollen (default: false, bei Betriebssystems-Ticks nicht wirksam)</param>
    /// <returns>"true", wenn ein Tick erreicht wurde, sonst "false"</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TickRefresh(int tickMs = 0, bool longTickCut = false)
    {
      int nextTick = Environment.TickCount + tickMs;

      int val = RefreshedThreadTicks[(byte)Thread.CurrentThread.ManagedThreadId];

      // --- Tick noch am laufen? ---
      if (val >= nextTick) return false;

      if (tickMs == 0) // Betriebsystem-Ticks benutzen?
      {
        RefreshedThreadTicks[(byte)Thread.CurrentThread.ManagedThreadId] = nextTick;
      }
      else
      {
        val += tickMs;
        if (longTickCut && val < nextTick - tickMs) val = nextTick; // muss Tick gekürzt werden?
        RefreshedThreadTicks[(byte)Thread.CurrentThread.ManagedThreadId] = val;
      }

      return true;
    }
    #endregion
  }
}
