
//namespace SokoWahnLib.Rooms
//{
//  /// <summary>
//  /// Klasse, welche einen kompletten Raum darstellt
//  /// </summary>
//  public class Room : IDisposable
//  {
//    #region # // --- Variablen ---
//    /// <summary>
//    /// merkt sich alle Zielfelder, welche zum Raum gehören
//    /// </summary>
//    public readonly HashSet<int> goalPosis;

//    /// <summary>
//    /// merkt sich die Daten der Varianten
//    /// </summary>
//    public readonly Bitter variantsData;
//    /// <summary>
//    /// Größe eines einzelnen Varianten-Elementes (in Bits)
//    /// </summary>
//    public readonly ulong variantsDataElement;
//    /// <summary>
//    /// Anzahl der benutzen Varianten-Elemente
//    /// </summary>
//    public uint variantsDataUsed;
//    /// <summary>
//    /// merkt sich alle Start-Varianten, wo der Spieler im Raum verbleibt
//    /// </summary>
//    public readonly List<uint> startPlayerVariants = new List<uint>();
//    /// <summary>
//    /// merkt sich alle Start-Varianten, wo der Spieler den Raum verlässt
//    /// </summary>
//    public readonly List<uint> startBoxVariants = new List<uint>();
//    #endregion

//    #region # // --- Zustand-Methoden ---
//    /// <summary>
//    /// importiert und verschmelzt die Zustände zweier Räume und gibt ein Mapping-Dict für die Zustände zurück
//    /// </summary>
//    /// <param name="room1">Raum 1, deren Zustände verwendet werden sollen</param>
//    /// <param name="room2">Raum 2, deren Zustände verwendet werden sollen</param>
//    public Dictionary<ulong, uint> AddMergeStates(Room room1, Room room2)
//    {
//      var newPlayerStates1 = new Dictionary<ulong, uint>();
//      var newPlayerStates2 = new Dictionary<ulong, uint>();
//      var newBoxStates = new Dictionary<ulong, uint>();
//      var newBoxIndexMap = fieldPosis.Select((p, i) => new { p, i }).ToDictionary(x => x.p, x => x.i);

//      for (uint sp1 = 0; sp1 < room1.statePlayerUsed; sp1++)
//      {
//        var sp1Info = room1.GetPlayerStateInfo(sp1);
//        for (uint sb2 = 0; sb2 < room2.stateBoxUsed; sb2++)
//        {
//          // --- Spieler-Zustand 1 und Kisten-Zustand 2 verschmelzen (newPlayerStates1) ---
//          var sb2Info = room2.GetBoxStateInfo(sb2);
//          newPlayerStates1.Add((ulong)sp1 << 32 | sb2, statePlayerUsed);
//          var boxBits = new bool[fieldPosis.Length];
//          foreach (var box1 in sp1Info.boxPosis) boxBits[newBoxIndexMap[box1]] = true;
//          foreach (var box2 in sb2Info.boxPosis) boxBits[newBoxIndexMap[box2]] = true;
//          AddPlayerState(sp1Info.playerPos, checked((byte)(sp1Info.boxCount + sb2Info.boxCount)), (byte)(sp1Info.finishedBoxCount + sb2Info.finishedBoxCount), boxBits);
//#if DEBUG
//          var newInfo = GetPlayerStateInfo(statePlayerUsed - 1);
//          Debug.Assert(newInfo.boxCount == sp1Info.boxCount + sb2Info.boxCount);
//          Debug.Assert(newInfo.boxPosis.Length == sp1Info.boxPosis.Length + sb2Info.boxPosis.Length);
//          for (int i = 0; i < newInfo.boxPosis.Length; i++)
//          {
//            Debug.Assert(sp1Info.boxPosis.Contains(newInfo.boxPosis[i]) || sb2Info.boxPosis.Contains(newInfo.boxPosis[i]));
//          }
//          Debug.Assert(newInfo.finishedBoxCount == sp1Info.finishedBoxCount + sb2Info.finishedBoxCount);
//          Debug.Assert(newInfo.playerPos == sp1Info.playerPos);
//          Debug.Assert(newInfo.room == this);
//#endif
//        }
//      }

