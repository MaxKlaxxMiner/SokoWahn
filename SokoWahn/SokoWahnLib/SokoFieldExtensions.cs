#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
#endregion

namespace SokoWahnLib
{
  /// <summary>
  /// Hilfsmethoden für SokoWahn-Spielfeld
  /// </summary>
  public static class SokoFieldExtensions
  {
    #region # public static unsafe string GetText(this ISokoField field)
    /// <summary>
    /// gibt das gesamte Spielfeld im Text-Format aus
    /// </summary>
    /// <param name="field">Spielfeld, welches ausgegeben werden soll</param>
    /// <returns>fertiges Spielfeld im Textformat</returns>
    public static unsafe string GetText(this ISokoField field)
    {
      int w = field.Width;
      int h = field.Height;
      string result = UnsafeHelper.FastAllocateString((w + 2) * h); // Ausgabe Größe berechnen (pro Zeile 2 Zeichen extra für die Zeilenumbrüche)

      fixed (char* resultP = result)
      {
        for (int resultPos = 0, fieldPos = 0; resultPos < result.Length; )
        {
          for (int x = 0; x < w; x++)
          {
            resultP[resultPos++] = field.GetFieldChar(fieldPos++);
          }
          resultP[resultPos++] = '\r'; // Zeilenumbruch hinzufügen
          resultP[resultPos++] = '\n';
        }
      }

      return result;
    }
    #endregion

    #region # public static bool ValidPos(this ISokoField field, int pos) // prüft, ob die Position auf dem Spielfeld gültig ist
    /// <summary>
    /// prüft, ob die Position auf dem Spielfeld gültig ist
    /// </summary>
    /// <param name="field">Spielfeld, welches benutzt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn es sich um eine gültige Position handelt</returns>
    public static bool ValidPos(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      return GetWalkPosis(field).Contains(pos);
    }
    #endregion

    #region # public static HashSet<int> GetWalkPosis(this ISokoField field) // gibt alle begehbaren Position auf dem Spielfeld zurück
    /// <summary>
    /// Cache für häufige Abfragen
    /// </summary>
    static readonly Dictionary<ISokoField, HashSet<int>> CacheWalkPosis = new Dictionary<ISokoField, HashSet<int>>();

    /// <summary>
    /// gibt alle begehbaren Position auf dem Spielfeld zurück
    /// </summary>
    /// <param name="field">gesamtes Spielfeld, welches abgefragt werden soll</param>
    /// <returns>HastSet der begehbaren Positionen</returns>
    public static HashSet<int> GetWalkPosis(this ISokoField field)
    {
      HashSet<int> walkFields;
      if (CacheWalkPosis.TryGetValue(field, out walkFields)) return walkFields;

      walkFields = new HashSet<int>();
      var todo = new Stack<int>();
      todo.Push(field.PlayerPos);
      while (todo.Count > 0)
      {
        int pos = todo.Pop();
        if (field.GetFieldChar(pos) == '#') continue; // Feld ist nie begehbar
        if (walkFields.Contains(pos)) continue;   // Feld schon bekannt
        walkFields.Add(pos);                      // bekannte Felder merken
        todo.Push(pos - 1); // links hinzufügen
        todo.Push(pos + 1); // rechts hinzufügen
        todo.Push(pos - field.Width); // oben hinzufügen
        todo.Push(pos + field.Width); // unten hinzufügen
      }

      CacheWalkPosis.Add(field, walkFields);
      return walkFields;
    }
    #endregion

    #region # public static MoveType GetMoveTypes(this ISokoField field) // gibt alle Bewegungsmöglichkeiten des Spielers zurück
    /// <summary>
    /// gibt alle Bewegungsmöglichkeiten des Spielers zurück
    /// </summary>
    /// <param name="field">Spielfeld, welches geprüft werden soll</param>
    /// <returns>Flags vom Typ MoveType</returns>
    public static MoveType GetMoveTypes(this ISokoField field)
    {
      var result = MoveType.None;

      int w = field.Width;
      int h = field.Height;
      int p = field.PlayerPos;
      int px = p % w;
      int py = p / w;

      // --- links prüfen ---
      if (px > 0)
      {
        char f = field.GetFieldChar(p - 1);
        if (f == ' ' || f == '.') result |= MoveType.Left; // Spielfeld leer?
        else if ((f == '$' || f == '*') && px > 1) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetFieldChar(p - 2);
          if (f == ' ' || f == '.') result |= MoveType.LeftPush; // Feld hinter der Kiste leer?
        }
      }

      // --- rechts prüfen ---
      if (px < w - 1)
      {
        char f = field.GetFieldChar(p + 1);
        if (f == ' ' || f == '.') result |= MoveType.Right; // Spielfeld leer?
        else if ((f == '$' || f == '*') && px < w - 2) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetFieldChar(p + 2);
          if (f == ' ' || f == '.') result |= MoveType.RightPush; // Feld hinter der Kiste leer?
        }
      }

