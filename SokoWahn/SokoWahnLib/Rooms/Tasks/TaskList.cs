using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// abstrakte Klasse zum Speichern von Aufgaben
  /// </summary>
  public abstract class TaskList : IDisposable
  {
    /// <summary>
    /// gibt die Länge einer einzelnen Aufgabe an (roomCount + 1)
    /// </summary>
    public readonly uint taskLength;

    /// <summary>
    /// Konstruktor
    /// </summary>
    protected TaskList(uint taskLength)
    {
      this.taskLength = taskLength;
    }

    /// <summary>
    /// fügt eine weitere Aufgabe hinzu
    /// </summary>
    /// <param name="newTask">Array mit den Daten der Aufgabe</param>
    public abstract void Add(ulong[] newTask);

    /// <summary>
    /// liest die erste Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public abstract bool FetchFirst(ulong[] readTask);

    /// <summary>
    /// liest die erste Aufgabe, belässt diese aber in der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public abstract bool PeekFirst(ulong[] readTask);

    /// <summary>
    /// liest die letzte Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public abstract bool FetchLast(ulong[] readTask);

    /// <summary>
    /// liest die letzte Aufgabe, belässt diese aber in der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public abstract bool PeekLast(ulong[] readTask);

    /// <summary>
    /// gibt die Anzahl der gespeicherten Aufgaben zurück
    /// </summary>
    public abstract ulong Count { get; }

    #region # // --- Dispose ---
    /// <summary>
    /// Destructor
    /// </summary>
    ~TaskList()
    {
      Dispose();
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public abstract void Dispose();
    #endregion
  }
}
