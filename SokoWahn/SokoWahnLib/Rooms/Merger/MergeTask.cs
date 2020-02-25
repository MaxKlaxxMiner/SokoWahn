﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Aufgabe zum verschmelzen zweier Räume
  /// </summary>
  public struct MergeTask
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
    /// Anzahl der Laufschritte insgesamt
    /// </summary>
    public readonly ulong moves;
    /// <summary>
    /// Anzahl der Kistenverschiebungen insgesamt
    /// </summary>
    public readonly ulong pushes;
    /// <summary>
    /// zurückgelegter Pfad insgesamt
    /// </summary>
    public readonly string path;
    /// <summary>
    /// alle Kisten, welche bisher rausgeschoben wurden
    /// </summary>
    public readonly uint[] oPortalBoxes;

    /// <summary>
    /// gibt an, ob der erste Raum die Basis darstellt (sonst: zweite Raum)
    /// </summary>
    public readonly bool mainRoom1;

    /// <summary>
    /// Nummer des eingehenden Portales (oder uint.MaxValue, wenn es sich um eine Start-Variante handelt)
    /// </summary>
    public readonly uint iPortalIndex;
    /// <summary>
    /// Variante, welche verarbeitet wird
    /// </summary>
    public readonly ulong variant;
    /// <summary>
    /// die zugehörigen Daten der Variante
    /// </summary>
    public readonly VariantData variantData;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="state1">Kistenzustand des ersten Raumes</param>
    /// <param name="state2">Kistenzustand des zweiten Raumes</param>
    /// <param name="oPortalBoxes">alle Kisten, welche bisher rausgeschoben wurden</param>
    /// <param name="moves">Anzahl der Laufschritte insgesamt</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen insgesamt</param>
    /// <param name="path">zurückgelegter Pfad insgesamt</param>
    /// <param name="mainRoom1">gibt an, ob der erste Raum die Basis darstellt (sonst: zweite Raum)</param>
    /// <param name="iPortalIndex">Nummer des eingehenden Portals (oder uint.MaxValue, wenn es sich um eine Start-Variante handelt)</param>
    /// <param name="variant">Variante, welche verarbeitet wurde</param>
    /// <param name="variantData">die zugehörigen Daten der Variante</param>
    public MergeTask(ulong state1, ulong state2, uint[] oPortalBoxes, ulong moves, ulong pushes, string path, bool mainRoom1, uint iPortalIndex, ulong variant, VariantData variantData)
    {
      this.state1 = state1;
      this.state2 = state2;
      this.oPortalBoxes = oPortalBoxes;

      this.moves = moves;
      this.pushes = pushes;
      this.path = path;

      this.mainRoom1 = mainRoom1;
      this.iPortalIndex = iPortalIndex;
      this.variant = variant;
      this.variantData = variantData;
    }

    /// <summary>
    /// gibt die Prüfsumme dieser Aufgabe zurück
    /// </summary>
    /// <returns>Prüfsumme</returns>
    public ulong GetCrc()
    {
      return Crc64.Start.Crc64Update(state1)
                        .Crc64Update(state2)
                        .Crc64Update(oPortalBoxes)
                        .Crc64Update(mainRoom1 ? 1 : 2)
                        .Crc64Update(variantData.oPortalIndexPlayer);
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { path, moves, pushes, boxes = "int[" + oPortalBoxes.Length + "]" }.ToString();
    }
  }
}
