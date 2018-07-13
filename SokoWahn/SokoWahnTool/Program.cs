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

      ISokoField iField = field; // nur Interface abfragen

      Console.Clear();
      Console.WriteLine();
      Console.WriteLine("    Size: {0} x {1}", iField.Width, iField.Height);
      Console.WriteLine();
      Console.WriteLine("  Player: {0}, {1}", iField.PlayerPos % iField.Width, iField.PlayerPos / iField.Width);
      Console.WriteLine();
      Console.WriteLine(iField.GetText()); // Spielfeld ausgeben

#if DEBUG
      if (Environment.CommandLine.Contains(".vshost.exe"))
      {
        Console.WriteLine("Press any key to continue . . .");
        Console.ReadKey(false);
      }
#endif
    }
  }
}
