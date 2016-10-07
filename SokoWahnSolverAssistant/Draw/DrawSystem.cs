﻿
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnCore;

#endregion

namespace SokoWahnSolverAssistent.Draw
{
  /// <summary>
  /// Klasse zum zeichnen eines Sokowahn-Spielfeldes
  /// </summary>
  public sealed class DrawSystem
  {
    /// <summary>
    /// Spielfeld, welches aktuell sichtbar ist
    /// </summary>
    SokowahnField drawField = new SokowahnField("#####\n#@$.#\n#####"); // minimum Dummy-Feld

    /// <summary>
    /// zeichnet ein bestimmtes Spielfeld
    /// </summary>
    /// <param name="field">Spielfeld, welches gezeichnet werden soll</param>
    public void DrawField(SokowahnField field)
    {
      if (field.width != drawField.width || field.height != drawField.height)
      {
        drawField = new SokowahnField(field);
        Array.Clear(field.fieldData, 0, field.fieldData.Length);
      }

      var drawData = drawField.fieldData;
      var f = field.fieldData;
      int w = field.width;

      var minField = new PointInt(int.MaxValue, int.MaxValue);
      var maxField = new PointInt(int.MinValue, int.MinValue);

      int tickLimit = Environment.TickCount + 1000;
      for (int y = 0; y < field.height; y++)
      {
        if (Environment.TickCount > tickLimit) return;
        for (int x = 0; x < w; x++)
        {
          char c = f[x + y * w];
          if (drawData[x + y * w] == c) continue;

          if (x < minField.x) minField.x = x;
          if (y < minField.y) minField.y = y;
          if (x > maxField.x) maxField.x = x;
          if (y > maxField.y) maxField.y = y;

          drawData[x + y * w] = c;
          switch (c)
          {
          //  case ' ': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); break;
          //  case '.': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.FreiZiel); break;
          //  case '$': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.KisteOffen); break;
          //  case '*': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.KisteZiel); break;
          //  case '@': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.Spieler); break;
          //  case '+': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.FreiZiel); MaleTestbild(viewContext, skinContext, x, y, BildElement.Spieler); break;
          //  case '#':
          //  {
          //    MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei);

          //    var lo = BildElement.WandVoll;
          //    var ro = BildElement.WandVoll;
          //    var lu = BildElement.WandVoll;
          //    var ru = BildElement.WandVoll;

          //    if (GetF(f, x - 1, y, w) && GetF(f, x, y - 1, w)) lo = BildElement.WandSpitzen;
          //    if (GetF(f, x - 1, y, w) && !GetF(f, x, y - 1, w)) lo = BildElement.WandSenkrecht;
          //    if (!GetF(f, x - 1, y, w) && GetF(f, x, y - 1, w)) lo = BildElement.WandWaagerecht;
          //    if (!GetF(f, x - 1, y, w) && !GetF(f, x, y - 1, w)) lo = GetF(f, x - 1, y - 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

          //    if (GetF(f, x + 1, y, w) && GetF(f, x, y - 1, w)) ro = BildElement.WandSpitzen;
          //    if (GetF(f, x + 1, y, w) && !GetF(f, x, y - 1, w)) ro = BildElement.WandSenkrecht;
          //    if (!GetF(f, x + 1, y, w) && GetF(f, x, y - 1, w)) ro = BildElement.WandWaagerecht;
          //    if (!GetF(f, x + 1, y, w) && !GetF(f, x, y - 1, w)) ro = GetF(f, x + 1, y - 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

          //    if (GetF(f, x - 1, y, w) && GetF(f, x, y + 1, w)) lu = BildElement.WandSpitzen;
          //    if (GetF(f, x - 1, y, w) && !GetF(f, x, y + 1, w)) lu = BildElement.WandSenkrecht;
          //    if (!GetF(f, x - 1, y, w) && GetF(f, x, y + 1, w)) lu = BildElement.WandWaagerecht;
          //    if (!GetF(f, x - 1, y, w) && !GetF(f, x, y + 1, w)) lu = GetF(f, x - 1, y + 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

          //    if (GetF(f, x + 1, y, w) && GetF(f, x, y + 1, w)) ru = BildElement.WandSpitzen;
          //    if (GetF(f, x + 1, y, w) && !GetF(f, x, y + 1, w)) ru = BildElement.WandSenkrecht;
          //    if (!GetF(f, x + 1, y, w) && GetF(f, x, y + 1, w)) ru = BildElement.WandWaagerecht;
          //    if (!GetF(f, x + 1, y, w) && !GetF(f, x, y + 1, w)) ru = GetF(f, x + 1, y + 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

          //    MaleTestbild(viewContext, skinContext, x, y, lo, BildTeile.LinksOben);
          //    MaleTestbild(viewContext, skinContext, x, y, ro, BildTeile.RechtsOben);
          //    MaleTestbild(viewContext, skinContext, x, y, lu, BildTeile.LinksUnten);
          //    MaleTestbild(viewContext, skinContext, x, y, ru, BildTeile.RechtsUnten);
          //  } break;
            default: throw new Exception("unknown Char: '" + c + "'");
          }
        }
      }

      if (minField.x != int.MaxValue)
      {
        maxField.x = maxField.x - minField.x + 1;
        maxField.y = maxField.y - minField.y + 1;
        //viewContext.Present(minField.x * BoxPixelWidth, minField.y * BoxPixelHeight, maxField.x * BoxPixelWidth, maxField.y * BoxPixelHeight);
      }
    }
  }
}
