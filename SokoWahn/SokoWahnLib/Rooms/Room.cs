#region # using *.*
using System;
using System.Diagnostics;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Raum (Teil von RoomNetwork)
  /// </summary>
  public class Room : IDisposable
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
    public readonly StateList stateList;
    /// <summary>
    /// Liste mit allen Varianten, welche innerhalb des Raumes durchgeführt werden können
    /// </summary>
    public readonly VariantList variantList;
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

      stateList = new StateListNormal(fieldPosis, fieldPosis.Where(field.IsGoal).ToArray());

      variantList = new VariantListNormal((uint)incomingPortals.Length);
    }
    #endregion

    #region # // --- States ---
    /// <summary>
    /// erstellt die ersten Zustände
    /// </summary>
    public void InitStates()
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(stateList.Count == 0);
      Debug.Assert(variantList.Count == 0);

      int pos = fieldPosis.First();

      switch (field.GetField(pos))
      {
        case '@': // Spieler auf einem leeren Feld
        case ' ': // leeres Feld
        {
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          if (!field.CheckCorner(pos)) stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen (sofern diese nicht in einer Ecke steht)
        } break;

        case '+': // Spieler auf einem Zielfeld
        case '.': // Zielfeld
        {
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
          stateList.Add(new int[0]); // Zwischen-Zustand: leeres Zielfeld
        } break;

        case '$': // Feld mit Kiste
        {
          if (field.CheckCorner(pos)) throw new SokoFieldException("found invalid Box on " + pos % field.Width + ", " + pos / field.Width);
          stateList.Add(new int[0]); // End-Zustand: leeres Feld
          stateList.Add(fieldPosis); // Zustand mit Kiste hinzufügen
        } break;

        case '*': // Kiste auf einem Zielfeld
        {
          stateList.Add(fieldPosis); // End-Zustand: Kiste auf Zielfeld
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
    public void InitVariants()
    {
      Debug.Assert(fieldPosis.Length == 1);
      Debug.Assert(stateList.Count > 0);
      Debug.Assert(variantList.Count == 0);

      int pos = fieldPosis[0];
      Debug.Assert(field.ValidPos(pos));

      #region # // --- Portale und deren Richtungen ermitteln ---
      int portalL = -1;
      int portalR = -2;
      int portalU = -3;
      int portalD = -4;

      for (int p = 0; p < incomingPortals.Length; p++)
      {
        int posFrom = incomingPortals[p].fromPos;
        if (posFrom == pos - 1) portalL = p;
        if (posFrom == pos + 1) portalR = p;
        if (posFrom == pos - field.Width) portalU = p;
        if (posFrom == pos + field.Width) portalD = p;
      }

      Debug.Assert(portalL >= 0 || portalR >= 0 || portalU >= 0 || portalD >= 0);
      Debug.Assert(portalL != portalR && portalL != portalU && portalL != portalD && portalR != portalU && portalR != portalD && portalU != portalD);
      Debug.Assert((portalL >= 0 ? 1 : 0) + (portalR >= 0 ? 1 : 0) + (portalU >= 0 ? 1 : 0) + (portalD >= 0 ? 1 : 0) == incomingPortals.Length);
      #endregion

      #region # // --- Start-Varianten hinzufügen ---
      if (field.IsPlayer(pos))
      {
        ulong oldState = field.IsGoal(pos) ? 1UL : 0UL;
        ulong newState = field.IsGoal(pos) ? 0UL : 1UL;

        if (portalL >= 0) { variantList.Add(oldState, 1, 0, new uint[0], (uint)portalL, newState, "l"); startVariantCount++; }
        if (portalR >= 0) { variantList.Add(oldState, 1, 0, new uint[0], (uint)portalR, newState, "r"); startVariantCount++; }
        if (portalU >= 0) { variantList.Add(oldState, 1, 0, new uint[0], (uint)portalU, newState, "u"); startVariantCount++; }
        if (portalD >= 0) { variantList.Add(oldState, 1, 0, new uint[0], (uint)portalD, newState, "d"); startVariantCount++; }

        Debug.Assert(variantList.Count == startVariantCount);
      }

      var portalDirections = Enumerable.Range(0, incomingPortals.Length).Select(portalIndex =>
      {
        if (portalIndex == portalL) return "l";
        if (portalIndex == portalR) return "r";
        if (portalIndex == portalU) return "u";
        if (portalIndex == portalD) return "d";
        throw new Exception("portal not found?");
      }).ToArray();
      #endregion

      #region # // --- Portal-Kisten-Zustandsänderungen hinzufügen ---
      foreach (var incomingPortal in incomingPortals)
      {
        incomingPortal.stateBoxSwap = new StateBoxSwapNormal(stateList);

        var st1 = stateList.FirstOrDefault(st => st.Value.Length == 0); // Zustand ohne Kiste suchen
        var st2 = stateList.FirstOrDefault(st => st.Value.Length == 1); // Zustand mit Kiste suchen

        if (st1.Value != null && st2.Value != null) // möglichen Zustandswechsel mit neuer Kiste gefunden?
        {
          incomingPortal.stateBoxSwap.Add(st1.Key, st2.Key);
        }
      }
      #endregion

      #region # // --- Portal-Varianten hinzufügen ---
      for (uint iPortal = 0; iPortal < incomingPortals.Length; iPortal++)
      {
        var portal = incomingPortals[iPortal];
        portal.variantStateDict = new VariantStateDictNormal(stateList, variantList); // Inhalstverzeichnis initialisieren

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
                portal.variantStateDict.Add(0, variantList.Add(0, 1, 0, new uint[0], oPortal, 0, portalDirections[oPortal]));
              }

              if (!field.CheckCorner(pos)) // Variante mit Kiste hinzufügen?
              {
                int boxPortal = -1; // ausgehendes Portal suchen (für die rausgeschobene Kiste)
                for (int bPortal = 0; bPortal < outgoingPortals.Length; bPortal++)
                {
                  if (outgoingPortals[bPortal].toPos - outgoingPortals[bPortal].fromPos == portal.toPos - portal.fromPos)
                  {
                    Debug.Assert(boxPortal == -1);
                    boxPortal = bPortal;
                  }
                }
                if (boxPortal == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?

                portal.variantStateDict.Add(1, variantList.Add(1, 1, 1, new[] { (uint)boxPortal }, oPortal, 0, portalDirections[oPortal]));
              }
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
                portal.variantStateDict.Add(1, variantList.Add(1, 1, 0, new uint[0], oPortal, 1, portalDirections[oPortal]));
              }

              if (!field.CheckCorner(pos)) // Variante mit Kiste hinzufügen?
              {
                int boxPortal = -1; // ausgehendes Portal suchen (für die rausgeschobene Kiste)
                for (int bPortal = 0; bPortal < outgoingPortals.Length; bPortal++)
                {
                  if (outgoingPortals[bPortal].toPos - outgoingPortals[bPortal].fromPos == portal.toPos - portal.fromPos)
                  {
                    Debug.Assert(boxPortal == -1);
                    boxPortal = bPortal;
                  }
                }
                if (boxPortal == -1) continue; // Kiste kann doch nicht rausgeschoben werden, da man auf der gegenüberliegenden Seite nicht herankommt?

                portal.variantStateDict.Add(0, variantList.Add(0, 1, 1, new[] { (uint)boxPortal }, oPortal, 1, portalDirections[oPortal]));
              }
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
