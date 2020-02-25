using System;
using System.Collections.Generic;
using System.Diagnostics;
// ReSharper disable MemberCanBePrivate.Global

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Suchaufgabe
  /// </summary>
  public struct SearchTask
  {
    /// <summary>
    /// Kistenzustand des ersten Raumes
    /// </summary>
    public readonly ulong state1;
    /// <summary>
    /// Kistenzustand des zweiten Raumes
    /// </summary>
    public readonly ulong state2;
    /// <summary>
    /// altes ausgehendes Portal des Spielers (oder uint.MaxValue, wenn es sich um eine End-Variante handelt)
    /// </summary>
    public readonly uint oPortalIndexPlayerOld;
    /// <summary>
    /// merkt sich die Kisten, welche bereits rausgeschoben wurden
    /// </summary>
    public readonly uint[] oPortalIndexBoxes;

    /// <summary>
    /// Anzahl der Laufschritte
    /// </summary>
    public readonly ulong moves;
    /// <summary>
    /// Anzahl der Kistenverschiebungen
    /// </summary>
    public readonly ulong pushes;
    /// <summary>
    /// zurückgelegter Pfad
    /// </summary>
    public readonly string path;

    /// <summary>
    /// Konstruktor für Aufgaben mit reinen Laufwegen
    /// </summary>
    /// <param name="state1">Kistenzustand des ersten Raumes</param>
    /// <param name="state2">Kistenzustand des zweiten Raumes</param>
    /// <param name="oPortalIndexPlayerOld">altes ausgehendes Portal des Spielers</param>
    /// <param name="moves">Anzahl der Laufschritte</param>
    /// <param name="path">zurückgelegter Pfad</param>
    public SearchTask(ulong state1, ulong state2, uint oPortalIndexPlayerOld, ulong moves, string path)
    {
      if (path == null) throw new NullReferenceException("path");
      Debug.Assert(oPortalIndexPlayerOld < uint.MaxValue);
      Debug.Assert(moves > 0);
      Debug.Assert((uint)path.Length == moves);

      this.state1 = state1;
      this.state2 = state2;
      this.oPortalIndexPlayerOld = oPortalIndexPlayerOld;
      oPortalIndexBoxes = new uint[0];
      this.moves = moves;
      pushes = 0;
      this.path = path;
    }

    /// <summary>
    /// Konstruktor für Aufgaben mit Kistenverschiebungen
    /// </summary>
    /// <param name="state1">Kistenzustand des ersten Raumes</param>
    /// <param name="state2">Kistenzustand des zweiten Raumes</param>
    /// <param name="oPortalIndexPlayerOld">altes ausgehendes Portal des Spielers oder uint.MaxValue, wenn der Endzustand erreicht wurde</param>
    /// <param name="oPortalIndexBoxes">Kisten, welche bereits rausgeschoben wurden</param>
    /// <param name="moves">Anzahl der Laufschritte</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen</param>
    /// <param name="path">zurückgelegter Pfad</param>
    public SearchTask(ulong state1, ulong state2, uint oPortalIndexPlayerOld, List<uint> oPortalIndexBoxes, ulong moves, ulong pushes, string path)
    {
      if (oPortalIndexBoxes == null) throw new NullReferenceException("oPortalIndexBoxes");
      if (path == null) throw new NullReferenceException("path");
      Debug.Assert(oPortalIndexPlayerOld < uint.MaxValue && moves > 0 || state1 == 0 && state2 == 0 && pushes > 0);
      Debug.Assert(oPortalIndexBoxes.Count == 0 || pushes > 0);
      Debug.Assert((uint)path.Length == moves);

      this.state1 = state1;
      this.state2 = state2;
      this.oPortalIndexPlayerOld = oPortalIndexPlayerOld;
      this.oPortalIndexBoxes = oPortalIndexBoxes.ToArray();
      this.moves = moves;
      this.pushes = pushes;
      this.path = path;
    }

    /// <summary>
    /// gibt die Prüfsumme der Aufgabe zurück (um Doppler zu filtern)
    /// </summary>
    /// <returns></returns>
    public ulong GetCrc()
    {
      return Crc64.Start.Crc64Update(oPortalIndexPlayerOld)
                        .Crc64Update(oPortalIndexBoxes)
                        .Crc64Update(state1)
                        .Crc64Update(state2);
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { path, moves, pushes, oPortalIndexPlayerOld, boxes = "int[" + oPortalIndexBoxes.Length + "]" }.ToString();
    }
  }
}
