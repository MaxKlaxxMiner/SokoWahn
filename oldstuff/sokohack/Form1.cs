#region # using *.*
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
#endregion

namespace Sokohack
{
 public partial class Form1 : Form
 {
  #region # public Form1()
  public Form1()
  {
   InitializeComponent();
  }
  #endregion

  #region # // --- FeldTypen zum erkennen der Felder ---
  Dictionary<long, int> feldTypen = new Dictionary<long, int>();

  void FeldTypenLaden()
  {
   if (!File.Exists(hauptOrdner + "feldtypen.txt")) return;
   feldTypen = new Dictionary<long, int>();
   string[] zeilen = File.ReadAllLines(hauptOrdner + "feldtypen.txt");
   var liste = from zeile in zeilen
               where zeile != ""
               select new 
               {
                summe = long.Parse(zeile.Split('\t')[0]),
                typ = int.Parse(zeile.Split('\t')[1])
               };
   foreach (var satz in liste) feldTypen.Add(satz.summe, satz.typ);
  }

  void FeldTypenSpeichern()
  {
   StringBuilder ausgabe = new StringBuilder();

   foreach (var satz in feldTypen) ausgabe.Append(satz.Key).Append('\t').Append(satz.Value).AppendLine();

   File.WriteAllText(hauptOrdner + "feldtypen.txt", ausgabe.ToString());
  }

  void FeldDazu(long summe, int typ)
  {
   feldTypen.Add(summe, typ);
   FeldTypenSpeichern();
  }

  int FeldTyp(long summe)
  {
   int ausgabe = -1;
   if (!feldTypen.TryGetValue(summe, out ausgabe)) return -1;
   return ausgabe;
  }
  #endregion

