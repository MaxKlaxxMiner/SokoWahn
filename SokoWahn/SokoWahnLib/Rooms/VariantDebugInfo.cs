﻿// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace SokoWahnLib.Rooms
{
  /*
   * --- Speicher-Nutzung ---
   * 
   * byte   outgoingPortal
   * uint   outgoingState
   * uint24 moves
   * uint24 pushes
   * 
   */

  /// <summary>
  /// Struktur zum speichern einer Spielvariante (für Debug-Zwecke)
  /// </summary>
  public class VariantDebugInfo
  {
    /// <summary>
    /// eigendes Portal, welches für den eigehenden Verkehr zuständig ist (null: kein Eingang verzeichnet, z.B. bei der Start-Stellung)
    /// </summary>
    public readonly RoomPortal incomingPortal;
    /// <summary>
    /// gibt an, ob eine Kiste durch das eingehende Portal geschoben wurde (sonst: Spieler)
    /// </summary>
    public readonly bool incomingBox;
    /// <summary>
    /// eigehender Anfangs-Zustand
    /// </summary>
    public readonly uint incomingState;
    /// <summary>
    /// ausgehendes Portal, welches für den ausgehenden Verkehr zuständig ist (null: kein Ausgang verzeichnet, z.B. bei der Ziel-Stellung)
    /// </summary>
    public readonly RoomPortal outgoingPortal;
    /// <summary>
    /// gibt an, ob eine Kiste durch das ausgehende Portal geschoben wurde (sonst: Spieler)
    /// </summary>
    public readonly bool outgoingBox;
    /// <summary>
    /// ausgehender End-Zustand
    /// </summary>
    public readonly uint outgoingState;
    /// <summary>
    /// Anzahl der Laufschritte, welche für den neuen Zustand nötig sind (inkl. Kistenverschiebungen)
    /// </summary>
    public readonly uint moves;
    /// <summary>
    /// Anzahl der Kistenverschiebungen, welche für den neuen Zustand nötig sind
    /// </summary>
    public readonly uint pushes;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="incomingPortal">eigendes Portal, welches für den eigehenden Verkehr zuständig ist (null: kein Eingang verzeichnet, z.B. bei der Start-Stellung)</param>
    /// <param name="incomingBox">gibt an, ob eine Kiste durch das eingehende Portal geschoben wurde (sonst: Spieler)</param>
    /// <param name="incomingState">eigehender Anfangs-Zustand</param>
    /// <param name="outgoingPortal">ausgehendes Portal, welches für den ausgehenden Verkehr zuständig ist (null: kein Ausgang verzeichnet, z.B. bei der Ziel-Stellung)</param>
    /// <param name="outgoingBox">gibt an, ob eine Kiste durch das ausgehende Portal geschoben wurde (sonst: Spieler)</param>
    /// <param name="outgoingState">ausgehender End-Zustand</param>
    /// <param name="moves">Anzahl der Laufschritte, welche für den neuen Zustand nötig sind (inkl. Kistenverschiebungen)</param>
    /// <param name="pushes">Anzahl der Kistenverschiebungen, welche für den neuen Zustand nötig sind</param>
    public VariantDebugInfo(RoomPortal incomingPortal, bool incomingBox, uint incomingState, RoomPortal outgoingPortal, bool outgoingBox, uint outgoingState, uint moves, uint pushes)
    {
      this.incomingPortal = incomingPortal;
      this.incomingBox = incomingBox;
      this.incomingState = incomingState;
      this.outgoingPortal = outgoingPortal;
      this.outgoingBox = outgoingBox;
      this.outgoingState = outgoingState;
      this.moves = moves;
      this.pushes = pushes;
    }
  }
}
