#region # using *.*

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    const string TestLevel1 = "       ###  ^_^  \n"
                            + "       #.#       \n"
                            + "   #####.#####   \n"
                            + "  ##         ##  \n"
                            + " ##  # # # #  ## \n"
                            + " #  ##     ##  # \n"
                            + " # ##  # #  ## #     \n"
                            + " #     $@$     # \n"
                            + " ####  ###  #### \n"
                            + "    #### #### :) \n";

    const string TestLevel2 = "  ####\n"
                            + "### @#\n"
                            + "#    #\n"
                            + "# .#.###\n"
                            + "# $    #\n"
                            + "##*#*# #\n"
                            + "# $    #\n"
                            + "#   ####\n"
                            + "#####";

    const string TestLevel3 = "    #####\n"
                            + "    #   #\n"
                            + "    #$  #\n"
                            + "  ###  $##\n"
                            + "  #  $ $ #\n"
                            + "### # ## #   ######\n"
                            + "#   # ## #####  ..#\n"
                            + "# $  $          ..#\n"
                            + "##### ### #@##  ..#\n"
                            + "    #     #########\n"
                            + "    #######";

    const string TestLevel4 = "########\n"
                            + "#.#    #\n"
                            + "#.# ## ####\n"
                            + "#.# $   $ #\n"
                            + "#.# # # # #\n"
                            + "#.# # # # #\n"
                            + "#   # #   #\n"
                            + "# $ $ # $ #\n"
                            + "#   #@  ###\n"
                            + "#########\n";

    const string TestLevel5 = "   #####\n"
                            + "   #   #\n"
                            + "   # # ##########\n"
                            + "#### #  #   #   #\n"
                            + "#       # $   $ #####\n"
                            + "# ## #$ #   #   #   #\n"
                            + "# $  #  ## ## $   $ #\n"
                            + "# ## #####  #   #   #\n"
                            + "# $   #  #  ###### ##\n"
                            + "### $    @  ...# # #\n"
                            + "  #   #     ...# # #\n"
                            + "  #####  ###...# # ###\n"
                            + "      #### ##### #   #\n"
                            + "                 # $ #\n"
                            + "                 #   #\n"
                            + "                 #####\n";

    #region # static void CreateProject() // erstellt das Projekt mit allen Dateien
    /// <summary>
    /// erstellt das Projekt mit allen Dateien
    /// </summary>
    static void CreateProject()
    {
      Directory.CreateDirectory(PathData);
      Directory.CreateDirectory(PathTest);

      var solutionGuid = Guid.NewGuid();
      var projectGuid = Guid.NewGuid();

      const string ProjectName = "Sokowahn";
      const string ProjectFile = ProjectName + ".csproj";
      const string SolutionFile = ProjectName + ".sln";
      const string CsFile = ProjectName + ".cs";

      var csFile = new CsFile(true);
      csFile.Write();
      csFile.Write("using System;");
      csFile.Write();
      csFile.Write("namespace " + ProjectName, ns =>
      {
        ns.Write("unsafe class Program", cl =>
        {
          cl.Write("static void Main(string[] args)", main =>
          {
            main.Write("Console.WriteLine('Hello World!');");
            main.Write("Console.ReadLine();");
          });
        });
      });

      csFile.SaveToFile(PathTest + CsFile);

      var projectFile = CsProject.CreateCsProjectFile(projectGuid, ProjectName, new[] { "System" }, new[] { CsFile });
      projectFile.SaveToFile(PathTest + ProjectFile);

      var solutionFile = CsProject.CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", ProjectFile);
      solutionFile.SaveToFile(PathTest + SolutionFile);
    }
    #endregion

    #region # static void MiniGame(SokowahnField field) // startet eine kleine spielbare Konsolen-Version eines Spielfeldes
    /// <summary>
    /// startet eine kleine spielbare Konsolen-Version eines Spielfeldes
    /// </summary>
    /// <param name="field">Spielfeld, welches benutzt werden soll</param>
    static void MiniGame(SokowahnField field)
    {
      var game = new SokowahnField(field);
      var steps = new Stack<ushort[]>();

      for (; ; )
      {
        string output = game.ToString();
        int playerChar = output.IndexOfAny(new[] { '@', '+' });

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(output.Substring(0, playerChar));

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(output[playerChar]);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(output.Remove(0, playerChar + 1));

        Console.WriteLine(); Console.WriteLine();
        Console.WriteLine("Steps:  " + steps.Count.ToString("N0"));
        Console.WriteLine();
        Console.WriteLine("Remain: " + game.boxesRemain);

        if (game.boxesRemain == 0) return;

        bool step = false;
        var oldState = game.GetGameState();

        switch (Console.ReadKey(true).Key)
        {
          case ConsoleKey.Escape: return;

          case ConsoleKey.A:
          case ConsoleKey.NumPad4:
          case ConsoleKey.LeftArrow: step = game.MoveLeft(); break;

          case ConsoleKey.D:
          case ConsoleKey.NumPad6:
          case ConsoleKey.RightArrow: step = game.MoveRight(); break;

          case ConsoleKey.W:
          case ConsoleKey.NumPad8:
          case ConsoleKey.UpArrow: step = game.MoveUp(); break;

          case ConsoleKey.S:
          case ConsoleKey.NumPad2:
          case ConsoleKey.DownArrow: step = game.MoveDown(); break;

          case ConsoleKey.Z:
          case ConsoleKey.Delete:
          case ConsoleKey.Backspace:
          {
            if (steps.Count == 0) break;
            game.SetGameState(steps.Pop());
          } break;

          default: continue;
        }

        if (step)
        {
          steps.Push(oldState);
        }
      }
    }
    #endregion

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

    #region # static bool EdgeBoxCheck(char[] data, int pos, int w) // prüft, ob eine Kiste sich in einer nicht mehr lösbaren Position befindet
    /// <summary>
    /// prüft, ob eine Kiste sich in einer nicht mehr lösbaren Position befindet
    /// </summary>
    /// <param name="data">Daten des Spielfeldes</param>
    /// <param name="pos">die Position der zu prüfenden Kiste</param>
    /// <param name="w">breite des Spielfeldes</param>
    /// <returns>true, wenn eine ungültige Stellung erkannt wurde</returns>
    static bool EdgeBoxCheck(char[] data, int pos, int w)
    {
      if (data[pos] != '$') return false;

      if (data[pos - 1] == '#' && data[pos - w] == '#') return true;
      if (data[pos + 1] == '#' && data[pos - w] == '#') return true;
      if (data[pos - 1] == '#' && data[pos + w] == '#') return true;
      if (data[pos + 1] == '#' && data[pos + w] == '#') return true;

      if ((data[pos - 1 - w] == '#' || data[pos - 1 - w] == '$' || data[pos - 1 - w] == '*') && (data[pos - 1] == '#' || data[pos - 1] == '$' || data[pos - 1] == '*') && (data[pos - w] == '#' || data[pos - w] == '$' || data[pos - w] == '*')) return true;
      if ((data[pos + 1 - w] == '#' || data[pos + 1 - w] == '$' || data[pos + 1 - w] == '*') && (data[pos + 1] == '#' || data[pos + 1] == '$' || data[pos + 1] == '*') && (data[pos - w] == '#' || data[pos - w] == '$' || data[pos - w] == '*')) return true;
      if ((data[pos - 1 + w] == '#' || data[pos - 1 + w] == '$' || data[pos - 1 + w] == '*') && (data[pos - 1] == '#' || data[pos - 1] == '$' || data[pos - 1] == '*') && (data[pos + w] == '#' || data[pos + w] == '$' || data[pos + w] == '*')) return true;
      if ((data[pos + 1 + w] == '#' || data[pos + 1 + w] == '$' || data[pos + 1 + w] == '*') && (data[pos + 1] == '#' || data[pos + 1] == '$' || data[pos + 1] == '*') && (data[pos + w] == '#' || data[pos + w] == '$' || data[pos + w] == '*')) return true;

      return false;
    }
    #endregion
    #region # static bool EdgeFailCheck(SokowahnField field) // prüft, ob der Spieler gerade eine Kiste in die Ecke geschoben hat
    /// <summary>
    /// prüft, ob der Spieler gerade eine Kiste in die Ecke geschoben hat
    /// </summary>
    /// <param name="field">Spielfeld, welches überprüft werden soll</param>
    /// <returns>true, wenn eine nicht mehr Lösbare Stellung gefunden wurde</returns>
    static bool EdgeFailCheck(SokowahnField field)
    {
      var data = field.fieldData;
      int pos = field.PlayerPos;

      if (EdgeBoxCheck(data, pos - 1, field.width)) return true;
      if (EdgeBoxCheck(data, pos + 1, field.width)) return true;
      if (EdgeBoxCheck(data, pos - field.width, field.width)) return true;
      if (EdgeBoxCheck(data, pos + field.width, field.width)) return true;

      return false;
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

        if (EdgeFailCheck(scanner)) continue;
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

    /// <summary>
    /// analysiert alle möglichen Kistenstellungen mit einfachen (langsamen) Methoden
    /// </summary>
    /// <param name="field">Feld, welches durchsucht werden soll</param>
    static void MiniSolverHashBuilder(SokowahnField field)
    {
      var scanner = new SokowahnField(field);
      Console.WriteLine(scanner.ToString());
      Console.WriteLine();

      var targetFields = scanner.fieldData.Select((c, i) => new { c, i }).Where(f => f.c == '.' || f.c == '*').Select(f => (ushort)f.i).ToArray();

      for (int boxesCount = 1; boxesCount <= targetFields.Length; boxesCount++)
      {

        // --- Variablen initialisieren ---
        var boxes = new ushort[boxesCount];
        int stateLen = 1 + boxes.Length;
        var todoBuf = new ushort[16777216 / (stateLen + 1) * (stateLen + 1)];
        int todoPos = 0, todoLen = 0;

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

        // --- Aufgaben weiter rückwärts gerichtet abarbeiten ---
        {
          var hash = new Dictionary<ulong, ushort>();
          var nextBuf = new ushort[stateLen * boxesCount * 4];

          while (todoPos < todoLen)
          {
            ushort depth = todoBuf[todoPos++];
            scanner.SetGameState(todoBuf, todoPos); todoPos += stateLen;
            scanner.SetPlayerPos(ScanTopLeftPos(scanner));

            ulong crc = scanner.GetGameStateCrc();
            if (hash.ContainsKey(crc)) continue;
            hash.Add(crc, depth);

            if ((hash.Count & 0xffff) == 0) Console.WriteLine("[" + boxesCount + "] (" + depth + ") " + ((todoLen - todoPos) / (stateLen + 1)).ToString("N0") + " / " + hash.Count.ToString("N0"));

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
          Console.WriteLine();
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine("[" + boxesCount + "] ok. Hash: " + hash.Count.ToString("N0"));
          Console.ForegroundColor = ConsoleColor.Gray;
          Console.WriteLine();
        }
      }
    }

    /// <summary>
    /// rundet den Wert auf einer Zweier-Potenz auf (z.B.: 6 = 8, 7 = 8, 8 = 8, 9 = 16, 10 = 16 usw...)
    /// </summary>
    /// <param name="value">Wert, welcher aufgerundet werden soll</param>
    /// <returns>fertig aufgerundeter Wert</returns>
    static int RoundUpP2(int value)
    {
      int multi = 1;
      while (multi < value) multi += multi;
      return multi;
    }

    /// <summary>
    /// analysiert alle möglichen Kistenstellungen mit spezialkompilierten Hochleistungs-Methoden
    /// </summary>
    /// <param name="field">Feld, welches durchsucht werden soll</param>
    static void MiniSolverHashBuilder2(SokowahnField field)
    {
      var scanner = new SokowahnField(field);
      Console.WriteLine(scanner.ToString());
      Console.WriteLine();


      string levelId = scanner.GetLevelId();

      var solutionGuid = CsProject.NewGuid("S" + levelId);
      var projectGuid = CsProject.NewGuid("P" + levelId);

      string projectName = "Sokowahn_HashBuilder_" + levelId;

      var csFile = new CsFile(true);

      #region # // --- Hauptkommentar inkl. Level erstellen ---
      int commentWidth = Math.Max(scanner.width + 2, 35);
      commentWidth = (commentWidth + 1) / 2 * 2 + scanner.width % 2;
      string emptyLine = " *" + new string(' ', commentWidth - 2) + "*";
      csFile.Write();
      csFile.Write();
      csFile.Write("/" + new string('*', commentWidth));
      csFile.Write(emptyLine);
      csFile.Write((" *  " + new string(' ', (commentWidth - 26) / 2) + "--- Hash Builder ---").PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      csFile.Write(" " + new string('*', commentWidth));
      csFile.Write(emptyLine);
      csFile.Write((" *  Level-Hash: " + levelId.Remove(0, levelId.LastIndexOf('_') + 1)).PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      csFile.Write((" *  Size      : " + scanner.width + " x " + scanner.height + " (" + (scanner.width * scanner.height).ToString("N0") + ")").PadRight(commentWidth, ' ') + "*");
      csFile.Write((" *  Boxes     : " + scanner.boxesCount).PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      string centerChars = new string(' ', (commentWidth - scanner.width - 2) / 2);
      csFile.Write(scanner.ToString().Replace("\r", "").Split('\n').Select(x => " *" + centerChars + x + centerChars + "*"));
      csFile.Write(emptyLine);
      csFile.Write(" " + new string('*', commentWidth) + "/");
      csFile.Write();
      csFile.Write();
      #endregion

      #region # // --- using *.* ---
      csFile.Write("#region # using *.*");
      csFile.Write();
      csFile.Write("using System.Linq;");
      csFile.Write("using System.Collections.Generic;");
      csFile.Write();
      csFile.Write("#endregion");
      csFile.Write();
      csFile.Write();
      #endregion

      csFile.Write("namespace " + projectName, ns =>
      {
        int fWidth = RoundUpP2(scanner.width);
        var fChars = new char[fWidth * scanner.height];
        ns.Write("static class Program", cl =>
        {
          #region # // --- static readonly char[] FieldData = ... ---
          cl.Write("const int FieldWidth = " + fWidth + "; // real: " + scanner.width);
          cl.Write("const int FieldHeight = " + scanner.height + ";");
          cl.Write("static readonly char[] FieldData =", f =>
          {
            var zeile = new StringBuilder();
            for (int y = 0; y < scanner.height; y++)
            {
              zeile.Clear();
              zeile.Append("/* " + (y * fWidth).ToString("N0").PadLeft(((scanner.height - 1) * fWidth).ToString("N0").Length) + " */ ");
              for (int x = 0; x < fWidth; x++)
              {
                char c;
                if (x < scanner.width)
                {
                  c = scanner.fieldData[x + y * scanner.width];
                }
                else
                {
                  if (x == scanner.width) zeile.Append("  ");
                  c = ' ';
                }
                fChars[x + y * fWidth] = c;
                if (c != '#') c = ' ';
                zeile.Append('´').Append(c).Append("´,");
              }
              if (y == scanner.height - 1) zeile.Remove(zeile.Length - 1, 1);
              f.Write(zeile.ToString());
            }
          });
          cl.WriteV(";\r\n\r\n");
          #endregion

          #region # // --- static readonly ushort[] TargetFields ---
          var targetFields = fChars.Select((c, i) => new { c, i }).Where(f => f.c == '.' || f.c == '*').Select(f => (ushort)f.i).ToArray();
          cl.Write("static readonly ushort[] TargetPosis = { " + string.Join(", ", targetFields) + " };");
          cl.Write();
          #endregion

          cl.Write("static void Main()", main =>
          {
            main.Write("for (int boxesCount = 1; boxesCount <= TargetPosis.Length; boxesCount++)", bx =>
            {
              bx.Write("var boxes = new ushort[boxesCount];");
              bx.Write("int stateLen = 1 + boxes.Length;");
              bx.Write("var todoBuf = new ushort[16777216 / (stateLen + 1) * (stateLen + 1)];");
              bx.Write("int todoPos = 0, todoLen = 0;");

              bx.Write("", sc =>
              {
                sc.Write("int emptyPlayerPos = FieldData.ToList().IndexOf(´ ´);");
                sc.Write("int[] playerDirections = { -1, +1, -FieldWidth, +FieldWidth };");
                sc.Write("var checkDuplicates = new HashSet<ulong>();");
                sc.Write();
                sc.Write("foreach (var boxesVariant in SokoTools.FieldBoxesVariants(TargetPosis.Length, boxesCount).Select(v => v.Select(f => TargetPosis[f]).ToArray()))", fe =>
                {
                  //      scanner.SetPlayerPos(emptyPlayerPos);
                  //scanner.SetBoxes(boxesVariant);
                  //      var fieldData = scanner.fieldData;
                  //      foreach (ushort box in boxesVariant)
                  //      {
                  //        foreach (int playerDir in playerDirections)
                  //        {
                  //          int playerPos = box - playerDir;
                  //          if (fieldData[playerPos] == '#' || fieldData[playerPos] == '$' || fieldData[playerPos] == '*') continue;
                  //          int revPos = playerPos - playerDir;
                  //          if (fieldData[revPos] == '#' || fieldData[revPos] == '$' || fieldData[revPos] == '*') continue;

                  //          scanner.SetPlayerPos(playerPos);
                  //          scanner.SetPlayerPos(ScanTopLeftPos(scanner));

                  //          ulong crc = scanner.GetGameStateCrc();
                  //          if (checkDuplicates.Contains(crc)) continue;
                  //          checkDuplicates.Add(crc);

                  //          todoBuf[todoLen++] = 0;
                  //          todoLen += scanner.GetGameState(todoBuf, todoLen);
                  //        }
                  //      }
                });
              });

              //  // --- Aufgaben weiter rückwärts gerichtet abarbeiten ---
              //  {
              //    var hash = new Dictionary<ulong, ushort>();
              //    var nextBuf = new ushort[stateLen * boxesCount * 4];

              //    while (todoPos < todoLen)
              //    {
              //      ushort depth = todoBuf[todoPos++];
              //      scanner.SetGameState(todoBuf, todoPos); todoPos += stateLen;
              //      scanner.SetPlayerPos(ScanTopLeftPos(scanner));

              //      ulong crc = scanner.GetGameStateCrc();
              //      if (hash.ContainsKey(crc)) continue;
              //      hash.Add(crc, depth);

              //      if ((hash.Count & 0xffff) == 0) Console.WriteLine("[" + boxesCount + "] (" + depth + ") " + ((todoLen - todoPos) / (stateLen + 1)).ToString("N0") + " / " + hash.Count.ToString("N0"));

              //      depth++;
              //      int nextLength = scanner.ScanReverseMoves(nextBuf) * stateLen;
              //      for (int next = 0; next < nextLength; next += stateLen)
              //      {
              //        todoBuf[todoLen++] = depth;
              //        for (int i = 0; i < stateLen; i++) todoBuf[todoLen++] = nextBuf[next + i];
              //      }
              //      if (todoBuf.Length - todoLen < nextLength * 2)
              //      {
              //        Array.Copy(todoBuf, todoPos, todoBuf, 0, todoLen - todoPos);
              //        todoLen -= todoPos;
              //        todoPos = 0;
              //      }
              //    }
              //    Console.WriteLine();
              //    Console.ForegroundColor = ConsoleColor.Yellow;
              //    Console.WriteLine("[" + boxesCount + "] ok. Hash: " + hash.Count.ToString("N0"));
              //    Console.ForegroundColor = ConsoleColor.Gray;
              //    Console.WriteLine();
              //  }
              //}
            });

          });
        });
      });

      csFile.SaveToFile(PathTest + "Program.cs");

      #region # // --- SokoTools.cs ---
      var csSokoTools = new CsFile(true);

      #region # // --- using *.* ---
      csSokoTools.Write();
      csSokoTools.Write();
      csSokoTools.Write("#region # using *.*");
      csSokoTools.Write();
      csSokoTools.Write("using System.Collections.Generic;");
      csSokoTools.Write();
      csSokoTools.Write("#endregion");
      csSokoTools.Write();
      csSokoTools.Write();
      #endregion

      csSokoTools.Write("namespace " + projectName, ns =>
      {
        ns.Write("static class SokoTools", cl =>
        {
          #region # // --- IEnumerable<int[]> FieldBoxesVariants(int fieldCount, int boxesCount) ---
          cl.Write("public static IEnumerable<int[]> FieldBoxesVariants(int fieldCount, int boxesCount)", m =>
          {
            m.Write("int dif = fieldCount - boxesCount;");
            m.Write("int end = boxesCount - 1;");
            m.Write();
            m.Write("var boxesVariant = new int[boxesCount];");
            m.Write();
            m.Write("for (int box = 0; ; )", f =>
            {
              f.Write("while (box < end) boxesVariant[box + 1] = boxesVariant[box++] + 1;");
              f.Write("yield return boxesVariant;");
              f.Write("while (boxesVariant[box]++ == box + dif) if (--box < 0) yield break;");
            });
          });
          #endregion
        });
      });

      csSokoTools.SaveToFile(PathTest + "SokoTools.cs");
      #endregion

      var projectFile = CsProject.CreateCsProjectFile(projectGuid, projectName, new[] { "System" }, new[] { "Program.cs", "SokoTools.cs" });
      projectFile.SaveToFile(PathTest + projectName + ".csproj");

      var solutionFile = CsProject.CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", projectName + ".csproj");
      solutionFile.SaveToFile(PathTest + projectName + ".sln");

      CsCompiler.Compile(PathTest + projectName + ".sln");
    }

    static void Main()
    {
      //MiniGame(new SokowahnField(TestLevel1));
      //MiniGame(new SokowahnField(TestLevel3));
      //MiniGame(new SokowahnField(TestLevel4));
      //MiniGame(new SokowahnField(TestLevel2));

      //MiniSolver(new SokowahnField(TestLevel));

      //MiniSolverHashBuilder(new SokowahnField(TestLevel3));
      MiniSolverHashBuilder2(new SokowahnField(TestLevel3));

      //Console.ReadLine();

      // CreateProject();
    }
  }
}