  #region # void ScanFelder(int ofsx, int ofsy) // scannt die Felder anhand eines Bildes des Spiels
  /// <summary>
  /// scannt die Felder anhand eines Bildes des Spiels
  /// </summary>
  /// <param name="ofsx">Offset-X</param>
  /// <param name="ofsy">Offset-Y</param>
  void ScanFelder(int ofsx, int ofsy)
  {
   try
   {
    Bitmap bild = null;
    try
    {
     bild = new Bitmap(Clipboard.GetImage());
     bild.Save("merk.png", ImageFormat.Png);
    }
    catch
    {
     bild = new Bitmap("merk.png");
    }
    Bitmap scanBild = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
    Graphics g = Graphics.FromImage(scanBild);

    textBox1.Text = "";
    button2.Enabled = false;

    List<int> feldSammler = new List<int>();
    feldData = null;

    feldHöhe = 0;
    for (int y = ofsy; y < bild.Height - 64; y += 32)
    {
     feldHöhe++;
     feldBreite = 0;
     for (int x = ofsx; x < bild.Width - 176; x += 32)
     {
      feldBreite++;
      g.DrawImage(bild, new Rectangle(0, 0, 32, 32), new Rectangle(x, y, 32, 32), GraphicsUnit.Pixel);
      BitmapData bdata = scanBild.LockBits(new Rectangle(0, 0, 32, 32), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

      int[] pixel = new int[32 * 32];
      Marshal.Copy(bdata.Scan0, pixel, 0, pixel.Length);

      long summe = 0;
      for (int i = 0; i < pixel.Length; i++) summe += (long)pixel[i] * (long)(i * 2 + 1);
      scanBild.UnlockBits(bdata);

      int feldTyp = FeldTyp(summe);

      if (feldTyp == 999)
      {
       if (ofsx < 35)
       {
        ScanFelder(ofsx + 16, ofsy);
       }
       else
       {
        if (ofsy < 60)
        {
         ScanFelder(ofsx, ofsy + 16);
        }
        else
        {
         ScanFelder(ofsx - 16, ofsy);
        }
       }
       return;
      }

      if (feldTyp < 0)
      {
       textBox1.Text = summe.ToString();
       Bitmap testBild = new Bitmap(32 * 3, 32 * 3, PixelFormat.Format32bppRgb);
       Graphics gr = Graphics.FromImage(testBild);
       gr.DrawImage(scanBild, 0, 0);
       gr.DrawImage(scanBild, 32, 0);
       gr.DrawImage(scanBild, 64, 0);
       gr.DrawImage(scanBild, 0, 32);
       gr.DrawImage(scanBild, 32, 32);
       gr.DrawImage(scanBild, 64, 32);
       gr.DrawImage(scanBild, 0, 64);
       gr.DrawImage(scanBild, 32, 64);
       gr.DrawImage(scanBild, 64, 64);
       pictureBox1.Image = testBild;
       button2.Enabled = true;
       Text = "0 = leer, 1 = Mauer, 2 = Würfel, 4 = Zielfeld, 6 = Würfel auf Zielfeld, 8 = Spieler, 12 = Spieler auf einem Zielfeld, 999 = falsche Justierung";
       throw new Exception();
      }

      feldSammler.Add(feldTyp);
     }
    }

    feldData = feldSammler.ToArray();
    feldAnzahl = feldBreite * feldHöhe;
   }
   catch { }
  }
  #endregion

  const string hauptOrdner = @"D:\User\Desktop\Prog\Zeugs\SpielZeugs\Sokohack\";
  //const string hauptOrdner = @"C:\Users\Administrator\Desktop\Tools\windiff\";

  const int hashMaxInit = 1024 * 1024 * 1024;
  //const int hashMaxInit = 2000000000;

  const int fehlerDataMax = 16777216;

  /// <summary>
  /// gibt die maximale Größe des Hashes in Bytes an
  /// </summary>
  int hashMax = hashMaxInit;
  int hashInfoMax = 1024;

  struct HashInfo
  {
   public bool verschoben;
   public int vorgänger;
   public HashInfo(int vorgänger, bool verschoben)
   {
    this.verschoben = verschoben;
    this.vorgänger = vorgänger;
   }
  }

  #region # // --- Globale Static Variablen ---
  /// <summary>
  /// merkt sich die Breite des Feldes
  /// </summary>
  static int feldBreite = 0;

  /// <summary>
  /// merkt sich die Höhe des Feldes
  /// </summary>
  static int feldHöhe = 0;

  /// <summary>
  /// merkt sich die Anzahl der Felder (feldBreite * feldHöhe)
  /// </summary>
  static int feldAnzahl = 0;

  /// <summary>
  /// merkt sich die Hash-Daten
  /// </summary>
  static byte[] hashData = new byte[hashMaxInit];

  HashInfo[] hashInfo = new HashInfo[65536];

  /// <summary>
  /// gibt die belegten Bytes im Hash an
  /// </summary>
  static int hashGro = 0;

  /// <summary>
  /// gibt die Größe eines Hash-Satzes an
  /// </summary>
  static int hashSatzGro = 0;

  /// <summary>
  /// merkt sich, ob auf den bestimmten Feldern eine Box liegen darf
  /// </summary>
  static bool[] erlaubteBoxFelder = null;

  /// <summary>
  /// Struktur, welche einen Bereich im Array fehlerData belegt
  /// </summary>
  struct FehlerInfo
  {
   /// <summary>
   /// merkt sich Position im Array fehlerData
   /// </summary>
   public int dataPos;
   /// <summary>
   /// merkt sich belegten Felder im Array fehlerData
   /// </summary>
   public int dataGro;
  }

  /*
   * --- Fehler-Datensatz (n = Anzahl der zusätzlich zu vergleichenden Boxen) ---
   * 
   * [0..n] Positionen der zusätzlich unerlaubten Boxen
   * [n]    gibt die Anzahl der nachfolgenden Gut-Positionen an, wo der Spieler stehen muss um die Situation noch zu retten zu können
   *        - 0: keine
   *        - kleiner als 0: es werden umgedreht nur die aussichtslosen Spielerpositionen angegeben
   * [..]   die einzelnen Spielerpositionen
   * 
   */

  /// <summary>
  /// merkt sich die genauen Daten für die Fehlerinformationen
  /// </summary>
  static short[] fehlerData = new short[fehlerDataMax];

  /// <summary>
  /// aktuelle Position im Array fehlerData
  /// </summary>
  static int fehlerDataPos = 0;

  /// <summary>
  /// gibt für jedes einzelne Feld an, welche Fehlerdaten pro Box bekannt sind (Bit kodiert, 0x1 = fehlerBox2, 0x2 = fehlerBox3 usw...)
  /// </summary>
  static int[] fehlerVorhanden = null;

  /// <summary>
  /// merkt sich für jedes einzelne Feld, welche jeweils zweite Boxpositionen nicht erlaubt sind
  /// </summary>
  static FehlerInfo[] fehlerBox2 = null;

  /// <summary>
  /// gleiche wie fehlerBox2, jedoch mit 3 Boxen
  /// </summary>
  static FehlerInfo[] fehlerBox3 = null;

  /// <summary>
  /// gleiche wie fehlerBox2, jedoch mit 4 Boxen
  /// </summary>
  static FehlerInfo[] fehlerBox4 = null;

  /// <summary>
  /// gleiche wie fehlerBox2, jedoch mit 5 Boxen
  /// </summary>
  static FehlerInfo[] fehlerBox5 = null;

  #endregion

  /// <summary>
  /// merkt sich die Felder
  /// 0 = leer, 1 = Mauer, 2 = Würfel, 4 = Zielfeld, 6 = Würfel auf Zielfeld, 8 = Spieler, 12 = Spieler auf einem Zielfeld, 999 = falsche Justierung
  /// </summary>
  static int[] feldData = null;

  #region # public struct HashFeld // merkt sich eine Verknüpfung zum Hash-Array
  /// <summary>
  /// merkt sich eine Verknüpfung zum Hash-Array
  /// </summary>
  public struct HashFeld
  {
   #region # public int hashPos; // merkt sich die Position im Hash-Array
   /// <summary>
   /// merkt sich die Position im Hash-Array
   /// </summary>
   public int hashPos;
   #endregion

   #region # public HashFeld(int hashPos) // Konstruktor
   /// <summary>
   /// Konstruktor
   /// </summary>
   /// <param name="hashPos">Hashposition im Hash-Array</param>
   public HashFeld(int hashPos)
   {
    this.hashPos = hashPos;
   }
   #endregion
   #region # public HashFeld(int hashPos, int[] feldData) // Konstruktor zum erstellen eines neuen Hash-Eintrages
   /// <summary>
   /// Konstruktor zum erstellen eines neuen Hash-Eintrages
   /// </summary>
   /// <param name="hashPos">Hashposition im Hash-Array</param>
   /// <param name="feldData">Daten des Feldes</param>
   public HashFeld(int hashPos, int[] feldData)
   {
    this = new HashFeld(hashPos, feldData, false);
   }
   #endregion
   #region # public HashFeld(int hashPos, int[] feldData, bool hack) // Konstruktor zum erstellen eines neuen Hash-Eintrages
   /// <summary>
   /// Konstruktor zum erstellen eines neuen Hash-Eintrages
   /// </summary>
   /// <param name="hashPos">Hashposition im Hash-Array</param>
   /// <param name="feldData">Daten des Feldes</param>
   /// <param name="hack">gibt an, das erkannte Fehler ignoriert werden sollen</param>
   public HashFeld(int hashPos, int[] feldData, bool hack)
   {
    this.hashPos = hashPos;
    int rest = 0;
    int ziel = 0;
    int spieler = -1;
    for (int i = 0; i < feldAnzahl; i++)
    {
     switch (hashData[hashPos + i] = (byte)feldData[i])
     {
      case 0: break;
      case 1: break;
      case 2: rest++; break;
      case 4: ziel++; break;
      case 6: break;
      case 8: if (spieler >= 0 && !hack) throw new Exception("Spieler ist mehrfach vorhanden"); spieler = i; break;
      case 12: ziel++; goto case 8;
      default: if (!hack) throw new Exception("Ungültiges Feld: " + feldData[i]); else break;
     }
    }
    if (rest != ziel && !hack) throw new Exception("Würfel != Ziele");
    if (spieler < 0 && !hack) throw new Exception("Spieler wurde nicht gefunden");
    this.SpielerPos = spieler;
    this.RestWürfel = rest;
   }
   #endregion

   #region # public ulong GetCrc64() // berechnet einen eindeutigen CRC-Schlüssel des Inhaltes
   /// <summary>
   /// berechnet einen eindeutigen CRC-Schlüssel des Inhaltes
   /// </summary>
   /// <returns>64-Bit CRC-Schlüssel</returns>
   public ulong GetCrc64()
   {
    ulong ergebnis = 0xcbf29ce484222325u; //init prime
    for (int i = 0; i < hashSatzGro; i++)
    {
     ergebnis = (ergebnis ^ hashData[hashPos + i]) * 0x100000001b3; //xor with new and mul with prime
    }
    return ergebnis;
   }
   #endregion

   #region # public int SpielerPos // gibt die absolute Position des Spielers zurück oder setzt diese
   /// <summary>
   /// gibt die absolute Position des Spielers zurück oder setzt diese
   /// </summary>
   public int SpielerPos 
   {
    get
    {
     return (int)hashData[hashPos + feldAnzahl] | (hashData[hashPos + feldAnzahl + 1] << 8);
    }
    set
    {
     hashData[hashPos + feldAnzahl] = (byte)value;
     hashData[hashPos + feldAnzahl + 1] = (byte)(value >> 8);
    }
   }
   #endregion
   #region # public int RestWürfel // gibt die Anzahl der restlichen Würfel zurück oder setzt diese
   /// <summary>
   /// gibt die Anzahl der restlichen Würfel zurück oder setzt diese
   /// </summary>
   public int RestWürfel
   {
    get
    {
     return (int)hashData[hashPos + feldAnzahl + 2];
    }
    set
    {
     hashData[hashPos + feldAnzahl + 2] = (byte)value;
    }
   }
   #endregion
   #region # public int SpielerX // gibt die X-Position des Spielers zurück
   /// <summary>
   /// gibt die X-Position des Spielers zurück
   /// </summary>
   public int SpielerX 
   {
    get
    { 
     return SpielerPos % feldBreite; 
    }
   }
   #endregion
   #region # public int SpielerY // gibt die Y-Position des Spielers zurück
   /// <summary>
   /// gibt die Y-Position des Spielers zurück
   /// </summary>
   public int SpielerY 
   {
    get 
    {
     return SpielerPos / feldBreite;
    }
   }
   #endregion

   #region # public bool KannLinks // gibt an, ob der Spieler nach links gehen kann
   /// <summary>
   /// gibt an, ob der Spieler nach links gehen kann
   /// </summary>
   public bool KannLinks
   {
    get
    {
     int checkPos = hashPos + SpielerPos - 1;
     switch (hashData[checkPos])
     {
      case 0: return true; // Feld ist frei
      case 2: // Box auf dem Feld
      {
       checkPos--;
       if ((hashData[checkPos] & 3) > 0 || !erlaubteBoxFelder[checkPos % hashSatzGro]) return false;
       hashData[checkPos + 1] ^= 2;
       bool killBox = FehlerCheckerHash(checkPos, checkPos + 1);
       hashData[checkPos + 1] ^= 2;
       return !killBox;
      }
      case 4: return true; // Feld ist frei (Zielfeld)
      case 6: goto case 2;
      default: return false;
     }
    }
   }
   #endregion
   #region # public bool KannRechts // gibt an, ob der Spieler nach rechts gehen kann
   /// <summary>
   /// gibt an, ob der Spieler nach rechts gehen kann
   /// </summary>
   public bool KannRechts
   {
    get
    {
     int checkPos = hashPos + SpielerPos + 1;
     switch (hashData[checkPos])
     {
      case 0: return true; // Feld ist frei
      case 2: // Box auf dem Feld
      {
       checkPos++;
       if ((hashData[checkPos] & 3) > 0 || !erlaubteBoxFelder[checkPos % hashSatzGro]) return false;
       hashData[checkPos - 1] ^= 2;
       bool killBox = FehlerCheckerHash(checkPos, checkPos - 1);
       hashData[checkPos - 1] ^= 2;
       return !killBox;
      }
      case 4: return true; // Feld ist frei (Zielfeld)
      case 6: goto case 2;
      default: return false;
     }
    }
   }
   #endregion
   #region # public bool KannOben // gibt an, ob der Spieler nach oben gehen kann
   /// <summary>
   /// gibt an, ob der Spieler nach oben gehen kann
   /// </summary>
   public bool KannOben
   {
    get
    {
     int checkPos = hashPos + SpielerPos - feldBreite;
     switch (hashData[checkPos])
     {
      case 0: return true; // Feld ist frei
      case 2: // Box auf dem Feld
      {
       checkPos -= feldBreite;
       if ((hashData[checkPos] & 3) > 0 || !erlaubteBoxFelder[checkPos % hashSatzGro]) return false;
       hashData[checkPos + feldBreite] ^= 2;
       bool killBox = FehlerCheckerHash(checkPos, checkPos + feldBreite);
       hashData[checkPos + feldBreite] ^= 2;
       return !killBox;
      }
      case 4: return true; // Feld ist frei (Zielfeld)
      case 6: goto case 2;
      default: return false;
     }
    }
   }
   #endregion
   #region # public bool KannUnten // gibt an, ob der Spieler nach unten gehen kann
   /// <summary>
   /// gibt an, ob der Spieler nach unten gehen kann
   /// </summary>
   public bool KannUnten
   {
    get
    {
     int checkPos = hashPos + SpielerPos + feldBreite;
     switch (hashData[checkPos])
     {
      case 0: return true; // Feld ist frei
      case 2: // Box auf dem Feld
      {
       checkPos += feldBreite;
       if ((hashData[checkPos] & 3) > 0 || !erlaubteBoxFelder[checkPos % hashSatzGro]) return false;
       hashData[checkPos - feldBreite] ^= 2;
       bool killBox = FehlerCheckerHash(checkPos, checkPos - feldBreite);
       hashData[checkPos - feldBreite] ^= 2;
       return !killBox;
      }
      case 4: return true; // Feld ist frei (Zielfeld)
      case 6: goto case 2;
      default: return false;
     }
    }
   }
   #endregion

   #region # public bool BewegeLinks(HashFeld zielHash) // bewegt den Spieler um eins nach links
   /// <summary>
   /// bewegt den Spieler um eins nach links
   /// </summary>
   /// <param name="zielHash">Ziel, in dem das neue Spielfeld erstellt wird</param>
   /// <returns>true, wenn auch eine Box verschoben wurde</returns>
   public bool BewegeLinks(HashFeld zielHash)
   {
    int ziel = zielHash.hashPos;
    int spieler = this.SpielerPos;
    for (int i = 0; i < hashSatzGro; i++) hashData[ziel + i] = hashData[hashPos + i];
    hashData[ziel + spieler] ^= 8; // Spielerfigur entfernen
    spieler--;
    zielHash.SpielerPos = spieler;
    hashData[ziel + spieler] ^= 8; // Spielerfigur setzen
    if ((hashData[ziel + spieler] & 2) > 0) // Box vorhanden?
    {
     hashData[ziel + spieler] ^= 2; // Box entfernen
     hashData[ziel + spieler - 1] ^= 2; // Box neu setzen
     switch ((hashData[ziel + spieler] & 0x4) | ((hashData[ziel + spieler - 1] << 4) & 0x40))
     {
      case 0x00: break;
      case 0x04: zielHash.RestWürfel++; break;
      case 0x40: zielHash.RestWürfel--; break;
      case 0x44: break;
     }
     return true;
    }
    return false;
   }
   #endregion
   #region # public bool BewegeRechts(HashFeld zielHash) // bewegt den Spieler um eins nach rechts
   /// <summary>
   /// bewegt den Spieler um eins nach rechts
   /// </summary>
   /// <param name="zielHash">Ziel, in dem das neue Spielfeld erstellt wird</param>
   /// <returns>true, wenn auch eine Box verschoben wurde</returns>
   public bool BewegeRechts(HashFeld zielHash)
   {
    int ziel = zielHash.hashPos;
    int spieler = this.SpielerPos;
    for (int i = 0; i < hashSatzGro; i++) hashData[ziel + i] = hashData[hashPos + i];
    hashData[ziel + spieler] ^= 8; // Spielerfigur entfernen
    spieler++;
    zielHash.SpielerPos = spieler;
    hashData[ziel + spieler] ^= 8; // Spielerfigur setzen
    if ((hashData[ziel + spieler] & 2) > 0) // Box vorhanden?
    {
     hashData[ziel + spieler] ^= 2; // Box entfernen
     hashData[ziel + spieler + 1] ^= 2; // Box neu setzen
     switch ((hashData[ziel + spieler] & 0x4) | ((hashData[ziel + spieler + 1] << 4) & 0x40))
     {
      case 0x00: break;
      case 0x04: zielHash.RestWürfel++; break;
      case 0x40: zielHash.RestWürfel--; break;
      case 0x44: break;
     }
     return true;
    }
    return false;
   }
   #endregion
   #region # public bool BewegeOben(HashFeld zielHash) // bewegt den Spieler um eins nach oben
   /// <summary>
   /// bewegt den Spieler um eins nach oben
   /// </summary>
   /// <param name="zielHash">Ziel, in dem das neue Spielfeld erstellt wird</param>
   /// <returns>true, wenn auch eine Box verschoben wurde</returns>
   public bool BewegeOben(HashFeld zielHash)
   {
    int ziel = zielHash.hashPos;
    int spieler = this.SpielerPos;
    for (int i = 0; i < hashSatzGro; i++) hashData[ziel + i] = hashData[hashPos + i];
    hashData[ziel + spieler] ^= 8; // Spielerfigur entfernen
    spieler -= feldBreite;
    zielHash.SpielerPos = spieler;
    hashData[ziel + spieler] ^= 8; // Spielerfigur setzen
    if ((hashData[ziel + spieler] & 2) > 0) // Box vorhanden?
    {
     hashData[ziel + spieler] ^= 2; // Box entfernen
     hashData[ziel + spieler - feldBreite] ^= 2; // Box neu setzen
     switch ((hashData[ziel + spieler] & 0x4) | ((hashData[ziel + spieler - feldBreite] << 4) & 0x40))
     {
      case 0x00: break;
      case 0x04: zielHash.RestWürfel++; break;
      case 0x40: zielHash.RestWürfel--; break;
      case 0x44: break;
     }
     return true;
    }
    return false;
   }
   #endregion
   #region # public bool BewegeUnten(HashFeld zielHash) // bewegt den Spieler um eins nach unten
   /// <summary>
   /// bewegt den Spieler um eins nach unten
   /// </summary>
   /// <param name="zielHash">Ziel, in dem das neue Spielfeld erstellt wird</param>
   /// <returns>true, wenn auch eine Box verschoben wurde</returns>
   public bool BewegeUnten(HashFeld zielHash)
   {
    int ziel = zielHash.hashPos;
    int spieler = this.SpielerPos;
    for (int i = 0; i < hashSatzGro; i++) hashData[ziel + i] = hashData[hashPos + i];
    hashData[ziel + spieler] ^= 8; // Spielerfigur entfernen
    spieler += feldBreite;
    zielHash.SpielerPos = spieler;
    hashData[ziel + spieler] ^= 8; // Spielerfigur setzen
    if ((hashData[ziel + spieler] & 2) > 0) // Box vorhanden?
    {
     hashData[ziel + spieler] ^= 2; // Box entfernen
     hashData[ziel + spieler + feldBreite] ^= 2; // Box neu setzen
     switch ((hashData[ziel + spieler] & 0x4) | ((hashData[ziel + spieler + feldBreite] << 4) & 0x40))
     {
      case 0x00: break;
      case 0x04: zielHash.RestWürfel++; break;
      case 0x40: zielHash.RestWürfel--; break;
      case 0x44: break;
     }
     return true;
    }
    return false;
   }
   #endregion
  }
  #endregion

  #region # void ErstelleErlaubteBoxFelder(int spielerPos) // erstellt die Map mit allen Feldern, wo eine Box stehen darf
  /// <summary>
  /// erstellt die Map mit allen Feldern, wo eine Box stehen darf
  /// </summary>
  /// <param name="spielerPos">Spieler Startposition</param>
  void ErstelleErlaubteBoxFelder(int spielerPos)
  {
   // pauschal alle Felder markieren, welche vom Spieler aus theoretisch erreichbar sind
   erlaubteBoxFelder = new bool[feldAnzahl];
   List<int> erlaubteBoxPositionen = new List<int>();
   erlaubteBoxPositionen.Add(spielerPos);
   var ofss = new[] { -1, +1, -feldBreite, +feldBreite };
   for (int i = 0; i < erlaubteBoxPositionen.Count; i++)
   {
    int checkPos = erlaubteBoxPositionen[i];
    foreach (int ofs in ofss) if ((feldData[checkPos + ofs] & 1) == 0 && erlaubteBoxPositionen.Where(p => p == checkPos + ofs).Count() == 0) erlaubteBoxPositionen.Add(checkPos + ofs);
   }
   erlaubteBoxPositionen.Select(p => erlaubteBoxFelder[p] = true).Count();

   // alle Eck-Fehler wieder entfernen
   foreach (int p in erlaubteBoxFelder.Select((f, p) => p).Where(p => erlaubteBoxFelder[p] && (feldData[p] & 4) == 0))
   {
    if (feldData[p - 1] == 1 && feldData[p - feldBreite] == 1) erlaubteBoxFelder[p] = false;
    if (feldData[p + 1] == 1 && feldData[p - feldBreite] == 1) erlaubteBoxFelder[p] = false;
    if (feldData[p - 1] == 1 && feldData[p + feldBreite] == 1) erlaubteBoxFelder[p] = false;
    if (feldData[p + 1] == 1 && feldData[p + feldBreite] == 1) erlaubteBoxFelder[p] = false;
   }
  }
  #endregion

  #region # void MapReset(bool leereHash) // leert die Map
  /// <summary>
  /// leert die Map
  /// </summary>
  /// <param name="leereHash">gibt an, ob auch der Hash geleert werden soll</param>
  void MapReset(bool leereHash)
  {
   for (int i = 0; i < feldAnzahl; i++) feldData[i] &= 0xff - 10;

   if (leereHash)
   {
    hashGro = 0;
    hashIndex.Clear();
   }
  }
  #endregion

  #region # void FehlerDataInit() // initialisiert die Fehlerdaten
  /// <summary>
  /// initialisiert die Fehlerdaten
  /// </summary>
  void FehlerDataInit()
  {
   fehlerVorhanden = new int[feldAnzahl];
   fehlerBox2 = new FehlerInfo[feldAnzahl];
   fehlerBox3 = new FehlerInfo[feldAnzahl];
   fehlerBox4 = new FehlerInfo[feldAnzahl];
   fehlerBox5 = new FehlerInfo[feldAnzahl];
   fehlerDataPos = 0;
  }
  #endregion

  #region # void FehlerDataUpdate(ref FehlerInfo info, List<short> dazu) // fügt neue Daten einem Datensatz hinzu
  /// <summary>
  /// fügt neue Daten einem Datensatz hinzu
  /// </summary>
  /// <param name="info">Datensatzknoten</param>
  /// <param name="dazu">neu zu speichernde Daten</param>
  void FehlerDataUpdate(ref FehlerInfo info, List<short> dazu)
  {
   if (info.dataGro == 0) // Feld enthält noch keine Daten?
   {
    info.dataPos = fehlerDataPos;
   }
   else // Daten schon vorhanden?
   {
    int altPos = info.dataPos;
    info.dataPos = fehlerDataPos;
    // alte Daten übertragen
    for (int i = 0; i < info.dataGro; i++) fehlerData[fehlerDataPos++] = fehlerData[altPos++];
   }
   foreach (short satz in dazu) fehlerData[fehlerDataPos++] = satz;
   info.dataGro += dazu.Count;
  }
  #endregion

  #region # void FehlerMapErstellen(int würfel) // System, welche die FehlerMapErstellen erstellt
  /// <summary>
  /// System, welche die FehlerMapErstellen erstellt
  /// </summary>
  /// <param name="würfel">Anzahl der zu testenden Würfel (muss immer weniger sein als die Ziel-Anzahl)</param>
  void FehlerMapErstellen(int würfel)
  {
   int[] kannFelder = Enumerable.Range(0, feldAnzahl).Where(pos => erlaubteBoxFelder[pos]).ToArray();
   var ofss = new[] { -1, +1, -feldBreite, +feldBreite };

   switch (würfel)
   {
    #region # case 1: // --- einzelne unerlaubte Boxen ermitteln ---
    case 1:
    {
     Dictionary<int, bool> spielerFertig = kannFelder.ToDictionary(p => p, p => false);

     foreach (int boxPos in kannFelder)
     {
      List<int> spielerGut = new List<int>();
      List<int> spielerSchlecht = new List<int>();

      foreach (int p in kannFelder) spielerFertig[p] = false;

      MapReset(true);
      feldData[boxPos] ^= 2;
      spielerFertig[boxPos] = true;

      foreach (int spielerPos in kannFelder.Where(pos => !spielerFertig[pos]))
      {
       #region # // --- Spieler setzen und Hash resetten ---
       feldData[spielerPos] ^= 8;
       HashFeld hashFeld = new HashFeld(0, feldData, true);
       hashIndex.Clear();
       hashGro = hashSatzGro;
       feldData[spielerPos] ^= 8;
       #endregion

       #region # // --- prüfen, ob Stellung gelöst werden kann ---
       bool find = false;

       for (int hashPos = 0; hashPos < hashGro; hashPos += hashSatzGro)
       {
        hashFeld = new HashFeld(hashPos);

        if (hashFeld.RestWürfel == 0) // machbare Lösung gefunden?
        {
         find = true;
         break;
        }

        if (hashGro > hashInfoMax * hashSatzGro)
        {
         hashInfoMax *= 2;
         Array.Resize(ref hashInfo, hashInfoMax + 65536);
        }

        if (hashFeld.KannLinks)
        {
         HashFeld neuHash = new HashFeld(hashGro);
         hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeLinks(neuHash));
         if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
        }

        if (hashFeld.KannRechts)
        {
         HashFeld neuHash = new HashFeld(hashGro);
         hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeRechts(neuHash));
         if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
        }

        if (hashFeld.KannOben)
        {
         HashFeld neuHash = new HashFeld(hashGro);
         hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeOben(neuHash));
         if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
        }

        if (hashFeld.KannUnten)
        {
         HashFeld neuHash = new HashFeld(hashGro);
         hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeUnten(neuHash));
         if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
        }
       }
       #endregion

       #region # // --- alle zusätzlich erreichbare Spielerpositionen ermitteln ---
       List<int> alleSpielerPos = new List<int>();
       alleSpielerPos.Add(spielerPos); spielerFertig[spielerPos] = true;
       for (int i = 0; i < alleSpielerPos.Count; i++)
       {
        int checkPos = alleSpielerPos[i];
        foreach (int ofs in ofss)
        {
         if ((feldData[checkPos + ofs] & 3) == 0 && !alleSpielerPos.Any(p => p == checkPos + ofs))
         {
          alleSpielerPos.Add(checkPos + ofs);
          if (erlaubteBoxFelder[checkPos + ofs]) spielerFertig[checkPos + ofs] = true;
         }
        }
       }
       #endregion

       if (find)
       {
        spielerGut.AddRange(alleSpielerPos);
       }
       else
       {
        spielerSchlecht.AddRange(alleSpielerPos);
        hashGro = 0;
        hashIndex.Clear();
       }

      }

      if (spielerGut.Count == 0) erlaubteBoxFelder[boxPos] = false;
     }

