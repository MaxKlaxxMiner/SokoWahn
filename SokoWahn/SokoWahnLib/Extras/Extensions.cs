
namespace SokoWahnLib
{
  /// <summary>
  /// Erweiterungsmethoden
  /// </summary>
  public static class ExtensionsHelper
  {
    /// <summary>
    /// prüft, ob ein bestimmter Wert enthalten ist
    /// </summary>
    /// <param name="array">Array, welches durchsucht werden soll</param>
    /// <param name="checkValue">der zu prüfende Wert</param>
    /// <returns>true, wenn der gefunden wurde</returns>
    public static bool Contains(this int[] array, int checkValue)
    {
      foreach (var value in array)
      {
        if (value == checkValue) return true;
      }
      return false;
    }

    /// <summary>
    /// prüft, ob ein bestimmter Wert enthalten ist
    /// </summary>
    /// <param name="array">Array, welches durchsucht werden soll</param>
    /// <param name="checkValue">der zu prüfende Wert</param>
    /// <returns>true, wenn der gefunden wurde</returns>
    public static bool Contains(this uint[] array, uint checkValue)
    {
      foreach (var value in array)
      {
        if (value == checkValue) return true;
      }
      return false;
    }
  }
}
