#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace Sokosolver.SokowahnTools
{
  /// <summary>
  /// Funktionen, welcher ein SokowahnBlocker bereit stellen muss
  /// </summary>
  public interface ISokowahnBlocker
  {
    /// <summary>
    /// prüft, ob eine bestimmte Stellung erlaubt ist
    /// </summary>
    /// <param name="spielerRaumPos">Spielerposition</param>
    /// <param name="raumZuKiste">zu prüfende Kistenpositionen direkt im Raum</param>
    /// <returns>true, wenn die Stellung erlaubt ist oder false, wenn anhand der Blocker eine verbotene Stellung erkannt wurde</returns>
    bool CheckErlaubt(int spielerRaumPos, int[] raumZuKiste);

    /// <summary>
    /// gibt an, ob der Blocker gerade erstellt wird
    /// </summary>
    bool ErstellungsModus { get; }

    /// <summary>
    /// berechnet die nächsten Blocker
    /// </summary>
    /// <param name="limit">maximale Anzahl der Berechnungen, oder 0, wenn die Berechnung beendet werden soll</param>
    /// <returns>true, wenn noch weitere Berechnungen anstehen</returns>
    bool Next(int limit);

    /// <summary>
    /// bricht die Berechnung vorzeitig ab (bereits fertig berechnete Blocker jedoch weiter genutzt werden)
    /// </summary>
    void Abbruch();
  }
}
