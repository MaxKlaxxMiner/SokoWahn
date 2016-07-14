#region # using *.*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SokoWahnCore;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable TailRecursiveCall
// ReSharper disable ConvertToLambdaExpressionWhenPossible

#endregion

namespace SokoWahn
{
  static class Program
  {
    const string PathData = "../../../Data/";
    const string PathTest = PathData + "Test/";

    #region # static int ScanTopLeftPos(SokowahnField field) // sucht die oberste und am weitestende linke Position, welche vom Spieler noch erreichbar ist
    /// <summary>
    /// sucht die oberste und am weitestende linke Position, welche vom Spieler noch erreichbar ist
    /// </summary>
    /// <param name="field">Spielfeld, welches gescannt werden soll</param>
    /// <returns>erreichbare Spielerposition</returns>
    static int ScanTopLeftPos(SokowahnField field)
    {
      var data = field.fieldData;
      int bestPos = int.MaxValue;
      int width = field.width;

      var scanned = new bool[data.Length];

      var next = new Stack<int>();
      next.Push(field.PlayerPos);

      while (next.Count > 0)
      {
        int checkPos = next.Pop();
        if (checkPos < bestPos) bestPos = checkPos;

        scanned[checkPos] = true;

        checkPos--;
        if (!scanned[checkPos] && (data[checkPos] == ' ' || data[checkPos] == '.')) next.Push(checkPos);

        checkPos += 2;
        if (!scanned[checkPos] && (data[checkPos] == ' ' || data[checkPos] == '.')) next.Push(checkPos);

        checkPos -= width + 1;
        if (!scanned[checkPos] && (data[checkPos] == ' ' || data[checkPos] == '.')) next.Push(checkPos);

        checkPos += width * 2;
        if (!scanned[checkPos] && (data[checkPos] == ' ' || data[checkPos] == '.')) next.Push(checkPos);
      }

      return bestPos;
    }
    #endregion

    #region # static void MiniSolver(SokowahnField field) // einfaches Tool zum finden irgendeiner Lösung eines Spielfeldes
    /// <summary>
    /// einfaches Tool zum finden irgendeiner Lösung eines Spielfeldes
    /// </summary>
    /// <param name="field">Spielfeld, welches gescannt werden soll</param>
    static void MiniSolver(SokowahnField field)
    {
      var scanner = new SokowahnField(field);

      int stateLen = scanner.posis.Length;
      var todoBuf = new ushort[16777216 * stateLen];
      int todoPos = 0;
      int todoLen = 0;
      foreach (var p in scanner.posis) todoBuf[todoLen++] = p;

      var nextBuf = new ushort[stateLen * (stateLen - 1) * 4];

      var hashCrcs = new HashSet<ulong>();

      while (todoPos < todoLen)
      {
        scanner.SetGameState(todoBuf, todoPos);
        todoPos += stateLen;

        if (todoLen - todoPos == 0 || (hashCrcs.Count & 0xfff) == 0)
        {
          Console.Clear();
          Console.WriteLine(scanner);
          Console.WriteLine();
          Console.WriteLine("Todo: " + ((todoLen - todoPos) / stateLen).ToString("N0") + " (" + (200.0 / todoBuf.Length * (todoLen - todoPos)).ToString("N1") + " %)");
          Console.WriteLine("Hash: " + hashCrcs.Count.ToString("N0") + " (" + (100.0 / 48000000 * hashCrcs.Count).ToString("N1") + " %)");
        }

        scanner.SetPlayerPos(ScanTopLeftPos(scanner));

        var crc = scanner.GetGameStateCrc();
        if (hashCrcs.Contains(crc)) continue;
        hashCrcs.Add(crc);

        int nextCount = scanner.ScanMoves(nextBuf);
        for (int i = 0; i < nextCount * stateLen; i++) todoBuf[todoLen++] = nextBuf[i];

        if (todoPos * 2 > todoBuf.Length)
        {
          for (int i = todoPos; i < todoLen; i++) todoBuf[i - todoPos] = todoBuf[i];
          todoLen -= todoPos;
          todoPos = 0;
        }
      }
      Console.ReadLine();
    }
    #endregion

