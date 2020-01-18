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
    /// <param name="field">Spielfeld, welches verwendet werden soll</param>
    /// <param name="fieldPosis">Positionen der Spielfelder, welche dem Raum zugeordnet werden</param>
    /// <param name="incomingPortals">eingehende Portale, welche zum eigenen Raum gehören</param>
    /// <param name="outgoingPortals">ausgehende Portale, welche zu anderen Räumen gehören</param>
    public Room(ISokoField field, int[] fieldPosis, RoomPortal[] incomingPortals, RoomPortal[] outgoingPortals)
    {
      #region # // --- Parameter prüfen ---
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

      switch (field.GetField(pos))
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

        default: throw new NotSupportedException("char: " + field.GetField(pos));
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
      foreach (var portal in incomingPortals)
      {
        portal.stateBoxSwap = new StateBoxSwapNormal(stateList);

        var st1 = stateList.FirstOrDefault(st => st.Value.Length == 0); // Zustand ohne Kiste suchen
        var st2 = stateList.FirstOrDefault(st => st.Value.Length == 1); // Zustand mit Kiste suchen

        if (st1.Value != null && st2.Value != null // möglichen Zustandswechsel mit neuer Kiste gefunden?
          && !field.CheckCorner(portal.fromPos) // Kiste steht vorher nicht in einer Ecke?
          && !field.IsWall(portal.fromPos + portal.fromPos - portal.toPos) // Spieler stand vorher nicht in der Wand?
          )
        {
          if (singleBoxScan == null || singleBoxScan.Any(x => x.Key == portal.fromPos && x.Value == portal.toPos)) // gültige Kisten-Varianten erkannt?
          {
            portal.stateBoxSwap.Add(st1.Key, st2.Key);
          }
        }
      }
      #endregion

      #region # // --- Portal-Varianten hinzufügen ---
      for (uint iPortal = 0; iPortal < incomingPortals.Length; iPortal++)
      {
        var portal = incomingPortals[iPortal];
        portal.variantStateDict = new VariantStateDictNormal(stateList, variantList); // Inhalstverzeichnis initialisieren

        // ausgehendes Portal suchen (für die gleichzeitig rausgeschobene Kiste, wenn der Raum auf der anderen Seite betreten wird)
        int boxPortal = -1;
        for (int bPortal = 0; bPortal < outgoingPortals.Length; bPortal++)
        {
          if (outgoingPortals[bPortal].toPos - outgoingPortals[bPortal].fromPos == portal.toPos - portal.fromPos)
          {
            if (field.CheckCorner(outgoingPortals[bPortal].toPos) && !field.IsGoal(outgoingPortals[bPortal].toPos)) continue; // Kiste würde in eine Ecke geschoben werden

            if (singleBoxScan != null && singleBoxScan.All(x => x.Key != pos || x.Value != outgoingPortals[bPortal].toPos)) continue; // ungültige Kisten-Varianten erkannt?

            Debug.Assert(boxPortal == -1);
            boxPortal = bPortal;
          }
        }

        switch (field.GetField(pos))
        {
          case '@': // Spieler auf einem leeren Feld
          case ' ': // leeres Feld
          case '$': // Feld mit Kiste
          {
            for (uint oPortal = 0; oPortal < outgoingPortals.Length; oPortal++)
            {
              if (iPortal != oPortal) // nur Durchlaufen aber nicht zum gleichen Portal zurück
              {
                portal.variantStateDict.Add(0, variantList.Add(0, 1, 0, new uint[0], oPortal, 0, outgoingPortals[oPortal].dirChar.ToString()));
              }

              if (field.CheckCorner(pos)) continue; // Varianten mit rauschiebender Kiste nicht möglich

              if (boxPortal == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?

              int checkPos = outgoingPortals[oPortal].toPos + outgoingPortals[oPortal].toPos - outgoingPortals[oPortal].fromPos;
              if (boxPortal == oPortal && field.CheckCorner(checkPos) && !field.IsGoal(checkPos)) continue; // Kiste würde noch weiter in eine Ecke geschoben werden

              portal.variantStateDict.Add(1, variantList.Add(1, 1, 1, new[] { (uint)boxPortal }, oPortal, 0, outgoingPortals[oPortal].dirChar.ToString()));
            }

            if (boxPortal >= 0 && field.IsGoal(outgoingPortals[boxPortal].toPos))
            {
              // End-Variante hinzufügen (Spieler verbleibt im Raum)
              portal.variantStateDict.Add(1, variantList.Add(1, 0, 1, new[] { (uint)boxPortal }, uint.MaxValue, 0, ""));
            }

          } break;

          case '+': // Spieler auf einem Zielfeld
          case '.': // leeres Zielfeld
          case '*': // Kiste auf einem Zielfeld 
          {
            for (uint oPortal = 0; oPortal < outgoingPortals.Length; oPortal++)
            {
              if (iPortal != oPortal) // nur Durchlaufen aber nicht zum gleichen Portal zurück
              {
                portal.variantStateDict.Add(1, variantList.Add(1, 1, 0, new uint[0], oPortal, 1, outgoingPortals[oPortal].dirChar.ToString()));
              }

              if (field.CheckCorner(pos)) continue; // Varianten mit rauschiebender Kiste nicht möglich

              if (boxPortal == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?

              int checkPos = outgoingPortals[oPortal].toPos + outgoingPortals[oPortal].toPos - outgoingPortals[oPortal].fromPos;
              if (boxPortal == oPortal && field.CheckCorner(checkPos) && !field.IsGoal(checkPos)) continue; // Kiste würde noch weiter in eine Ecke geschoben werden

              portal.variantStateDict.Add(0, variantList.Add(0, 1, 1, new[] { (uint)boxPortal }, oPortal, 1, outgoingPortals[oPortal].dirChar.ToString()));
            }
          } break;

          default: throw new NotSupportedException("char: " + field.GetField(pos));
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
  }
}