     FehlerDataInit();
    } break;
    #endregion

    #region # case 2: // --- unerlaubte Zweier-Kombinationen der Boxen ermitteln ---
    case 2:
    {
     Dictionary<int, bool> spielerFertig = kannFelder.ToDictionary(p => p, p => false);

     MapReset(true);

     for (int p1 = 0; p1 < kannFelder.Length; p1++)
     {
      int boxPos1 = kannFelder[p1];
      feldData[boxPos1] ^= 2;

      feldZeichner.Zeichne(pictureBox1, feldData); Application.DoEvents();

      for (int p2 = p1 + 1; p2 < kannFelder.Length; p2++)
      {
       int boxPos2 = kannFelder[p2];
       feldData[boxPos2] ^= 2;

       List<int> spielerGut = new List<int>();
       List<int> spielerSchlecht = new List<int>();

       foreach (int p in kannFelder) spielerFertig[p] = false;

       spielerFertig[boxPos1] = true;
       spielerFertig[boxPos2] = true;

       foreach (int spielerPos in kannFelder.Where(pos => !spielerFertig[pos]))
       {
        #region # // --- Spieler setzen und Hash resetten ---
        feldData[spielerPos] ^= 8;
        HashFeld hashFeld = new HashFeld(0, feldData, true);
        hashIndex.Clear();
        hashGro = hashSatzGro;
        feldData[spielerPos] ^= 8;
        #endregion

        #region # // --- prüfen, ob Stellung gelöst werden kann ---
        bool find = false;

        for (int hashPos = 0; hashPos < hashGro; hashPos += hashSatzGro)
        {
         hashFeld = new HashFeld(hashPos);

         if (hashFeld.RestWürfel == 0) // machbare Lösung gefunden?
         {
          find = true;
          break;
         }

         if (hashGro > hashInfoMax * hashSatzGro)
         {
          hashInfoMax *= 2;
          Array.Resize(ref hashInfo, hashInfoMax + 65536);
         }

         if (hashFeld.KannLinks)
         {
          HashFeld neuHash = new HashFeld(hashGro);
          hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeLinks(neuHash));
          if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
         }

         if (hashFeld.KannRechts)
         {
          HashFeld neuHash = new HashFeld(hashGro);
          hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeRechts(neuHash));
          if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
         }

         if (hashFeld.KannOben)
         {
          HashFeld neuHash = new HashFeld(hashGro);
          hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeOben(neuHash));
          if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
         }

         if (hashFeld.KannUnten)
         {
          HashFeld neuHash = new HashFeld(hashGro);
          hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeUnten(neuHash));
          if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
         }
        }
        #endregion

