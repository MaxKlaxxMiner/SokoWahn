#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SokoWahnLib;
using SokoWahnLib.Rooms;
using SokoWahnWin;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 169
#endregion

namespace SokoWahnTest
{
  static class Program
  {
    #region # public struct ChainTask // einzelne Aufgabe in der Kette
    /// <summary>
    /// einzelne Aufgabe in der Kette
    /// </summary>
    public struct ChainTask
    {
      /// <summary>
      /// Portal, worüber der Spieler den Raum betritt (uint.MaxValue = Spieler startet im Raum)
      /// </summary>
      public readonly uint iPortalIndex;
      /// <summary>
      /// Portal, worüber der Spieler den Raum wieder verlässt (uint.MaxValue = Spieler verbleibt im Raum)
      /// </summary>
      public readonly uint oPortalIndex;
      /// <summary>
      /// Kisten, welche reingeschoben wurden (markierte Bits der Portale)
      /// </summary>
      public readonly ulong iBoxesBits;
      /// <summary>
      /// Kisten, welche rausgeschoben wurden (markierte Bits der Portale)
      /// </summary>
      public readonly ulong oBoxesBits;

      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="iPortalIndex">Portal, worüber der Spieler den Raum betritt (uint.MaxValue = Spieler startet im Raum)</param>
      /// <param name="oPortalIndex">Portal, worüber der Spieler den Raum wieder verlässt (uint.MaxValue = Spieler verbleibt im Raum)</param>
      /// <param name="iBoxesBits">Kisten, welche reingeschoben wurden (markierte Bits der Portale)</param>
      /// <param name="oBoxesBits">Kisten, welche rausgeschoben wurden (markierte Bits der Portale)</param>
      public ChainTask(uint iPortalIndex, uint oPortalIndex, ulong iBoxesBits, ulong oBoxesBits)
      {
        this.iPortalIndex = iPortalIndex;
        this.oPortalIndex = oPortalIndex;
        this.iBoxesBits = iBoxesBits;
        this.oBoxesBits = oBoxesBits;
      }
      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { iBoxes = Convert.ToString((long)iBoxesBits, 2), iPortalIndex, oBoxes = Convert.ToString((long)oBoxesBits, 2), oPortalIndex }.ToString();
      }
    }
    #endregion

