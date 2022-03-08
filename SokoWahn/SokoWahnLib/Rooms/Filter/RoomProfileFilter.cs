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
        nextMove = cloneMove.nextMove;
      }
      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenkette zurück
      /// </summary>
      /// <returns>lesbare Zeichenkette</returns>
      public override string ToString()
      {
        return new { state, totalMoveCount, totalPushCount, nextMove }.ToString();
      }
    }

    public void Step1_GenerateProfiles()
    {
      var room = this.room;

      if (room.startVariantCount > 0) throw new NotImplementedException();

      var moves = new List<ProfileMove> { new ProfileMove { state = room.startState, portalBoxInputCount = new uint[room.incomingPortals.Length], portalBoxOuputCount = new uint[room.outgoingPortals.Length] } };

      while (moves.Count > 0)
      {
        var move = moves[moves.Count - 1];
        if (move == null)
        {
          moves.RemoveAt(moves.Count - 1);
          if (moves.Count > 0) moves[moves.Count - 1] = moves[moves.Count - 1].nextMove;
          continue;
        }
        ProfileMove nextMove = null;

        for (uint iPortalIndex = 0; iPortalIndex < room.incomingPortals.Length; iPortalIndex++)
        {
          var portal = room.incomingPortals[iPortalIndex];

          var boxState = portal.stateBoxSwap.Get(move.state);
          if (boxState != move.state) // Variante mit reinschiebbarer Kiste vorhanden?
          {
            nextMove = new ProfileMove(move) { nextMove = nextMove, state = boxState };
            nextMove.totalMoveCount++;
            nextMove.totalPushCount++;
            nextMove.portalBoxInputCount[iPortalIndex]++;
          }

          foreach (ulong variant in room.incomingPortals[iPortalIndex].variantStateDict.GetVariantSpan(move.state).AsEnumerable())
          {
            var variantData = room.variantList.GetData(variant);

            nextMove = new ProfileMove(move) { nextMove = nextMove, state = variantData.newState };
            nextMove.totalMoveCount += variantData.moves;
            nextMove.totalPushCount += variantData.pushes;
            foreach (uint portalBoxOutput in variantData.oPortalIndexBoxes)
            {
              nextMove.portalBoxOuputCount[portalBoxOutput]++;
            }
          }
        }

        if (nextMove != null)
        {
          // todo: dopplercheck
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