    #region # static List<Dictionary<ulong, ushort>> MiniSolverHashBuilder(SokowahnField field, int minBoxes = 1, int maxBoxes = int.MaxValue) // analysiert alle möglichen Kistenstellungen mit einfachen (langsamen) Methoden
    /// <summary>
    /// analysiert alle möglichen Kistenstellungen mit einfachen (langsamen) Methoden
    /// </summary>
    /// <param name="field">Feld, welches durchsucht werden soll</param>
    /// <param name="minBoxes">minimale Anzahl der Kisten, welche berechnet werden sollen</param>
    /// <param name="maxBoxes">maximale Anzahl der Kisten, welche berechnet werden sollen</param>
    static List<Dictionary<ulong, ushort>> MiniSolverHashBuilder(SokowahnField field, int minBoxes = 1, int maxBoxes = int.MaxValue)
    {
      var scanner = new SokowahnField(field);

      var targetFields = scanner.fieldData.Select((c, i) => new { c, i }).Where(f => f.c == '.' || f.c == '*').Select(f => (ushort)f.i).ToArray();
      if (targetFields.Length < maxBoxes) maxBoxes = targetFields.Length;
      if (minBoxes < 1 || minBoxes > maxBoxes) throw new ArgumentException("minBoxes");

      var hashResults = new List<Dictionary<ulong, ushort>>();

      for (int boxesCount = minBoxes; boxesCount <= maxBoxes; boxesCount++)
      {

        // --- Variablen initialisieren ---
        var boxes = new ushort[boxesCount];
        int stateLen = 1 + boxes.Length;
        var todoBuf = new ushort[16777216 * 2 / (stateLen + 1) * (stateLen + 1)];
        int todoPos = 0, todoLen = 0;
        var stopWatch = Stopwatch.StartNew();

        // --- Startaufgaben scannen und setzen ---
        {
          int emptyPlayerPos = scanner.fieldData.ToList().IndexOf(' ');
          int[] playerDirections = { -1, +1, -scanner.width, +scanner.width };
          var checkDuplicates = new HashSet<ulong>();

          foreach (var boxesVariant in SokoTools.FieldBoxesVariants(targetFields.Length, boxesCount, false).Select(v => v.SelectArray(f => targetFields[f])))
          {
            scanner.SetPlayerPos(emptyPlayerPos);
            scanner.SetBoxes(boxesVariant);
            var fieldData = scanner.fieldData;
            foreach (ushort box in boxesVariant)
            {
              foreach (int playerDir in playerDirections)
              {
                int playerPos = box - playerDir;
                if (fieldData[playerPos] == '#' || fieldData[playerPos] == '$' || fieldData[playerPos] == '*') continue;
                int revPos = playerPos - playerDir;
                if (fieldData[revPos] == '#' || fieldData[revPos] == '$' || fieldData[revPos] == '*') continue;

                scanner.SetPlayerPos(playerPos);
                scanner.SetPlayerPos(ScanTopLeftPos(scanner));

                ulong crc = scanner.GetGameStateCrc();
                if (checkDuplicates.Contains(crc)) continue;
                checkDuplicates.Add(crc);

                todoBuf[todoLen++] = 0;
                todoLen += scanner.GetGameState(todoBuf, todoLen);
              }
            }
          }
        }

        Console.WriteLine(field.ToString());
        Console.WriteLine();

        // --- Aufgaben weiter rückwärts gerichtet abarbeiten ---
        {
          var hash = new Dictionary<ulong, ushort>();
          var nextBuf = new ushort[stateLen * boxesCount * 4];
          int limitTickCount = Environment.TickCount + 1000;

          while (todoPos < todoLen)
          {
            ushort depth = todoBuf[todoPos++];
            scanner.SetGameState(todoBuf, todoPos); todoPos += stateLen;
            scanner.SetPlayerPos(ScanTopLeftPos(scanner));

            ulong crc = scanner.GetGameStateCrc();
            if (hash.ContainsKey(crc)) continue;
            hash.Add(crc, depth);

            if ((hash.Count & 0xff) == 0 && Environment.TickCount > limitTickCount)
            {
              limitTickCount = Environment.TickCount + 1000;
              Console.WriteLine("[" + boxesCount + "] (" + depth + ") " + ((todoLen - todoPos) / (stateLen + 1)).ToString("N0") + " / " + hash.Count.ToString("N0"));
            }

            depth++;
            int nextLength = scanner.ScanReverseMoves(nextBuf) * stateLen;
            for (int next = 0; next < nextLength; next += stateLen)
            {
              todoBuf[todoLen++] = depth;
              for (int i = 0; i < stateLen; i++) todoBuf[todoLen++] = nextBuf[next + i];
            }
            if (todoBuf.Length - todoLen < nextLength * 2)
            {
              Array.Copy(todoBuf, todoPos, todoBuf, 0, todoLen - todoPos);
              todoLen -= todoPos;
              todoPos = 0;
            }
          }
          stopWatch.Stop();
          Console.WriteLine();
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine("[" + boxesCount + "] ok. Hash: " + hash.Count.ToString("N0") + " (" + stopWatch.ElapsedMilliseconds.ToString("N0") + " ms)");
          Console.ForegroundColor = ConsoleColor.Gray;
          Console.WriteLine();

          hashResults.Add(hash);
        }
      }

      return hashResults;
    }
    #endregion

