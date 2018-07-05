
// ReSharper disable UnusedMember.Global

namespace SokoWahn
{
  public static class TestData
  {
    /// <summary>
    /// 78 moves, 17 pushes - LrRluulldlddrUUUUdddlllluurururRRlddrruUUdddrrdrddlUUUUdddrrrruulululLLrddlluU
    /// </summary>
    public const string Level1 = "       ###  ^_^  \n"
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
    public const string Level2 = "  ####\n"
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
    public const string Level3 = "    #####\n"
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
    public const string Level4 = "8#\n"
                               + "#.#4 #\n"
                               + "#.# ## 4#\n"
                               + "#.# $3 $ #\n"
                               + "#.# # # # #\n"
                               + "#.# # # # #\n"
                               + "#3 # #3 #\n"
                               + "# $ $ # $ #\n"
                               + "#3 #@  3#\n"
                               + "9#\n";

    /// <summary>
    /// 361 moves, 139 pushes - llllulldRRRRRRRuuuuluurDDDDDDldRRRRurDlllluuuuuurrrurrdLLLLulDDDDDDldRRRlluuuuuurrrdddrUUruLLLLulDDDDDDRRRDullllllllluuuuulllddRRlluurrrdDDDldRRRRRRdRRRRlluulDldRRRlluuuuuurrrrrddrrurrdLLLLdlUUruLLLLulDDDDDDldRRullllllluuulllddRRlluurrrdDldRRRRRRRRRDulllllllluuuuurrrddlUruLLruuullddDDDDDldRRRRRRRRluuuuurrrrrddrrrddddddrrdLdlUUUUUUUruLLLLdlUUruLLLLulDDDDDDldRR
    /// </summary>
    public const string Level5 = "   #####\n"
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
    public const string Level6 = "           #######\n"
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
    public const string Level7 = "     ####   \n"
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
    /// extreme
    /// </summary>
    public const string Level8 = "20#n#18-#n#-$--$-$-$-$-$-$-$-#n#-5$43#n#25-.22-#n#-5$#--$-$-$-$-$-26#-#-#n#6-#--$-$-$-$-$-#3-$--$--$--$--$--$--$4-#-#n#-5$#--3#-#-#-##-#23-$3-#n"
                               + "#6-#--#8-#--#-21#-##-#n#-5$#--4#-##-##$-#3-#17-#-#--#n#6-#5-#-#--#3-#-#-.17-#-#--#n#-5$#--3$#-#--#3-#-#-16#-##-#--#n#6-#5-#-#--#-$-#-#16-#--#$#-"
                               + "-#n#-5$#--3$#-#--#3-#-#-14#-#--#-#--#n#6-#5-#-#--#3-#-#.#12.#-#--#-#--#n#-5$#--3$#-#--#-$-#-#.#12.#-#--#-#--#n#6-#5-#-#--#3-#-#.#12.#-#--#4-#n#-"
                               + "5$#--3$#-#--#3-#-#.#12.#-#--#$#--#n#6-#5-#-#--#-$-#-#.#12.#-#--#-#--#n#-5$#--3$#-#--#3-#-#.#12.#-#--#-#--#n#6-#5-#-#--#3-#-#.#9.3-#-#--#-#--#n#@"
                               + "5$#--3$#-#--#-$-#--14.#-#--#4-#n#6-#5-#-#--#3-#-#.#9.--.#-#--#$#--#n#-5$#--3$#-#--#3-#-#.#12.#-#--#-#--#n#6-#5-#-#--#-$-#-#.#12.#-#--#-#--#n#-5$"
                               + "#--3$#-#--#3-#-#.#12.#-#--#-#--#n#6-#5-#-#--#3-#-#.#12.#-#--#4-#n#-5$#--3$#-#--#-$-#-#.#12.#-#--#$#--#n#6-#5-#-#--#3-#-#-#12.#-#--#-#--#n#-5$#--"
                               + "3$#-#--#3-#-#-16#--#-#--#n#6-#5-#-#--#-#-#-#16-#--#-#--#n#-5$#--3$#-#--#-$23-#4-#n#6-#5-#-#--##-#-19#$##$#--#n#-5$#--#4-#5-#19-#-##-#--#n#6-#--4"
                               + "#-5#-#--$$-$$-$$-$$-$$-$$3-#-#--#n#-5$#11-#-#-$--$--$--$--$--$--$$-#-#--#n#6-#--3#--#-#-#-#-$--$--$--$--$--$--$--$-$--#n#-5$9#-#-#-24#-##-#n#16-"
                               + "#31-#n#6-#7-#33-#n50#n";
  }
}
