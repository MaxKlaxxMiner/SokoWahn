#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SokoWahnLib;
using SokoWahnLib.Rooms;
using SokoWahnLib.Rooms.Merger;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCommaInInitializer
#endregion

namespace SokoWahnWin
{
  public sealed partial class FormDebugger : Form
  {
    RoomNetwork roomNetwork;

    #region # // --- normale einfache Spielfelder ---
    static readonly SokoField FieldTest1 = new SokoField(@"
      ######
      #    #
      # $@.#
      ######
    ");

    static readonly SokoField FieldTest2 = new SokoField(@"
      ########
      #      #
      # $ $+.#
      ########
    ");

    static readonly SokoField FieldTest3 = new SokoField(@"
         ########
      ####  #   #
      #.   $#   #
      #@     .$ #
      #.   $#   #
      ####  #   #
         ########
    ");

    static readonly SokoField FieldTest4 = new SokoField(@"
           #####
          ##   #
          #    #
        ###    ######
        #.#.# ##.   #
      ### ###  ##   #
      #   #  $  ## ##
      #     $@$     #
      #   #  $  #   #
      ######   ### ##
       #  .## #### #
       #           #
       ##  #########
        ####
    ");

    static readonly SokoField FieldTest5 = new SokoField(@"
        #####     
      ###   ####  
      #@  #    #  
      ### # ## ## 
        #   #   # 
        ##### $ ##
           #  #  #
           #  .  #
           #######
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
    static readonly SokoField FieldBuggy = new SokoField(@"
      ######
      #..*.#
      #.$  #
      ## $ #
      ##$ ##
      #@$ # 
      ##  # 
       ####
    ");

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

    readonly FormSolver formSolver = new FormSolver();

    /// <summary>
    /// Geschwindigkeit der Varianten-Anzeige (Millisekunden pro Schritt)
    /// </summary>
    const int VariantDelay = 300;

    /// <summary>
    /// Kostruktor
    /// </summary>
    public FormDebugger()
    {
      InitializeComponent();
      splitContainer1_Resize(splitContainer1, null);

      fieldDisplay = new FieldDisplay(pictureBoxField);

      //roomNetwork = new RoomNetwork(FieldTest1);       // sehr einfaches Testlevel (eine Kiste, 6 Moves)
      //roomNetwork = new RoomNetwork(FieldTest2);       // sehr einfaches Testlevel (zwei Kisten, 15 Moves)
      //roomNetwork = new RoomNetwork(FieldTest3);       // einfaches Testlevel (drei Kisten, 52 Moves)
      //roomNetwork = new RoomNetwork(FieldTest4);       // leicht lösbares Testlevel (vier Kisten, 83 Moves)
      //roomNetwork = new RoomNetwork(FieldTest5);       // sehr einfaches Testlevel zum Prüfen erster Optimierungsfunktionen (eine Kiste, 21 Moves)
      //roomNetwork = new RoomNetwork(FieldStart);       // Klassik Sokoban 1. Level
      //roomNetwork = new RoomNetwork(Field628);         // bisher nie gefundene Lösung mit 628 Moves
      roomNetwork = new RoomNetwork(FieldMoves105022); // Spielfeld mit über 100k Moves
      //roomNetwork = new RoomNetwork(FieldMonster);     // aufwendiges Spielfeld mit vielen Möglichkeiten
      //roomNetwork = new RoomNetwork(FieldDiamond);     // Diamand geformter Klumpen mit vielen Deadlock-Situaonen
      //roomNetwork = new RoomNetwork(FieldRunner);      // einfach zu lösen, jedoch sehr viele Moves notwendig (rund 50k)

      //roomNetwork = new RoomNetwork(FieldBuggy);         // Spielfeld um Bugs zu lösen

      displaySettings = new DisplaySettings(roomNetwork.field);
    }

    /// <summary>
    /// aktuelle angezeigte Varianten-Position
    /// </summary>
    int variantTime;

    /// <summary>
    /// aktualisiert die Anzeige
    /// </summary>
    void DisplayUpdate()
    {
      if (roomNetwork == null) return; // Räume noch nicht initialisiert?

      #region # // --- Room-Liste erneuern (falls notwendig) ---
      if (listRooms.Items.Count != roomNetwork.rooms.Length)
      {
        int oldSelected = listRooms.SelectedIndex;
        listRooms.BeginUpdate();
        listRooms.Items.Clear();
        for (int i = 0; i < roomNetwork.rooms.Length; i++)
        {
          listRooms.Items.Add("Room " + (i + 1) + " [" + roomNetwork.rooms[i].fieldPosis.Length + "]");
        }
        listRooms.SelectedIndex = Math.Min(oldSelected, roomNetwork.rooms.Length - 1);
        listRooms.EndUpdate();

        displaySettings.hBack = roomNetwork.rooms.Select(room => new Highlight(0x003366, 0.7f, room.fieldPosis)).ToArray();
        displaySettings.hFront = new Highlight[0];

        listStates.BeginUpdate();
        listStates.Items.Clear();
        listStates.EndUpdate();

        textBoxInfo.Text = "Effort: " + roomNetwork.Effort();
      }
      #endregion

      #region # // --- States-Liste erneuern (falls notwendig) ---
      if (listStates.Items.Count == 0 && listRooms.SelectedIndex >= 0)
      {
        listStates.BeginUpdate();
        foreach (int roomIndex in listRooms.SelectedIndices.Cast<int>())
        {
          ulong stateCount = roomNetwork.rooms[roomIndex].stateList.Count;
          ulong startState = roomNetwork.rooms[roomIndex].startState;
          listStates.Items.Add("-- Room " + (roomIndex + 1) + " [" + stateCount.ToString("N0") + "] --");
          for (ulong i = 0; i < stateCount; i++)
          {
            listStates.Items.Add(new StateListItem(roomIndex, i, i == startState));
          }
        }
        listStates.EndUpdate();

        listVariants.BeginUpdate();
        listVariants.Items.Clear();
        listVariants.EndUpdate();
      }
      #endregion

      #region # // --- Variants-Liste erneuern (falls notwendig) ---
      if (listVariants.Items.Count == 0 && listStates.SelectedItem is StateListItem)
      {
        listVariants.BeginUpdate();
        var stateItem = (StateListItem)listStates.SelectedItem;
        var room = roomNetwork.rooms[stateItem.roomIndex];
        var stateList = room.stateList;
        var variantList = room.variantList;
        var incomingPortals = room.incomingPortals;

        // --- Start-Varianten des Raums auflisten ---
        ulong startVariants = room.startVariantCount;
        if (startVariants > 0 && variantList.GetData(0).oldState == stateItem.state)
        {
          listVariants.Items.Add("-- Starts --");

          int variantCount = 0;
          for (ulong variant = 0; variant < startVariants; variant++)
          {
            variantCount++;
            var variantData = variantList.GetData(variant);
            string path = variantData.path;
            if (path != null)
            {
              var el = new VariantPathElement(room.field.PlayerPos, stateList.Get(variantData.oldState)); // Start-Stellung erzeugen
              var variantPath = new List<VariantPathElement> { el };

              foreach (char c in path)
              {
                int newPlayerPos;
                switch (char.ToLower(c))
                {
                  case 'l': newPlayerPos = el.playerPos - 1; break; // nach links
                  case 'r': newPlayerPos = el.playerPos + 1; break; // nach rechts
                  case 'u': newPlayerPos = el.playerPos - room.field.Width; break; // nach oben
                  case 'd': newPlayerPos = el.playerPos + room.field.Width; break; // nach unten
                  default: throw new Exception("invalid Path-Char: " + c);
                }
                if (el.boxes.Contains(newPlayerPos)) // wurde eine Kiste verschoben?
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes.Select(pos => pos == newPlayerPos ? newPlayerPos - el.playerPos + pos : pos).ToArray());
                }
                else
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes);
                }

                variantPath.Add(el);
              }

              if (variantData.oPortalIndexBoxes.Length > 0) path += " > " + string.Join(",", variantData.oPortalIndexBoxes.Select(x => (x + 1) + "" + room.outgoingPortals[x].dirChar));

              listVariants.Items.Add(new VariantListItem("Variant " + (variantData.oPortalIndexPlayer < uint.MaxValue ? variantCount.ToString() : "End") + " -> " + variantData.newState + " (" + path + ")", variantData.newState, variantPath.ToArray()));
            }
            else
            {
              listVariants.Items.Add("Variant " + variantCount);
            }
          }
        }

        // --- Varianten der Portale auflisten ---
        for (int portalIndex = 0; portalIndex < incomingPortals.Length; portalIndex++)
        {
          var portal = incomingPortals[portalIndex];
          listVariants.Items.Add("-- Portal " + (portalIndex + 1) + portal.dirChar + (portal.blockedBox ? " - [BB] --" : " --"));

          var boxState = portal.stateBoxSwap.Get(stateItem.state);
          if (boxState != stateItem.state) // Variante mit reinschiebbarer Kiste vorhanden?
          {
            listVariants.Items.Add(new VariantListItem("Variant B (" + portal.dirChar + ") -> " + boxState, boxState, new[]
            {
              new VariantPathElement(portal.fromPos + portal.fromPos - portal.toPos, room.stateList.Get(stateItem.state).Concat(new [] { portal.fromPos }).ToArray()),
              new VariantPathElement(portal.fromPos, room.stateList.Get(boxState)),
            }));
          }

          int variantCount = 0;
          foreach (ulong variant in portal.variantStateDict.GetVariantSpan(stateItem.state).AsEnumerable())
          {
            variantCount++;
            var variantData = variantList.GetData(variant);
            string path = variantData.path;
            if (path != null)
            {
              var el = new VariantPathElement(portal.fromPos, stateList.Get(variantData.oldState)); // Start-Stellung erzeugen
              var variantPath = new List<VariantPathElement> { el };

              path = portal.dirChar + path; // ersten Schritt durch das eingehende Portal hinzufügen

              foreach (char c in path)
              {
                int newPlayerPos;
                switch (char.ToLower(c))
                {
                  case 'l': newPlayerPos = el.playerPos - 1; break; // nach links
                  case 'r': newPlayerPos = el.playerPos + 1; break; // nach rechts
                  case 'u': newPlayerPos = el.playerPos - room.field.Width; break; // nach oben
                  case 'd': newPlayerPos = el.playerPos + room.field.Width; break; // nach unten
                  default: throw new Exception("invalid Path-Char: " + c);
                }
                if (el.boxes.Contains(newPlayerPos)) // wurde eine Kiste verschoben?
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes.Select(pos => pos == newPlayerPos ? newPlayerPos - el.playerPos + pos : pos).ToArray());
                }
                else
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes);
                }

                variantPath.Add(el);
              }

              if (variantData.oPortalIndexBoxes.Length > 0) path += " > " + string.Join(",", variantData.oPortalIndexBoxes.Select(x => (x + 1) + "" + room.outgoingPortals[x].dirChar));

              listVariants.Items.Add(new VariantListItem("Variant " + (variantData.oPortalIndexPlayer < uint.MaxValue ? variantCount.ToString() : "End") + " -> " + variantData.newState + " (" + path + ")", variantData.newState, variantPath.ToArray()));
            }
            else
            {
              listVariants.Items.Add("Variant " + variantCount);
            }
          }

          if (variantCount == 0 && boxState == stateItem.state)
          {
            listVariants.Items.Add("no variants");
          }
        }
        listVariants.EndUpdate();
      }
      #endregion

