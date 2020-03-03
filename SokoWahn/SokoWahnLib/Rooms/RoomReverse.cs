using System;

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
    readonly Room room;

    /// <summary>
    /// merkt sich die Zustandsänderungen des Raumes pro Portal, wenn Kisten wieder herrausgezogen werden
    /// </summary>
    readonly StateBoxSwap[] portalStateSwaps;

    /// <summary>
    /// merkt sich die gesammelten Daten der Rückwärts-Varianten
    /// </summary>
    readonly VariantMap[] variantMap;

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
    }

    #region # struct VariantMap // Struktur zum Merken der jeweiligen Varianten und Zustand/Portal Kombinationen
    /// <summary>
    /// Struktur zum Merken der jeweiligen Varianten und Zustand/Portal Kombinationen
    /// </summary>
    struct VariantMap
    {
      /// <summary>
      /// merkt sich die Variante
      /// </summary>
      public readonly ulong variant;
      /// <summary>
      /// merkt sich den Zustand des Raumes
      /// </summary>
      public readonly ulong state;
      /// <summary>
      /// merkt sich das eingehende Portal
      /// </summary>
      public readonly uint iPortalIndex;
      /// <summary>
      /// merkt sich, ob nur Laufwege benutzt werden und keine Kisten verschoben wurden
      /// </summary>
      public readonly bool onlyMoves;

      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="variant">Variante</param>
      /// <param name="state">Zustand des Raumes</param>
      /// <param name="iPortalIndex">eingehendes Portal</param>
      /// <param name="onlyMoves">es wurden nur Laufwege benutzt (keine Kisten wurden verschoben)</param>
      public VariantMap(ulong variant, ulong state, uint iPortalIndex, bool onlyMoves)
      {
        this.variant = variant;
        this.state = state;
        this.iPortalIndex = iPortalIndex;
        this.onlyMoves = onlyMoves;
      }

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { iPortalIndex, state, onlyMoves, variant }.ToString();
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
          variantData.newState,
          variantData.oPortalIndexPlayer,
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
              variantData.newState,
              variantData.oPortalIndexPlayer,
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
        int diff = (int)x.iPortalIndex - (int)y.iPortalIndex;
        if (diff != 0) return diff;
        diff = x.state.CompareTo(y.state);
        if (diff != 0) return diff;
        diff = (x.onlyMoves ? 0 : 1) - (y.onlyMoves ? 0 : 1);
        return diff != 0 ? diff : x.variant.CompareTo(y.variant);
      });
    }
    #endregion

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
