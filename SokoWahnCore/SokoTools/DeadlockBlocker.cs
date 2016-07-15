
#region # using *.*

using System.Collections.Generic;
using System.Linq;

#endregion

namespace SokoWahnCore
{
  /// <summary>
  /// Klasse zum scannen von blockierten Stellungen, wo sich keine Kisten aufhalten dürfen
  /// </summary>
  public sealed class DeadlockBlocker
  {
    /// <summary>
    /// merkt sich das aktuelle Spielfeld
    /// </summary>
    SokowahnField field;

    /// <summary>
    /// merkt sich die Wege-Felder, wo sich der Spieler aufhalten darf
    /// </summary>
    public readonly bool[] wayMap;

    /// <summary>
    /// merkt sich die direkt blockierten Spielfelder, wo sich keine Kisten aufhalten dürfen
    /// </summary>
    public readonly bool[] blockerSingle;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="field">Spielfeld, welches betroffen ist</param>
    public DeadlockBlocker(SokowahnField field)
    {
      this.field = new SokowahnField(field);

      wayMap = SokoTools.CreateWayMap(field.fieldData, field.width, field.PlayerPos);

      blockerSingle = wayMap.SelectArray(b => !b);

      ScanBlockerSingle();
    }

    /// <summary>
    /// scannt nach einzelnen Kisten-Positionen, welche nicht mehr lösbar sind
    /// </summary>
    void ScanBlockerSingle()
    {
      var targetFields = field.fieldData.Select((c, i) => new { c, i }).Where(f => wayMap[f.i] && (f.c == '.' || f.c == '*')).Select(f => (ushort)f.i).ToArray();
      var boxFields = field.fieldData.Select((c, i) => new { c, i }).Where(f => wayMap[f.i] && (f.c == '$' || f.c == '*')).Select(f => (ushort)f.i).ToArray();

      var scanner = new SokowahnField(field);
      var fieldData = scanner.fieldData;
      int emptyPlayerPos = scanner.fieldData.ToList().IndexOf(' ');
      int[] playerDirections = { -1, +1, -scanner.width, +scanner.width };
      var todoStates = new Queue<ushort[]>();
      const int StateLen = 2;

      #region # // --- Rückwärts-Suche vorbereiten ---
      foreach (ushort box in targetFields)
      {
        scanner.SetGameState(new[] { (ushort)emptyPlayerPos, box });
        foreach (int playerDir in playerDirections)
        {
          int playerPos = box - playerDir;
          if (fieldData[playerPos] == '#' || fieldData[playerPos] == '$' || fieldData[playerPos] == '*') continue;
          int revPos = playerPos - playerDir;
          if (fieldData[revPos] == '#' || fieldData[revPos] == '$' || fieldData[revPos] == '*') continue;

          scanner.SetPlayerPos(playerPos);
          scanner.SetPlayerTopLeft();

          todoStates.Enqueue(scanner.GetGameState());
        }
      }
      #endregion

      #region # // --- Rückwärts-Suche durchführen ---
      var reverseHash = new HashSet<ulong>(); // alle Stellungen, welche mit einer Kiste rückwärts erreichbar sind
      var reverseBoxPosis = new HashSet<int>(); // alle Positionen, wo eine Kiste stehen konnte
      var nextBuf = new ushort[StateLen * (1) * 4];
      while (todoStates.Count > 0)
      {
        var gameState = todoStates.Dequeue();
        reverseBoxPosis.Add(gameState[1]);
        scanner.SetGameState(gameState);
        scanner.SetPlayerTopLeft();

        ulong crc = scanner.GetGameStateCrc();
        if (reverseHash.Contains(crc)) continue;
        reverseHash.Add(crc);


        int nextLength = scanner.ScanReverseMoves(nextBuf) * StateLen;
        for (int next = 0; next < nextLength; next += StateLen)
        {
          todoStates.Enqueue(new[] { nextBuf[next], nextBuf[next + 1] });
        }
      }
      #endregion

      #region # // --- Vorwärts-Suche vorbereiten ---
      foreach (ushort box in boxFields)
      {
        todoStates.Enqueue(new[] { (ushort)field.PlayerPos, box });
      }
      #endregion

      #region # // --- Vorwärts-Suche durchführen ---
      var forwardHash = new HashSet<ulong>(); // alle Stellungen, welche mit einer Kiste vorwärts erreichbar sind
      var forwardBoxPosis = new HashSet<ushort>(); // alle Positionen, wo eine Kiste stehen konnte
      while (todoStates.Count > 0)
      {
        var gameState = todoStates.Dequeue();
        scanner.SetGameState(gameState);
        scanner.SetPlayerTopLeft();

        ulong crc = scanner.GetGameStateCrc();
        if (forwardHash.Contains(crc)) continue;
        forwardHash.Add(crc);
        if (!reverseHash.Contains(crc)) continue;

        forwardBoxPosis.Add(gameState[1]);

        int nextLength = scanner.ScanMoves(nextBuf) * StateLen;
        for (int next = 0; next < nextLength; next += StateLen)
        {
          todoStates.Enqueue(new[] { nextBuf[next], nextBuf[next + 1] });
        }
      }
      #endregion
    }
  }
}