//      for (uint sb1 = 0; sb1 < room1.stateBoxUsed; sb1++)
//      {
//        var sb1Info = room1.GetBoxStateInfo(sb1);
//        for (uint sp2 = 0; sp2 < room2.statePlayerUsed; sp2++)
//        {
//          // --- Kisten-Zustand 1 und Spieler-Zustand 2 verschmelzen (newPlayerStates2) ---
//          var sp2Info = room2.GetPlayerStateInfo(sp2);
//          newPlayerStates2.Add((ulong)sb1 << 32 | sp2, statePlayerUsed);
//          var boxBits = new bool[fieldPosis.Length];
//          foreach (var box1 in sb1Info.boxPosis) boxBits[newBoxIndexMap[box1]] = true;
//          foreach (var box2 in sp2Info.boxPosis) boxBits[newBoxIndexMap[box2]] = true;
//          AddPlayerState(sp2Info.playerPos, checked((byte)(sb1Info.boxCount + sp2Info.boxCount)), (byte)(sb1Info.finishedBoxCount + sp2Info.finishedBoxCount), boxBits);
//#if DEBUG
//          var newInfo = GetPlayerStateInfo(statePlayerUsed - 1);
//          Debug.Assert(newInfo.boxCount == sb1Info.boxCount + sp2Info.boxCount);
//          Debug.Assert(newInfo.boxPosis.Length == sb1Info.boxPosis.Length + sp2Info.boxPosis.Length);
//          for (int i = 0; i < newInfo.boxPosis.Length; i++)
//          {
//            Debug.Assert(sb1Info.boxPosis.Contains(newInfo.boxPosis[i]) || sp2Info.boxPosis.Contains(newInfo.boxPosis[i]));
//          }
//          Debug.Assert(newInfo.finishedBoxCount == sb1Info.finishedBoxCount + sp2Info.finishedBoxCount);
//          Debug.Assert(newInfo.playerPos == sp2Info.playerPos);
//          Debug.Assert(newInfo.room == this);
//#endif
//        }
//        for (uint sb2 = 0; sb2 < room2.stateBoxUsed; sb2++)
//        {
//          // --- Kisten-Zustand 1 und Kisten-Zustand 2 verschmelzen (newBoxStates) ---
//          var sb2Info = room2.GetBoxStateInfo(sb2);
//          newBoxStates.Add((ulong)sb1 << 32 | sb2, stateBoxUsed);
//          var boxBits = new bool[fieldPosis.Length];
//          foreach (var box1 in sb1Info.boxPosis) boxBits[newBoxIndexMap[box1]] = true;
//          foreach (var box2 in sb2Info.boxPosis) boxBits[newBoxIndexMap[box2]] = true;
//          AddBoxState(checked((byte)(sb1Info.boxCount + sb2Info.boxCount)), (byte)(sb1Info.finishedBoxCount + sb2Info.finishedBoxCount), boxBits);
//#if DEBUG
//          var newInfo = GetBoxStateInfo(stateBoxUsed - 1);
//          Debug.Assert(newInfo.boxCount == sb1Info.boxCount + sb2Info.boxCount);
//          Debug.Assert(newInfo.boxPosis.Length == sb1Info.boxPosis.Length + sb2Info.boxPosis.Length);
//          for (int i = 0; i < newInfo.boxPosis.Length; i++)
//          {
//            Debug.Assert(sb1Info.boxPosis.Contains(newInfo.boxPosis[i]) || sb2Info.boxPosis.Contains(newInfo.boxPosis[i]));
//          }
//          Debug.Assert(newInfo.finishedBoxCount == sb1Info.finishedBoxCount + sb2Info.finishedBoxCount);
//          Debug.Assert(newInfo.playerPos == 0);
//          Debug.Assert(newInfo.room == this);
//#endif
//        }
//      }

//      // --- neues Zustand-Dictionary aus Kisten-Dict und Spieler-Dict erstellen ---
//      var newStates = new Dictionary<ulong, uint>();
//      foreach (var playerState in newPlayerStates1)
//      {
//        uint k1 = (uint)(playerState.Key >> 32);
//        uint k2 = (uint)playerState.Key + statePlayerUsed;
//        uint st = playerState.Value;
//        newStates.Add((ulong)k1 << 32 | k2, st);
//      }
//      foreach (var playerState in newPlayerStates2)
//      {
//        uint k1 = (uint)(playerState.Key >> 32) + statePlayerUsed;
//        uint k2 = (uint)playerState.Key;
//        uint st = playerState.Value;
//        newStates.Add((ulong)k1 << 32 | k2, st);
//      }
//      foreach (var boxState in newBoxStates)
//      {
//        uint k1 = (uint)(boxState.Key >> 32) + statePlayerUsed;
//        uint k2 = (uint)boxState.Key + statePlayerUsed;
//        uint st = boxState.Value + statePlayerUsed;
//        newStates.Add((ulong)k1 << 32 | k2, st);
//      }
//      return newStates;
//    }
//    #endregion

