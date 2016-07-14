using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SokoWahn
{
  public class TestData
  {
    /// <summary>
    /// 78 moves, 17 pushes - LrRluulldlddrUUUUdddlllluurururRRlddrruUUdddrrdrddlUUUUdddrrrruulululLLrddlluU
    /// </summary>
    const string Level1 = "       ###  ^_^  \n"
                        + "       #.#       \n"
                        + "   #####.#####   \n"
                        + "  ##         ##  \n"
                        + " ##  # # # #  ## \n"
                        + " #  ##     ##  # \n"
                        + " # ##  # #  ## #     \n"
                        + " #     $@$     # \n"
                        + " ####  ###  #### \n"
                        + "    #### #### :) \n";

    /// <summary>
    /// 115 moves, 20 pushes - dlllddRuurrddrrddllldlluRUUluurrrddrrddllLdlUrrrruullDuuulllddrRlluurrrdDllUluRRurDllddrrrrddlLLdlluRUUrrrrddllLdlU
    /// </summary>
    const string Level2 = "  ####\n"
                        + "### @#\n"
                        + "#    #\n"
                        + "# .#.###\n"
                        + "# $    #\n"
                        + "##*#*# #\n"
                        + "# $    #\n"
                        + "#   ####\n"
                        + "#####";

    /// <summary>
    /// 230 moves, 97 pushes - ullluuuLUllDlldddrRRRRRRRRRRurDllllllluuululldDDuulldddrRRRRRRRRRRdRRlUllllllluuulLulDDDuulldddrRRRRRRRRRRRRlllllllllllllulldRRRRRRRRRRRRRuRRlDllllllluuulluuurDDuullDDDDDuulldddrRRRRRRRRRRRllllllluuuLLulDDDuulldddrRRRRRRRRRRdRUluR
    /// </summary>
    const string Level3 = "    #####\n"
                        + "    #   #\n"
                        + "    #$  #\n"
                        + "  ###  $##\n"
                        + "  #  $ $ #\n"
                        + "### # ## #   ######\n"
                        + "#   # ## #####  ..#\n"
                        + "# $  $          ..#\n"
                        + "##### ### #@##  ..#\n"
                        + "    #     #########\n"
                        + "    #######";

    /// <summary>
    /// 206 moves, 59 pushes - uuuuuruulllddddddLdlUUUUUUddddrruuuuurrrddlLrddddLLLdlUUUUUdddrruuUrrruulllDDDDDuuurrddddlLLdlUUUUdddrrrrdrruurrdLruuuuLLLLLrruullldDDDDuuurrddddlLLdlUUUddrrrrdrrUUUUddrruuulLLLLrruullldDDDDuuurrddddlLLdlUU
    /// </summary>
    const string Level4 = "########\n"
                        + "#.#    #\n"
                        + "#.# ## ####\n"
                        + "#.# $   $ #\n"
                        + "#.# # # # #\n"
                        + "#.# # # # #\n"
                        + "#   # #   #\n"
                        + "# $ $ # $ #\n"
                        + "#   #@  ###\n"
                        + "#########\n";

    /// <summary>
    /// 361 moves, 139 pushes - llllulldRRRRRRRuuuuluurDDDDDDldRRRRurDlllluuuuuurrrurrdLLLLulDDDDDDldRRRlluuuuuurrrdddrUUruLLLLulDDDDDDRRRDullllllllluuuuulllddRRlluurrrdDDDldRRRRRRdRRRRlluulDldRRRlluuuuuurrrrrddrrurrdLLLLdlUUruLLLLulDDDDDDldRRullllllluuulllddRRlluurrrdDldRRRRRRRRRDulllllllluuuuurrrddlUruLLruuullddDDDDDldRRRRRRRRluuuuurrrrrddrrrddddddrrdLdlUUUUUUUruLLLLdlUUruLLLLulDDDDDDldRR
    /// </summary>
    const string Level5 = "   #####\n"
                        + "   #   #\n"
                        + "   # # ##########\n"
                        + "#### #  #   #   #\n"
                        + "#       # $   $ #####\n"
                        + "# ## #$ #   #   #   #\n"
                        + "# $  #  ## ## $   $ #\n"
                        + "# ## #####  #   #   #\n"
                        + "# $   #  #  ###### ##\n"
                        + "### $    @  ...# # #\n"
                        + "  #   #     ...# # #\n"
                        + "  #####  ###...# # ###\n"
                        + "      #### ##### #   #\n"
                        + "                 # $ #\n"
                        + "                 #   #\n"
                        + "                 #####\n";

    /// <summary>
    /// 1121 moves, 303 pushes - dllluullldddldldldldllURURURURUdldldldlluRuRuRuRuRRurrdLddLdLdLdLdlluuururururRlldldldldddrruLUlldRdrruruLrrururuUllldldDRURUdlluRuRRuRRRRddddddrrrdrruuuuuuuullllDDullldLddLLLuurRllddrrruUluRRRRddddddrrruuuulLrrdddddrruuuuuuuullllDDulllldddllUluRRlddrruUluRRRRdDRdLdddrrruuuuLLrrdddddrruuuuuuuullllDDullllldllddddrUUUluRRlddrruUluRRRRdDDDlddrrrruuuulLrrdddddrruuuuuuuullllDDullllldlldddddldlluuuRRRdrUUUluRRlddrruUluRRRRdDlDDrDLdRRRuruuulLdLdddrrdrrruuuuuuuullllDDullllldlldddlluRRdrUUluRRlddrruUluRRRRdDDulDDDrdLrrruruuulLdlDDldRddrrurrruuuuuuuullllDDullllldlldddddlUUluRRdrUUluRRlddrruUluRRRRdDDDDlddrdrruuuruuulLrrdddlddrrruuuuuuuullllDDulllldddllllldldRRdrUUldddlUUluRRRdrUUUluRRlddrruUluRRRRdDDlddddrdrruuuruuulLrrdddlddrrruuuuuuuullllDDullllldlldldRdrUUluRRlddrruUluRRRRdDldddddrdrruuuruuulLdLrurrdddlddrrruuuuuuuullllDldDrrrdddDldRRdrUUUUUUUUdddddddllldlluluurDRRurDldRRdrUUUUUUUddddddllldllUluRRRurDldRRdrUUUUUUdddddlllullldlluRRRRRurDldRRdrUUUUUddddlluuuuullluurDldlDDDulDDldRRluRRRRurDldRRdrUUUUdddllldllUluRRRurDldRRdrUUUddlluuuuulLdLDDldRRRurDldRRdrUUdlluuuuullulDDDDldRRRurDldRRdrUlluuuuullllDDDldRRRRurDldRR
    /// </summary>
    const string Level6 = "           #######\n"
                        + "      ######    .#\n"
                        + "    ###      ###.#\n"
                        + "   ##     #  #@#.#\n"
                        + "  ##  $# #     #.#\n"
                        + " ##  $$  #   # #.#\n"
                        + "##  $$  #   ## #.#\n"
                        + "#  $$  ##   #  #.#\n"
                        + "# $$  ##       #.#\n"
                        + "##   ###    #   .#\n"
                        + " ##### ####   #  #\n"
                        + "          ########\n";

    /// <summary>
    /// 229 moves, 76 pushes - RRRUUluRRRdddLdlUUUluRllluurrDullddrRdrddrruuuuuluurDDDDDuuuurrrddlUrdddlULrUruuLLLulDrddDrrrdddLUUddLLLdlUUdlllUUUrruullDDDDldRRRRRRRlllllluuuuurruRRurDDurrrddlUruLLrrdddLLrrddddlUUUruLdddllllllluuuuurrurrurDlllddLulDDDDldRRRRRR
    /// </summary>
    const string Level7 = "     ####   \n"
                        + "   ###  ####\n"
                        + " ### $ $   #\n"
                        + " #   #..#$ #\n"
                        + " # $$#*.#  #\n"
                        + " #   ....$ #\n"
                        + "## # .#.#  #\n"
                        + "# $##$#.#$ #\n"
                        + "# @$    .$ #\n"
                        + "##  #  ##  #\n"
                        + " ###########\n";

    /// <summary>
    /// extrem
    /// </summary>
    const string Level8 = "####################\n"
                        + "#                  #\n"
                        + "# $  $ $ $ $ $ $ $ #\n"
                        + "# $$$$$###########################################\n"
                        + "#                         .                      #\n"
                        + "# $$$$$#  $ $ $ $ $ ########################## # #\n"
                        + "#      #  $ $ $ $ $ #   $  $  $  $  $  $  $    # #\n"
                        + "# $$$$$#  ### # # ## #                       $   #\n"
                        + "#      #  #        #  # ##################### ## #\n"
                        + "# $$$$$#  #### ## ##$ #   #                 # #  #\n"
                        + "#      #     # #  #   # # .                 # #  #\n"
                        + "# $$$$$#  $$$# #  #   # # ################ ## #  #\n"
                        + "#      #     # #  # $ # #                #  #$#  #\n"
                        + "# $$$$$#  $$$# #  #   # # ############## #  # #  #\n"
                        + "#      #     # #  #   # #.#............# #  # #  #\n"
                        + "# $$$$$#  $$$# #  # $ # #.#............# #  # #  #\n"
                        + "#      #     # #  #   # #.#............# #  #    #\n"
                        + "# $$$$$#  $$$# #  #   # #.#............# #  #$#  #\n"
                        + "#      #     # #  # $ # #.#............# #  # #  #\n"
                        + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                        + "#      #     # #  #   # #.#.........   # #  # #  #\n"
                        + "#@$$$$$#  $$$# #  # $ #  ..............# #  #    #\n"
                        + "#      #     # #  #   # #.#.........  .# #  #$#  #\n"
                        + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                        + "#      #     # #  # $ # #.#............# #  # #  #\n"
                        + "# $$$$$#  $$$# #  #   # #.#............# #  # #  #\n"
                        + "#      #     # #  #   # #.#............# #  #    #\n"
                        + "# $$$$$#  $$$# #  # $ # #.#............# #  #$#  #\n"
                        + "#      #     # #  #   # # #............# #  # #  #\n"
                        + "# $$$$$#  $$$# #  #   # # ################  # #  #\n"
                        + "#      #     # #  # # # #                #  # #  #\n"
                        + "# $$$$$#  $$$# #  # $                       #    #\n"
                        + "#      #     # #  ## # ###################$##$#  #\n"
                        + "# $$$$$#  #    #     #                   # ## #  #\n"
                        + "#      #  #### ##### #  $$ $$ $$ $$ $$ $$   # #  #\n"
                        + "# $$$$$#           # # $  $  $  $  $  $  $$ # #  #\n"
                        + "#      #  ###  # # # # $  $  $  $  $  $  $  $ $  #\n"
                        + "# $$$$$######### # # ######################## ## #\n"
                        + "#                #                               #\n"
                        + "#      #       #                                 #\n"
                        + "##################################################\n";
  }
}
