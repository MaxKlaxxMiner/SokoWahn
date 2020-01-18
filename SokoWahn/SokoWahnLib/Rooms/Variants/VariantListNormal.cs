using System.Collections.Generic;
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
    /// merkt sich die Anzahl der End-Varianten (wo der Spieler im Raum verbleibt)
    /// </summary>
    uint endVariants;

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
    /// gibt die Anzahl der jeweiligen End-Varianten zurück
    /// </summary>
    public override ulong CountEnd { get { return endVariants; } }

    /// <summary>
    /// fügt eine weitere Variante hinzu und gibt deren ID zurück
    /// </summary>
    /// <param name="oldStateId">vorheriger Raum-Zustand</param>
    /// <param name="moves">Anzahl der Bewegungsschritte (nur Bewegungen innerhalb des Raumes und beim Verlassen des Raumes wird gezählt)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen (nur Verschiebungen innerhalb des Raumes oder beim Verlassen des Raumes wird gezählt)</param>
    /// <param name="boxPortals">alle Portale, wohin eine Kiste rausgeschoben wurde</param>
    /// <param name="playerPortal">Portal, wo der Spieler den Raum zum Schluss verlassen hat (uint.MaxValue: Spieler verbleibt irgendwo im Raum = Zielstellung erreicht)</param>
    /// <param name="newStateId">nachfolgender Raum-Zustand</param>
    /// <param name="path">optionaler Pfad in XSB-Schreibweise (lrudLRUD bzw. auch RLE komprimiert erlaubt)</param>
    /// <returns>neue Varianten-ID</returns>
    public override ulong Add(ulong oldStateId, ulong moves, ulong pushes, uint[] boxPortals, uint playerPortal, ulong newStateId, string path = null)
    {
      if (playerPortal == uint.MaxValue)
      {
        endVariants++;
        Debug.Assert(moves + 1 >= pushes);
      }
      else
      {
        Debug.Assert(moves > 0);
        Debug.Assert(moves  >= pushes);
      }
      Debug.Assert(boxPortals.All(portal => portal < portalCount));
      Debug.Assert(playerPortal < portalCount || (playerPortal == uint.MaxValue && newStateId == 0)); // Spieler verlässt den Raum oder kann bleiben, wenn der End-Zustand erreicht wurde
      Debug.Assert(path != null && (ulong)VariantData.UncompressPath(path).Length == moves);

      ulong id = (uint)variantData.Count;
      variantData.Add(new VariantData { oldStateId = oldStateId, moves = moves, pushes = pushes, boxPortals = boxPortals, playerPortal = playerPortal, newStateId = newStateId, path = path });
      return id;
    }

    /// <summary>
    /// fragt die Daten einer bestimmten Variante ab
    /// </summary>
    /// <param name="variantId">ID der Variante, welche abgefragt werden soll</param>
    /// <returns>Daten der abgefragten Variante</returns>
    public override VariantData GetData(ulong variantId)
    {
      Debug.Assert(variantId < Count);

      return variantData[(int)(uint)variantId];
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose(){}
  }
}
