
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum Lösen eines Spielfeldes
  /// </summary>
  public sealed class RoomSolver
  {
    /// <summary>
    /// aktuelles Räume-Netzwerk, welches für die Suche verwendet werden soll
    /// </summary>
    readonly RoomNetwork network;

    /// <summary>
    /// merkt sich die Räume im Netzwerk
    /// </summary>
    readonly Room[] rooms;

    /// <summary>
    /// merkt sich die Anzahl der Räume im Netzwerk
    /// </summary>
    readonly int roomCount;

    /// <summary>
    /// Anzahl der Bits für die Variante innerhalb eines 64-Bit Wertes
    /// </summary>
    const int VariantBits = 48;
    /// <summary>
    /// Bit-Maske für die Variante
    /// </summary>
    const ulong VariantMask = (1UL << VariantBits) - 1;

    /// <summary>
    /// aktueller Such-Status
    /// </summary>
    enum SolveState
    {
      /// <summary>
      /// initialer Anfangs-Zustand
      /// </summary>
      Init,
      /// <summary>
      /// fügt alle Start-Varianten hinzu
      /// </summary>
      AddStarts,
    }

    /// <summary>
    /// merkt sich den aktuellen Such-Status
    /// </summary>
    SolveState solveState = SolveState.Init;

    /// <summary>
    /// merkt aktuellen Spielfeld-Zustand
    /// </summary>
    readonly ulong[] currentState;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="network">Netzwerk, welches verwendet werden soll</param>
    public RoomSolver(RoomNetwork network)
    {
      if (network == null) throw new ArgumentNullException("network");
      this.network = network;
      rooms = network.rooms;
      if (rooms == null) throw new ArgumentNullException("network");
      roomCount = rooms.Length;
      currentState = new ulong[roomCount + 1]; // Raum-Zustände[] + (uhort)Raum-Nummer | ausgewählte Variante
    }

    /// <summary>
    /// sucht nach der Lösung
    /// </summary>
    /// <param name="maxTicks">maximale Anzahl der Rechenschritte</param>
    /// <returns>true, wenn eine Lösung gefunden wurde, sonst: false</returns>
    public bool Search(int maxTicks)
    {
      switch (solveState)
      {
        #region # // --- Init - erste Initialisierung ---
        case SolveState.Init:
        {
          if (network.rooms.Length > 65535) throw new SokoFieldException("to many rooms (" + network.rooms.Length + " > 65535)");
          int startRoom = -1; // Raum suchen, wo der Spieler beginnt
          for (int i = 0; i < network.rooms.Length; i++)
          {
            currentState[i] = network.rooms[i].startState;
            if (network.rooms[i].variantList.Count > VariantMask) throw new SokoFieldException("overflow variant.Count");
            if (network.rooms[i].startVariantCount > 0)
            {
              if (startRoom >= 0) throw new SokoFieldException("duplicate start-room");
              startRoom = i;
            }
          }
          if (startRoom < 0) throw new SokoFieldException("no start-room");
          currentState[roomCount] = (ulong)(uint)startRoom << VariantBits;
          solveState = SolveState.AddStarts;
        } break;
        case SolveState.AddStarts:
        {
          uint startRoom = (uint)(currentState[roomCount] >> VariantBits);
          ulong variantId = currentState[roomCount] & VariantMask;

          // todo: Start-Variante in Aufgaben-Liste merken

          // zur nächsten Start-Variante springen
          variantId++;
          currentState[roomCount]++;

          if (variantId == rooms[startRoom].startVariantCount) // alle Start-Varianten hinzugefügt?
          {
            throw new NotImplementedException();
          }
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    /// <summary>
    /// gibt die aktuelle Spielerposition zurück
    /// </summary>
    public int CurrentPlayerPos
    {
      get
      {
        uint currentRoom = (uint)(currentState[roomCount] >> VariantBits);
        ulong currentVariant = currentState[roomCount] & VariantMask;

        switch (solveState)
        {
          case SolveState.AddStarts: return rooms[currentRoom].outgoingPortals[rooms[currentRoom].variantList.GetData(currentVariant).playerPortal].toPos;
          default: return network.field.PlayerPos;
        }
      }
    }

    /// <summary>
    /// gibt die aktuelle Kisten-Positionen zurücfk
    /// </summary>
    public int[] CurrentBoxes
    {
      get
      {
        switch (solveState)
        {
          case SolveState.Init: return Enumerable.Range(0, network.field.Width * network.field.Height).Where(network.field.IsBox).ToArray();
          default:
          {
            var boxes = new List<int>();

            for (int r = 0; r < roomCount; r++)
            {
              boxes.AddRange(rooms[r].stateList.Get(currentState[r]));
            }

            return boxes.ToArray();
          }
        }
      }
    }

    /// <summary>
    /// gibt den aktuellen Such-Status zurück
    /// </summary>
    /// <returns>lesbarer Inhalt vom Such-Status</returns>
    public override string ToString()
    {
      var sb = new StringBuilder("\r\n  Hash: " + (0UL).ToString("N0") + "\r\n\r\n");
      sb.AppendLine(" State: " + solveState).AppendLine();

      uint currentRoom = (uint)(currentState[roomCount] >> VariantBits);
      ulong currentVariant = currentState[roomCount] & VariantMask;
      switch (solveState)
      {
        case SolveState.AddStarts:
        {
          sb.AppendLine(string.Format(" Add-Starts: {0:N0} / {1:N0}", (currentState[roomCount] & VariantMask) + 1, rooms[currentRoom].startVariantCount)).AppendLine();
        } break;
      }

      if (solveState != SolveState.Init)
      {
        sb.AppendLine(" Room: " + currentRoom);
        sb.AppendLine(" V-ID: " + currentVariant);
        sb.AppendLine(" Path: " + rooms[currentRoom].variantList.GetData(currentVariant).path);
      }

      return sb.ToString();
    }
  }
}
