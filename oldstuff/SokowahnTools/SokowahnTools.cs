#region # using *.*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace Sokosolver.SokowahnTools
{
 #region # public static class SokowahnTools // Klasse mit Tools für die Spielfelder
 /// <summary>
 /// Klasse mit Tools für die Spielfelder
 /// </summary>
 public static class SokowahnStaticTools
 {
  #region # public static void SpielfeldEinlesen(string spielFeld, out int feldBreite, out int feldHöhe, out int raumSpielerPos, out char[] feldData, out char[] feldData) // liest ein Spielfeld ein und gibt deren Daten in passendem Format zurück
  /// <summary>
  /// liest ein Spielfeld ein und gibt deren Daten in passendem Format zurück
  /// </summary>
  /// <param name="spielFeld">Spielfeld als String</param>
  /// <param name="feldBreite">Breite des Feldes in Zeichen</param>
  /// <param name="feldHöhe">Höhe des Feldes in Zeichen</param>
  /// <param name="raumSpielerPos">Position des Spielers</param>
  /// <param name="feldData">fertiges Spielfeld als einzelne Zeichen (mögliche Zeichen: '#', ' ', '.', '$', '*', '@', '+')</param>
  /// <param name="feldData">gleiche wie feldData, jedoch ohne Kisten und ohne Spielerfigur (mögliche Zeichen: '#', ' ', '.')</param>
  public static void SpielfeldEinlesen(string spielFeld, out int feldBreite, out int feldHöhe, out int spielerPos, out char[] feldData, out char[] feldDataLeer)
  {
   int cutter = spielFeld.IndexOfAny(Enumerable.Range(0, char.MaxValue).Select(c => (char)c).Where(c => !"# .$*@+\r\n\t".Contains(c)).ToArray());
   if (cutter >= 0) spielFeld = spielFeld.Substring(0, cutter);

   #region # // --- einlesen ---
   string[] zeilen = spielFeld.Replace('\r', '\n').Replace('\t', ' ').Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

   int breite = zeilen.Max(z => z.Length);

   char[] zeichen = zeilen.SelectMany(z => z.PadRight(breite, ' ')).ToArray();

   int höhe = zeichen.Length / breite;
   #endregion

   #region # // --- zurecht schneiden ---
   // Links weg schneiden
   while (Enumerable.Range(0, höhe).All(i => zeichen[i * breite] == ' '))
   {
    for (int y = 0; y < höhe; y++) for (int x = 1; x < breite; x++) zeichen[x - 1 + y * (breite - 1)] = zeichen[x + y * breite];
    breite--;
   }

   // Oben weg schneiden
   while (Enumerable.Range(0, breite).All(i => zeichen[i] == ' '))
   {
    for (int y = 1; y < höhe; y++) for (int x = 0; x < breite; x++) zeichen[x + (y - 1) * breite] = zeichen[x + y * breite];
    höhe--;
   }

   // Rechts weg schneiden
   while (Enumerable.Range(0, höhe).All(i => zeichen[i * breite + breite - 1] == ' '))
   {
    for (int y = 0; y < höhe; y++) for (int x = 0; x < breite - 1; x++) zeichen[x + y * (breite - 1)] = zeichen[x + y * breite];
    breite--;
   }

   // Unten weg schneiden
   while (Enumerable.Range(0, breite).All(i => zeichen[i + (breite * höhe) - breite] == ' '))
   {
    höhe--;
   }

   Array.Resize(ref zeichen, breite * höhe);
   #endregion

   #region # // --- Felder zuweisen ---
   feldBreite = breite;

   feldHöhe = höhe;

   feldData = zeichen.ToArray();

   int findPos = -1;

   feldDataLeer = zeichen.Select((z, i) =>
   {
    switch (z)
    {
     case '#': return '#';
     case ' ': return ' ';
     case '.': return '.';
     case '$': return ' ';
     case '*': return '.';
     case '@': if (findPos >= 0) throw new Exception("Fehler, Spielerfigur mehrmals vorhanden!"); findPos = i; return ' ';
     case '+': if (findPos >= 0) throw new Exception("Fehler, Spielerfigur mehrmals vorhanden!"); findPos = i; return '.';
     default: throw new Exception("ungültiges Zeichen: " + z);
    }
   }).ToArray();

   spielerPos = findPos;
   #endregion
  }
  #endregion

  #region # public static bool[] SpielfeldRaumScan(char[] feldData, int feldBreite) // ermittelt die Felder, wo der Spieler sich aufhalten darf
  /// <summary>
  /// ermittelt die Felder, wo der Spieler sich aufhalten darf
  /// </summary>
  /// <param name="feldData">Felddaten des Spielfeldes als char-Array</param>
  /// <param name="feldBreite">Breite des Feldes (Höhe wird automatisch ermittelt)</param>
  /// <returns>Bool-Array mit gleicher Größe wie feldData, gibt an, wo der Spieler sich aufhalten darf</returns>
  public static bool[] SpielfeldRaumScan(char[] feldData, int feldBreite)
  {
   int feldHöhe = feldData.Length / feldBreite;

   bool[] spielerRaum = feldData.Select(c => c == '@' || c == '+').ToArray();

   bool find = true;
   while (find)
   {
    find = false;
    for (int y = 1; y < feldHöhe - 1; y++)
    {
     for (int x = 1; x < feldBreite - 1; x++)
     {
      if (spielerRaum[x + y * feldBreite])
      {
       int p = x + y * feldBreite - feldBreite;
       if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
       p += feldBreite - 1;
       if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
       p += 2;
       if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
       p += feldBreite - 1;
       if (!spielerRaum[p] && " .$*".Any(c => feldData[p] == c)) find = spielerRaum[p] = true;
      }
     }
    }
   }

   // --- verursacht Inkonsistenzen, wenn Spielerfigur selbst in einer Sackgasse steht und diese weg optimiert wird, wenn der Spieler dort mal nicht steht... ---

   // unsinnige Lauf-Felder entfernen (Sackgassen entfernen)
   //find = true;
   //while (find)
   //{
   // find = false;
   // for (int y = 1; y < feldHöhe - 1; y++)
   // {
   //  for (int x = 1; x < feldBreite - 1; x++)
   //  {
   //   int p = x + y * feldBreite;
   //   if (spielerRaum[p] && feldData[p] == ' ')
   //   {
   //    if (!spielerRaum[p - 1] && !spielerRaum[p - feldBreite] && !spielerRaum[p + 1]) { spielerRaum[p] = false; find = true; }
   //    if (!spielerRaum[p - 1] && !spielerRaum[p + feldBreite] && !spielerRaum[p + 1]) { spielerRaum[p] = false; find = true; }
   //    if (!spielerRaum[p - feldBreite] && !spielerRaum[p - 1] && !spielerRaum[p + feldBreite]) { spielerRaum[p] = false; find = true; }
   //    if (!spielerRaum[p - feldBreite] && !spielerRaum[p + 1] && !spielerRaum[p + feldBreite]) { spielerRaum[p] = false; find = true; }
   //   }
   //  }
   // }
   //}

   return spielerRaum;
  }
  #endregion

  #region # public static IEnumerable<SokowahnStellung> SucheZielStellungen(SokowahnRaum raumBasis) // ermittelt alle möglichen Zielstellungen
  /// <summary>
  /// ermittelt alle möglichen Zielstellungen
  /// </summary>
  /// <param name="raumBasis">Raumsystem mit Basis-Stellung</param>
  /// <returns>Enumerable aller möglichen Zielstellungen</returns>
  public static IEnumerable<SokowahnStellung> SucheZielStellungen(SokowahnRaum raumBasis)
  {
   int feldBreite = raumBasis.FeldBreite;
   int feldHöhe = raumBasis.FeldHöhe;
   char[] feldData = raumBasis.FeldDataLeer.Select(c => c == '.' ? '*' : c).ToArray();

   for (int spielerY = 1; spielerY < feldHöhe - 1; spielerY++)
   {
    for (int spielerX = 1; spielerX < feldBreite - 1; spielerX++)
    {
     int pos = spielerX + spielerY * feldBreite;
     if (feldData[pos] == ' ' && (feldData[pos - 1] == '*' || feldData[pos + 1] == '*' || feldData[pos - feldBreite] == '*' || feldData[pos + feldBreite] == '*'))
     {
      feldData[pos] = '@';
      SokowahnRaum tmpRaum = new SokowahnRaum(feldData, feldBreite);
      tmpRaum.SpielerZugTiefe = 60000;

      var findStellungen = tmpRaum.GetVariantenRückwärts().ToArray();

      if (findStellungen.Length > 0)
      {
       yield return tmpRaum.GetStellung();
//       foreach (var findStellung in findStellungen) yield return findStellung;
      }

      feldData[pos] = ' ';
     }
    }
   }

   yield break;
  }
  #endregion
 }
 #endregion
}
