using System;
using System.Diagnostics;

// ReSharper disable UnusedMember.Global

namespace SokoWahnLib.Rooms
{
  /// <summary>
  /// einfachste Implementierung einer Aufgaben-Liste
  /// </summary>
  public sealed class TaskListNormal : TaskList
  {
    /// <summary>
    /// merkt sich die Daten der Aufgaben
    /// </summary>
    ulong[] taskData;
    /// <summary>
    /// merkt sich die Lese-Position als Aufgaben-Nummer am Anfang der Aufgaben-Liste
    /// </summary>
    ulong taskReadPos;
    /// <summary>
    /// merkt sich die Schreib-Position als Aufgaben-Number am Ende der Aufgaben-Liste
    /// </summary>
    ulong taskWritePos;

    /// <summary>
    /// Konstruktor
    /// </summary>
    public TaskListNormal(uint taskSize)
      : base(taskSize)
    {
      taskData = new ulong[taskSize];
    }

    /// <summary>
    /// fügt eine weitere Aufgabe hinzu
    /// </summary>
    /// <param name="newTask">Array mit den Daten der Aufgabe</param>
    public override void Add(ulong[] newTask)
    {
      Debug.Assert(taskSize == newTask.Length);

      ulong taskOffset = taskWritePos++ * taskSize;
      if (taskOffset == (uint)taskData.Length)
      {
        Array.Resize(ref taskData, taskData.Length * 2); // Array vergrößern
      }

      for (ulong i = 0; i < taskSize; i++) taskData[taskOffset + i] = newTask[i];
    }

    /// <summary>
    /// liest die erste Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool FetchFirst(ulong[] readTask)
    {
      Debug.Assert(taskSize <= readTask.Length);
      if (taskReadPos == taskWritePos) return false; // keine Aufgaben zum Lesen vorhanden?

      ulong taskOffset = taskReadPos++ * taskSize;
      for (ulong i = 0; i < taskSize; i++) readTask[i] = taskData[taskOffset + i];

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
      taskReadPos--; // Vorwärtszählung rückgängig machen
      return true;
    }

    /// <summary>
    /// liest die letzte Aufgabe und entfernt diese aus der Liste
    /// </summary>
    /// <param name="readTask">Array, wohin die Aufgaben-Daten geschrieben werden sollen</param>
    /// <returns>true, wenn die Aufgabe gelesen wurde (sonst false)</returns>
    public override bool FetchLast(ulong[] readTask)
    {
      Debug.Assert(taskSize <= readTask.Length);
      if (taskReadPos == taskWritePos) return false; // keine Aufgaben zum Lesen vorhanden?

      ulong taskOffset = --taskWritePos * taskSize;
      for (ulong i = 0; i < taskSize; i++) readTask[i] = taskData[taskOffset + i];

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
      taskWritePos++; // Abwärtszählung rückgängig machen
      return true;
    }

    /// <summary>
    /// gibt die Anzahl der gespeicherten Aufgaben zurück
    /// </summary>
    public override ulong Count
    {
      get { return taskWritePos - taskReadPos; }
    }

    /// <summary>
    /// gibt die Anzahl der bereits abgefragten Aufgaben zurück (vom Anfang der Liste)
    /// </summary>
    public override ulong CountFetchedFirst
    {
      get { return taskReadPos; }
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public override void Dispose()
    {
      taskData = null;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { Count, taskReadPos, taskWritePos, bufferSize = (taskData.LongLength * sizeof(ulong) / 1024.0).ToString("N1") + " kByte" }.ToString();
    }
  }
}
