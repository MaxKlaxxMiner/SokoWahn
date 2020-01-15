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
    RoomNetwork network;
    DisplaySettings displaySettings;
    readonly FieldDisplay fieldDisplay;

    /// <summary>
    /// setzt ein neues Spielfeld-Netzwerk zum Lösen
    /// </summary>
    /// <param name="network">Netzwerk, welches gelöst werden soll</param>
    public void InitRoomNetwork(RoomNetwork network)
    {
      if (ReferenceEquals(this.network, network)) return; // gleiches Spielfeld/Netzwerk?

      this.network = network;
      displaySettings = new DisplaySettings(network.field);
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    public FormSolver()
    {
      InitializeComponent();

      fieldDisplay = new FieldDisplay(pictureBoxField);
    }

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    void DisplayUpdate()
    {
      if (network == null) return; // Räume noch nicht initialisiert?

      fieldDisplay.Update(network, displaySettings);
    }

    /// <summary>
    /// Methode für gedrückte Tasten
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">Informationen über die gedrückten Tasten</param>
    void Form_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Escape: Close(); break;
      }
    }

    /// <summary>
    /// merkt sich, ob ein Bild-Update gerade durchgeführt wird
    /// </summary>
    bool innerTimer;
    /// <summary>
    /// automatischer Timmer für Bild-Updates
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
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
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void FormSolver_Resize(object sender, EventArgs e)
    {
      if (innerTimer) return;
      innerTimer = true;
      DisplayUpdate();
      innerTimer = false;
    }
  }
}
