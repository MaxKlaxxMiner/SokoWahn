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
    static void Main()
    {
      var field = new SokoField(@"
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

      //MiniGame(field);

      var solver = new RoomSolver(field);
      solver.DisplayConsole();
      Console.ReadKey();

    }
  }
}
