using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global

namespace SokoWahnLib.Rooms.Merger
{
  /// <summary>
  /// Klasse mit Hilfsmethoden zum optimieren von Räumen
  /// </summary>
  public static class OptimizeTools
  {
    /// <summary>
    /// erneuert die Varianten im Raum und deren Verlinkungen
    /// </summary>
    /// <param name="room">Raum, welcher bearbeitet werden soll</param>
    /// <param name="skip">Liste mit allen überspringbaren bzw. weiter zu verwendenden Varianten</param>
    public static void RenewVariants(Room room, SkipMapper skip)
    {
      Debug.Assert(skip.usedCount > 0);
      if (skip.usedCount == (uint)skip.map.Length) return; // nichts zu tun?

      // --- Variantenliste neu erstellen und gefiltert befüllen ---
      var oldVariants = room.variantList;
      var newVariants = new VariantListNormal((uint)room.incomingPortals.Length);
      for (ulong variant = 0; variant < oldVariants.Count; variant++)
      {
        ulong map = skip.map[variant];
        if (map == ulong.MaxValue) continue;
        Debug.Assert(map == newVariants.Count);
        var v = oldVariants.GetData(variant);
        newVariants.Add(v.oldState, v.moves, v.pushes, v.oPortalIndexBoxes, v.oPortalIndexPlayer, v.newState, v.path);
      }
      Debug.Assert(newVariants.Count == skip.usedCount);
      oldVariants.Dispose();
      room.variantList = newVariants;

      // --- Start-Varianten neu zählen ---
      ulong newStartVariants = 0;
      for (ulong i = room.startVariantCount - 1; i < room.startVariantCount; i--)
      {
        if (skip.map[i] < ulong.MaxValue) newStartVariants++;
      }
      room.startVariantCount = newStartVariants;

      // --- verlinkte Varianten in den Portalen neu setzen ---
      foreach (var portal in room.incomingPortals)
      {
        // --- variantStateDict übertragen ---
        var oldDict = portal.variantStateDict;
        var newDict = new VariantStateDictNormal(room.stateList, room.variantList);
        foreach (ulong state in oldDict.GetAllStates())
        {
          ulong skipVariants = 0;
          foreach (ulong variant in oldDict.GetVariantSpan(state).AsEnumerable())
          {
            Debug.Assert(variant < (uint)skip.map.Length);
            if (skip.map[variant] == ulong.MaxValue) // Variante wird nicht mehr verwendet?
            {
              skipVariants++;
              continue;
            }
            Debug.Assert(skip.map[variant] < room.variantList.Count);
            newDict.Add(state, skip.map[variant]);
          }
          Debug.Assert(oldDict.GetVariantSpan(state).variantCount == newDict.GetVariantSpan(state).variantCount + skipVariants);
        }
        oldDict.Dispose();
        portal.variantStateDict = newDict;
      }
    }

    /// <summary>
    /// entfernt unbenutzte Kistenzustände aus einem Raum
    /// </summary>
    /// <param name="room">Raum, welcher optimiert werden soll</param>
    public static void RemoveUnusedStates(Room room)
    {
      while (RemoveUnusedStatesInternal(room)) { }
    }