//    #region # // --- Varianten-Methoden ---
//    /// <summary>
//    /// gibt den eingehenden und ausgehenden Zustand einer Variante zurück
//    /// </summary>
//    /// <param name="variantIndex">Variante, welche abgefragt werden soll</param>
//    /// <returns>ein- und ausgehender Zustand</returns>
//    public KeyValuePair<uint, uint> GetVariantStates(uint variantIndex)
//    {
//      ulong bitPos = variantIndex * variantsDataElement;
//      uint incomingState = variantsData.GetUInt(bitPos);
//      uint outgoingState = variantsData.GetUInt(bitPos + 40);
//      return new KeyValuePair<uint, uint>(incomingState, outgoingState);
//    }

//    /// <summary>
//    /// setzt den ein- und ausgehenden Zustand einer Variante neu
//    /// </summary>
//    /// <param name="variantIndex">Variante, welche geändert werden soll</param>
//    /// <param name="incomingState">neuer eingehender Zustand</param>
//    /// <param name="outgoingState">neuer ausgehender Zustand</param>
//    public void SetVariantStates(uint variantIndex, uint incomingState, uint outgoingState)
//    {
//      ulong bitPos = variantIndex * variantsDataElement;
//      Debug.Assert(incomingState < uint.MaxValue);
//      Debug.Assert(outgoingState < uint.MaxValue);
//      variantsData.SetUInt(bitPos, incomingState);
//      variantsData.SetUInt(bitPos + 40, outgoingState);
//      Debug.Assert(variantsData.GetUInt(bitPos) == incomingState);
//      Debug.Assert(variantsData.GetUInt(bitPos + 40) == outgoingState);
//    }

//    /// <summary>
//    /// fragt eine bestimmte Variante ab (für Debug-Zwecke)
//    /// </summary>
//    /// <param name="variantIndex">Index der Variante</param>
//    /// <returns>fertig ausgelesene Variante</returns>
//    public VariantDebugInfo GetVariantInfo(uint variantIndex)
//    {
//      Debug.Assert(variantIndex < variantsDataUsed);

//      ulong bitPos = variantIndex * variantsDataElement;
//      var incomingPortal = incomingPortals.FirstOrDefault(p => p.roomToPlayerVariants.Any(x => x == variantIndex) || p.roomToBoxVariants.Any(x => x == variantIndex));
//      bool incomingBox = incomingPortal != null && incomingPortal.roomToBoxVariants.Any(x => x == variantIndex);
//      uint incomingState = variantsData.GetUInt(bitPos);
//      uint outgoingState = variantsData.GetUInt(bitPos + 40);

//      return new VariantDebugInfo(
//        incomingPortal,
//        incomingBox,
//        incomingState,
//        variantsData.GetByte(bitPos + 32) == 0xff ? null : outgoingPortals[variantsData.GetByte(bitPos + 32)],
//        !incomingBox && GetStateInfo(incomingState).boxCount > GetStateInfo(outgoingState).boxCount,
//        outgoingState,
//        variantsData.GetUInt24(bitPos + 72),
//        variantsData.GetUInt24(bitPos + 96)
//      );
//    }
//    #endregion

//    #region # // --- Optimize ---
//    /// <summary>
//    /// prüft den eigenen Raum, ob Zustände bekannt sind, wo Kisten enthalten sein können
//    /// </summary>
//    /// <returns>true, wenn Zustände mit enthaltenen Kisten gefunden wurden</returns>
//    public bool HasBoxStates()
//    {
//      for (uint state = 0; state < stateBoxUsed; state++)
//      {
//        if (GetBoxStateInfo(state).boxCount > 0) return true;
//      }

//      for (uint state = 0; state < statePlayerUsed; state++)
//      {
//        if (GetPlayerStateInfo(state).boxCount > 0) return true;
//      }

//      return false;
//    }

//    /// <summary>
//    /// prüft, ob mindestens eine Kiste bei der Start-Stellung enthalten ist
//    /// </summary>
//    /// <returns>true, wenn mindestens eine Kiste beim Start vorhanden ist</returns>
//    public bool HasStartBoxes()
//    {
//      return fieldPosis.Any(pos => field.IsBox(pos));
//    }
//    #endregion

//    #region # // --- ToString() ---
//    /// <summary>
//    /// gibt den Inhalt als lesbare Zeichenkette zurück
//    /// </summary>
//    /// <returns>lesbare Zeichenkette</returns>
//    public override string ToString()
//    {
//      return new
//      {
//        startPos = fieldPosis.Min(),
//        size = fieldPosis.Length,
//        incomingPortals = incomingPortals.Length + ": " + string.Join(", ", incomingPortals.AsEnumerable()),
//        posis = string.Join(",", fieldPosis.OrderBy(pos => pos))
//      }.ToString();
//    }
//    #endregion
//  }
//}
