using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

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
    /// merkt sich die Daten der Zustände
    /// </summary>
    public readonly Bitter stateData;
    /// <summary>
    /// Größe eines einzelnen Zustand-Elementes (in Bits)
    /// </summary>
    public readonly ulong stateDataElement;
    /// <summary>
    /// Anzahl der benutzen Zustand-Elemente
    /// </summary>
    public uint stateDataUsed;

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

      stateDataElement = sizeof(ushort) * 8        // Spieler-Position (wenn auf dem Spielfeld vorhanden, sonst = 0)
                       + sizeof(byte) * 8          // Anzahl der Kisten, welche sich auf dem Spielfeld befinden
                       + sizeof(byte) * 8          // Anzahl der Kisten, welche sich bereits auf Zielfelder befinden
                       + (ulong)fieldPosis.Length; // Bit-markierte Felder, welche mit Kisten belegt sind
      stateData = new Bitter(stateDataElement * (ulong)fieldPosis.Length * 3UL); // Raum mit einen einzelnen Feld kann nur drei Zustände annehmen: 1 = leer, 2 = Spieler, 3 = Kiste
      stateDataUsed = 0;

      variantsDateElement = 1                 // gibt an, ob durch den Ausgang eine Kiste geschoben wurde (sonst: Spieler)
                          + sizeof(byte) * 8  // das verwendete ausgehende Portal (0xff == kein Ausgang benutzt)
                          + sizeof(uint) * 8  // Status, welcher der eigene Raum erhält
                          + 3 * 8             // Anzahl der Laufschritte, welche benötigt werden (inkl. Kistenverschiebungen)
                          + 3 * 8;            // Anzahl der Kistenverschiebungen, welche benötigt werden
      variantsData = new Bitter(variantsDateElement * (4 * 4 + 4 * 4 + 4 + 4)); // (4 eigehende Kisten * 4 ausgehende Kisten) + (4 eingehden Spieler * 4 ausgehende Spieler) + (4 Starts) + (4 Ziele)
      variantsDataUsed = 0;

      switch (field.GetField(pos))
      {
        case '@': // Spieler auf einem leeren Feld
        {
          AddState((ushort)pos, 0, 0, false); // | Start | Ende | Spieler auf einem leeren Feld
          AddState(0, 0, 0, false);           // |     - | Ende | leeres Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddState(0, 1, 0, true);          // |     - |    - | Kiste auf einem leeren Feld
          }
        } break;

        case '+': // Spieler auf einem Zielfeld
        {
          AddState((ushort)pos, 0, 0, false); // | Start |    - | Spieler auf einem Zielfeld
          AddState(0, 0, 0, false);           // |     - |    - | leeres Zielfeld
          AddState(0, 1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
        } break;

        case ' ': // leeres Feld
        {
          AddState(0, 0, 0, false);           // | Start | Ende | leeres Feld
          AddState((ushort)pos, 0, 0, false); // |     - | Ende | Spieler auf einem leeren Feld
          if (!field.CheckCorner(pos)) // Kiste darf nicht in einer Ecke stehen
          {
            AddState(0, 1, 0, true);          // |     - |    - | Kiste auf einem leeren Feld
          }
        } break;

        case '.': // Zielfeld
        {
          AddState(0, 0, 0, false);           // | Start |    - | leeres Zielfeld
          AddState((ushort)pos, 0, 0, false); // |     - |    - | Spieler auf einem Zielfeld
          AddState(0, 1, 1, true);            // |     - | Ende | Kiste auf einem Zielfeld
        } break;

        case '$': // Feld mit Kiste
        {
          AddState(0, 1, 0, true);            // | Start |    - | Kiste auf einem leeren Feld
          AddState(0, 0, 0, false);           // |     - | Ende | leeres Feld
          AddState((ushort)pos, 0, 0, false); // |     - | Ende | Spieler auf einem leeren Feld
          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
        } break;

        case '*': // Kiste auf einem Zielfeld
        {
          AddState(0, 1, 1, true);              // | Start | Ende | Kiste auf einem Zielfeld
          if (!field.CheckCorner(pos)) // Kiste kann weggeschoben werden?
          {
            AddState(0, 0, 0, false);           // |     - |    - | leeres Feld
            AddState((ushort)pos, 0, 0, false); // |     - |    - | Spieler auf einem leeren Feld
          }
        } break;

        default: throw new NotSupportedException("char: " + field.GetField(pos));
      }

      // --- ausgehende Varianten hinzufügen (nur bei Start-Stellung) ---
      if (field.GetField(pos) == '@' || field.GetField(pos) == '+')
      {
        for (int outgoingPortalIndex = 0; outgoingPortalIndex < outgoingPortals.Length; outgoingPortalIndex++)
        {
          var outgoingPortal = outgoingPortals[outgoingPortalIndex];
        }
      }

      // --- eingehende Varianten hinzufügen (End-Stellung) ---

      // --- eingehende und ausgehende Varianten hinzufügen (mittleres Spiel) ---
      for (int incomingPortalIndex = 0; incomingPortalIndex < incomingPortals.Length; incomingPortalIndex++)
      {
        var incomingPortal = incomingPortals[incomingPortalIndex];

      }
    }

    #region # // --- Zustand-Methoden ---
    /// <summary>
    /// fügt einen weiteren Raum-Zustand hinzu
    /// </summary>
    void AddState(ushort playerPos, byte boxCount, byte finishedBoxCount, params bool[] boxBits)
    {
      Debug.Assert(playerPos < field.Width * field.Height);
      Debug.Assert(playerPos == 0 || field.ValidPos(playerPos));
      Debug.Assert(boxCount <= fieldPosis.Length);
      Debug.Assert(finishedBoxCount <= boxCount);
      Debug.Assert(boxBits.Length == fieldPosis.Length);
      Debug.Assert(boxBits.Count(x => x) == boxCount);

      ulong bitPos = stateDataUsed * stateDataElement;
      stateData.SetUShort(bitPos, playerPos);
      stateData.SetByte(bitPos + 16, boxCount);
      stateData.SetByte(bitPos + 24, finishedBoxCount);
      stateData.ClearBits(bitPos + 32, (uint)boxBits.Length);
      for (uint i = 0; i < boxBits.Length; i++)
      {
        if (boxBits[i]) stateData.SetBit(bitPos + 32 + i);
        Debug.Assert(stateData.GetBit(bitPos + 32 + i) == boxBits[i]);
      }
      Debug.Assert(stateData.GetUShort(bitPos) == playerPos);
      Debug.Assert(stateData.GetByte(bitPos + 16) == boxCount);
      Debug.Assert(stateData.GetByte(bitPos + 24) == finishedBoxCount);
      stateDataUsed++;
    }

    /// <summary>
    /// fragt einen betsimmten Raumzustand ab (für Debug-Zwecke)
    /// </summary>
    /// <param name="stateIndex">Zustand, welcher ausgewählt wird (muss kleiner als stateDataUsed sein)</param>
    /// <returns>Zustand des Raumes</returns>
    public StateDebugInfo GetStateInfo(uint stateIndex)
    {
      if (stateIndex >= stateDataUsed) throw new ArgumentOutOfRangeException("stateIndex");
      ulong bitPos = stateIndex * stateDataElement;

      return new StateDebugInfo(
        this, // Room
        stateData.GetUShort(bitPos),    // Player-Pos
        stateData.GetByte(bitPos + 16), // Box-Count
        stateData.GetByte(bitPos + 24), // Box-Count (finished)
        Enumerable.Range(0, fieldPosis.Length).Where(i => stateData.GetBit(bitPos + 32 + (ulong)i)).Select(i => fieldPosis[i]).ToArray() // Box-Positions
      );
    }
    #endregion

    #region # // --- Varianten-Methoden ---
    /// <summary>
    /// fügt eine weitere Variante hinzu
    /// </summary>
    /// <param name="outgoingBox">gibt an, ob eine Kiste durch das ausgehende Portal geschoben wurde (sonst: Spieler)</param>
    /// <param name="outgoingPortal">ausgehendes Portal, welches für den ausgehenden Verkehr zuständig ist (-1: kein Ausgang verzeichnet, z.B. bei der Ziel-Stellung)</param>
    /// <param name="outgoingState">ausgehender End-Zustand</param>
    /// <param name="moves">Anzahl der Laufschritte, welche für den neuen Zustand nötig sind (inkl. Kistenverschiebungen)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen, welche für den neuen Zustand nötig sind</param>
    void AddVariant(bool outgoingBox, int outgoingPortal, uint outgoingState, int moves, int pushes)
    {
      Debug.Assert(!outgoingBox || outgoingPortal >= 0);
      Debug.Assert(outgoingPortal == -1 || (outgoingPortal >= 0 && outgoingPortal < outgoingPortals.Length && outgoingPortal<0xff));
      Debug.Assert(outgoingState < stateDataUsed);
      Debug.Assert(moves > 0);
      Debug.Assert(pushes >= 0);

      ulong bitPos = variantsDataUsed * variantsDateElement;
      if (outgoingBox) variantsData.SetBit(bitPos); else variantsData.ClearBit(bitPos);
      variantsData.SetByte(bitPos + 1, (byte)(uint)outgoingPortal);
      variantsData.SetUInt(bitPos + 9, outgoingState);
      variantsData.SetUInt24(bitPos + 41, (uint)moves);
      variantsData.SetUInt24(bitPos + 65, (uint)pushes);

      Debug.Assert(variantsData.GetBit(bitPos) == outgoingBox);
      Debug.Assert(variantsData.GetByte(bitPos + 1) == outgoingPortal || (variantsData.GetByte(bitPos + 1) == 0xff && outgoingPortal == -1));
      Debug.Assert(variantsData.GetUInt(bitPos + 9) == outgoingState);
      Debug.Assert(variantsData.GetUInt24(bitPos + 41) == moves);
      Debug.Assert(variantsData.GetUInt24(bitPos + 65) == pushes);
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
