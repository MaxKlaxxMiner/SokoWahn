using System;
using System.Diagnostics;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse, welche eine Liste mit Varianten speichert
  /// </summary>
  public abstract class VariantList : IDisposable
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
      Debug.Assert(portalCount < ushort.MaxValue);

      this.portalCount = portalCount;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Varianten insgesamt zurück
    /// </summary>
    public abstract ulong Count { get; }

    /// <summary>
    /// gibt die Anzahl der jeweiligen End-Varianten zurück
    /// </summary>
    public abstract ulong EndVariantCount { get; }

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
    public abstract ulong Add(ulong oldState, ulong moves, ulong pushes, uint[] oPortalIndexBoxes, uint oPortalIndexPlayer, ulong newState, string path = null);

    /// <summary>
    /// fragt die Daten einer bestimmten Variante ab
    /// </summary>
    /// <param name="variant">ID der Variante, welche abgefragt werden soll</param>
    /// <returns>Daten der abgefragten Variante</returns>
    public abstract VariantData GetData(ulong variant);

    #region # // --- Dispose ---
    /// <summary>
    /// Destructor
    /// </summary>
    ~VariantList()
    {
      Dispose();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public abstract void Dispose();
    #endregion
  }
}