    #region # static void MiniSolverHashBuilder2(SokowahnField field) // analysiert alle möglichen Kistenstellungen mit spezialkompilierten Hochleistungs-Methoden
    /// <summary>
    /// analysiert alle möglichen Kistenstellungen mit spezialkompilierten Hochleistungs-Methoden
    /// </summary>
    /// <param name="field">Feld, welches durchsucht werden soll</param>
    static void MiniSolverHashBuilder2(SokowahnField field)
    {
      CreateHashBuilder.CreateProject(field, PathTest);
    }
    #endregion

    #region # static HashSet<int> FilterWays(int playerPos, int width, HashSet<int> baseWays, HashSet<int> blocked) // Filtert einen begehbaren Bereich und gibt alle restlichen begehbaren Felder zurück
    /// <summary>
    /// Filtert einen begehbaren Bereich und gibt alle restlichen begehbaren Felder zurück
    /// </summary>
    /// <param name="playerPos">Startposition, wo sich der Spieler befindet</param>
    /// <param name="width">Breite des Spielfeldes</param>
    /// <param name="baseWays">Basis-Wege welche normalerweise begehbar sind</param>
    /// <param name="blocked">einzelne blockierte Felder, welche nicht mehr begehbar sind</param>
    /// <returns>alle Felder, welche nach dem filtern noch begehbar sind</returns>
    static HashSet<ushort> FilterWays(ushort playerPos, int width, HashSet<ushort> baseWays, HashSet<ushort> blocked)
    {
      var filteredWays = new HashSet<ushort>();

      var todo = new Stack<ushort>();
      todo.Push(playerPos);
      while (todo.Count > 0)
      {
        var pos = todo.Pop();
        if (!baseWays.Contains(pos) || blocked.Contains(pos) || filteredWays.Contains(pos)) continue;
        filteredWays.Add(pos);
        todo.Push((ushort)(pos - 1));
        todo.Push((ushort)(pos + 1));
        todo.Push((ushort)(pos - width));
        todo.Push((ushort)(pos + width));
      }

      return filteredWays;
    }
    #endregion

    #region # static List<int> ScanBestTopLeftWay(int playerPos, int width, HashSet<int> baseWays, HashSet<int> blocked) // sucht die am weitesten erreichbare Position links-oben und gibt den vollständigen Weg zurück
    /// <summary>
    /// sucht die am weitesten erreichbare Position links-oben und gibt den vollständigen Weg zurück
    /// </summary>
    /// <param name="playerPos">Startposition des Spielers</param>
    /// <param name="width">Breite des Spielfeldes</param>
    /// <param name="baseWays">Basis-Felder, welche erreichbar sind</param>
    /// <param name="blocked">Blockierte Felder, welche nicht mehr erreichbar sind</param>
    /// <returns>vollständiger Weg</returns>
    static List<ushort> ScanBestTopLeftWay(ushort playerPos, int width, HashSet<ushort> baseWays, HashSet<ushort> blocked)
    {
      var openWays = FilterWays(playerPos, width, baseWays, blocked);
      var bestPos = openWays.OrderBy(x => x).First();

      var searched = new Dictionary<ushort, ushort>();
      ushort depth = 0;
      var search = new List<ushort> { bestPos };
      while (search.Count > 0)
      {
        depth++;
        var next = new List<ushort>();
        foreach (var pos in search)
        {
          if (!openWays.Contains(pos)) continue;
          if (searched.ContainsKey(pos)) continue;
          searched.Add(pos, depth);
          next.Add((ushort)(pos - 1));
          next.Add((ushort)(pos + 1));
          next.Add((ushort)(pos - width));
          next.Add((ushort)(pos + width));
        }
        search = next;
      }

      var result = new List<ushort> { playerPos };
      for (ushort d = (ushort)(searched[playerPos] - 1); d > 0; d--)
      {
        playerPos = result.Last();
        if (searched.ContainsKey((ushort)(playerPos - width)) && searched[(ushort)(playerPos - width)] == d)
        {
          result.Add((ushort)(playerPos - width));
          continue;
        }
        if (searched.ContainsKey((ushort)(playerPos - 1)) && searched[(ushort)(playerPos - 1)] == d)
        {
          result.Add((ushort)(playerPos - 1));
          continue;
        }
        if (searched.ContainsKey((ushort)(playerPos + 1)) && searched[(ushort)(playerPos + 1)] == d)
        {
          result.Add((ushort)(playerPos + 1));
          continue;
        }
        if (searched.ContainsKey((ushort)(playerPos + width)) && searched[(ushort)(playerPos + width)] == d)
        {
          result.Add((ushort)(playerPos + width));
          continue;
        }
        throw new Exception("search-error");
      }

      return result;
    }
    #endregion

