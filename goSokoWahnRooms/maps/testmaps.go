package maps

// Level 200 von game-sokoban.com (aenigma "soko 01", 78 Moves optimal) -
// von Max als Solver-Referenz vorgeschlagen: super einfach, muss sich ganz
// ohne Mergen lösen lassen
const Map200 = `
      ###
      #.#
  #####.#####
 ##         ##
##  # # # #  ##
#  ##     ##  #
# ##  # #  ## #
#     $@$     #
####  ###  ####
   #### ####
`

// Level 202 von game-sokoban.com (aenigma "soko 03", 83 Moves optimal) -
// Standard-Level der Debug-GUI, wie im C#-Original (FormDebugger.FieldTest4)
const Map202 = `
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
`

// Level 5018 von game-sokoban.com (aenigma "soko 47", 401 Moves optimal) -
// zweites Rooms-Test-Level: 3x3-Gitter aus 2-Portal-Kammern mit Kisten-
// Durchgängen, links der Ziel-Trakt als große Ein-Portal-Kammer (9 Ziele)
const Map5018 = `
      #############
      #   #   #   #
      #   $   $   #
      #   #   #   #
########$#######$##
#...  #   #   #   #
#...  $   $ @ #   #
#...  #   #   #   #
################$##
      #   #   #   #
      #   $   $   #
      #   #   #   #
      #############
`

// Level 5005 von game-sokoban.com (aenigma "soko 18", 628 Moves optimal) -
// Single-Portal-Kammern wie bei 5018, aber als verschachtelte Kette; für
// Brute bislang UNLÖSBAR (zu groß) - der Ernstfall für das Rooms-Konzept.
// Achtung: der Spieler startet im Ziel-Trakt (das "+" oben links)
const Map5005 = `
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
`

// Level 37708 von game-sokoban.com (ABHT 01 "level 46", 1605 Moves optimal) -
// drittes Rooms-Test-Level: keine Kammer-Struktur mehr, sondern eine
// diagonale Ziel-Treppe oben links (Kisten teils schon auf Zielen) und ein
// dichtes Kisten-Feld darunter
const Map37708 = `
        ##########
       ##   #    #
      ##....$ #$ #
     ##..*.#     #
 #####..*.##  ####
 #  #..*.#       #
 # $#.*.#   #  # #
 #  ##+###       #
## $ $ $ #  #    #
#  # $ $ # ###  ##
#   $ $$ #  $    #
#  #$# $ #       #
##   $ # ####    #
 ####  $ #  ######
    #    #
    ######
`

const MapVanilla = `
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
`
