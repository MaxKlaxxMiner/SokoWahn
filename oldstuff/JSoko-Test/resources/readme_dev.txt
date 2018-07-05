This program implements the game of Sokoban.

If you want to help improving this program or just want to understand the code,
here is a short description of how the program works:

The main class is JSoko. This class holds references to all important other
objects of the program.
In the method "startProgram" the whole program is started.
Every important class is instanciated here and a reference to the created
instance is hold. One can see that the main class "JSoko" is passed as a
parameter to most of the other classes, for instance:
    leveldata = new LevelsIO(this);
where "this" is a reference to the "JSoko" object.

This way all the other objects have access to all other objects through the
main class. The instance of JSoko is labeled "mainObject" or "application"
in the other classes. That means a method in LevelsIO may access other objects
of the program by using this mainObject, for instance:
    mainObject.redraw(false);  
for letting the program redraw itself and showing any new information.


The most important classes and their functions:

JSoko:
Main class of the program. This class holds a reference to every other
important class of the program. A reference to this class is passed to every
important class in the program. Hence, all other objects have access to the
public variables of JSoko.
This class handles all important events (mouse + key events and fired actions).

GUI: 
This class manages every graphical output of the program (there are some
classes which display extra information in new windows, though).
It is used for: loading and displaying the graphics and displaying text
information in a status bar. This class also handles all events that are only
GUI specific like changing the displayed skin or rotating the displayed board.

LevelsIO:
This classes manages all level data. That is:
- loading levels from and saving levels to the disk.
- extract level data (from a level file or the clipboard)
- managing the level data in the RAM

Board:
This class contains the board data of the game and methods for modifying them
(mostly setting / removing walls, goals and boxes on the board).
There are some inner classes inside "Board" which are used to determine the
reachable squares of the player or a box, for calculating the distances between
specific board squares and for identifying illegal squares (squares a box must
not be pushed onto).


Those classes are the main classes which are very important for the game.
Here is a quick description of the other classes:

BoxData: "Help class" for the Board class for storing information about the
box positions.

PositionStorage: Stores boardPositions. A boardPosition includes: all box
positions and the player position.

Settings: Manages global settings of the game and holds the most important
constants that are used in the program.

Sound: Class for playing sounds.

Package "boardpositions" and subpackages:
This package contains several classes for storing boardPositions
(boardPosition = all box positions + player position). Every one of them stores
some other extra information besides the positions of the boxes and the player.

Package "deadlockdetection":
This package contains classes that are used to detect situations where a level
cannot be solved anymore (-> deadlock situation).

Package / class "editor":
This class implements a editor functionality which lets the user edit a level.

Package "GUI":
This package contains of all classes that are used for displaying something.
- AboutBox: displays the information shown when the user presses
  "about this program" menu item
- GUI: paints all graphic. Main class for the graphical output.
- Transformation: Used for all transformations (rotating, mirroring the board)

Package "leveldata":
- Database: This class offers the functionality for storing the level data
  in a database.
- History: Stores the movement history for being able to do undo/redo.
  + HistoryElement: help class for "History" class
- LevelData: Class which stores all data for a specific level
- LevelManagement: This class is used when the database is used for managing
  all levels that are stored in the database.
- LevelsIO: loading and saving of level data. Also manages all currently loaded
  level data.
- Solutions: Manages all solutions of a specific level. It also displays the
  solutions in a new window if necessary.

Package "lowerBoundCalculation":
This package contains classes for calculating a pushes lower bound, that is:
how many pushes is the user - at least! -  away from solving the current level.

Package "optimizer":
This package contains classes for optimizing a solution of a specific level.

Package "solver":
Contains classes that try to solve a level. Every class implements another
method for solving a level.



For understanding the program it maybe a good idea to see how it reacts to
specific input events. The events are handled here:
First the class "GUI" handles events. All mouse and key events are passed to
the main class "JSoko".
GUI only handles those actions that deal with the graphic output like:
rotating the display of the board, setting a new skin, ...
All other events are handled in the class JSoko. The three main methods for 
this are:
keyEvent          -> for all key events
mousePressedEvent -> for all mouse events
actionEvent       -> all actions (for instance if a menu item is clicked)
