#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SokoWahnLib.Rooms;
#endregion

namespace SokoWahnWin
{
  public sealed partial class FormSolver : Form
  {
    RoomNetwork roomNetwork;
    RoomSolver roomSolver;
    DisplaySettings displaySettings;
    readonly FieldDisplay fieldDisplay;

    /// <summary>
    /// Geschwindigkeit der Varianten-Anzeige (Millisekunden pro Schritt)
    /// </summary>
    const int VariantDelay = 300;

    /// <summary>
    /// aktuelle angezeigte Varianten-Position
    /// </summary>
    int variantTime;

    /// <summary>
    /// merkt sich die aktuelle Variante
    /// </summary>
    VariantPathElement[] variantPath;

    /// <summary>
    /// setzt ein neues Spielfeld-Netzwerk zum Lösen
    /// </summary>
    /// <param name="roomNetwork">Netzwerk, welches gelöst werden soll</param>
    public void InitRoomNetwork(RoomNetwork roomNetwork)
    {
      if (ReferenceEquals(this.roomNetwork, roomNetwork)) return; // gleiches Spielfeld/Netzwerk?

      this.roomNetwork = roomNetwork;
      roomSolver = new RoomSolver(roomNetwork, () =>
      {
        UpdateSolverDisplay();
        DisplayUpdate();
        Application.DoEvents();
      });
      displaySettings = new DisplaySettings(roomNetwork.field);
      UpdateSolverDisplay();
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    public FormSolver()
    {
      InitializeComponent();

      fieldDisplay = new FieldDisplay(pictureBoxField);

#if !DEBUG
      textBoxTicks.Text = "1000";
#endif
    }

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    void DisplayUpdate()
    {
      if (roomNetwork == null) return; // Räume noch nicht initialisiert?

      if (variantPath != null)
      {
        int time = Environment.TickCount;

        int timePos = (time - variantTime) / VariantDelay;
        if (timePos < 0)
        {
          variantTime = time;
          timePos = 0;
        }
        if (timePos >= variantPath.Length)
        {
          if (timePos > variantPath.Length + 1) variantTime = time;
          if (timePos == variantPath.Length) timePos = variantPath.Length - 1; else timePos = 0;
        }

        var el = variantPath[timePos];
        displaySettings.playerPos = el.playerPos;
        displaySettings.boxes = el.boxes;
      }

      fieldDisplay.Update(roomNetwork, displaySettings);
    }

    /// <summary>
    /// Methode für gedrückte Tasten
    /// </summary>
    void Form_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Escape: Application.Exit(); break;
      }
    }

    /// <summary>
    /// merkt sich, ob ein Bild-Update gerade durchgeführt wird
    /// </summary>
    bool innerTimer;
    /// <summary>
    /// automatischer Timmer für Bild-Updates
    /// </summary>
    void timerDisplay_Tick(object sender, EventArgs e)
    {
      if (innerTimer) return;
      innerTimer = true;
      DisplayUpdate();
      innerTimer = false;
    }

    /// <summary>
    /// Event, wenn die Fenstergröße geändert wird
    /// </summary>
    void FormSolver_Resize(object sender, EventArgs e)
    {
      if (innerTimer) return;
      innerTimer = true;
      DisplayUpdate();
      innerTimer = false;
    }

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    void UpdateSolverDisplay()
    {
      var playerPosis = roomSolver.PlayerPathPosis;
      var el = new VariantPathElement(playerPosis.First(), roomSolver.CurrentBoxIndices); // Start-Stellung erzeugen
      var variantPath = new List<VariantPathElement> { el };
      foreach (int nextPlayerPos in playerPosis)
      {
        if (el.boxes.Any(pos => pos == nextPlayerPos)) // wurde eine Kiste verschoben?
        {
          el = new VariantPathElement(nextPlayerPos, el.boxes.Select(pos => pos == nextPlayerPos ? nextPlayerPos - el.playerPos + pos : pos).ToArray());
        }
        else
        {
          el = new VariantPathElement(nextPlayerPos, el.boxes);
        }
        variantPath.Add(el);
      }
      this.variantPath = variantPath.ToArray();
      displaySettings.playerPos = roomSolver.PlayerPathPosis.First();
      displaySettings.boxes = roomSolver.CurrentBoxIndices;
      //todo
      //if (playerPosis.Length > 1)
      //{
      //  displaySettings.hBack = new[] { new Highlight(0x003366, 0.7f, roomSolver.rooms[roomSolver.GetTaskRoomIndex(roomSolver.currentTask)].fieldPosis) };
      //}
      //else
      {
        displaySettings.hBack = new Highlight[0];
      }
      textBoxLog.Text = roomSolver.ToString();
      textBoxLog.Update();
    }

    /// <summary>
    /// führt einen oder mehrere Lösungsschritte durch
    /// </summary>
    void buttonSolve_Click(object sender, EventArgs e)
    {
      try
      {
        int ticks = int.Parse(textBoxTicks.Text);
        roomSolver.SearchCycle(ticks);
      }
      catch (Exception exc)
      {
        MessageBox.Show(exc.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      UpdateSolverDisplay();
      timerDisplay_Tick(null, null);
    }

    /// <summary>
    /// beim schließen des Fensters, Programm direkt beenden
    /// </summary>
    void FormSolver_FormClosed(object sender, FormClosedEventArgs e)
    {
      Application.Exit();
    }
  }
}
