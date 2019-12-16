// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMethodReturnValue.Global
using System.Diagnostics;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse, welche eine Liste mit Varianten speichert
  /// </summary>
  public abstract class VariantList
  {
    /// <summary>
    /// merkt sich die Anzahl der vorhandenen Portale
    /// </summary>
    public readonly uint portalCount;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="portalCount">Anzahl der vorhandenen Portale</param>
    protected VariantList(uint portalCount)
    {
      Debug.Assert(portalCount > 0 && portalCount < ushort.MaxValue);

      this.portalCount = portalCount;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Varianten zurück
    /// </summary>
    public abstract ulong Count { get; }

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
    public abstract ulong Add(ulong oldStateId, ulong moves, ulong pushes, uint[] boxPortals, uint playerPortal, ulong newStateId, string path = null);
  }
}
