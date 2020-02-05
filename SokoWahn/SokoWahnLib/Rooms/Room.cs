#region # using *.*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Raum (Teil von RoomNetwork)
  /// </summary>
  public sealed class Room : IDisposable
  {
    /// <summary>
    /// Spielfeld, welches verwendet wird
    /// </summary>
    public readonly ISokoField field;
    /// <summary>
    /// Positionen der Spielfelder, welche dem Raum zugeordnet sind
    /// </summary>
    public readonly int[] fieldPosis;
    /// <summary>
    /// Positionen der Zielfelder, welche dem Raum zugeordnet sind 
    /// </summary>
    public readonly int[] goalPosis;
    /// <summary>
    /// Positionen der Kisten am Start, welche sich am Anfang im Raum befinden
    /// </summary>
    public readonly int[] startBoxPosis;
    /// <summary>
    /// eigender Index des Rooms
    /// </summary>
    public readonly uint roomIndex;
    /// <summary>
    /// eingehende Portale, welche zum eigenen Raum gehören
    /// </summary>
    public readonly RoomPortal[] incomingPortals;
    /// <summary>
    /// ausgehende Portale, welche zu anderen Räumen gehören
    /// </summary>
    public readonly RoomPortal[] outgoingPortals;
    /// <summary>
    /// Liste mit allen Zuständen, welche der Raum annehmen kann
    /// </summary>
    public StateList stateList;
    /// <summary>
    /// merkt sich den Anfangs-Zustand des Raumes bei Beginn des Spieles
    /// </summary>
    public ulong startState;
    /// <summary>
    /// Liste mit allen Varianten, welche innerhalb des Raumes durchgeführt werden können
    /// </summary>
    public VariantList variantList;
    /// <summary>
    /// merkt sich die Anzahl der vorhandenen Start-Varianten (nur wenn der Spieler in diesem Raum startet)
    /// </summary>
    public ulong startVariantCount;

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="roomIndex">eigener Room-Index</param>
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    /// <param name="fieldPosis">Positionen der Spielfelder, welche dem Raum zugeordnet werden</param>
    /// <param name="incomingPortals">eingehende Portale, welche zum eigenen Raum gehören</param>
    /// <param name="outgoingPortals">ausgehende Portale, welche zu anderen Räumen gehören</param>
    public Room(uint roomIndex, ISokoField field, int[] fieldPosis, RoomPortal[] incomingPortals, RoomPortal[] outgoingPortals)
    {
      #region # // --- Parameter prüfen und Werte initialisieren ---
      this.roomIndex = roomIndex;

      if (field == null) throw new ArgumentNullException("field");
      this.field = field;

      if (fieldPosis == null || fieldPosis.Length == 0) throw new ArgumentNullException("fieldPosis");
      var walkPosis = field.GetWalkPosis();
      if (fieldPosis.Any(pos => !walkPosis.Contains(pos))) throw new ArgumentOutOfRangeException("fieldPosis");
      this.fieldPosis = fieldPosis;

      if (incomingPortals == null || incomingPortals.Length == 0) throw new ArgumentNullException("incomingPortals");
      this.incomingPortals = incomingPortals;
      if (outgoingPortals == null || outgoingPortals.Length == 0) throw new ArgumentNullException("outgoingPortals");
      this.outgoingPortals = outgoingPortals;
      if (incomingPortals.Length != outgoingPortals.Length) throw new ArgumentException("iPortals.Length != oPortals.Length");
      #endregion

      goalPosis = fieldPosis.Where(field.IsGoal).ToArray();
      startBoxPosis = fieldPosis.Where(field.IsBox).ToArray();

      stateList = new StateListNormal(fieldPosis, goalPosis);

      variantList = new VariantListNormal((uint)incomingPortals.Length);
    }
    #endregion

    #region # // --- States ---
    /// <summary>
    /// erstellt die ersten Zustände
    /// </summary>
    /// <param name="singleBoxScan">Scan-Ergebniss einer einzelnen Kiste (alle erlaubten Varianten)</param>
    public void InitStates(List<KeyValuePair<int, int>> singleBoxScan)
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(stateList.Count == 0);
      Debug.Assert(variantList.Count == 0);

      int pos = fieldPosis.First();
      bool isBoxField = singleBoxScan != null ? singleBoxScan.Any(x => x.Key == pos || x.Value == pos) : field.IsGoal(pos) || !field.CheckCorner(pos); // gibt an, ob theoretisch eine Kiste auf dem Spielfeld sein darf

      switch (field.GetFieldChar(pos))
      {
        case '@': // Spieler auf einem leeren Feld
        case ' ': // leeres Feld
        {
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          startState = 0;
          if (isBoxField) stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen
        } break;

        case '+': // Spieler auf einem Zielfeld
        case '.': // Zielfeld
        {
          Debug.Assert(isBoxField);
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
          stateList.Add(new int[0]); // Zwischen-Zustand: leeres Zielfeld
          startState = 1;
        } break;

        case '$': // Feld mit Kiste
        {
          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
          Debug.Assert(isBoxField);
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen
          startState = 1;
        } break;

        case '*': // Kiste auf einem Zielfeld
        {
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
          startState = 0;
          if (!field.CheckCorner(pos)) stateList.Add(new int[0]); // Zustand ohne Kiste hinzufügen (nur wenn die Kiste herausgeschoben werden kann)
        } break;

        default: throw new NotSupportedException("char: " + field.GetFieldChar(pos));
      }
    }
    #endregion

    #region # // --- Variants ---
    /// <summary>
    /// initialisiert die ersten Varianten
    /// </summary>
    /// <param name="singleBoxScan">Scan-Ergebniss einer einzelnen Kiste (alle erlaubten Varianten)</param>
    public void InitVariants(List<KeyValuePair<int, int>> singleBoxScan)
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(stateList.Count > 0);
      Debug.Assert(variantList.Count == 0);

      int pos = fieldPosis[0];
      Debug.Assert(field.ValidPos(pos));

      #region # // --- Start-Varianten hinzufügen ---
      if (field.IsPlayer(pos))
      {
        ulong state = field.IsGoal(pos) ? 1UL : 0UL;
        for (int p = 0; p < incomingPortals.Length; p++)
        {
          variantList.Add(state, 1, 0, new uint[0], (uint)p, state, outgoingPortals[p].dirChar.ToString());
          startVariantCount++;
        }
      }

      Debug.Assert(variantList.Count == startVariantCount);
      #endregion

      #region # // --- Portal-Kisten-Zustandsänderungen hinzufügen ---
      foreach (var iPortal in incomingPortals)
      {
        iPortal.stateBoxSwap = new StateBoxSwapNormal(stateList);

        var st1 = stateList.FirstOrDefault(st => st.Value.Length == 0); // Zustand ohne Kiste suchen
        var st2 = stateList.FirstOrDefault(st => st.Value.Length == 1); // Zustand mit Kiste suchen

        if (st1.Value != null && st2.Value != null // möglichen Zustandswechsel mit neuer Kiste gefunden?
            && !field.CheckCorner(iPortal.fromPos) // Kiste steht vorher nicht in einer Ecke?
            && !field.IsWall(iPortal.fromPos + iPortal.fromPos - iPortal.toPos) // Spieler stand vorher nicht in der Wand?
        )
        {
          if (singleBoxScan == null || singleBoxScan.Any(x => x.Key == iPortal.fromPos && x.Value == iPortal.toPos)) // gültige Kisten-Varianten erkannt?
          {
            iPortal.stateBoxSwap.Add(st1.Key, st2.Key);
          }
        }
      }
      #endregion

      #region # // --- Portal-Varianten hinzufügen ---
      for (uint iPortalIndex = 0; iPortalIndex < incomingPortals.Length; iPortalIndex++)
      {
        var iPortal = incomingPortals[iPortalIndex];
        iPortal.variantStateDict = new VariantStateDictNormal(stateList, variantList); // Inhalstverzeichnis initialisieren

        // ausgehendes Portal suchen (für die gleichzeitig rausgeschobene Kiste, wenn der Raum auf der anderen Seite betreten wird)
        int boxPortalIndex = -1;
        for (int o = 0; o < outgoingPortals.Length; o++)
        {
          if (outgoingPortals[o].toPos - outgoingPortals[o].fromPos == iPortal.toPos - iPortal.fromPos)
          {
            if (field.CheckCorner(outgoingPortals[o].toPos) && !field.IsGoal(outgoingPortals[o].toPos)) continue; // Kiste würde in eine Ecke geschoben werden

            if (singleBoxScan != null && singleBoxScan.All(x => x.Key != pos || x.Value != outgoingPortals[o].toPos)) continue; // ungültige Kisten-Varianten erkannt?

            Debug.Assert(boxPortalIndex == -1);
            boxPortalIndex = o;
          }
        }

        switch (field.GetFieldChar(pos))
        {
          case '@': // Spieler auf einem leeren Feld
          case ' ': // leeres Feld
          case '$': // Feld mit Kiste
          {
            // --- durchlaufen ---
            for (uint oPortalIndex = 0; oPortalIndex < outgoingPortals.Length; oPortalIndex++)
            {
              if (iPortalIndex == oPortalIndex) continue; // nicht zum gleichen Portal zurück laufen

              iPortal.variantStateDict.Add(0, variantList.Add(0, 1, 0, new uint[0], oPortalIndex, 0, outgoingPortals[oPortalIndex].dirChar.ToString()));
            }

            // --- Kiste schieben ---
            for (uint oPortalIndex = 0; oPortalIndex < outgoingPortals.Length; oPortalIndex++)
            {
              if (field.CheckCorner(pos)) continue; // Varianten mit rauschiebender Kiste nicht möglich

              if (boxPortalIndex == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?

              int checkPos = outgoingPortals[oPortalIndex].toPos + outgoingPortals[oPortalIndex].toPos - outgoingPortals[oPortalIndex].fromPos;
              if (boxPortalIndex == oPortalIndex && field.CheckCorner(checkPos) && !field.IsGoal(checkPos)) continue; // Kiste würde noch weiter in eine Ecke geschoben werden

              iPortal.variantStateDict.Add(1, variantList.Add(1, 1, 1, new[] { (uint)boxPortalIndex }, oPortalIndex, 0, outgoingPortals[oPortalIndex].dirChar.ToString()));
            }

            // --- Kiste schieben und gesamtes Spiel abschließen ---
            if (boxPortalIndex >= 0 && field.IsGoal(outgoingPortals[boxPortalIndex].toPos))
            {
              // End-Variante hinzufügen (Spieler hat als letztes eine Kiste geschoben und verbleibt im Raum)
              iPortal.variantStateDict.Add(1, variantList.Add(1, 0, 1, new[] { (uint)boxPortalIndex }, uint.MaxValue, 0, ""));
            }

          } break;

          case '+': // Spieler auf einem Zielfeld
          case '.': // leeres Zielfeld
          case '*': // Kiste auf einem Zielfeld 
          {
            // --- Kiste schieben ---
            for (uint oPortalIndex = 0; oPortalIndex < outgoingPortals.Length; oPortalIndex++)
            {
              if (field.CheckCorner(pos)) continue; // Varianten mit rauschiebender Kiste nicht möglich

              if (boxPortalIndex == -1) continue; // Kiste kann nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt

              int checkPos = outgoingPortals[oPortalIndex].toPos + outgoingPortals[oPortalIndex].toPos - outgoingPortals[oPortalIndex].fromPos;
              if (boxPortalIndex == oPortalIndex && field.CheckCorner(checkPos) && !field.IsGoal(checkPos)) continue; // Kiste würde noch weiter in einer Ecke laden

              iPortal.variantStateDict.Add(0, variantList.Add(0, 1, 1, new[] { (uint)boxPortalIndex }, oPortalIndex, 1, outgoingPortals[oPortalIndex].dirChar.ToString()));
            }

            // --- durchlaufen ---
            for (uint oPortalIndex = 0; oPortalIndex < outgoingPortals.Length; oPortalIndex++)
            {
              if (iPortalIndex == oPortalIndex) continue; // nicht zum gleichen Portal zurück laufen

              iPortal.variantStateDict.Add(1, variantList.Add(1, 1, 0, new uint[0], oPortalIndex, 1, outgoingPortals[oPortalIndex].dirChar.ToString()));
            }
          } break;

          default: throw new NotSupportedException("char: " + field.GetFieldChar(pos));
        }
      }
      #endregion
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle verwendeten Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      // --- nur die eingehenden Portale auflösen, welche zum eigenen Raum gehören ---
      if (incomingPortals != null)
      {
        for (int p = 0; p < incomingPortals.Length; p++)
        {
          if (incomingPortals[p] != null) incomingPortals[p].Dispose();
          incomingPortals[p] = null;
        }
      }

      if (stateList != null)
      {
        stateList.Dispose();
        stateList = null;
      }

      if (variantList != null)
      {
        variantList.Dispose();
        variantList = null;
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

    #region # // --- Compare ---
    /// <summary>
    /// Hash-Code für einfachen Vergleich
    /// </summary>
    /// <returns>eindeutiger Hash-Code</returns>
    public override int GetHashCode()
    {
      return fieldPosis[0];
    }
    /// <summary>
    /// direkter Vergleich der Objekte
    /// </summary>
    /// <param name="obj">Objekt, welches verglichen werden soll</param>
    /// <returns>true, wenn Beide identisch sind</returns>
    public override bool Equals(object obj)
    {
      return ReferenceEquals(obj, this);
    }
    #endregion

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return "Posis: " + string.Join(", ", fieldPosis);
    }
  }
}
