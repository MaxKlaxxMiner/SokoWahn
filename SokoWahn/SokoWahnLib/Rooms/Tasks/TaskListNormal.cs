using System;
using System.Diagnostics;
// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Implementierung einer Aufgaben-Liste
  /// </summary>
  public class TaskListNormal : TaskList
  {
    /// <summary>
    /// merkt sich die Daten der Aufgaben
    /// </summary>
    ulong[] taskData;
    /// <summary>
    /// merkt sich die Anzahl der tatsächlich gespeicherten Aufgaben
    /// </summary>
    ulong taskCount;
    /// <summary>
    /// merkt sich die Lese-Position am Anfang der Aufgaben-Liste
    /// </summary>
    ulong taskPos;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public TaskListNormal(uint taskLength)
      : base(taskLength)
    {
      taskData = new ulong[taskLength];
    }

    /// <summary>
    /// fügt eine weitere Aufgabe hinzu
    /// </summary>
    /// <param name="newTask">Array mit den Daten der Aufgabe</param>
    public override void Add(ulong[] newTask)
    {
      Debug.Assert(taskLength <= newTask.Length);

      ulong taskOffset = taskCount++ * taskLength;
      if (taskOffset == (uint)taskData.Length)
      {
        Array.Resize(ref taskData, taskData.Length * 2); // Array vergrößern
      }

      for (ulong i = 0; i < taskLength; i++) taskData[taskOffset + i] = newTask[i];
    }

    /// <summary>
    /// liest die erste Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool FetchFirst(ulong[] readTask)
    {
      Debug.Assert(taskLength <= readTask.Length);
      if (taskPos == taskCount) return false; // keine Aufgaben zum Lesen vorhanden?

      ulong taskOffset = taskPos++ * taskLength;
      for (ulong i = 0; i < taskLength; i++) readTask[i] = taskData[taskOffset + i];

      return true;
    }

    /// <summary>
    /// liest die erste Aufgabe, belässt diese aber in der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool PeekFirst(ulong[] readTask)
    {
      if (!FetchFirst(readTask)) return false;
      taskPos--; // Vorwärtszählung rückgängig machen
      return true;
    }

    /// <summary>
    /// liest die letzte Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool FetchLast(ulong[] readTask)
    {
      Debug.Assert(taskLength <= readTask.Length);
      if (taskPos == taskCount) return false; // keine Aufgaben zum Lesen vorhanden?

      ulong taskOffset = --taskCount * taskLength;
      for (ulong i = 0; i < taskLength; i++) readTask[i] = taskData[taskOffset + i];

      return true;
    }

    /// <summary>
    /// liest die letzte Aufgabe, belässt diese aber in der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool PeekLast(ulong[] readTask)
    {
      if (!FetchLast(readTask)) return false;
      taskCount++; // Abwärtszählung rückgängig machen
      return true;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Aufgaben zurück
    /// </summary>
    public override ulong Count
    {
      get { return taskCount - taskPos; }
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose() { }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { Count, taskPos, taskCount }.ToString();
    }
  }
}
