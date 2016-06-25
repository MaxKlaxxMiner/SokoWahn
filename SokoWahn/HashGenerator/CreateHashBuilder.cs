
#region # using *.*

using System;
using System.Linq;
using System.Text;
// ReSharper disable ConvertToLambdaExpressionWhenPossible

#endregion

namespace SokoWahn
{
  /// <summary>
  /// Klasse zum generieren eines eigenständigen HashBuilders
  /// </summary>
  static class CreateHashBuilder
  {
    /// <summary>
    /// erstellt ein vollständiges Hashbuilder-Projekt und kompiliert dieses
    /// </summary>
    /// <param name="field">Spielfeld, welches als Grundlage dient</param>
    /// <param name="solutionPath">Pfad zum Ordner, wo die Projektmappe erstellt werden soll</param>
    public static void CreateProject(SokowahnField field, string solutionPath)
    {
      var scanner = new SokowahnField(field);
      Console.WriteLine(scanner.ToString());
      Console.WriteLine();

      string levelId = scanner.GetLevelId();

      var solutionGuid = CsProject.NewGuid("S" + levelId);
      var projectGuid = CsProject.NewGuid("P" + levelId);

      string projectName = "Sokowahn_HashBuilder_" + levelId;

      var csFile = new CsFile();

      csFile.Write();
      csFile.Write();
      GenLevelTools.AddLevelComments(csFile, scanner, "Hash Builder Alpha");
      csFile.Write();
      csFile.Write();

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

      #region # // --- Program.cs ---
      csFile.Write("namespace " + projectName, ns =>
      {
        ns.Write("static unsafe class Program", cl =>
        {
          GenLevelTools.AddLevelBasics(cl, scanner);

          GenLevelTools.AddBoxFunctions(cl);

          #region # // --- static int ScanTopLeftPos(int startPos) ---
          cl.Write("static readonly int[] PlayerDirections = { -1, +1, -FieldWidth, +FieldWidth };");
          cl.Write();
          cl.Write("static readonly int[] ScanTmp = new int[FieldCount];");
          cl.Write();
          cl.Write("static int ScanTopLeftPosIntern(int* next, bool* scanned, char* fd)", sc =>
          {
            sc.Write("int bestPos = int.MaxValue;");
            sc.Write("int nextPos = 1;");
            sc.Write("while (nextPos > 0)", wh =>
            {
              wh.Write("int checkPos = next[--nextPos];");
              wh.Write("if (checkPos < bestPos) bestPos = checkPos;");
              wh.Write("scanned[checkPos] = true;");
              wh.Write("if (!scanned[checkPos - 1] && fd[checkPos - 1] == ' ') next[nextPos++] = checkPos - 1;");
              wh.Write("if (!scanned[checkPos + 1] && fd[checkPos + 1] == ' ') next[nextPos++] = checkPos + 1;");
              wh.Write("if (!scanned[checkPos - FieldWidth] && fd[checkPos - FieldWidth] == ' ') next[nextPos++] = checkPos - FieldWidth;");
              wh.Write("if (!scanned[checkPos + FieldWidth] && fd[checkPos + FieldWidth] == ' ') next[nextPos++] = checkPos + FieldWidth;");
            });
            sc.Write("return bestPos;");
          });
          cl.Write();
          cl.Write("static int ScanTopLeftPos(int startPos)", sc =>
          {
            sc.Write("fixed (int* next = ScanTmp) fixed (char* fd = FieldData)", f =>
            {
              f.Write("bool* scanned = stackalloc bool[FieldCount];");
              f.Write("*next = startPos;");
              f.Write("return ScanTopLeftPosIntern(next, scanned, fd);");
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
                sc.Write("var hash = new DictionaryFastCrc<ushort>();");
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

      csFile.SaveToFile(solutionPath + "Program.cs");
      #endregion

      var csSokoTools = GenStaticTools.GenSokoTools(projectName);
      csSokoTools.SaveToFile(solutionPath + "SokoTools.cs");

      var csDictFast = GenStaticTools.GenDictionaryFastCrc(projectName);
      csDictFast.SaveToFile(solutionPath + "DictionaryFastCrc.cs");

      var projectFile = CsProject.CreateCsProjectFile(projectGuid, projectName, new[] { "System" }, new[] { "Program.cs", "SokoTools.cs", "DictionaryFastCrc.cs" });
      projectFile.SaveToFile(solutionPath + projectName + ".csproj");

      var solutionFile = CsProject.CreateSolutionFile(solutionGuid, projectGuid, "Sokowahn", projectName + ".csproj");
      solutionFile.SaveToFile(solutionPath + projectName + ".sln");

      CsCompiler.Compile(solutionPath + projectName + ".sln");
    }
  }
}
