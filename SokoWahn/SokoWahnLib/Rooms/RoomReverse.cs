using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum Erstellen und Speichern von Rückwärts-Varianten
  /// </summary>
  public sealed class RoomReverse : IDisposable
  {
    /// <summary>
    /// merkt sich den zugehörigen Raum der Varianten
    /// </summary>
    public readonly Room room;

    /// <summary>
    /// merkt sich die Zustandsänderungen des Raumes pro Portal, wenn Kisten wieder herrausgezogen werden
    /// </summary>
    public readonly StateBoxSwap[] portalStateSwaps;

    /// <summary>
    /// merkt sich die gesammelten Daten der Rückwärts-Varianten (sortiert nach: oPortalIndex, newState, onlyMoves)
    /// </summary>
    public readonly VariantMap[] variantMap;

    /// <summary>
    /// merkt sich das Inhaltsverzeichnis der VariantMap
    /// </summary>
    readonly Dictionary<ulong, ulong>[] dictVariantMap;

    /// <summary>
    /// merkt sich die Anzahl der End-Varianten
    /// </summary>
    public ulong endVariantCount;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Raum, welcher verwendet werden soll</param>
    public RoomReverse(Room room)
    {
      if (room == null) throw new NullReferenceException("room");
      this.room = room;
      portalStateSwaps = new StateBoxSwap[room.incomingPortals.Length];
      variantMap = new VariantMap[room.variantList.Count];
      dictVariantMap = Enumerable.Range(0, room.incomingPortals.Length + 1).Select(i => new Dictionary<ulong, ulong>()).ToArray();
    }

    #region # struct VariantMap // Struktur zum Merken der jeweiligen Varianten und Zustand/Portal Kombinationen
    /// <summary>
    /// Struktur zum Merken der jeweiligen Varianten und Zustand/Portal Kombinationen
    /// </summary>
    public struct VariantMap
    {
      /// <summary>
      /// merkt sich die Variante
      /// </summary>
      public readonly ulong variant;
      /// <summary>
      /// merkt sich das eingehende Portal (oder uint.MaxValue, wenn es sich um eine Startvariante handelt)
      /// </summary>
      public readonly uint iPortalIndex;
      /// <summary>
      /// merkt sich das ausgehende Portal (oder uint.MaxValue, wenn es sich um eine Endvariante handelt)
      /// </summary>
      public readonly uint oPortalIndex;
      /// <summary>
      /// merkt sich den neuen Zustand des Raumes
      /// </summary>
      public readonly ulong newState;
      /// <summary>
      /// merkt sich, ob nur Laufwege benutzt und keine Kisten verschoben wurden
      /// </summary>
      public readonly bool onlyMoves;

      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="variant">Variante</param>
      /// <param name="iPortalIndex">eingehendes Portal</param>
      /// <param name="oPortalIndex">ausgehendes Portal</param>
      /// <param name="newState">neuer Zustand des Raumes</param>
      /// <param name="onlyMoves">es wurden nur Laufwege benutzt (keine Kisten wurden verschoben)</param>
      public VariantMap(ulong variant, uint iPortalIndex, uint oPortalIndex, ulong newState, bool onlyMoves)
      {
        this.variant = variant;
        this.iPortalIndex = iPortalIndex;
        this.oPortalIndex = oPortalIndex;
        this.newState = newState;
        this.onlyMoves = onlyMoves;
      }

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { oPortalIndex, newState, onlyMoves, variant, iPortalIndex }.ToString();
      }
    }
    #endregion

    #region # public void Step1_FillPortalStateSwaps() // Schritt 1: füllt die Zustandsänderungen des Raumes pro Portal, wenn Kisten wieder herrausgezogen werden
    /// <summary>
    /// Schritt 1: füllt die Zustandsänderungen des Raumes pro Portal, wenn Kisten wieder herrausgezogen werden
    /// </summary>
    public void Step1_FillPortalStateSwaps()
    {
      for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
      {
        var stateSwap = room.incomingPortals[iPortalIndex].stateBoxSwap;
        var newStateSwap = portalStateSwaps[iPortalIndex] = new StateBoxSwapNormal(room.stateList);
        foreach (ulong oldState in stateSwap.GetAllKeys())
        {
          ulong newState = stateSwap.Get(oldState);
          newStateSwap.Add(newState, oldState);
        }
      }
    }
    #endregion

    #region # public void Step2_CollectVariantsPerState() // Schritt 2: sammelt die Varianten der jeweiligen Raumzustände
    /// <summary>
    /// Schritt 2: sammelt die Varianten der jeweiligen Raumzustände
    /// </summary>
    public void Step2_CollectVariantsPerState()
    {
      var variantList = room.variantList;
      var variantMap = this.variantMap;
      uint variantMapIndex = 0;

      // --- Startvarianten abarbeiten ---
      for (ulong variant = 0; variant < room.startVariantCount; variant++)
      {
        var variantData = variantList.GetData(variant);
        var v = new VariantMap
        (
          variant,
          uint.MaxValue,
          variantData.oPortalIndexPlayer,
          variantData.newState,
          variantData.pushes == 0
        );
        variantMap[variantMapIndex++] = v;
      }

      // --- Portalvarianten abarbeiten ---
      for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
      {
        var stateDict = room.incomingPortals[iPortalIndex].variantStateDict;
        foreach (var state in stateDict.GetAllStates())
        {
          foreach (var variant in stateDict.GetVariantSpan(state).AsEnumerable())
          {
            var variantData = variantList.GetData(variant);
            var v = new VariantMap
            (
              variant,
              iPortalIndex,
              variantData.oPortalIndexPlayer,
              variantData.newState,
              variantData.pushes == 0
            );
            variantMap[variantMapIndex++] = v;
          }
        }
      }

      if (variantMapIndex < variantMap.Length) throw new Exception("use variants-error");

      // --- Varianten nach Portalen und Zuständen sortieren ---
      Sort.QuickSortLowStack(variantMap, (x, y) =>
      {
        int diff = (int)x.oPortalIndex - (int)y.oPortalIndex;
        if (diff != 0) return diff;
        diff = x.newState.CompareTo(y.newState);
        if (diff != 0) return diff;
        diff = (x.onlyMoves ? 0 : 1) - (y.onlyMoves ? 0 : 1);
        return diff != 0 ? diff : x.variant.CompareTo(y.variant);
      });

      for (endVariantCount = 0; endVariantCount < (uint)variantMap.Length && variantMap[endVariantCount].oPortalIndex == uint.MaxValue; endVariantCount++) { }
    }
    #endregion

    #region # public void Step3_FillDicts() // Schritt 3: füllt die Dictionaries für die schnellere Suche
    /// <summary>
    /// Schritt 3: füllt die Dictionaries für die schnellere Suche
    /// </summary>
    public void Step3_FillDicts()
    {
      for (uint index = 0; index < variantMap.Length; )
      {
        uint oPortalIndex = variantMap[index].oPortalIndex;
        ulong newState = variantMap[index].newState;
        uint ofs;
        for (ofs = index; ofs < variantMap.Length && variantMap[ofs].oPortalIndex == oPortalIndex && variantMap[ofs].newState == newState; ofs++) { }
        dictVariantMap[oPortalIndex + 1].Add(newState, index | ((ulong)(ofs - index) << 32));
        index = ofs;
      }
    }
    #endregion

    /// <summary>
    /// gibt die Varianten eines ausgehendes Portals zurück
    /// </summary>
    /// <param name="oPortalIndex">Portal, worüber der Spieler den Raum verlässt (oder uint.MaxValue, wenn es sich um Endvarianten handelt)</param>
    /// <param name="newState">neuer Kistenzustand nach verlassen des Raumes</param>
    /// <returns>Enumerable der Varianten</returns>
    public IEnumerable<VariantMap> GetVariants(uint oPortalIndex, ulong newState)
    {
      var dict = dictVariantMap[oPortalIndex + 1];

      ulong val;
      if (!dict.TryGetValue(newState, out val)) yield break; // Zustand nicht gefunden?

      uint ofs = (uint)val;
      uint count = (uint)(val >> 32);
      for (uint i = 0; i < count; i++)
      {
        yield return variantMap[i + ofs];
      }
    }

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      for (uint iPortalIndex = 0; iPortalIndex < portalStateSwaps.Length; iPortalIndex++)
      {
        if (portalStateSwaps[iPortalIndex] != null)
        {
          portalStateSwaps[iPortalIndex].Dispose();
          portalStateSwaps[iPortalIndex] = null;
        }
      }
    }

    /// <summary>
    /// Destruktor
    /// </summary>
    ~RoomReverse()
    {
      Dispose();
    }
    #endregion
  }
}
