using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sokosolver.SokowahnTools
{
 /// <summary>
 /// enthält einige statische Erweiterungen
 /// </summary>
 public static class SokowahnErweiterungen
 {
  /// <summary>
  /// erstellt eine gekürzte Kopie eines Arrays
  /// </summary>
  /// <param name="array">Array mit den entsprechenden Daten</param>
  /// <param name="anzahl">Anzahl der Datensätze (darf maximale so lang sein wie das Array selbst)</param>
  /// <returns>neues Array mit den entsprechenden Daten</returns>
  public static T[] TeilArray<T>(this T[] array, int anzahl) where T : struct
  {
   T[] ausgabe = new T[anzahl];

   for (int i = 0; i < anzahl; i++) ausgabe[i] = array[i];

   return ausgabe;
  }
 }

}