        #region # // --- alle zusätzlich erreichbare Spielerpositionen ermitteln ---
        List<int> alleSpielerPos = new List<int>();
        alleSpielerPos.Add(spielerPos); spielerFertig[spielerPos] = true;
        for (int i = 0; i < alleSpielerPos.Count; i++)
        {
         int checkPos = alleSpielerPos[i];
         foreach (int ofs in ofss)
         {
          if ((feldData[checkPos + ofs] & 3) == 0 && !alleSpielerPos.Any(p => p == checkPos + ofs))
          {
           alleSpielerPos.Add(checkPos + ofs);
           if (erlaubteBoxFelder[checkPos + ofs]) spielerFertig[checkPos + ofs] = true;
          }
         }
        }
        #endregion

        if (find)
        {
         spielerGut.AddRange(alleSpielerPos);
        }
        else
        {
         spielerSchlecht.AddRange(alleSpielerPos);
        }
       }

       if (spielerGut.Count == 0 || spielerSchlecht.Count > 0)
       {
        #region # // --- Box 1 -> Box 2 ---
        List<short> dazu = new List<short>();

        dazu.Add((short)boxPos2);
        if (spielerGut.Count < spielerSchlecht.Count) // nur die guten Felder merken?
        {
         dazu.Add((short)spielerGut.Count);
         dazu.AddRange(spielerGut.Select(pos => (short)pos));
        }
        else // nur die schlechten Felder merken (da weniger)
        {
         dazu.Add((short)-spielerSchlecht.Count);
         dazu.AddRange(spielerSchlecht.Select(pos => (short)pos));
        }

        FehlerDataUpdate(ref fehlerBox2[boxPos1], dazu);
        fehlerVorhanden[boxPos1] |= 1;
        #endregion

        #region # // --- Box 2 -> Box 1 ---
        dazu.Clear();

        dazu.Add((short)boxPos1);
        if (spielerGut.Count < spielerSchlecht.Count) // nur die guten Felder merken?
        {
         dazu.Add((short)spielerGut.Count);
         dazu.AddRange(spielerGut.Select(pos => (short)pos));
        }
        else // nur die schlechten Felder merken (da weniger)
        {
         dazu.Add((short)-spielerSchlecht.Count);
         dazu.AddRange(spielerSchlecht.Select(pos => (short)pos));
        }

        FehlerDataUpdate(ref fehlerBox2[boxPos2], dazu);
        fehlerVorhanden[boxPos2] |= 1;
        #endregion
       }

       feldData[boxPos2] ^= 2;
      }
      feldData[boxPos1] ^= 2;
     }
    } break;
    #endregion

    #region # case 3: // --- unerlaubte Dreier-Kombinationen der Boxen ermitteln ---
    case 3:
    {
     Dictionary<int, bool> spielerFertig = kannFelder.ToDictionary(p => p, p => false);

     MapReset(true);

     for (int p1 = 0; p1 < kannFelder.Length; p1++)
     {
      int boxPos1 = kannFelder[p1];
      int boxPos1x = boxPos1 % feldBreite;
      int boxPos1y = boxPos1 / feldBreite;
      feldData[boxPos1] ^= 2;

      for (int p2 = p1 + 1; p2 < kannFelder.Length; p2++)
      {
       int boxPos2 = kannFelder[p2];
       if (FehlerCheckDirekt(boxPos2)) continue;
       int boxPos2x = boxPos2 % feldBreite;
       int boxPos2y = boxPos2 / feldBreite;
       if (Math.Abs(boxPos2x - boxPos1x) > 2) continue;
       if (Math.Abs(boxPos2y - boxPos1y) > 2) continue;

       feldData[boxPos2] ^= 2;

       feldZeichner.Zeichne(pictureBox1, feldData); Application.DoEvents();

       for (int p3 = p2 + 1; p3 < kannFelder.Length; p3++)
       {
        int boxPos3 = kannFelder[p3];
        if (FehlerCheckDirekt(boxPos3)) continue;
        int boxPos3x = boxPos3 % feldBreite;
        int boxPos3y = boxPos3 / feldBreite;
        if (Math.Abs(boxPos3x - boxPos2x) > 2 && Math.Abs(boxPos3x - boxPos1x) > 2) continue;
        if (Math.Abs(boxPos3y - boxPos2y) > 2 && Math.Abs(boxPos3y - boxPos1y) > 2) continue;

        feldData[boxPos3] ^= 2;

        List<int> spielerGut = new List<int>();
        List<int> spielerSchlecht = new List<int>();

        foreach (int p in kannFelder) spielerFertig[p] = false;

        spielerFertig[boxPos1] = true;
        spielerFertig[boxPos2] = true;
        spielerFertig[boxPos3] = true;

        foreach (int spielerPos in kannFelder.Where(pos => !spielerFertig[pos]))
        {
         #region # // --- Spieler setzen und Hash resetten ---
         feldData[spielerPos] ^= 8;
         HashFeld hashFeld = new HashFeld(0, feldData, true);
         hashIndex.Clear();
         hashGro = hashSatzGro;
         feldData[spielerPos] ^= 8;
         #endregion

         #region # // --- prüfen, ob Stellung gelöst werden kann ---
         bool find = false;

         for (int hashPos = 0; hashPos < hashGro; hashPos += hashSatzGro)
         {
          hashFeld = new HashFeld(hashPos);

          if (hashFeld.RestWürfel == 0) // machbare Lösung gefunden?
          {
           find = true;
           break;
          }

          if (hashGro > hashInfoMax * hashSatzGro)
          {
           hashInfoMax *= 2;
           Array.Resize(ref hashInfo, hashInfoMax + 65536);
          }

          if (hashFeld.KannLinks)
          {
           HashFeld neuHash = new HashFeld(hashGro);
           hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeLinks(neuHash));
           if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
          }

          if (hashFeld.KannRechts)
          {
           HashFeld neuHash = new HashFeld(hashGro);
           hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeRechts(neuHash));
           if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
          }

          if (hashFeld.KannOben)
          {
           HashFeld neuHash = new HashFeld(hashGro);
           hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeOben(neuHash));
           if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
          }

          if (hashFeld.KannUnten)
          {
           HashFeld neuHash = new HashFeld(hashGro);
           hashInfo[hashGro / hashSatzGro] = new HashInfo(hashPos, hashFeld.BewegeUnten(neuHash));
           if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
          }
         }
         #endregion

         #region # // --- alle zusätzlich erreichbare Spielerpositionen ermitteln ---
         List<int> alleSpielerPos = new List<int>();
         alleSpielerPos.Add(spielerPos); spielerFertig[spielerPos] = true;
         for (int i = 0; i < alleSpielerPos.Count; i++)
         {
          int checkPos = alleSpielerPos[i];
          foreach (int ofs in ofss)
          {
           if ((feldData[checkPos + ofs] & 3) == 0 && !alleSpielerPos.Any(p => p == checkPos + ofs))
           {
            alleSpielerPos.Add(checkPos + ofs);
            if (erlaubteBoxFelder[checkPos + ofs]) spielerFertig[checkPos + ofs] = true;
           }
          }
         }
         #endregion

         if (find)
         {
          spielerGut.AddRange(alleSpielerPos);
         }
         else
         {
          spielerSchlecht.AddRange(alleSpielerPos);
         }
        }

        if (spielerGut.Count == 0 || spielerSchlecht.Count > 0)
        {
         #region # // --- Box 1 -> Box 2 + 3 ---
         List<short> dazu = new List<short>();

         dazu.Add((short)boxPos2);
         dazu.Add((short)boxPos3);
         if (spielerGut.Count < spielerSchlecht.Count) // nur die guten Felder merken?
         {
          dazu.Add((short)spielerGut.Count);
          dazu.AddRange(spielerGut.Select(pos => (short)pos));
         }
         else // nur die schlechten Felder merken (da weniger)
         {
          dazu.Add((short)-spielerSchlecht.Count);
          dazu.AddRange(spielerSchlecht.Select(pos => (short)pos));
         }

         FehlerDataUpdate(ref fehlerBox3[boxPos1], dazu);
         fehlerVorhanden[boxPos1] |= 1;
         #endregion

         #region # // --- Box 2 -> Box 1 + 3 ---
         dazu.Clear();

         dazu.Add((short)boxPos1);
         dazu.Add((short)boxPos3);
         if (spielerGut.Count < spielerSchlecht.Count) // nur die guten Felder merken?
         {
          dazu.Add((short)spielerGut.Count);
          dazu.AddRange(spielerGut.Select(pos => (short)pos));
         }
         else // nur die schlechten Felder merken (da weniger)
         {
          dazu.Add((short)-spielerSchlecht.Count);
          dazu.AddRange(spielerSchlecht.Select(pos => (short)pos));
         }

         FehlerDataUpdate(ref fehlerBox3[boxPos2], dazu);
         fehlerVorhanden[boxPos2] |= 1;
         #endregion

         #region # // --- Box 3 -> Box 1 + 2 ---
         dazu.Clear();

         dazu.Add((short)boxPos1);
         dazu.Add((short)boxPos2);
         if (spielerGut.Count < spielerSchlecht.Count) // nur die guten Felder merken?
         {
          dazu.Add((short)spielerGut.Count);
          dazu.AddRange(spielerGut.Select(pos => (short)pos));
         }
         else // nur die schlechten Felder merken (da weniger)
         {
          dazu.Add((short)-spielerSchlecht.Count);
          dazu.AddRange(spielerSchlecht.Select(pos => (short)pos));
         }

         FehlerDataUpdate(ref fehlerBox3[boxPos3], dazu);
         fehlerVorhanden[boxPos3] |= 1;
         #endregion
        }

        feldData[boxPos3] ^= 2;
       }

       feldData[boxPos2] ^= 2;
      }

      feldData[boxPos1] ^= 2;
     }
    } break;
    #endregion

    default: throw new NotSupportedException();
   }

  }
  #endregion

  #region # bool FehlerCheckDirekt(int boxPos) // prüft, ob ein bestimmte Boxkombination unerlaubt ist (Spielerposition wird nicht beachtet)
  /// <summary>
  /// prüft, ob ein bestimmte Boxkombination unerlaubt ist (Spielerposition wird nicht beachtet)
  /// </summary>
  /// <param name="boxPos">Hauptbox welche überprüft werden soll</param>
  /// <returns>true, wenn die Box ungültig ist</returns>
  bool FehlerCheckDirekt(int boxPos)
  {
   if (fehlerBox2[boxPos].dataGro > 0)
   {
    int p = fehlerBox2[boxPos].dataPos;
    int pBis = p + fehlerBox2[boxPos].dataGro;
    int anzahl;
    while (p < pBis)
    {
     if ((feldData[fehlerData[p++]] & 2) == 2) // Boxfehler zutreffend?
     {
      if ((anzahl = fehlerData[p++]) == 0) return true;
     }
     else
     {
      anzahl = fehlerData[p++];
     }
     if (anzahl < 0) anzahl = -anzahl;
     p += anzahl;
    }
   }
   return false;
  }
  #endregion

  #region # static bool FehlerCheckerHash(int boxPos, int spielerPos) // prüft ob an der bestimmten Stelle eine Box stehen darf
  /// <summary>
  /// prüft ob an der bestimmten Stelle eine Box stehen darf
  /// </summary>
  /// <param name="boxPos">Position der zu prüfenden Box</param>
  /// <param name="spielerPos">Position des eigenen Spielers</param>
  /// <returns>true, wenn der Platz für die Box ungültig ist, sonst false</returns>
  static bool FehlerCheckerHash(int boxPos, int spielerPos)
  {
   int hashOfs = boxPos / hashSatzGro * hashSatzGro;
   boxPos %= hashSatzGro; spielerPos %= hashSatzGro;

   if (fehlerVorhanden == null || fehlerVorhanden.Length != feldAnzahl) return false;

   #region # // --- fehlerBox2 ---
   if (fehlerBox2[boxPos].dataGro > 0)
   {
    int p = fehlerBox2[boxPos].dataPos;
    int pBis = p + fehlerBox2[boxPos].dataGro;
    while (p < pBis)
    {
     if ((hashData[fehlerData[p++] + hashOfs] & 2) == 2) // Boxfehler zutreffend?
     {
      int anzahl = fehlerData[p++];
      if (anzahl >= 0)
      {
       bool gutFind = false;
       while (--anzahl >= 0) if (fehlerData[p++] == spielerPos) gutFind = true;
       if (!gutFind) return true; // Treffer, keine erlaubte Spielerposition
      }
      else
      {
       anzahl = -anzahl;
       while (--anzahl >= 0) if (fehlerData[p++] == spielerPos) return true; // Treffer, unerlaubte Spielerposition
      }
     }
     else
     {
      int anzahl = fehlerData[p++];
      if (anzahl < 0) anzahl = -anzahl;
      p += anzahl;
     }
    }
   }
   #endregion

   #region # // --- fehlerBox3 ---
   if (fehlerBox3[boxPos].dataGro > 0)
   {
    int p = fehlerBox3[boxPos].dataPos;
    int pBis = p + fehlerBox3[boxPos].dataGro;
    while (p < pBis)
    {
     bool treffer1 = (hashData[fehlerData[p++] + hashOfs] & 2) == 2;
     bool treffer2 = (hashData[fehlerData[p++] + hashOfs] & 2) == 2;
     if (treffer1 && treffer2) // Boxfehler zutreffend?
     {
      int anzahl = fehlerData[p++];
      if (anzahl >= 0)
      {
       bool gutFind = false;
       while (--anzahl >= 0) if (fehlerData[p++] == spielerPos) gutFind = true;
       if (!gutFind) return true; // Treffer, keine erlaubte Spielerposition
      }
      else
      {
       anzahl = -anzahl;
       while (--anzahl >= 0) if (fehlerData[p++] == spielerPos) return true; // Treffer, unerlaubte Spielerposition
      }
     }
     else
     {
      int anzahl = fehlerData[p++];
      if (anzahl < 0) anzahl = -anzahl;
      p += anzahl;
     }
    }
   }
   #endregion

   return false;
  }
  #endregion

  #region # // --- Scan-Button ---
  private void button1_Click(object sender, EventArgs e)
  {
   button1.Text = "scan";
   button1.Update();
   pictureBox1.Image = null;
   pictureBox1.Update();

   ScanFelder(2 + 16, 26 + 32);

   if (feldData != null)
   {
    #region # // --- Feld auf das nötigste verkleinern ---
    // --- obere leere Zeilen entfernen ---
    for (int c = 0; ; )
    {
     for (int i = 0; i < feldBreite; i++) if (feldData[i] == 0) c++;
     if (c < feldBreite) break;
     for (int i = feldBreite; i < feldData.Length; i++) feldData[i - feldBreite] = feldData[i];
     feldHöhe--;
     Array.Resize(ref feldData, feldData.Length - feldBreite);
     c = 0;
    }

    // --- untere leere Zeilen entfernen ---
    for (int c = 0; ; )
    {
     for (int i = 0; i < feldBreite; i++) if (feldData[i + (feldHöhe - 1) * feldBreite] == 0) c++;
     if (c < feldBreite) break;
     feldHöhe--;
     Array.Resize(ref feldData, feldData.Length - feldBreite);
     c = 0;
    }

    // --- linke leere Spalten entfernen ---
    for (int c = 0; ; )
    {
     for (int i = 0; i < feldHöhe; i++) if (feldData[i * feldBreite] == 0) c++;
     if (c < feldHöhe) break;
     for (int y = 0; y < feldHöhe; y++) for (int x = 1; x < feldBreite; x++) feldData[(x - 1) + y * (feldBreite - 1)] = feldData[x + y * feldBreite];
     feldBreite--;
     Array.Resize(ref feldData, feldData.Length - feldHöhe);
     c = 0;
    }

    // --- rechte leere Spalten entfernen ---
    for (int c = 0; ; )
    {
     for (int i = 0; i < feldHöhe; i++) if (feldData[i * feldBreite + feldBreite - 1] == 0) c++;
     if (c < feldHöhe) break;
     for (int y = 1; y < feldHöhe; y++) for (int x = 0; x < feldBreite - 1; x++) feldData[x + y * (feldBreite - 1)] = feldData[x + y * feldBreite];
     feldBreite--;
     Array.Resize(ref feldData, feldData.Length - feldHöhe);
     c = 0;
    }
    #endregion

    feldAnzahl = feldBreite * feldHöhe;
    hashSatzGro = feldAnzahl + 1 + 1 + 1; // Anzahl + SpielerL + SpielerH + Restwürfel

    button1.Text = "rechne...";
    button1.Update();

    ErstelleErlaubteBoxFelder((new HashFeld(0, feldData)).SpielerPos);

    int[] merkFelder = new int[feldAnzahl];
    Array.Copy(feldData, merkFelder, feldAnzahl);

    FehlerMapErstellen(1);
    FehlerMapErstellen(2);
    FehlerMapErstellen(3);

    MapReset(true);

    hashGro = hashSatzGro;

    Array.Copy(merkFelder, feldData, feldAnzahl);

    HashFeld h = new HashFeld(0, feldData);

    feldZeichner.Zeichne(pictureBox1, h);

    merkHashPos = 0;
    hashInfo[0].verschoben = true;
    hashInfo[0].vorgänger = 0;

    zurückButton.Enabled = false;
    vorButton.Enabled = false;

    hashIndex.Clear();
   }

   button1.Text = "ok";
  }
  #endregion
  #region # // --- Dazu-Button ---
  private void button2_Click(object sender, EventArgs e)
  {
   FeldDazu(long.Parse(textBox1.Text), int.Parse(textBox2.Text));
   button1_Click(null, null);
  }
  #endregion

  #region # public class FeldZeichner // Klasse zum zeichnen des Feldes
  /// <summary>
  /// Klasse zum zeichnen des Feldes
  /// </summary>
  public class FeldZeichner
  {
   /// <summary>
   /// merkt sich die Bilder im Array
   /// </summary>
   Bitmap[] bilder = null;

   /// <summary>
   /// Konstruktor
   /// </summary>
   /// <param name="hauptPfad">Hauptpfad, wo die Bilder liegen (Pic_*.png)</param>
   public FeldZeichner(string hauptPfad)
   {
    // 0 = leer, 1 = Mauer, 2 = Würfel, 4 = Zielfeld, 6 = Würfel auf Zielfeld, 8 = Spieler, 12 = Spieler auf einem Zielfeld
    string[] bilderNamen = { 
                            "Pic_Leer.png",        // 0 = leer
                            "Pic_Wand.png",        // 1 = Mauer
                            "Pic_Box.png",         // 2 = Würfel
                            "",                    // 3
                            "Pic_Ziel.png",        // 4 = Zielfeld
                            "",                    // 5
                            "Pic_OkBox.png",       // 6 = Würfel auf Zielfeld
                            "",                    // 7
                            "Pic_Spieler.png",     // 8 = Spieler
                            "",                    // 9
                            "",                    // 10
                            "",                    // 11
                            "Pic_ZielSpieler.png", // 12 = Spieler auf einem Zielfeld
                           };

    bilder = bilderNamen.Select(name => (name ?? "") != "" && File.Exists(hauptPfad + name) ? new Bitmap(hauptPfad + name) : null).ToArray();
   }

   public void Zeichne(PictureBox pictureBox, HashFeld hashFeld)
   {
    Zeichne(pictureBox, Enumerable.Range(hashFeld.hashPos, hashSatzGro).Select(i => (int)hashData[i]).ToArray());
   }

   public void Zeichne(PictureBox pictureBox, int hashPos)
   {
    Zeichne(pictureBox, Enumerable.Range(hashPos, hashSatzGro).Select(i => (int)hashData[i]).ToArray());
   }
   
   public void Zeichne(PictureBox pictureBox, int[] feldData)
   {
    Bitmap ausgabe = new Bitmap(feldBreite * 32 + 32, feldHöhe * 32 + 32, PixelFormat.Format32bppRgb);
    Graphics g = Graphics.FromImage(ausgabe);

    for (int y = 0; y < feldHöhe; y++)
    {
     for (int x = 0; x < feldBreite; x++)
     {
      int f = feldData[x + y * feldBreite];
      if ((uint)f < bilder.Length && bilder[f] != null) g.DrawImage(bilder[f], x * 32 + 16, y * 32 + 16);
     }
    }

    pictureBox.Image = ausgabe;
    pictureBox.Update();
    GC.Collect();
   }
  }
  #endregion

  FeldZeichner feldZeichner = new FeldZeichner(hauptOrdner);

  private void Form1_Load(object sender, EventArgs e)
  {
   FeldTypenLaden();

  }

  int merkHashPos = 0;

  Dictionary<ulong, bool> hashIndex = new Dictionary<ulong, bool>();

  #region # bool HashBekannt(HashFeld suchHash) // gibt an, ob der Hashknoten schon bekannt ist
  /// <summary>
  /// gibt an, ob der Hashknoten schon bekannt ist
  /// </summary>
  /// <param name="suchHash">Hash nach dem gesucht werden soll</param>
  /// <returns>true, wenn der Hashknoten schon bekannt ist</returns>
  bool HashBekannt(HashFeld suchHash)
  {
   ulong suchCrc = suchHash.GetCrc64();

   bool find;

   if (hashIndex.TryGetValue(suchCrc, out find)) return true;

   hashIndex.Add(suchCrc, true);
   return false;
  }
  #endregion

  #region # void Tick(int limit) // berechnet einen oder mehrere Schritte
  /// <summary>
  /// berechnet einen oder mehrere Schritte
  /// </summary>
  /// <param name="limit">maximal zu berechnende Schritte</param>
  void Tick(int limit)
  {
   zurückButton.Enabled = false;
   vorButton.Enabled = false;

   for (int lim = 0; lim < limit; lim++)
   {
    do
    {
     if (merkHashPos == hashGro)
     {
      tickButton.Text = "keine weiteren Knoten bekannt";
      return;
     }

     if (hashGro > hashInfoMax * hashSatzGro)
     {
      hashInfoMax *= 2;
      Array.Resize(ref hashInfo, hashInfoMax + 65536);
     }

     HashFeld h = new HashFeld(merkHashPos);

     if (h.RestWürfel == 0)
     {
      feldZeichner.Zeichne(pictureBox1, new HashFeld(merkHashPos));
      tickButton.Text = "Ziel gefunden! (" + hashIndex.Count.ToString("#,##0") + " Knoten)";
      int pos = merkHashPos;
      List<int> posListe = new List<int>();
      posListe.Add(pos);
      while (pos > 0)
      {
       pos = hashInfo[pos / hashSatzGro].vorgänger;
       posListe.Add(pos);
      }
      posListe.Reverse();
      viewListe = posListe.ToArray();
      viewPos = 0;
      zurückButton.Enabled = false;
      vorButton.Enabled = true;
      return;
     }

     if (h.KannLinks)
     {
      HashFeld neuHash = new HashFeld(hashGro);
      hashInfo[hashGro / hashSatzGro] = new HashInfo(merkHashPos, h.BewegeLinks(neuHash));
      if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
     }

     if (h.KannRechts)
     {
      HashFeld neuHash = new HashFeld(hashGro);
      hashInfo[hashGro / hashSatzGro] = new HashInfo(merkHashPos, h.BewegeRechts(neuHash));
      if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
     }

     if (h.KannOben)
     {
      HashFeld neuHash = new HashFeld(hashGro);
      hashInfo[hashGro / hashSatzGro] = new HashInfo(merkHashPos, h.BewegeOben(neuHash));
      if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
     }

     if (h.KannUnten)
     {
      HashFeld neuHash = new HashFeld(hashGro);
      hashInfo[hashGro / hashSatzGro] = new HashInfo(merkHashPos, h.BewegeUnten(neuHash));
      if (!HashBekannt(neuHash)) hashGro += hashSatzGro;
     }

     merkHashPos += hashSatzGro;
    } while (merkHashPos < hashGro && !hashInfo[merkHashPos / hashSatzGro].verschoben);

    if (merkHashPos < hashGro)
    {
     if (lim + 1 == limit)
     {
      feldZeichner.Zeichne(pictureBox1, new HashFeld(merkHashPos));
      int schritte = OptiErmitteleTiefe(merkHashPos / hashSatzGro);
      tickButton.Text = (hashGro / hashSatzGro).ToString("#,##0") + " Felder (" + ((double)hashGro / 1048576.0).ToString("#,##0.0") + " MB) [" + schritte + "]";
      tickButton.Update();
     }
    }
    else
    {
     tickButton.Text = "keine weiteren Knoten bekannt";
     return;
    }
   }
  }
  #endregion

  #region # // --- Tick-Buttons ---
  private void button3_Click(object sender, EventArgs e)
  {
   Tick(1);
  }
  private void button3_Click_1(object sender, EventArgs e)
  {
   Tick(10);
  }
  private void button4_Click(object sender, EventArgs e)
  {
   Tick(100);
  }
  private void button5_Click(object sender, EventArgs e)
  {
   Tick(1000);
  }
  private void button6_Click_1(object sender, EventArgs e)
  {
   Tick(10000);
  }
  #endregion

  int[] viewListe = null;
  int viewPos = 0;

  private void vorButton_Click(object sender, EventArgs e)
  {
   viewPos++;
   if (viewPos >= viewListe.Length - 1)
   {
    viewPos = viewListe.Length - 1;
    vorButton.Enabled = false;
   }
   zurückButton.Enabled = true;
   feldZeichner.Zeichne(pictureBox1, viewListe[viewPos]);
  }

  private void zurückButton_Click(object sender, EventArgs e)
  {
   viewPos--;
   if (viewPos <= 0)
   {
    viewPos = 0;
    zurückButton.Enabled = false;
   }
   vorButton.Enabled = true;
   feldZeichner.Zeichne(pictureBox1, viewListe[viewPos]);
  }

  /// <summary>
  /// ermittelt die Tiefe eines bestimmten Hash-Eintrages
  /// </summary>
  /// <param name="pos">Position auf den Hasheintrag (Index)</param>
  /// <returns>Tiefe in Schritten</returns>
  int OptiErmitteleTiefe(int pos)
  {
   int schritte = 0;
   while (pos > 0)
   {
    pos = hashInfo[pos].vorgänger / hashSatzGro;
    schritte++;
   }
   return schritte;
  }

  /// <summary>
  /// sucht den Anfang der letzten vollständigen Knotenkette
  /// </summary>
  /// <returns>Anfangsposition der Kette</returns>
  int OptiSucheEndKnoten()
  {
   int pos = hashGro / hashSatzGro;
   pos--;
   int tiefe = OptiErmitteleTiefe(pos) - 1;
   while (pos > 0 && OptiErmitteleTiefe(pos - 1) >= tiefe) pos--;
   return pos;
  }

  int OptiSucheEndKnoten(int tiefe)
  {
   int pos = hashGro / hashSatzGro;
   while (pos > 0 && OptiErmitteleTiefe(pos - 1) >= tiefe) pos--;
   return pos;
  }

  private void button6_Click(object sender, EventArgs e)
  {
   optimizeButton.Text = "suche... (1 / 5)"; optimizeButton.Update(); Application.DoEvents();
   int pos = OptiSucheEndKnoten();
   if (pos > 100)
   {
    optimizeButton.Text = "rechne... (2 / 5)"; optimizeButton.Update(); Application.DoEvents();

    int altAnzahl = hashGro / hashSatzGro;
    int altTiefe = OptiErmitteleTiefe(merkHashPos / hashSatzGro);

    Dictionary<int, int> knotenBehalten = new Dictionary<int, int>();
    for (int i = hashGro / hashSatzGro - 1; i >= pos; i--)
    {
     if (knotenBehalten.ContainsKey(i)) continue;
     int vg = hashInfo[i].vorgänger / hashSatzGro;
     knotenBehalten.Add(i, vg);
     for (; ; )
     {
      int p = vg;
      vg = hashInfo[vg].vorgänger / hashSatzGro;
      if (knotenBehalten.ContainsKey(p)) break;
      knotenBehalten.Add(p, vg);
      if (p == 0) break;
     }
    }

    optimizeButton.Text = "sortiere... (3 / 5)"; optimizeButton.Update(); Application.DoEvents();

    int[] trans = new int[altAnzahl];
    int cc = 0;
    int[] tmp = knotenBehalten.Select(satz => satz.Key).ToArray();
    Array.Sort(tmp);
    foreach (int satz in tmp) trans[satz] = cc++;
    tmp = null;
    GC.Collect();

    optimizeButton.Text = "schmelze... (4 / 5)"; optimizeButton.Update(); Application.DoEvents();

    int y = 0;
    foreach (var altKnoten in knotenBehalten.OrderBy(x => x.Key))
    {
     hashInfo[y] = new HashInfo(trans[hashInfo[altKnoten.Key].vorgänger / hashSatzGro] * hashSatzGro, hashInfo[altKnoten.Key].verschoben);
     int neuPos = y * hashSatzGro;
     int altPos = altKnoten.Key * hashSatzGro;
     for (int i = 0; i < hashSatzGro; i++) hashData[neuPos + i] = hashData[altPos + i];
     y++;
    }

    hashGro = y * hashSatzGro;

    optimizeButton.Text = "abschluss... (5 / 5)"; optimizeButton.Update(); Application.DoEvents();

    merkHashPos = OptiSucheEndKnoten(altTiefe) * hashSatzGro;

    optimizeButton.Text = "ok, " + (altAnzahl - y).ToString("#,##0") + " Knoten entfernt";
    Tick(1);
   }

   GC.Collect();
  }

 }
}
