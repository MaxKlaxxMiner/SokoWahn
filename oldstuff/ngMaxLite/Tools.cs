using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sokosolver
{
  public static class Tools
  {
    /// <summary>
    /// wandelt ein Byte-Array in eine Zeichenkette um (UTF8-kodiert)
    /// </summary>
    /// <param name="datenArray">Byte-Array mit den Daten</param>
    /// <returns>fertiger dekodierter String</returns>
    public static string ToUtf8String(this byte[] datenArray)
    {
      return Encoding.UTF8.GetString(datenArray);
    }

    /// <summary>
    /// gibt einen Datensatz aus einem Dictionary zurück oder sendet die Alternative zurück
    /// </summary>
    /// <typeparam name="TKey">Typ des Keys</typeparam>
    /// <typeparam name="TValue">Typ des Inhaltes</typeparam>
    /// <param name="dict">Dictionary in dem gesucht werden soll</param>
    /// <param name="suchKey">Schlüssel, welcher gesucht werden soll</param>
    /// <param name="alternative">alternativer Wert, wenn der Schlüssel nicht gefunden wurde</param>
    /// <returns>Inhalt des gefunden Schlüssels oder alternativer Wert</returns>
    public static TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey suchKey, TValue alternative)
    {
      TValue ergebnis;
      return dict.TryGetValue(suchKey, out ergebnis) ? ergebnis : alternative;
    }

    /// <summary>
    /// Parst die Zeichenkette als Zahl (bei einem Fehler wird der Alternativ-Wert zurück gegeben)
    /// </summary>
    /// <param name="wert">String, welcher geparst wird</param>
    /// <param name="alternative">alternativer Wert, falls beim Parsen ein Fehler auftritt</param>
    /// <returns>entsprechend geparster Wert</returns>
    public static int TryParse(this string wert, int alternative)
    {
      int ergebnis;
      return int.TryParse(wert, out ergebnis) ? ergebnis : alternative;
    }

    /// <summary>
    /// Parst die Zeichenkette als Zahl (bei einem Fehler wird der Alternativ-Wert zurück gegeben)
    /// </summary>
    /// <param name="wert">String, welcher geparst wird</param>
    /// <param name="alternative">alternativer Wert, falls beim Parsen ein Fehler auftritt</param>
    /// <returns>entsprechend geparster Wert</returns>
    public static double TryParse(this string wert, double alternative)
    {
      double ergebnis;
      return double.TryParse((wert ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out ergebnis) ? ergebnis : alternative;
    }

    #region # public static byte[] Download(string downloadURL, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, params Cookie[] kekse) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <param name="userAgent">gibt den Namen des Browsers bzw. den Name vom Bot an (Standard = null)</param>
    /// <param name="nutzerName">Nutzername für Authentifizierung (Standard = null)</param>
    /// <param name="passwort">Passwort für Authentifizierung (Standard = null)</param>
    /// <param name="zusatzDaten">Zusätzliche Daten, welche per Post verschickt werden sollen (Standard = null)</param>
    /// <param name="zusatzDatenTyp">Mime-Typ der Zusatzdaten (Standard = null)</param>
    /// <param name="timeOut">gibt den Timeout in Millisekunden an (Standard: 10000)</param>
    /// <param name="headers">zusätzliche Headers, welche gesendet werden sollen (Standard = null)</param>
    /// <param name="httpMethode">gibt fest die HTTP-Methode an (z.B. "GET", "POST", "PUT", "UPDATE" usw.)</param>
    /// <param name="kekse">mit zu sendene Cookies (Standard = null)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, string[] headers, string httpMethode, params Cookie[] kekse)
    {

      // WICHTIG !!! Wenn diese Methode sehr langsam ist, dann Internet Explorer öffnen und unter Optionen "automatische Suche nach Proxy Einstellungen" deaktivieren

      try
      {
        var webRequest = (HttpWebRequest)WebRequest.Create(downloadUrl);
        webRequest.Timeout = timeOut;
        if ((httpMethode ?? "") != "") webRequest.Method = httpMethode.ToUpper(); else webRequest.Method = "GET";
        if (!string.IsNullOrEmpty(nutzerName) && !string.IsNullOrEmpty(passwort))
        {
          webRequest.Credentials = new NetworkCredential(nutzerName, passwort);
        }
        if (simulateBrowser)
        {
          webRequest.Accept = "*/*";
          webRequest.Headers.Add("Accept-Language", "de");
          webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
          webRequest.Headers.Add("UA-CPU", "x86");
          webRequest.UserAgent = userAgent ?? "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0)";
        }
        else
        {
          webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
          if (userAgent != null) webRequest.UserAgent = userAgent;
        }

        if (headers != null && headers.Length > 0)
        {
          foreach (string satz in headers) webRequest.Headers.Add(satz);
        }

        if (kekse != null && kekse.Length > 0)
        {
          string domain = downloadUrl;
          domain = domain.Substring(0, domain.IndexOf('/', 8));
          webRequest.CookieContainer = new CookieContainer();
          foreach (var keks in kekse)
          {
            webRequest.CookieContainer.Add(new Uri(domain), keks);
          }
        }

        if (zusatzDaten != null && zusatzDatenTyp != null && zusatzDatenTyp != "")
        {
          if ((httpMethode ?? "") != "") webRequest.Method = httpMethode.ToUpper(); else webRequest.Method = "POST";
          webRequest.ContentType = zusatzDatenTyp;
          webRequest.ContentLength = zusatzDaten.Length;
          var postStream = webRequest.GetRequestStream();
          //     int pos = 0;
          //     if (status == null || zusatzDaten.Length == 0)
          {
            postStream.Write(zusatzDaten, 0, zusatzDaten.Length);
          }
          //else
          //{
          // string merk = status.Text;
          // while (pos < zusatzDaten.Length)
          // {
          //  status.Text = merk + " " + (100.0 / (double)zusatzDaten.Length * (double)pos).ToString("0.0") + " %";
          //  status.Update();
          //  postStream.Write(zusatzDaten, pos, Math.Min(zusatzDaten.Length - pos, 4096));
          //  pos += 4096;
          // }
          // status.Text = merk;
          // status.Update();
          //}
          postStream.Close();
        }

        var webResponse = (HttpWebResponse)webRequest.GetResponse();
        var downloadStream = webResponse.GetResponseStream();
        if (webResponse.ContentEncoding != null && webResponse.ContentEncoding == "gzip") downloadStream = new GZipStream(downloadStream, CompressionMode.Decompress);
        var ausgabe = new byte[65536];
        int ausgabePos = 0;
        int lese;
        while ((lese = downloadStream.Read(ausgabe, ausgabePos, ausgabe.Length - ausgabePos)) > 0)
        {
          ausgabePos += lese;
          if (ausgabePos == ausgabe.Length) Array.Resize(ref ausgabe, ausgabePos * 2);
        }
        downloadStream.Close();
        Array.Resize(ref ausgabe, ausgabePos);
        return ausgabe;
      }
      catch // (Exception exc)
      {
        return null;
      }
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, params Cookie[] kekse) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <param name="userAgent">gibt den Namen des Browsers bzw. den Name vom Bot an (Standard = null)</param>
    /// <param name="nutzerName">Nutzername für Authentifizierung (Standard = null)</param>
    /// <param name="passwort">Passwort für Authentifizierung (Standard = null)</param>
    /// <param name="zusatzDaten">Zusätzliche Daten, welche per Post verschickt werden sollen (Standard = null)</param>
    /// <param name="zusatzDatenTyp">Mime-Typ der Zusatzdaten (Standard = null)</param>
    /// <param name="timeOut">gibt den Timeout in Millisekunden an (Standard: 10000)</param>
    /// <param name="headers">zusätzliche Headers, welche gesendet werden sollen (Standard = null)</param>
    /// <param name="kekse">mit zu sendene Cookies (Standard = null)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, string[] headers, params Cookie[] kekse)
    {
      return Download(downloadUrl, simulateBrowser, userAgent, nutzerName, passwort, zusatzDaten, zusatzDatenTyp, timeOut, headers, null, kekse);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, params Cookie[] kekse) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <param name="userAgent">gibt den Namen des Browsers bzw. den Name vom Bot an (Standard = null)</param>
    /// <param name="nutzerName">Nutzername für Authentifizierung (Standard = null)</param>
    /// <param name="passwort">Passwort für Authentifizierung (Standard = null)</param>
    /// <param name="zusatzDaten">Zusätzliche Daten, welche per Post verschickt werden sollen (Standard = null)</param>
    /// <param name="zusatzDatenTyp">Mime-Typ der Zusatzdaten (Standard = null)</param>
    /// <param name="timeOut">gibt den Timeout in Millisekunden an (Standard: 10000)</param>
    /// <param name="kekse">mit zu sendene Cookies (Standard = null)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser, string userAgent, string nutzerName, string passwort, byte[] zusatzDaten, string zusatzDatenTyp, int timeOut, params Cookie[] kekse)
    {
      return Download(downloadUrl, simulateBrowser, userAgent, nutzerName, passwort, zusatzDaten, zusatzDatenTyp, timeOut, null, kekse);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, bool simulateBrowser, string userAgent, int timeOut, params Cookie[] kekse) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <param name="userAgent">gibt den Namen des Browsers bzw. den Name vom Bot an (Standard = null)</param>
    /// <param name="kekse">mit zu sendene Cookies (Standard = null)</param>
    /// <param name="timeOut">gibt den Timeout in Millisekunden an (Standard: 10000)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser, string userAgent, int timeOut, params Cookie[] kekse)
    {
      return Download(downloadUrl, simulateBrowser, userAgent, null, null, null, null, timeOut, null, kekse);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, bool simulateBrowser, Cookie[] kekse) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <param name="kekse">mit zu sendene Cookies (Standard = null)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser, params Cookie[] kekse)
    {
      return Download(downloadUrl, simulateBrowser, null, 10000, kekse);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, bool simulateBrowser) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="simulateBrowser">gibt an, ob ein Browser simuliert werden soll (Standard = true)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, bool simulateBrowser)
    {
      return Download(downloadUrl, simulateBrowser, null, 10000, null);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL, string userAgent) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <param name="userAgent">gibt den Namen des Browsers bzw. den Name vom Bot an (Standard = null)</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl, string userAgent)
    {
      return Download(downloadUrl, true, userAgent, 10000, null);
    }
    #endregion
    #region # public static byte[] Download(string downloadURL) // lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// <summary>
    /// lädt eine Datei von einer bestimmten Adresse herrunter (gibt "null" zurück, wenn ein Fehler aufgetreten ist)
    /// </summary>
    /// <param name="downloadUrl">Download-Adresse</param>
    /// <returns>fertige geladene Datei</returns>
    public static byte[] Download(string downloadUrl)
    {
      return Download(downloadUrl, true, null, 10000, null);
    }
    #endregion

    /// <summary>
    /// wandelt das eigene Element in eine IEnumerable-Abfrage um, welches dieses Element nur einmal zurück gibt
    /// </summary>
    /// <typeparam name="T">Typ des Wertes</typeparam>
    /// <param name="singleValue">Wert, welcher zurück gegeben wird</param>
    /// <returns>Enumerable mit dem Wert</returns>
    public static IEnumerable<T> SelfEnumerable<T>(this T singleValue)
    {
      yield return singleValue;
    }

    /// <summary>
    /// gibt alle Varianten zurück, welche eine bestimmte Anzahl von Elementen in einem begrenzten Feld einnehmen können
    /// z.B.: (49, 6) = alle Lottozahlen von 6 aus 49 = 13.983.816 Varianten
    /// </summary>
    /// <param name="elementeAnzahl">Anzahl der Elemente, welche existieren</param>
    /// <param name="felderAnzahl">begrenzte Anzahl der Felder, wo sich die Elemente befinden können (muss kleinergleich "elementeAnzahl" sein)</param>
    /// <param name="arrayKopien">gibt an, ob jedes zurückgegebene Array als Kopie erstellt werden soll (langsamer und benötigt mehr Speicher, Inhalte dürfen dann jedoch geändert werden)</param>
    /// <returns>Enumerable aller berechneten Varianten</returns>
    public static IEnumerable<int[]> BerechneElementeVarianten(int elementeAnzahl, int felderAnzahl, bool arrayKopien)
    {
      int qz = elementeAnzahl - felderAnzahl;
      int vl1 = felderAnzahl - 1;

      var elemente = new int[felderAnzahl];

      int p = 0;

      if (arrayKopien)
      {
        for (; ; )
        {
          while (p < vl1) elemente[p + 1] = elemente[p++] + 1;

          yield return elemente.ToArray();

          while (elemente[p]++ == p + qz) if (--p < 0) yield break;
        }
      }
      for (; ; )
      {
        while (p < vl1) elemente[p + 1] = elemente[p++] + 1;

        yield return elemente;

        while (elemente[p]++ == p + qz) if (--p < 0) yield break;
      }
    }
  }
}
