#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SokoWahnWin;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 169
#endregion

namespace SokoWahnTest
{
  class Program
  {
    /// <summary>
    /// einzelne Aufgabe der Kette
    /// </summary>
    public struct ChainTask
    {
      /// <summary>
      /// Portal, worüber der Spieler den Raum betritt (uint.MaxValue = Spieler startet im Raum)
      /// </summary>
      public uint iPortalIndex;
      /// <summary>
      /// Portal, worüber der Spieler den Raum wieder verlässt (uint.MaxValue = Spieler verbleibt im Raum)
      /// </summary>
      public uint oPortalIndex;
      /// <summary>
      /// Kisten, welche reingeschoben wurden (markierte Bits der Portale)
      /// </summary>
      public ulong iBoxesBits;
      /// <summary>
      /// Kisten, welche rausgeschoben wurden (markierte Bits der Portale)
      /// </summary>
      public ulong oBoxesBits;
    }

    /// <summary>
    /// merkt sich eine komplette Ketten-Variante
    /// </summary>
    public class ChainVariant
    {
      public readonly List<ChainTask> tasks = new List<ChainTask>();
    }

    static void Main(string[] args)
    {
      var testRooms = FormDebugger.CreateTestRooms();

      var variants = new List<ChainVariant>();
    }

  }
}
