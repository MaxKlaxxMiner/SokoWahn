#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable RedundantIfElseBlock
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBeMadeStatic.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse, welche einen kompletten Raum darstellt
  /// </summary>
  public class Room : IDisposable
  {
    #region # // --- Variablen ---
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
    public readonly ulong variantsDataElement;
    /// <summary>
    /// Anzahl der benutzen Varianten-Elemente
    /// </summary>
    public uint variantsDataUsed;
    /// <summary>
    /// merkt sich alle Start-Varianten, wo der Spieler im Raum verbleibt
    /// </summary>
    public readonly List<uint> startPlayerVariants = new List<uint>();
    /// <summary>
    /// merkt sich alle Start-Varianten, wo der Spieler den Raum verlässt
    /// </summary>
    public readonly List<uint> startBoxVariants = new List<uint>();
    #endregion

    #region # // --- Konstruktor ---
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
      statePlayerData = new Bitter(statePlayerElement * 1UL); // Raum mit einzelnen Spieler-Feld kann nur ein Zustand annehmen: 1 = Spieler
      statePlayerUsed = 0;

      variantsDataElement = sizeof(uint) * 8  // vorheriger Raum-Zustand
                          + sizeof(byte) * 8  // das verwendete ausgehende Portal (0xff == kein Ausgang benutzt)
                          + sizeof(uint) * 8  // Raum-Zustand, welcher erreicht werden kann
                          + 3 * 8             // Anzahl der Laufschritte, welche benötigt werden (inkl. Kistenverschiebungen)
                          + 3 * 8;            // Anzahl der Kistenverschiebungen, welche benötigt werden
      variantsData = new Bitter(variantsDataElement * (4 * 4 + 4 * 4 + 4 + 4)); // (4 in-Kisten * 4 out-Kisten) + (4 in-Spieler * 4 out-Spieler) + (4 Starts) + (4 Ziele)
      variantsDataUsed = 0;
    }
    #endregion

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
          AddPlayerState(pos, 0, 0, false);   // | Start | Ende | Spieler auf einem Feld
          AddBoxState(0, 0, false);           // |     - | Ende | leeres Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddBoxState(1, 0, true);          // |     - |    - | Kiste auf einem Feld
          }
        } break;

        case '+': // Spieler auf einem Zielfeld
        {
          AddPlayerState(pos, 0, 0, false);   // | Start |    - | Spieler auf einem Zielfeld
          AddBoxState(0, 0, false);           // |     - |    - | leeres Zielfeld
          AddBoxState(1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
        } break;

        case ' ': // leeres Feld
        {
          AddBoxState(0, 0, false);           // | Start | Ende | leeres Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddPlayerState(pos, 0, 0, false); // |     - | Ende | Spieler auf einem Feld
            AddBoxState(1, 0, true);          // |     - |    - | Kiste auf einem Feld
          }
        } break;

        case '.': // Zielfeld
        {
          AddBoxState(0, 0, false);           // | Start |    - | leeres Zielfeld
          AddBoxState(1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
          if (!field.CheckCorner(pos)) // Spieler kann sich nur auf dem Feld befinden, wenn die Kiste rausgeschoben wurde (was bei einer Ecke nicht möglich ist)
          {
            AddPlayerState(pos, 0, 0, false); // |     - |    - | Spieler auf einem Zielfeld
          }
        } break;

        case '$': // Feld mit Kiste
        {
          AddBoxState(1, 0, true);            // | Start |    - | Kiste auf einem Feld
          AddBoxState(0, 0, false);           // |     - | Ende | leeres Feld
          AddPlayerState(pos, 0, 0, false);   // |     - | Ende | Spieler auf einem leeren Feld
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
      Debug.Assert(field.ValidPos(pos));

      // --- Start-Varianten hinzufügen, wo der Spieler sich im Raum befindet ---
      if (field.IsPlayer(pos)) // Spieler befindet sich am Anfang des Spiels im Raum
      {
        Debug.Assert(GetStateInfo(0).playerPos == pos);
        Debug.Assert(GetBoxStateInfo(0).boxCount == 0);
        for (int outgoingPortal = 0; outgoingPortal < outgoingPortals.Length; outgoingPortal++)
        {
          int nextPos = outgoingPortals[outgoingPortal].posTo;
          if (field.IsBox(nextPos)) // Kiste zum verschieben am Anfang erkannt?
          {
            nextPos += nextPos - pos;
            if (field.CheckCorner(nextPos) && field.GetField(nextPos) != '.') continue; // Kiste würde in die Ecke geschoben werden und ist kein Zielfeld
            if (!field.IsFree(nextPos)) continue; // dahinter liegendes Feld ist am Anfang blockiert
          }

          // Variante hinzufügen: Spieler steht zu Spielbegin auf dem Feld und verlässt nun den Raum (Moves: 1, Pushes: 0)
          startBoxVariants.Add(variantsDataUsed);
          AddVariant(0, outgoingPortal, statePlayerUsed, 1, 0);
        }
      }
      else // Spieler befindet sich im Raum, weil vorher eine Kiste rausgeschoben wurde
      {
        for (uint startState = 0; startState < statePlayerUsed; startState++)
        {
          var startSt = GetPlayerStateInfo(startState); Debug.Assert(startSt.playerPos == pos); Debug.Assert(startSt.boxCount == 0);
          var foundBoxVariants = new List<int>(); // theoretische Box-Varianten ermitteln (wohin eventuell eine Kiste geschoben wurde)
          if (field.ValidPos(pos - 1) && (field.IsGoal(pos + 1) || !field.CheckCorner(pos + 1))) foundBoxVariants.Add(pos + 1); // Kiste nach rechts geschoben
          if (field.ValidPos(pos + 1) && (field.IsGoal(pos - 1) || !field.CheckCorner(pos - 1))) foundBoxVariants.Add(pos - 1); // Kiste nach links geschoben
          if (field.ValidPos(pos - field.Width) && (field.IsGoal(pos + field.Width) || !field.CheckCorner(pos + field.Width))) foundBoxVariants.Add(pos + field.Width); // Kiste nach unten geschoben
          if (field.ValidPos(pos + field.Width) && (field.IsGoal(pos - field.Width) || !field.CheckCorner(pos - field.Width))) foundBoxVariants.Add(pos - field.Width); // Kiste nach oben geschoben
          if (foundBoxVariants.Count == 0) continue; // keine Variante mit rausgeschobener Kiste gefunden

          for (int outgoingPortal = 0; outgoingPortal < outgoingPortals.Length; outgoingPortal++)
          {
            // prüfen, ob bei der einzigen Kisten-Variante die Kiste nicht mehr weitergeschoben werden darf
            if (foundBoxVariants.Count == 1 && outgoingPortals[outgoingPortal].posTo == foundBoxVariants[0]) // einzige Kisten-Variante erkannt
            {
              int nextPos = foundBoxVariants[0] + foundBoxVariants[0] - pos;
              if (field.CheckCorner(nextPos) && !field.IsGoal(nextPos)) continue; // Kiste darf nicht in eine Ecke geschoben werden
            }

            for (uint endState = 0; endState < stateBoxUsed; endState++)
            {
              var endSt = GetBoxStateInfo(endState); Debug.Assert(endSt.playerPos == 0);
              if (endSt.boxCount > 0) continue; // Feld muss nach dem Verlassen leer sein

              // Variante hinzufügen: Spieler hatte mit dem betreten eine Kiste aus dem Raum geschoben und verlässt nun den Raum wieder (Moves: 1, Pushes: 0)
              startBoxVariants.Add(variantsDataUsed);
              AddVariant(startState, outgoingPortal, endState + statePlayerUsed, 1, 0);
            }
          }
        }
      }

      // --- Varianten hinzufügen, wo der Spieler im Raum bleibt ---
      for (uint endState = 0; endState < statePlayerUsed; endState++) // Zustände durcharbeiten, wo der Spieler im Raum verbleibt
      {
        var endSt = GetPlayerStateInfo(endState); Debug.Assert(endSt.playerPos == pos);
        foreach (var portal in incomingPortals) // eingehende Portale verarbeiten
        {
          for (uint startState = 0; startState < stateBoxUsed; startState++) // nur Zustände beachten, wo der Spieler vorher noch nicht im Raum war
          {
            var startSt = GetBoxStateInfo(startState); Debug.Assert(startSt.playerPos == 0);
            if (startSt.boxCount == 0) continue; // wenn keine Kiste vorher zum verschieben vorhanden war, macht der End-Aufenthalt des Spieler keinen Sinn mehr
            int outgoingPortal = -1; // ausgehendes Portal suchen (für die rausgeschobene Kiste)
            for (int i = 0; i < outgoingPortals.Length; i++)
            {
              if (outgoingPortals[i].posTo - outgoingPortals[i].posFrom == portal.posTo - portal.posFrom)
              {
                Debug.Assert(outgoingPortal == -1);
                outgoingPortal = i;
              }
            }
            if (outgoingPortal == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt
            if (field.CheckCorner(outgoingPortals[outgoingPortal].posTo) && !field.IsGoal(outgoingPortals[outgoingPortal].posTo)) continue; // Kiste darf nicht in eine Ecke geschoben werden

            // Variante hinzufügen: Spieler betritt den Raum, vorhandene Kiste wird rausgeschoben (Moves: 0, Pushes: 1), Info: eingehende Spielerbewegung wird noch nicht als Move gewertet
            portal.roomToPlayerVariants.Add(variantsDataUsed);
            AddVariant(startState + statePlayerUsed, outgoingPortal, endState, 0, 1);
          }
        }
      }

      // --- Varianten hinzufügen, wo der Spieler nicht im Raum bleibt ---
      for (uint endState = 0; endState < stateBoxUsed; endState++)
      {
        var endSt = GetBoxStateInfo(endState); Debug.Assert(endSt.playerPos == 0);

        foreach (var portal in incomingPortals) // eingehende Portale verarbeiten
        {
          if (endSt.boxCount > 0) // Feld mit Kiste
          {
            for (uint startState = 0; startState < stateBoxUsed; startState++)
            {
              var startSt = GetBoxStateInfo(startState); Debug.Assert(startSt.playerPos == 0);
              if (startSt.boxCount > 0) continue;
              Debug.Assert(startSt.boxCount == 0);

              // Kiste kann doch nicht reingeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?
              if (!field.ValidPos(portal.posFrom + portal.posFrom - pos)) continue;

              // Variante hinzufügen: Kiste wird in den Raum geschoben (Moves: 0, Pushes: 0), Info: eingehende Kistenbewegung wird noch nicht als Push gewertet
              portal.roomToBoxVariants.Add(variantsDataUsed);
              AddVariant(startState + statePlayerUsed, -1, endState + statePlayerUsed, 0, 0);
            }
          }
          else // leeres Feld bleibt leer -> Spieler durchquert nur den Raum
          {
            for (uint startState = 0; startState < stateBoxUsed; startState++)
            {
              var startSt = GetBoxStateInfo(startState); Debug.Assert(startSt.playerPos == 0);
              if (startSt.boxCount > 0) continue; // Raum ist mit einer Kiste belegt -> Spieler kann nicht durchlaufen (Push wird weiter oben verarbeitet)
              Debug.Assert(startSt.boxCount == 0);
              for (int outgoingPortal = 0; outgoingPortal < outgoingPortals.Length; outgoingPortal++)
              {
                if (portal.posFrom == outgoingPortals[outgoingPortal].posTo) continue; // direktes rein- und rauslaufen vermeiden

                // Variante hinzufügen: Spieler betritt den Raum und verlässt diesen über ein anderes Portal (Moves: 1, Pushes: 0), Info: nur der ausgehende Schritt wird als Move gewertet
                portal.roomToPlayerVariants.Add(variantsDataUsed);
                AddVariant(startState + statePlayerUsed, outgoingPortal, endState + statePlayerUsed, 1, 0);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// fügt eine weitere Variante hinzu
    /// </summary>
    /// <param name="incomingState">Vorher-Zustand</param>
    /// <param name="outgoingPortal">das Portal an, wo eine Kiste rausgeschoben wurde oder der Spieler den Raum verlassen hat (oder -1, wenn nichts den Raum verlässt)</param>
    /// <param name="outgoingState">erreichbaren End-Zustand</param>
    /// <param name="moves">Anzahl der Laufschritte, welche für den neuen Zustand nötig sind (inkl. Kistenverschiebungen)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen, welche für den neuen Zustand nötig sind</param>
    void AddVariant(uint incomingState, int outgoingPortal, uint outgoingState, uint moves, uint pushes)
    {
      Debug.Assert(incomingState < statePlayerUsed + stateBoxUsed);
      Debug.Assert(outgoingPortal == -1 || (outgoingPortal >= 0 && outgoingPortal < outgoingPortals.Length && outgoingPortal < 0xff));
      Debug.Assert(outgoingState < statePlayerUsed + stateBoxUsed);
      Debug.Assert(moves < 16777216);
      Debug.Assert(pushes < 16777216 && pushes <= moves + 1);

      ulong bitPos = variantsDataUsed * variantsDataElement;
      variantsData.SetUInt(bitPos, incomingState);
      variantsData.SetByte(bitPos + 32, (byte)(uint)outgoingPortal);
      variantsData.SetUInt(bitPos + 40, outgoingState);
      variantsData.SetUInt24(bitPos + 72, moves);
      variantsData.SetUInt24(bitPos + 96, pushes);

      Debug.Assert(variantsData.GetUInt(bitPos) == incomingState);
      Debug.Assert(variantsData.GetByte(bitPos + 32) == outgoingPortal || (variantsData.GetByte(bitPos + 32) == 0xff && outgoingPortal == -1));
      Debug.Assert(variantsData.GetUInt(bitPos + 40) == outgoingState);
      Debug.Assert(variantsData.GetUInt24(bitPos + 72) == moves);
      Debug.Assert(variantsData.GetUInt24(bitPos + 96) == pushes);
      Debug.Assert(variantsDataElement == 120);
      variantsDataUsed++;
    }

    /// <summary>
    /// gibt den eingehenden und ausgehenden Zustand einer Variante zurück
    /// </summary>
    /// <param name="variantIndex">Variante, welche abgefragt werden soll</param>
    /// <returns>ein- und ausgehender Zustand</returns>
    public KeyValuePair<uint, uint> GetVariantStates(uint variantIndex)
    {
      ulong bitPos = variantIndex * variantsDataElement;
      uint incomingState = variantsData.GetUInt(bitPos);
      uint outgoingState = variantsData.GetUInt(bitPos + 40);
      return new KeyValuePair<uint, uint>(incomingState, outgoingState);
    }

    /// <summary>
    /// fragt eine bestimmte Variante ab (für Debug-Zwecke)
    /// </summary>
    /// <param name="variantIndex">Index der Variante</param>
    /// <returns>fertig ausgelesene Variante</returns>
    public VariantDebugInfo GetVariantInfo(uint variantIndex)
    {
      Debug.Assert(variantIndex < variantsDataUsed);

      ulong bitPos = variantIndex * variantsDataElement;
      var incomingPortal = incomingPortals.FirstOrDefault(p => p.roomToPlayerVariants.Any(x => x == variantIndex) || p.roomToBoxVariants.Any(x => x == variantIndex));
      bool incomingBox = incomingPortal != null && incomingPortal.roomToBoxVariants.Any(x => x == variantIndex);
      uint incomingState = variantsData.GetUInt(bitPos);
      uint outgoingState = variantsData.GetUInt(bitPos + 40);

      return new VariantDebugInfo(
        incomingPortal,
        incomingBox,
        incomingState,
        variantsData.GetByte(bitPos + 32) == 0xff ? null : outgoingPortals[variantsData.GetByte(bitPos + 32)],
        !incomingBox && GetStateInfo(incomingState).boxCount > GetStateInfo(outgoingState).boxCount,
        outgoingState,
        variantsData.GetUInt24(bitPos + 72),
        variantsData.GetUInt24(bitPos + 96)
      );
    }
    #endregion

    #region # // --- Optimize ---
    /// <summary>
    /// prüft den eigenen Raum, ob Zustände bekannt sind, wo Kisten enthalten sein können
    /// </summary>
    /// <returns>true, wenn Zustände mit enthaltenen Kisten gefunden wurden</returns>
    public bool HasBoxStates()
    {
      for (uint state = 0; state < stateBoxUsed; state++)
      {
        if (GetBoxStateInfo(state).boxCount > 0) return true;
      }

      for (uint state = 0; state < statePlayerUsed; state++)
      {
        if (GetPlayerStateInfo(state).boxCount > 0) return true;
      }

      return false;
    }

    /// <summary>
    /// prüft, ob mindestens eine Kiste bei der Start-Stellung enthalten ist
    /// </summary>
    /// <returns>true, wenn mindestens eine Kiste beim Start vorhanden ist</returns>
    public bool HasStartBoxes()
    {
      return fieldPosis.Any(pos => field.IsBox(pos));
    }

    /// <summary>
    /// optimiert den Raum und deren Verbindungen zu den benachbarten Räumen und gibt die Anzahl der durchgeführten Optimierungen zurück
    /// </summary>
    /// <param name="maxCount">maximale Anzahl der erlaubten Optimierungen</param>
    /// <param name="output">Ausgabe-Liste mit den durchgeführten Optimierungen</param>
    /// <returns>Anzahl der insgesamt durchgeführten Optimierungen</returns>
    public int Optimize(int maxCount, List<KeyValuePair<string, int>> output)
    {
      if (incomingPortals.Length < 3) return 0;

      foreach (var incomingPortal in incomingPortals)
      {
        var outgoingPortal = incomingPortal.oppositePortal;
      }

      return 0;
    }
    #endregion

    #region # // --- Dispose ---
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
    #endregion

    #region # // --- ToString() ---
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
    #endregion
  }
}