      // --- oben prüfen ---
      if (py > 0)
      {
        char f = field.GetFieldChar(p - w);
        if (f == ' ' || f == '.') result |= MoveType.Up; // Spielfeld leer?
        else if ((f == '$' || f == '*') && py > 1) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetFieldChar(p - w * 2);
          if (f == ' ' || f == '.') result |= MoveType.UpPush; // Feld hinter der Kiste leer?
        }
      }

      // --- unten prüfen ---
      if (py < h - 1)
      {
        char f = field.GetFieldChar(p + w);
        if (f == ' ' || f == '.') result |= MoveType.Down; // Spielfeld leer?
        else if ((f == '$' || f == '*') && py < h - 2) // Kiste vorhanden und theoretisch Platz dahinter?
        {
          f = field.GetFieldChar(p + w * 2);
          if (f == ' ' || f == '.') result |= MoveType.DownPush; // Feld hinter der Kiste leer?
        }
      }

      // --- gefundene Ergebnisse zurück geben ---
      return result;
    }
    #endregion

    #region # public static bool SafeMove(this ISokoField field, char dirChar)
    /// <summary>
    /// führt einen Bewegungs-Schritt durch (laufen oder Kiste schieben) und gibt true zurück, wenn der Zug erfolgreich war
    /// </summary>
    /// <param name="field">Spielfeld, welches betroffen ist</param>
    /// <param name="dirChar">Bewegungsrichtung ('l' = links, 'r' = rechts, 'u' = hoch, 'd' = runter)</param>
    public static bool SafeMove(this ISokoField field, char dirChar)
    {
      var moveTypes = field.GetMoveTypes();
      switch (dirChar)
      {
        case 'l': if ((moveTypes & MoveType.Left) == MoveType.None) return false; field.Move(moveTypes & MoveType.LeftPush); return true;
        case 'r': if ((moveTypes & MoveType.Right) == MoveType.None) return false; field.Move(moveTypes & MoveType.RightPush); return true;
        case 'u': if ((moveTypes & MoveType.Up) == MoveType.None) return false; field.Move(moveTypes & MoveType.UpPush); return true;
        case 'd': if ((moveTypes & MoveType.Down) == MoveType.None) return false; field.Move(moveTypes & MoveType.DownPush); return true;
        default: throw new Exception("invalid dirChar: '" + dirChar + "'");
      }
    }
    #endregion

    /// <summary>
    /// prüft, ob eine bestimmte Position sich in einer Ecke befindet
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn es sich um eine Ecke handelt</returns>
    public static bool CheckCorner(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height || field.GetFieldChar(pos) == '#') return true; // ungültige Felder zählen auch als "Ecken"
      bool left = field.GetFieldChar(pos - 1) == '#';
      bool right = field.GetFieldChar(pos + 1) == '#';
      bool top = field.GetFieldChar(pos - field.Width) == '#';
      bool bottom = field.GetFieldChar(pos + field.Width) == '#';
      return top && (left || right) || bottom && (left || right);
    }

    /// <summary>
    /// gibt an, ob an einer bestimmten Position ein Spieler steht
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn ein Spieler sich auf dem Feld befindet</returns>
    public static bool IsPlayer(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      char c = field.GetFieldChar(pos);
      return c == '@' || c == '+';
    }

    /// <summary>
    /// gibt an, ob an einer bestimmten Position sich eine Kiste befindet
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn eine Kiste sich auf dem Feld befindet</returns>
    public static bool IsBox(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      char c = field.GetFieldChar(pos);
      return c == '$' || c == '*';
    }

    /// <summary>
    /// gibt an, ob sich an einer bestimmten Position ein Zielfeld befindet
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn es ein Zielfeld ist</returns>
    public static bool IsGoal(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      char c = field.GetFieldChar(pos);
      return c == '.' || c == '*' || c == '+';
    }

    /// <summary>
    /// prüft, ob ein bestimmtes Feld frei ist
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn das Feld frei ist</returns>
    public static bool IsFree(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      char c = field.GetFieldChar(pos);
      return c == ' ' || c == '.';
    }

    /// <summary>
    /// prüft, ob ein bestimmtes Feld aus einer Wand besteht
    /// </summary>
    /// <param name="field">Spielfeld, welches abgefragt werden soll</param>
    /// <param name="pos">Position, welche geprüft werden soll</param>
    /// <returns>true, wenn das Feld eine Wand darstellt</returns>
    public static bool IsWall(this ISokoField field, int pos)
    {
      if (pos < 0 || pos >= field.Width * field.Height) return false;
      return field.GetFieldChar(pos) == '#';
    }
  }
}
