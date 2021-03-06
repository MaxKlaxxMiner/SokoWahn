﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Version einer Varianten-Liste
  /// </summary>
  public sealed class VariantListNormal : VariantList
  {
    /// <summary>
    /// merkt sich die gespeicherten Varianten
    /// </summary>
    public readonly List<VariantData> variantData = new List<VariantData>();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="portalCount">Anzahl der vorhandenen Portale</param>
    public VariantListNormal(uint portalCount) : base(portalCount) { }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Varianten insgesamt zurück
    /// </summary>
    public override ulong Count { get { return (uint)variantData.Count; } }

    /// <summary>
    /// fügt eine weitere Variante hinzu und gibt deren ID zurück
    /// </summary>
    /// <param name="oldState">vorheriger Raum-Zustand</param>
    /// <param name="moves">Anzahl der Bewegungsschritte (nur Bewegungen innerhalb des Raumes und beim Verlassen des Raumes wird gezählt)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen (nur Verschiebungen innerhalb des Raumes oder beim Verlassen des Raumes wird gezählt)</param>
    /// <param name="oPortalIndexBoxes">alle Portale, wohin eine Kiste rausgeschoben wurde</param>
    /// <param name="oPortalIndexPlayer">Portal, wo der Spieler den Raum zum Schluss verlassen hat (uint.MaxValue: Spieler verbleibt irgendwo im Raum = Zielstellung erreicht)</param>
    /// <param name="newState">nachfolgender Raum-Zustand</param>
    /// <param name="path">optionaler Pfad in XSB-Schreibweise (lrudLRUD bzw. auch RLE komprimiert erlaubt)</param>
    /// <returns>neue Varianten-ID</returns>
    public override ulong Add(ulong oldState, ulong moves, ulong pushes, uint[] oPortalIndexBoxes, uint oPortalIndexPlayer, ulong newState, string path = null)
    {
#if DEBUG
      if (oPortalIndexPlayer == uint.MaxValue)
      {
        Debug.Assert(moves + 1 >= pushes);
      }
      else
      {
        Debug.Assert(moves > 0);
        Debug.Assert(moves  >= pushes);
      }
#endif
      Debug.Assert(oPortalIndexBoxes.All(portal => portal < portalCount));
      Debug.Assert(oPortalIndexPlayer < portalCount || (oPortalIndexPlayer == uint.MaxValue && newState == 0)); // Spieler verlässt den Raum oder kann bleiben, wenn der End-Zustand erreicht wurde
      Debug.Assert(path != null && (ulong)VariantData.UncompressPath(path).Length == moves);

      ulong id = (uint)variantData.Count;
      variantData.Add(new VariantData { oldState = oldState, moves = moves, pushes = pushes, oPortalIndexBoxes = oPortalIndexBoxes, oPortalIndexPlayer = oPortalIndexPlayer, newState = newState, path = path });
      return id;
    }

    /// <summary>
    /// fragt die Daten einer bestimmten Variante ab
    /// </summary>
    /// <param name="variant">ID der Variante, welche abgefragt werden soll</param>
    /// <returns>Daten der abgefragten Variante</returns>
    public override VariantData GetData(ulong variant)
    {
      Debug.Assert(variant < Count);

      return variantData[(int)(uint)variant];
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose(){}
  }
}