    /// <summary>
    /// entfernt unbenutzte Kistenzustände aus einem Raum 
    /// </summary>
    /// <param name="room">Raum, welcher optimiert werden soll</param>
    static bool RemoveUnusedStatesInternal(Room room)
    {
      var stateList = room.stateList;
      var variantList = room.variantList;

      using (var usingStates = new Bitter(stateList.Count))
      {
        usingStates.SetBit(0); // ersten Zustand immer pauschal markieren (End-Zustand)
        usingStates.SetBit(room.startState); // Start-Zustand ebenfalls markieren

        // --- alle Start-Varianten prüfen ---
        for (ulong variant = 0; variant < room.startVariantCount; variant++)
        {
          Debug.Assert(variant < variantList.Count);
          var v = variantList.GetData(variant);

          Debug.Assert(v.oldState < stateList.Count);
          usingStates.SetBit(v.oldState);
        }

        foreach (var iPortal in room.incomingPortals)
        {
          // --- Kistenzustände mit gültigen Varianten hinzufügen ---
          foreach (ulong state in iPortal.variantStateDict.GetAllStates())
          {
            Debug.Assert(state < stateList.Count);
            Debug.Assert(iPortal.variantStateDict.GetVariantSpan(state).variantCount > 0);
            usingStates.SetBit(state);
          }

          // --- Zustandsveränderungen durch reingeschobene Kisten hinzufügen ---
          foreach (ulong state in iPortal.stateBoxSwap.GetAllKeys())
          {
            ulong nextState = iPortal.stateBoxSwap.Get(state);
            if (iPortal.variantStateDict.GetVariantSpan(nextState).variantCount == 0 // keine Varianten im nachfolgenden Zustand mehr möglich?
              && stateList.Get(nextState).Any(pos => !room.field.IsGoal(pos))) continue; // und nicht alle Kisten auf den Zielfeldern? -> Kistenzustand ungültig
            usingStates.SetBit(state);
          }
        }

        if (usingStates.CountMarkedBits(0) != usingStates.Length)
        {
          RenewStates(room, new SkipMapper(usingStates));
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// erneuert die Zustände im Raum und deren Verlinkungen
    /// </summary>
    /// <param name="room">Raum, welcher bearbeitet werden soll</param>
    /// <param name="skip">Liste mit allen überpringbaren bzw. weiter zu verwendenden Zuständen</param>
    public static void RenewStates(Room room, SkipMapper skip)
    {
      Debug.Assert(skip.usedCount > 0);
      Debug.Assert(skip.usedCount < (uint)skip.map.Length);

      // --- Startzustand des Raumes neu setzen ---
      room.startState = skip.map[room.startState];

      // --- Zustandsliste neu erstellen und gefiltert befüllen ---
      var oldStates = room.stateList;
      var newStates = new StateListNormal(room.fieldPosis, room.goalPosis);
      for (ulong state = 0; state < oldStates.Count; state++)
      {
        ulong map = skip.map[state];
        if (map == ulong.MaxValue) continue;
        Debug.Assert(map == newStates.Count);
        newStates.Add(oldStates.Get(state));
        Debug.Assert(newStates.Get(map).Length == oldStates.Get(state).Length);
      }
      Debug.Assert(newStates.Count == skip.usedCount);
      oldStates.Dispose();
      room.stateList = newStates;

      // --- verlinkte Zustände innerhalb der Varianten neu setzen ---
      var oldVariants = room.variantList;
      var newVariants = new VariantListNormal(oldVariants.portalCount);
      ulong skipVariants = 0;
      var useVariants = new Bitter(oldVariants.Count);
      for (ulong variant = 0; variant < oldVariants.Count; variant++)
      {
        var v = oldVariants.GetData(variant);
        Debug.Assert(v.oldState < oldStates.Count);
        Debug.Assert(skip.map[v.oldState] < newStates.Count);
        Debug.Assert(v.newState < oldStates.Count);
        if (skip.map[v.newState] == ulong.MaxValue) { skipVariants++; continue; }
        Debug.Assert(skip.map[v.newState] < newStates.Count);
        useVariants.SetBit(variant);
        Debug.Assert(variant == newVariants.Count + skipVariants);
        newVariants.Add(skip.map[v.oldState], v.moves, v.pushes, v.oPortalIndexBoxes, v.oPortalIndexPlayer, skip.map[v.newState], v.path);
      }
      Debug.Assert(newVariants.Count + skipVariants == oldVariants.Count);
      oldVariants.Dispose();
      room.variantList = newVariants;

      var variantSkipper = new SkipMapper(useVariants);
      useVariants.Dispose();

      // --- verlinkte Zustände in den Portalen neu setzen ---
      foreach (var portal in room.incomingPortals)
      {
        // --- stateBoxSwap übertragen ---
        var oldSwap = portal.stateBoxSwap;
        var newSwap = new StateBoxSwapNormal(room.stateList);
        ulong skipSwaps = 0;
        foreach (ulong oldKey in oldSwap.GetAllKeys())
        {
          ulong newKey = skip.map[oldKey];
          if (newKey == ulong.MaxValue) { skipSwaps++; continue; } // nicht mehr gültige Swaps überspringen
          Debug.Assert(newKey < room.stateList.Count);
          ulong oldState = oldSwap.Get(oldKey);
          Debug.Assert(oldState != oldKey);
          ulong newState = skip.map[oldState];
          if (newState == ulong.MaxValue) { skipSwaps++; continue; }  // nicht mehr gültige Swaps überspringen
          Debug.Assert(newState < room.stateList.Count);
          newSwap.Add(newKey, newState);
        }
        Debug.Assert(newSwap.Count + skipSwaps == oldSwap.Count);
        oldSwap.Dispose();
        portal.stateBoxSwap = newSwap;

        // --- variantStateDict übertragen ---
        var oldDict = portal.variantStateDict;
        var newDict = new VariantStateDictNormal(room.stateList, room.variantList);
        foreach (ulong oldState in oldDict.GetAllStates())
        {
          if (oldState == ulong.MaxValue) continue;
          ulong newState = skip.map[oldState];
          if (newState == ulong.MaxValue) continue;
          skipVariants = 0;
          foreach (ulong variant in oldDict.GetVariantSpan(oldState).AsEnumerable())
          {
            ulong newVariant = variantSkipper.map[variant];
            if (newVariant == ulong.MaxValue) { skipVariants++; continue; }
            Debug.Assert(newVariant < room.variantList.Count);
            newDict.Add(newState, newVariant);
          }
          Debug.Assert(oldDict.GetVariantSpan(oldState).variantCount == newDict.GetVariantSpan(newState).variantCount + skipVariants);
        }
        oldDict.Dispose();
        portal.variantStateDict = newDict;
      }
    }
  }
}
