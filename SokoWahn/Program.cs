#region # using *.*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    /// <summary>
    /// 229 moves, 76 pushes - RRRUUluRRRdddLdlUUUluRllluurrDullddrRdrddrruuuuuluurDDDDDuuuurrrddlUrdddlULrUruuLLLulDrddDrrrdddLUUddLLLdlUUdlllUUUrruullDDDDldRRRRRRRlllllluuuuurruRRurDDurrrddlUruLLrrdddLLrrddddlUUUruLdddllllllluuuuurrurrurDlllddLulDDDDldRRRRRR
    /// </summary>
    const string TestLevel7 = "     ####   \n"
                            + "   ###  ####\n"
                            + " ### $ $   #\n"
                            + " #   #..#$ #\n"
                            + " # $$#*.#  #\n"
                            + " #   ....$ #\n"
                            + "## # .#.#  #\n"
                            + "# $##$#.#$ #\n"
                            + "# @$    .$ #\n"
                            + "##  #  ##  #\n"
                            + " ###########\n";

    /// <summary>
    /// extrem
    /// </summary>
    const string TestLevel8 = "####################\n"
                            + "#                  #\n"
                            + "# $  $ $ $ $ $ $ $ #\n"
                            + "# $$$$$###########################################\n"
                            + "#                         .                      #\n"
                            + "# $$$$$#  $ $ $ $ $ ########################## # #\n"
                            + "#      #  $ $ $ $ $ #   $  $  $  $  $  $  $    # #\n"
                            + "# $$$$$#  ### # # ## #                       $   #\n"
                            + "#      #  #        #  # ##################### ## #\n"
                            + "# $$$$$#  #### ## ##$ #   #                 # #  #\n"
                            + "#      #     # #  #   # # .                 # #  #\n"
                            + "# $$$$$#  $$$# #  #   # # ################ ## #  #\n"
                            + "#      #     # #  # $ # #                #  #$#  #\n"
                            + "# $$$$$#  $$$# #  #   # # ############## #  # #  #\n"
                            + "#      #     # #  #   # #.#............# #  # #  #\n"
                            + "# $$$$$#  $$$# #  # $ # #.#............# #  # #  #\n"
                            + "#      #     # #  #   # #.#............# #  #    #\n"
                            + "# $$$$$#  $$$# #  #   # #.#............# #  #$#  #\n"
                            + "#      #     # #  # $ # #.#............# #  # #  #\n"
                            + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                            + "#      #     # #  #   # #.#.........   # #  # #  #\n"
                            + "#@$$$$$#  $$$# #  # $ #  ..............# #  #    #\n"
                            + "#      #     # #  #   # #.#.........  .# #  #$#  #\n"
                            + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                            + "#      #     # #  # $ # #.#............# #  # #  #\n"
                            + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                            + "#      #     # #  #   # #.#............# #  #    #\n"
                            + "# $$$$$#  $$$# #  # $ # #.#............# #  #$#  #\n"
                            + "#      #     # #  #   # # #............# #  # #  #\n"
                            + "# $$$$$#  $$$# #  #   # # ################  # #  #\n"
                            + "#      #     # #  # # # #                #  # #  #\n"
                            + "# $$$$$#  $$$# #  # $                       #    #\n"
                            + "#      #     # #  ## # ###################$##$#  #\n"
                            + "# $$$$$#  #    #     #                   # ## #  #\n"
                            + "#      #  #### ##### #  $$ $$ $$ $$ $$ $$   # #  #\n"
                            + "# $$$$$#           # # $  $  $  $  $  $  $$ # #  #\n"
                            + "#      #  ###  # # # # $  $  $  $  $  $  $  $ $  #\n"
                            + "# $$$$$######### # # ######################## ## #\n"
                            + "#                #                               #\n"
                            + "#      #       #                                 #\n"
                            + "##################################################\n";
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
            game.SetGameState(steps.Pop(), 0);
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

    static List<ushort> TestScan(int width, TopLeftTodo topLeftTodo, HashSet<ushort> ways, SokowahnField view, bool debug = true)
    {
      var state = topLeftTodo.state;
      view.SetGameState(state);

      if (debug)
      {
        Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - 2));
        Console.WriteLine(new string(' ', Console.WindowWidth - 1));

        Console.SetCursorPosition(0, 1);
        Console.WriteLine(view.ToString());
        Console.WriteLine();
      }

      var result = ScanBestTopLeftWay(state[0], width, ways, new HashSet<ushort>(state.Skip(1)));
      var resultFiltered = result.Where(f => !topLeftTodo.known.Contains(f)).ToList();
      if (result.Last() != resultFiltered.LastOrDefault()) resultFiltered.Add(result.Last());

      if (debug)
      {
        string line = state[0] + " - " + string.Join(", ", result.Skip(1));
        Console.WriteLine(line);
        Console.WriteLine();
        //    Console.ReadLine();
      }

      return resultFiltered;
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
      /// Spielstatus, welches noch geprüft werden muss
      /// </summary>
      public ushort[] state;
      /// <summary>
      /// Felder, welche bereits geprüft wurden (inkl. blocker)
      /// </summary>
      public HashSet<ushort> known;
    }
    #endregion

    #region # static ushort[] AppendBoxes(ushort[] boxes, ushort newBox) // fügt eine Box zum neuen Array hinzu (sortiert)
    /// <summary>
    /// fügt eine Box zum Spielstatus hinzu (sortiert)
    /// </summary>
    /// <param name="state">bisheriger Spielstatus (sortiert)</param>
    /// <param name="newBox">Kiste, welche hinzugefügt werden soll</param>
    /// <returns>neuer Spielstatus</returns>
    static ushort[] AppendBoxes(ushort[] state, ushort newBox)
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
      var view = new SokowahnField(field);
      int maxBoxes = field.boxesCount;

      int width = field.width;
      var ways = FilterWays((ushort)field.PlayerPos, width, new HashSet<ushort>(Enumerable.Range(0, field.fieldData.Length).Select(f => (ushort)f)), new HashSet<ushort>(field.fieldData.Select((c, i) => new { c, i = (ushort)i }).Where(x => x.c == '#').Select(x => x.i)));

      var todo = new Queue<TopLeftTodo>();
      var map = new List<int> { ways.Count };

      foreach (var f in ways.OrderBy(x => x))
      {
        map.Add(0); // Index Platzhalter
        todo.Enqueue(new TopLeftTodo { mapIndex = map.Count - 1, state = new[] { f }, known = new HashSet<ushort>() });
      }

      int tick = 0;
      while (todo.Count > 0)
      {
        int t = Environment.TickCount;
        if (t > tick + 50)
        {
          Console.Title = "remain: " + todo.Count.ToString("N0") + " / " + map.Count.ToString("N0") + " (" + (Process.GetCurrentProcess().WorkingSet64 / 1048576.0).ToString("N1") + " MB)";
          tick = t;
        }

        var next = todo.Dequeue();
        var result = TestScan(width, next, ways, view, t == tick);

        map[next.mapIndex] = map.Count;
        map.Add(result.Count - 1);
        for (int r = 1; r < result.Count; r++)
        {
          map.Add(result[r]);
          next.known.Add(result[r]);
          map.Add(0);  // Index Platzhalter
          if (next.state.Length <= maxBoxes)
          {
            var newState = AppendBoxes(next.state, result[r]);
            newState[0] = result[r - 1];
            todo.Enqueue(new TopLeftTodo { mapIndex = map.Count - 1, state = newState, known = new HashSet<ushort>(next.known) });
          }
        }
      }

      Console.WriteLine("Hash: " + map.Count.ToString("N0"));
    }

    static void Main()
    {
      //MiniGame(new SokowahnField(TestLevel1));
      //MiniGame(new SokowahnField(TestLevel3));
      //MiniGame(new SokowahnField(TestLevel4));
      //MiniGame(new SokowahnField(TestLevel2));

      //MiniSolver(new SokowahnField(TestLevel));

      //MiniSolverHashBuilder(new SokowahnField(TestLevel3));
      //MiniSolverHashBuilder2(new SokowahnField(TestLevel3));
      ScanTopLeftFields(new SokowahnField(TestLevel4));

      #region # --- ScanTopLeftFields ---
      // --- base --------------------------------------
      // Level 1:       34.194 -    20 MB
      // Level 2:       19.747 -    20 MB
      // Level 3:      672.351 -   113 MB
      // Level 4:    4.090.845 -   317 MB
      // Level 5: * 15.245.914 - 5.093 MB (4.404.002)
      // Level 6: * 14.234.848 - 5.053 MB (5.085.224)
      // Level 7: * 15.718.791 - 5.017 MB (5.222.287)
      // Level 8: *  7.197.767 - 5.091 MB (3.474.847)
      #endregion


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
