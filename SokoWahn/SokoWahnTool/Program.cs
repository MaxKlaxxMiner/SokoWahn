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

      //var solver = new RoomSolver(FieldTest1);
      //var solver = new RoomSolver(FieldStart);
      var solver = new RoomSolver(FieldMonster);
      int selectRoom = -1;
      int selectState = -1;
      for (; ; )
      {
        Console.Clear();
        solver.DisplayConsole(selectRoom, selectState);
        var key = Console.ReadKey();
        switch (key.Key)
        {
          case ConsoleKey.Escape: return;

          case ConsoleKey.Add:
          case ConsoleKey.OemPlus:
          {
            selectRoom++;
            if (selectRoom >= solver.rooms.Length) selectRoom = solver.rooms.Length - 1;
            selectState = -1;
          } break;

          case ConsoleKey.Subtract:
          case ConsoleKey.OemMinus:
          {
            selectRoom--;
            if (selectRoom < -1) selectRoom = -1;
            selectState = -1;
          } break;

          case ConsoleKey.DownArrow:
          {
            if (selectRoom < 0) selectRoom = 0;
            selectState++;
            if (selectState >= (int)solver.rooms[selectRoom].stateDataUsed)
            {
              if (selectRoom < solver.rooms.Length - 1)
              {
                selectRoom++;
                selectState = 0;
                break;
              }
              selectState = (int)solver.rooms[selectRoom].stateDataUsed - 1;
            }
          } break;

          case ConsoleKey.UpArrow:
          {
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
              selectState = (int)solver.rooms[selectRoom].stateDataUsed - 1;
            }
          } break;
        }
      }

    }
  }
}
