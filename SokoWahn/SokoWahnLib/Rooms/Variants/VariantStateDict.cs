// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global
using System;
using System.Collections.Generic;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Inhaltsverzeichnis für mehrere Varianten pro Raumzustand
  /// </summary>
  public abstract class VariantStateDict : IDisposable
  {
    /// <summary>
    /// Liste mit allen Zuständen im Raum
    /// </summary>
    public readonly StateList stateList;
    /// <summary>
    /// Liste mit allen Varianten im Raum
    /// </summary>
    public readonly VariantList variantList;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="stateList">Liste mit allen Zuständen im Raum</param>
    /// <param name="variantList">Liste mit allen Varianten im Raum</param>
    protected VariantStateDict(StateList stateList, VariantList variantList)
    {
      if (stateList == null) throw new ArgumentNullException("stateList");
      if (variantList == null) throw new ArgumentNullException("variantList");

      this.stateList = stateList;
      this.variantList = variantList;
    }

    /// <summary>
    /// fügt eine weitere Variante pro Raumzustand hinzu
    /// </summary>
    /// <param name="stateId">Raumzustand, welcher betroffen ist</param>
    /// <param name="variantId">Variante, welche hinzugefügt werden soll</param>
    public abstract void Add(ulong stateId, ulong variantId);

    /// <summary>
    /// gibt alle Zustände zurück, wofür Varianten bekannt sind
    /// </summary>
    /// <returns>Enumerable der bekannten Zustände</returns>
    public abstract IEnumerable<ulong> GetAllStates();

    /// <summary>
    /// fragt alle Varianten ab, welche zu einem bestimmten Zustand gehören und gibt diese zurück
    /// </summary>
    /// <param name="stateId">Raumzustand, welche abgefragt werden soll</param>
    /// <returns>Enumerable der zugehörigen Varianten</returns>
    public abstract IEnumerable<ulong> GetVariants(ulong stateId);

    #region # // --- Dispose ---
    /// <summary>
    /// Destruktor
    /// </summary>
    ~VariantStateDict()
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
