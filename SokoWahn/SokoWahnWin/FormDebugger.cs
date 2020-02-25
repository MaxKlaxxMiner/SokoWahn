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
      roomNetwork = new RoomNetwork(FieldTest5);       // sehr einfaches Testlevel zum Prüfen erster Optimierungsfunktionen (eine Kiste, 21 Moves)
      //roomNetwork = new RoomNetwork(FieldStart);       // Klassik Sokoban 1. Level
      //roomNetwork = new RoomNetwork(Field628);         // bisher nie gefundene Lösung mit 628 Moves
      //roomNetwork = new RoomNetwork(FieldMoves105022); // Spielfeld mit über 100k Moves
      //roomNetwork = new RoomNetwork(FieldMonster);     // aufwendiges Spielfeld mit vielen Möglichkeiten
      //roomNetwork = new RoomNetwork(FieldDiamond);     // Diamand geformter Klumpen mit vielen Deadlock-Situaonen
      //roomNetwork = new RoomNetwork(FieldRunner);      // einfach zu lösen, jedoch sehr viele Moves notwendig (rund 50k)

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
          listStates.Items.Add("-- Room " + (roomIndex + 1) + " --");
          ulong stateCount = roomNetwork.rooms[roomIndex].stateList.Count;
          for (ulong i = 0; i < stateCount; i++)
          {
            listStates.Items.Add(new StateListItem(roomIndex, i));
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
                if (el.boxes.Any(pos => pos == newPlayerPos)) // wurde eine Kiste verschoben?
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes.Select(pos => pos == newPlayerPos ? newPlayerPos - el.playerPos + pos : pos).ToArray());
                }
                else
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes);
                }

                variantPath.Add(el);
              }

              listVariants.Items.Add(new VariantListItem("Variant " + (variantData.oPortalIndexPlayer < uint.MaxValue ? variantCount.ToString() : "End") + " (" + path + ")", variantPath.ToArray()));
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
          listVariants.Items.Add("-- Portal " + (portalIndex + 1) + " --");

          var boxState = portal.stateBoxSwap.Get(stateItem.state);
          if (boxState != stateItem.state) // Variante mit reinschiebbarer Kiste vorhanden?
          {
            listVariants.Items.Add(new VariantListItem("Variant B (" + portal.dirChar + ")", new[]
            {
              new VariantPathElement(portal.fromPos + portal.fromPos - portal.toPos, new [] { portal.fromPos }),
              new VariantPathElement(portal.fromPos, new [] { portal.toPos }),
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
                if (el.boxes.Any(pos => pos == newPlayerPos)) // wurde eine Kiste verschoben?
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes.Select(pos => pos == newPlayerPos ? newPlayerPos - el.playerPos + pos : pos).ToArray());
                }
                else
                {
                  el = new VariantPathElement(newPlayerPos, el.boxes);
                }

                variantPath.Add(el);
              }

              listVariants.Items.Add(new VariantListItem("Variant " + (variantData.oPortalIndexPlayer < uint.MaxValue ? variantCount.ToString() : "End") + " (" + path + ")", variantPath.ToArray()));
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
      displaySettings.hFront = listRooms.SelectedIndices.Cast<int>().Select(i => roomNetwork.rooms[i])
        .Select(room => new Highlight(0x0080ff, 0.7f, room.fieldPosis)).ToArray();

      listStates.BeginUpdate();
      listStates.Items.Clear();
      listStates.EndUpdate();

      displaySettings.boxes = Enumerable.Range(0, roomNetwork.field.Width * roomNetwork.field.Height).Where(roomNetwork.field.IsBox).ToArray();
      displaySettings.playerPos = roomNetwork.field.PlayerPos;
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
    }

    /// <summary>
    /// neue Variante ausgewählt
    /// </summary>
    void listVariants_SelectedIndexChanged(object sender, EventArgs e)
    {
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
      if (e.Button == MouseButtons.None) return;
      fieldMouseActive = true;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < roomNetwork.rooms.Length; i++) if (roomNetwork.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
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
    void pictureBoxField_MouseMove(object sender, MouseEventArgs e)
    {
      if (!fieldMouseActive) return;
      int pos = fieldDisplay.GetFieldPos(e.X, e.Y);
      if (pos >= 0)
      {
        int roomIndex = -1;
        for (int i = 0; i < roomNetwork.rooms.Length; i++) if (roomNetwork.rooms[i].fieldPosis.Contains(pos)) roomIndex = i;
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
    void pictureBoxField_MouseUp(object sender, MouseEventArgs e)
    {
      fieldMouseActive = false;
    }

    /// <summary>
    /// passt die Größenverhältnisse der Zustandliste und Variantenliste an
    /// </summary>
    void splitContainer1_Resize(object sender, EventArgs e)
    {
      splitContainer1.SplitterDistance = (int)(splitContainer1.ClientSize.Height * (1 - 0.618));
    }
    #endregion

    #region # // --- Optimize ---
    /// <summary>
    /// erster Optimierungsschritt
    /// </summary>
    /// <param name="room">Raum, welcher optimiert werden soll</param>
    /// <returns>true, wenn etwas optimiert werden konnte</returns>
    static bool OptimizeStep1(Room room)
    {
      var stateList = room.stateList;
      var variantList = room.variantList;

      var boxPortals = new HashSet<uint>(); // merkt sich die Portale, wohin Kisten geschoben wurden

      #region # // --- befüllen: boxPortals ---
      // --- alle Varianten prüfen (inkl. Start-Varianten) ---
      for (ulong variant = 0; variant < variantList.Count; variant++)
      {
        var variantData = variantList.GetData(variant);
        if (variantData.oPortalIndexBoxes.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen
        foreach (var boxPortal in variantData.oPortalIndexBoxes) boxPortals.Add(boxPortal);
      }
      #endregion

      #region # // --- Kisten-Varianten entfernen, wenn der benachbarte Raum keine Kiste aufnehmen kann ---
      using (var killVariants = new Bitter(variantList.Count))
      {
        // --- Start-Varianten prüfen ---
        for (ulong variant = 0; variant < room.startVariantCount; variant++)
        {
          var variantData = variantList.GetData(variant);
          if (variantData.oPortalIndexBoxes.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen

          foreach (var boxPortal in variantData.oPortalIndexBoxes)
          {
            var oPortal = room.outgoingPortals[boxPortal];
            if (oPortal.stateBoxSwap.Count == 0) // keine Aufnahmemöglichkeit von Kisten erkannt?
            {
              killVariants.SetBit(variant); // Variante als löschbar markieren
            }
          }
        }

        foreach (var st in stateList)
        {
          if (st.Value.Length == 0) continue; // keine Kisten-Zustände im Raum vorhanden

          // --- Varianten der Portale prüfen ---
          foreach (var portal in room.incomingPortals)
          {
            foreach (var variant in portal.variantStateDict.GetVariantSpan(st.Key).AsEnumerable())
            {
              var variantData = variantList.GetData(variant);
              if (variantData.oPortalIndexBoxes.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen

              foreach (var boxPortal in variantData.oPortalIndexBoxes)
              {
                var oPortal = room.outgoingPortals[boxPortal];
                if (oPortal.stateBoxSwap.Count == 0) // keine Aufnahmemöglichkeit von Kisten erkannt?
                {
                  killVariants.SetBit(variant); // Variante als löschbar markieren
                }
              }
            }
          }
        }

        ulong freeBits = killVariants.CountFreeBits(0);
        if (freeBits < killVariants.Length) // als gelöscht markierte Varianten erkannt?
        {
          var skip = new SkipMapper(killVariants, false);
          RenewVariants(room, skip);
          return true;
        }
      }
      #endregion

      return false;
    }

    /// <summary>
    /// Optimierung: entfernt nicht mehr benutzte/verlinkte Zustände
    /// </summary>
    /// <param name="room">Raum, welcher optimiert werden soll</param>
    /// <returns>true, wenn veraltete Zustände erkannt und entfernt wurden</returns>
    static bool OptimizeUnusedStates(Room room)
    {
      var stateList = room.stateList;
      var variantList = room.variantList;

      using (var usingStates = new Bitter(stateList.Count))
      {
        usingStates.SetBit(0); // ersten Zustand immer pauschal markieren (End-Zustand)
        usingStates.SetBit(room.startState); // Start-Zustand ebenfalls markieren

        // Start-Varianten durchsuchen
        for (ulong variant = 0; variant < room.startVariantCount; variant++)
        {
          Debug.Assert(variant < variantList.Count);
          var v = variantList.GetData(variant);

          Debug.Assert(v.oldState < stateList.Count);
          usingStates.SetBit(v.oldState);

          Debug.Assert(v.newState < stateList.Count);
          usingStates.SetBit(v.newState);
        }

        // Portal-Varianten durchsuchen
        foreach (var portal in room.incomingPortals)
        {
          foreach (var state in portal.variantStateDict.GetAllStates())
          {
            Debug.Assert(state < usingStates.Length);
            foreach (var variant in portal.variantStateDict.GetVariantSpan(state).AsEnumerable())
            {
              Debug.Assert(variant < variantList.Count);
              usingStates.SetBit(state);
            }
          }
        }

        if (usingStates.CountMarkedBits(0) != usingStates.Length)
        {
          var skip = new SkipMapper(usingStates);
          RenewStates(room, skip);
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// entfernt alle Kistenzustände aus einem Raum
    /// </summary>
    /// <param name="room">Raum, welcher bearbeitet werden soll</param>
    /// <returns>true, wenn Kisten-Zustände entfernt wurden</returns>
    static bool RemoveBoxStates(Room room)
    {
      Debug.Assert(room.fieldPosis.All(pos => !room.field.IsGoal(pos)));

      var stateList = room.stateList;
      var variantList = room.variantList;

      using (var usingStates = new Bitter(stateList.Count))
      {
        for (ulong state = 0; state < stateList.Count; state++)
        {
          if (stateList.Get(state).Length == 0) usingStates.SetBit(state);
        }

        if (usingStates.CountMarkedBits(0) != usingStates.Length)
        {
          var skipStates = new SkipMapper(usingStates);
          Debug.Assert(skipStates.map[0] < usingStates.Length);

          using (var usingVariants = new Bitter(variantList.Count))
          {
            for (ulong variant = 0; variant < variantList.Count; variant++)
            {
              var variantData = variantList.GetData(variant);
              if (variantData.oPortalIndexBoxes.Length == 0 && skipStates.map[variantData.oldState] != ulong.MaxValue && skipStates.map[variantData.newState] != ulong.MaxValue)
              {
                usingVariants.SetBit(variant);
              }
            }

            ulong markedBits = usingVariants.CountMarkedBits(0);
            if (markedBits < usingVariants.Length)
            {
              var skipVariants = new SkipMapper(usingVariants);
              RenewVariants(room, skipVariants);
            }
          }

          RenewStates(room, skipStates);
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// erneuert die Varianten im Raum und deren Verlinkungen
    /// </summary>
    /// <param name="room">Raum, welcher bearbeitet werden soll</param>
    /// <param name="skip">Liste mit allen überspringbaren bzw. weiter zu verwendenden Varianten</param>
    static void RenewVariants(Room room, SkipMapper skip)
    {
      Debug.Assert(skip.usedCount > 0);
      Debug.Assert(skip.usedCount < (uint)skip.map.Length);

      // --- Variantenliste neu erstellen und gefiltert befüllen ---
      var oldVariants = room.variantList;
      var newVariants = new VariantListNormal((uint)room.incomingPortals.Length);
      for (ulong variant = 0; variant < oldVariants.Count; variant++)
      {
        ulong map = skip.map[variant];
        if (map == ulong.MaxValue) continue;
        Debug.Assert(map == newVariants.Count);
        var v = oldVariants.GetData(variant);
        newVariants.Add(v.oldState, v.moves, v.pushes, v.oPortalIndexBoxes, v.oPortalIndexPlayer, v.newState, v.path);
      }
      Debug.Assert(newVariants.Count == skip.usedCount);
      oldVariants.Dispose();
      room.variantList = newVariants;

      // --- Start-Varianten neu zählen ---
      ulong newStartVariants = 0;
      for (ulong i = room.startVariantCount - 1; i < room.startVariantCount; i--)
      {
        if (skip.map[i] < ulong.MaxValue) newStartVariants++;
      }
      room.startVariantCount = newStartVariants;

      // --- verlinkte Varianten in den Portalen neu setzen ---
      foreach (var portal in room.incomingPortals)
      {
        // --- variantStateDict übertragen ---
        var oldDict = portal.variantStateDict;
        var newDict = new VariantStateDictNormal(room.stateList, room.variantList);
        foreach (ulong state in oldDict.GetAllStates())
        {
          ulong skipVariants = 0;
          foreach (ulong variant in oldDict.GetVariantSpan(state).AsEnumerable())
          {
            Debug.Assert(variant < (uint)skip.map.Length);
            if (skip.map[variant] == ulong.MaxValue) // Variante wird nicht mehr verwendet?
            {
              skipVariants++;
              continue;
            }
            Debug.Assert(skip.map[variant] < room.variantList.Count);
            newDict.Add(state, skip.map[variant]);
          }
          Debug.Assert(oldDict.GetVariantSpan(state).variantCount == newDict.GetVariantSpan(state).variantCount + skipVariants);
        }
        oldDict.Dispose();
        portal.variantStateDict = newDict;
      }
    }

    /// <summary>
    /// erneuert die Zustände im Raum und deren Verlinkungen
    /// </summary>
    /// <param name="room">Raum, welcher bearbeitet werden soll</param>
    /// <param name="skip">Liste mit allen überpringbaren bzw. weiter zu verwendenden Zuständen</param>
    static void RenewStates(Room room, SkipMapper skip)
    {
      Debug.Assert(skip.usedCount > 0);
      Debug.Assert(skip.usedCount < (uint)skip.map.Length);

      // --- Zustandsliste neu erstellen und gefiltert befüllen ---
      var oldStates = room.stateList;
      var newStates = new StateListNormal(room.fieldPosis, room.goalPosis);
      for (ulong state = 0; state < oldStates.Count; state++)
      {
        ulong map = skip.map[state];
        if (map == ulong.MaxValue) continue;
        Debug.Assert(map == newStates.Count);
        newStates.Add(oldStates.Get(state));
        Debug.Assert(newStates.Get(map).Length == oldStates.Get(state).Length);
      }
      Debug.Assert(newStates.Count == skip.usedCount);
      oldStates.Dispose();
      room.stateList = newStates;

      // --- verlinkte Zustände innerhalb der Varianten neu setzen ---
      var oldVariants = room.variantList;
      var newVariants = new VariantListNormal(oldVariants.portalCount);
      for (ulong variant = 0; variant < oldVariants.Count; variant++)
      {
        var v = oldVariants.GetData(variant);
        Debug.Assert(v.oldState < oldStates.Count);
        Debug.Assert(skip.map[v.oldState] < newStates.Count);
        Debug.Assert(v.newState < oldStates.Count);
        Debug.Assert(skip.map[v.newState] < newStates.Count);
        Debug.Assert(variant == newVariants.Count);
        newVariants.Add(skip.map[v.oldState], v.moves, v.pushes, v.oPortalIndexBoxes, v.oPortalIndexPlayer, skip.map[v.newState], v.path);
      }
      oldVariants.Dispose();
      Debug.Assert(newVariants.Count == oldVariants.Count);

      // --- verlinkte Zustände in den Portalen neu setzen ---
      foreach (var portal in room.incomingPortals)
      {
        // --- stateBoxSwap übertragen ---
        var oldSwap = portal.stateBoxSwap;
        var newSwap = new StateBoxSwapNormal(room.stateList);
        ulong skipSwaps = 0;
        foreach (ulong oldKey in oldSwap.GetAllKeys())
        {
          ulong newKey = skip.map[oldKey];
          if (newKey == ulong.MaxValue) { skipSwaps++; continue; } // nicht mehr gültige Swaps überspringen
          Debug.Assert(newKey < room.stateList.Count);
          ulong oldState = oldSwap.Get(oldKey);
          Debug.Assert(oldState != oldKey);
          ulong newState = skip.map[oldState];
          if (newState == ulong.MaxValue) { skipSwaps++; continue; }  // nicht mehr gültige Swaps überspringen
          Debug.Assert(newState < room.stateList.Count);
          newSwap.Add(newKey, newState);
        }
        Debug.Assert(newSwap.Count + skipSwaps == oldSwap.Count);
        oldSwap.Dispose();
        portal.stateBoxSwap = newSwap;

        // --- variantStateDict übertragen ---
        var oldDict = portal.variantStateDict;
        var newDict = new VariantStateDictNormal(room.stateList, room.variantList);
        foreach (ulong oldState in oldDict.GetAllStates())
        {
          Debug.Assert(oldState < (uint)skip.map.Length);
          ulong newState = skip.map[oldState];
          Debug.Assert(newState < room.stateList.Count);
          foreach (ulong variant in oldDict.GetVariantSpan(oldState).AsEnumerable())
          {
            Debug.Assert(variant < room.variantList.Count);
            newDict.Add(newState, variant);
          }
          Debug.Assert(oldDict.GetVariantSpan(oldState).variantCount == newDict.GetVariantSpan(newState).variantCount);
        }
        Debug.Assert(newDict.GetAllStates().Count() == oldDict.GetAllStates().Count());
        oldDict.Dispose();
        portal.variantStateDict = newDict;
      }
    }
    #endregion

    /// <summary>
    /// Merge-Button
    /// </summary>
    void buttonMerge_Click(object sender, EventArgs e)
    {
      var mergeRooms = listRooms.SelectedIndices.Cast<int>().Select(i => roomNetwork.rooms[i]).ToArray();
      if (mergeRooms.Length == 0)
      {
        mergeRooms = roomNetwork.rooms.Where(x => x.stateList.Count == 1).ToArray();
        if (mergeRooms.Length == 1) mergeRooms = roomNetwork.rooms.ToArray();
      }

      listRooms.BeginUpdate();
      listRooms.Items.Clear();
      listRooms.EndUpdate();

      var mergeFields = new HashSet<int>(mergeRooms.SelectMany(room => room.fieldPosis));

      for (; ; )
      {
        mergeRooms = roomNetwork.rooms.Where(room => room.fieldPosis.Any(pos => mergeFields.Contains(pos))).ToArray();
        var bestRoomConnections = new List<Tuple<BigInteger, Room, Room>>();
        var useRooms = new HashSet<Room>(mergeRooms);
        foreach (var room in mergeRooms)
        {
          var connectedRooms = room.incomingPortals.Select(iPortal => iPortal.fromRoom).Where(fromRoom => useRooms.Contains(fromRoom)).ToArray();
          foreach (var room2 in connectedRooms)
          {
            if (room.roomIndex > room2.roomIndex) continue; // doppelte Raumverknüpfung vermeiden

            bestRoomConnections.Add(new Tuple<BigInteger, Room, Room>
            (
              RoomNetwork.MulNumber(room.incomingPortals.Where(iPortal => iPortal.fromRoom.roomIndex == room2.roomIndex).Select(x => x.variantStateDict.TotalVariantCount)),
              room,
              room2
            ));
          }
        }

        if (bestRoomConnections.Count == 0) return; // nichts zum verschmelzen gefunden?

        var bestRoomConnection = bestRoomConnections.First();
        foreach (var c in bestRoomConnections)
        {
          if (c.Item1 < bestRoomConnection.Item1) bestRoomConnection = c;
        }

#if DEBUG
        // die 2 besten Räume verschmelzen
        roomNetwork.MergeRooms(bestRoomConnection.Item2, bestRoomConnection.Item3);
#else
        // die 2 besten Räume verschmelzen und Zeit messen
        int tick = Environment.TickCount;
        roomNetwork.MergeRooms(bestRoomConnection.Item2, bestRoomConnection.Item3);
        tick = Environment.TickCount - tick;
        if (tick > 100) break; // Abbruch, wenn das Verschmelzen zweier Räume zu lange gedauert hat
#endif
      }
    }

    /// <summary>
    /// Step-Button
    /// </summary>
    void buttonStep_Click(object sender, EventArgs e)
    {
      listRooms.BeginUpdate();
      listRooms.Items.Clear();
      listRooms.EndUpdate();

      // --- Kisten-Netzwerke filtern ---
      var roomsBoxesStart = new HashSet<Room>();  // Räume mit Kisten am Anfang
      var roomsBoxesGoals = new HashSet<Room>();  // Räume mit Kisten-Zielfeldern
      var roomsBoxesPass = new HashSet<Room>();   // Räume mit Kisten
      var roomsBoxesEmpty = new HashSet<Room>();  // Räume ohne Kisten
      var roomsCheck = new Stack<Room>(); // Räume, welche noch geprüft werden müssen

      // --- roomsBoxesStart und roomsBoxesGoals befüllen ---
      foreach (var room in roomNetwork.rooms)
      {
        if (room.startBoxPosis.Length > 0) { roomsBoxesStart.Add(room); roomsCheck.Push(room); }
        if (room.goalPosis.Length > 0) { roomsBoxesGoals.Add(room); roomsCheck.Push(room); }
      }

      // --- roomsBoxesPass befüllen ---
      while (roomsCheck.Count > 0)
      {
        var checkRoom = roomsCheck.Pop();
        if (roomsBoxesPass.Contains(checkRoom)) continue;

        var variantList = checkRoom.variantList;
        var outgoingBoxPortals = new bool[checkRoom.incomingPortals.Length];
        for (ulong variant = 0; variant < variantList.Count; variant++)
        {
          var v = variantList.GetData(variant);
          foreach (uint p in v.oPortalIndexBoxes) outgoingBoxPortals[p] = true;
        }
        if (outgoingBoxPortals.Any(x => x)) roomsBoxesPass.Add(checkRoom);
        for (uint p = 0; p < outgoingBoxPortals.Length; p++)
        {
          if (outgoingBoxPortals[p]) roomsCheck.Push(checkRoom.outgoingPortals[p].toRoom);
        }
      }

      foreach (var room in roomNetwork.rooms)
      {
        if (roomsBoxesStart.Contains(room)) continue;
        if (roomsBoxesGoals.Contains(room)) continue;
        if (roomsBoxesPass.Contains(room)) continue;
        roomsBoxesEmpty.Add(room);
      }

      int roomIndex = 0;
      foreach (var room in roomNetwork.rooms)
      {
        roomIndex++;
        if (roomsBoxesEmpty.Contains(room))
        {
          if (RemoveBoxStates(room))
          {
            buttonStep.Text = "B-Room " + roomIndex;
            return;
          }
        }
        if (OptimizeStep1(room))
        {
          buttonStep.Text = "1-Room " + roomIndex;
          return;
        }
        if (OptimizeUnusedStates(room))
        {
          buttonStep.Text = "S-Room " + roomIndex;
          return;
        }
      }

      buttonStep.Text = "ok.";
    }

    /// <summary>
    /// Validate-Button
    /// </summary>
    void buttonValidate_Click(object sender, EventArgs e)
    {
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
      var screenPosition = MousePosition;
      var listBoxClientAreaPosition = listVariants.PointToClient(screenPosition);

      int hoveredIndex = listVariants.IndexFromPoint(listBoxClientAreaPosition);
      if (hoveredIndex != -1)
      {
        return listVariants.Items[hoveredIndex] as VariantListItem;
      }
      return null;
    }

    /// <summary>
    /// Button zum öffnen des Lösung-Fensters
    /// </summary>
    void buttonSolver_Click(object sender, EventArgs e)
    {
      formSolver.InitRoomNetwork(roomNetwork);
      formSolver.ShowDialog();
    }

    void FormDebugger_Load(object sender, EventArgs e)
    {
      //buttonSolver_Click(sender, e);
    }
  }
}
