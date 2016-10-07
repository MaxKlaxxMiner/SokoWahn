
#region # using *.*

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

// ReSharper disable MemberCanBeInternal

#endregion

namespace SokoWahnWinPhone
{
  /// <summary>
  /// Klasse mit diversen Tools für WindowsRT-Systemen
  /// </summary>
  public static class RtTools
  {
    #region # public static async Task<byte[]> ReadAllBytesAsync(string fileName) // liest alle Bytes einer Datei aus (rückgabe null wenn nicht gefunden)
    /// <summary>
    /// liest alle Bytes einer Datei aus (rückgabe null wenn nicht gefunden)
    /// </summary>
    /// <param name="fileName">Datei, welche geladen werden soll</param>
    /// <returns>gelesene Daten als Byte-Array</returns>
    public static async Task<byte[]> ReadAllBytesAsync(string fileName)
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
          reader.Dispose();
          return fileContent;
        }
      }
      catch (Exception)
      {
        // ignored
      }
      return null;
    }
    #endregion

    #region # // --- WriteLocalAllBytes / ReadLocalAllBytes ---
    /// <summary>
    /// schreibt eine Datei in den lokalen Ordner
    /// </summary>
    /// <param name="fileName">Datei, welche geschrieben werden soll</param>
    /// <param name="bytes">Bytes, welche geschrieben werden sollen</param>
    public static async void WriteLocalAllBytesAsync(string fileName, byte[] bytes)
    {
      var folder = ApplicationData.Current.LocalFolder;
      var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
      await FileIO.WriteBytesAsync(file, bytes);
    }

    /// <summary>
    /// liest eine Datei aus den lokalen und gibt den Inhalt als Byte-Array zurück (oder "null" wenn nicht gefunden)
    /// </summary>
    /// <param name="fileName">Datei, welche gelesene werden soll</param>
    /// <returns>Bytes, welche ausgelesen wurden (oder "null" wenn nicht gefunden)</returns>
    public static async Task<byte[]> ReadLocalAllBytesAsync(string fileName)
    {
      try
      {
        var folder = ApplicationData.Current.LocalFolder;
        var file = await folder.GetFileAsync(fileName);
        if (file == null) return null;
        var buffer = await FileIO.ReadBufferAsync(file);
        var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(bytes);
        reader.Dispose();
        return bytes;
      }
      catch (Exception exc)
      {
        return null;
      }
    }
    #endregion

    #region # public static async Task<WriteableBitmap> ReadBitmapAsync(string fileName) // liest eine Bilddatei und gibt das ensprechande Bitmap-Objekt zurück
    /// <summary>
    /// liest eine Bilddatei und gibt das ensprechande Bitmap-Objekt zurück
    /// </summary>
    /// <param name="fileName">Name der Bilddateie, welche geladen werden soll</param>
    /// <returns>fertig geladenes Bild</returns>
    public static async Task<WriteableBitmap> ReadBitmapAsync(string fileName)
    {
      var data = await ReadAllBytesAsync(fileName);

      var size = GetBildGröße(data);

      var memStream = new MemoryStream();
      await memStream.WriteAsync(data, 0, data.Length);
      memStream.Position = 0;

      var result = new WriteableBitmap(size.w, size.h);

      result.SetSource(memStream.AsRandomAccessStream());

      return result;
    }
    #endregion

    #region # public static Point GetBildGröße(byte[] bildDaten) // gibt die Breite und Höhe eines Bildes zurück (funktioniert mit PNG, GIF, TIFF und JPEG)
    /// <summary>
    /// gibt die Breite und Höhe eines Bildes zurück (funktioniert mit PNG, GIF, TIFF und JPEG)
    /// </summary>
    /// <param name="bildDaten">Header des Bildes (mindestens die ersten 24 Bytes sind erforderlich, empfohlen: 65536 Bytes oder mehr)</param>
    /// <returns>Point mit Breite und Höhe des Bildes, -1 wenn ein Fehler aufgetreten ist</returns>
    public static SizeInt GetBildGröße(byte[] bildDaten)
    {
      try
      {
        if (bildDaten == null) throw new Exception(); // keine Bilddaten
        if (bildDaten.Length < 24) throw new Exception(); // zu wenig Bilddaten

        #region # // --- PNG-Bild ---
        if (bildDaten[0] == 0x89 && bildDaten[1] == 0x50 && bildDaten[2] == 0x4e && bildDaten[3] == 0x47 && bildDaten[4] == 0x0d && bildDaten[5] == 0x0a && bildDaten[6] == 0x1a && bildDaten[7] == 0x0a)
        {
          if (bildDaten[12] != 0x49 || bildDaten[13] != 0x48 || bildDaten[14] != 0x44 || bildDaten[15] != 0x52) throw new Exception();
          int breite = (bildDaten[16] << 24) | (bildDaten[17] << 16) | (bildDaten[18] << 8) | (bildDaten[19] << 0);
          int höhe = (bildDaten[20] << 24) | (bildDaten[21] << 16) | (bildDaten[22] << 8) | (bildDaten[23] << 0);
          if (breite < 1 || breite > 20000 || höhe < 1 || höhe > 20000) throw new Exception(); // unrealistische bzw. ungültige Größenangaben
          return new SizeInt(breite, höhe);
        }
        #endregion
        #region # // --- GIF-Bild ---
        if (bildDaten[0] == 0x47 && bildDaten[1] == 0x49 && bildDaten[2] == 0x46 && bildDaten[3] == 0x38 && (bildDaten[4] == 0x37 || bildDaten[4] == 0x39) && bildDaten[5] == 0x61)
        {
          int breite = (bildDaten[6] << 0) | (bildDaten[7] << 8);
          int höhe = (bildDaten[8] << 0) | (bildDaten[9] << 8);
          if (breite < 1 || breite > 20000 || höhe < 1 || höhe > 20000) throw new Exception(); // unrealistische bzw. ungültige Größenangaben
          return new SizeInt(breite, höhe);
        }
        #endregion
        #region # // --- TIFF-Bild ---
        if (bildDaten[0] == 0x49 && bildDaten[1] == 0x49 && bildDaten[2] == 0x2a && bildDaten[3] == 0x00)
        {
          int p = 4;
          int off = bildDaten[p++]; off += bildDaten[p++] << 8; off += bildDaten[p++] << 16; off += bildDaten[p++] << 24;
          p = off;
          int entrys = bildDaten[p++]; entrys += bildDaten[p++] << 8;
          int merkBreite = 0;
          int merkHöhe = 0;
          for (int e = 0; e < entrys; e++)
          {
            int ident = bildDaten[p++]; ident += bildDaten[p++] << 8;
            int typ = bildDaten[p++]; typ += bildDaten[p++] << 8;
            int werte = bildDaten[p++]; werte += bildDaten[p++] << 8; werte += bildDaten[p++] << 16; werte += bildDaten[p++] << 24;
            int offset = bildDaten[p++]; offset += bildDaten[p++] << 8; offset += bildDaten[p++] << 16; offset += bildDaten[p++] << 24;
            if (ident == 256) merkBreite = offset;
            if (ident == 257) merkHöhe = offset;
            if (merkBreite > 0 && merkHöhe > 0) return new SizeInt(merkBreite, merkHöhe);
          }
        }
        if (bildDaten[0] == 0x4d && bildDaten[1] == 0x4d && bildDaten[2] == 0x00 && bildDaten[3] == 0x2a)
        {
          int p = 4;
          int off = bildDaten[p++] << 24; off += bildDaten[p++] << 16; off += bildDaten[p++] << 8; off += bildDaten[p++];
          p = off;
          int entrys = bildDaten[p++] << 8; entrys += bildDaten[p++];
          int merkBreite = 0;
          int merkHöhe = 0;
          for (int e = 0; e < entrys; e++)
          {
            int ident = bildDaten[p++] << 8; ident += bildDaten[p++];
            int typ = bildDaten[p++] << 8; typ += bildDaten[p++];
            int werte = bildDaten[p++]; werte += bildDaten[p++] << 8; werte += bildDaten[p++] << 16; werte += bildDaten[p++] << 24;
            int offset = bildDaten[p++] << 8; offset += bildDaten[p++]; p++; p++;
            if (ident == 256) merkBreite = offset;
            if (ident == 257) merkHöhe = offset;
            if (merkBreite > 0 && merkHöhe > 0) return new SizeInt(merkBreite, merkHöhe);
          }
        }
        #endregion
        #region # // --- JPG-Bild ---
        if (bildDaten[0] == 0xff && bildDaten[1] == 0xd8)
        {
          int p = 2;

          while (bildDaten[p] == 0xff)
          {
            p += 2;
            if (p >= bildDaten.Length - 4) return new SizeInt(-1, -1);
            switch (bildDaten[p - 1])
            {
              case 0xc4:
              case 0xe0:
              case 0xe1:
              case 0xe2:
              case 0xe3:
              case 0xe4:
              case 0xe5:
              case 0xe6:
              case 0xe7:
              case 0xe8:
              case 0xe9:
              case 0xea:
              case 0xeb:
              case 0xec:
              case 0xed:
              case 0xee:
              case 0xef:
              case 0xdb:
              case 0xdd:
              case 0xfe: p += (bildDaten[p] << 8) | (bildDaten[p + 1] << 0); break; // alle überflüssigen Blöcke überspringen

              case 0xc0:
              {
                p += 2;
                if (bildDaten[p++] != 0x08) throw new Exception(); // Flags sind nicht richtig gesetzt
                int höhe = (bildDaten[p] << 8) | (bildDaten[p + 1] << 0); p += 2;
                int breite = (bildDaten[p] << 8) | (bildDaten[p + 1] << 0); p += 2;
                if (breite < 1 || breite > 20000 || höhe < 1 || höhe > 20000) throw new Exception(); // unrealistische bzw. ungültige Größenangaben
                return new SizeInt(breite, höhe);
              }
              case 0xc2: goto case 0xc0;
              default: throw new Exception();
            }
            if (p >= bildDaten.Length - 4) return new SizeInt(-1, -1);
          }
          throw new Exception(); // Ende oder keinen passenden JPEG-Block gefunden
        }
        #endregion
        #region # // --- BMP-Bild ---
        if (bildDaten[0] == 0x42 && bildDaten[1] == 0x4d && bildDaten[14] == 0x28)
        {
          int breite = BitConverter.ToInt32(bildDaten, 18);
          int höhe = Math.Abs(BitConverter.ToInt32(bildDaten, 22));
          if (breite < 30000 && höhe < 30000) return new SizeInt(breite, höhe);
        }
        #endregion

        throw new Exception(); // Bildtyp unbekannt
      }
      catch
      {
        return new SizeInt(-1, -1);
      }
    }
    #endregion
  }
}
