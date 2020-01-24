using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
// ReSharper disable UnusedMethodReturnValue.Global

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
    readonly uint roomCount;

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
      AddStarts
    }

    /// <summary>
    /// merkt sich den aktuellen Such-Status
    /// </summary>
    SolveState solveState = SolveState.Init;

    /// <summary>
    /// merkt sich den aktuellen Spielfeld-Zustand
    /// </summary>
    readonly ulong[] currentState;

    /// <summary>
    /// merkt sich die abzuarbeitenden Aufgaben pro Zugtiefe
    /// </summary>
    readonly List<TaskList> moveForwardTasks = new List<TaskList>();

    /// <summary>
    /// merkt sich alle bereits verarbeiteten Hash-Einträge
    /// </summary>
    readonly HashCrc hashTable = new HashCrcNormal();

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
      roomCount = (uint)rooms.Length;
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
        #endregion
        #region # // --- AddStarts - Anfangswerte als Aufgaben hinzufügen ---
        case SolveState.AddStarts:
        {
          uint startRoom = (uint)(currentState[roomCount] >> VariantBits);
          ulong variantId = currentState[roomCount] & VariantMask;
          var tmpStates = new ulong[roomCount + 1];

          for (; maxTicks > 0; maxTicks--)
          {
            // --- Aufgabe prüfen und in die Aufgaben-Liste hinzufügen ---
            Array.Copy(currentState, tmpStates, tmpStates.Length);
            int okMoves = ResolveMoves(tmpStates, rooms[startRoom], variantId);
            if (okMoves >= 0)
            {
              ulong crc = Crc64.Start;
              for (int i = 0; i < tmpStates.Length; i++) crc = crc.Crc64Update(tmpStates[i]);
              hashTable.Add(crc, (uint)okMoves);

              while (okMoves >= moveForwardTasks.Count) moveForwardTasks.Add(new TaskListNormal(roomCount + 1));
              moveForwardTasks[okMoves].Add(tmpStates);
            }

            // --- zur nächsten Start-Variante springen ---
            variantId++;
            currentState[roomCount]++;

            if (variantId == rooms[startRoom].startVariantCount) // alle Start-Varianten bereits ermittelt?
            {
              throw new NotImplementedException();
            }
          }
        } break;
        #endregion
        default: throw new NotSupportedException(solveState.ToString());
      }

      return false;
    }

    /// <summary>
    /// berechnet eine neue Variante und die Anzahl der Laufschritte zurück (oder -1, wenn die Variante ungültig ist)
    /// </summary>
    /// <param name="states">Array mit den Zuständen, welche aktualisiert werden sollen</param>
    /// <param name="room">aktueller Raum für die Berechnung</param>
    /// <param name="variantId">Variante im Raum, welche verwendet werden soll</param>
    /// <returns>Anzahl der Laufschritte oder -1 wenn der Zug ungültig ist</returns>
    static int ResolveMoves(ulong[] states, Room room, ulong variantId)
    {
      Debug.Assert(variantId < room.variantList.Count);
      var vData = room.variantList.GetData(variantId);

      foreach (var boxPortal in vData.boxPortals) // Kisten wurden durch benachbarte Portale geschoben?
      {
        throw new NotImplementedException();
      }

      if (vData.playerPortal == uint.MaxValue) // End-Stellung erreicht?
      {
        throw new NotImplementedException();
      }

      var oPortal = room.outgoingPortals[vData.playerPortal];
      if (!oPortal.variantStateDict.GetVariants(states[oPortal.toRoom.roomIndex]).Any()) return -1; // keine gültigen Varianten gefunden?

      Debug.Assert(vData.moves < int.MaxValue);
      return (int)(uint)vData.moves;
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
      var sb = new StringBuilder("\r\n");
      sb.AppendLine("  Hash: " + hashTable.Count.ToString("N0"));
      ulong tasks = 0;
      foreach (var t in moveForwardTasks) tasks += t.Count;
      sb.AppendLine(" Tasks: " + tasks.ToString("N0"));
      sb.AppendLine();
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
        sb.AppendLine();

        for (int moveIndex = 0; moveIndex < moveForwardTasks.Count; moveIndex++)
        {
          var task = moveForwardTasks[moveIndex];
          if (task.Count > 0) sb.AppendLine(" [" + moveIndex.ToString("N0") + "]: " + task.Count.ToString("N0"));
        }
      }

      return sb.ToString();
    }
  }
}
