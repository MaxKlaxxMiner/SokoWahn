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
using SokoWahnLib;
using SokoWahnLib.Rooms;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCommaInInitializer
#endregion

namespace SokoWahnWin
{
  public partial class FormDebugger : Form
  {
    RoomNetwork network;

    #region # // --- normale einfache Spielfelder ---
    static readonly SokoField FieldTest1 = new SokoField(@"
      ######
      #    #
      # $@.#
      ######
    ");

    static readonly SokoField FieldStart = new SokoField(@"
          #####
          #   #
          #$  #
        ###  $##
        #  $ $ #
      ### # ## #   ######
      #   # ## #####  ..#
      # $  $          ..#
      ##### ### #@##  ..#
          #     #########
          #######
    ");
    #endregion

    #region # // --- bisher unlösbare Spielfelder ---
    static readonly SokoField Field628 = new SokoField(@"
      ####################
      #+.... #   #   #   #
      #...   $   $   $   #
      #...   #   #   #   #
      #.   ############$##
      #.  ##  #  #  ##   #
      ##$##   $ $#$      #
      #   #   #  $  ##   #
      #   #####     ######
      #   #   ###$###     
      ##$## $ ##   #      
       # ##   ##   #      
       # #  ####   #      
       #     #######      
       # #     #          
       # ##$## #          
       #    #  #          
       #### # ##          
          #   #           
          #####
    ");

    static readonly SokoField FieldMoves105022 = new SokoField(@"
       #               ###  
      #.###############   # 
      # $                $# 
      #*.**************.**@#
      #                $ $ #
       ################.  # 
                       ###  
    ");

    static readonly SokoField FieldMonster = new SokoField(@"
      ###################################
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $@$ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      #..... $ $ $ $ $   $ $ $ $ $ .....#
      #.....  $ $ $ $ $ $ $ $ $ $  .....#
      ###################################
    ");

    static readonly SokoField FieldDiamond = new SokoField(@"
               #################         
              ##               ##        
             ##  * *$* *$*$*$*  ##       
            ##  *$*$. *..$.$. *  ##      
           ##  *$.$.$* *$.$.$*$*  ##     
          ##  *$.$.$.$* * . . . .  ##    
         ##  *$.$.$.$. * * * .$. *  ##   
        ##  * .*.$. . * * .$# #*# *$ ##  
       ##  *$*$. * *$* * * * * * * .$ ## 
      ##  $$. *$.$. .$*** * # * * * .$ ##
      #  $$. # . * .$* * *$* * *$# * *  #
      # #.$ ##. *.* . .$# . ### .## .$  #
      #  ##  * *$.$* * * . *$.$* * . .$ #
      ## $.#  .$. * + *##$. * * . .$*  ##
       ##  *. $.$. .$. # .$.$. .$.$$. ## 
        ##  **$**$.$.$. .$.$*$***$$. ##  
         ##  #.* *$.$*$.$.$* * * $. ##   
          ##  * $ *$. .$.$* * * $. ##    
           ##  *.$ *$.$.$* * * $. ##     
            ##  *.* *$*#* * * $. ##      
             ## .$.$.$.$.$.$.$  ##       
              ##               ##        
               #################         
    ");

    static readonly SokoField FieldRunner = new SokoField(@"
      ##################################################
      #                                                #
      # $######################################## #### #
      #                                           #  # #
      # $###################### ################# #  # #
      #                         # $               #  # #
      ##### ################### #                 #  # #
      #   # #                   ### $             #  # #
      # $$# # ################################### #  # #
      #   # #                           @      ## #  # #
      # $$# # #### # #########################.   #### #
      #   # #        ############............#  # ##   #
      # $$# ###  # # #                       #  # ##$$ #
      #   # # # ##                           #  # ##   #
      # $$# # # #######                      #  # ##$$ #
      #   # # # #    #.......................#  # ##   #
      # $$# # # #    #.......................#  # ##$$ #
      #   # # # #    #########################  # ##   #
      # $$# # # #                            #  # ##$$ #
      #   # # # ##############################  # ##   #
      # $$# # #                                 # ##$$ #
      #   # # # ###############################    # # #
      # $$# # #                                 #    # #
      #   # # ###################################### # #
      #     #                                      # # #
      # $#  #                                      # # #
      #  # ######################################### # #
      # $# #                                       $ # #
      #  #                                         #   #
      # $# #########################################$# #
      #  #                                             #
      ## #  ######################################## ###
       # ######################################### $ $ #
       # $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $ $   #
       #             #                               ###
       ###############################################  
    ");
    #endregion

    DisplaySettings displaySettings;
    readonly FieldDisplay fieldDisplay;

    /// <summary>
    /// Kostruktor
    /// </summary>
    public FormDebugger()
    {
      InitializeComponent();
      fieldDisplay = new FieldDisplay(pictureBoxField);

      network = new RoomNetwork(FieldTest1);       // sehr einfaches Testlevel
      //network = new RoomNetwork(FieldStart);       // Klassik Sokoban 1. Level
      //network = new RoomNetwork(Field628);         // bisher nie gefundene Lösung mit 628 Moves
      //network = new RoomNetwork(FieldMoves105022); // Spielfeld mit über 100k Moves
      //network = new RoomNetwork(FieldMonster);     // aufwendiges Spielfeld mit viele Möglichkeiten
      //network = new RoomNetwork(FieldDiamond);     // Diamand geformter Klumpen mit vielen Deadlock-Situationen
      //network = new RoomNetwork(FieldRunner);      // einfach zu lösen, jedoch sehr viele Moves notwendig (rund 50k)

      displaySettings = new DisplaySettings(network.field);
    }

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    void DisplayUpdate()
    {
      if (network == null) return; // Räume noch nicht initialisiert?

      #region # // --- Room-Liste erneuern (falls notwendig) ---
      if (listRooms.Items.Count != network.rooms.Length)
      {
        int oldSelected = listRooms.SelectedIndex;
        listRooms.BeginUpdate();
        listRooms.Items.Clear();
        for (int i = 0; i < network.rooms.Length; i++)
        {
          listRooms.Items.Add("Room " + (i + 1) + " [" + network.rooms[i].fieldPosis.Length + "]");
        }
        listRooms.SelectedIndex = Math.Min(oldSelected, network.rooms.Length - 1);
        listRooms.EndUpdate();

        displaySettings.hBack = network.rooms.Select(room => new Highlight(0x003366, 0.7f, room.fieldPosis)).ToArray();

        listStates.BeginUpdate();
        listStates.Items.Clear();
        listStates.EndUpdate();
      }
      #endregion

      #region # // --- States-Liste erneuern (falls notwendig) ---
      if (listStates.Items.Count == 0 && listRooms.SelectedIndex >= 0)
      {
        listStates.BeginUpdate();
        foreach (int roomIndex in listRooms.SelectedIndices.Cast<int>())
        {
          listStates.Items.Add("-- Room " + (roomIndex + 1) + " --");
          ulong stateCount = network.rooms[roomIndex].stateList.Count;
          for (ulong i = 0; i < stateCount; i++)
          {
            listStates.Items.Add(new StateListItem(roomIndex, i));
          }
        }
        listStates.EndUpdate();
      }
      #endregion

      fieldDisplay.Update(network, displaySettings);
    }

    #region # // --- Form-Handling ---
    /// <summary>
    /// Event, wenn die Raum-Auswahl geändert wurde
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void listRooms_SelectedIndexChanged(object sender, EventArgs e)
    {
      displaySettings.hFront = listRooms.SelectedIndices.Cast<int>().Select(i => network.rooms[i])
        .Select(room => new Highlight(0x0080ff, 0.7f, room.fieldPosis)).ToArray();

      listStates.BeginUpdate();
      listStates.Items.Clear();
      listStates.EndUpdate();

      displaySettings.boxes = Enumerable.Range(0, network.field.Width * network.field.Height).Where(network.field.IsBox).ToArray();
      displaySettings.playerPos = network.field.PlayerPos;
    }

    /// <summary>
    /// Event, wenn ein bestimmter Zustand ausgewählt wurde
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void listStates_SelectedIndexChanged(object sender, EventArgs e)
    {
      for (int i = 0; i < displaySettings.hFront.Length; i++)
      {
        if (displaySettings.hFront[i].color == 0xffff00)
        {
          displaySettings.hFront[i].color = 0x0080ff;
        }
      }

      if (listStates.SelectedIndex >= 0)
      {
        if (listStates.Items[listStates.SelectedIndex] is StateListItem)
        {
          var selected = (StateListItem)listStates.Items[listStates.SelectedIndex];
          displaySettings.boxes = network.rooms[selected.roomIndex].stateList.Get(selected.stateId);
          displaySettings.playerPos = -1;
          for (int i = 0; i < displaySettings.hFront.Length; i++)
          {
            if (displaySettings.hFront[i].fields.Contains(network.rooms[selected.roomIndex].fieldPosis[0]))
            {
              displaySettings.hFront[i].color = 0xffff00;
            }
          }
        }
        else
        {
          displaySettings.boxes = Enumerable.Range(0, network.field.Width * network.field.Height).Where(network.field.IsBox).ToArray();
          displaySettings.playerPos = network.field.PlayerPos;
        }
      }
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
    void FormDebugger_Resize(object sender, EventArgs e)
    {
      if (innerTimer) return;
      innerTimer = true;
      DisplayUpdate();
      innerTimer = false;
    }

    /// <summary>
    /// gitb an, ob in das Spielfeld gerade aktiv geklickt wird (Maustaste gehalten)
    /// </summary>
    bool fieldMouseActive;
    /// <summary>
    /// Mausklick in das Spielfeld
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Maus-Infos</param>
    void pictureBoxField_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.None) return;
      fieldMouseActive = true;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < network.rooms.Length; i++) if (network.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
        if (roomIndex >= 0)
        {
          if (e.Button == MouseButtons.Left) listRooms.SelectedIndices.Add(roomIndex);
          if (e.Button == MouseButtons.Right) listRooms.SelectedIndices.Remove(roomIndex);
        }
      }
    }
    /// <summary>
    /// bewegte Maus über das Spielfeld
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Maus-Infos</param>
    void pictureBoxField_MouseMove(object sender, MouseEventArgs e)
    {
      if (!fieldMouseActive) return;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < network.rooms.Length; i++) if (network.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
        if (roomIndex >= 0)
        {
          if (e.Button == MouseButtons.Left) listRooms.SelectedIndices.Add(roomIndex);
          if (e.Button == MouseButtons.Right) listRooms.SelectedIndices.Remove(roomIndex);
        }
      }
    }
    /// <summary>
    /// Maustaste über dem Spielfeld wieder losgelassen
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Maus-Infos</param>
    void pictureBoxField_MouseUp(object sender, MouseEventArgs e)
    {
      fieldMouseActive = false;
    }
    #endregion


  }
}
