using System;

namespace SokoWahnLib
{
  /// <summary>
  /// Typ in welcher Richtung der Spieler sich bewegen kann
  /// </summary>
  public enum MoveType
  {
    None = 0,
    Left = 1,
    LeftPush = Left | 2,
    Right = 4,
    RightPush = Right | 8,
    Up = 16,
    UpPush = Up | 32,
    Down = 64,
    DownPush = Down | 128
  }
}
