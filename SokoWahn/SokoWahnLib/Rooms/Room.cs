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
    /// merkt sich die Portale zu den anderen Räumen
    /// </summary>
    public readonly RoomPortal[] portals;
    /// <summary>
    /// merkt sich die die Daten der Zustände
    /// </summary>
    public readonly Bitter stateData;
    /// <summary>
    /// Größe eines einzelnen Zustand-Elementes
    /// </summary>
    public readonly ulong stateDataElement;
    /// <summary>
    /// Anzahl der benutzen Zustand-Elemente
    /// </summary>
    public uint stateDataUsed;

    /// <summary>
    /// Konstruktor um ein Raum aus einem einzelnen Feld zu erstellen
    /// </summary>
    /// <param name="field">gesamtes Spielfeld, welches verwendet wird</param>
    /// <param name="pos">Position des Feldes, worraus der Raum generiert werden soll</param>
    /// <param name="portals">vorhandene Portale zu anderen Räumen</param>
    public Room(ISokoField field, int pos, RoomPortal[] portals)
    {
      if (field == null) throw new ArgumentNullException("field");
      if (!field.ValidPos(pos)) throw new ArgumentOutOfRangeException("pos");
      if (portals == null) throw new ArgumentNullException("portals");
      this.field = field;
      fieldPosis = new[] { pos };
      goalPosis = new HashSet<int>();
      if (field.GetField(pos) == '.' || field.GetField(pos) == '*') goalPosis.Add(pos);
      this.portals = portals;
      stateDataElement = sizeof(ushort) * 8        // Spieler-Position (wenn auf dem Spielfeld vorhanden, sonst = 0)
                       + sizeof(byte) * 8          // Anzahl der Kisten, welche sich auf dem Spielfeld befinden
                       + sizeof(byte) * 8          // Anzahl der Kisten, welche sich bereits auf Zielfelder befinden
                       + (ulong)fieldPosis.Length; // Bit-markierte Felder, welche mit Kisten belegt sind
      stateData = new Bitter(stateDataElement * (ulong)fieldPosis.Length * 3UL); // Raum mit einzelnen Feld kann nur drei Zustände annehmen: 1 = leer, 2 = Spieler, 3 = Kiste
      stateDataUsed = 0;
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
          AddState(0, 1, 1, true);            // | Start | Ende | Kiste auf einem Zielfeld
          if (!field.CheckCorner(pos)) // Kiste kann weggeschoben werden?
          {
            AddState(0, 0, 0, false);           // |     - |    - | leeres Feld
            AddState((ushort)pos, 0, 0, false); // |     - |    - | Spieler auf einem leeren Feld
          }
        } break;

        default: throw new NotSupportedException("char: " + field.GetField(pos));
      }
    }

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

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      for (int i = 0; i < portals.Length; i++)
      {
        if (portals[i] != null) portals[i].Dispose();
        portals[i] = null;
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
        portals = portals.Length + ": " + string.Join(", ", portals.AsEnumerable()),
        posis = string.Join(",", fieldPosis.OrderBy(pos => pos))
      }.ToString();
    }
  }
}
