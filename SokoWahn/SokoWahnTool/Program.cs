#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SokoWahnLib;
#endregion

namespace SokoWahnTool
{
  static class Program
  {
    static void Main(string[] args)
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

      ISokoField iField = field;
    }
  }
}
