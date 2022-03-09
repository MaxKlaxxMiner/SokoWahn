#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable ClassCanBeSealed.Local
// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
#pragma warning disable 649
#endregion

namespace SokoWahnLib.Rooms.Filter
{
  /// <summary>
  /// Klasse zum Erstellen und Filtern von Room-Profilen
  /// </summary>
  public class RoomProfileFilter : IDisposable
  {
    /// <summary>
    /// merkt sich den Raum, welcher optimiert werden soll
    /// </summary>
    readonly Room room;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="room">Room, welcher gefiltert werden soll</param>
    public RoomProfileFilter(Room room)
    {
      this.room = room;
    }

    class ProfileMove
    {
      /// <summary>
      /// aktueller Raumzustand
      /// </summary>
      public ulong state;
      /// <summary>
      /// Anzahl der Bewegungsschritte, um diesen Raumzustand zu erreichen
      /// </summary>
      public ulong totalMoveCount;
      /// <summary>
      /// Anzahl der Kistenbewegungen, um diesen Raumzustand zu erreichen
      /// </summary>
      public ulong totalPushCount;
      /// <summary>
      /// Anzahl der Kisten pro Portal, welche bisher importiert wurden
      /// </summary>
      public uint[] portalBoxInputCount;
      /// <summary>
      /// Anzahl der Kisten pro Portal, welche bisher exportiert wurden
      /// </summary>
      public uint[] portalBoxOuputCount;
      /// <summary>
      /// Portal, worüber der Spieler den Raum betreten hat (uint.MaxValue = wenn der Spieler vorher bereits im Raum war)
      /// </summary>
      public uint playerIncomingPortalIndex;
      /// <summary>
      /// Portal, worüber der Spieler den Raum wieder verlassen hat (uint.MaxValue = wenn der Raum nicht verlassen wurde)
      /// </summary>
      public uint playerOutgoingPortalIndex;
      /// <summary>
      /// Variante, welche benutzt wurde (oder ulong.MaxValue, wenn es nur eine Statusänderung war)
      /// </summary>
      public ulong variant;
      /// <summary>
      /// nächster Zug auf gleicher Ebene
      /// </summary>
      public ProfileMove nextMove;
      /// <summary>
      /// leerer Konstruktor
      /// </summary>
      public ProfileMove()
      {
      }
      /// <summary>
      /// Konstruktor, welche eine Kopie eines bestehenden Zuges erstellt
      /// </summary>
      /// <param name="cloneMove">Zug, welcher kopiert werden soll</param>
      public ProfileMove(ProfileMove cloneMove)
      {
        state = cloneMove.state;
        totalMoveCount = cloneMove.totalMoveCount;
        totalPushCount = cloneMove.totalPushCount;
        portalBoxInputCount = cloneMove.portalBoxInputCount.ToArray();
        portalBoxOuputCount = cloneMove.portalBoxOuputCount.ToArray();
        playerIncomingPortalIndex = cloneMove.playerIncomingPortalIndex;
        playerOutgoingPortalIndex = cloneMove.playerOutgoingPortalIndex;
        variant = cloneMove.variant;
        nextMove = cloneMove.nextMove;
      }
      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { state, variant, totalMoveCount, totalPushCount, playerIncomingPortalIndex, playerOutgoingPortalIndex, portalBoxInputCount = string.Join(", ", portalBoxInputCount), portalBoxOuputCount = string.Join(", ", portalBoxOuputCount), nextMove }.ToString();
      }
      /// <summary>
      /// berechnet die Prüfsumme des Zuges (für Doppler-Prüfung)
      /// </summary>
      public ulong Crc
      {
        get
        {
          return Crc64.Start.Crc64Update(state).Crc64Update(playerIncomingPortalIndex).Crc64Update(playerOutgoingPortalIndex).Crc64Update(variant);
        }
      }
    }

    public void Step1_GenerateProfiles()
    {
      var room = this.room;

      if (room.startVariantCount > 0) throw new NotImplementedException();

      var moves = new List<ProfileMove> { new ProfileMove { state = room.startState, variant = ulong.MaxValue, playerIncomingPortalIndex = uint.MaxValue, playerOutgoingPortalIndex = uint.MaxValue, portalBoxInputCount = new uint[room.incomingPortals.Length], portalBoxOuputCount = new uint[room.outgoingPortals.Length] } };

      while (moves.Count > 0)
      {
        var move = moves[moves.Count - 1];
        if (move == null)
        {
          moves.RemoveAt(moves.Count - 1);
          if (moves.Count > 0) moves[moves.Count - 1] = moves[moves.Count - 1].nextMove;
          continue;
        }

        if (move.state == 0) // wurde ein Ende erreicht?
        {
          string path = move.totalMoveCount + "-" + move.totalPushCount;
          for (int i = 1; i < moves.Count; i++)
          {
            if (moves[i].playerIncomingPortalIndex < uint.MaxValue) path += ",i" + moves[i].playerIncomingPortalIndex;
            for (int p = 0; p < moves[i].portalBoxInputCount.Length; p++)
            {
              if (moves[i].portalBoxInputCount[p] > moves[i - 1].portalBoxInputCount[p]) path += ",bi" + p;
            }
            for (int p = 0; p < moves[i].portalBoxOuputCount.Length; p++)
            {
              if (moves[i].portalBoxOuputCount[p] > moves[i - 1].portalBoxOuputCount[p]) path += ",bo" + p;
            }
            if (moves[i].playerOutgoingPortalIndex < uint.MaxValue) path += ",o" + moves[i].playerOutgoingPortalIndex;
          }

          int stop = 0;
        }

        ProfileMove nextMove = null;

        if (move.playerOutgoingPortalIndex < uint.MaxValue || move.variant == ulong.MaxValue)
        {
          for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
          {
            var portal = room.incomingPortals[iPortalIndex];

            // --- auf eingehende Kisten prüfen ---
            var boxState = portal.stateBoxSwap.Get(move.state);
            if (boxState != move.state) // Variante mit reinschiebbarer Kiste vorhanden?
            {
              nextMove = new ProfileMove(move) { nextMove = nextMove, state = boxState, variant = ulong.MaxValue, playerIncomingPortalIndex = uint.MaxValue, playerOutgoingPortalIndex = uint.MaxValue };
              nextMove.portalBoxInputCount[iPortalIndex]++;

              ulong crc = nextMove.Crc;
              if (moves.Count(m => m.Crc == crc) >= 2) nextMove = nextMove.nextMove;
            }

            // --- Varianten prüfen ---
            foreach (ulong variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(move.state).AsEnumerable())
            {
              var variantData = room.variantList.GetData(variant);

              nextMove = new ProfileMove(move) { nextMove = nextMove, state = variantData.newState, variant = variant, playerIncomingPortalIndex = iPortalIndex, playerOutgoingPortalIndex = variantData.oPortalIndexPlayer };
              nextMove.totalMoveCount += variantData.moves;
              nextMove.totalPushCount += variantData.pushes;
              foreach (uint portalBoxOutput in variantData.oPortalIndexBoxes)
              {
                nextMove.portalBoxOuputCount[portalBoxOutput]++;
              }

              ulong crc = nextMove.Crc;
              if (moves.Count(m => m.Crc == crc) >= 2) nextMove = nextMove.nextMove;
            }
          }
        }

        if (nextMove != null)
        {
          moves.Add(nextMove);
        }
        else
        {
          moves[moves.Count - 1] = move.nextMove;
        }
      }
    }

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Destruktor
    /// </summary>
    ~RoomProfileFilter()
    {
      Dispose();
    }
    #endregion
  }
}
