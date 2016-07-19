
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
// ReSharper disable UnusedMember.Local

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

    /// <summary>
    /// Wird aufgerufen, wenn diese Seite in einem Rahmen angezeigt werden soll.
    /// </summary>
    /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
    /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      // TODO: Seite vorbereiten, um sie hier anzuzeigen.

      // TODO: Wenn Ihre Anwendung mehrere Seiten enthält, stellen Sie sicher, dass
      // die Hardware-Zurück-Taste behandelt wird, indem Sie das
      // Windows.Phone.UI.Input.HardwareButtons.BackPressed-Ereignis registrieren.
      // Wenn Sie den NavigationHelper verwenden, der bei einigen Vorlagen zur Verfügung steht,
      // wird dieses Ereignis für Sie behandelt.
    }

    static async Task<byte[]> ReadAllBytesAsync(string fileName)
    {
      try
      {
        var storageFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);
        if (storageFile != null)
        {
          var buffer = await FileIO.ReadBufferAsync(storageFile);
          var reader = DataReader.FromBuffer(buffer);
          var fileContent = new byte[reader.UnconsumedBufferLength];
          reader.ReadBytes(fileContent);
          return fileContent;
        }
      }
      catch (Exception)
      {
        // ignored
      }
      return null;
    }

    readonly WriteableBitmap skinImg = new WriteableBitmap(350, 400);
    WriteableBitmap testBild;

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
      var img = new Image
      {
        Width = Content.RenderSize.Width,
        Height = Content.RenderSize.Height,
        VerticalAlignment = VerticalAlignment.Top,
        HorizontalAlignment = HorizontalAlignment.Left
      };

      var data = await ReadAllBytesAsync("Assets\\skin-yasc.png");

      var memStream = new MemoryStream();
      await memStream.WriteAsync(data, 0, data.Length);
      memStream.Position = 0;

      skinImg.SetSource(memStream.AsRandomAccessStream());

      var btn = (Button)sender;
      btn.Content = "lol: " + skinImg.PixelWidth;

      int boxWidth = 8;
      int boxHeight = 8;

      int width = boxWidth * BoxPixelWidth * Multi;
      int height = boxHeight * BoxPixelHeight * Multi + 1;

      testBild = new WriteableBitmap(width, height);
      img.Source = testBild;

      (Content as Grid).Children.Add(img);
    }

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

    bool GetF(char[] felder, int x, int y, int breite)
    {
      if (x < 0 || y < 0 || x >= breite || y * breite >= felder.Length) return true;
      return felder[x + y * breite] != '#';
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      string[] zeilen = {
                          "  ##### ",
                          "###   # ",
                          "# $ # ##",
                          "# #  . #",
                          "#    # #",
                          "## #   #",
                          " #@  ###",
                          " #####  "
                        };


      int br = zeilen.First().Length;
      int höhe = zeilen.Length;
      var felder = zeilen.SelectMany(x => x).ToArray();

      using (var dstC = testBild.GetBitmapContext())
      {
        using (var srcC = skinImg.GetBitmapContext())
        {
          for (int y = 0; y < höhe; y++)
          {
            for (int x = 0; x < br; x++)
            {
              switch (felder[x + y * br])
              {
                case ' ': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); break;
                case '.': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); MaleTestbild(dstC, srcC, x, y, BildElement.FreiZiel); break;
                case '$': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); MaleTestbild(dstC, srcC, x, y, BildElement.KisteOffen); break;
                case '*': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); MaleTestbild(dstC, srcC, x, y, BildElement.KisteZiel); break;
                case '@': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); MaleTestbild(dstC, srcC, x, y, BildElement.Spieler); break;
                case '+': MaleTestbild(dstC, srcC, x, y, BildElement.Frei); MaleTestbild(dstC, srcC, x, y, BildElement.FreiZiel); MaleTestbild(dstC, srcC, x, y, BildElement.Spieler); break;
                case '#':
                {
                  MaleTestbild(dstC, srcC, x, y, BildElement.Frei);

                  var lo = BildElement.WandVoll;
                  var ro = BildElement.WandVoll;
                  var lu = BildElement.WandVoll;
                  var ru = BildElement.WandVoll;

                  if (GetF(felder, x - 1, y, br) && GetF(felder, x, y - 1, br)) lo = BildElement.WandSpitzen;
                  if (GetF(felder, x - 1, y, br) && !GetF(felder, x, y - 1, br)) lo = BildElement.WandSenkrecht;
                  if (!GetF(felder, x - 1, y, br) && GetF(felder, x, y - 1, br)) lo = BildElement.WandWaagerecht;
                  if (!GetF(felder, x - 1, y, br) && !GetF(felder, x, y - 1, br)) lo = GetF(felder, x - 1, y - 1, br) ? BildElement.WandVoll : BildElement.WandEcken;

                  if (GetF(felder, x + 1, y, br) && GetF(felder, x, y - 1, br)) ro = BildElement.WandSpitzen;
                  if (GetF(felder, x + 1, y, br) && !GetF(felder, x, y - 1, br)) ro = BildElement.WandSenkrecht;
                  if (!GetF(felder, x + 1, y, br) && GetF(felder, x, y - 1, br)) ro = BildElement.WandWaagerecht;
                  if (!GetF(felder, x + 1, y, br) && !GetF(felder, x, y - 1, br)) ro = GetF(felder, x + 1, y - 1, br) ? BildElement.WandVoll : BildElement.WandEcken;

                  if (GetF(felder, x - 1, y, br) && GetF(felder, x, y + 1, br)) lu = BildElement.WandSpitzen;
                  if (GetF(felder, x - 1, y, br) && !GetF(felder, x, y + 1, br)) lu = BildElement.WandSenkrecht;
                  if (!GetF(felder, x - 1, y, br) && GetF(felder, x, y + 1, br)) lu = BildElement.WandWaagerecht;
                  if (!GetF(felder, x - 1, y, br) && !GetF(felder, x, y + 1, br)) lu = GetF(felder, x - 1, y + 1, br) ? BildElement.WandVoll : BildElement.WandEcken;

                  if (GetF(felder, x + 1, y, br) && GetF(felder, x, y + 1, br)) ru = BildElement.WandSpitzen;
                  if (GetF(felder, x + 1, y, br) && !GetF(felder, x, y + 1, br)) ru = BildElement.WandSenkrecht;
                  if (!GetF(felder, x + 1, y, br) && GetF(felder, x, y + 1, br)) ru = BildElement.WandWaagerecht;
                  if (!GetF(felder, x + 1, y, br) && !GetF(felder, x, y + 1, br)) ru = GetF(felder, x + 1, y + 1, br) ? BildElement.WandVoll : BildElement.WandEcken;

                  MaleTestbild(dstC, srcC, x, y, lo, BildTeile.LinksOben);
                  MaleTestbild(dstC, srcC, x, y, ro, BildTeile.RechtsOben);
                  MaleTestbild(dstC, srcC, x, y, lu, BildTeile.LinksUnten);
                  MaleTestbild(dstC, srcC, x, y, ru, BildTeile.RechtsUnten);
                } break;
                default: throw new Exception("zeichen unbekannt: '" + felder[x + y * br] + "'");
              }
            }
          }
        }
      }

      //var imgBitmapBuf = new byte[width * height * 4];
      //var rnd = new Random();
      //for (int i = 0; i < imgBitmapBuf.Length; i++) imgBitmapBuf[i] = (byte)rnd.Next(256);

      //using (var str = test.PixelBuffer.AsStream()) { str.Write(imgBitmapBuf, 0, imgBitmapBuf.Length); }
      testBild.Invalidate();
    }
  }
}
