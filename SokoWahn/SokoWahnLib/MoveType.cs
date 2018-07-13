using System;

namespace SokoWahnLib
{
  /// <summary>
  /// Typ in welcher Richtung der Spieler sich bewegen kann
  /// </summary>
  public enum MoveType
  {
    /// <summary>
    /// keine Bewegung möglich
    /// </summary>
    None = 0,
    /// <summary>
    /// nach links laufen
    /// </summary>
    Left = 1,
    /// <summary>
    /// nach links laufen und eine Kiste schieben
    /// </summary>
    LeftPush = Left | 2,
    /// <summary>
    /// nach rechts laufen
    /// </summary>
    Right = 4,
    /// <summary>
    /// nach rechts laufen und eine Kiste schieben
    /// </summary>
    RightPush = Right | 8,
    /// <summary>
    /// nach oben laufen
    /// </summary>
    Up = 16,
    /// <summary>
    /// nach oben laufen und eine Kiste schieben
    /// </summary>
    UpPush = Up | 32,
    /// <summary>
    /// nach unten laufen
    /// </summary>
    Down = 64,
    /// <summary>
    /// nach unten laufen und eine Kiste schieben
    /// </summary>
    DownPush = Down | 128
  }
}
