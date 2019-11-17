using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable RedundantIfElseBlock

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse, welche einen kompletten Raum darstellt
  /// </summary>
  public class Room : IDisposable
  {
    /// <summary>
    /// merkt sich das gesamte Spiel, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// merkt sich alle Felder, welche zum Raum gehören
    /// </summary>
    public readonly int[] fieldPosis;
    /// <summary>
    /// merkt sich alle Zielfelder, welche zum Raum gehören
    /// </summary>
    public readonly HashSet<int> goalPosis;
    /// <summary>
    /// merkt sich die eigenden Portale von den anderen Räumen
    /// </summary>
    public readonly RoomPortal[] incomingPortals;
    /// <summary>
    /// merkt sich die ausgehenden Portal in andere Räume
    /// </summary>
    public readonly RoomPortal[] outgoingPortals;

    /// <summary>
    /// merkt sich die Daten der Zustände mit Spieler
    /// </summary>
    public readonly Bitter statePlayerData;
    /// <summary>
    /// Größe eines einzelnen Zustand-Elementes mit Spieler (in Bits)
    /// </summary>
    public readonly ulong statePlayerElement;
    /// <summary>
    /// Anzahl der benutzten Zustand-Elemente mit Spieler
    /// </summary>
    public uint statePlayerUsed;

    /// <summary>
    /// merkt sich die Daten der Zustände ohne Spieler
    /// </summary>
    public readonly Bitter stateBoxData;
    /// <summary>
    /// Größe eines einzelnen Zustand-Elementes ohne Spieler (in Bits)
    /// </summary>
    public readonly ulong stateBoxElement;
    /// <summary>
    /// Anzahl der benutzen Zustand-Elemente ohne Spieler
    /// </summary>
    public uint stateBoxUsed;

    /// <summary>
    /// gibt die Gesamtzahl der gespeicherten Zustände zurück
    /// </summary>
    public uint StateUsed { get { return statePlayerUsed + stateBoxUsed; } }

    /// <summary>
    /// merkt sich die Daten der Varianten
    /// </summary>
    public readonly Bitter variantsData;
    /// <summary>
    /// Größe eines einzelnen Varianten-Elementes (in Bits)
    /// </summary>
    public readonly ulong variantsDateElement;
    /// <summary>
    /// Anzahl der benutzen Zustand-Elemente
    /// </summary>
    public uint variantsDataUsed;

    /// <summary>
    /// Konstruktor um ein Raum aus einem einzelnen Feld zu erstellen
    /// </summary>
    /// <param name="field">gesamtes Spielfeld, welches verwendet wird</param>
    /// <param name="pos">Position des Feldes, worraus der Raum generiert werden soll</param>
    /// <param name="incomingPortals">eingehende Portale von den anderen Räumen</param>
    /// <param name="outgoingPortals">ausgehende Portale in andere Räume</param>
    public Room(ISokoField field, int pos, RoomPortal[] incomingPortals, RoomPortal[] outgoingPortals)
    {
      if (field == null) throw new ArgumentNullException("field");
      if (!field.ValidPos(pos)) throw new ArgumentOutOfRangeException("pos");
      if (incomingPortals == null) throw new ArgumentNullException("incomingPortals");
      if (outgoingPortals == null) throw new ArgumentNullException("outgoingPortals");
      if (incomingPortals.Length != outgoingPortals.Length) throw new ArgumentException("incomingPortals.Length != outgoingPortals.Length");

      this.field = field;
      fieldPosis = new[] { pos };
      goalPosis = new HashSet<int>();
      if (field.GetField(pos) == '.' || field.GetField(pos) == '*') goalPosis.Add(pos);
      this.incomingPortals = incomingPortals;
      this.outgoingPortals = outgoingPortals;

      stateBoxElement = sizeof(byte) * 8          // Anzahl der Kisten, welche sich auf dem Spielfeld befinden
                      + sizeof(byte) * 8          // Anzahl der Kisten, welche sich bereits auf Zielfeldern befinden
                      + (ulong)fieldPosis.Length; // Bit-markierte Felder, welche mit Kisten belegt sind
      stateBoxData = new Bitter(stateBoxElement * (ulong)fieldPosis.Length * 2UL); // Raum mit einzelnen Kisten-Feld kann nur zwei Zustände annehmen: 1 = leer, 2 = Kiste
      stateBoxUsed = 0;

      statePlayerElement = sizeof(ushort) * 8        // Spieler-Position
                         + sizeof(byte) * 8          // Anzahl der Kisten, welche sich auf dem Spielfeld befinden
                         + sizeof(byte) * 8          // Anzahl der Kisten, welche sich bereits auf Zielfelder befinden
                         + (ulong)fieldPosis.Length; // Bit-markierte Felder, welche mit Kisten belegt sind
      statePlayerData = new Bitter(statePlayerElement * 1UL); // Raum mit eintelnen Spieler-Feld kann nur ein Zustand annehmen: 1 = Spieler
      statePlayerUsed = 0;

      variantsDateElement = sizeof(byte) * 8  // das verwendete ausgehende Portal (0xff == kein Ausgang benutzt)
                          + sizeof(uint) * 8  // Raum-Zustand, welcher erreicht werden kann
                          + 3 * 8             // Anzahl der Laufschritte, welche benötigt werden (inkl. Kistenverschiebungen)
                          + 3 * 8;            // Anzahl der Kistenverschiebungen, welche benötigt werden
      variantsData = new Bitter(variantsDateElement * (4 * 4 + 4 * 4 + 4 + 4)); // (4 eigehende Kisten * 4 ausgehende Kisten) + (4 eingehden Spieler * 4 ausgehende Spieler) + (4 Starts) + (4 Ziele)
      variantsDataUsed = 0;
    }

    #region # // --- Zustand-Methoden ---
    /// <summary>
    /// erstellt die ersten Zustände
    /// </summary>
    public void InitStates()
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(statePlayerUsed == 0);
      Debug.Assert(stateBoxUsed == 0);
      Debug.Assert(variantsDataUsed == 0);

      int pos = fieldPosis.First();

      switch (field.GetField(pos))
      {
        case '@': // Spieler auf einem leeren Feld
        {
          AddPlayerState(pos, 0, 0, false); // | Start | Ende | Spieler auf einem leeren Feld
          AddBoxState(0, 0, false);         // |     - | Ende | leeres Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddBoxState(1, 0, true);        // |     - |    - | Kiste auf einem leeren Feld
          }
        } break;

        case '+': // Spieler auf einem Zielfeld
        {
          AddPlayerState(pos, 0, 0, false); // | Start |    - | Spieler auf einem Zielfeld
          AddBoxState(0, 0, false);         // |     - |    - | leeres Zielfeld
          AddBoxState(1, 1, true);          // |     - | Ende | Kiste auf einem Zielfeld
        } break;

        case ' ': // leeres Feld
        {
          AddBoxState(0, 0, false);         // | Start | Ende | leeres Feld
          AddPlayerState(pos, 0, 0, false); // |     - | Ende | Spieler auf einem leeren Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddBoxState(1, 0, true);        // |     - |    - | Kiste auf einem leeren Feld
          }
        } break;

        case '.': // Zielfeld
        {
          AddBoxState(0, 0, false);         // | Start |    - | leeres Zielfeld
          AddPlayerState(pos, 0, 0, false); // |     - |    - | Spieler auf einem Zielfeld
          AddBoxState(1, 1, true);          // |     - | Ende | Kiste auf einem Zielfeld
        } break;

        case '$': // Feld mit Kiste
        {
          AddBoxState(1, 0, true);          // | Start |    - | Kiste auf einem leeren Feld
          AddBoxState(0, 0, false);         // |     - | Ende | leeres Feld
          AddPlayerState(pos, 0, 0, false); // |     - | Ende | Spieler auf einem leeren Feld
          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
        } break;

        case '*': // Kiste auf einem Zielfeld
        {
          AddBoxState(1, 1, true);            // | Start | Ende | Kiste auf einem Zielfeld
          if (!field.CheckCorner(pos)) // Kiste kann weggeschoben werden?
          {
            AddBoxState(0, 0, false);         // |     - |    - | leeres Feld
            AddPlayerState(pos, 0, 0, false); // |     - |    - | Spieler auf einem leeren Feld
          }
        } break;

        default: throw new NotSupportedException("char: " + field.GetField(pos));
      }
    }

    /// <summary>
    /// fügt einen weiteren Raum-Zustand mit Spieler hinzu
    /// </summary>
    /// <param name="playerPos">Spieler-Position</param>
    /// <param name="boxCount">Anzahl der enthaltenen Kisten</param>
    /// <param name="finishedBoxCount">Anzahl der enthalten Kisten, welche auf Zielfeldern stehen</param>
    /// <param name="boxBits">Bits der Felder für markierten Kisten</param>
    void AddPlayerState(int playerPos, byte boxCount, byte finishedBoxCount, params bool[] boxBits)
    {
      Debug.Assert(field.ValidPos(playerPos) && playerPos < ushort.MaxValue);
      Debug.Assert(fieldPosis.Any(pos => playerPos == pos));
      Debug.Assert(boxCount <= fieldPosis.Length);
      Debug.Assert(finishedBoxCount <= boxCount);
      Debug.Assert(boxBits.Length == fieldPosis.Length);
      Debug.Assert(boxBits.Count(x => x) == boxCount);

      ulong bitPos = statePlayerUsed * statePlayerElement;
      statePlayerData.SetUShort(bitPos, (ushort)playerPos);
      statePlayerData.SetByte(bitPos + 16, boxCount);
      statePlayerData.SetByte(bitPos + 24, finishedBoxCount);
      statePlayerData.ClearBits(bitPos + 32, (uint)boxBits.Length);
      for (uint i = 0; i < boxBits.Length; i++)
      {
        if (boxBits[i]) statePlayerData.SetBit(bitPos + 32 + i);
        Debug.Assert(statePlayerData.GetBit(bitPos + 32 + i) == boxBits[i]);
      }
      Debug.Assert(statePlayerData.GetUShort(bitPos) == playerPos);
      Debug.Assert(statePlayerData.GetByte(bitPos + 16) == boxCount);
      Debug.Assert(statePlayerData.GetByte(bitPos + 24) == finishedBoxCount);
      statePlayerUsed++;
    }

    /// <summary>
    /// fügt einen weiteren Raum-Zustand ohne Spieler hinzu
    /// </summary>
    /// <param name="boxCount">Anzahl der enthaltenen Kisten</param>
    /// <param name="finishedBoxCount">Anzahl der enthalten Kisten, welche auf Zielfeldern stehen</param>
    /// <param name="boxBits">Bits der Felder für markierten Kisten</param>
    void AddBoxState(byte boxCount, byte finishedBoxCount, params bool[] boxBits)
    {
      Debug.Assert(boxCount <= fieldPosis.Length);
      Debug.Assert(finishedBoxCount <= boxCount);
      Debug.Assert(boxBits.Length == fieldPosis.Length);
      Debug.Assert(boxBits.Count(x => x) == boxCount);

      ulong bitPos = stateBoxUsed * stateBoxElement;
      stateBoxData.SetByte(bitPos, boxCount);
      stateBoxData.SetByte(bitPos + 8, finishedBoxCount);
      stateBoxData.ClearBits(bitPos + 16, (uint)boxBits.Length);
      for (uint i = 0; i < boxBits.Length; i++)
      {
        if (boxBits[i]) stateBoxData.SetBit(bitPos + 16 + i);
        Debug.Assert(stateBoxData.GetBit(bitPos + 16 + i) == boxBits[i]);
      }
      Debug.Assert(stateBoxData.GetByte(bitPos) == boxCount);
      Debug.Assert(stateBoxData.GetByte(bitPos + 8) == finishedBoxCount);
      stateBoxUsed++;
    }

    /// <summary>
    /// gibt einen bestimmten Raumzustand mit Spieler zurück (für Debug-Zwecke)
    /// </summary>
    /// <param name="statePlayerIndex">Zustand-Index, welcher ausgewählt wird (muss kleiner als statePlayerUsed sein)</param>
    /// <returns>Zustand des Raumes mit Spieler</returns>
    public StateDebugInfo GetPlayerStateInfo(uint statePlayerIndex)
    {
      Debug.Assert(statePlayerIndex < statePlayerUsed);

      ulong bitPos = statePlayerIndex * statePlayerElement;

      return new StateDebugInfo(
        this, // Room
        statePlayerData.GetUShort(bitPos),    // Player-Pos
        statePlayerData.GetByte(bitPos + 16), // Box-Count
        statePlayerData.GetByte(bitPos + 24), // Box-Count (finished)
        Enumerable.Range(0, fieldPosis.Length).Where(i => statePlayerData.GetBit(bitPos + 32 + (ulong)i)).Select(i => fieldPosis[i]).ToArray() // Box-Positions
      );
    }

    /// <summary>
    /// gibt einen bestimmten Raumzustand ohne Spieler zurück (für Debug-Zwecke)
    /// </summary>
    /// <param name="stateBoxIndex">Zustand-Index, welcher ausgewählt wird (muss kleiner als stateBoxUsed sein)</param>
    /// <returns>Zustand des Raumes ohne Spieler</returns>
    public StateDebugInfo GetBoxStateInfo(uint stateBoxIndex)
    {
      Debug.Assert(stateBoxIndex < stateBoxUsed);

      ulong bitPos = stateBoxIndex * stateBoxElement;

      return new StateDebugInfo(
        this, // Room
        0,    // Player-Pos = null
        stateBoxData.GetByte(bitPos), // Box-Count
        stateBoxData.GetByte(bitPos + 8), // Box-Count (finished)
        Enumerable.Range(0, fieldPosis.Length).Where(i => stateBoxData.GetBit(bitPos + 16 + (ulong)i)).Select(i => fieldPosis[i]).ToArray() // Box-Positions
      );
    }

    /// <summary>
    /// fragt einen bestimmten Raumzustand ab (für Debug-Zwecke)
    /// </summary>
    /// <param name="stateIndex">Zustand, welcher ausgewählt wird (muss kleiner als stateDataUsed sein)</param>
    /// <returns>Zustand des Raumes</returns>
    public StateDebugInfo GetStateInfo(uint stateIndex)
    {
      if (stateIndex >= statePlayerUsed + stateBoxUsed) throw new ArgumentOutOfRangeException("stateIndex");

      if (stateIndex < statePlayerUsed)
      {
        return GetPlayerStateInfo(stateIndex);
      }
      else
      {
        return GetBoxStateInfo(stateIndex - statePlayerUsed);
      }
    }
    #endregion

    #region # // --- Varianten-Methoden ---
    /// <summary>
    /// initialisiert die ersten Varianten
    /// </summary>
    public void InitVariants()
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(statePlayerUsed + stateBoxUsed > 0);
      Debug.Assert(variantsDataUsed == 0);

      int pos = fieldPosis.First();

      // --- Varianten hinzufügen, wo der Spieler im Raum verbleibt ---
      for (uint state = 0; state < statePlayerUsed; state++)
      {
        var st = GetPlayerStateInfo(state);

      }

      // --- Varianten hinzufügen, wo der Spieler den Raum verlässt ---
      for (uint state = 0; state < stateBoxUsed; state++)
      {
        var st = GetBoxStateInfo(state);
      }
    }

    /// <summary>
    /// fügt eine weitere Variante hinzu
    /// </summary>
    /// <param name="outgoingPortal">gibt das Portal an, wo eine Kiste rausgeschoben wurde (oder -1, wenn keine Kiste den Raum verlässt)</param>
    /// <param name="outgoingState">gibt den erreichbaren End-Zustand</param>
    /// <param name="moves">Anzahl der Laufschritte, welche für den neuen Zustand nötig sind (inkl. Kistenverschiebungen)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen, welche für den neuen Zustand nötig sind</param>
    void AddVariant(int outgoingPortal, uint outgoingState, uint moves, uint pushes)
    {
      Debug.Assert(outgoingPortal == -1 || (outgoingPortal >= 0 && outgoingPortal < outgoingPortals.Length && outgoingPortal < 0xff));
      Debug.Assert(outgoingState < statePlayerUsed + stateBoxUsed);
      Debug.Assert(moves > 0 && moves < 16777216);
      Debug.Assert(pushes < 16777216 && pushes <= moves);

      ulong bitPos = variantsDataUsed * variantsDateElement;
      variantsData.SetByte(bitPos, (byte)(uint)outgoingPortal);
      variantsData.SetUInt(bitPos + 8, outgoingState);
      variantsData.SetUInt24(bitPos + 40, moves);
      variantsData.SetUInt24(bitPos + 64, pushes);

      Debug.Assert(variantsData.GetByte(bitPos) == outgoingPortal || (variantsData.GetByte(bitPos) == 0xff && outgoingPortal == -1));
      Debug.Assert(variantsData.GetUInt(bitPos + 8) == outgoingState);
      Debug.Assert(variantsData.GetUInt24(bitPos + 40) == moves);
      Debug.Assert(variantsData.GetUInt24(bitPos + 64) == pushes);
      variantsDataUsed++;
    }
    #endregion

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      for (int i = 0; i < incomingPortals.Length; i++)
      {
        if (incomingPortals[i] != null) incomingPortals[i].Dispose();
        incomingPortals[i] = null;
      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~Room()
    {
      Dispose();
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new
      {
        startPos = fieldPosis.Min(),
        size = fieldPosis.Length,
        incomingPortals = incomingPortals.Length + ": " + string.Join(", ", incomingPortals.AsEnumerable()),
        posis = string.Join(",", fieldPosis.OrderBy(pos => pos))
      }.ToString();
    }
  }
}