    static Dictionary<int, HashSet<ulong>> boxesHash;
    static HashSet<ushort> invalidBoxes;

    static ushort[] TestScan(int width, TopLeftTodo topLeftTodo, HashSet<ushort> ways, SokowahnField view, bool debug = true)
    {
      var state = topLeftTodo.state;
      view.SetGameState(state);

      var result = ScanBestTopLeftWay(state[0], width, ways, new HashSet<ushort>(state.Skip(1)));

      if (state.Length > 1 && CheckDeadlock(topLeftTodo, view, result[result.Count - 1])) return null;

      if (debug)
      {
        Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - 2));
        Console.WriteLine(new string(' ', Console.WindowWidth - 1));

        Console.SetCursorPosition(0, 1);
        Console.WriteLine(view.ToString());
        Console.WriteLine();

        string line = state[0] + " - " + string.Join(", ", result.Skip(1));
        Console.WriteLine(line);
        Console.WriteLine();
      }

      var known = new HashSet<ushort>(topLeftTodo.known);

      if (known.Contains(result[result.Count - 1])) return new[] { result[result.Count - 1] };

      var resultFiltered = result.Where(f => !known.Contains(f)).ToArray();

      return resultFiltered;
    }

    static bool CheckDeadlock(TopLeftTodo topLeftTodo, SokowahnField view, ushort playerTopLeft)
    {
      HashSet<ulong> bHash;
      int boxesCount = topLeftTodo.state.Length - 1;
      if (boxesHash.TryGetValue(boxesCount, out bHash))
      {
        ulong crc = Crc64.Start.Crc64Update(playerTopLeft).Crc64Update(topLeftTodo.state, 1, boxesCount);
        if (!bHash.Contains(crc))
        {
          return true;
        }
      }
      else
      {
        // --- schnelle Vorprüfung ---
        for (int b = 1; b < topLeftTodo.state.Length; b++)
        {
          if (invalidBoxes.Contains(topLeftTodo.state[b])) return true;
        }

        // --- bestmögliche Hashprüfung ---
        int boxesCountMin = boxesHash.Count;
        bHash = boxesHash[boxesCountMin];
        var checkBoxes = topLeftTodo.state.Skip(1).Where(b => b != topLeftTodo.lastBox).ToArray();
        var checkState = new ushort[1 + boxesCountMin];
        checkState[0] = playerTopLeft;
        foreach (var variant in SokoTools.FieldBoxesVariants(checkBoxes.Length, boxesCountMin - 1, false))
        {
          for (int v = 0; v < variant.Length; v++) checkState[v + 1] = checkBoxes[variant[v]];
          AppendBoxes(checkState, topLeftTodo.lastBox);
          view.SetGameState(checkState);
          view.SetPlayerPos(ScanTopLeftPos(view));
          ulong crc = view.GetGameStateCrc();
          if (!bHash.Contains(crc))
          {
            return true;
          }
        }
      }
      return false;
    }

    #region # struct TopLeftTodo // Struktur einer TopLeft-Aufgabe
    /// <summary>
    /// Struktur einer TopLeft-Aufgabe
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct TopLeftTodo
    {
      /// <summary>
      /// merkt sich die Map-Position, welche auf die eigene Struktur zeigt
      /// </summary>
      public int mapIndex;
      /// <summary>
      /// letzte Kiste, welche hinzugefügt wurde
      /// </summary>
      public ushort lastBox;
      /// <summary>
      /// Spielstatus, welches noch geprüft werden muss
      /// </summary>
      public ushort[] state;
      /// <summary>
      /// Felder, welche bereits geprüft wurden (inkl. blocker)
      /// </summary>
      public ushort[] known;
    }
    #endregion

    #region # static ushort[] AppendBoxes(ushort[] boxes, ushort newBox) // fügt eine Box zum neuen Array hinzu (sortiert)
    /// <summary>
    /// fügt eine Box zum Spielstatus hinzu (sortiert)
    /// </summary>
    /// <param name="state">bisheriger Spielstatus (sortiert)</param>
    /// <param name="newBox">Kiste, welche hinzugefügt werden soll</param>
    static void AppendBoxes(ushort[] state, ushort newBox)
    {
      int p = state.Length - 1;
      while (p > 1 && state[p - 1] > newBox)
      {
        state[p] = state[p - 1];
        p--;
      }
      state[p] = newBox;
    }

    /// <summary>
    /// fügt eine Box zum Spielstatus hinzu (sortiert)
    /// </summary>
    /// <param name="state">bisheriger Spielstatus (sortiert)</param>
    /// <param name="newBox">Kiste, welche hinzugefügt werden soll</param>
    /// <returns>neuer Spielstatus</returns>
    static ushort[] AppendBoxesNewArray(ushort[] state, ushort newBox)
    {
      var output = new ushort[state.Length + 1];
      Array.Copy(state, output, state.Length);
      int p = state.Length;
      while (p > 1 && output[p - 1] > newBox)
      {
        output[p] = output[p - 1];
        p--;
      }
      output[p] = newBox;
      return output;
    }
    #endregion

    static void ScanTopLeftFields(SokowahnField field)
    {
      boxesHash = new Dictionary<int, HashSet<ulong>>();
      for (int b = 1; b <= field.boxesCount; b++)
      {
        Console.Clear();
        Console.WriteLine();
        for (int known = 1; known < b; known++)
        {
          Console.WriteLine(" known Boxes: " + known + " - Hash: " + boxesHash[known].Count.ToString("N0") + " Nodes");
        }
        Console.WriteLine();
        Console.WriteLine(" --- Calc Boxes: " + b + " ---");
        Console.WriteLine();
        int time = Environment.TickCount;
        var hash = MiniSolverHashBuilder(field, b, b);
        time = Environment.TickCount - time;
        boxesHash.Add(b, new HashSet<ulong>(hash.First().Keys));
        if (time > 10000) break;
        if (time > 250 && b == field.boxesCount - 1) break;
      }
      Console.Clear();

      var view = new SokowahnField(field);
      int maxBoxes = field.boxesCount;

      int width = field.width;
      var ways = FilterWays((ushort)field.PlayerPos, width, new HashSet<ushort>(Enumerable.Range(0, field.fieldData.Length).Select(f => (ushort)f)), new HashSet<ushort>(field.fieldData.Select((c, i) => new { c, i = (ushort)i }).Where(x => x.c == '#').Select(x => x.i)));

      #region # // --- ungültige Einzel-Box Positionen suchen ---
      invalidBoxes = new HashSet<ushort>();
      foreach (ushort box in ways)
      {
        var checkCrc = new List<ulong>();
        if (ways.Contains((ushort)(box - 1)))
        {
          view.SetGameState(new[] { (ushort)(box - 1), box });
          view.SetPlayerPos(ScanTopLeftPos(view));
          checkCrc.Add(view.GetGameStateCrc());
        }
        if (ways.Contains((ushort)(box + 1)))
        {
          view.SetGameState(new[] { (ushort)(box + 1), box });
          view.SetPlayerPos(ScanTopLeftPos(view));
          checkCrc.Add(view.GetGameStateCrc());
        }
        if (ways.Contains((ushort)(box - width)))
        {
          view.SetGameState(new[] { (ushort)(box - width), box });
          view.SetPlayerPos(ScanTopLeftPos(view));
          checkCrc.Add(view.GetGameStateCrc());
        }
        if (ways.Contains((ushort)(box + width)))
        {
          view.SetGameState(new[] { (ushort)(box + width), box });
          view.SetPlayerPos(ScanTopLeftPos(view));
          checkCrc.Add(view.GetGameStateCrc());
        }
        if (!checkCrc.Any(crc => boxesHash[1].Contains(crc)))
        {
          invalidBoxes.Add(box);
        }
      }
      #endregion

      var todo = new Queue<TopLeftTodo>();
      var map = new List<int> { ways.Count };

      foreach (var f in ways.OrderBy(x => x))
      {
        map.Add(0); // Index Platzhalter
        todo.Enqueue(new TopLeftTodo { mapIndex = map.Count - 1, state = new[] { f }, known = new ushort[0] });
      }

      int tick = 0;
      int nextTick = 0;
      while (todo.Count > 0)
      {
        bool dbg = false;

        if (map.Count > nextTick)
        {
          int t = Environment.TickCount;
          if (t > tick + 100)
          {
            Console.Title = "remain: " + todo.Count.ToString("N0") + " / " + map.Count.ToString("N0") + " (" + (Process.GetCurrentProcess().WorkingSet64 / 1048576.0).ToString("N1") + " MB)";
            tick = t;
            dbg = true;
          }
          nextTick += 1000;
        }

        var next = todo.Dequeue();
        var result = TestScan(width, next, ways, view, dbg);

        if (result != null)
        {
          if (result.Length <= 1)
          {
            map[next.mapIndex] = -result[0];
          }
          else
          {
            map[next.mapIndex] = map.Count;
            map.Add(result.Length - 1);
            var known = new HashSet<ushort>(next.known);
            for (int r = 1; r < result.Length; r++)
            {
              map.Add(result[r]);
              known.Add(result[r]);
              map.Add(0);  // Index Platzhalter
              if (next.state.Length <= maxBoxes)
              {
                var newState = AppendBoxesNewArray(next.state, result[r]);
                newState[0] = result[r - 1];
                todo.Enqueue(new TopLeftTodo { mapIndex = map.Count - 1, state = newState, lastBox = result[r], known = known.ToArray() });
              }
            }
          }
        }
        else
        {
          map[next.mapIndex] = -1; // ungültige Position
          if (dbg)
          {
            nextTick -= 1000;
            tick = 0;
          }
        }
      }

      Console.WriteLine();
      Console.WriteLine("Map-Size: " + map.Count.ToString("N0") + " (" + (map.Count / 262144.0).ToString("N1") + " MB)");
      Console.WriteLine();
    }

    static void Main()
    {
      //MiniGame(new SokowahnField(TestLevel1));
      //MiniGame(new SokowahnField(TestLevel3));
      //MiniGame(new SokowahnField(TestLevel4));
      //MiniGame(new SokowahnField(TestLevel2));

      //MiniSolver(new SokowahnField(TestLevel1));

      //MiniSolverHashBuilder(new SokowahnField(TestLevel3));
      //MiniSolverHashBuilder2(new SokowahnField(TestLevel3));

      ScanTopLeftFields(new SokowahnField(TestLevel3));

      #region # --- ScanTopLeftFields ---
      // --- base -------------------------------------- --- ushort-optimize ---------------------
      //  Level 1:       34.194 -    20 MB                      34.194 -    19 MB
      //  Level 2:       19.747 -    20 MB                      19.747 -    17 MB
      //  Level 3:      672.351 -   113 MB                     672.351 -    41 MB
      //  Level 4:    4.090.845 -   317 MB                   4.090.845 -   133 MB
      //  Level 5: * 15.245.914 - 5.093 MB (4.404.002)   * 111.678.536 - 5.002 MB (24.615.628)
      //  Level 6: * 14.234.848 - 5.053 MB (5.085.224)
      //  Level 7: * 15.718.791 - 5.017 MB (5.222.287)
      //  Level 8: *  7.197.767 - 5.091 MB (3.474.847)
      // --- hash-blocker ------------------------------ --- stopper-optimize --------------------
      //  Level 1:       26.479 -    15 MB                      25.862 -    15 MB
      //  Level 2:        7.554 -    15 MB                       6.610 -    15 MB
      //  Level 3:      193.988 -   126 MB                     154.561 -   125 MB
      //  Level 4:    1.693.282 -   119 MB                   1.447.603 -    89 MB
      //  Level 5:   17.309.502 -   796 MB (4 Boxes)        15.457.591 -   654 MB (4 Boxes)
      #endregion


      #region # // --- Level 3 Hash-Stats ---
      // boxes todoLen todoLenEnd  Hashtable
      //     1      18        249         47
      //     2      60     11.392      1.025
      //     3     110    237.475     13.832
      //     4     102  2.865.660    127.887
      //     5      42  6.207.453    843.347
      //     6       8  5.055.224  4.021.944
      #endregion

      //Console.ReadLine();
    }
  }
}
