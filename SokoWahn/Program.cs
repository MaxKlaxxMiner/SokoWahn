#region # using *.*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    #region # // --- Test Levels ---
    /// <summary>
    /// 78 moves, 17 pushes - LrRluulldlddrUUUUdddlllluurururRRlddrruUUdddrrdrddlUUUUdddrrrruulululLLrddlluU
    /// </summary>
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

    /// <summary>
    /// 115 moves, 20 pushes - dlllddRuurrddrrddllldlluRUUluurrrddrrddllLdlUrrrruullDuuulllddrRlluurrrdDllUluRRurDllddrrrrddlLLdlluRUUrrrrddllLdlU
    /// </summary>
    const string TestLevel2 = "  ####\n"
                            + "### @#\n"
                            + "#    #\n"
                            + "# .#.###\n"
                            + "# $    #\n"
                            + "##*#*# #\n"
                            + "# $    #\n"
                            + "#   ####\n"
                            + "#####";

    /// <summary>
    /// 230 moves, 97 pushes - ullluuuLUllDlldddrRRRRRRRRRRurDllllllluuululldDDuulldddrRRRRRRRRRRdRRlUllllllluuulLulDDDuulldddrRRRRRRRRRRRRlllllllllllllulldRRRRRRRRRRRRRuRRlDllllllluuulluuurDDuullDDDDDuulldddrRRRRRRRRRRRllllllluuuLLulDDDuulldddrRRRRRRRRRRdRUluR
    /// </summary>
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

    /// <summary>
    /// 206 moves, 59 pushes - uuuuuruulllddddddLdlUUUUUUddddrruuuuurrrddlLrddddLLLdlUUUUUdddrruuUrrruulllDDDDDuuurrddddlLLdlUUUUdddrrrrdrruurrdLruuuuLLLLLrruullldDDDDuuurrddddlLLdlUUUddrrrrdrrUUUUddrruuulLLLLrruullldDDDDuuurrddddlLLdlUU
    /// </summary>
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

    /// <summary>
    /// 361 moves, 139 pushes - llllulldRRRRRRRuuuuluurDDDDDDldRRRRurDlllluuuuuurrrurrdLLLLulDDDDDDldRRRlluuuuuurrrdddrUUruLLLLulDDDDDDRRRDullllllllluuuuulllddRRlluurrrdDDDldRRRRRRdRRRRlluulDldRRRlluuuuuurrrrrddrrurrdLLLLdlUUruLLLLulDDDDDDldRRullllllluuulllddRRlluurrrdDldRRRRRRRRRDulllllllluuuuurrrddlUruLLruuullddDDDDDldRRRRRRRRluuuuurrrrrddrrrddddddrrdLdlUUUUUUUruLLLLdlUUruLLLLulDDDDDDldRR
    /// </summary>
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

    /// <summary>
    /// 1121 moves, 303 pushes - dllluullldddldldldldllURURURURUdldldldlluRuRuRuRuRRurrdLddLdLdLdLdlluuururururRlldldldldddrruLUlldRdrruruLrrururuUllldldDRURUdlluRuRRuRRRRddddddrrrdrruuuuuuuullllDDullldLddLLLuurRllddrrruUluRRRRddddddrrruuuulLrrdddddrruuuuuuuullllDDulllldddllUluRRlddrruUluRRRRdDRdLdddrrruuuuLLrrdddddrruuuuuuuullllDDullllldllddddrUUUluRRlddrruUluRRRRdDDDlddrrrruuuulLrrdddddrruuuuuuuullllDDullllldlldddddldlluuuRRRdrUUUluRRlddrruUluRRRRdDlDDrDLdRRRuruuulLdLdddrrdrrruuuuuuuullllDDullllldlldddlluRRdrUUluRRlddrruUluRRRRdDDulDDDrdLrrruruuulLdlDDldRddrrurrruuuuuuuullllDDullllldlldddddlUUluRRdrUUluRRlddrruUluRRRRdDDDDlddrdrruuuruuulLrrdddlddrrruuuuuuuullllDDulllldddllllldldRRdrUUldddlUUluRRRdrUUUluRRlddrruUluRRRRdDDlddddrdrruuuruuulLrrdddlddrrruuuuuuuullllDDullllldlldldRdrUUluRRlddrruUluRRRRdDldddddrdrruuuruuulLdLrurrdddlddrrruuuuuuuullllDldDrrrdddDldRRdrUUUUUUUUdddddddllldlluluurDRRurDldRRdrUUUUUUUddddddllldllUluRRRurDldRRdrUUUUUUdddddlllullldlluRRRRRurDldRRdrUUUUUddddlluuuuullluurDldlDDDulDDldRRluRRRRurDldRRdrUUUUdddllldllUluRRRurDldRRdrUUUddlluuuuulLdLDDldRRRurDldRRdrUUdlluuuuullulDDDDldRRRurDldRRdrUlluuuuullllDDDldRRRRurDldRR
    /// </summary>
    const string TestLevel6 = "           #######\n"
                            + "      ######    .#\n"
                            + "    ###      ###.#\n"
                            + "   ##     #  #@#.#\n"
                            + "  ##  $# #     #.#\n"
                            + " ##  $$  #   # #.#\n"
                            + "##  $$  #   ## #.#\n"
                            + "#  $$  ##   #  #.#\n"
                            + "# $$  ##       #.#\n"
                            + "##   ###    #   .#\n"
                            + " ##### ####   #  #\n"
                            + "          ########\n";
    #endregion

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

    #region # static void MiniSolverHashBuilder(SokowahnField field) // analysiert alle möglichen Kistenstellungen mit einfachen (langsamen) Methoden
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
          stopWatch.Stop();
          Console.WriteLine();
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine("[" + boxesCount + "] ok. Hash: " + hash.Count.ToString("N0") + " (" + stopWatch.ElapsedMilliseconds.ToString("N0") + " ms)");
          Console.ForegroundColor = ConsoleColor.Gray;
          Console.WriteLine();
        }
      }
    }
    #endregion

    #region # static void MiniSolverHashBuilder2(SokowahnField field) // analysiert alle möglichen Kistenstellungen mit spezialkompilierten Hochleistungs-Methoden
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

      var csFile = new CsFile();

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
      csFile.Write("using System;");
      csFile.Write("using System.Linq;");
      csFile.Write("using System.Collections.Generic;");
      csFile.Write("using System.Diagnostics;");
      csFile.Write();
      csFile.Write("// ReSharper disable UnusedMember.Local");
      csFile.Write();
      csFile.Write("#endregion");
      csFile.Write();
      csFile.Write();
      #endregion

      csFile.Write("namespace " + projectName, ns =>
      {
        var fChars = new char[scanner.width * scanner.height];
        ns.Write("static unsafe class Program", cl =>
        {
          #region # // --- static readonly char[] FieldData = ... ---
          cl.Write("const int FieldWidth = " + scanner.width + ";");
          cl.Write("const int FieldHeight = " + scanner.height + ";");
          cl.Write("const int FieldCount = FieldWidth * FieldHeight;");
          cl.Write();
          cl.Write("static readonly char[] FieldData =", f =>
          {
            var zeile = new StringBuilder();
            for (int y = 0; y < scanner.height; y++)
            {
              zeile.Clear();
              zeile.Append("/* " + (y * scanner.width).ToString("N0").PadLeft(((scanner.height - 1) * scanner.width).ToString("N0").Length) + " */ ");
              for (int x = 0; x < scanner.width; x++)
              {
                char c = scanner.fieldData[x + y * scanner.width];
                fChars[x + y * scanner.width] = c;
                if (c != '#') c = ' ';
                zeile.Append('\'').Append(c).Append("',");
              }
              if (y == scanner.height - 1) zeile.Remove(zeile.Length - 1, 1);
              f.Write(zeile.ToString());
            }
          });
          cl.WriteV(";\r\n\r\n");
          #endregion

          #region # // --- static readonly ushort[] TargetPosis ---
          var targetFields = fChars.Select((c, i) => new { c, i }).Where(f => f.c == '.' || f.c == '*').Select(f => (ushort)f.i).ToArray();
          cl.Write("static readonly ushort[] TargetPosis = { " + string.Join(", ", targetFields) + " };");
          cl.Write();
          #endregion

          #region # // --- static readonly ushort[] BoxPosis ---
          cl.Write("static readonly ushort[] BoxPosis = { " + string.Join(", ", targetFields.Select(x => fChars.Length - 1)) + " };");
          cl.Write();
          #endregion

          #region # // --- TxtView ---
          cl.Write("static string TxtViewP(int playerPos = -1) { return SokoTools.TxtView(FieldData, FieldWidth, TargetPosis, playerPos); }");
          cl.Write("static string TxtView { get { return TxtViewP(); } }");
          cl.Write();
          #endregion

          #region # // --- static int ScanTopLeftPos(int startPos) ---
          cl.Write("static readonly int[] PlayerDirections = { -1, +1, -FieldWidth, +FieldWidth };");
          cl.Write();
          cl.Write("static int ScanTopLeftPos(int startPos)", sc =>
          {
            sc.Write("int bestPos = int.MaxValue;");
            sc.Write("bool* scanned = stackalloc bool[FieldCount];");
            sc.Write("int* next = stackalloc int[FieldCount];");
            sc.Write("int nextPos = 0;");
            sc.Write("next[nextPos++] = startPos;");
            sc.Write("while (nextPos > 0)", wh =>
            {
              wh.Write("int checkPos = next[--nextPos];");
              wh.Write("if (checkPos < bestPos) bestPos = checkPos;");
              wh.Write("scanned[checkPos] = true;");
              wh.Write("if (!scanned[checkPos - 1] && FieldData[checkPos - 1] == ' ') next[nextPos++] = checkPos - 1;");
              wh.Write("if (!scanned[checkPos + 1] && FieldData[checkPos + 1] == ' ') next[nextPos++] = checkPos + 1;");
              wh.Write("if (!scanned[checkPos - FieldWidth] && FieldData[checkPos - FieldWidth] == ' ') next[nextPos++] = checkPos - FieldWidth;");
              wh.Write("if (!scanned[checkPos + FieldWidth] && FieldData[checkPos + FieldWidth] == ' ') next[nextPos++] = checkPos + FieldWidth;");
            });
            sc.Write("return bestPos;");
          });
          cl.Write();
          #endregion

          #region # // --- static ulong SetBoxes(ushort[] buf, int offset, int boxesCount) ---
          cl.Write("static ulong SetBoxes(ushort[] buf, int offset, int boxesCount)", sb =>
          {
            sb.Write("for (int i = 0; i < boxesCount; i++) FieldData[BoxPosis[i]] = ' ';");
            sb.Write();
            sb.Write("ulong crc = SokoTools.CrcStart;");
            sb.Write("for (int i = 0; i < boxesCount; i++)", f =>
            {
              f.Write("ushort pos = buf[offset + i];");
              f.Write("crc = SokoTools.CrcCompute(crc, pos);");
              f.Write("BoxPosis[i] = pos;");
              f.Write("FieldData[pos] = (char)(i + '0');");
            });
            sb.Write("return crc;");
          });
          cl.Write();
          #endregion

          #region # // --- static void MoveBox(ushort oldPos, ushort newPos) ---
          cl.Write("static void MoveBox(ushort oldPos, ushort newPos)", mb =>
          {
            mb.Write("int index = FieldData[oldPos] - '0';");
            mb.Write("FieldData[oldPos] = ' ';");
            mb.Write("FieldData[newPos] = (char)(index + '0');");
            mb.Write("BoxPosis[index] = newPos;");
            mb.Write();
            mb.Write("if (newPos < oldPos)", f =>
            {
              f.Write("while (index > 0 && newPos < BoxPosis[index - 1])", wh =>
              {
                wh.Write("BoxPosis[index] = BoxPosis[index - 1];");
                wh.Write("FieldData[BoxPosis[index]] = (char)(index + '0');");
                wh.Write("index--;");
                wh.Write("BoxPosis[index] = newPos;");
                wh.Write("FieldData[BoxPosis[index]] = (char)(index + '0');");
              });
            });
            mb.Write("else", f =>
            {
              f.Write("while (index < BoxPosis.Length - 1 && newPos > BoxPosis[index + 1])", wh =>
              {
                wh.Write("BoxPosis[index] = BoxPosis[index + 1];");
                wh.Write("FieldData[BoxPosis[index]] = (char)(index + '0');");
                wh.Write("index++;");
                wh.Write("BoxPosis[index] = newPos;");
                wh.Write("FieldData[BoxPosis[index]] = (char)(index + '0');");
              });
            });
          });
          cl.Write();
          #endregion

          #region # // --- static int ScanReverseMoves(int startPlayerPos, ushort[] output) ---
          cl.Write("static int ScanReverseMoves(int startPlayerPos, ushort[] output, int boxesCount)", sc =>
          {
            sc.Write("int outputLen = 0;");
            sc.Write();
            sc.Write("bool* scannedFields = stackalloc bool[FieldCount];");
            sc.Write("ushort* scanTodo = stackalloc ushort[FieldCount];");
            sc.Write();
            sc.Write("int scanTodoPos = 0;");
            sc.Write("int scanTodoLen = 0;");
            sc.Write();
            sc.Write("scanTodo[scanTodoLen++] = (ushort)startPlayerPos;");
            sc.Write("scannedFields[startPlayerPos] = true;");
            sc.Write();
            sc.Write("while (scanTodoPos < scanTodoLen)", wh =>
            {
              wh.Write("ushort scan = scanTodo[scanTodoPos++];");
              wh.Write();

              wh.Write("#region # // --- links (zurück nach rechts) ---");
              wh.Write("switch (FieldData[scan - 1])", sw =>
              {
                sw.Write("case '#': break;");
                sw.Write("case ' ': if (!scannedFields[scan - 1]) { scannedFields[scan - 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan - 1); } break;");
                sw.Write("default:", bx =>
                {
                  bx.Write("if (FieldData[scan + 1] != ' ') break;");
                  bx.Write();
                  bx.Write("MoveBox((ushort)(scan - 1), scan);");
                  bx.Write();
                  bx.Write("for (int i = 0; i < boxesCount; i++) output[outputLen + i] = BoxPosis[i];");
                  bx.Write("output[outputLen + boxesCount] = (ushort)(scan + 1);");
                  bx.Write("outputLen += boxesCount + 1;");
                  bx.Write();
                  bx.Write("MoveBox(scan, (ushort)(scan - 1));");
                });
                sw.Write("break;");
              });
              wh.Write("#endregion");
              wh.Write();

              wh.Write("#region # // --- rechts (zurück nach links) ---");
              wh.Write("switch (FieldData[scan + 1])", sw =>
              {
                sw.Write("case '#': break;");
                sw.Write("case ' ': if (!scannedFields[scan + 1]) { scannedFields[scan + 1] = true; scanTodo[scanTodoLen++] = (ushort)(scan + 1); } break;");
                sw.Write("default:", bx =>
                {
                  bx.Write("if (FieldData[scan - 1] != ' ') break;");
                  bx.Write();
                  bx.Write("MoveBox((ushort)(scan + 1), scan);");
                  bx.Write();
                  bx.Write("for (int i = 0; i < boxesCount; i++) output[outputLen + i] = BoxPosis[i];");
                  bx.Write("output[outputLen + boxesCount] = (ushort)(scan - 1);");
                  bx.Write("outputLen += boxesCount + 1;");
                  bx.Write();
                  bx.Write("MoveBox(scan, (ushort)(scan + 1));");
                });
                sw.Write("break;");
              });
              wh.Write("#endregion");
              wh.Write();

              wh.Write("#region # // --- oben (zurück nach unten) ---");
              wh.Write("switch (FieldData[scan - FieldWidth])", sw =>
              {
                sw.Write("case '#': break;");
                sw.Write("case ' ': if (!scannedFields[scan - FieldWidth]) { scannedFields[scan - FieldWidth] = true; scanTodo[scanTodoLen++] = (ushort)(scan - FieldWidth); } break;");
                sw.Write("default:", bx =>
                {
                  bx.Write("if (FieldData[scan + FieldWidth] != ' ') break;");
                  bx.Write();
                  bx.Write("MoveBox((ushort)(scan - FieldWidth), scan);");
                  bx.Write();
                  bx.Write("for (int i = 0; i < boxesCount; i++) output[outputLen + i] = BoxPosis[i];");
                  bx.Write("output[outputLen + boxesCount] = (ushort)(scan + FieldWidth);");
                  bx.Write("outputLen += boxesCount + 1;");
                  bx.Write();
                  bx.Write("MoveBox(scan, (ushort)(scan - FieldWidth));");
                });
                sw.Write("break;");
              });
              wh.Write("#endregion");
              wh.Write();

              wh.Write("#region # // --- unten (zurück nach oben) ---");
              wh.Write("switch (FieldData[scan + FieldWidth])", sw =>
              {
                sw.Write("case '#': break;");
                sw.Write("case ' ': if (!scannedFields[scan + FieldWidth]) { scannedFields[scan + FieldWidth] = true; scanTodo[scanTodoLen++] = (ushort)(scan + FieldWidth); } break;");
                sw.Write("default:", bx =>
                {
                  bx.Write("if (FieldData[scan - FieldWidth] != ' ') break;");
                  bx.Write();
                  bx.Write("MoveBox((ushort)(scan + FieldWidth), scan);");
                  bx.Write();
                  bx.Write("for (int i = 0; i < boxesCount; i++) output[outputLen + i] = BoxPosis[i];");
                  bx.Write("output[outputLen + boxesCount] = (ushort)(scan - FieldWidth);");
                  bx.Write("outputLen += boxesCount + 1;");
                  bx.Write();
                  bx.Write("MoveBox(scan, (ushort)(scan + FieldWidth));");
                });
                sw.Write("break;");
              });
              wh.Write("#endregion");
              wh.Write();
            });
            sc.Write();
            sc.Write("return outputLen;");
          });
          cl.Write();
          #endregion

          #region # // --- static void Main() ---
          cl.Write("static void Main()", main =>
          {
            main.Write("for (int boxesCount = 1; boxesCount <= TargetPosis.Length; boxesCount++)", bx =>
            {
              bx.Write("int stateLen = boxesCount + 1;");
              bx.Write("var todoBuf = new ushort[16777216 / (stateLen + 1) * (stateLen + 1)];");
              bx.Write("int todoLen = 0;");
              bx.Write("var stopWatch = Stopwatch.StartNew();");
              bx.Write();

              #region # // --- Suche End-Varianten ---
              bx.Write("#region # // --- search all finish-positions -> put into \"todoBuf\" ---", sc =>
              {
                sc.Write("var checkDuplicates = new HashSet<ulong>();");
                sc.Write();
                sc.Write("foreach (var boxesVariant in SokoTools.FieldBoxesVariants(TargetPosis.Length, boxesCount).Select(v => v.Select(f => TargetPosis[f]).ToArray()))", fe =>
                {
                  fe.Write("foreach (var box in boxesVariant) FieldData[box] = '$';");
                  fe.Write();
                  fe.Write("ulong boxCrc = SokoTools.CrcCompute(SokoTools.CrcStart, boxesVariant, 0, boxesVariant.Length);");
                  fe.Write();
                  fe.Write("foreach (var box in boxesVariant)", feb =>
                  {
                    feb.Write("foreach (int playerDir in PlayerDirections)", febs =>
                    {
                      febs.Write("int playerPos = box - playerDir;");
                      febs.Write("if (FieldData[playerPos] != ' ') continue;");
                      febs.Write("if (FieldData[playerPos - playerDir] != ' ') continue;");
                      febs.Write();
                      febs.Write("ulong crc = SokoTools.CrcCompute(boxCrc, playerPos);");
                      febs.Write("if (checkDuplicates.Contains(crc)) continue;");
                      febs.Write("checkDuplicates.Add(crc);");
                      febs.Write();
                      febs.Write("int topPlayerPos = ScanTopLeftPos(playerPos);");
                      febs.Write();
                      febs.Write("if (topPlayerPos != playerPos)", febst =>
                      {
                        febst.Write("crc = SokoTools.CrcCompute(boxCrc, topPlayerPos);");
                        febst.Write("if (checkDuplicates.Contains(crc)) continue;");
                        febst.Write("checkDuplicates.Add(crc);");
                      });
                      febs.Write();
                      febs.Write("todoBuf[todoLen++] = 0;");
                      febs.Write("for(int i = 0; i < boxesVariant.Length; i++) todoBuf[todoLen + i] = boxesVariant[i];");
                      febs.Write("todoBuf[todoLen + boxesVariant.Length] = (ushort)topPlayerPos;");
                      febs.Write("todoLen += stateLen;");
                    });
                  });
                  fe.Write();
                  fe.Write("foreach (var box in boxesVariant) FieldData[box] = ' ';");
                });
              });
              bx.Write("#endregion");
              bx.Write();
              #endregion

              #region # // --- Durchsuche Rückwärts alle Möglichkeiten ---
              bx.Write("#region # // --- search all possible positions (bruteforce-reverse) ---", sc =>
              {
                sc.Write("var hash = new Dictionary<ulong, ushort>();");
                sc.Write("var nextBuf = new ushort[stateLen * boxesCount * 4];");
                sc.Write();
                sc.Write("int todoPos = 0;");
                sc.Write("while (todoPos < todoLen)", wh =>
                {
                  wh.Write("ushort depth = todoBuf[todoPos++];");
                  wh.Write("ulong crc = SetBoxes(todoBuf, todoPos, boxesCount);");
                  wh.Write("int playerPos = ScanTopLeftPos(todoBuf[todoPos + boxesCount]);");
                  wh.Write("crc = SokoTools.CrcCompute(crc, playerPos);");
                  wh.Write();
                  wh.Write("if (hash.ContainsKey(crc))", skip =>
                  {
                    skip.Write("todoPos += stateLen;");
                    skip.Write("continue;");
                  });
                  wh.Write();
                  wh.Write("hash.Add(crc, depth);");
                  wh.Write("if ((hash.Count & 0xffff) == 0) Console.WriteLine(\"[\" + boxesCount + \"] (\" + depth + \") \" + ((todoLen - todoPos) / (stateLen + 1)).ToString(\"N0\") + \" / \" + hash.Count.ToString(\"N0\"));");
                  wh.Write();
                  wh.Write("depth++;");
                  wh.Write("int nextLength = ScanReverseMoves(playerPos, nextBuf, boxesCount);");
                  wh.Write("for (int next = 0; next < nextLength; next += stateLen)", f =>
                  {
                    f.Write("todoBuf[todoLen++] = depth;");
                    f.Write("for (int i = 0; i < stateLen; i++) todoBuf[todoLen++] = nextBuf[next + i];");
                  });
                  wh.Write();
                  wh.Write("todoPos += stateLen;");
                  wh.Write("if (todoBuf.Length - todoLen < nextLength * 2)", arr =>
                  {
                    arr.Write("Array.Copy(todoBuf, todoPos, todoBuf, 0, todoLen - todoPos);");
                    arr.Write("todoLen -= todoPos;");
                    arr.Write("todoPos = 0;");
                  });
                });
                sc.Write("stopWatch.Stop();");
                sc.Write("Console.WriteLine();");
                sc.Write("Console.ForegroundColor = ConsoleColor.Yellow;");
                sc.Write("Console.WriteLine(\"[\" + boxesCount + \"] ok. Hash: \" + hash.Count.ToString(\"N0\") + \" (\" + stopWatch.ElapsedMilliseconds.ToString(\"N0\") + \" ms)\");");
                sc.Write("Console.ForegroundColor = ConsoleColor.Gray;");
                sc.Write("Console.WriteLine();");
              });
              bx.Write("#endregion");
              #endregion
            });
          });
          #endregion
        });
      });

      csFile.SaveToFile(PathTest + "Program.cs");

      #region # // --- SokoTools.cs ---
      var csSokoTools = new CsFile();

      #region # // --- using *.* ---
      csSokoTools.Write();
      csSokoTools.Write();
      csSokoTools.Write("#region # using *.*");
      csSokoTools.Write();
      csSokoTools.Write("using System.Text;");
      csSokoTools.Write("using System.Linq;");
      csSokoTools.Write("using System.Collections.Generic;");
      csSokoTools.Write("using System.Runtime.CompilerServices;");
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
          cl.Write();
          #endregion

          #region # // --- Crc64-Tools ---
          cl.Write("public const ulong CrcStart = 0xcbf29ce484222325u;");
          cl.Write("const ulong CrcMul = 0x100000001b3;");
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("public static ulong CrcCompute(ulong crc64, int value)", crcf =>
          {
            cl.Write("return (crc64 ^ (uint)value) * CrcMul;");
          });
          cl.Write();
          cl.Write("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
          cl.Write("public static ulong CrcCompute(ulong crc64, ushort[] buffer, int ofs, int len)", crcf =>
          {
            crcf.Write("crc64 ^= buffer[ofs];");
            crcf.Write("for (int i = 1; i < len; i++) crc64 = crc64 * CrcMul ^ buffer[i + ofs];");
            crcf.Write("return crc64 * CrcMul;");
          });
          cl.Write();
          #endregion

          #region # // --- TxtView() ---
          cl.Write("public static string TxtView(char[] fieldData, int fieldWidth, ushort[] fieldTargets, int playerPos)", tx =>
          {
            tx.Write("var output = new StringBuilder();");
            tx.Write("for (int i = 0; i < fieldData.Length - 1; i++)", f =>
            {
              f.Write("bool target = fieldTargets.Any(x => x == i);");
              f.Write("bool player = playerPos == i;");
              f.Write("switch (fieldData[i])", sw =>
              {
                sw.Write("case ' ': output.Append(target ? (player ? '+' : '.') : (player ? '@' : ' ')); break;");
                sw.Write("case '#': output.Append('#'); break;");
                sw.Write("default: output.Append(target ? '*' : '$'); break;");
              });
              f.Write("if (i % fieldWidth == fieldWidth - 1) output.AppendLine();");
            });
            tx.Write("output.Append(fieldData[fieldData.Length - 2]).AppendLine().AppendLine();");
            tx.Write("return output.ToString();");
          });
          #endregion
        });
      });

      csSokoTools.SaveToFile(PathTest + "SokoTools.cs");
      #endregion

      #region # // --- Projekt speichern und kompilieren ---
      var projectFile = CsProject.CreateCsProjectFile(projectGuid, projectName, new[] { "System" }, new[] { "Program.cs", "SokoTools.cs" });
      projectFile.SaveToFile(PathTest + projectName + ".csproj");

      var solutionFile = CsProject.CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", projectName + ".csproj");
      solutionFile.SaveToFile(PathTest + projectName + ".sln");

      CsCompiler.Compile(PathTest + projectName + ".sln");
      #endregion
    }
    #endregion

    static void Main()
    {
      //MiniGame(new SokowahnField(TestLevel1));
      //MiniGame(new SokowahnField(TestLevel3));
      //MiniGame(new SokowahnField(TestLevel4));
      //MiniGame(new SokowahnField(TestLevel2));

      //MiniSolver(new SokowahnField(TestLevel));

      //MiniSolverHashBuilder(new SokowahnField(TestLevel3));
      MiniSolverHashBuilder2(new SokowahnField(TestLevel3));

      // --- Level 3 Hash-Stats ---
      // boxes todoLen todoLenEnd  Hashtable
      //     1      18        249         47
      //     2      60     11.392      1.025
      //     3     110    237.475     13.832
      //     4     102  2.865.660    127.887
      //     5      42  6.207.453    843.347
      //     6       8  5.055.224  4.021.944

      //Console.ReadLine();

      // CreateProject();
    }
  }
}
