#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnLib;
using SokoWahnLib.Rooms;

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

    static void Main()
    {
      //MiniGame(FieldTest1);

      var solver = new RoomSolver(FieldTest1);
      int selectRoom = -1;
      for (; ; )
      {
        Console.Clear();
        solver.DisplayConsole(selectRoom);
        var key = Console.ReadKey();
        switch (key.Key)
        {
          case ConsoleKey.Escape: return;
          case ConsoleKey.Add:
          case ConsoleKey.OemPlus:
          {
            selectRoom++;
            if (selectRoom >= solver.rooms.Count) selectRoom = solver.rooms.Count - 1;
          } break;
          case ConsoleKey.Subtract:
          case ConsoleKey.OemMinus:
          {
            selectRoom--;
            if (selectRoom < -1) selectRoom = -1;
          } break;
        }
      }

    }
  }
}
