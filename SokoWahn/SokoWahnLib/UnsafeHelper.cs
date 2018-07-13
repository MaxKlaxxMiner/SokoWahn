#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable RedundantUnsafeContext
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Klasse mit Hilfsmethoden zum arbeiten mit Zeigern und direkten Speicherbereichen
  /// </summary>
  internal static unsafe class UnsafeHelper
  {
    /// <summary>
    /// schnelle Methode um eine neue leere Zeichenkette zu erstellen
    /// </summary>
    public static readonly Func<int, string> FastAllocateString = GetFastAllocateString();

    /// <summary>
    /// gibt die Methode "string.FastAllocateString" zurück
    /// </summary>
    /// <returns>Delegate auf die Methode</returns>
    static Func<int, string> GetFastAllocateString()
    {
      try
      {
        return (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), typeof(string).GetMethod("FastAllocateString", BindingFlags.NonPublic | BindingFlags.Static));
      }
      catch
      {
        return count => new string('\0', count); // Fallback 
      }
    }
  }
}