      #region # // --- Varianten-Animation (sofern eine ausgewählt wurde) ---
      var listItem = listVariants.SelectedItem as VariantListItem;
      if (listItem != null)
      {
        var variantPath = listItem.variantPath;

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
        Text = "Player: " + string.Join(", ", variantPath.Select(x => x.playerPos)) + " -> " + el.playerPos;
        displaySettings.boxes = el.boxes;
      }
      else Text = "-";
      #endregion

      fieldDisplay.Update(roomNetwork, displaySettings);
    }

    #region # // --- Form-Handling ---
    /// <summary>
    /// Event, wenn die Raum-Auswahl geändert wurde
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void listRooms_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (activeMerge) return;
      activeMerge = true;

      var selectedRooms = listRooms.SelectedIndices.Cast<int>().Select(i => roomNetwork.rooms[i]).ToArray();

      displaySettings.hFront = selectedRooms.Select(room => new Highlight(0x0080ff, 0.7f, room.fieldPosis)).ToArray();

      listStates.BeginUpdate();
      listStates.Items.Clear();
      listStates.EndUpdate();

      displaySettings.boxes = Enumerable.Range(0, roomNetwork.field.Width * roomNetwork.field.Height).Where(roomNetwork.field.IsBox).ToArray();
      displaySettings.playerPos = roomNetwork.field.PlayerPos;

      textBoxInfo.Text = "Effort: " + roomNetwork.Effort(selectedRooms);

      activeMerge = false;
    }

    /// <summary>
    /// Event, wenn ein bestimmter Zustand ausgewählt wurde
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void listStates_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (activeMerge) return;
      activeMerge = true;

      for (int i = 0; i < displaySettings.hFront.Length; i++)
      {
        if (displaySettings.hFront[i].color == 0xffff00)
        {
          displaySettings.hFront[i].color = 0x0080ff;
        }
      }

      if (listStates.SelectedIndex >= 0)
      {
        if (listStates.SelectedItem is StateListItem)
        {
          var selected = (StateListItem)listStates.SelectedItem;
          displaySettings.boxes = roomNetwork.rooms[selected.roomIndex].stateList.Get(selected.state);
          displaySettings.playerPos = -1;
          for (int i = 0; i < displaySettings.hFront.Length; i++)
          {
            if (displaySettings.hFront[i].fields.Contains(roomNetwork.rooms[selected.roomIndex].fieldPosis[0]))
            {
              displaySettings.hFront[i].color = 0xffff00;
            }
          }
        }
        else
        {
          displaySettings.boxes = Enumerable.Range(0, roomNetwork.field.Width * roomNetwork.field.Height).Where(roomNetwork.field.IsBox).ToArray();
          displaySettings.playerPos = roomNetwork.field.PlayerPos;
        }

        listVariants.BeginUpdate();
        listVariants.Items.Clear();
        listVariants.EndUpdate();

        DisplayUpdate();
      }

      activeMerge = false;
    }

    /// <summary>
    /// neue Variante ausgewählt
    /// </summary>
    void listVariants_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (activeMerge) return;
      activeMerge = true;

      variantTime = 0;
      if (listStates.SelectedItem is StateListItem)
      {
        var selected = (StateListItem)listStates.SelectedItem;
        displaySettings.boxes = roomNetwork.rooms[selected.roomIndex].stateList.Get(selected.state);
        displaySettings.playerPos = -1;
        for (int i = 0; i < displaySettings.hFront.Length; i++)
        {
          if (displaySettings.hFront[i].fields.Contains(roomNetwork.rooms[selected.roomIndex].fieldPosis[0]))
          {
            displaySettings.hFront[i].color = 0xffff00;
          }
        }
      }
      else
      {
        displaySettings.boxes = Enumerable.Range(0, roomNetwork.field.Width * roomNetwork.field.Height).Where(roomNetwork.field.IsBox).ToArray();
        displaySettings.playerPos = roomNetwork.field.PlayerPos;
      }

      activeMerge = false;
    }

    /// <summary>
    /// Methode für gedrückte Tasten
    /// </summary>
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
    void timerDisplay_Tick(object sender, EventArgs e)
    {
      if (activeMerge) return;
      if (innerTimer) return;
      innerTimer = true;
      DisplayUpdate();
      innerTimer = false;
    }

    /// <summary>
    /// Event, wenn die Fenstergröße geändert wird
    /// </summary>
    void FormDebugger_Resize(object sender, EventArgs e)
    {
      if (activeMerge) return;
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
    void pictureBoxField_MouseDown(object sender, MouseEventArgs e)
    {
      if (activeMerge) return;
      if (e.Button == MouseButtons.None) return;
      fieldMouseActive = true;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < roomNetwork.rooms.Length; i++) if (roomNetwork.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
        if (roomIndex >= 0)
        {
          if (e.Button == MouseButtons.Left && roomIndex < listRooms.Items.Count) listRooms.SelectedIndices.Add(roomIndex);
          if (e.Button == MouseButtons.Right && roomIndex < listRooms.Items.Count) listRooms.SelectedIndices.Remove(roomIndex);
        }
      }
    }
    /// <summary>
    /// bewegte Maus über das Spielfeld
    /// </summary>
    void pictureBoxField_MouseMove(object sender, MouseEventArgs e)
    {
      if (activeMerge) return;
      if (!fieldMouseActive) return;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < roomNetwork.rooms.Length; i++) if (roomNetwork.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
        if (roomIndex >= 0)
        {
          if (e.Button == MouseButtons.Left && roomIndex < listRooms.Items.Count) listRooms.SelectedIndices.Add(roomIndex);
          if (e.Button == MouseButtons.Right && roomIndex < listRooms.Items.Count) listRooms.SelectedIndices.Remove(roomIndex);
        }
      }
    }
    /// <summary>
    /// Maustaste über dem Spielfeld wieder losgelassen
    /// </summary>
    void pictureBoxField_MouseUp(object sender, MouseEventArgs e)
    {
      fieldMouseActive = false;
    }

    /// <summary>
    /// passt die Größenverhältnisse der Zustandliste und Variantenliste an
    /// </summary>
    void splitContainer1_Resize(object sender, EventArgs e)
    {
      splitContainer1.SplitterDistance = (int)(splitContainer1.ClientSize.Height * 0.618);
    }

    /// <summary>
    /// Button zum öffnen des Lösung-Fensters
    /// </summary>
    void buttonSolver_Click(object sender, EventArgs e)
    {
      if (activeMerge) return;

      formSolver.InitRoomNetwork(roomNetwork);
      formSolver.ShowDialog();
    }

    /// <summary>
    /// Doppelklick in die Varianten-Liste (wählt den nachfolgenden Kistenzustand aus)
    /// </summary>
    void listVariants_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      if (activeMerge) return;

      var listItem = listVariants.SelectedItem as VariantListItem;
      if (listItem != null)
      {
        ulong state = listItem.nextState;
        int index = -1;
        for (int i = 0; i < listStates.Items.Count; i++)
        {
          var item = listStates.Items[i];
          if (!(item is StateListItem)) continue;
          if (((StateListItem)item).state == state) { index = i; break; }
        }
        if (index >= 0) listStates.SelectedIndex = index;
      }
    }
    #endregion

    /// <summary>
    /// merkt sich, ob eine Merge-Vorgang bereits aktiv arbeitet
    /// </summary>
    bool activeMerge;
    /// <summary>
    /// Merge-Button
    /// </summary>
    void buttonMerge_Click(object sender, EventArgs e)
    {
      if (activeMerge) return;
      activeMerge = true;

      var mergeRooms = listRooms.SelectedIndices.Cast<int>().Select(i => roomNetwork.rooms[i]).ToArray();
      if (mergeRooms.Length == 0)
      {
        if (roomNetwork.rooms.Length == 35) // Test-Mode
        {
          mergeRooms = roomNetwork.rooms.Skip(0).Take(2)
               .Concat(roomNetwork.rooms.Skip(8).Take(2))
               .Concat(roomNetwork.rooms.Skip(16).Take(2))
               .Concat(roomNetwork.rooms.Skip(25).Take(2))
               .Concat(roomNetwork.rooms.Skip(30).Take(2))
               .ToArray();
        }
        else if (roomNetwork.rooms.Length == 56) // Test-Mode
        {
          mergeRooms = roomNetwork.rooms.Skip(23).Take(4)
               .Concat(roomNetwork.rooms.Skip(40).Take(4))
               .Concat(roomNetwork.rooms.Skip(47).Take(4)).ToArray();
        }
        else
        {
          mergeRooms = roomNetwork.rooms.ToArray(); // alle Räume mergen, wenn keine ausgewählt wurden
        }
      }

      listRooms.BeginUpdate();
      listRooms.Items.Clear();
      listRooms.EndUpdate();

      var mergeFields = new HashSet<int>(mergeRooms.SelectMany(room => room.fieldPosis));

      bool first = true;
      for (; ; )
      {
        mergeRooms = roomNetwork.rooms.Where(room => room.fieldPosis.Any(pos => mergeFields.Contains(pos))).ToArray();
        var bestRoomConnections = new List<Tuple<ulong, Room, Room>>();
        var useRooms = new HashSet<Room>(mergeRooms);
        foreach (var room in mergeRooms)
        {
          var connectedRooms = room.incomingPortals.Select(iPortal => iPortal.fromRoom).Where(fromRoom => useRooms.Contains(fromRoom)).ToArray();
          foreach (var room2 in connectedRooms)
          {
            if (room.roomIndex > room2.roomIndex) continue; // doppelte Raumverknüpfung vermeiden

            bestRoomConnections.Add(new Tuple<ulong, Room, Room>
            (
              room.stateList.Count * room2.stateList.Count * (ulong)(room.incomingPortals.Length + room2.incomingPortals.Length),
              room,
              room2
            ));
          }
        }

        if (bestRoomConnections.Count == 0) break; // nichts zum Verschmelzen gefunden?

        if (bestRoomConnections.Min(x => x.Item1) > 100) // nur noch Varianten-Aufwand beachten
        {
          bestRoomConnections.Clear();
          foreach (var room in mergeRooms)
          {
            var connectedRooms = room.incomingPortals.Select(iPortal => iPortal.fromRoom).Where(fromRoom => useRooms.Contains(fromRoom)).ToArray();
            foreach (var room2 in connectedRooms)
            {
              if (room.roomIndex > room2.roomIndex) continue; // doppelte Raumverknüpfung vermeiden

              bestRoomConnections.Add(new Tuple<ulong, Room, Room>
              (
                room.variantList.Count * room2.variantList.Count,
                room,
                room2
              ));
            }
          }
        }

        var bestRoomConnection = bestRoomConnections.First();
        foreach (var c in bestRoomConnections)
        {
          if (c.Item1 < bestRoomConnection.Item1) bestRoomConnection = c;
        }
        ulong effort = bestRoomConnection.Item2.variantList.Count * bestRoomConnection.Item3.variantList.Count;

        if (effort > 10000000 && !first) break;
        first = false;

        bool liveView = effort > 100000;
        if (liveView)
        {
          displaySettings.hFront = new[] { bestRoomConnection.Item2, bestRoomConnection.Item3 }.Select(room => new Highlight(0x0080ff, 0.7f, room.fieldPosis)).ToArray();

          for (int i = 0; i < displaySettings.hFront.Length; i++)
          {
            if (displaySettings.hFront[i].fields.Contains(bestRoomConnection.Item2.fieldPosis[0]) || displaySettings.hFront[i].fields.Contains(bestRoomConnection.Item3.fieldPosis[0]))
            {
              displaySettings.hFront[i].color = 0xffff00;
            }
          }
          fieldDisplay.Update(roomNetwork, displaySettings);
          Application.DoEvents();
        }

        Text = "Calc: " + bestRoomConnection.Item1.ToString("N0");

        int oldWidth = pictureBoxField.Width;
        int oldHeight = pictureBoxField.Height;

        Func<string, bool> status = txt =>
        {
          textBoxInfo.Text = txt;
          Application.DoEvents();
          if (oldWidth != pictureBoxField.Width || oldHeight != pictureBoxField.Height)
          {
            fieldDisplay.Update(roomNetwork, displaySettings);
            Application.DoEvents();
            oldWidth = pictureBoxField.Width;
            oldHeight = pictureBoxField.Height;
          }
          return activeMerge;
        };

#if DEBUG
        // die 2 besten Räume verschmelzen
        roomNetwork.MergeRooms(bestRoomConnection.Item2, bestRoomConnection.Item3, status);
        //break;
#else
        // die 2 besten Räume verschmelzen und Zeit messen
        int tick = Environment.TickCount;
        roomNetwork.MergeRooms(bestRoomConnection.Item2, bestRoomConnection.Item3, status);
        if (!activeMerge) return;
        tick = Environment.TickCount - tick;
        if (tick > 10000) break; // Abbruch, wenn das Verschmelzen zweier Räume zu lange gedauert hat
#endif

        if (liveView) DisplayUpdate();
      }

      activeMerge = false;
    }

    /// <summary>
    /// Step-Button
    /// </summary>
    void buttonOptimize_Click(object sender, EventArgs e)
    {
      if (activeMerge) return;
      activeMerge = true;

      var optimizeRooms = listRooms.SelectedIndices.Cast<int>().Select(i => roomNetwork.rooms[i]).ToArray();

      if (roomNetwork.rooms.Length == 35)
      {
        buttonMerge_Click(sender, e);
        optimizeRooms = new[] { roomNetwork.rooms[0] };
      }
      else if (roomNetwork.rooms.Length == 56)
      {
        buttonMerge_Click(sender, e);
        optimizeRooms = new[] { roomNetwork.rooms[23] };
      }

      listRooms.BeginUpdate();
      listRooms.Items.Clear();
      listRooms.EndUpdate();

      int oldWidth = pictureBoxField.Width;
      int oldHeight = pictureBoxField.Height;

      Func<string, bool> status = txt =>
      {
        textBoxInfo.Text = txt;
        Application.DoEvents();
        if (oldWidth != pictureBoxField.Width || oldHeight != pictureBoxField.Height)
        {
          fieldDisplay.Update(roomNetwork, displaySettings);
          Application.DoEvents();
          oldWidth = pictureBoxField.Width;
          oldHeight = pictureBoxField.Height;
        }
        return activeMerge;
      };

      ulong oldValue = 0;
      ulong newValue = 0;
      foreach (var room in optimizeRooms)
      {
        oldValue += room.variantList.Count + room.stateList.Count;

        displaySettings.hFront = new[] { new Highlight(0xffff00, 0.7f, room.fieldPosis) };
        fieldDisplay.Update(roomNetwork, displaySettings);
        Application.DoEvents();

        if (!status("Optimize[" + room.fieldPosis.First() + "]: (1 / 5) init")) return;
        var scanner = new RoomDeadlockScanner(room);

        if (!status("Optimize[" + room.fieldPosis.First() + "]: (2 / 5) reverse map")) return;
        scanner.Step1_CreateReverseMap();

        if (!status("Optimize[" + room.fieldPosis.First() + "]: (3 / 5) scan forward")) return;
        scanner.Step2_ScanForward();

        if (!status("Optimize[" + room.fieldPosis.First() + "]: (4 / 5) scan backward")) return;
        scanner.Step3_ScanBackward();

        if (!status("Optimize[" + room.fieldPosis.First() + "]: (5 / 5) remove unused variants")) return;
        scanner.Step4_RemoveUnusedVariants();

        newValue += room.variantList.Count + room.stateList.Count;
      }

      buttonOptimize.Text = "ok. (" + (oldValue - newValue).ToString("N0") + ")";
      activeMerge = false;
    }

    /// <summary>
    /// Validate-Button
    /// </summary>
    void buttonValidate_Click(object sender, EventArgs e)
    {
      if (activeMerge) return;

      try
      {
        roomNetwork.Validate(true);
        MessageBox.Show("Validate: ok.\r\n\r\n" +
                        "Rooms: " + roomNetwork.rooms.Length.ToString("N0") + "\r\n\r\n" +
                        "States: " + roomNetwork.rooms.Sum(room => (double)room.stateList.Count).ToString("N0") + "\r\n\r\n" +
                        "Variants: " + roomNetwork.rooms.Sum(room => (double)room.variantList.Count).ToString("N0"),
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception exc)
      {
        MessageBox.Show(exc.ToString().Replace("System.Exception: ", ""), "Validation", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    /// <summary>
    /// gibt das Varianten-Element zurück, wenn nur mit der Maus drauf gezeigt wird (nicht geklickt)
    /// </summary>
    /// <returns>gehovertes Element (oder null)</returns>
    VariantListItem DetermineHoveredItem()
    {
      if (activeMerge) return null;

      var screenPosition = MousePosition;
      var listBoxClientAreaPosition = listVariants.PointToClient(screenPosition);

      int hoveredIndex = listVariants.IndexFromPoint(listBoxClientAreaPosition);
      if (hoveredIndex != -1)
      {
        return listVariants.Items[hoveredIndex] as VariantListItem;
      }
      return null;
    }

    void FormDebugger_Load(object sender, EventArgs e)
    {
      //buttonSolver_Click(sender, e);
    }

    void FormDebugger_FormClosed(object sender, FormClosedEventArgs e)
    {
      activeMerge = false;
    }
  }
}
