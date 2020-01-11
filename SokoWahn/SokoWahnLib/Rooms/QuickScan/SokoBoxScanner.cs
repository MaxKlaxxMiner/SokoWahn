// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global
using System.Collections.Generic;
using System.Linq;

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// Klasse zum berechnen, wohin eine einzelne Kiste geschoben werden kann/darf
  /// </summary>
  public sealed class SokoBoxScanner
  {
    /// <summary>
    /// scannt ein Spielfeld und gibt alle gültigen Varianten einer einzelnen Kiste zurück, Liste: Key: von-Pos, Value: nach-Pos
    /// </summary>
    /// <param name="field">Spielfeld, welches gescannt werden soll</param>
    /// <returns>Liste mit allen gültigen Kisten-Varianten</returns>
    public static List<KeyValuePair<int,int>> ScanSingleBoxPushes(ISokoField field)
    {
      var quick = new SokoFieldQuickScan(field);
      var startState = new ushort[1 + quick.BoxesCount];
      quick.SaveState(startState, 0);
      quick.BoxesCount = 1;

      // --- Ziel-Stellungen mit einer Kiste ermitteln ---
      var data = new ushort[2];
      data[0] = startState[0]; // Spielerposition kopieren
      var scanStates = new Stack<SokowahnState>();
      for (int roomPos = 0; roomPos < quick.RoomCount; roomPos++)
      {
        data[1] = (ushort)roomPos;
        quick.LoadState(data, 0);
        if (quick.GetText().IndexOf('*') < 0) continue; // nur Kisten auf Zielfeldern beachten
        foreach (var v in quick.GetVariantsBlockerGoals()) scanStates.Push(v);
      }

      // --- alle Push-Varianten mit einer einzelnen Kiste rückwärts durchsuchen ---
      var backwardHashes = new HashSet<ulong>(); // Prüfsummen der Stellungen, welche Rückwärts erreicht werden können
      while (scanStates.Count > 0)
      {
        var check = scanStates.Pop();
        if (backwardHashes.Contains(check.crc64)) continue; // schon bekannt?
        backwardHashes.Add(check.crc64);
        quick.LoadState(check);
        foreach (var v in quick.GetVariantsBackward()) scanStates.Push(v);
      }

      // --- Start-Stellungen ermitteln und als Aufgaben hinzufügen ---
      for (int i = 1; i < startState.Length; i++)
      {
        data[1] = startState[i]; // Kisten-Position von der Start-Stellung übernehmen
        quick.LoadState(data, 0);
        scanStates.Push(quick.GetState());
      }

      // --- alle Push-Varianten mit einer einzelnen Kiste vorwärts durchsuchen und alle gültigen Kisten-Varianten merken ---
      var boxMoves = new List<KeyValuePair<int, int>>(); // Liste mit allen gültigen Kisten-Varianten (key: von-Pos, value: nach-Pos)
      var forwardHashes = new HashSet<ulong>();
      while (scanStates.Count > 0)
      {
        var check = scanStates.Pop();
        if (forwardHashes.Contains(check.crc64)) continue; // schon bekannt?
        forwardHashes.Add(check.crc64);
        quick.LoadState(check);
        int boxPosFrom = Enumerable.Range(0, quick.Width * quick.Height).First(quick.IsBox);
        var variants = quick.GetVariants().ToArray();
        foreach (var v in variants)
        {
          if (!backwardHashes.Contains(v.crc64)) continue; // unbekannte Stellung bei der Rückwärts-Suche? -> Ziel kann hier nie erreicht werden
          scanStates.Push(v);
          quick.LoadState(v);

          int boxPosTo = Enumerable.Range(0, quick.Width * quick.Height).First(quick.IsBox);
          if (boxMoves.Any(x => x.Key == boxPosFrom && x.Value == boxPosTo)) continue; // Kisten-Variante schon vorhanden?
          boxMoves.Add(new KeyValuePair<int, int>(boxPosFrom, boxPosTo));
        }
      }

      return boxMoves;
    }
  }
}