    #region # public struct RealTask // speichert eine gefundene Lösung der zugeordneten Aufgabe
    /// <summary>
    /// speichert eine gefundene Lösung der zugeordneten Aufgabe
    /// </summary>
    public struct SolutionTask
    {
      /// <summary>
      /// merkt sich die Variante, welche durchgeführt wurde
      /// </summary>
      public readonly ulong variant;
      /// <summary>
      /// merkt sich den Kistenzustand am Ende der Aufgabe
      /// </summary>
      public readonly ulong state;

      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="variant">Variante, welche durchgeführt wurde</param>
      /// <param name="state">Kistenzustand am Ende der Aufgabe</param>
      public SolutionTask(ulong variant, ulong state)
      {
        this.variant = variant;
        this.state = state;
      }

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { variant, state }.ToString();
      }
    }
    #endregion

    #region # public sealed class ChainVariant // merkt sich eine komplette Ketten-Variante
    /// <summary>
    /// merkt sich eine komplette Ketten-Variante
    /// </summary>
    public sealed class ChainVariant
    {
      /// <summary>
      /// merkt sich die aktuelle Anzahl der Kisten
      /// </summary>
      public readonly uint boxes;
      /// <summary>
      /// merkt sich die Kette der abzuarbeitenden Aufgaben
      /// </summary>
      public readonly ChainTask[] tasks;
      /// <summary>
      /// merkt sich die gefundenen dazugehörigen Lösungen
      /// </summary>
      public readonly List<List<SolutionTask>> solutions;
      /// <summary>
      /// Konstruktor (für neue Aufgaben-Ketten)
      /// </summary>
      /// <param name="startBoxes">Anzahl der zu startenen Kisten</param>
      public ChainVariant(uint startBoxes)
      {
        boxes = startBoxes;
        tasks = new ChainTask[0];
        solutions = new List<List<SolutionTask>>();
      }
      /// <summary>
      /// Konstruktor (für neue Aufgaben-Ketten mit erster Start-Variante)
      /// </summary>
      /// <param name="startBoxes">Anzahl der zu startenen Kisten</param>
      /// <param name="startTask">erste Aufgabe in der Kette</param>
      public ChainVariant(uint startBoxes, ChainTask startTask)
      {
        Debug.Assert((int)(startBoxes + Tools.BitCount(startTask.iBoxesBits) - Tools.BitCount(startTask.oBoxesBits)) >= 0);
        boxes = startBoxes + Tools.BitCount(startTask.iBoxesBits) - Tools.BitCount(startTask.oBoxesBits);
        tasks = new[] { startTask };
        solutions = new List<List<SolutionTask>>();
      }
      /// <summary>
      /// Konstruktor (Kopien erstellen von bestehenden Aufgaben-Ketten)
      /// </summary>
      /// <param name="oldChain">Aufgaben-Kette, welche kopiert werden soll</param>
      /// <param name="nextTask">neue hinzugefügte Aufgabe</param>
      public ChainVariant(ChainVariant oldChain, ChainTask nextTask)
      {
        Debug.Assert((int)(oldChain.boxes + Tools.BitCount(nextTask.iBoxesBits) - Tools.BitCount(nextTask.oBoxesBits)) >= 0);

        boxes = oldChain.boxes + Tools.BitCount(nextTask.iBoxesBits) - Tools.BitCount(nextTask.oBoxesBits);
        tasks = new ChainTask[oldChain.tasks.Length + 1];
        Array.Copy(oldChain.tasks, tasks, oldChain.tasks.Length);
        tasks[oldChain.tasks.Length] = nextTask;
        solutions = oldChain.solutions.Select(x => x.ToList()).ToList();
      }
      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { boxes, tasks = tasks.Length, solutions = solutions.Count }.ToString();
      }
    }
    #endregion

    static void UpdateChainSolutions(ChainVariant chain, Room room)
    {

    }

    static void Main()
    {
      var testRooms = FormDebugger.CreateTestRooms();

      var room = testRooms.rooms[1];

      uint portalCount = (uint)room.incomingPortals.Length;
      ulong portalMask = 1; // Bitmaske je nach Anzahl der Portale (theoretisch 63 Portale möglich)
      for (uint i = 0; i < portalCount; i++) portalMask <<= 1;

      uint minBoxes = uint.MaxValue;
      uint maxBoxes = 0;
      uint startBoxes = 0;
      uint endBoxes = 0;

      foreach (var state in room.stateList)
      {
        uint boxes = (uint)state.Value.Length;
        if (boxes > maxBoxes) maxBoxes = boxes;
        if (boxes < minBoxes) minBoxes = boxes;
        if (state.Key == room.startState) startBoxes = boxes;
        if (state.Key == 0) endBoxes = boxes;
      }

      var finalVariants = new List<ChainVariant>(); // fertig berechnete Varianten
      var variants = new List<ChainVariant>(); // noch abzuarbeitende Varianten

      #region # // --- Startaufgaben erstellen ---
      if (startBoxes == endBoxes) finalVariants.Add(new ChainVariant(startBoxes)); // End-Zustand hinzufügen (ohne etwas zu tun)
      if (room.startVariantCount > 0)
      {
        throw new NotImplementedException("debug-check");
        for (ulong outputBoxMask = 0; outputBoxMask < portalMask; outputBoxMask++)
        {
          uint outputBoxes = Tools.BitCount(outputBoxMask);
          if (outputBoxes > startBoxes) continue; // zuviele Kisten rausgeschoben
          uint nextBoxes = startBoxes - outputBoxes;
          for (uint oPortalIndex = 0; oPortalIndex < portalCount; oPortalIndex++)
          {
            finalVariants.Add(new ChainVariant(startBoxes, new ChainTask(uint.MaxValue, uint.MaxValue, 0, outputBoxMask))); // direkt erreichbaren End-Zustand hinzufügen
          }
          if (nextBoxes >= minBoxes && nextBoxes <= maxBoxes)
          {
            for (uint oPortalIndex = 0; oPortalIndex < portalCount; oPortalIndex++)
            {
              variants.Add(new ChainVariant(startBoxes, new ChainTask(uint.MaxValue, oPortalIndex, 0, outputBoxMask)));
              if (nextBoxes == endBoxes)
              {
                finalVariants.Add(new ChainVariant(startBoxes, new ChainTask(uint.MaxValue, oPortalIndex, 0, outputBoxMask))); // direkt erreichbaren End-Zustand hinzufügen (mit Verlassen des Raumes)
              }
            }
          }
        }
      }
      else
      {
        for (ulong inputBoxMask = 0; inputBoxMask < portalMask; inputBoxMask++)
        {
          uint inputBoxes = Tools.BitCount(inputBoxMask);
          uint boxes = startBoxes + inputBoxes;
          if (boxes > maxBoxes && boxes != endBoxes) continue; // zuviele Kisten reingeschoben
          for (ulong outputBoxMask = 0; outputBoxMask < portalMask; outputBoxMask++)
          {
            uint outputBoxes = Tools.BitCount(outputBoxMask);
            if (outputBoxes > boxes) continue; // zuviele Kisten rausgeschoben
            uint nextBoxes = boxes - outputBoxes;
            if (nextBoxes == endBoxes)
            {
              for (uint iPortalIndex = 0; iPortalIndex < portalCount; iPortalIndex++)
              {
                finalVariants.Add(new ChainVariant(startBoxes, new ChainTask(iPortalIndex, uint.MaxValue, inputBoxMask, outputBoxMask))); // direkt erreichbaren End-Zustand hinzufügen
              }
            }
            if (nextBoxes >= minBoxes && nextBoxes <= maxBoxes)
            {
              for (uint iPortalIndex = 0; iPortalIndex < portalCount; iPortalIndex++)
              {
                for (uint oPortalIndex = 0; oPortalIndex < portalCount; oPortalIndex++)
                {
                  if (inputBoxes > 0 || outputBoxes > 0 || iPortalIndex != oPortalIndex)
                  {
                    variants.Add(new ChainVariant(startBoxes, new ChainTask(iPortalIndex, oPortalIndex, inputBoxMask, outputBoxMask)));
                  }
                  if (nextBoxes == endBoxes)
                  {
                    finalVariants.Add(new ChainVariant(startBoxes, new ChainTask(iPortalIndex, oPortalIndex, inputBoxMask, outputBoxMask))); // direkt erreichbaren End-Zustand hinzufügen (mit Verlassen des Raumes)
                  }
                }
              }
            }
          }
        }
      }
      #endregion

      foreach (var f in finalVariants)
      {
        UpdateChainSolutions(f, room);
      }

      // todo 1: reale Lösungen für bereits gefundene End-Ketten zuordnen und ineffizienteste Ketten entfernen

      // todo 2: schrittweise-Lösungen für bestehende Ketten zuordnen und nicht erreichbare Ketten entfernen

      // todo 3: bestehende Ketten weiter vertiefen und ggf. Schleifen beachten, neue End-Ketten erzeugen -> weiter mit todo 1

      // todo 4: Ketten durchsuchen und benötigte Varianten merken

      // todo 5: gleichgute Ketten vergleichen und die Ketten entfernen, welche die meisten Varianten entfernen könnten

    }

  }
}
