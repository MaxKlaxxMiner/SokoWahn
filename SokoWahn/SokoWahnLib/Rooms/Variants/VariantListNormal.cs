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
    /// gibt die Anzahl der gespeicherten Varianten zurück
    /// </summary>
    public override ulong Count { get { return (uint)variantData.Count; } }

    /// <summary>
    /// fügt eine weitere Variante hinzu und gibt deren ID zurück
    /// </summary>
    /// <param name="oldStateId">vorheriger Raum-Zustand</param>
    /// <param name="moves">Anzahl der Bewegungsschritte</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen</param>
    /// <param name="boxPortals">alle Portale, wohin eine Kiste rausgeschoben wurde</param>
    /// <param name="playerPortal">Portal, wo der Spieler den Raum zum Schluss verlassen hat (uint.MaxValue: Spieler verbleibt irgendwo im Raum = Zielstellung erreicht)</param>
    /// <param name="newStateId">nachfolgender Raum-Zustand</param>
    /// <param name="path">optionaler Pfad in XSB-Schreibweise (lrudLRUD bzw. auch RLE komprimiert erlaubt)</param>
    /// <returns>neue Varianten-ID</returns>
    public override ulong Add(ulong oldStateId, ulong moves, ulong pushes, uint[] boxPortals, uint playerPortal, ulong newStateId, string path = null)
    {
      Debug.Assert(moves > 0);
      Debug.Assert(moves >= pushes);
      Debug.Assert(boxPortals.All(portal => portal < portalCount));
      Debug.Assert(playerPortal < portalCount || (playerPortal == uint.MaxValue && newStateId == 0)); // Spieler verlässt den Raum oder kann bleiben, wenn der End-Zustand erreicht wurde
      Debug.Assert(path != null && (ulong)VariantData.UncompressPath(path).Length == moves);

      ulong id = (uint)variantData.Count;
      variantData.Add(new VariantData { oldStateId = oldStateId, moves = moves, pushes = pushes, boxPortals = boxPortals, playerPortal = playerPortal, newStateId = newStateId, path = path });
      return id;
    }
  }
}
