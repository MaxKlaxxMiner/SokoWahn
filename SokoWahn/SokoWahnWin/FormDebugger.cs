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

      network = new RoomNetwork(FieldTest1);       // sehr einfaches Testlevel
      //network = new RoomNetwork(FieldStart);       // Klassik Sokoban 1. Level
      //network = new RoomNetwork(Field628);         // bisher nie gefundene Lösung mit 628 Moves
      //network = new RoomNetwork(FieldMoves105022); // Spielfeld mit über 100k Moves
      //network = new RoomNetwork(FieldMonster);     // aufwendiges Spielfeld mit viele Möglichkeiten
      //network = new RoomNetwork(FieldDiamond);     // Diamand geformter Klumpen mit vielen Deadlock-Situaonen
      //network = new RoomNetwork(FieldRunner);      // einfach zu lösen, jedoch sehr viele Moves notwendig (rund 50k)

      displaySettings = new DisplaySettings(network.field);
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

        textBoxInfo.Text = "Effort: " + network.Effort();
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
        var room = network.rooms[stateItem.roomIndex];
        var stateList = room.stateList;
        var variantList = room.variantList;
        var incomingPortals = room.incomingPortals;

        // --- Start-Varianten des Raums auflisten ---
        ulong startVariants = room.startVariantCount;
        if (startVariants > 0 && variantList.GetData(0).oldStateId == stateItem.stateId)
        {
          listVariants.Items.Add("-- Starts --");

          int variantCount = 0;
          for (ulong variantId = 0; variantId < startVariants; variantId++)
          {
            variantCount++;
            var variant = variantList.GetData(variantId);
            string path = variant.path;
            if (path != null)
            {
              var el = new VariantPathElement(room.field.PlayerPos, stateList.Get(variant.oldStateId)); // Start-Stellung erzeugen
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

              listVariants.Items.Add(new VariantListItem("Variant " + variantCount + " (" + path + ")", variantPath.ToArray()));
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

          var boxState = portal.stateBoxSwap.Get(stateItem.stateId);
          if (boxState != stateItem.stateId) // Variante mit reinschiebbarer Kiste vorhanden?
          {
            listVariants.Items.Add(new VariantListItem("Variant B (" + portal.dirChar + ")", new[]
            {
              new VariantPathElement(portal.fromPos + portal.fromPos - portal.toPos, new [] { portal.fromPos }),
              new VariantPathElement(portal.fromPos, new [] { portal.toPos }),
            }));
          }

          int variantCount = 0;
          foreach (ulong variantId in portal.variantStateDict.GetVariants(stateItem.stateId))
          {
            variantCount++;
            var variant = variantList.GetData(variantId);
            string path = variant.path;
            if (path != null)
            {
              var el = new VariantPathElement(portal.fromPos, stateList.Get(variant.oldStateId)); // Start-Stellung erzeugen
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

              listVariants.Items.Add(new VariantListItem("Variant " + variantCount + " (" + path + ")", variantPath.ToArray()));
            }
            else
            {
              listVariants.Items.Add("Variant " + variantCount);
            }
          }

          if (variantCount == 0)
          {
            listVariants.Items.Add("no variants");
          }
        }
        listVariants.EndUpdate();
      }
      #endregion

      #region # // --- Varianten-Animation (sofern eine ausgewählt wurde) ---
      if (listVariants.SelectedItem is VariantListItem)
      {
        var variantPath = ((VariantListItem)listVariants.SelectedItem).variantPath;

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
        if (listStates.SelectedItem is StateListItem)
        {
          var selected = (StateListItem)listStates.SelectedItem;
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

        listVariants.BeginUpdate();
        listVariants.Items.Clear();
        listVariants.EndUpdate();

        DisplayUpdate();
      }
    }

    /// <summary>
    /// neue Variante ausgewählt
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">zusätzliche Event-Infos</param>
    void listVariants_SelectedIndexChanged(object sender, EventArgs e)
    {
      variantTime = 0;
      if (listStates.SelectedItem is StateListItem)
      {
        var selected = (StateListItem)listStates.SelectedItem;
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

    /// <summary>
    /// passt die Größenverhältnisse der Zustandliste und Variantenliste an
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">Event-Infos</param>
    void splitContainer1_Resize(object sender, EventArgs e)
    {
      splitContainer1.SplitterDistance = (int)(splitContainer1.ClientSize.Height * 0.618);
    }
    #endregion

    /// <summary>
    /// erster Optimierungsschritt
    /// </summary>
    /// <param name="room">Raum, welches optimiert werden soll</param>
    /// <returns>true, wenn etwas optimiert werden konnte</returns>
    static bool OptimizeStep1(Room room)
    {
      var stateList = room.stateList;
      var variantList = room.variantList;

      var boxPortals = new HashSet<uint>(); // merkt sich die Portale, wohin Kisten geschoben wurden

      #region # // --- befüllen: boxPortals ---
      // --- alle Varianten prüfen (inkl. Start-Varianten) ---
      for (ulong variantId = 0; variantId < variantList.Count; variantId++)
      {
        var v = variantList.GetData(variantId);
        if (v.boxPortals.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen
        foreach (var boxPortal in v.boxPortals) boxPortals.Add(boxPortal);
      }
      #endregion

      #region # // --- Kisten-Varianten entfernen, wenn der benachbarte Raum keine Kiste aufnehmen kann ---
      using (var killVariants = new Bitter(variantList.Count))
      {
        // --- Start-Varianten prüfen ---
        for (ulong variantId = 0; variantId < room.startVariantCount; variantId++)
        {
          var v = variantList.GetData(variantId);
          if (v.boxPortals.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen

          foreach (var boxPortal in v.boxPortals)
          {
            var oPortal = room.outgoingPortals[boxPortal];
            if (oPortal.stateBoxSwap.Count == 0) // keine Aufnahmemöglichkeit von Kisten erkannt?
            {
              killVariants.SetBit(variantId); // Variante als löschbar markieren
            }
          }
        }

        foreach (var st in stateList)
        {
          if (st.Value.Length == 0) continue; // keine Kisten-Zustände im Raum vorhanden

          // --- Varianten der Portale prüfen ---
          foreach (var portal in room.incomingPortals)
          {
            foreach (var variantId in portal.variantStateDict.GetVariants(st.Key))
            {
              var v = variantList.GetData(variantId);
              if (v.boxPortals.Length == 0) continue; // keine Kiste hat bei dieser Variante den Raum verlassen

              foreach (var boxPortal in v.boxPortals)
              {
                var oPortal = room.outgoingPortals[boxPortal];
                if (oPortal.stateBoxSwap.Count == 0) // keine Aufnahmemöglichkeit von Kisten erkannt?
                {
                  killVariants.SetBit(variantId); // Variante als löschbar markieren
                }
              }
            }
          }
        }

        ulong freeBits = killVariants.CountFreeBits(0);
        if (freeBits < killVariants.Length) // als gelöscht markierte Varianten erkannt?
        {
          throw new NotImplementedException("todo");
        }
      }
      #endregion

      return false;
    }

    /// <summary>
    /// Step-Button
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">Event-Infos</param>
    void button1_Click(object sender, EventArgs e)
    {
      listRooms.BeginUpdate();
      listRooms.Items.Clear();
      listRooms.EndUpdate();

      foreach (var room in network.rooms)
      {
        if (OptimizeStep1(room)) return;
      }
    }

    /// <summary>
    /// Validate-Button
    /// </summary>
    /// <param name="sender">Objekt, welches dieses Event erzeugt hat</param>
    /// <param name="e">Event-Infos</param>
    void buttonValidate_Click(object sender, EventArgs e)
    {
      try
      {
        network.Validate(true);
        MessageBox.Show("Validate: ok.\r\n\r\n" +
                        "Rooms: " + network.rooms.Length.ToString("N0") + "\r\n\r\n" +
                        "States: " + network.rooms.Sum(room => (double)room.stateList.Count).ToString("N0") + "\r\n\r\n" +
                        "Variants: " + network.rooms.Sum(room => (double)room.variantList.Count).ToString("N0"),
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception exc)
      {
        MessageBox.Show(exc.ToString().Replace("System.Exception: ", ""), "Validation", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }
}
