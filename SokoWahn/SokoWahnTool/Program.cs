#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnLib;
using SokoWahnLib.Rooms;
// ReSharper disable UnusedMember.Local
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
#endregion

namespace SokoWahnTool
{
  static partial class Program
  {
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

    static readonly SokoField FieldTest1 = new SokoField(@"
      ######
      #    #
      # $@.#
      ######
    ");

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
    #endregion

    static void Main()
    {
      //MiniGame(FieldTest1);

      var solver = new RoomNetwork(FieldTest1);
      //var solver = new RoomsNetwork(FieldStart);
      //var solver = new RoomsNetwork(FieldMoves105022);
      //var solver = new RoomsNetwork(Field628);
      //var solver = new RoomsNetwork(FieldMonster);

      //todo RoomSearchForward search = null;

      int selectRoom = -1;
      int selectState = -1;  // ausgewählter Zustand (Konflikt mit selectPortal)
      int selectPortal = -1; // ausgewähltes Portal  (Konflikt mit selectState)
      int selectVariant = -1; // ausgewählt Portal-Variante
      int optimizeOffset = 0;
      var lastOptimize = new List<KeyValuePair<string, int>>();
      //todo while (solver.Optimize(100, lastOptimize) > 0) { }
      //todo solver.Merge(0, 1);
      //todo search = new RoomSearchForward(solver); for (; ; ) search.Tick(1);

      for (; ; )
      {
        Console.Clear();
        //todo solver.DisplayConsole(selectRoom, selectState, selectPortal, selectVariant);
        while (lastOptimize.Count > 1 && lastOptimize.Count > (Console.WindowHeight - Console.CursorTop) - 2) { lastOptimize.RemoveAt(0); optimizeOffset++; }
        for (int i = 0; i < lastOptimize.Count; i++)
        {
          var optimizeLine = lastOptimize[i];
          Console.WriteLine("  {0:N0}: {1}, count: {2:N0}", optimizeOffset + i + 1, optimizeLine.Key, optimizeLine.Value);
        }
        var key = Console.ReadKey(true);
        switch (key.Key)
        {
          case ConsoleKey.Escape: return;

          #region # // --- Raum-Auswahl (+/- Tasten) ---
          case ConsoleKey.Add:
          case ConsoleKey.OemPlus: // zum nächsten Raum wechseln
          {
            selectState = -1;
            selectPortal = -1;
            selectVariant = -1;

            selectRoom++;
            //todo if (selectRoom >= solver.rooms.Length) selectRoom = solver.rooms.Length - 1;
          } break;

          case ConsoleKey.Subtract:
          case ConsoleKey.OemMinus: // zum vorherigen Raum wechseln
          {
            selectState = -1;
            selectPortal = -1;
            selectVariant = -1;

            selectRoom--;
            if (selectRoom < -1) selectRoom = -1;
          } break;
          #endregion

          #region # // --- Zustand-Auswahl (Bild-hoch/Bild-unten Tasten) ---
          case ConsoleKey.PageDown: // zum nächsten Zustand wecheln
          {
            selectPortal = -1;
            selectVariant = -1;

            if (selectRoom < 0) selectRoom = 0;
            selectState++;
            //todo if (selectState >= (int)solver.rooms[selectRoom].StateUsed)
            //todo {
            //todo   if (selectRoom < solver.rooms.Length - 1)
            //todo   {
            //todo     selectRoom++;
            //todo     selectState = 0;
            //todo     break;
            //todo   }
            //todo   selectState = (int)solver.rooms[selectRoom].StateUsed - 1;
            //todo }
          } break;

          case ConsoleKey.PageUp: // zum vorherigen Zustand wechseln
          {
            selectPortal = -1;
            selectVariant = -1;

            if (selectRoom < 0) break;
            selectState--;
            if (selectState < 0)
            {
              selectRoom--;
              if (selectRoom < 0)
              {
                selectRoom = -1;
                break;
              }
              //todo selectState = (int)solver.rooms[selectRoom].StateUsed - 1;
            }
          } break;
          #endregion

          #region # // --- Portal-Auswahl (Hoch/Runter Tasten + Leertaste) ---
          case ConsoleKey.DownArrow: // zum nächsten Portal wechseln
          {
            if (selectVariant >= 0 && selectRoom >= 0 && selectPortal >= 0)
            {
              //todo var incomingPortal = solver.rooms[selectRoom].incomingPortals[selectPortal];
              //todo selectVariant++;
              //todo if (selectVariant < incomingPortal.roomToPlayerVariants.Count + incomingPortal.roomToBoxVariants.Count) break;
            }

            selectState = -1;
            selectVariant = -1;

            if (selectRoom < 0) selectRoom = 0;
            selectPortal++;
            //todo if (selectPortal >= solver.rooms[selectRoom].incomingPortals.Length)
            //todo {
            //todo   if (selectRoom < solver.rooms.Length - 1)
            //todo   {
            //todo     selectRoom++;
            //todo     selectPortal = 0;
            //todo     break;
            //todo   }
            //todo   selectPortal = solver.rooms[selectRoom].incomingPortals.Length - 1;
            //todo }
          } break;

          case ConsoleKey.UpArrow: // zum vorherigen Portal wechseln
          {
            if (selectVariant >= 0 && selectRoom >= 0 && selectPortal >= 0)
            {
              selectVariant--;
              break;
            }

            selectState = -1;
            selectVariant = -1;

            if (selectRoom < 0) break;
            selectPortal--;
            if (selectPortal < 0)
            {
              selectRoom--;
              if (selectRoom < 0)
              {
                selectRoom = -1;
                break;
              }
              //todo selectPortal = solver.rooms[selectRoom].incomingPortals.Length - 1;
            }
          } break;

          case ConsoleKey.Spacebar: // durch das Portal zum gegenüberliegenden Raum wechseln
          {
            if (selectRoom < 0 || selectPortal < 0) break;

            //todo var nextRoom = solver.rooms[selectRoom].incomingPortals[selectPortal].roomFrom;
            //todo var nextPortal = solver.rooms[selectRoom].outgoingPortals[selectPortal];

            //todo selectRoom = -1;
            //todo selectVariant = -1;
            //todo for (int r = 0; r < solver.rooms.Length; r++)
            //todo {
            //todo   if (solver.rooms[r] == nextRoom) { selectRoom = r; break; }
            //todo }

            //todo selectPortal = -1;
            //todo for (int p = 0; p < nextRoom.incomingPortals.Length; p++)
            //todo {
            //todo   if (nextRoom.incomingPortals[p] == nextPortal) { selectPortal = p; break; }
            //todo }
          } break;
          #endregion

          #region # // --- Varianten-Auswahl (Links/Rechts Tasten) ---
          case ConsoleKey.RightArrow:
          {
            if (selectRoom < 0 || selectPortal < 0) break;
            //todo var incomingPortal = solver.rooms[selectRoom].incomingPortals[selectPortal];
            //todo selectVariant++;
            //todo if (selectVariant >= incomingPortal.roomToPlayerVariants.Count + incomingPortal.roomToBoxVariants.Count) selectVariant--;
          } break;
          case ConsoleKey.LeftArrow:
          {
            selectVariant = -1;
          } break;
          #endregion

          #region # // --- Optimize ---
          case ConsoleKey.Enter:
          {
            selectRoom = -1;
            selectState = -1;
            selectPortal = -1;
            selectVariant = -1;
            //todo int count = solver.Optimize(1, lastOptimize);
            //todo if (count == 0) lastOptimize.Add(new KeyValuePair<string, int>("no optimizations found", 0));
          } break;
          #endregion

          #region # // --- Suche ---
          case ConsoleKey.NumPad0:
          {
            //todo if (search != null)
            //todo {
            //todo   search.Tick(1);
            //todo }
            //todo else
            //todo {
            //todo   search = new RoomSearchForward(solver);
            //todo }
          } break;
          #endregion

          #region # // --- Merge ---
          case ConsoleKey.Backspace:
          {
            selectRoom = -1;
            selectState = -1;
            selectPortal = -1;
            selectVariant = -1;
            //todo solver.Merge(0, 1);
          } break;
          #endregion
        }
      }

    }
  }
}
