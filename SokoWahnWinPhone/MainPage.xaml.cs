﻿
#region # using *.*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using SokoWahnCore;

// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBeInternal

#endregion

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=391641 dokumentiert.

namespace SokoWahnWinPhone
{
  /// <summary>
  /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
  /// </summary>
  public sealed partial class MainPage
  {
    public MainPage()
    {
      InitializeComponent();

      NavigationCacheMode = NavigationCacheMode.Required;
    }

    public const string Level3 = "    #####\n"
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
    /// extreme
    /// </summary>
    public const string Level8 = "####################\n"
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

    /// <summary>
    /// Wird aufgerufen, wenn diese Seite in einem Rahmen angezeigt werden soll.
    /// </summary>
    /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
    /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
      skinImg = await RtTools.ReadBitmapAsync("Assets\\skin-yasc.png");
      skinContext = skinImg.GetBitmapContext();

      InitGame(Level3);

      // TODO: Wenn Ihre Anwendung mehrere Seiten enthält, stellen Sie sicher, dass
      // die Hardware-Zurück-Taste behandelt wird, indem Sie das
      // Windows.Phone.UI.Input.HardwareButtons.BackPressed-Ereignis registrieren.
      // Wenn Sie den NavigationHelper verwenden, der bei einigen Vorlagen zur Verfügung steht,
      // wird dieses Ereignis für Sie behandelt.
    }

    WriteableBitmap viewImage;
    BitmapContext viewContext;

    WriteableBitmap skinImg;
    BitmapContext skinContext;

    const int Multi = 1;
    const int BoxPixelWidth = 50;
    const int BoxPixelHeight = BoxPixelWidth;

    enum BildElement
    {
      Frei = 0,
      FreiZiel = 7,
      Spieler = 1,
      KisteOffen = 2,
      KisteZiel = 9,
      WandEcken = 14,
      WandWaagerecht = 15,
      WandVoll = 16,
      WandSenkrecht = 21,
      WandSpitzen = 22
    };

    enum BildTeile
    {
      LinksOben = 1,
      RechtsOben = 2,
      LinksUnten = 4,
      RechtsUnten = 8,
      Links = LinksOben | LinksUnten,
      Rechts = RechtsOben | RechtsUnten,
      Oben = LinksOben | RechtsOben,
      Unten = LinksUnten | RechtsUnten,
      Alle = LinksOben | RechtsOben | LinksUnten | RechtsUnten
    }

    static void MaleTestbild(BitmapContext bmpTarget, BitmapContext bmpSource, int px, int py, BildElement bildElement, BildTeile bildTeile = BildTeile.Alle)
    {
      var target = new RectInt(px * BoxPixelWidth * Multi, py * BoxPixelHeight * Multi, BoxPixelWidth * Multi, BoxPixelHeight * Multi);
      int elY = (int)bildElement / 7;
      int elX = (int)bildElement - (elY * 7);
      var source = new RectInt(elX * BoxPixelWidth, elY * BoxPixelHeight, BoxPixelWidth, BoxPixelHeight);

      switch (bildTeile)
      {
        case BildTeile.Alle: break;
        case BildTeile.Links: target.w /= 2; source.w /= 2; break;
        case BildTeile.Rechts: target.w /= 2; source.w /= 2; target.x += BoxPixelWidth * Multi / 2; source.x += BoxPixelWidth / 2; break;
        case BildTeile.Oben: target.h /= 2; source.h /= 2; break;
        case BildTeile.Unten: target.h /= 2; source.h /= 2; target.y += BoxPixelHeight * Multi / 2; source.y += BoxPixelHeight / 2; break;
        case BildTeile.LinksOben: target.w /= 2; target.h /= 2; source.w /= 2; source.h /= 2; break;
        case BildTeile.RechtsOben: target.w /= 2; target.h /= 2; source.w /= 2; source.h /= 2; target.x += BoxPixelWidth * Multi / 2; source.x += BoxPixelWidth / 2; break;
        case BildTeile.LinksUnten: target.w /= 2; target.h /= 2; source.w /= 2; source.h /= 2; target.y += BoxPixelHeight * Multi / 2; source.y += BoxPixelHeight / 2; break;
        case BildTeile.RechtsUnten: target.w /= 2; target.h /= 2; source.w /= 2; source.h /= 2; target.x += BoxPixelWidth * Multi / 2; source.x += BoxPixelWidth / 2; target.y += BoxPixelHeight * Multi / 2; source.y += BoxPixelHeight / 2; break;
        default: throw new Exception("kombi unbekannt: " + (int)bildTeile);
      }

      if (bildElement == BildElement.Frei)
      {
        bmpTarget.FillRectangle(target.x, target.y, target.x + target.w, target.y + target.h, 0x001122);
      }

      bmpTarget.Blit(target, bmpSource, source, Colors.White, BlendMode.Alpha);
    }

    static bool GetF(char[] felder, int x, int y, int breite)
    {
      if (x < 0 || y < 0 || x >= breite || y * breite >= felder.Length) return true;
      return felder[x + y * breite] != '#';
    }

    SokowahnField playField;

