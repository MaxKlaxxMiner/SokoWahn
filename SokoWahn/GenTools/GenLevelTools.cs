
#region # using *.*

using System;
using System.Linq;
using System.Text;

// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToLambdaExpressionWhenPossible

#endregion


namespace SokoWahn
{
  /// <summary>
  /// Klasse zum generieren von Quellcodes zum verarbeiten von Levels
  /// </summary>
  static class GenLevelTools
  {
    #region # public static void AddLevelComments(CsFile csFile, SokowahnField field, string headLine) // fügt Informationen über das Spielfeld als Kommentare im Quellcode hinzu
    /// <summary>
    /// fügt Informationen über das Spielfeld als Kommentare im Quellcode hinzu
    /// </summary>
    /// <param name="csFile">Cs-Datei, wo der Code hinzugefügt werden soll</param>
    /// <param name="field">Spielfeld, welches dargestellt werden soll</param>
    /// <param name="headLine">Überschrift, welche über dem Level verwendet werden soll</param>
    public static void AddLevelComments(CsFile csFile, SokowahnField field, string headLine)
    {
      int commentWidth = Math.Max(field.width + 2, 35);
      commentWidth = (commentWidth + 1) / 2 * 2 + field.width % 2;
      string levelId = field.GetLevelId();

      string emptyLine = " *" + new string(' ', commentWidth - 2) + "*";
      csFile.Write("/" + new string('*', commentWidth));
      csFile.Write(emptyLine);
      csFile.Write((" *  " + new string(' ', (commentWidth - 14 - headLine.Length) / 2) + "--- " + headLine + " ---").PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      csFile.Write(" " + new string('*', commentWidth));
      csFile.Write(emptyLine);
      csFile.Write((" *  Level-Hash: " + levelId.Remove(0, levelId.LastIndexOf('_') + 1)).PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      csFile.Write((" *  Size      : " + field.width + " x " + field.height + " (" + (field.width * field.height).ToString("N0") + ")").PadRight(commentWidth, ' ') + "*");
      csFile.Write((" *  Boxes     : " + field.boxesCount).PadRight(commentWidth, ' ') + "*");
      csFile.Write(emptyLine);
      string centerChars = new string(' ', (commentWidth - field.width - 2) / 2);
      csFile.Write(field.ToString().Replace("\r", "").Split('\n').Select(x => " *" + centerChars + x + centerChars + "*"));
      csFile.Write(emptyLine);
      csFile.Write(" " + new string('*', commentWidth) + "/");
    }
    #endregion

    #region # public static void AddLevelBasic(CsFile csFile, SokowahnField field) // fügt den Code für das Spielfeld inkl. deren Basis-Werte hinzu
    /// <summary>
    /// fügt den Code für das Spielfeld inkl. deren Basis-Werte hinzu
    /// </summary>
    /// <param name="csFile">Cs-Datei, wo der Code hinzugefügt werden soll</param>
    /// <param name="field">Spielfeld, welches dargestellt werden soll</param>
    public static void AddLevelBasics(CsFile csFile, SokowahnField field)
    {
      var fChars = new char[field.width * field.height];

      #region # // --- static readonly char[] FieldData = ... ---
      csFile.Write("const int FieldWidth = " + field.width + ";");
      csFile.Write("const int FieldHeight = " + field.height + ";");
      csFile.Write("const int FieldCount = FieldWidth * FieldHeight;");
      csFile.Write();
      csFile.Write("static readonly char[] FieldData =", f =>
      {
        var zeile = new StringBuilder();
        for (int y = 0; y < field.height; y++)
        {
          zeile.Clear();
          zeile.Append("/* " + (y * field.width).ToString("N0").PadLeft(((field.height - 1) * field.width).ToString("N0").Length) + " */ ");
          for (int x = 0; x < field.width; x++)
          {
            char c = field.fieldData[x + y * field.width];
            fChars[x + y * field.width] = c;
            if (c != '#') c = ' ';
            zeile.Append('\'').Append(c).Append("',");
          }
          if (y == field.height - 1) zeile.Remove(zeile.Length - 1, 1);
          f.Write(zeile.ToString());
        }
      });
      csFile.WriteV(";\r\n\r\n");
      #endregion

      #region # // --- static readonly ushort[] TargetPosis ---
      var targetFields = fChars.Select((c, i) => new { c, i }).Where(f => f.c == '.' || f.c == '*').Select(f => (ushort)f.i).ToArray();
      csFile.Write("static readonly ushort[] TargetPosis = { " + string.Join(", ", targetFields) + " };");
      csFile.Write();
      #endregion

      #region # // --- static readonly ushort[] BoxPosis ---
      csFile.Write("static readonly ushort[] BoxPosis = { " + string.Join(", ", targetFields.Select(x => fChars.Length - 1)) + " };");
      csFile.Write();
      #endregion

      #region # // --- TxtView ---
      csFile.Write("static string TxtViewP(int playerPos = -1) { return SokoTools.TxtView(FieldData, FieldWidth, TargetPosis, playerPos); }");
      csFile.Write("static string TxtView { get { return TxtViewP(); } }");
      csFile.Write();
      #endregion
    }
    #endregion

    #region # public static void AddBoxFunctions(CsFile csFile) // fügt Methoden hinzu um die Kisten auf dem Spielfeld zu beeinflussen
    /// <summary>
    /// fügt Methoden hinzu um die Kisten auf dem Spielfeld zu beeinflussen
    /// </summary>
    /// <param name="csFile">Cs-Datei, wo der Code hinzugefügt werden soll</param>
    public static void AddBoxFunctions(CsFile csFile)
    {
      // --- static ulong SetBoxes(ushort[] buf, int offset, int boxesCount) ---
      csFile.Write("static ulong SetBoxes(ushort[] buf, int offset, int boxesCount)", sb =>
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
      csFile.Write();

      // --- static void MoveBox(ushort oldPos, ushort newPos) ---
      csFile.Write("static void MoveBox(ushort oldPos, ushort newPos)", mb =>
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
      csFile.Write();
    }
    #endregion
  }
}