    SokowahnField drawField;
    void UpdateScreen(SokowahnField field)
    {
      if (playField.width != drawField.width || playField.height != drawField.height)
      {
        throw new NotImplementedException("Resize Screen");
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
            case ' ': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); break;
            case '.': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.FreiZiel); break;
            case '$': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.KisteOffen); break;
            case '*': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.KisteZiel); break;
            case '@': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.Spieler); break;
            case '+': MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei); MaleTestbild(viewContext, skinContext, x, y, BildElement.FreiZiel); MaleTestbild(viewContext, skinContext, x, y, BildElement.Spieler); break;
            case '#':
            {
              MaleTestbild(viewContext, skinContext, x, y, BildElement.Frei);

              var lo = BildElement.WandVoll;
              var ro = BildElement.WandVoll;
              var lu = BildElement.WandVoll;
              var ru = BildElement.WandVoll;

              if (GetF(f, x - 1, y, w) && GetF(f, x, y - 1, w)) lo = BildElement.WandSpitzen;
              if (GetF(f, x - 1, y, w) && !GetF(f, x, y - 1, w)) lo = BildElement.WandSenkrecht;
              if (!GetF(f, x - 1, y, w) && GetF(f, x, y - 1, w)) lo = BildElement.WandWaagerecht;
              if (!GetF(f, x - 1, y, w) && !GetF(f, x, y - 1, w)) lo = GetF(f, x - 1, y - 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

              if (GetF(f, x + 1, y, w) && GetF(f, x, y - 1, w)) ro = BildElement.WandSpitzen;
              if (GetF(f, x + 1, y, w) && !GetF(f, x, y - 1, w)) ro = BildElement.WandSenkrecht;
              if (!GetF(f, x + 1, y, w) && GetF(f, x, y - 1, w)) ro = BildElement.WandWaagerecht;
              if (!GetF(f, x + 1, y, w) && !GetF(f, x, y - 1, w)) ro = GetF(f, x + 1, y - 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

              if (GetF(f, x - 1, y, w) && GetF(f, x, y + 1, w)) lu = BildElement.WandSpitzen;
              if (GetF(f, x - 1, y, w) && !GetF(f, x, y + 1, w)) lu = BildElement.WandSenkrecht;
              if (!GetF(f, x - 1, y, w) && GetF(f, x, y + 1, w)) lu = BildElement.WandWaagerecht;
              if (!GetF(f, x - 1, y, w) && !GetF(f, x, y + 1, w)) lu = GetF(f, x - 1, y + 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

              if (GetF(f, x + 1, y, w) && GetF(f, x, y + 1, w)) ru = BildElement.WandSpitzen;
              if (GetF(f, x + 1, y, w) && !GetF(f, x, y + 1, w)) ru = BildElement.WandSenkrecht;
              if (!GetF(f, x + 1, y, w) && GetF(f, x, y + 1, w)) ru = BildElement.WandWaagerecht;
              if (!GetF(f, x + 1, y, w) && !GetF(f, x, y + 1, w)) ru = GetF(f, x + 1, y + 1, w) ? BildElement.WandEcken : BildElement.WandVoll;

              MaleTestbild(viewContext, skinContext, x, y, lo, BildTeile.LinksOben);
              MaleTestbild(viewContext, skinContext, x, y, ro, BildTeile.RechtsOben);
              MaleTestbild(viewContext, skinContext, x, y, lu, BildTeile.LinksUnten);
              MaleTestbild(viewContext, skinContext, x, y, ru, BildTeile.RechtsUnten);
            } break;
            default: throw new Exception("unknown Char: '" + c + "'");
          }
        }
      }

      if (minField.x != int.MaxValue)
      {
        maxField.x = maxField.x - minField.x + 1;
        maxField.y = maxField.y - minField.y + 1;
        viewContext.Present(minField.x * BoxPixelWidth, minField.y * BoxPixelHeight, maxField.x * BoxPixelWidth, maxField.y * BoxPixelHeight);
      }
    }

    void InitGame(string gameTxt)
    {
      playField = new SokowahnField(gameTxt);
      undoList.Clear();
      undoList.Push(playField.GetGameState());
      drawField = new SokowahnField(playField);
      for (int i = 0; i < drawField.fieldData.Length; i++) drawField.fieldData[i] = '-';

      int width = playField.width * BoxPixelWidth * Multi;
      int height = playField.height * BoxPixelHeight * Multi + 1;

      viewImage = new WriteableBitmap(width, height);
      viewContext = viewImage.GetBitmapContext();

      GameImage.Source = viewImage;

      UpdateScreen(playField);
    }

    readonly Stack<ushort[]> undoList = new Stack<ushort[]>();

    void UpdateGame()
    {
      undoList.Push(playField.GetGameState());
      UpdateScreen(playField);
      if (playField.boxesRemain == 0) Application.Current.Exit();
    }

    private void ButtonLeft_Click(object sender, RoutedEventArgs e)
    {
      if (playField.MoveLeft()) UpdateGame();
    }

    private void ButtonRight_Click(object sender, RoutedEventArgs e)
    {
      if (playField.MoveRight()) UpdateGame();
    }

    private void ButtonUp_Click(object sender, RoutedEventArgs e)
    {
      if (playField.MoveUp()) UpdateGame();
    }

    private void ButtonDown_Click(object sender, RoutedEventArgs e)
    {
      if (playField.MoveDown()) UpdateGame();
    }

    private void ButtonUndo_Click(object sender, RoutedEventArgs e)
    {
      if (undoList.Count > 1)
      {
        undoList.Pop();
        playField.SetGameState(undoList.Peek());
        UpdateScreen(playField);
      }
    }

  }
}
