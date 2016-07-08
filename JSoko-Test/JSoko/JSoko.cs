using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSoko.JSoko
{
  class JSoko
  {
///**
// *  JSoko - A Java implementation of the game of Sokoban
// *  Copyright (c) 2012 by Matthias Meger, Germany
// *
// *  This file is part of JSoko.
// *
// *	JSoko is free software; you can redistribute it and/or modify
// *	it under the terms of the GNU General Public License as published by
// *	the Free Software Foundation; either version 2 of the License, or
// *	(at your option) any later version.
// *
// *	This program is distributed in the hope that it will be useful,
// *  but WITHOUT ANY WARRANTY; without even the implied warranty of
// *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// *  GNU General Public License for more details.
// *
// *  You should have received a copy of the GNU General Public License
// *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */
//package de.sokoban_online.jsoko;

//import java.awt.BorderLayout;
//import java.awt.Cursor;
//import java.awt.Desktop;
//import java.awt.Dimension;
//import java.awt.Font;
//import java.awt.GraphicsEnvironment;
//import java.awt.Point;
//import java.awt.Rectangle;
//import java.awt.Toolkit;
//import java.awt.datatransfer.DataFlavor;
//import java.awt.datatransfer.Transferable;
//import java.awt.dnd.DnDConstants;
//import java.awt.dnd.DropTargetDropEvent;
//import java.awt.event.ActionEvent;
//import java.awt.event.ActionListener;
//import java.awt.event.KeyEvent;
//import java.awt.event.MouseEvent;
//import java.awt.event.WindowAdapter;
//import java.awt.event.WindowEvent;
//import java.io.File;
//import java.io.FileNotFoundException;
//import java.io.IOException;
//import java.lang.reflect.InvocationTargetException;
//import java.net.URI;
//import java.net.URL;
//import java.util.ArrayList;
//import java.util.Collections;
//import java.util.Comparator;
//import java.util.EventObject;
//import java.util.HashSet;
//import java.util.Iterator;
//import java.util.LinkedHashSet;
//import java.util.List;
//import java.util.Scanner;
//import java.util.concurrent.atomic.AtomicBoolean;

//import javax.help.CSH;
//import javax.swing.AbstractAction;
//import javax.swing.AbstractButton;
//import javax.swing.BorderFactory;
//import javax.swing.DefaultListModel;
//import javax.swing.JButton;
//import javax.swing.JCheckBoxMenuItem;
//import javax.swing.JComponent;
//import javax.swing.JDialog;
//import javax.swing.JFrame;
//import javax.swing.JList;
//import javax.swing.JOptionPane;
//import javax.swing.JPanel;
//import javax.swing.JScrollPane;
//import javax.swing.JSlider;
//import javax.swing.JTextField;
//import javax.swing.SwingConstants;
//import javax.swing.SwingUtilities;
//import javax.swing.SwingWorker;
//import javax.swing.WindowConstants;
//import javax.swing.event.ChangeEvent;
//import javax.swing.event.ChangeListener;
//import javax.swing.filechooser.FileFilter;

//import com.jidesoft.dialog.ButtonPanel;
//import com.jidesoft.dialog.StandardDialog;
//import com.jidesoft.list.StyledListCellRenderer;
//import com.jidesoft.plaf.UIDefaultsLookup;
//import com.jidesoft.swing.JideTitledBorder;
//import com.jidesoft.swing.PartialEtchedBorder;
//import com.jidesoft.swing.PartialSide;
//import com.jidesoft.swing.StyleRange;

//import de.sokoban_online.jsoko.board.Board;
//import de.sokoban_online.jsoko.board.DirectionConstants;
//import de.sokoban_online.jsoko.board.Directions;
//import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
//import de.sokoban_online.jsoko.deadlockdetection.ClosedDiagonalDeadlock;
//import de.sokoban_online.jsoko.deadlockdetection.DeadlockDebug;
//import de.sokoban_online.jsoko.deadlockdetection.DeadlockDetection;
//import de.sokoban_online.jsoko.editor.Editor;
//import de.sokoban_online.jsoko.gui.GUI;
//import de.sokoban_online.jsoko.gui.GraphicalLevelBrowser;
//import de.sokoban_online.jsoko.gui.JSokoAboutBox;
//import de.sokoban_online.jsoko.gui.MainBoardDisplay;
//import de.sokoban_online.jsoko.gui.MessageDialogs;
//import de.sokoban_online.jsoko.gui.Transformation;
//import de.sokoban_online.jsoko.leveldata.Author;
//import de.sokoban_online.jsoko.leveldata.Database;
//import de.sokoban_online.jsoko.leveldata.History;
//import de.sokoban_online.jsoko.leveldata.HistoryElement;
//import de.sokoban_online.jsoko.leveldata.Level;
//import de.sokoban_online.jsoko.leveldata.LevelCollection;
//import de.sokoban_online.jsoko.leveldata.LevelsIO;
//import de.sokoban_online.jsoko.leveldata.RunLengthFormat;
//import de.sokoban_online.jsoko.leveldata.SelectableLevelCollectionComboBoxModel;
//import de.sokoban_online.jsoko.leveldata.SelectableLevelCollectionComboBoxModel.SelectableLevelCollection;
//import de.sokoban_online.jsoko.leveldata.Snapshot;
//import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseDataEvent;
//import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseEventListener;
//import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseGUI;
//import de.sokoban_online.jsoko.leveldata.solutions.Solution;
//import de.sokoban_online.jsoko.leveldata.solutions.SolutionEvent;
//import de.sokoban_online.jsoko.leveldata.solutions.SolutionEventListener;
//import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager;
//import de.sokoban_online.jsoko.optimizer.GUI.OptimizerGUI;
//import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
//import de.sokoban_online.jsoko.pushesLowerBoundCalculation.Penalty;
//import de.sokoban_online.jsoko.resourceHandling.Settings;
//import de.sokoban_online.jsoko.resourceHandling.Texts;
//import de.sokoban_online.jsoko.solver.Solver;
//import de.sokoban_online.jsoko.solver.SolverAStar;
//import de.sokoban_online.jsoko.solver.SolverGUI;
//import de.sokoban_online.jsoko.solver.SolverIDAStarPushesMoves;
//import de.sokoban_online.jsoko.sound.Sound;
//import de.sokoban_online.jsoko.utilities.Debug;
//import de.sokoban_online.jsoko.utilities.Delays;
//import de.sokoban_online.jsoko.utilities.FileSelector;
//import de.sokoban_online.jsoko.utilities.Utilities;


///**
// * This is the main class holding all references of this program. Every
// * important object in this program holds a reference to this object in order to
// * have access to every other object in the game. In this class all important
// * actions are handled.
// */
//@SuppressWarnings("serial")
//public class JSoko extends JFrame implements DirectionConstants, ActionListener, SolutionEventListener, ChangeListener {

//  private enum GameMode {
//    /** Play mode -> the user can play the game. 								 */  PLAY,
//    /** Editor mode -> the user can use the editor but can't play. 				 */  EDITOR,
//    /** Invalid level mode -> the user can just see the level but can't play it. */  INVALID_LEVEL
//  }

//  // Current modus of the game (editor mode, play mode, invalid level mode).
//  private GameMode gameMode = GameMode.PLAY;

//  // Status of the display of the deadlock squares before the editor is
//  // activated. After the editor has been left this status must be set again.
//  private boolean isShowDeadlocksActivatedWhenEnteringEditor = false;

//  /** The transformation set when the editor is activated. */
//  private String transformationStringWhenEditorIsActivated;

//  /** The GUI of this program. Public for easier access. */
//  public GUI applicationGUI;

//  /** Object managing the board of this program. Public for easier access. */
//  public Board board;

//  /** Object for deadlock detection. */
//  private DeadlockDetection deadlockDetection;

//  /** Calculates the lower bound of pushes to solve a board. */
//  private LowerBoundCalculation lowerBoundCalculation;

//  /**
//   * Object storing all changes of the board for supporting a history
//   * functionality. Public for easier access.
//   */
//  public History movesHistory;

//  /** Object holding all level data. Public for easier access. */
//  public LevelsIO levelIO;

//  /**
//   * Object holding the collection data -> all levels that are currently ready
//   * for playing. Public for easier access.
//   */
//  public LevelCollection currentLevelCollection;

//  /** Object holding the data of the current level. Public for easier access. */
//  public Level currentLevel;

//  /** Object implementing the editor functionality. Public for easier access. */
//  public Editor editor;

//  /** The currently used solver GUI. */
//  public SolverGUI solverGUI;

//  /** Indicates whether a box has been selected for pushing. */
//  boolean isABoxSelected = false;

//  /** The position of the box the user has selected. */
//  int selectedBoxPosition = 0;

//  /** Indicates whether the reachable squares of the player have to be highlighted. */
//  private boolean isHighLightOfPlayerSquaresActivated;

//  /** All collections the user can select for playing. */
//  private SelectableLevelCollectionComboBoxModel levelCollectionsList = new SelectableLevelCollectionComboBoxModel(5);

//  /**
//   * Own thread that moves the player in order to avoid blocking the event
//   * dispatcher thread for doing this.
//   */
//  Thread movePlayerThread;

//  /**
//   * Flag indicating whether the slider for browsing the movement history is
//   * shown.
//   */
//  boolean isMovementHistorySliderActivated = false;

//  /**
//   * Initial player position of the level. This is needed, because there is no
//   * push before the first moves, so playerPositionAfterLastPush has to be set
//   * to his position.
//   */
//  private int initialPlayerPosition = 0;

//  /**
//   * This value is used in the BFS-Solver. This value is added to the number
//   * of the forward board positions. Hence, a high value will result in a
//   * preference of the backward search. => This value determines which search
//   * direction is used. 0 = both search directions.
//   */
//  public int preferredSearchDirection = 0;

//  /** Counter: How many boxes are located on a goal. Public for easier access. */
//  int boxesOnGoalsCount = 0;

//  /** Counter of moves. Public for easier access. */
//  public int movesCount = 0;

//  /** Counter of pushes. Public for easier access. */
//  public int pushesCount = 0;


//  /**
//   * The main method of this application.
//   * <p>
//   *
//   * @param argv passed parameters
//   */
//  static public void main(String[] argv) {

//    // Check for debug parameters.
//    Debug.checkParameters(argv);

//    SwingUtilities.invokeLater(new Runnable() {
//      @Override
//      public void run() {
//        new JSoko().startProgram();
//      }
//    });
//  }

//  /**
//   * Starts this program.
//   */

//  private void startProgram() {

//    // All uncaught exceptions / errors are to be handled by the method uncaughtException.
//    Thread.setDefaultUncaughtExceptionHandler(ExceptionHandler.INSTANCE);

//    // For debug purposes all classes can access this JSoko object using this reference.
//    Debug.debugApplication = this;

//    // Load settings.
//    Settings.loadSettings(this);

//    GUI.setLookAndFeel();

//    // Load language texts.
//    Texts.loadAndSetTexts();

//    // Create object for the level management in an extra thread for better performance because the DB is opened in LevelsIO.
//    Thread levelsIOThread = new Thread(new Runnable() {
//      @Override
//      public void run() {
//        levelIO = new LevelsIO(JSoko.this);
//      }
//    });levelsIOThread.start();

//    // Set the title of the game.
//    setTitle("JSoko");

//    // Add the JSoko icon to the frame.
//    setIconImage(Utilities.getJSokoIcon());

//    // Set bounds of this program.
//    setBounds( Settings.getInt("applicationXCoordinate", 0),
//           Settings.getInt("applicationYCoordinate", 0),
//           Settings.getInt("applicationWidth",  1024),
//           Settings.getInt("applicationHeight",  800)   );

//    // Add a WindowListener.
//    setDefaultCloseOperation(WindowConstants.DO_NOTHING_ON_CLOSE);
//    addWindowListener(new WindowAdapter() {
//      @Override
//      public void windowClosing(WindowEvent event) {
//        actionPerformed(new ActionEvent(event.getSource(), event.getID(), "programClosing"));
//      }
//    });

//    // Set the reference to this object in the static class object
//    // "transformation"
//    Transformation.setApplication(this);
//    Transformation.addChangeEventListener(this);

//    // Create the object holding all board data.
//    board = new Board();

//    // Create the panel which displays the graphical output of this program.
//    // (uses internally the board reference. Hence, "board" must already been referencing to the board!)
//    applicationGUI = new GUI(this);

//    // Add the GUI of the program.
//    add(applicationGUI);
//    applicationGUI.addActionListener(this);

//    // Ensure that no parts of the program will be off-screen and the program is displayed centered.
//    // Get the maximum size available for the program.
//    Rectangle maximumWindowBounds = GraphicsEnvironment.getLocalGraphicsEnvironment().getMaximumWindowBounds();

//    // If the current windows is larger than the maximum available size then
//    // shrink it to the maximum size.
//    if (getSize().width > maximumWindowBounds.width || getSize().height > maximumWindowBounds.height) {
//      setSize(maximumWindowBounds.width, maximumWindowBounds.height);
//    }

//    // If the position is currently -1 this means there aren't any user settings, yet.
//    // The new position should be in the higher middle of the screen:
//    if (getBounds().x == -1 && getBounds().y == -1) {
//      int xCoord = (maximumWindowBounds.width / 2 - getSize().width / 2);
//      int yCoord = (maximumWindowBounds.height / 4 - getSize().height / 4);
//      setLocation(xCoord, yCoord);
//    }

//    // Pre-load the sounds.
//    Sound.Effects.loadSounds();

//    // Wait until the database has been connected before loading the start level.
//    try { levelsIOThread.join(); } catch (InterruptedException e) { e.printStackTrace(); }

//    loadStartLevel();

//    // Display the Frame.
//    setVisible(true);

//    // Load the available collections.
//    levelCollectionsList.updateAvailableCollections(levelIO.database);

//    // Set the same bounds for the Java help as the program but a little
//    // bit smaller size so the user can see that the program has opened
//    // behind the help.
//    Texts.helpBroker.setSize(new Dimension(getSize().width - 40, getSize().height - 40));
//    Texts.helpBroker.setLocation(new Point(getLocation().x + 20, getLocation().y + 20));

//    // Show the help if this is the first start of the program.
//    if(Settings.getBool("showHelp") == true && Debug.isDebugModeActivated == false) {

//      // The next time the program is opened the help shouldn't be displayed again.
//      Settings.set("showHelp", Boolean.FALSE.toString());

//      // Show the help.
//      new CSH.DisplayHelpFromSource(Texts.helpBroker).actionPerformed(new ActionEvent(this, 0, null));
//    }

//    // The main board display should have the focus for catching key and mouse events.
//    applicationGUI.mainBoardDisplay.requestFocusInWindow();

//    // Check for updates if requested.
//    if(Settings.getBool("automaticUpdateCheck")) {
//      checkForUpdates();
//    }
//  }

//  /**
//   * Loads the last loaded collection and sets the start level for the game.
//   */
//  private void loadStartLevel() {

//    List<SelectableLevelCollection> lastPlayedLevelCollections = Settings.getLastPlayedCollections();
//    levelCollectionsList.setLastPlayedCollections(lastPlayedLevelCollections);


//    LevelCollection levelCollection = null;
//    int levelNoToSet = 1;

//    Collections.reverse(lastPlayedLevelCollections); // last played is now first element
//    for(SelectableLevelCollection lastPlayedLevelCollection : lastPlayedLevelCollections) {

//      // Try to load the debug level when the debug mode is activated.
//      try {
//        if (Debug.isDebugModeActivated) {
//          levelCollection = levelIO.getLevelCollectionFromFile("debuglevel.sok");
//          if("debuglevel.sok".equals(lastPlayedLevelCollection.file)) {
//            levelNoToSet = Settings.lastPlayedLevelNumber;
//          }
//          break;
//        }
//      } catch (IOException e) { /* do nothing */ }

//      // If it's a collection from the database it can be loaded from there.
//      if(levelCollection == null && lastPlayedLevelCollection.databaseID != Database.NO_ID) {
//        levelCollection = levelIO.database.getLevelCollection(lastPlayedLevelCollection.databaseID);
//      }

//      // If no level collection has been loaded yet, try to load it from file.
//      if(levelCollection == null && !lastPlayedLevelCollection.file.isEmpty()) {
//        try {
//          levelCollection = levelIO.getLevelCollectionFromFile(lastPlayedLevelCollection.file);
//        } catch (IOException e) { /* do nothing. The collection stays in "levelCollections" as info for the user. */ }
//      }

//      if(levelCollection != null) {
//        levelNoToSet = Settings.lastPlayedLevelNumber;
//        break;
//      }

//      // The settings only contain the level number for the last played collection.
//      // Since the last played collection couldn't be loaded the number is set to the default value 1.
//      Settings.lastPlayedLevelNumber = 1;

//    }

//    // If no collection has been loaded, yet, create a dummy collection so that the program can start.
//    if(levelCollection == null || levelCollection.getLevelsCount() == 0) {
//      levelCollection = new LevelCollection.Builder().setLevels(new Level(levelIO.database)).build();
//    }

//    // Set the collection as current collection and the first level as "active" level.
//    setCollectionForPlaying(levelCollection);
//    setLevelForPlaying(levelNoToSet);

//  }

//  private static void loggTime(int location) {
//    Utilities.loggTime(location, "setLevelForPlaying");
//  }

//  /**
//   * Sets the passed level as new level for playing.
//   *
//   * @param level  level to be set for playing
//   */
//  public void setLevelForPlaying(Level level) {
//    setLevelForPlaying(currentLevelCollection.getLevelNo(level));
//  }

//  /**
//   * Sets a new level for playing.
//   *
//   * @param levelNo  number of the level to set (first is 1)
//   */
//  public void setLevelForPlaying(int levelNo) {

//    Level level = currentLevelCollection.getLevel(levelNo);
//    if(level == null) {
//      level = currentLevelCollection.getLevel(1);

//      if(level == null) {
//        level = new Level(levelIO.database); // Create a dummy level
//      }
//    }

//    // Check if a duplicate delegate level for this level is stored in the database.
//    // If yes, then the solutions and snapshots of this delegate level are added to the current level.
//    final Level finalLevel = level;
//    Thread databaseThread = new Thread(new Runnable() {
//      @Override
//      public void run() {
//        if(!finalLevel.isConnectedWithDB()) {
//          saveInDBAsDelegateLevel(finalLevel);
//        }
//      }
//    });
//    databaseThread.start();

//    // Flag, indicating whether the current level is a valid one.
//    boolean isLevelValid = true;

//    loggTime(0);

//    // Initialize the number of moves and pushes for the new level.
//    movesCount = 0;
//    pushesCount = 0;

//    // The new level can't be saved with "save" but only with "save as".
//    applicationGUI.getSaveLevelMenuItem().setEnabled(false);

//    // Remove JSoko as listener from the current level and save the current moves as save game.
//    if(currentLevel != null) {
//      currentLevel.getSolutionsManager().removeSolutionEventListener(this);

//      // Save the current moves history as save game in the database.
//      Snapshot saveGame = new Snapshot(movesHistory.getHistoryAsSaveGame(), true);
//      currentLevel.setSaveGame(saveGame);
//    }

//    // Set the new level as new current level.
//    currentLevel = level;

//    loggTime(1);

//    // Set the level on the board.
//    try {
//      board.setBoardFromString(level.getBoardDataAsString());
//    } catch (final Exception e) {
//      // Display a message after the level has been loaded to inform
//      // the user that the level couldn't be completely loaded.
//      SwingUtilities.invokeLater(new Runnable() {
//        @Override
//        public void run() {
//          MessageDialogs.showExceptionError(applicationGUI, e);
//        }
//      });
//    }
//    loggTime(2);

//    // Inform the transformation object about the new level.
//    Transformation.newlevel();

//    // Check level validity.
//    isLevelValid = isLevelValid();
//    loggTime(3);

//    // Inform the GUI about the new board.
//    applicationGUI.mainBoardDisplay.setBoardToDisplay(board);
//    loggTime(4);

//    // Update the level combo box in the GUI.
//    applicationGUI.selectLevelInLevelComboBox(level);

//    // Create a local reference to the history object of the level for easier access.
//    movesHistory = currentLevel.getHistory();
//    loggTime(6);

//    // Set the history to the start, because the level is also set to the start position.
//    movesHistory.setHistoryToStart();
//    loggTime(7);

//    // If the level is invalid, set the corresponding mode, show the level and return.
//    if (isLevelValid == false) {
//      setInvalidLevelMode();
//      // Wait until all solutions and snapshots have been added.
//      try {
//        databaseThread.join();
//      } catch (InterruptedException e) { /* shouldn't happen */ }
//      if(currentLevel.isStoredAsDelegate()) {
//        levelIO.database.deleteLevel(currentLevel.getDatabaseID()); // invalid levels needn't to be saved
//        currentLevel.setDatabaseID(Database.NO_ID);					// hence, delete it
//        currentLevel.setDelegate(false);
//      }
//      applicationGUI.getSolutionsView().setLevel(currentLevel);   	// remove all solutions from the previous level in the solutions view
//      redraw(false);

//      return;
//    }

//    // The level is valid. Set the play mode, because the editor mode
//    // may still be active from the previous level.
//    setPlayMode();
//    loggTime(11);

//    // Prepare the board for the new level.
//    board.prepareBoard();
//    loggTime(12);

//    // Wait until all solutions and snapshots have been added.
//    try {
//      databaseThread.join();
//    } catch (InterruptedException e) { /* shouldn't happen */ }


//    // The history is set to the position in the saved history (marked with a "*").
//    setSnapshot(currentLevel.getSaveGame());

//    // Let the player look to the last move direction.
//    HistoryElement lastMove = movesHistory.getPrecedingMovement();
//    if(lastMove != null) {
//      movesHistory.goToNextMovement();
//      applicationGUI.mainBoardDisplay.setViewDirection(lastMove.direction);
//    }
//    else {
//      // Don't let the player look at a wall.
//      for (int direction = 0; direction < DIRS_COUNT; direction++) {
//        int newPlayerPosition = board.getPosition(board.playerPosition, direction);
//        if (!board.isWall(newPlayerPosition)) {
//          applicationGUI.mainBoardDisplay.setViewDirection(direction);
//          break;
//        }
//      }
//    }
//    loggTime(13);


//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();
//    loggTime(14);

//    // Set the solutions of the current level in the solutions view.
//    applicationGUI.getSolutionsView().setLevel(currentLevel);
//    loggTime(15);

//    // Display level so the user gets the level to see although some
//    // calculations have to be done until the game is ready.
//    redraw(false);
//    loggTime(16);

//    // Determine the number of boxes on a goal
//    boxesOnGoalsCount = board.boxData.getBoxesOnGoalsCount();
//    loggTime(21);

//    // Create the object doing all deadlock detection. This has to be done
//    // for every level, because the board size may have changed
//    // and this size is used in many objects.
//    deadlockDetection = new DeadlockDetection(board);
//    loggTime(22);

//    // New lower bound calculation for the new board.
//    lowerBoundCalculation = new LowerBoundCalculation(board);

//    // Show penalty squares for debugging if needed.
//    if (Debug.debugShowPenaltyFields) {
//      Penalty penalty = new Penalty(board);
//      penalty.debugShowPenaltySituations();
//    }

//    // Refresh the level list when a solution is added/removed in order to display the "level solved" indicator.
//    level.getSolutionsManager().addSolutionEventListener(this);

//    // The initial player position is saved in the changes object
//    // for calculating the move distance to this position.
//    initialPlayerPosition = board.playerPosition;

//    // Do a dummy push for immediately checking if the level
//    // is already solved or a deadlock is present.
//    final int boxpos = board.boxData.getBoxPosition(0);
//    analyzeNewBoardPosition(boxpos, boxpos);
//    loggTime(23);
//  }

//  /**
//   * Saves a delegate level in the database in order to store new
//   * solutions and snapshot for the current level there. <br>
//   * The database ID of the delegate level is stored in the passed level.<br>
//   * If an identical delegate level is already stored in the DB, then the solutions and snapshots
//   * of this delegate level are added to the current level and the database ID of the
//   * stored delegate level is set in the passed level.
//   * Note: snapshot handling isn't implemented, yet.
//   *
//   * @param currentLevel  the {@code Level} to store as delegate
//   */
//  private void saveInDBAsDelegateLevel(final Level currentLevel) {

//    List<Level> duplicateLevels = levelIO.database.getLevel(currentLevel.getBoardDataAsString());

//    for(Level delegateLevel : duplicateLevels) {

//      // There may be several duplicate levels but only one delegate level.
//      if(delegateLevel.isStoredAsDelegate()) {

//        // Get a map of all solutions stored in the delegate level. A linked map is used
//        // because the solutions are ordered according to their "order value" and this
//        // order has to be retained.
//        LinkedHashSet<Solution> solutionsDelegateLevel = new LinkedHashSet<Solution>();
//        solutionsDelegateLevel.addAll(delegateLevel.getSolutionsManager().getSolutions());

//        for(Solution solution : currentLevel.getSolutionsManager().getSolutions()) {
//          if(!solutionsDelegateLevel.contains(solution)) {
//            // Add new solutions of the loaded level to the delegate level.
//            levelIO.database.insertSolution(solution, delegateLevel.getDatabaseID());
//          }
//          else {
//            // Delete the solution because the one from the delegate will be added.
//            // This ensures that a solution comment, order value, ... from the saved solution is used.
//            currentLevel.deleteSolution(solution); // delete the solution which isn't connected with the DB
//          }
//        }

//        // Add all solutions of the delegate level which haven't been in the loaded level, yet.
//        // The solutions are stored in the order they have been stored in the delegate level.
//        for(Solution solution : solutionsDelegateLevel) {
//          currentLevel.addSolution(solution);
//        }

//        // Connect the current level with the delegate level stored in the database.
//        // This is done after the delegate solutions have been added using "addSolution",
//        // because this way the addSolution-method doesn't try to store the solution again
//        // on the database, because the level hasn't been stored in the DB, yet.
//        currentLevel.setDelegate(true);
//        currentLevel.setDatabaseID(delegateLevel.getDatabaseID());

//        // Add all snapshots of the delegate level. This doesn't overwrite existing duplicate snapshots.
//        // Hence, duplicate snapshots of "currentLevel" stay unchanged.
//        // This is just for simpler coding, as otherwise the same logic as for solutions has to be implemented.
//        for(Snapshot snapshot : delegateLevel.getSnapshots()) {
//          currentLevel.addSnapshot(snapshot);
//        }

//        // Additional level data that are taken from the delegate.
//        currentLevel.setComment(delegateLevel.getComment());
//        currentLevel.setTransformationString(delegateLevel.getTransformationString());
//        currentLevel.setDifficulty(delegateLevel.getDifficulty());
//      }
//    }

//    // If the level isn't connected with a delegate level yet, then save a delegate level for it.
//    if(!currentLevel.isConnectedWithDB()) {
//      levelIO.database.insertAsDelegateLevel(currentLevel);
//    }

//    // If this is the first delegate level a new "unregistered" collection has been created for storing the delegates.
//    // This new collection has to be displayed in the collections list, too. For better performance, we check
//    // whether the delegate collection isn't already stored first.
//    if(levelCollectionsList.getLevelCollectionByID(levelIO.database.getDelegateLevelsCollectionID()) == null) {
//      levelCollectionsList.updateAvailableCollections(levelIO.database);
//    }
//  }

//  /**
//   * Opens a file chooser to select a database to import data from.
//   * All data of the selected database are imported to the current database in use of JSoko.<br>
//   */
//  private void importDataFromAnotherDatabase() {

//    final String databaseFileSuffix = ".data";

//    // File filter for filtering for database files only.
//    FileFilter fileFilter = new FileFilter() {

//      @Override
//      public String getDescription() {
//        return Texts.getText("database");
//      }

//      @Override
//      public boolean accept(File f) {
//        return f != null && ( f.getName().toLowerCase().endsWith(databaseFileSuffix) || f.isDirectory() );
//      }
//    };

//    // Ask the user to select the database file.
//    final String[] otherDatabaseFilePath = applicationGUI.getFileDataForLoading(".", Texts.getText("database"), fileFilter);
//    if(otherDatabaseFilePath == null || otherDatabaseFilePath[1] == null || !otherDatabaseFilePath[1].endsWith(databaseFileSuffix)) {
//      return;
//    }


//    final char SUCCESS_ICON = '0';
//    final char FAILED_ICON  = '1';

//    // Create a panel showing the import status of every collection.
//    final JPanel importResults = new JPanel(new BorderLayout());
//    final JList<String> importedCollections = new JList<String>();
//    final DefaultListModel<String> model = new DefaultListModel<String>();
//    importedCollections.setModel(model);
//    importedCollections.setCellRenderer(new StyledListCellRenderer() {
//      @Override
//      protected void customizeStyledLabel(JList list, Object value, int index, boolean isSelected, boolean cellHasFocus) {
//        super.customizeStyledLabel(list, value, index, isSelected, cellHasFocus);

//        String text = getText();

//        if(text.length() > 0 && text.charAt(0) == SUCCESS_ICON) {
//          setIcon(Utilities.getIcon("apply (oxygen).png", ""));
//          setText(text.substring(1));
//        }
//        else if(text.length() > 0 && text.charAt(0) == FAILED_ICON) {
//          setIcon(Utilities.getIcon("process-stop.png", ""));
//          setText(text.substring(1));
//        } else {
//          setIcon(null);
//          addStyleRange(new StyleRange(Font.BOLD));
//        }

//        setBorder(BorderFactory.createEmptyBorder(0, 2, 0, 2));
//      }
//    });
//    importResults.add(new JScrollPane(importedCollections), BorderLayout.CENTER);


//    final JTextField infoText = new JTextField();
//    infoText.setEditable(false);
//    importResults.add(infoText, BorderLayout.SOUTH);
//    importResults.setPreferredSize(new Dimension(400, 600));

//    // OK button and cancel button of the dialog showing the results of the import.
//    // These buttons are already created here so the import SwingWorker can access them.
//    // The ok button is first enabled when the import has been completed.
//    final JButton okButton = new JButton();
//    okButton.setEnabled(false);
//    final JButton cancelButton = new JButton();

//    // Mutable boolean for canceling the SwingWorker. the "cancel" method of
//    // Swingworker would cancel the whole thread which includes coding that
//    // may be interrupted (for instance SwingUtilities.invokeAndWait()).
//    // However, the coding should continue safely until the current collection
//    // is completely imported and then cancel the Swingworker before the next
//    // collection is processed. This also ensures that "done()" is first called
//    // AFTER "doInBackground()" has ended.
//    final boolean[] isImportCancelled = new boolean[1];

//    // The worker thread that imports all collections.
//    final SwingWorker<Void, String> importWorker = new SwingWorker<Void, String>() {

//      int importedCollectionsCount = 0;
//      List<LevelCollection> collectionsToBeImported = new ArrayList<LevelCollection>(0);

//      @Override
//      protected Void doInBackground() throws Exception {

//        try {
//          // The database suffix ".data" mustn't be passed for connecting => remove it.
//          String databaseFilePath = otherDatabaseFilePath[1].substring(0, otherDatabaseFilePath[1].length() - databaseFileSuffix.length());
//          Database otherDatabase = Database.connectTo(databaseFilePath);

//          collectionsToBeImported = otherDatabase.getCollectionsInfo();
//          for(LevelCollection collection : collectionsToBeImported) {
//            LevelCollection otherLevelCollection = otherDatabase.getLevelCollection(collection.getDatabaseID());

//            if(otherLevelCollection.isDelegateLevelsCollection()) {
//              for(Level level : otherLevelCollection.getLevels()) {
//                level.setDatabaseID(Database.NO_ID);// Delete the ID which is only valid for the other DB
//                saveInDBAsDelegateLevel(level);
//              }
//            }
//            else {
//              levelIO.database.insertLevelCollection(otherLevelCollection);
//            }

//            // Update the info text about how many collections have been imported to the database.
//            publish(  SUCCESS_ICON
//                + otherLevelCollection.getTitle()
//                + " (" + otherLevelCollection.getLevelsCount()
//                + " "  + Texts.getText("general.levels")
//                + ")" );

//            // Break if the import has been cancelled by the user.
//            if(isImportCancelled[0] == true) {
//              break;
//            }
//          }

//          otherDatabase.shutdown();

//        } catch (Exception e) {
//          e.printStackTrace();
//        }

//        // 100% of the collections have been processed.
//        setProgress(100);

//        return null;
//      }

//      @Override
//      protected void process(List<String> data) {
//        for(String s : data) {
//          model.addElement(s);
//          infoText.setText(Texts.getText("importedXOfYCollections", ++importedCollectionsCount, collectionsToBeImported.size()));
//        }

//        importedCollections.ensureIndexIsVisible(model.getSize() - 1);
//      }

//      @Override
//      protected void done() {

//        if(isImportCancelled[0] == true) {
//          model.addElement(FAILED_ICON+Texts.getText("furtherImportCancelled"));
//        }

//        // The import has been completed. The user may leave the dialog using
//        // the OK button but no abort the import anymore.
//        okButton.setEnabled(true);
//        cancelButton.getAction().setEnabled(false);

//        // Show a message that the import has been completed.
//        model.addElement(" ");
//        model.addElement(Texts.getText("importCompleted"));

//        // Show the last added texts.
//        importedCollections.ensureIndexIsVisible(model.getSize() - 1);
//      }
//    };

//    Utilities.executor.submit(importWorker);

//    // Create a dialog for showing the import status so the user can't use
//    // the main game GUI while the import is done.
//    final StandardDialog dialog = new StandardDialog(this, Texts.getText("importResults"), true) {

//      @Override
//      public JComponent createBannerPanel() {
//        return null;
//      }

//      @Override
//      public JComponent createContentPanel() {
//        JPanel panel = new JPanel(new BorderLayout(2, 2));
//        panel.setBorder(BorderFactory.createCompoundBorder(
//            new JideTitledBorder(
//                new PartialEtchedBorder(PartialEtchedBorder.LOWERED,
//                    PartialSide.NORTH),
//                    Texts.getText("importedCollections"),
//                    JideTitledBorder.LEADING,
//                    JideTitledBorder.ABOVE_TOP),
//                    BorderFactory.createEmptyBorder(6, 0, 0, 0)));
//        panel.add(importResults);
//        return panel;
//      }

//      @Override
//      public ButtonPanel createButtonPanel() {

//        ButtonPanel buttonPanel = new ButtonPanel(SwingConstants.CENTER);

//        // OK button
//        okButton.setName(OK);
//        okButton.setText(UIDefaultsLookup.getString("OptionPane.okButtonText"));
//        okButton.addActionListener(new ActionListener() {
//          @Override
//          public void actionPerformed(ActionEvent e) {
//            setDialogResult(RESULT_AFFIRMED);
//            setVisible(false);
//            dispose();
//          }
//        });

//        // Cancel button
//        cancelButton.setName(CANCEL);
//        cancelButton.setAction(new AbstractAction(UIDefaultsLookup.getString("OptionPane.cancelButtonText")) {
//          @Override
//          public void actionPerformed(ActionEvent e) {
//            if(importWorker.getProgress() < 100) {
//              // Cancel the import.
//              isImportCancelled[0] = true;
//            }
//          }
//        });

//        buttonPanel.addButton(okButton, ButtonPanel.AFFIRMATIVE_BUTTON);
//        buttonPanel.addButton(cancelButton, ButtonPanel.CANCEL_BUTTON);

//        setDefaultCancelAction(cancelButton.getAction());
//        getRootPane().setDefaultButton(okButton);
//        buttonPanel.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));
//        return buttonPanel;

//      }
//    };

//    dialog.pack();
//    dialog.setLocationRelativeTo(null);
//    dialog.setVisible(true);

//  }

//  /**
//   * Convenience method to set the info text which is to be displayed in the
//   * <code>applicationGUI.mainBoardDisplay</code>.
//   *
//   * @param infotext
//   * @see MainBoardDisplay#displayInfotext(String)
//   */
//  public final void displayInfotext(String infotext) {
//    applicationGUI.mainBoardDisplay.displayInfotext(infotext);
//  }

//  /**
//   * This method is called every time a box is pushed.
//   * <p>
//   * If a box is pushed automatically in method "pushBoxAutomaticall" then
//   * this method is called only once, no matter how many squares the box is
//   * pushed.
//   *
//   * @param oldBoxPosition
//   *            the position the box has been located before the push
//   * @param newBoxPosition
//   *            the current box position
//   */
//  void analyzeNewBoardPosition(final int oldBoxPosition, final int newBoxPosition) {

//    // Ensure that this method is called on the EDT so the user can't
//    // change the board while it is analyzed. While this method is
//    // executed the user mustn't have access to the game to ensure
//    // the board remains unchanged while it is analyzed.
//    // Hence, even when the thread is interrupted we have to wait.
//    final AtomicBoolean atomicBool = new AtomicBoolean();
//    if(SwingUtilities.isEventDispatchThread() == false) {
//      try {
//        SwingUtilities.invokeAndWait(new Runnable() {
//          @Override
//          public void run() {
//            analyzeNewBoardPosition(oldBoxPosition, newBoxPosition);
//            atomicBool.set(true);
//          }
//        });
//      } catch (InterruptedException e) {
//        Thread.currentThread().interrupt();
//      } catch (InvocationTargetException e) { ExceptionHandler.INSTANCE.uncaughtException(Thread.currentThread(), e); }
//      while(atomicBool.get() == false) {
//        Thread.yield();
//      }
//      return;
//    }


//    // The info text is deleted after every push.
//    displayInfotext("");

//    // Update the "boxes on goals" counter.
//    if (board.isGoal(oldBoxPosition)) {
//      boxesOnGoalsCount--;
//    }
//    if (board.isGoal(newBoxPosition)) {
//      boxesOnGoalsCount++;
//    }

//    // Check if the level is solved.
//    if (boxesOnGoalsCount == board.goalsCount) {

//      // The info text to be displayed.
//      String infoText = Texts.getText("solved");	// with trailing blank

//      // Set the new solution.
//      Solution newSolution = new Solution(movesHistory.getLURDFromHistory());
//      newSolution.name = Utilities.nowString();
//      newSolution.isOwnSolution = true;

//      // Add the solution to the current level data. If there haven't been
//      // any movements the level is solved right from the beginning.
//      // This is a special type of level. To avoid problems with these
//      // levels only solutions having at least one movement are saved.
//      if (newSolution.lurd.length() > 0) {
//        switch(currentLevel.addSolution(newSolution)) {
//          case DUPLICATE_SOLUTION:
//            infoText += "(" + Texts.getText("solutionIsADuplicate") + ") ";
//            break;
//          case NEW_FIRST_SOLUTION:
//            // No extra text for first found solution.
//            break;
//          case NEW_BEST_SOLUTION:
//            infoText += Texts.getText("newBestSolution");
//            break;
//          case NEW_BEST_PUSHES_SOLUTION:
//            infoText += Texts.getText("newBestPushesSolution");
//            break;
//          case NEW_BEST_MOVES_SOLUTION:
//            infoText += Texts.getText("newBestMovesSolution");
//            break;
//          case INVALID_SOLUTION:
//            break;
//          case NEW_SOLUTION:
//            break;
//        }
//      }

//      // "Level solved! ", possibly extended
//      displayInfotext(infoText);
//    } else {
//      // If the lower bound for the pushes has to be shown the real lower bound
//      // is calculated (inclusive a deadlock detection!). Otherwise just a
//      // deadlock detection is performed.
//      int pushesLowerBound = Settings.showMinimumSolutionLength ?
//        lowerBoundCalculation.calculatePushesLowerBound(newBoxPosition) :
//        deadlockDetection.isDeadlock(newBoxPosition) ? LowerBoundCalculation.DEADLOCK : 0;

//      // Display "level unsolvable" if the level can't be solved anymore.
//      if (pushesLowerBound == LowerBoundCalculation.DEADLOCK) {
//        displayInfotext(pushesCount > 0 ? Texts.getText("notsolvableanymore") : Texts.getText("levelNotSolvable"));
//      } else {
//        // If requested display the minimal solution length.
//        if (Settings.showMinimumSolutionLength) {
//          displayInfotext(Texts.getText("minimumSolutionLength")
//                      + " " + (pushesCount + pushesLowerBound)
//                      + " " + Texts.getText("pushes_lowercase"));
//        }
//      }
//    }

//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();

//    /*
//     * All coding below is just for debug purposes
//     */

//    // If demanded, we calculate, time and show the lower bound
//    if (Debug.debugShowLowerBoundForward) {
//      long begTime = System.currentTimeMillis();
//      int pushesLowerbound = lowerBoundCalculation.calculatePushesLowerbound();
//      long endTime = System.currentTimeMillis();
//      displayInfotext("Lowerbound: " + pushesLowerbound
//          + ", Zeit: " + (endTime - begTime)
//          + " ms. Minimum solution length: "
//          + (pushesLowerbound + pushesCount));
//    }

//    // If demanded, we calculate, time and show the backwards lower bound
//    if (Debug.debugShowLowerBoundBackward) {
//      long begTime = System.currentTimeMillis();
//      board.boxData.setAllBoxesNotFrozen();
//      int lowerBoundbackwards = lowerBoundCalculation.calculatePushesLowerBoundBackwardsSearch();
//      long endTime = System.currentTimeMillis();
//      displayInfotext("Lowerbound: " + lowerBoundbackwards
//                  + ", Zeit: " + (endTime - begTime) + " ms");
//    }

//    // If we shall show, whether the box is considered "inside tunnel",
//    // then we do it here and now.
//    // (Method "isBoxInATunnel" has been made "public" for this debug purposes)
//    if (       newBoxPosition != oldBoxPosition
//        && (Debug.debugShowBoxInTunnelStatusPush || Debug.debugShowBoxInTunnelStatusMove)) {

//      Solver solver;

//      // Create the appropriate solver.
//      if (Debug.debugShowBoxInTunnelStatusPush) {
//        solver = new SolverAStar(this, new SolverGUI(this));
//      } else {
//        solver = new SolverIDAStarPushesMoves(this, new SolverGUI(this));
//      }

//      // The tunnel detection expects the currently reachable squares
//      // of the player to be calculated.
//      solver.playersReachableSquares.update();

//      // Set frozen boxes
//      board.boxData.setAllBoxesNotFrozen();
//      deadlockDetection.freezeDeadlockDetection.isDeadlock(newBoxPosition, true);
//      int movementDirection = movesHistory.getPrecedingMovement().direction;
//      final int boxNo       = board.getBoxNo(newBoxPosition);
//      boolean isInAtunnel;
//      if (Debug.debugShowBoxInTunnelStatusPush) {
//        isInAtunnel = ((SolverAStar) solver).isBoxInATunnel(boxNo, movementDirection);
//      } else {
//        isInAtunnel = ((SolverIDAStarPushesMoves) solver).isBoxInATunnel(boxNo, movementDirection);
//      }
//      displayInfotext("Box in tunnel = " + isInAtunnel);
//    }

//    // Show I-Corrals if requested.
//    if (Debug.debugShowICorrals || Debug.debugShowCorralsWhileSolving ) {
//      SolverAStar solver = new SolverAStar(this, new SolverGUI(this));
//      board.playersReachableSquares.update();
//      solver.playersReachableSquares = board.playersReachableSquares.getClone();
//      solver.identifyRelevantBoxes();
//    }

//    // Display all "closed diagonal deadlocks" if requested.
//    if (Debug.debugShowAllClosedDiagonalDeadlocks) {
//      ClosedDiagonalDeadlock closedDiagonalDeadlockDetection = new ClosedDiagonalDeadlock(board);

//      board.removeAllMarking();

//      for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
//        if(closedDiagonalDeadlockDetection.isDeadlock(board.boxData.getBoxPosition(boxNo))) {
//          board.setMarking(board.boxData.getBoxPosition(boxNo));
//        }
//      }

//      redraw(false);
//    }
//  }

//  /**
//   * Redraws the screen and waits for "enter" if needed.
//   *
//   * @param waitForEnter  whether the method is to wait for enter after drawing
//   */
//  public void redraw(boolean waitForEnter) {

//    // Draw the new GUI.
//    applicationGUI.mainBoardDisplay.repaint();

//    // For debugging purposes wait for "Enter"
//    if (waitForEnter) {
//      JDialog dialog = new JOptionPane("").createDialog(null, "Waiting for Enter");
//      dialog.setLocation(getX() + getWidth() + 45, getY() + 10);
//      dialog.setVisible(true);
//      // Beginners explanation: The above dialog is "modal", and hence freezes
//      // all other graphics activity.  That way "we wait" by being frozen.
//      // When "setVisible(true)" returns, the dialog is done!
//    }
//  }

//  /**
//   * Debug: Calculates and displays the lower bound for all levels.
//   */
//  private void calculateLowerboundOfAllLevel() {

//    final int maxLevelNo = currentLevelCollection.getLevelsCount();
//    int[] pushesLowerBounds = new int[1 + maxLevelNo];

//    for (int levelNo = 1; levelNo <= maxLevelNo; levelNo++) {
//      setLevelForPlaying(levelNo);
//      pushesLowerBounds[levelNo] = lowerBoundCalculation.calculatePushesLowerbound();
//    }

//    for (int levelNo = 1; levelNo <= maxLevelNo; levelNo++) {
//      System.out.printf("Level: %3d  Name: %-60s Lowerbound: %3d \n",
//          levelNo, currentLevelCollection.getLevel(levelNo).getTitle(), pushesLowerBounds[levelNo]);
//    }
//  }

//  /**
//   * Returns whether the game is in play mode, just now.
//   *
//   * @return <code>true</code>, if the game is in play mode
//   */
//  public boolean isPlayModeActivated() {
//    return gameMode == GameMode.PLAY;
//  }

//  /**
//   * Returns whether the game is in editor mode, just now.
//   *
//   * @return <code>true</code>, iff the game is in editor mode
//   */
//  public boolean isEditorModeActivated() {
//    return gameMode == GameMode.EDITOR;
//  }

//  /**
//   * Sets the "invalid level" mode. The current level is invalid. Therefore
//   * the user isn't allowed to play it. Furthermore the solver and the optimizer are disabled.
//   */
//  private void setInvalidLevelMode() {
//    applicationGUI.setInvalidLevelModeDependentObjectsEnabled(false);
//    gameMode = GameMode.INVALID_LEVEL;
//  }

//  /**
//   * Sets the (normal, default) "play" mode.
//   */
//  private void setPlayMode() {

//    // When we terminate "editor mode", we restore the deadlock square display option,
//    // as it was saved when entering editor mode.
//    if (gameMode == GameMode.EDITOR) {
//      Settings.showDeadlockFields = isShowDeadlocksActivatedWhenEnteringEditor;
//    }

//    // Set the mode itself
//    gameMode = GameMode.PLAY;

//    // Remove the mouse listener of the editor
//    applicationGUI.mainBoardDisplay.removeMouseWheelListener(editor);

//    // In play mode we reactivate all objects, which were inactive in editor mode.
//    applicationGUI.setModeDependentObjectStatus();

//    // While the editor is opened the solutions sidebar has been set invisible -> set is visible again.
//    // (quick and dirty because editor has to be an own dialog in the future at any rate).
//    applicationGUI.getSolutionsView().getParent().getParent().getParent().getParent().getParent().setVisible(true);

//    // Enable help for playing.
//    Texts.helpBroker.enableHelpKey(applicationGUI.mainBoardDisplay, "jsoko.how-to-play", null);
//  }

//  /**
//   * Sets the editor mode.
//   */
//  private void setEditorMode() {

//    // Create an editor object if there has not yet been created one.
//    if (editor == null) {
//      editor = new Editor(this);
//    }

//    // Deselect a possibly selected box.
//    isABoxSelected = false;

//    // In the editor mode the deadlock squares aren't shown. Therefore this flag is saved.
//    isShowDeadlocksActivatedWhenEnteringEditor = Settings.showDeadlockFields;

//    // Remove all listeners of the editor (if two levels are invalid one after the other
//    // then this method is called twice, so more than one listener are registered).
//    applicationGUI.mainBoardDisplay.removeMouseWheelListener(editor);

//    // For choosing the editor objects a mouse listener is registered.
//    applicationGUI.mainBoardDisplay.addMouseWheelListener(editor);

//    // Inform the editor about the new level.
//    editor.newLevel();

//    // Set the game mode to editor mode.
//    gameMode = GameMode.EDITOR;

//    // While the editor is activated the undo / redo buttons are set disabled. (They are handled separately because
//    // their status also depends on the history).
//    applicationGUI.setUndoButtonsEnabled(false);
//    applicationGUI.setRedoButtonsEnabled(false);

//    // Deactivate some of the menu items, because they are not needed in editor mode.
//    applicationGUI.setModeDependentObjectStatus();

//    // While the editor is opened the solutions sidebar is set invisible.
//    // (quick and dirty because editor has to be an own
//    // dialog in the future at any rate).
//    applicationGUI.getSolutionsView().getParent().getParent().getParent().getParent().getParent().setVisible(false);

//    // Enable help for the editor.
//    Texts.helpBroker.enableHelpKey(applicationGUI.mainBoardDisplay, "editor", null);

//    // Backup the current transformation.
//    // This value must be set back when the editor is left with "cancel".
//    transformationStringWhenEditorIsActivated = Transformation.getTransformationAsString();
//  }

//  /**
//   * Switches from editor mode into play mode and back.
//   */
//  private void switchEditorPlayMode() {

//    // If the current mode isn't the editor mode then switch to the editor
//    // mode (note: there are several modes other than editor mode)
//    if (isEditorModeActivated() == false) {

//      // set the editor mode.
//      setEditorMode();

//    } else {
//      // If the level has been transformed it is treated as changed in
//      // every case.
//      if (Transformation.getTransformationAsString().equals(transformationStringWhenEditorIsActivated) == false) {
//        editor.hasBoardBeenChanged = true;
//      }

//      // If the level hasn't been changed it's the same as leaving the editor with "cancel."
//      if (editor.hasBoardBeenChanged == false) {
//        setPlaymodeWithCancelingEditorChanges();

//        return;
//      }

//      // Adopt the board from the editor without empty rows.
//      // This also deletes all transformations of the level -> the level
//      // is treated as "not transformed".
//      board.adoptBoardFromEditor(applicationGUI);

//      // Get the level data from the current board.
//      Level level = getLevelFromBoard();

//      // Add the level to a new collection and set this collection as
//      // current collection.
//      LevelCollection levelCollection = new LevelCollection.Builder()
//                          .setTitle(Texts.getText("editor"))
//                          .setLevels(level)
//                          .build();
//      setCollectionForPlaying(levelCollection);

//      // Set the play mode, because the editor has been left (setNewLevel will do the same).
//      setPlayMode();

//      // Load the new level.
//      setLevelForPlaying(currentLevelCollection.getLevelsCount());

//      // Analyze the new board position (maybe it is already solved or a
//      // deadlock)
//      analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//    }

//    // Display the changes.
//    redraw(false);

//    return;
//  }

//  /**
//   * Cancels all editor changes and sets the play mode again.
//   */
//  private void setPlaymodeWithCancelingEditorChanges() {

//    // Backup the current movement number.
//    int currentMovementNumber = movesHistory.getCurrentMovementNo();

//    // Reset the transformation when the editor had been activated.
//    currentLevel.setTransformationString(transformationStringWhenEditorIsActivated);

//    // Set the play mode, because the editor has been left. (Important because the transformation object
//    // checks the mode when the method "Transformation.newLevel" is called)
//    setPlayMode();

//    // Set the current level as new level. This is necessary because the editor
//    // has changed the board and therefore the level has to be reloaded from scratch.
//    setLevelForPlaying(currentLevel.getNumber());

//    // Redo the history until the current board position is reached. To
//    // avoid crashes this isn't done when the level is invalid.
//    if (gameMode != GameMode.INVALID_LEVEL) {
//      redoMovementsWithoutDisplay(currentMovementNumber);
//    }

//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();

//    // The editor changes have been canceled.
//    editor.hasBoardBeenChanged = false;
//  }

//  /**
//   * Returns a <code>Level</code> object containing the data of the currently shown board.
//   * <p>
//   * This method is called when a level created in the editor must be transformed to xsb data.
//   *
//   * @return the <code>LevelData</code> object of the current level
//   */
//  private Level getLevelFromBoard() {

//    // Level to be returned.
//    Level level = new Level(levelIO.database);

//    level.setBoardData(board.getBoardDataAsString());
//    level.setLevelTitle(Texts.getText("newlevel"));
//    level.setWidth(board.width);
//    level.setHeight(board.height);

//    return level;
//  }

//  /**
//   * Enables or disables the buttons for "undo" and "redo" by inspection of
//   * the history, i.e. whether there is something to undo or redo.
//   */
//  private void setUndoRedoFromHistory() {
//    applicationGUI.setUndoButtonsEnabled(movesHistory.hasPrecedingMovement());
//    applicationGUI.setRedoButtonsEnabled(movesHistory.hasSuccessorMovement());
//  }

//  /**
//   * Copies the data of the current level to the clipboard, optionally
//   * using run length encoding (RLE), and/or transformation.
//   *
//   * @param withRLE whether to use RLE
//   * @param withTransform whether to transform the Board
//   */
//  private void exportLevelToClipboard(boolean withRLE, boolean withTransform) {
//    /*
//     * NB: Internally, we have to transform, first, and do RLE last.
//     */

//        // Data to be copied to the clipboard.
//        StringBuilder clipboardData = new StringBuilder();

//        // Add the board of the current level to the clipboard data.
//    // The transformation wants the board data split up into lines
//        // and needs to transform them all together.
//    List<String> boardData = getLevelFromBoard().getBoardData();
//    if (withTransform) {
//      boardData = Transformation.getTransformedBoardData(boardData);
//    }

//    // Instead of applying RLE to the complete string built from a list
//    // of rows, we apply RLE to each row before we concatenate them.
//    // While we have no empty rows, there is no difference.
//    for (String boardRow : boardData) {

//      // Empty board rows are called "interior rows" and are used in some levels
//      // to represent special esthetic arrangements. The currently supported
//      // file format represents these "empty rows" using a single "-".
//      if(boardRow.trim().isEmpty()) {
//        boardRow = "-";
//      }

//      clipboardData.append(withRLE ? RunLengthFormat.runLengthEncode(boardRow) : boardRow);
//      clipboardData.append("\n");
//    }

//        // Get the LURD-representation of the history and store it in the clipboard.
//        String lurdString = movesHistory.getLURDFromHistory();

//        // Store the history string in the clipboard if there is any.
//        if (lurdString.length() > 0) {
//          if (withTransform) {
//              lurdString = Transformation.getTransformedLURDInternalToExternal(lurdString);
//            }
//          if (withRLE) {
//            lurdString = RunLengthFormat.runLengthEncode(lurdString);
//          }
//            clipboardData.append("\n\nSavegame: \n" + lurdString);
//        } else {
//            // There isn't any history: the level is in the beginning position.
//          // In this case show / store all solutions in the clipboard.
//            SolutionsManager solutions = currentLevel.getSolutionsManager();
//            for (Solution solution : solutions.getSolutions()) {
//                String lurd = solution.lurd;
//                if (withTransform) {
//                  lurd = Transformation.getTransformedLURDInternalToExternal(lurd);
//                }
//                if (withRLE) {
//          lurd = RunLengthFormat.runLengthEncode(lurd);
//        }
//        clipboardData.append("\n\n" + "Solution"			//Solution is a key word, hence no translation
//                                 + ": " + solution.movesCount
//                                 + " "  + Texts.getText("moves")
//                                 + ", " + solution.pushesCount
//                                 + " "  + Texts.getText("pushes")
//                                 + "\n" + lurd);
//            }
//        }

//        // Add one last "\n" so the cursor is located at the beginning of the line.
//        clipboardData.append("\n");

//        // Store the collected data in the clipboard.
//        Utilities.putStringToClipboard(clipboardData.toString());
//  }

//  /**
//   * Copies the data of the current level to the clipboard, optionally
//   * using run length encoding (RLE).
//   *
//   * @param withRLE whether to use RLE
//   * @see #exportLevelToClipboard(boolean, boolean)
//   */
//  private void exportLevelToClipboard(boolean withRLE) {
//    exportLevelToClipboard(withRLE, false);
//  }

//  /**
//   * Moves the player to the passed direction (if possible).
//   * <p>
//   * The direction is passed as "external" direction that means if the direction
//   * is UP this coding checks whether the board is displayed transformed (rotated or flipped)
//   * and transforms the direction to the proper internal direction which might be DOWN (because
//   * the board is drawn 180° rotated to the screen).
//   * If a box is next to the player located on the square, the player should move to, the box is
//   * pushed (if possible). This method increases the moves and pushes currently played if
//   * necessary and stores the moves in the movement history.
//   *
//   * @param dir  direction of the push
//   */
//  private void movePlayerExternalStep(int dir) {
//    int internalDirection = Transformation.getInternalDirection(dir);
//    int newPlayerPosition = board.getPosition(board.playerPosition, internalDirection);
//    movePlayer(newPlayerPosition, false);
//  }

//  /**
//   * Handles all action events fired by using the keyboard.
//   *
//   * @param evt  the action event to be handled.
//   */
//  public void keyEvent(KeyEvent evt) {

//    // The debug mode can be activated by pressing keys.
//    Debug.keyPressed(evt);
//  }

//  /**
//   * Handles all action events not fired by a key or the mouse.
//   *
//   * @param evt  the action event to be handled.
//   */
//  @Override
//  public void actionPerformed(ActionEvent evt) {

//    // Check whether the current status of the program allows
//    // action handling at the moment.
//    if (preActionHandling(evt, false) == true) {
//      return;
//    }

//    String action = evt.getActionCommand();

//    // If the current level is left, but has not been saved yet,
//    // the "save level" dialog is shown.
//    // The saved status is stored in the collection,
//    // because the whole collection has to be saved, anyway.
//    if (   currentLevelCollection.getFile().isEmpty()     // yet not stored in a file
//      && !currentLevel.isConnectedWithDB() 			  // level isn't stored in the database
//      && !currentLevel.getBoardDataAsString().isEmpty() // empty levels needn't to be saved
//      && (   action.startsWith("load")
//        || action.equals("importLevelFromClipboard")
//        || action.equals("collectionSelected")
//        || action.equals("previousLevel") || action.equals("anyLevel")
//        || action.equals("nextLevel")     || action.equals("newLevel"))) {

//      // Ask the user whether the level is to be saved.
//      int chosenOption = JOptionPane.showConfirmDialog(this, Texts.getText("save_current_level"), "",
//                                                   JOptionPane.YES_NO_CANCEL_OPTION,
//                                                   JOptionPane.QUESTION_MESSAGE);
//      if (chosenOption == JOptionPane.YES_OPTION) {
//        if (applicationGUI.getSaveLevelMenuItem().isEnabled()) {
//          actionPerformed(new ActionEvent(this, 0, "saveLevel"));
//        } else {
//          actionPerformed(new ActionEvent(this, 0, "saveLevelAs"));
//        }
//      } else if (chosenOption == JOptionPane.CANCEL_OPTION) {

//        // If the collection has been selected from the combo box,
//        // then the current level collection is set back.
//        levelCollectionsList.setSelectedItem(currentLevelCollection);

//        return;
//      }
//    }

//    /*
//     * Actions for moving the player start with "move".
//     */
//    if(action.startsWith("move")) {

//      // While the solver is running or the level is invalid the player can't move.
//      // TODO: solver should be a modal JDialog
//      if (solverGUI != null || gameMode == GameMode.INVALID_LEVEL) {
//        return;
//      }

//      movePlayerExternalStep(action.equals("moveUp")   ? UP :
//                   action.equals("moveDown") ? DOWN :
//                   action.equals("moveLeft") ? LEFT : RIGHT);

//      return;
//    }


//    /*
//     * Level events
//     */

//    /*
//     * Let the user choose the level to load.
//     */
//    if (action == "loadIndividualLevel") {
//      try {
//        FileFilter fileFilter = new FileFilter() {
//          @Override
//          public boolean accept(File f) {
//            final String lowerCaseName = f.getName().toLowerCase();
//            return     lowerCaseName.endsWith(".sok")
//                || lowerCaseName.endsWith(".txt")
//                || lowerCaseName.endsWith(".xsb")
//                || f.isDirectory();
//          }

//          @Override
//          public String getDescription() {
//            return Texts.getText("supported_leveltypes");
//          }
//        };

//        // Let the user pick the level file. (null = no default directory)
//        String[] fileData = applicationGUI.getFileDataForLoading(null, Texts.getText("choose_level"), fileFilter);

//        // If the user hasn't chosen a collection file return immediately.
//        if (fileData == null) {
//          return;
//        }

//        // Load a collection and set the first of the new levels for playing.
//        LevelCollection levelCollection = levelIO.getLevelCollectionFromFile(fileData[1]);

//        // If the collection is empty, inform the user that no useful
//        // data has been found, and return.
//        if (levelCollection.isEmpty()) {
//          JOptionPane.showMessageDialog(this, Texts.getText("noLevelsFound"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
//          return;
//        }

//        // Set the new collection as current collection and the first
//        // level for playing.
//        setCollectionForPlaying(levelCollection);
//        setLevelForPlaying(1);
//        return;

//      } catch (IOException e) {
//        MessageDialogs.showExceptionFailure(this, e);
//      }
//    }

//    /*
//     * A collection has been selected (from the combox box in the GUI).
//     */
//    if(action.startsWith("collectionSelected")) {

//      SelectableLevelCollection selectedLevelCollection = levelCollectionsList.getSelectedItem();

//      if(selectedLevelCollection == null) {
//        return;
//      }

//      // If it's a collection from the database it can be loaded from there.
//      if(selectedLevelCollection.databaseID != Database.NO_ID) {
//        LevelCollection levelCollection = levelIO.database.getLevelCollection(selectedLevelCollection.databaseID);
//        setCollectionForPlaying(levelCollection);
//        setLevelForPlaying(1);
//        redraw(false);
//        return;
//      }

//      // If the collection has been loaded from file load it again from that file.
//      if(!selectedLevelCollection.file.isEmpty()) {
//        // Just change the action command.
//        action = "loadLevel_"+selectedLevelCollection.file;
//      }
//    }

//    /*
//     * Load and set the level
//     */
//    if (action.startsWith("loadLevel_")) {
//      try {
//        // Load a collection and set the first of the new levels for playing.
//        LevelCollection levelCollection = levelIO.getLevelCollectionFromFile(action.substring(10));

//        // If the collection is empty, inform the user that no useful
//        // data have been found and return.
//        if (levelCollection.isEmpty()) {
//          JOptionPane.showMessageDialog(this, Texts.getText("noLevelsFound"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
//          return;
//        }

//        // Set the new collection as current collection and the first
//        // level for playing.
//        setCollectionForPlaying(levelCollection);
//        setLevelForPlaying(1);

//        return;

//      } catch (IOException e) {
//        MessageDialogs.showExceptionFailure(this, e);

//        // Set back the current level collection for the case that the level collection to be loaded
//        // has been selected from the combo box in the GUI. This doesn't fire an event because
//        // the combo box handles events only when the popup is visible (see class GUI for implementation).
//        levelCollectionsList.setSelectedItem(currentLevelCollection);
//      }
//    }


//    /*
//     * Load and set a level from the clipboard.
//     */
//    if (action.equals("importLevelFromClipboard")) {

//      // Import the new collection.
//      LevelCollection levelCollection = levelIO.getLevelCollectionFromStringDataFlavor(Toolkit.getDefaultToolkit().getSystemClipboard().getContents(null));

//      // If no relevant data have been found inform the user and return.
//      if (levelCollection.isEmpty()) {
//        JOptionPane.showMessageDialog(this, Texts.getText("noLevelsFound"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
//        return;
//      }

//      // Set the new collection and the first of the new loaded levels for playing.
//      setCollectionForPlaying(levelCollection);
//      setLevelForPlaying(1);

//      return;
//    }

//    /*
//     * A level has been selected (from the combox box in the GUI).
//     */
//    if(action.startsWith("levelSelected_")) {
//      try {
//        int selectedIndex = Integer.parseInt(action.substring(14));
//        int levelNo = selectedIndex + 1; // First level is level 1, selection starts with 0
//        setLevelForPlaying(levelNo);
//      }catch(NumberFormatException e) {}

//      return;
//    }


//    /*
//     * Save level. This coding presumes that a level has been saved before
//     * and is to be saved to the same file location again.
//     */
//    if (action.equals("saveLevel")) {

//      // The level to be saved.
//      Level level = currentLevel;

//      // If a level from the editor is to be saved it has first to be
//      // translated into a new Level object.
//      if (isEditorModeActivated()) {
//        board.adoptBoardFromEditor(applicationGUI);
//        level = getLevelFromBoard();
//      }

//      try {
//        levelIO.saveLevel(level, currentLevelCollection.getFile());

//        // A saved level must be treated as "not modified".
//        if (isEditorModeActivated()) {
//          editor.hasBoardBeenChanged = false;
//        }

//      } catch (IOException e) {
//        MessageDialogs.showExceptionFailure(this, e);
//      }

//      // If the level is saved from the editor the level is now put back
//      // to the editor because the editor
//      // hasn't all the data anymore.
//      if (isEditorModeActivated() == true) {
//        editor.newLevel();
//      }

//      return;
//    }

//    /*
//     * Save level as.
//     */
//    if (action == "saveLevelAs") {

//      // Let the user pick the level file. (null = no default directory)
//      String filePath = applicationGUI.getFileDataForSaving(null);

//      // If the user hasn't chosen a collection file return immediately.
//      if (filePath == null) {
//        return;
//      }

//      // The level to be saved.
//      Level level = currentLevel;

//      // If a level from the editor is to be saved it has first to be
//      // translated into a new Level object.
//      if (isEditorModeActivated() == true) {
//        board.adoptBoardFromEditor(applicationGUI);
//        level = getLevelFromBoard();
//      }

//      // A level saved to a new file is treated as new collection.
//      level.setNumber(1); //TODO: level is mutable. This is only safe because the old level isn't used anymore!
//      LevelCollection.Builder levelCollectionBuilder = new LevelCollection.Builder().setLevels(level);

//      try {
//        levelIO.saveLevel(level, filePath);

//        // If saving the level succeeds the "Save level" menu item can
//        // be set enabled.
//        applicationGUI.getSaveLevelMenuItem().setEnabled(true);

//        // A saved level must be treated as "not modified".
//        if (isEditorModeActivated()) {
//          editor.hasBoardBeenChanged = false;
//        }

//        // Set the file the collection has been saved to.
//        levelCollectionBuilder.setCollectionFile(filePath);

//        levelCollectionBuilder.setTitle(Utilities.getFileName(filePath));

//        // Set the new collection as current collection. If the editor
//        // is activated the level is just saved,
//        // but the collection isn't set as new collection so the user
//        // can continue playing the loaded collection.
//        if (isEditorModeActivated() == false) {
//          setCollectionForPlaying(levelCollectionBuilder.build());
//        }

//      } catch (IOException e) {
//        MessageDialogs.showExceptionFailure(this, e);
//      }

//      // If the level is saved from the editor the level is now put back
//      // to the editor because the editor
//      // hasn't all the data anymore (board has changed).
//      if (isEditorModeActivated() == true) {
//        editor.newLevel();
//      }

//      return;
//    }

//    /*
//     * Save collection as.
//     */
//    if (action == "saveCollectionAs") {

//      // Let the user pick the level file. (null = no default directory)
//      String fileName = applicationGUI.getFileDataForSaving(null);

//      // If the user hasn't chosen a collection file return immediately.
//      if (fileName == null) {
//        return;
//      }

//      try {
//        levelIO.saveCollection(currentLevelCollection, fileName);

//        // Save the location of the file in the LevelCollection object.
//        currentLevelCollection = currentLevelCollection.setFileLocation(fileName);

//        // A collection for another file is being played. Hence, set this as new collection.
//        setCollectionForPlaying(currentLevelCollection);

//        // A saved collection must consider all levels as "not modified".
//        if (isEditorModeActivated()) {
//          editor.hasBoardBeenChanged = false;
//        }

//      } catch (IOException e) {
//        MessageDialogs.showExceptionFailure(this, e);
//      }

//      return;
//    }

//    /*
//     * Exports the level to the clipboard.
//     */
//    if (action.equals("exportLevelToClipboard")) {
//          exportLevelToClipboard(false);

//      return;
//    }

//    /*
//         * Exports the run length encoded level to the clipboard.
//         */
//        if (action.equals("exportLevelToClipboard(RLE)")) {
//          exportLevelToClipboard(true);

//            return;
//        }

//    /*
//     * Exports the level to the clipboard regarding all transformations
//     * (mirroring, flipping of the board).
//     */
//    if (action.equals("exportLevelToClipboardWithTransformation")) {
//      exportLevelToClipboard(false, true);	// no RLE, but transform

//      return;
//    }

//    // Jump to next level.
//    // If the last level is currently displayed jump to the first one.
//    if (action == "nextLevel") {
//      int currentLevelNumber = currentLevel.getNumber() + 1;
//      if(currentLevelNumber > currentLevelCollection.getLevelsCount()) {
//        currentLevelNumber = 1;
//      }

//      setLevelForPlaying(currentLevelNumber);

//      return;
//    }

//    // Jump to next unsolved level.
//    if (action == "nextUnsolvedLevel") {

//      // Get the number of the current level.
//      int levelNo = currentLevel.getNumber();

//      // Search unsolved level in the whole collection (except the
//      // currently loaded level).
//      for (int counter = currentLevelCollection.getLevelsCount() - 1; counter > 0; counter--) {

//        // Calculate the number of the next level. If it's the last
//        // level jump to the first level.
//        if (++levelNo > currentLevelCollection.getLevelsCount()) {
//          levelNo = 1;
//        }

//        // Get the level from the collection.
//        Level level = currentLevelCollection.getLevel(levelNo);

//        // If an unsolved level has been found set it for playing.
//        if (level.getSolutionsManager().getSolutionCount() == 0) {
//          setLevelForPlaying(levelNo);
//          return;
//        }
//      }

//      // Display a message since no unsolved level has been found.
//      MessageDialogs.showInfoNoteTextKey(this, "nextUnsolvedLevel.message");

//      return;
//    }

//    // Jump to previous unsolved level.
//    if (action == "previousUnsolvedLevel") {

//      // Get the number of the current level.
//      int levelNo = currentLevel.getNumber();

//      // Search unsolved level in the whole collection (except the
//      // currently loaded level).
//      for (int counter = currentLevelCollection.getLevelsCount() - 1; counter > 0; counter--) {

//        // Calculate the number of the next level. If it's the first
//        // level jump to the last level.
//        if (--levelNo < 1) {
//          levelNo = currentLevelCollection.getLevelsCount();
//        }

//        // Get the level from the collection.
//        Level level = currentLevelCollection.getLevel(levelNo);

//        // If a unsolved level has been found set it for playing.
//        if (level.getSolutionsManager().getSolutionCount() == 0) {
//          setLevelForPlaying(levelNo);
//          return;
//        }
//      }

//      // Display a message since no unsolved level has been found.
//      MessageDialogs.showInfoNoteTextKey(this, "previousUnsolvedLevel.message");

//      return;
//    }

//    // Jump to the previous level. If the first level is currently displayed
//    // jump to the last one.
//    if (action == "previousLevel") {
//      if (currentLevel.getNumber() > 1) {
//        setLevelForPlaying(currentLevel.getNumber() - 1);
//      } else {
//        setLevelForPlaying(currentLevelCollection.getLevelsCount());
//      }

//      return;
//    }

//    // Jump to a specific level.
//    if (action == "anyLevel") {
//      String levelNo = JOptionPane.showInputDialog(this, Texts.getText("levelnumber") + ":", "");

//      // Return immediately when no level number has been entered.
//      if (levelNo == null) {
//        return;
//      }

//      try {
//        // Parse the entered level number.
//        int levelnumber = Integer.parseInt(levelNo.trim());

//        // If the level number is valid set the selected level.
//        if (levelnumber > 0 && levelnumber < currentLevelCollection.getLevelsCount() + 1) {
//          setLevelForPlaying(levelnumber);
//        }

//      } catch (NumberFormatException n) { /* just continue */ }

//      return;
//    }

//    // Open the graphical level browser.
//    if (action.equals("openGraphicalLevelBrowser")) {
//      GraphicalLevelBrowser browser = new GraphicalLevelBrowser(this);
//      LevelCollection currentlyPlayingLevelCollection = currentLevelCollection;
//      browser.addLevelCollection(currentlyPlayingLevelCollection, 1, currentlyPlayingLevelCollection.getLevelsCount());
//        browser.showAsDialog(Texts.getText("graphicalLevelBrowser.title"));
//    }

//    // Jump to a specific movement.
//    if (action.equals("jumpToMovementHistorySlider")) {

//      int movementNoToJumpTo = 0;

//      // The slider mustn't be used when the level is invalid.
//      if (gameMode == GameMode.INVALID_LEVEL) {
//        return;
//      }

//      JSlider slider = (JSlider) evt.getSource();

//      // Jump to the history browser movement that has been set with the slider.
//      movementNoToJumpTo = slider.getValue();

//      // Delete the info text.
//      displayInfotext("");

//      // Jump to the specific movement and redraw the new board position.
//      undoAllMovementsWithoutDisplay();
//      if (movementNoToJumpTo > 0) {
//        redoMovementsWithoutDisplay(movesHistory.getMovementNoOfHistoryBrowserMovement(movementNoToJumpTo));
//      }
//      redraw(false);

//      // Enable / disable the undo / redo button depending on the history status.
//      setUndoRedoFromHistory();

//      return;
//    }


//    // Jumps to the previous history browser movement. The history browser
//    // doesn't jump by push or move because this would result in too many
//    // small steps. Instead all pushes of the same box are combined to one movement.
//    // However, this can be overruled by the setting "singleStepUndoRedo".
//    if (action.contains("HistoryBrowserMovement")) {

//      // The slider mustn't be used when the level is invalid.
//      if (gameMode == GameMode.INVALID_LEVEL) {
//        return;
//      }

//      // If the editor or the solver is activated the slider mustn't
//      // be shown. They both have no own class for displaying the board, yet :(
//      if(isEditorModeActivated() || solverGUI != null) {
//        return;
//      }

//      // The user can enforce single step undo / redo. In this case the redo and undo
//      // method can be used and called directly on the event dispatcher thread.
//      if(Settings.singleStepUndoRedo) {

//        if(action.contains("jumpToNext")) {
//          redoMovement(false);
//        }
//        else {
//          undoMovement();
//        }
//        return;
//      }

//      int oldMovesCount = movesCount;
//            int currentHistoryMovementNo = movesHistory.getHistoryBrowserMovementNoFromMovementNo();
//      int newHistoryMovementNo = currentHistoryMovementNo + (action.contains("jumpToNext") ? 1 : -1);
//      int newMovementNo = movesHistory.getMovementNoOfHistoryBrowserMovement(newHistoryMovementNo);

//      // Jump to the specific movement and redraw the new board position.
//      undoAllMovementsWithoutDisplay();
//      if(newHistoryMovementNo > 0) {
//        redoMovementsWithoutDisplay(newMovementNo);
//      }

//      // If the history doesn't contain the requested movement then return immediately.
//      // This happens for example, if the user scrolls backwards for jumping to previous movements
//      // but there aren't any previous movements.
//      if(movesCount != oldMovesCount) {

//        // Delete the info text. It may contain old text (for instance a deadlock information message).
//        displayInfotext("");

//        // Enable / disable the undo / redo button depending on the history status.
//        setUndoRedoFromHistory();

//        redraw(false);
//      }

//      return;
//    }


//    // Show the GUI for the optimizer.
//    if (action == "openOptimizer") {

//      // Create a new optimizer. The optimizer is shown in an own JFrame.
//      new OptimizerGUI(this);

//      return;
//    }

//    /*
//     * Editor menu
//     */
//    // Activate / activate editor mode
//    if (action == "activateEditor") {
//      switchEditorPlayMode();

//      return;
//    }

//    // New level for the editor.
//    if (action == "newLevel") {

//      if (isEditorModeActivated() == false) {
//        switchEditorPlayMode();
//      }

//      // Set minimal size of board.
//      board.newBoard(3, 3);

//      // Inform the editor about the new level.
//      editor.newLevel();

//      return;
//    }

//    // All changes are to be ignored and the editor is left.
//    if (action == "cancelEditor") {
//      setPlaymodeWithCancelingEditorChanges();
//      return;
//    }

//    /*
//     * Database menu
//     */
//    // Opens a dialog to browse in the database.
//    if (action.startsWith("browseDatabase")) {

//      // Displays the LevelManagement-GUI in a modal JDialog. Hence, this
//      // threads stops until the JDialog is closed.
//      DatabaseGUI databaseGUI = new DatabaseGUI(this);

//      // The user may change the currently played collection in the database GUI.
//      // For a better performance the changes done in the GUI are filtered for
//      // relevant changes regarding the current collection. The loaded collection
//      // is reloaded from the database only when relevant changes have occurred.
//      final AtomicBoolean isReloadOfCollectionNecessary = new AtomicBoolean(); // mutable boolean

//      final HashSet<Integer> levelIDs    = new HashSet<Integer>();
//      final HashSet<Integer> authorIDs   = new HashSet<Integer>();
//      final HashSet<Integer> solutionIDs = new HashSet<Integer>();
//      final HashSet<Integer> snapshotIDs = new HashSet<Integer>();

//      // Collect all database IDs in the current collection. This way we can listen
//      // to changes in the database and check whether any data of the current collection
//      // is changed in the database.
//      authorIDs.add(currentLevelCollection.getAuthor().getDatabaseID());
//      for(Level level : currentLevelCollection) {
//        if(level.isConnectedWithDB()) {
//          levelIDs.add(level.getDatabaseID());
//          authorIDs.add(level.getAuthor().getDatabaseID());

//          for(Solution solution : level.getSolutionsManager().getSolutions()) {
//            solutionIDs.add(solution.databaseID);
//          }

//          for(Snapshot snapshot : level.getSnapshots()) {
//            snapshotIDs.add(snapshot.getDatabaseID());
//          }
//        }
//      }

//      // Save the current history in the played level. This way the level can be reloaded from database
//      // and the save game can be restored.
//      currentLevel.setSaveGame(new Snapshot(movesHistory.getHistoryAsSaveGame(), true));

//      // Check if any part of the currently played collection is changed in the database.
//      // If yes, all data of the collection are reloaded from the database.
//      // Reloading only the parts that have been changed is faster but reloading all
//      // data is safer and the performance isn't that bad.
//      DatabaseEventListener databaseEventListener = new DatabaseEventListener() {
//        @Override
//        public void databaseEventFired(DatabaseDataEvent event) {

//          if(Debug.isDebugModeActivated) {
//            System.out.println("fired: "+event.getEventObjectClass()+ "  id: "+event.getDatabaseIDs());
//          }

//          if(isReloadOfCollectionNecessary.get() == false) {
//            for(int id : event.getDatabaseIDs()) {
//              if(event.getEventObjectClass() == LevelCollection.class && id == currentLevelCollection.getDatabaseID()
//              || event.getEventObjectClass() == Level.class 		    && levelIDs.contains(id)
//              || event.getEventObjectClass() == Author.class 			&& authorIDs.contains(id)
//              || event.getEventObjectClass() == Solution.class 		&& solutionIDs.contains(id)
//              || event.getEventObjectClass() == Snapshot.class 		&& snapshotIDs.contains(id)) {
//                isReloadOfCollectionNecessary.set(true);
//              }
//            }
//          }
//        }
//      };

//      levelIO.database.addDatabaseEventListener(databaseEventListener);

//      databaseGUI.showAsModalDialog();

//      levelIO.database.removeDatabaseEventListener(databaseEventListener);


//      // The user may have changed the stored level collections. Hence, the list of available collections has to be updated.
//      levelCollectionsList.updateAvailableCollections(levelIO.database);
//      applicationGUI.repaint();

//      // If there weren't any changes of the current collection then just return.
//      if(!isReloadOfCollectionNecessary.get()) {
//        // If the clipboard isn't permanently stored the collection list doesn't contain the collection.
//        // However, the collection title is displayed as selected item, anyhow. Hence, set the collection.
//        if(!currentLevelCollection.isConnectedWithDatabase() && currentLevelCollection.getFile().isEmpty()) {
//          setCollectionForPlaying(currentLevelCollection);
//        }
//        return;
//      }

//      // currentLevel might have been deleted. However, "setLevelForPlaying" will try to save the savegame
//      // for the level in the database. Hence, the database connection is removed.
//      currentLevel.setDatabaseID(Database.NO_ID);

//      // The collection data (title, author, ...) can only be read when it is connected with the db.
//      // If it is and it is to be reloaded then the whole collection is reloaded from the database.
//      if(currentLevelCollection.isConnectedWithDatabase()) {
//        LevelCollection levelCollection = levelIO.database.getLevelCollection(currentLevelCollection.getDatabaseID());

//        // If the user hasn't deleted the collection then set it and we have the current version active for playing.
//        if(levelCollection != null) {
//          setCollectionForPlaying(levelCollection);
//          setLevelForPlaying(currentLevel.getNumber()); // May result in another level to be played if user has added/removed levels from the collection in the DB
//          return;
//        }

//        // The user has deleted the collection from the database.
//        currentLevelCollection = currentLevelCollection.setDatabaseID(Database.NO_ID);
//      }

//      /**
//       * The collection isn't connected with the database. Hence, the level collection data itself can remain unchanged.
//       * However: none, some or all levels may be connected with the database => reload the data from the database.
//       */

//      // We build a whole new collection (like a fresh one loaded from disk) but keep the connection of all levels in the database.
//      ArrayList<Level> newCollectionLevels = new ArrayList<Level>();
//      for(Level level : currentLevelCollection) {
//        if(level.isConnectedWithDB()) {
//          Level freshLevel = levelIO.database.getLevel(level.getDatabaseID());
//          if(freshLevel != null) {
//            level = freshLevel;
//          }
//          else {
//            // The user has deleted the level form the database.
//            level.setDatabaseID(Database.NO_ID);
//          }
//        }
//        newCollectionLevels.add(level);
//      }

//      LevelCollection newLevelCollection = currentLevelCollection.getBuilder()
//                         .setLevels(newCollectionLevels).build();

//      setCollectionForPlaying(newLevelCollection);
//      setLevelForPlaying(currentLevel.getNumber());

//      // The currently loaded collection may have changed. Hence, the
//      // number of levels may also have changed => update the GUI.
//      applicationGUI.repaint();

//      return;
//    }

//    /*
//     * Import the current level to the database.
//     */
//    if (action.startsWith("importLevelToDB")) {

//      // If the level isn't connected with the database, check whether it is already stored there.
//      if (!currentLevel.isConnectedWithDB() || currentLevel.isStoredAsDelegate()) {
//        ArrayList<Level> levelsInDB = levelIO.database.getLevel(currentLevel.getBoardDataAsString());

//        for(Level levelInDB : levelsInDB) {

//          // A delegate level can be ignored because this new level will be stored
//          // as "regular" level. This way the author, level comments, ... don't have to be
//          // shared will other levels similar to the delegate level.
//          if(levelInDB.isStoredAsDelegate()) {
//            continue;
//          }

//          // Get the name of the level that is already stored.
//          String levelName = Utilities.clipToEllipsis(levelInDB.getTitle(), 30);

//          // Ask the user what to do: save as new level or save new solutions only.
//          String answer = (String) JOptionPane.showInputDialog(this,
//              Texts.getText("levelAlreadyStored")
//              + "\n" + Texts.getText("level")
//              + ": " + levelName,
//              Texts.getText("note"),
//              JOptionPane.QUESTION_MESSAGE,
//              null,
//              new Object[] { Texts.getText("storeLevelAsNewOne"),
//            Texts.getText("storeNewSolutionsOnly") },
//            null);

//          // If the user has canceled the InputDialog return immediately.
//          if (answer == null) {
//            return;
//          }

//          // Add the current solutions to the solutions stored in the database.
//          if (answer.equals(Texts.getText("storeNewSolutionsOnly"))) {

//            for (Solution solution : currentLevel.getSolutionsManager().getSolutions()) {
//              levelIO.database.insertSolution(solution, levelInDB.getDatabaseID());
//            }

//            return;
//          }

//          // Just the first found level is shown to the user to make the dialog not too complex.
//          break;
//        }
//      } else {
//        // The level is already connected with the database. Ask the user whether it is to be saved as new level.
//        int answer = JOptionPane.showConfirmDialog(this, Texts.getText("levelAlreadyStored") + " " +
//            Texts.getText("storeLevelAsNewOne") + "?", Texts.getText("question"), JOptionPane.OK_CANCEL_OPTION, JOptionPane.QUESTION_MESSAGE);
//        if (answer == JOptionPane.CANCEL_OPTION) {
//          return;
//        }
//      }

//      // The level may be a delegate. However, after being saved it's just a normal level.
//      currentLevel.setDelegate(false);

//      // Store the level as new level in the database.
//      levelIO.database.insertLevel(currentLevel);

//      // Show a info message to inform the user about the imported level.
//      JOptionPane.showMessageDialog(this, Texts.getText("levelImportedToDatabase"));

//      // The level is saved, hence it isn't a temporarily level anymore.
//      if (editor != null) {
//        editor.hasBoardBeenChanged = false;
//      }

//      return;
//    }

//    /*
//     * Import the current collection to the database.
//     */
//    if (action.startsWith("importCollectionToDB")) {

//      // If the collection is connected to the database the user may only
//      // import it again as new collection.
//      if (currentLevelCollection.isConnectedWithDatabase()) {

//        // Ask the user whether he wants to import the collection as new one or not.
//        if (JOptionPane.showConfirmDialog(this, Texts.getText("collectionAlreadyInDatabase"),
//            Texts.getText("question"), JOptionPane.OK_CANCEL_OPTION, JOptionPane.QUESTION_MESSAGE) == JOptionPane.CANCEL_OPTION) {
//          return;
//        }
//      }

//      // Save the collection in the database.
//      LevelCollection savedLevelCollection = levelIO.database.insertLevelCollection(currentLevelCollection);
//      if(savedLevelCollection != null) {
//        displayInfotext(Texts.getText("collectionImportedToDatabase") + "  (" + savedLevelCollection.getLevelsCount()+" "+Texts.getText("general.levels") + ")");

//        // Update the list of available collections in the database.
//        levelCollectionsList.updateAvailableCollections(levelIO.database);

//        // The collection is now connected to the database and no more with the file.
//        // If the user changes the database entry for this collection it must be avoided to change the file, too.
//        // Hence delete the file entry.
//        savedLevelCollection = savedLevelCollection.setFileLocation("");

//        // Set the new collection having the database ID for playing.
//        setCollectionForPlaying(savedLevelCollection);
//      }

//      return;
//    }

//    /*
//     * Import the current collection to the database.
//     */
//    if (action.startsWith("importCollectionsOfFolder")) {
//      importCollectionsOfFolderToDatabase();
//      levelCollectionsList.updateAvailableCollections(levelIO.database);

//      return;
//    }

//    /*
//     * Import data of another database.
//     */
//    if (action.startsWith("importDataFromOtherDatabase")) {
//      importDataFromAnotherDatabase();
//      levelCollectionsList.updateAvailableCollections(levelIO.database);

//      return;
//    }


//    /*
//     * Moves menu
//     */
//    // Undo the last movement.
//    if (action == "undo") {

//      // If just a single move has to be redone -> use the EDT.
//      // Otherwise use an extra thread in order not to block the EDT too long.
//      if(Settings.singleStepUndoRedo) {
//        undoMovement();
//      }
//      else {
//        undoMovementInOwnThread();
//      }

//      return;
//    }

//    // Redo the last undone movement.
//    if (action == "redo") {

//      // Just a single move has to be redone -> use the EDT.
//      if(Settings.singleStepUndoRedo) {
//        redoMovement(false);
//      }
//      else {
//        // The method redoes several moves. In order not to block the EDT
//        // an own thread is used for doing this.
//        redoMovementInOwnThread(false);
//      }

//      return;
//    }


//    // Replay functionality from the toolbar button.
//    if(action.equals("replay")) {
//      redoMovementInOwnThread(true);

//      return;
//    }

//    // Undo all movements
//    if (action == "undoAll") {

//      // Undo all movements
//      undoAllMovementsWithoutDisplay();

//      // Determine new level status (solved, unsolvable, ...)
//      analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//      redraw(false);

//      return;
//    }

//    // Redo all movements
//    if (action == "redoAll") {
//      // Redo all movements
//      redoMovementsWithoutDisplay(movesHistory.getMovementsCount() - 1);

//      // Determine new level status (solved, unsolvable, ...)
//      analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//      redraw(false);

//      return;
//    }

//    // Go to move ...
//    if (action == "goToMove") {

//      String movementNumberString = JOptionPane.showInputDialog(this, Texts.getText("goToMoveDialog"));

//      // Return immediately when no movement number has been entered.
//      if (movementNumberString == null) {
//        return;
//      }

//      try {
//        // Parse the entered number.
//        int movementNumber = Integer.parseInt(movementNumberString.trim());

//        // If the movement number is valid go to the entered movement.
//        if (movementNumber >= 0 && movementNumber <= movesHistory.getMovementsCount()) {
//          undoAllMovementsWithoutDisplay();
//          redoMovementsWithoutDisplay(movementNumber - 1);

//          // Determine new level status (solved, unsolvable, ...)
//          analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));

//          redraw(false);
//        }
//      } catch (NumberFormatException n) {
//        /* just continue */
//      }

//      return;
//    }

//    // Copy the lurd-string of the current movement history to the clipboard.
//    if (action == "copyMovesToClipboard") {

//      // Store the LURD-representation of the history in the clipboard.
//      Utilities.putStringToClipboard(Transformation.getTransformedLURDInternalToExternal(movesHistory.getLURDFromHistory()));

//      return;
//    }

//    // Copy the lurd-string of the current movement history from move x to
//    // move y to the clipboard.
//    if (action == "copyMovesXToYToClipboard") {

//      // Ask the user which part of the lurd is to be copied.
//      String answer = JOptionPane.showInputDialog(this, Texts.getText("copyMovesXToYDialog.question", 1, movesHistory.getMovementsCount()),
//          Texts.getText("copyMovesXToYDialog.title"), JOptionPane.QUESTION_MESSAGE);

//      // Just return if the user canceled the dialog.
//      if (answer == null) {
//        return;
//      }

//      try {
//        String[] inputNumbers = answer.split("-");

//        // The user must have entered two numbers separated by a "-".
//        if (inputNumbers.length != 2) {
//          return;
//        }

//        // Parse the entered numbers.
//        int fromMove = Integer.parseInt(inputNumbers[0].trim());
//        int toMove = Integer.parseInt(inputNumbers[1].trim());

//        // Ensure the range is valid.
//        if (fromMove < 1) {
//          fromMove = 1;
//        }
//        if (toMove > movesHistory.getMovementsCount() || toMove < fromMove) {
//          toMove = movesHistory.getMovementsCount();
//        }

//        // Internal the first movement is movement number 0. Hence,
//        // subtract 1.
//        fromMove--;
//        toMove--;

//        // Store the LURD-representation of the history in the clipboard.
//        Utilities.putStringToClipboard(Transformation.getTransformedLURDInternalToExternal(movesHistory.getLURDFromHistory(fromMove, toMove)));

//      } catch (NumberFormatException n) { /* do nothing */
//      }

//      return;
//    }

//    // Paste the lurd-string stored in the clipboard to the movement history.
//    if (action == "pasteMovesFromClipboard") {

//      // The imported lurd string.
//      String movesFromClipboard = "";

//      // Current number of movements.
//      int currentMovementNumberBackup = movesHistory.getCurrentMovementNo();

//      // Import the data from the clipboard.
//      Transferable transferable = Toolkit.getDefaultToolkit().getSystemClipboard().getContents(this);

//      try {
//        if (transferable != null && transferable.isDataFlavorSupported(DataFlavor.stringFlavor)) {
//          movesFromClipboard = (String) transferable.getTransferData(DataFlavor.stringFlavor);
//        }

//        // Transform the lurd string according to the current
//        // transformation.
//        movesFromClipboard = Transformation.getTransformedLURDExternalToInternal(movesFromClipboard);

//        String combinedMoves = movesHistory.getLURDFromHistory() + movesFromClipboard;

//        // Add the imported moves to the history.
//        int validMovesCount = setHistoryFromLURDString(combinedMoves);

//        // Undo all movements and then do all movements including the new ones.
//        undoAllMovementsWithoutDisplay();
//        redoMovementsWithoutDisplay(validMovesCount - 1); // first movement has index 0!

//      } catch (Exception e) { /* is handled in finally block */
//      } finally {
//        // If no additional movement has be done the imported moves from
//        // the clipboard aren't valid.
//        if (currentMovementNumberBackup == movesHistory.getCurrentMovementNo()) {
//          JOptionPane.showMessageDialog(this, Texts.getText("pasteMovesFromClipboard.noMovesFound"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);
//          return;
//        }
//      }

//      // Draw new game status
//      redraw(false);

//      // Play the move sound (even if a box has been pushed).
//      Sound.Effects.MOVE.play();

//      // Analyze the new board position by passing a dummy push (there
//      // might not have been a push at all in
//      // the imported movements, hence to be sure a dummy push is passed).
//      analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));

//      return;
//    }

//    // Display the pushes / moves as lurd string.
//    if (action == "showlurd") {

//      // Get the lurd string of the done moves and transform them
//      // according to the currently set transformation.
//      String lurdString = Transformation.getTransformedLURDInternalToExternal(movesHistory.getLURDFromHistory());

//      // Show a popup showing the lurd string.
//      applicationGUI.showPopupTextarea("LURD: ", lurdString);

//      return;
//    }

//    /*
//     * Solutions menu
//     */
//    // Imports a solution from the clipboard
//    if (action == "importSolution") {
//      currentLevel.getSolutionsManager().importSolutionFromClipboard();

//      return;
//    }

//    // Open the solution management frame.
//    if (action.startsWith("solutionManagement")) {
//      currentLevel.getSolutionsManager().displaySolutionsInDialog(this);

//      return;
//    }

//    /*
//     * Settings menu
//     */

//    // Change language
//    if (action.startsWith("language")) {
//      setLanguage(action.substring(8));

//      // Let the GUI set the language in all components.
//      applicationGUI.setNewLanguage();

//      // Disable / enable the undo / redo buttons depending on the history.
//      setUndoRedoFromHistory();

//      // Optimizer, solutions view,... aren't updated. Hence, a message for the user is displayed.
//      JOptionPane.showMessageDialog(this, Texts.getText("languageChange"), Texts.getText("note"), JOptionPane.INFORMATION_MESSAGE);

//      return;
//    }

//    // Deadlock detection
//    if (action.startsWith("detect")) {
//      if (action == "detectSimpleDeadlocks") {
//        Settings.detectSimpleDeadlocks = ((AbstractButton) evt.getSource()).isSelected();

//        // If simple deadlock detection is deactivated freeze- and
//        // bipartite deadlock detections
//        // also deactivated because they include simple deadlock
//        // detection.
//        if (Settings.detectSimpleDeadlocks == false) {
//          applicationGUI.detectFreezeDeadlocks.setSelected(false);
//          Settings.detectFreezeDeadlocks = false;
//          applicationGUI.detectBipartiteDeadlocks.setSelected(false);
//          Settings.detectBipartiteDeadlocks = false;
//        }
//      }
//      if (action == "detectFreezeDeadlocks") {
//        Settings.detectFreezeDeadlocks = ((AbstractButton) evt.getSource()).isSelected();
//      }
//      if (action == "detectCorralDeadlocks") {
//        Settings.detectCorralDeadlocks = ((AbstractButton) evt.getSource()).isSelected();
//      }
//      if (action == "detectBipartiteDeadlocks") {
//        Settings.detectBipartiteDeadlocks = ((AbstractButton) evt.getSource()).isSelected();
//      }
//      if (action == "detectClosedDiagonalDeadlocks") {
//        Settings.detectClosedDiagonalDeadlocks = ((AbstractButton) evt.getSource()).isSelected();
//      }

//      // Bipartite and freeze deadlock detection include a simple deadlock detection.
//      if (Settings.detectSimpleDeadlocks == false && (Settings.detectFreezeDeadlocks || Settings.detectBipartiteDeadlocks)) {
//        applicationGUI.detectSimpleDeadlocks.setSelected(true);
//        Settings.detectSimpleDeadlocks = true;
//      }

//      redraw(false);

//      return;
//    }

//    // Show the simple deadlock squares.
//    if (action == "showDeadlockSquares") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Settings.showDeadlockFields = checkbox.isSelected();
//      redraw(false);

//      return;
//    }

//    // Show the simple deadlock squares.
//    if (action == "showMinimumSolutionLength") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Settings.showMinimumSolutionLength = checkbox.isSelected();

//      // Display the minimum solution length in valid levels.
//      if (gameMode != GameMode.INVALID_LEVEL) {
//        analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//      }

//      return;
//    }

//    // Switch between moves and pushes optimized path finding.
//    if (action == "pathfindingOptimization") {

//      AbstractButton button = (AbstractButton) evt.getSource();

//      // Switch the value used for the path finding.
//      Settings.movesVSPushes = Settings.movesVSPushes == 0 ? 30000 : 0;

//      // Set the correct text according to the new setting.
//      button.setText(Settings.movesVSPushes == 0 ? Texts.getText("moves") : Texts.getText("pushes"));

//      return;
//    }

//    // Set sound enabled/disabled.
//    if (action == "sound") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Settings.soundEffectsEnabled = checkbox.isSelected();

//      return;
//    }

//    // Set the animation speed.
//    if (action == "animationSpeed") {

//      String input = JOptionPane.showInputDialog(this, Texts.getText("animationsspeed") + " (1-250):", 250 - Settings.delayValue + "");
//      if (input == null) {
//        return;
//      }

//      // If no valid new speed has been entered the old value is kept.
//      try {
//        short speed = (short) Integer.parseInt(input.trim());
//        if (speed >= 0 && speed <= 250) {
//          Settings.delayValue = (short) (250 - speed);
//        }
//      } catch (NumberFormatException n) { /* just continue */ }

//      return;
//    }

//    // Flag, indicating whether reversely played moves should be treated as undo.
//    if (action == "reverseMoves") {
//      Settings.treatReverseMovesAsUndo = ((AbstractButton) evt.getSource()).isSelected();

//      return;
//    }

//    // Flag, indicating whether moves between pushes should be optimized automatically.
//    if (action == "optimizeBetweenMoves") {
//      Settings.optimizeMovesBetweenPushes = ((AbstractButton) evt.getSource()).isSelected();

//      return;
//    }

//    // Flag specifying whether the reachable squares of a box are to be highlighted.
//    if (action == "showReachableBoxPositions") {
//      Settings.showReachableBoxPositions = ((AbstractButton) evt.getSource()).isSelected();

//      return;
//    }

//    // Set animation speed when undoing / redoing a movement.
//    if (action == "animationSpeedUndoRedo") {

//      String input = JOptionPane.showInputDialog(this,
//                Texts.getText("animationsspeedundoredo") + " (1-250):",
//                250 - Settings.delayValueUndoRedo + "" );
//      if (input == null) {
//        return;
//      }

//      // If no valid new speed has been entered the old value is kept.
//      try {
//        short speed = (short) Integer.parseInt(input.trim());
//        if (speed >= 0 && speed <= 250) {
//          Settings.delayValueUndoRedo = (short) (250 - speed);
//        }
//      } catch (NumberFormatException n) {/* just continue */ }

//      return;
//    }

//    // Sets the "single step undo" on/off
//    if (action == "singleStepUndoRedo") {
//      Settings.singleStepUndoRedo = !Settings.singleStepUndoRedo;

//      return;
//    }

//    // Show info about this program.
//    if (action == "aboutJSoko") {
//      new JSokoAboutBox(this);

//      return;
//    }

//    /*
//     * Debug actions ... by annotation.
//     */
//    Debug.DebugField actionField = Debug.findActionDebugField(action);
//    if (actionField != null) {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      actionField.setValue( checkbox.isSelected() );

//      if (actionField.anno.doZeroPos()) {
//        Debug.debugSquarePosition = 0;
//      }
//      if (actionField.anno.doRepaint()) {
//        repaint();
//      }
//      if (actionField.anno.doRedraw()) {
//        redraw(false);
//      }

//      // Check whether that is the complete action handling
//      if ( ! actionField.anno.doGoon()) {
//        return;
//      }
//      // We expect to have even more action coding below ...
//    }

//    /*
//     * Debug actions (non-annotated part).
//     */
//    if (action.equals("debugDrawOwnSkin")) {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugDrawOwnSkin = checkbox.isSelected();

//      String bgFilename;
//      if (Debug.debugDrawOwnSkin) {
//        bgFilename = "";
//      } else {
//        bgFilename = Settings.get("backgroundImageFile");
//      }

//      // set a new skin which is just used as a dummy
//      applicationGUI.mainBoardDisplay.setSkin(Settings.currentSkin);
//      try {
//        applicationGUI.mainBoardDisplay.setBackgroundGraphic(bgFilename);
//      } catch (FileNotFoundException e) {}
//      repaint();

//      return;
//    }

//    // Set push penalty. This value is added for every push.
//    // Hence, this value controls
//    // whether push or move optimal paths are preferred.
//    if (action == "setPushPenalty") {
//      float pushValue = 0;
//      String input = JOptionPane.showInputDialog(this, "Pushpenalty", "");
//      if (input == null) {
//        return;
//      }

//      input = input.trim().replace(',', '.');
//      try {
//        pushValue = Float.parseFloat(input);
//      } catch (NumberFormatException n) {/* just continue */
//      }
//      if (pushValue >= 0) {
//        Settings.movesVSPushes = pushValue;
//      }

//      return;
//    }

//    if (action == "showCorralForcerSituations") {
//      board.distances.debugShowCorralForcerSituations();
//      redraw(false);

//      return;
//    }

//    if (action == "showBoxData") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugBoxDataAreToBeShown = checkbox.isSelected();
//      Debug.debugShowBoxData = Debug.debugBoxDataAreToBeShown;

//      return;
//    }

//    if (action == "showLowerboundForward") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowLowerBoundForward = checkbox.isSelected();

//      // Do dummy push so the lower bound is calculated and shown immediately.
//      if (checkbox.isSelected()) {
//        analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//      }

//      return;
//    }

//    if (action == "showLowerboundBackward") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowLowerBoundBackward = checkbox.isSelected();

//      if (checkbox.isSelected() == true) {
//        analyzeNewBoardPosition(board.boxData.getBoxPosition(0), board.boxData.getBoxPosition(0));
//      }

//      return;
//    }

//    if (action == "showLowerboundAllLevels") {
//      calculateLowerboundOfAllLevel();

//      return;
//    }

//    if (action == "showBoxDistanceForward") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowBoxDistanceForward = checkbox.isSelected();

//      // For showing the distance between two squares there have to be
//      // selected two squares.
//      // The first selected square is saved in this variable, which is
//      // now initialized to zero, indicating "no square selected, yet."
//      Debug.debugSquarePosition = 0;

//      // Displaying "backwards" at the same time does not make sense.
//      Debug.debugShowBoxDistanceBackward = false;

//      return;
//    }

//    if (action == "showBoxDistanceBackward") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowBoxDistanceBackward = checkbox.isSelected();

//      // For showing the distance between two squares there have to be
//      // selected two squares.
//      // The first selected square is saved in this variable, which is
//      // now initialized to zero, indicating "no square selected, yet."
//      Debug.debugSquarePosition = 0;

//      // Displaying "forward" at the same time does not make sense.
//      Debug.debugShowBoxDistanceForward = false;

//      return;
//    }

//    // Display penalty squares of the board, simultaneously.
//    if (action == "showPenaltySquares") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowPenaltyFields = checkbox.isSelected();

//      if (checkbox.isSelected()) {
//        Penalty penalty = new Penalty(board);
//        penalty.debugShowPenaltySituations();
//      } else {
//        board.removeAllMarking();
//        redraw(false);
//      }

//      return;
//    }

//    // Display penalty squares of the board, separately.
//    if (action == "showPenaltySituationsSeparately") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();
//      Debug.debugShowPenaltyFields = checkbox.isSelected();

//      if (checkbox.isSelected()) {
//        Debug.debugShowPenaltySituationsSeparately = true;
//        Penalty penalty = new Penalty(board);
//        penalty.debugShowPenaltySituations();
//      } else {
//        board.removeAllMarking();
//        redraw(false);
//        Debug.debugShowPenaltySituationsSeparately = false;
//      }

//      return;
//    }

//    // Display penalty squares of the board, separately.
//    if (action == "showLevelAreas") {
//      AbstractButton checkbox = (AbstractButton) evt.getSource();

//      if (checkbox.isSelected()) {
//        Debug.debugShowDifferentReachableGoalsAreas = true;
//        redraw(false);
//      } else {
//        Debug.debugShowDifferentReachableGoalsAreas = false;
//        redraw(false);
//      }

//      return;
//    }

//    // Mark all boxes being part of a "closed diagonal deadlock".
//    if(action.equals("markClosedDiagonalDeadlockBoxes")) {

//      // Set the new settings status for "closed diagonal deadlocks".
//      Debug.debugShowAllClosedDiagonalDeadlocks = ((JCheckBoxMenuItem) evt.getSource()).isSelected();

//      // If "closed diagonal deadlocks" are to be displayed then display them all now.
//      if(Debug.debugShowAllClosedDiagonalDeadlocks == true) {
//        ClosedDiagonalDeadlock closedDiagonalDeadlockDetection = new ClosedDiagonalDeadlock(board);

//        board.removeAllMarking();

//        for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
//          if(closedDiagonalDeadlockDetection.isDeadlock(board.boxData.getBoxPosition(boxNo))) {
//            board.setMarking(board.boxData.getBoxPosition(boxNo));
//          }
//        }

//        redraw(false);
//      }
//    }

//    // Display all not identified deadlock positions of the current level.
//    if (action == "showNotIdentifiedDeadlockPositions") {
//      new DeadlockDebug(this).start();

//      return;
//    }

//    // Show the GUI for the solver.
//    if (action == "openSolver") {

//      // Hide the info bar of the main GUI.
//      applicationGUI.mainBoardDisplay.setInfoBarVisible(false);

//      // Disable the undo / redo button.
//      applicationGUI.setUndoButtonsEnabled(false);
//      applicationGUI.setRedoButtonsEnabled(false);

//      // While the solver is running the user shouldn't take any solution as new history from the solutions view.
//      applicationGUI.getSolutionsView().setTakeSolutionAsHistoryVisible(false);

//      solverGUI = new SolverGUI(this);
//      add(solverGUI, BorderLayout.SOUTH);
//      validate();
//      solverGUI.requestFocus(); // Request the focus so pressing F1 opens the help for the solver
//      applicationGUI.setSolverDependentObjectsEnabled(false);

//      return;
//    }

//    // Show the GUI for the solver.
//    if (action == "openRemodelSolver") {

//      // Create a new remodel solver.
//      new de.sokoban_online.jsoko.optimizer.GUI.RemodelSolverGUI(this);

//      return;
//    }

//    // Close the solver.
//    if (action == "closeSolver") {

//      // Due to multi threading it's possible to press the "close" solver button
//      // more than one time before the solver GUI really closes. Hence, the solver
//      // GUI may already be "null" here. Therefore this is checked.
//      if(solverGUI != null) {
//        remove(solverGUI);
//        validate();

//        // Set the solver object to null.
//        solverGUI = null;
//      }

//      // The solver has been closed.
//      applicationGUI.setSolverDependentObjectsEnabled(true);

//      displayInfotext("");

//      // Display the info bar of the main GUI again.
//      applicationGUI.mainBoardDisplay.setInfoBarVisible(true);

//      // Enable / disable the undo / redo button depending on the history status.
//      setUndoRedoFromHistory();

//      // Show the "take solution as history" menu item again.
//      applicationGUI.getSolutionsView().setTakeSolutionAsHistoryVisible(true);

//      // Request focus for the GUI again.
//      applicationGUI.mainBoardDisplay.requestFocusInWindow();

//      return;
//    }

//    /*
//     * Close of the program.
//     */
//    if (action == "programClosing") {

//      // If the solver GUI is opened it is closed instead of the whole program. This ensures that the user
//      // doesn't close the program although just the solver is to be closed.
//      if(solverGUI != null) {
//        actionPerformed(new ActionEvent(this, 0, "closeSolver"));
//        return;
//      }

//      Thread saveSettingsThread = new Thread(new Runnable() {
//        @Override
//        public void run() {
//          // Save the last last played level number for setting this level as start level at the next program start.
//          // This need only be done if the collection can be loaded again => is stored in a file or in the database.
//          if (currentLevelCollection.getFile() != null && !currentLevelCollection.getFile().isEmpty() || currentLevelCollection.isConnectedWithDatabase()) {
//            Settings.lastPlayedLevelNumber = currentLevel.getNumber();
//          }

//          // Save the current application bounds.
//          Settings.applicationBounds = getBounds();

//          // Set the last played collections in the settings so they are saved to the settings.ini file.
//          Settings.setLastPlayedCollections(levelCollectionsList.getLastPlayedCollections());

//          // Save the current settings to the hard disk.
//          try {
//            Settings.saveSettings();
//          } catch (final IOException e) {
//            try {
//              SwingUtilities.invokeAndWait(new Runnable() {
//                @Override
//                public void run() {
//                  JOptionPane.showMessageDialog(JSoko.this, Texts.getText("cantSaveSettings") + "\n" + e.getMessage(), "Failure", JOptionPane.ERROR_MESSAGE);
//                }
//              });
//            } catch (Exception e1) {}
//          }
//        }
//      });
//      saveSettingsThread.start();


//      // Save the current moves history as save game in the database.
//      currentLevel.setSaveGame(new Snapshot(movesHistory.getHistoryAsSaveGame(), true));

//      // Shutdown the database if a connection has been established.
//      Thread shutDownDatabaseThread = new Thread(new Runnable() {
//        @Override
//        public void run() {
//          if (Database.isBlockedByAnotherProcess() == false) {
//            levelIO.database.shutdown();
//          }
//        }
//      });
//      shutDownDatabaseThread.start();

//      // Closing the database may take some time. Set the program invisible so the user
//      // needn't to look at the program the whole time while closing.
//      setVisible(false);

//      try {
//        saveSettingsThread.join();
//        shutDownDatabaseThread.join();
//      } catch (InterruptedException e) { /* do nothing */ }

//      // Exit program
//      dispose();
//      if(Debug.isDebugModeActivated) {
//        System.out.println("exiting JSoko");
//      }
//      System.exit(0);
//    }

//    // Check whether a newer version of JSoko is available.
//    if(action.equals("updateCheck")) {

//      checkForUpdates();
//      return;
//    }

//    if(action.equals("openWebsite")) {
//      try {
//        Desktop.getDesktop().browse(new URI("http://www.sokoban-online.de"));
//      } catch (Exception e1) {
//        e1.printStackTrace();
//      }
//      return;
//    }

//    if(action.equals("openOnlineHelp")) {
//      try {
//        Desktop.getDesktop().browse(new URI("http://www.sokoban-online.de"));
//      } catch (Exception e1) {
//        e1.printStackTrace();
//      }
//      return;
//    }

//    // Every time something has to be performed for testing something
//    if (action == "test") {

//    }
//  }


//  /**
//   * Handles mouse events.
//   *
//   * @param evt  				the action event to be handled.
//   * @param isMouseDragged    flag, indicating whether the mouse has been dragged
//   * @param mouseXCoordinate  x coordinate of the mouse click
//   * @param mouseYCoordinate  y coordinate of the mouse click
//   * @param mouseHasBeenClickedWithinBoard  flag, indicating whether the mouse has been clicked within the board
//   */
//  public void mousePressedEvent(MouseEvent evt, boolean isMouseDragged,
//                            int mouseXCoordinate, int mouseYCoordinate,
//                            boolean mouseHasBeenClickedWithinBoard )
//  {
//    // While the solver is running mouse events are ignored.
//    // They are also ignored when the level is invalid.
//    if (solverGUI != null || gameMode == GameMode.INVALID_LEVEL) {
//      return;
//    }

//    // If the editor is activated all mouse events are handled by it.
//    if (isEditorModeActivated()) {

//      // In the editor it's annoying when an object is set although the program
//      // didn't have the focus.
//      if (applicationGUI.mainBoardDisplay.hasFocus()) {
//        editor.handleMouseEvent(evt, isMouseDragged, mouseXCoordinate, mouseYCoordinate, mouseHasBeenClickedWithinBoard);
//      }
//      return;
//    }

//    // Dragged mouse movements are only important for the editor.
//    if (isMouseDragged) {
//      return;
//    }

//    // Check whether the current status of the program allows action handling at the moment.
//    if (preActionHandling(evt, isMouseDragged)) {
//      // That call completely handled the event: we are done.
//      return;
//    }

//    // If the right mouse button has been clicked the last movement is undone.
//    if (evt.getButton() == MouseEvent.BUTTON3) {
//      undoMovementInOwnThread();
//      return;
//    }

//    // Only handle mouse events that occur on the board.
//    if (mouseHasBeenClickedWithinBoard == false) {
//      return;
//    }

//    // Calculate the internal board position of the click coordinates.
//    int position = Transformation.getInternalPosition(mouseXCoordinate + Transformation.getOutputLevelWidth() * mouseYCoordinate);

//    // Display the box data after every mouse click if requested.
//    Debug.debugShowBoxData = Debug.debugBoxDataAreToBeShown;

//    // DEBUG: If debug mode "show player distances" is activated two squares must be selected.
//    // After this is done the distance between these two squares is shown.
//    if (Debug.debugShowPlayerDistance) {

//      // If the first square is selected its position is saved.
//      if (Debug.debugSquarePosition == 0) {
//        Debug.debugSquarePosition = position;
//        board.setMarking(position);
//      } else {
//        displayInfotext("Distance = " + board.getPlayerDistance(Debug.debugSquarePosition, position));
//        board.removeMarking(Debug.debugSquarePosition);
//        Debug.debugSquarePosition = 0;
//      }
//    }

//    // DEBUG: If debug mode "show influence" is activated
//    //        the start square can be set by the user.
//    if (Debug.debugShowInfluenceValues || Debug.debugShowInfluenceColors) {

//      // Set the position.
//      Debug.debugSquarePosition = position;

//      // Display the influence from that square to every other square
//      // and return immediately without moving the player.
//      redraw(false);

//      return;
//    }

//    // DEBUG: If the debug mode "show box distances" is activated the clicked position
//    // is saved, because it is the start position for the distance calculation.
//    if (Debug.debugShowBoxDistanceForward || Debug.debugShowBoxDistanceBackward) {
//      Debug.debugSquarePosition = position;
//      redraw(false);

//      return;
//    }

//    // DEBUG: Show the index in the array of the clicked position.
//    if (Debug.debugShowPositionIndex) {
//      displayInfotext("Position at Clickposition: " + position);

//      return;
//    }

//    // DEBUG: Set or remove a marking if the marking mode is enabled.
//    if (Debug.debugMarkingModeEnabled) {
//      board.flipMarking(position);
//      redraw(false);

//      return;
//    }

//    // Handling of clicks at an accessible position (empty square or goal square)
//    if (board.isAccessible(position)) {

//      // If the position of the player has been clicked (and this is not the
//      // selection of the target square of a push)
//      // then the reachable squares of the player are highlighted.
//      if(position == board.playerPosition && isABoxSelected == false) {
//        board.playersReachableSquares.update();
//        isHighLightOfPlayerSquaresActivated = true;
//        applicationGUI.setCursor(Cursor.HAND_CURSOR);
//        redraw(false);
//        return;
//      }

//      // If a box had been selected it is pushed to the clicked position now.
//      // Otherwise the player is moved to the clicked position. This may results in several moves.
//      // Therefore the moving is done in an extra thread that can be interrupted.
//      // Backup the variables because they could have changed before this thread evaluates them.
//      final int     positionFinal       = position;
//      final boolean isABoxSelectedFinal = isABoxSelected;
//      movePlayerThread = new Thread() {

//        @Override
//        public void run() {
//          if (isABoxSelectedFinal) {
//            pushBoxAutomatically(selectedBoxPosition, positionFinal);
//          } else {
//            movePlayer(positionFinal, false);
//          }

//          SwingUtilities.invokeLater(new Runnable() {
//            @Override
//            public void run() {
//              redraw(false);
//            }
//          });
//        }
//      };
//      movePlayerThread.start();

//      return;
//    }

//    // If a box has been clicked, we store the position of the box, set the flag,
//    // which indicates that a box has been "marked", and change the cursor.
//    // That happens only for boxes, which are not outside the game (aka beauty boxes).
//    // If a marked box gets clicked, again, the mark is removed.

//    // Handling of clicks at a box that is in the "active" board area
//    // (sometimes boxes can never be reached by the player. They are just there for special level designs).
//    if (board.isBox(position) && board.isOuterSquareOrWall(position) == false &&
//        (isABoxSelected == false || selectedBoxPosition != position)) {

//      // Save the position of the selected box.
//      selectedBoxPosition = position;

//      // Show the new GUI after all other events. This is necessary because in
//      // "preActionHandling" there might have already been an event added to the EDT.
//      SwingUtilities.invokeLater(new Runnable() {
//        @Override
//        public void run() {
//          // Set the flag, indicating that a box has been selected and set the "move" cursor.
//          isABoxSelected = true;

//          // Determine the reachable squares of the box so the GUI can highlight them.
//          board.boxReachableSquares.markReachableSquares(selectedBoxPosition, false);

//          applicationGUI.setCursor(Cursor.HAND_CURSOR);

//          // Draw the selected status of the box.
//          redraw(false);
//        }
//      });

//      return;
//    }

//    return;
//  }

//  /**
//   * Handles drop events.
//   * <p>
//   * This method is called when the user has dropped something on this program.
//   *
//   * @param dtde  the DropTargetDropEvent
//   */
//  public void dropEvent(DropTargetDropEvent dtde) {

//    // Only accept copy or move drops.
//    if ((dtde.getSourceActions() & DnDConstants.ACTION_COPY_OR_MOVE) == 0) {
//      dtde.rejectDrop();
//      return;
//    }

//    // Check whether the current status of the program allows action handling at the moment.
//    if (preActionHandling(dtde, false) == true) {
//      return;
//    }

//    // The loaded level collection.
//    LevelCollection levelCollection = new LevelCollection.Builder().build();

//    // Accept the drop.
//    dtde.acceptDrop(DnDConstants.ACTION_COPY_OR_MOVE);

//    Transferable transferable = dtde.getTransferable();
//    DataFlavor[] dataFlavors = dtde.getCurrentDataFlavors();

//    // Determine which type of flavor it is.
//    for (DataFlavor currentFlavor : dataFlavors) {

//      try {
//        // If the dropped data are stored in a file then load the file.
//        if (DataFlavor.javaFileListFlavor.equals(currentFlavor)) {

//          Iterator<File> files = ((List<File>) transferable.getTransferData(currentFlavor)).iterator();

//          while (files.hasNext()) {
//            File file = files.next();
//            if (file.canRead() == false || file.isFile() == false) {
//              break;
//            }

//            // Load a collection and set the first of the new levels for playing.
//            levelCollection = levelIO.getLevelCollectionFromFile(file.getPath());

//            // Break after loading the first collection. There is no
//            // reason for loading more than one collection.
//            break;
//          }
//        }
//        else {
//          // If the dropped data are stored in Strings.
//          if (DataFlavor.stringFlavor.equals(currentFlavor)) {
//            levelCollection = levelIO.getLevelCollectionFromStringDataFlavor(transferable);
//          }
//        }
//      } catch (Exception e) {
//        e.printStackTrace();
//      }
//    }

//    // If the collection is empty, inform the user that no useful data have
//    // been found and return.
//    if (levelCollection.isEmpty()) {
//      JOptionPane.showMessageDialog(this, Texts.getText("noLevelsFound"), Texts.getText("note"), JOptionPane.WARNING_MESSAGE);

//      return;
//    }

//    // Set the new collection as current collection and the first level for playing.
//    setCollectionForPlaying(levelCollection);
//    setLevelForPlaying(1);
//  }

//  @Override
//  public void stateChanged(ChangeEvent e) {

//    // The view on the level may have changed.
//    if(e.getSource() == Transformation.class) {
//      String transformationString = Transformation.getTransformationAsString();
//      if(!currentLevel.getTransformationString().equals(transformationString)) {
//        currentLevel.setTransformationString(transformationString);
//        levelIO.database.updateTranformation(currentLevel);
//      }
//    }

//  }

//  /**
//   * This method is called whenever an action is fired that must be handled by
//   * the main application. Actions are handled in the methods:
//   * <ul>
//   *   <li> {@link #keyEvent(KeyEvent)}
//   *   <li> {@link #actionPerformed(ActionEvent)}
//   *   <li> {@link #mousePressedEvent(MouseEvent, boolean, int, int, boolean)}
//   *   <li> {@link #dropEvent(DropTargetDropEvent)}
//   * </ul>
//   * <p>
//   * Note: Some actions are handled in other classes. For instance changing
//   * the look&feel is handled in the GUI class without calling any of the
//   * mentioned methods because the event only affects the GUI but not any game data.
//   * <p>
//   * This method is called before the action is handled by the mentioned
//   * methods in order to do "everything that has to be done before the action
//   * is handled". This is necessary because some methods are running in extra threads
//   * and handling new actions before these threads are canceled will cause problems.
//   *
//   * @param evt  the event that that has been fired
//   * @param isMouseDragged   whether the mouse is dragged at the moment
//   * @return <code>true</code> the action has been handled in this method and
//   *                           mustn't be handled anywhere else,
//   *        <code>false</code> the action still must be handled in the calling method
//   */
//  private boolean preActionHandling(EventObject evt, boolean isMouseDragged) {

//    // If the player is currently being moved then interrupt the moving
//    // and ignore the new event.
//    // The event must be ignored because the moving thread may take
//    // some time for canceling, hence the new event mustn't
//    // be handled before the thread has canceled!
//    Thread moveThread = movePlayerThread; // The movePlayer thread sets the variable "null" at the end
//    if (moveThread != null && moveThread.isAlive()) {

//      // Mouse dragging is just ignored while the player is moving.
//      if (isMouseDragged) {
//        return true;
//      }

//      // Force the thread to finish. The variable is never null here, because the following
//      // coding after this if is the only coding which sets it to null.
//      movePlayerThread.interrupt();

//      // The event has been "consumed" and will not be executed anymore. While the "movePlayer"-thread
//      // is alive any action will just stop that thread but nothing more.
//      // This is according to the reference behaviour of Sokoban YASC.
//      return true;
//    }

//    // Delete any thread reference that might still be there from a
//    // previous moving action just to free some RAM.
//    movePlayerThread = null;

//    // Any action deselects a selected box.
//    // However, first this action event has to be handled.
//    // Some actions depend on the status of the selected box (e.g.: if a box
//    // is selected and the user clicks at an empty square the box is pushed there).
//    if(isABoxSelected) {
//      SwingUtilities.invokeLater(new Runnable() {
//        @Override
//        public void run() {
//          isABoxSelected = false;
//          applicationGUI.setCursor(Cursor.DEFAULT_CURSOR);
//          redraw(false);
//        }
//      });
//      // While we just initiated a side effect, the event is not yet really handled.
//      // Hence, we go on ...
//    }

//    // If the player reachable squares are highlighted, currently,
//    // every new action cancels the highlighting.
//    if(isHighLightOfPlayerSquaresActivated) {
//      SwingUtilities.invokeLater(new Runnable() {
//        @Override
//        public void run() {
//          isHighLightOfPlayerSquaresActivated = false;
//          applicationGUI.setCursor(Cursor.DEFAULT_CURSOR);
//          redraw(false);
//        }
//      });
//      // While we just initiated a side effect, the event is not yet really handled.
//      // Hence, we go on ...
//    }

//    // No Sir, we have not yet completely handled this event
//    return false;
//  }

//  /**
//   * Returns if a box has been selected.
//   *
//   * @return <code>true</code> a box has been selected <code>false</code> no box
//   *         has been selected
//   */
//  public boolean isABoxSelected() {
//    return isABoxSelected;
//  }

//  /**
//   * Returns whether the player reachable squares are to be highlighted.
//   *
//   * @return <code>true</code> if the player reachable squares are to be highlighted,<br>
//   * 	      <code>false</code> otherwise
//   */
//  public boolean isHighLightingOfPlayerReachableSquaresActivated() {
//    return isHighLightOfPlayerSquaresActivated;
//  }

//  /**
//   * Returns the position of the selected box.
//   *
//   * @return the position of the selected box
//   */
//  public int getSelectedBoxPosition() {
//    return selectedBoxPosition;
//  }

//  /**
//   * Moves (pushes) a box from a start square to a destination square,
//   * where both squares need not be directly adjacent.
//   * In that (non-trivial) case the box is pushed along some other squares to
//   * reach its destination.  That is automatically computed and performed.
//   *
//   * @param fromSquare  where the box is just now
//   * @param toSquare    where the box shall be moved to
//   * @return <code>0</code> if the box could be pushed that way,<br>
//   *        <code>-1</code> if the box could not be pushed that way
//   */
//  public byte pushBoxAutomatically(int fromSquare, int toSquare) {

//    // Indicates the intended next position for each step of the box path
//    int newBoxPosition = 0;

//    // Indicates the current position of the box during scanning of the box path
//    int currentBoxPosition = 0;

//    // Compute the path of the box from start to destination square.
//    ArrayList<Integer> boxPath = getBoxPath(fromSquare, toSquare);

//    if (boxPath.size() == 0) {
//      return -1;
//    }

//    /*
//     * Scan the box path square by square and move box and player accordingly.
//     * FFS/hm: tell "movePlayer" whether it just follows up itself.
//     */
//    currentBoxPosition = fromSquare;
//    for (int i = 0; i < boxPath.size() && Thread.currentThread().isInterrupted() == false; i++) {

//      newBoxPosition = boxPath.get(i).intValue();

//      // Move the player beneath the box.  To that side, from where it can do
//      // the next push, i.e. opposite of the push destination square.
//      movePlayer(currentBoxPosition + (currentBoxPosition - newBoxPosition), true);

//      // Don't do the push if the moving has been interrupted.
//      if (Thread.currentThread().isInterrupted() == false) {
//        // Move the player to the box square.  In "movePlayer()" this is detected,
//        // and the box is pushed, also... to the "newBoxPosition".
//        movePlayer(currentBoxPosition, true);

//        currentBoxPosition = newBoxPosition;
//      }
//    }

//    // If the box has been pushed a sound is played and the new board
//    // position is analyzed.
//    if (fromSquare != currentBoxPosition) {

//      // Play the "push / push to a goal"-sound. According to YASC, no sound when interrupted.
//      if(!Thread.currentThread().isInterrupted()) {
//        if (board.isGoal(newBoxPosition)) {
//          Sound.Effects.PUSH_TO_GOAL.play();
//        } else {
//          Sound.Effects.PUSH.play();
//        }
//      }

//      // Analyze the new board position.
//      analyzeNewBoardPosition(fromSquare, currentBoxPosition);
//    } else {
//      // The push has been interrupted. Play the move sound instead.
//      if(!Thread.currentThread().isInterrupted()) {
//        Sound.Effects.MOVE.play();
//      }

//      // Update the status of the redo / undo buttons.
//      setUndoRedoFromHistory();
//    }

//    return 0;
//  }

//  /**
//   * Moves the player to the passed square (if possible). If a box is next to
//   * the player located on the square, the player should move to, the box is
//   * pushed. This method increases the moves and pushes currently played if
//   * necessary and stores the moves in the movement history.
//   *
//   * @param newPlayerPosition  position the player should move to
//   * @param isBoxPushedAutomatically flag indicating whether this method is called from "pushBoxAutomatically"
//   * @return <code>0</code> if the player has been moved to the new position,<br>
//   *        <code>-1</code> if the player coundn't be moved to the new position
//   *        (may be he was there, already)
//   */
//  public byte movePlayer(int newPlayerPosition, boolean isBoxPushedAutomatically) {

//    // Nimmt den Zielpfad auf, auf dem der Spieler das Zielfeld erreichen kann.
//    int[] playerPath;

//    // Kistenstart- und Zielposition, die gemerkt werden muss, um sie im Historyobjekt
//    // ablegen zu können (= um sie für undo / redo speichern zu können)
//    int boxStartPosition  = 0;
//    int boxTargetPosition = 0;

//    // Nimmt die Nummer der verschobenen Kiste auf (Keine Kiste => -1)
//    int pushedBoxNo = -1;

//    // Holds an element from the history.
//    HistoryElement historyElement = null;

//    // Index of the last push in the history
//    int lastPushIndex = 0;

//    // Player position after the last push
//    int playerPositionAfterLastPush = 0;

//    // If the player is already there... nothing happens
//    if (board.playerPosition == newPlayerPosition) {
//      return -1;
//    }

//    // Search a path to the destination
//    playerPath = board.playerPath.getPathTo(newPlayerPosition);

//    // If the player cannot enter the destination square, we have to check
//    // whether a pushable box is the obstacle.
//    // If that is the case, we push that box now.
//    // This coding assumes that the player is always next to the box to be pushed!
//    if (playerPath == null && board.isBox(newPlayerPosition) && board.pushBox(newPlayerPosition, 2 * newPlayerPosition - board.playerPosition) >= 0) {

//      // Increase the push count
//      pushesCount++;

//      // Remember the number of the pushed box
//      pushedBoxNo = board.getBoxNo(newPlayerPosition);

//      boxStartPosition  = newPlayerPosition;
//      boxTargetPosition = 2 * newPlayerPosition - board.playerPosition;

//      // Erneut versuchen einen Zielpfad zu ermitteln, da die Kiste ja
//      // jetzt verschoben wurde
//      playerPath = board.playerPath.getPathTo(newPlayerPosition);
//    }

//    // Only move the player if a path has been found
//    if (playerPath != null) {

//      Delays delay = Delays.makeDelayNormal(playerPath.length);

//      boolean firstStep = true;

//      // Regarding interrupts... our caller may be pushBocAutomatically(),
//      // who also wants to detect any interrup that is detected here.
//      // Hence we here cannot clear the interrupt status unconditionally.

//      // Move the player corresponding to the found path. Begin with 1 because the first
//      // position is the start position of the player.
//      for (int i = 1; i < playerPath.length && ! Thread.currentThread().isInterrupted(); i++) {

//        // Before we go on with the next movement, we may want to wait a bit,
//        // in order to create an observable animation speed.
//        historyElement = movesHistory.getMovement(movesHistory.getCurrentMovementNo());
//        boolean isapush = (historyElement != null) && historyElement.isPush();
//        if (!firstStep) {
//          delay.sleep(true, isapush);
//          if (Thread.currentThread().isInterrupted()) {
//            // We did not yet change the board according to the step,
//            // but the history is also still unchanged.
//            break;
//          }
//        }

//        // FFS: Unfortunately, we currently are not sure, that the board is up to date
//        //      with respect to our efforts to use paintImmediately / paintRegion,
//        //      except we have done it ourselves.
//        final boolean quickPaint = (i > 1);
//        final int changedPos1 = board.playerPosition;
//        final int changedPos2 = playerPath[i];
//        board.playerPosition  = playerPath[i]; // <<<=== the board change!

//        // Determine and set view direction of the player
//        int movementDirection = board.getMoveDirection(playerPath[i - 1], playerPath[i]);
//        applicationGUI.mainBoardDisplay.setViewDirection(movementDirection);

//        // If the player reverses his last move it is treated as undo.
//        historyElement = movesHistory.getMovement(movesHistory.getCurrentMovementNo());
//        if( Settings.treatReverseMovesAsUndo
//            && historyElement != null
//            && historyElement.isPush() == false
//            && Directions.isValidDirectionsAndOpposite(historyElement.direction, movementDirection) )
//        {
//          movesHistory.goToPrecedingMovement();
//          movesCount--;
//        } else {
//          // Increase number of moves and store movement in the
//          // history
//          movesCount++;
//          movesHistory.addMovement(movementDirection, pushedBoxNo);
//        }

//        // If the player needn't push a box it is checked whether the
//        // number of moves since the last push is optimal.
//        if (pushedBoxNo == -1 && Settings.optimizeMovesBetweenPushes) {

//          // Determine the player position after the last push
//          playerPositionAfterLastPush = initialPlayerPosition;
//          int currentMovementIndex = movesHistory.getCurrentMovementNo();
//          for (lastPushIndex = currentMovementIndex; lastPushIndex >= 0; lastPushIndex--) {
//            historyElement = movesHistory.getMovement(lastPushIndex);
//            if (historyElement.isPush()) {
//              int boxPosition = board.boxData.getBoxPosition(historyElement.pushedBoxNo);
//              playerPositionAfterLastPush =  board.getPositionAtOppositeDirection(boxPosition, historyElement.direction);
//              break;
//            }
//          }

//          // If there is no push at all in the history lastPushIndex is -1!
//          int numberOfMovesSinceLastPush = currentMovementIndex - lastPushIndex;
//          int[] optimalPlayerPath = board.playerPath.getPath(playerPositionAfterLastPush, board.playerPosition);
//          if (numberOfMovesSinceLastPush > optimalPlayerPath.length - 1) {

//            // The optimal path is shorter than the one the user has
//            // chosen => save the better path in the history
//            movesHistory.setMovementNo(lastPushIndex);
//            for (int index = 1; index < optimalPlayerPath.length; index++) {
//              movementDirection = board.getMoveDirection(optimalPlayerPath[index-1], optimalPlayerPath[index]);
//              movesHistory.addPlayerMove(movementDirection);
//            }
//            // Correct the number of moves
//            movesCount -= (numberOfMovesSinceLastPush - optimalPlayerPath.length + 1);
//          }
//        }

//        // Draw new game status and wait until the new board has been drawn.
//        // Only changed positions are: changedPos1 and changedPos2
//        if (quickPaint) {
//          applicationGUI.mainBoardDisplay.paintRegion(true, changedPos1, changedPos2);
//        } else {
//          applicationGUI.mainBoardDisplay.repaintAndWait();
//        }

//        // Before we go on with the next movement, we may want to wait a bit,
//        // in order to create an observable animation speed.
//        //				if (isBoxPushedAutomatically || i < playerPath.length - 1) {
//        //					delay.sleep(true);
//        //				}
//        firstStep = false;
//      }

//      // We have to also add a delay after the last movement.
//      // If automatic box pushing is not activated (i.e. only the
//      // player is moved), we need no waiting after the last move.
//      if (isBoxPushedAutomatically) {
//        delay.sleep(true);		// it's a push, hence wait
//      }
//      else {
//        // Play the move sound. If it's a combined push (box is pushed
//        // automatically) only the push sound has to be played after the
//        // push.
//        if (pushedBoxNo == -1) {

//          // According to YASC no sound when interrupted.
//          if(Thread.currentThread().isInterrupted() == false) {
//            Sound.Effects.MOVE.play();
//          }

//          // Enable / disable the undo / redo button depending on the history
//          // status. This is only done when only moves have been made.
//          // If a box has been pushed the status is set in the method
//          // "analyzeNewBoardPosition".
//          setUndoRedoFromHistory();
//        }


//        // If a box has been pushed the new board position must be analyzed.
//        // This mustn't be done if this push is part of
//        // a combined movement. If this is the case only the method
//        // "pushBoxAutomatically" knows when the combined push ends.
//        if (pushedBoxNo != -1) {

//          // Play the sound for push / reaching a goal.
//          // According to YASC no sound when interrupted.
//          if(Thread.currentThread().isInterrupted() == false) {
//            if(board.isGoal(boxTargetPosition)) {
//              Sound.Effects.PUSH_TO_GOAL.play();
//            }
//            else {
//              Sound.Effects.PUSH.play();
//            }
//          }

//          analyzeNewBoardPosition(boxStartPosition, boxTargetPosition);
//        }
//      }
//    }

//    return 0;
//  }

//  /**
//   * Computes the path onto which a box can be pushed from a start square to a
//   * destination square.
//   * @param fromSquare location in board where we find the box to be pushed
//   * @param toSquare   location in board where the box could be pushed to
//   * @return path along which the box can be pushed from <code>fromSquare</code>
//   *         to <code>toSquare</code>, excluding the <code>fromSquare</code>
//   */
//  public ArrayList<Integer> getBoxPath(int fromSquare, int toSquare) {

//    if(fromSquare == toSquare) {
//      return new ArrayList<Integer>();
//    }

//    int startXCoordinate = fromSquare % board.width;
//    int startYCoordinate = fromSquare / board.width;

//    int targetXCoordinate = toSquare % board.width;
//    int targetYCoordinate = toSquare / board.width;

//    /**
//     * This local class stores 4 distance values (initially MAX_VALUE) for the
//     * 4 incoming directions of a square.
//     */
//    class Direction {

//      int above;
//      int below;
//      int left;
//      int right;

//      /**
//       * Creates an object holding the movement values for a box.
//       */
//      public Direction() {

//        // By default we use the maximally possible reachability distance value.
//        // When a square really is reached, that smaller distance value is stored
//        // for the direction from which the box could be pushed onto the square.
//        above = Integer.MAX_VALUE;
//        below = Integer.MAX_VALUE;
//        left  = Integer.MAX_VALUE;
//        right = Integer.MAX_VALUE;
//      }
//    }

//    // Hier werden die im aktuellen Zug erreichbaren Felder gespeichert
//    ArrayList<int[]> reachableSquares = new ArrayList<int[]>();

//    // In diesem Vektor werden die erreichbaren Felder eines Zuges abgelegt
//    // (l_zugNr[x] enthält alle Felder, die im x. Zug erreicht werden konnten)
//    ArrayList<ArrayList<int[]>> movements = new ArrayList<ArrayList<int[]>>();

//    // Hier werden die im letzen Zug erreichten Felder abgelegt
//    ArrayList<int[]> reachableSquaresLastMovement = new ArrayList<int[]>();

//    // Hierdrin wird der Pfad zum Zielfeld erstellt
//    ArrayList<Integer> boxPath = new ArrayList<Integer>();

//    // Indicates whether there are more reachable squares to be investigated
//    boolean moreReachableSquaresExist = true;

//    // player coordinates
//    int xPlayerPosition = board.playerPosition % board.width;
//    int yPlayerPosition = board.playerPosition / board.width;

//    // Zug, indem eine Lösung gefunden wurde
//    int noMovementBestSolutionWasFound = -1;

//    // Bewegungswert der bisher besten Lösungsvariante (wird benötigt, um
//    // die beste
//    // Lösungsvariante zu ermitteln)
//    // Der Bewegungswert errechnet sich folgendermaßen:
//    // Anzahl Spielerbewegungen + Faktor * Anzahl Kistenverschiebungen
//    int minimalMovementValue = Integer.MAX_VALUE;

//    // Nimmt stets den aktuellen Bewegungswert auf
//    int movementValue = 0;

//    // Gibt an, ob bei dem Entfernen einer Kiste wirklich eine Kiste
//    // entfernt wurde
//    // (der Algorithmus startet mit der Spielerposition als
//    // Kistenstartposition, weil
//    // der Spieler im nächsten Zug immer auf die Position der alten Kiste
//    // gesetzt wird.
//    // In diesem Fall wird an der alten Position deshalb keine Kiste vom
//    // Feld genommen,
//    // und darf deshalb auch nicht wieder auf das Feld gesetzt werden!!!)
//    boolean boxHasToBeSet = false;

//    // box coordinates
//    int newXBoxPosition = 0;
//    int newYBoxPosition = 0;
//    int xBoxPosition = 0;
//    int yBoxPosition = 0;

//    /*
//     * Nimmt folgends auf: int xPosUrsprungsfeld; int yPosUrsprungsfeld; int
//     * xPosErreichtesFeld; int yPosErreichtesFeld; int AnzahlBewegungen;
//     */
//    int[] movementData;

//    // Weiteres Spielfeld, in dem gespeichert wird, welche Spielsituationen
//    // schon vorkamen,
//    // damit eine Kiste nicht mehrmals auf die gleiche Art auf ein Feld
//    // geschoben wird.
//    Direction[][] reachedSquares = new Direction[board.width][board.height];

//    for (int index = 0; index < board.width; index++) {
//      for (int index2 = 0; index2 < board.height; index2++) {
//        reachedSquares[index][index2] = new Direction();
//      }
//    }

//    // Zur Aufnahme des Pfades den die Spielfigur gehen muss
//    int[] playerPath;

//    // Das Ursprungsfeld kann in 0 Zügen erreicht werden (Bewegungswert = 0)
//    // Das Ursprungsfeld wird als "Zielfeld" eingetragen, die
//    // Spielerkoordinaten beim "Startfeld",
//    // damit der nachfolgende Algorithmus funktioniert
//    // (->Spielfigurkoordinaten = Koordinaten des "Startfeldes")
//    movementData = new int[5];
//    movementData[0] = xPlayerPosition;
//    movementData[1] = yPlayerPosition;
//    movementData[2] = startXCoordinate;
//    movementData[3] = startYCoordinate;
//    movementData[4] = 0;

//    // Diese "Dummydaten" eintragen, damit der Algorithmus Anfangsdaten hat,
//    // mit denen er loslegen kann.
//    reachableSquaresLastMovement.add(movementData);
//    movements.add(reachableSquaresLastMovement);

//    // Feldwert auf "freies Feld" setzen, damit das Feld der Kiste im
//    // Verlauf der Prüfungen nicht dauernd als belegt gilt
//    board.removeBox(startXCoordinate, startYCoordinate);

//    while (moreReachableSquaresExist == true) {

//      // Die erreichbaren Felder des letzten Zuges holen
//      reachableSquaresLastMovement = movements.get(movements.size() - 1);

//      // Die Liste der zuletzt erreichten Felder durchgehen und für jedes
//      // Feld prüfen
//      // welche anderen Felder von diesem Feld aus erreicht werden können
//      // (auf jeden Fall alle erreichbaren Felder des letzten Zuges
//      // durchgehen, da innerhalb
//      // dieses Zuges ja das Zielfeld von mehreren Ursprungsfelder
//      // erreicht werden könnte und
//      // dann die Anzahl der Spielerbewegungen gilt)
//      // => deshalb NICHT "&& l_zielfeldErreicht == false" in die
//      // Abbruchbedingung der Schleife aufnehmen!
//      for (int index = 0; index < reachableSquaresLastMovement.size(); index++) {

//        // Die Koordinaten des aktuell zu untersuchenden Feldes auf die Koordinaten
//        // des Feldes sezten, dass im letzten Durchlauf erreicht werden konnte.
//        // Die Koordinaten des Ursprungsfeldes ebenfalls merken
//        xBoxPosition = reachableSquaresLastMovement.get(index)[0];
//        yBoxPosition = reachableSquaresLastMovement.get(index)[1];
//        newXBoxPosition = reachableSquaresLastMovement.get(index)[2];
//        newYBoxPosition = reachableSquaresLastMovement.get(index)[3];

//        // Merken, ob auf dem alten Feld eine Kiste steht
//        boxHasToBeSet = board.isBox(xBoxPosition, yBoxPosition);

//        // Kiste auf das neue Feld verschieben
//        board.removeBox(xBoxPosition, yBoxPosition);
//        board.setBox(newXBoxPosition, newYBoxPosition);

//        // Existiert ein Feld oberhalb und eines unterhalb des aktuellen Feldes ?
//        if (newYBoxPosition - 1 >= 0
//            && newYBoxPosition + 1 < board.height) {

//          // Prüfen, ob die Kiste nach oben und / oder nach unten
//          // verschoben werden kann:
//          // Nur weiter prüfen, wenn Felder oberhalb und unterhalb der
//          // Kiste frei sind
//          if (       board.isAccessible(newXBoxPosition, newYBoxPosition - 1)
//              && board.isAccessible(newXBoxPosition, newYBoxPosition + 1)) {

//            // Spielfigur auf das Feld setzen, von welchem im
//            // letzten Zug die Kiste verschoben wurde
//            xPlayerPosition = xBoxPosition;
//            yPlayerPosition = yBoxPosition;

//            // Pfad für die Spielfigur suchen, der die Spielfigur
//            // unter die aktuelle Kiste führt
//            playerPath = board.playerPath.getPath(xPlayerPosition + board.width * yPlayerPosition,
//                                         newXBoxPosition + board.width * (newYBoxPosition + 1));

//            // Nur weiter machen, wenn Spieler unter die Kiste
//            // gelangen kann
//            if (playerPath != null) {
//              // Den letzten Bewegungswert holen und die in diesem
//              // Zug gemachten Spielerbewegungen addieren
//              // Achtung: Die Länge des Zielpfades entspricht der
//              // Anzahl der zu begehenden
//              // Felder. Da im Zielpfad das Startfeld selber auch
//              // enthalten ist, ist
//              // die Anzahl der Felder, die gegangen werden muss,
//              // eigentlich um 1 geringer.
//              // Da aber der Pfad zu dem Feld neben die Kiste
//              // gesucht wurde, und somit zum Verschieben
//              // der Kiste noch eine weitere Bewegung nötig ist,
//              // kann einfach "l_pfad.length" addiert werden!
//              // (gesucht ist ja die Anzahl der Bewegungen, um die
//              // Kiste von der alten Position auf
//              // die neue Position zu verschieben!)
//              // Zusätzlich muss noch der g_movesVSPusheswert
//              // addiert werden, da ja der Bewegungswert
//              // gesucht wird (Erklärung siehe Deklaration von
//              // g_movesVSPushes)
//              movementValue = reachableSquaresLastMovement.get(index)[4];
//              movementValue += playerPath.length + Settings.movesVSPushes;

//              // Falls die Kiste schon einmal von unten auf dieses
//              // Feld verschoben wurde,
//              // muss nur weiter gemacht werden, wenn es diesmal
//              // mit weniger Zügen möglich ist.
//              // Außerdem muss nur weitergemacht werden, wenn
//              // bisher weniger Spielerbewegungen
//              // gemacht wurden, als beim bisher besten Pfad zum
//              // Zielfeld.
//              // (Die letzte Bedingung ist immer "true", solange
//              // das Zielfeld noch nicht erreicht wurde)
//              if (movementValue < reachedSquares[newXBoxPosition][newYBoxPosition - 1].below && movementValue < minimalMovementValue) {

//                // Merken, welcher Bewegungswert insgesamt dabei
//                // entstanden ist, die Kiste
//                // von unten auf das neue Feld zu schieben
//                reachedSquares[newXBoxPosition][newYBoxPosition - 1].below = movementValue;

//                movementData = new int[5];
//                movementData[0] = newXBoxPosition;
//                movementData[1] = newYBoxPosition;
//                movementData[2] = newXBoxPosition;
//                movementData[3] = newYBoxPosition - 1;
//                movementData[4] = movementValue;

//                reachableSquares.add(movementData);

//                // Falls das Zielfeld erreicht wurde, wird die
//                // aktuelle Position im
//                // l_zugNr-Vektor gesichert (was der Anzahl der
//                // bisher
//                // durchgeführten Züge entspricht.) .size() gibt
//                // zwar eigentlich
//                // einen Wert zurück, der um 1 zu hoch ist (1.
//                // Eintrag im Vektor wäre
//                // ja an Stelle 0, da aber der aktuelle Zug noch
//                // nicht in den Vektor
//                // eingefügt wurde, ist dies so ok)
//                // Die benötigte Anzahl an Spielerbewegungen
//                // wird als neue
//                // minimale Anzahl gespeichert.
//                if (targetXCoordinate == newXBoxPosition && targetYCoordinate == newYBoxPosition - 1) {
//                  noMovementBestSolutionWasFound = movements.size();
//                  minimalMovementValue = movementValue;
//                }
//              }
//            }

//            // Prüfen, ob Kiste nach unten verschoben werden kann:

//            // Spielfigur auf das Feld setzen, von welchem im
//            // letzten Zug die Kiste verschoben wurde
//            xPlayerPosition = xBoxPosition;
//            yPlayerPosition = yBoxPosition;

//            // Pfad für die Spielfigur suchen, der die Spielfigur
//            // über die Kiste führt
//            playerPath = board.playerPath.getPath(xPlayerPosition + board.width * yPlayerPosition,
//                                         newXBoxPosition + board.width * (newYBoxPosition - 1));

//            // Nur weiter machen, wenn Spieler über die Kiste
//            // gelangen kann
//            if (playerPath != null) {
//              // Den letzten Bewegungswert holen und die in diesem
//              // Zug gemachten Spielerbewegungen addieren
//              // Achtung: Die Länge des Zielpfades entspricht der
//              // Anzahl der zu begehenden
//              // Felder. Da im Zielpfad das Startfeld selber auch
//              // enthalten ist, ist
//              // die Anzahl der Felder, die gegangen werden muss,
//              // eigentlich um 1 geringer.
//              // Da aber der Pfad zu dem Feld neben die Kiste
//              // gesucht wurde, und somit zum Verschieben
//              // der Kiste noch eine weitere Bewegung nötig ist,
//              // kann einfach "l_pfad.length" addiert werden!
//              // (gesucht ist ja die Anzahl der Bewegungen, um die
//              // Kiste von der alten Position auf
//              // die neue Position zu verschieben!)
//              // Zusätzlich muss noch der g_movesVSPusheswert
//              // addiert werden, da ja der Bewegungswert
//              // gesucht wird (Erklärung siehe Deklaration von
//              // g_movesVSPushes)
//              movementValue = reachableSquaresLastMovement.get(index)[4];
//              movementValue += playerPath.length + Settings.movesVSPushes;

//              // Falls die Kiste schon einmal von oben auf dieses
//              // Feld verschoben wurde,
//              // muss nur weiter gemacht werden, wenn es diesmal
//              // mit weniger Zügen möglich ist.
//              // Außerdem muss nur weitergemacht werden, wenn
//              // bisher weniger Spielerbewegungen
//              // gemacht wurden, als beim bisher besten Pfad zum
//              // Zielfeld.
//              // (Die letzte Bedingung ist immer "true", solange
//              // das Zielfeld noch nicht erreicht wurde)
//              if (movementValue < reachedSquares[newXBoxPosition][newYBoxPosition + 1].above && movementValue < minimalMovementValue) {

//                // Merken, welcher Bewegungswert insgesamt dabei
//                // entstanden ist, die Kiste
//                // von oben auf das neue Feld zu schieben
//                reachedSquares[newXBoxPosition][newYBoxPosition + 1].above = movementValue;

//                movementData = new int[5];
//                movementData[0] = newXBoxPosition;
//                movementData[1] = newYBoxPosition;
//                movementData[2] = newXBoxPosition;
//                movementData[3] = newYBoxPosition + 1;
//                movementData[4] = movementValue;

//                reachableSquares.add(movementData);

//                // Falls das Zielfeld erreicht wurde, wird die
//                // aktuelle Position im l_zugNr-Vektor gesichert
//                // (was der Anzahl der bisher durchgeführten
//                // Züge entspricht.) .size() gibt zwar
//                // eigentlich
//                // einen Wert zurück, der um 1 zu hoch ist (1.
//                // Eintrag im Vektor wäre ja an Stelle 0,
//                // da aber der aktuelle Zug noch nicht in den
//                // Vektor eingefügt wurde, ist dies so ok)
//                // Die benötigte Anzahl an Spielerbewegungen
//                // wird als neue minimale Anzahl gespeichert.
//                if (targetXCoordinate == newXBoxPosition && targetYCoordinate == newYBoxPosition + 1) {
//                  noMovementBestSolutionWasFound = movements.size();
//                  minimalMovementValue = movementValue;
//                }
//              }
//            }
//          }
//        }

//        // Existiert ein Feld links und eines rechts des aktuellen Feldes ?
//        if (newXBoxPosition - 1 >= 0 && newXBoxPosition + 1 < board.width) {

//          // Prüfen, ob die Kiste nach links und / oder nach rechts
//          // verschoben werden kann:

//          // Nur weiter prüfen, wenn Felder links und rechts von der
//          // Kiste frei sind
//          if (       board.isAccessible(newXBoxPosition - 1, newYBoxPosition)
//              && board.isAccessible(newXBoxPosition + 1, newYBoxPosition)) {

//            // Spielfigur auf das Feld setzen, von welchem im
//            // letzten Zug die Kiste verschoben wurde
//            xPlayerPosition = xBoxPosition;
//            yPlayerPosition = yBoxPosition;

//            // Pfad für die Spielfigur suchen, der rechts neben die Kiste führt
//            playerPath = board.playerPath.getPath(xPlayerPosition + board.width * yPlayerPosition,
//                                         newXBoxPosition + 1 + board.width * newYBoxPosition);

//            // Nur weiter machen, wenn Spieler rechts neben die
//            // Kiste gelangen kann
//            if (playerPath != null) {
//              // Den letzten Bewegungswert holen und die in diesem
//              // Zug gemachten Spielerbewegungen addieren
//              // Achtung: Die Länge des Zielpfades entspricht der
//              // Anzahl der zu begehenden
//              // Felder. Da im Zielpfad das Startfeld selber auch
//              // enthalten ist, ist
//              // die Anzahl der Felder, die gegangen werden muss,
//              // eigentlich um 1 geringer.
//              // Da aber der Pfad zu dem Feld neben die Kiste
//              // gesucht wurde, und somit zum Verschieben
//              // der Kiste noch eine weitere Bewegung nötig ist,
//              // kann einfach "l_pfad.length" addiert werden!
//              // (gesucht ist ja die Anzahl der Bewegungen, um die
//              // Kiste von der alten Position auf
//              // die neue Position zu verschieben!)
//              // Zusätzlich muss noch der g_movesVSPusheswert
//              // addiert werden, da ja der Bewegungswert
//              // gesucht wird (Erklärung siehe Deklaration von
//              // g_movesVSPushes)
//              movementValue = reachableSquaresLastMovement.get(index)[4];
//              movementValue += playerPath.length + Settings.movesVSPushes;

//              // Falls die Kiste schon einmal von rechts auf
//              // dieses Feld verschoben wurde,
//              // muss nur weiter gemacht werden, wenn es diesmal
//              // mit weniger Zügen möglich ist.
//              // Außerdem muss nur weitergemacht werden, wenn
//              // bisher weniger Spielerbewegungen
//              // gemacht wurden, als beim bisher besten Pfad zum
//              // Zielfeld.
//              // (Die letzte Bedingung ist immer "true", solange
//              // das Zielfeld noch nicht erreicht wurde)
//              if (movementValue < reachedSquares[newXBoxPosition - 1][newYBoxPosition].right && movementValue < minimalMovementValue) {

//                // Merken, welcher Bewegungswert insgesamt dabei
//                // entstanden ist, die Kiste
//                // von rechts auf das neue Feld zu schieben
//                reachedSquares[newXBoxPosition - 1][newYBoxPosition].right = movementValue;

//                movementData = new int[5];
//                movementData[0] = newXBoxPosition;
//                movementData[1] = newYBoxPosition;
//                movementData[2] = newXBoxPosition - 1;
//                movementData[3] = newYBoxPosition;
//                movementData[4] = movementValue;

//                reachableSquares.add(movementData);

//                // Falls das Zielfeld erreicht wurde, wird die
//                // aktuelle Position im
//                // l_zugNr-Vektor gesichert (was der Anzahl der
//                // bisher
//                // durchgeführten Züge entspricht.) .size() gibt
//                // zwar eigentlich
//                // einen Wert zurück, der um 1 zu hoch ist (1.
//                // Eintrag im Vektor wäre
//                // ja an Stelle 0, da aber der aktuelle Zug noch
//                // nicht in den Vektor
//                // eingefügt wurde, ist dies so ok)
//                // Die benötigte Anzahl an Spielerbewegungen
//                // wird als neue minimale Anzahl gespeichert.
//                if (targetXCoordinate == newXBoxPosition - 1 && targetYCoordinate == newYBoxPosition) {
//                  noMovementBestSolutionWasFound = movements.size();
//                  minimalMovementValue = movementValue;
//                }
//              }
//            }

//            // Prüfen, ob Kiste nach rechts verschoben werden kann:

//            // Spielfigur auf das Feld setzen, von welchem im
//            // letzten Zug die Kiste verschoben wurde
//            xPlayerPosition = xBoxPosition;
//            yPlayerPosition = yBoxPosition;

//            // Pfad für die Spielfigur suchen, der die Spielfigur
//            // links neben die Kiste führt
//            playerPath = board.playerPath.getPath(xPlayerPosition + board.width * yPlayerPosition,
//                                         newXBoxPosition - 1 + board.width * newYBoxPosition);

//            // Nur weiter machen, wenn Spieler links neben die Kiste
//            // gelangen kann
//            if (playerPath != null) {
//              // Den letzten Bewegungswert holen und die in diesem
//              // Zug gemachten Spielerbewegungen addieren
//              // Achtung: Die Länge des Zielpfades entspricht der
//              // Anzahl der zu begehenden
//              // Felder. Da im Zielpfad das Startfeld selber auch
//              // enthalten ist, ist
//              // die Anzahl der Felder, die gegangen werden muss,
//              // eigentlich um 1 geringer.
//              // Da aber der Pfad zu dem Feld neben die Kiste
//              // gesucht wurde, und somit zum Verschieben
//              // der Kiste noch eine weitere Bewegung nötig ist,
//              // kann einfach "l_pfad.length" addiert werden!
//              // (gesucht ist ja die Anzahl der Bewegungen, um die
//              // Kiste von der alten Position auf
//              // die neue Position zu verschieben!)
//              // Zusätzlich muss noch der g_movesVSPusheswert
//              // addiert werden, da ja der Bewegungswert
//              // gesucht wird (Erklärung siehe Deklaration von
//              // g_movesVSPushes)
//              movementValue = reachableSquaresLastMovement.get(index)[4];
//              movementValue += playerPath.length + Settings.movesVSPushes;

//              // Falls die Kiste schon einmal von links auf dieses
//              // Feld verschoben wurde,
//              // muss nur weiter gemacht werden, wenn es diesmal
//              // mit weniger Zügen möglich ist.
//              // Außerdem muss nur weitergemacht werden, wenn
//              // bisher weniger Spielerbewegungen
//              // gemacht wurden, als beim bisher besten Pfad zum
//              // Zielfeld.
//              // (Die letzte Bedingung ist immer "true", solange
//              // das Zielfeld noch nicht erreicht wurde)
//              if (movementValue < reachedSquares[newXBoxPosition + 1][newYBoxPosition].left && movementValue < minimalMovementValue) {

//                // Merken, welcher Bewegungswert insgesamt dabei
//                // entstanden ist, die Kiste
//                // von links auf das neue Feld zu schieben
//                reachedSquares[newXBoxPosition + 1][newYBoxPosition].left = movementValue;

//                movementData = new int[5];
//                movementData[0] = newXBoxPosition;
//                movementData[1] = newYBoxPosition;
//                movementData[2] = newXBoxPosition + 1;
//                movementData[3] = newYBoxPosition;
//                movementData[4] = movementValue;

//                reachableSquares.add(movementData);

//                // Falls das Zielfeld erreicht wurde, wird die
//                // aktuelle Position im
//                // l_zugNr-Vektor gesichert (was der Anzahl der
//                // bisher durchgeführten
//                // Züge entspricht.) .size() gibt zwar
//                // eigentlich einen Wert zurück,
//                // der um 1 zu hoch ist (1. Eintrag im Vektor
//                // wäre ja an Stelle 0,
//                // da aber der aktuelle Zug noch nicht in den
//                // Vektor eingefügt wurde,
//                // ist dies so ok)
//                // Die benötigte Anzahl an Spielerbewegungen
//                // wird als neue minimale Anzahl gespeichert.
//                if (targetXCoordinate == newXBoxPosition + 1 && targetYCoordinate == newYBoxPosition) {
//                  noMovementBestSolutionWasFound = movements.size();
//                  minimalMovementValue = movementValue;
//                }
//              }
//            }
//          }
//        }

//        // Restore the original state of the board.
//        if (boxHasToBeSet == true) {
//          board.setBox(xBoxPosition, yBoxPosition);
//        }
//        board.removeBox(newXBoxPosition, newYBoxPosition);

//      }
//      // Ende Schleife über alle Felder, die im letzten Zug erreicht
//      // werden konnten (For-Schleife)

//      // Erreichte Felder nur speichern, wenn auch welche erreicht wurden
//      if (reachableSquares.size() != 0) {
//        // Felder, die in diesem Zug erreicht werden können speichern
//        movements.add(reachableSquares);

//        // create a new vector for the next set of reachable squares
//        reachableSquares = new ArrayList<int[]>();
//      } else {
//        // No more squares could be reached.  Hence, terminate outer while loop.
//        moreReachableSquaresExist = false;
//      }

//    }
//    // end of while loop

//    // Restore the box on the original start square
//    board.setBox(startXCoordinate, startYCoordinate);

//    // Es kann sein, dass ein Zielfeld innerhalb eines Zuges von verschiedenen
//    // Seiten erreicht werden kann. In diesem Fall muss für den Zielpfad die
//    // Alternative genommen werden, die die geringsten Bewegungen benötigt.
//    // Um zu ermitteln, welches die beste Alternative ist, werden die beiden
//    // folgenden Variablen benötigt.
//    int movesCount = Integer.MAX_VALUE;
//    int l_positionInErreichbareFelder = 0;

//    // Nimmt die Daten eines Zuges auf (Startfeld, Zielfeld und Anzahl Züge)
//    movementData = new int[5];

//    // l_besteLösungGefundenBeiZugNr wurde mit -1 initialisiert. Wenn eine
//    // Lösung gefunden wurde, so ist dieser Wert jetzt != -1
//    if (noMovementBestSolutionWasFound != -1) {
//      // index > 0, weil sonst der Zug 0 auch untersucht wurde, dieser Zug
//      // ist allerdings nur ein Dummyzug (siehe weiter oben (vor der
//      // Whileschleife))
//      for (int movementNo = noMovementBestSolutionWasFound; movementNo > 0; movementNo--) {
//        reachableSquares = movements.get(movementNo);

//        // Feld, welches auf dem Zielpfad liegt und mit den wenigsten
//        // Bewegungen erreichbar war suchen
//        for (int index = 0; index < reachableSquares.size(); index++) {
//          movementData = reachableSquares.get(index);

//          // Handelt es sich um ein Feld auf dem Zielpfad ?
//          if (movementData[2] == targetXCoordinate
//              && movementData[3] == targetYCoordinate) {
//            // Merken, mit wie vielen Bewegungen dieses Feld
//            // erreichbar ist
//            // Außerdem merken, an welcher Position im Vektor dieses
//            // Feld ist
//            if (movementData[4] < movesCount) {
//              movesCount = movementData[4];
//              l_positionInErreichbareFelder = index;
//            }
//          }
//        }
//        // Daten zu dem Zug holen, der das Zielfeld mit den wenigsten
//        // Bewegungen
//        // erreichen konnte
//        movementData = reachableSquares.get(l_positionInErreichbareFelder);

//        // Feld zum Zielpfad hinzufügen (alledings in umgekehrter
//        // Reihenfolge, da vom Zielfeld
//        // rückwärts die zu begehenden Felder ermittelt und abgelegt
//        // werden.
//        boxPath.add(movementData[2] + board.width * movementData[3]);

//        // Beim nächsten Durchlauf ist das jetzige Startfeld (dessen
//        // Koordinaten
//        // in x[0] und x[1] abgelegt sind) das Zielfeld
//        targetXCoordinate = movementData[0];
//        targetYCoordinate = movementData[1];
//        // System.out.println("XKoor: " + zielXKoordinate);
//        // System.out.println("YKoor: " + zielYKoordinate);
//      }
//    }

//    // Compute a reversal of the "boxPath", and return it
//    Collections.reverse(boxPath);
//    return boxPath;
//  }

//  /**
//   * Plays the next movements from the history movements, in an own thread,
//   * in order to avoid blocking the event dispatcher thread (EDT).
//   *
//   * @param redoAllMovements whether all movements have to be redone
//   */
//  private void redoMovementInOwnThread(final boolean redoAllMovements) {

//    movePlayerThread = new Thread() {
//      @Override
//      public void run() {
//        redoMovement(redoAllMovements);

//        // The "setUndoRedoFromHistory()" has happened already.

//        // This thread has finished its work. The thread isn't needed any more.
//        movePlayerThread = null;
//      }
//    };
//    movePlayerThread.start();
//  }

//  /**
//   * Plays the next movements from the history movements.
//   *
//   * @param redoAllMovements  flag, indicating whether all movements have to be redone
//   */
//  public void redoMovement(boolean redoAllMovements) {

//    // If a box has been pushed these variables store the start and the target position.
//    int boxStartPosition  = 0;
//    int boxTargetPosition = 0;

//    // Return immediately if there isn't any successor movement to redo.
//    if (movesHistory.hasSuccessorMovement() == false) {
//      return;
//    }

//    // The first step is played without a delay.
//    boolean isFirstStep = true;

//    /*
//     * Compute the number of moves we are going to do, in order
//     * to derive a meaningful delay value.
//     *
//     * We have 3 different approaches to animate the complete sequence:
//     * (1) using the overall length
//     * (2) using the average length of the combined movements
//     * (3) evalating each combined movement separately.
//     * Using (1) tends to become by far too fast for long solutions.
//     * We do (3).
//     */
//    // Loop over all (combined) movements.
//        do {
//      int movesToRedoCount = Settings.singleStepUndoRedo? 1 : movesHistory.getNextCombinedMovementLength();

//      // Create a Delay object suitable for undo/redo and the above length.
//      Delays delay = Delays.makeDelayUndoRedo(movesToRedoCount);

//      // Redo the specified number of movements.
//      for (int redoneMovesCount = 0 ; !Thread.currentThread().isInterrupted() && redoneMovesCount < movesToRedoCount; redoneMovesCount++) {

//        // Record up to 4 positions for a selective call to paintRegion(),
//        // so only the parts that have changed have to be redrawn.
//        int boxPositionBeforePush = -1;
//        int boxPositionAfterPush  = -1;
//        final int playerPositionBeforeMove;
//        final int playerPositionAfterMove;

//        final HistoryElement nextMovement = movesHistory.getSuccessorMovement();

//        // Provided this is not the first step, insert a delay, to create an observable motion.
//        if (!isFirstStep) {
//          // Since this is an own thread the user may have changed the speed in the meantime => set the current value.
//          delay.setStep(Settings.delayValueUndoRedo);
//          delay.sleep(true, nextMovement.isPush());
//        }
//        isFirstStep = false;

//        // If a box is to be pushed, do this now.
//        if (nextMovement.isPush()) {
//          int boxPosition = board.boxData.getBoxPosition(nextMovement.pushedBoxNo);
//          boxTargetPosition = board.getPosition(boxPosition, nextMovement.direction);

//          // Perform the push in the model
//          board.pushBox(boxPosition, boxTargetPosition);

//          pushesCount++;

//          // Remember the start position of the box in this (combined) movement
//          if (boxStartPosition == 0) {
//            boxStartPosition = boxPosition;
//          }
//          boxPositionBeforePush = boxPosition;
//          boxPositionAfterPush  = boxTargetPosition;
//        }

//        // Set the correct view direction of the player.
//        applicationGUI.mainBoardDisplay.setViewDirection(nextMovement.direction);

//        // Move the player, and record 2 positions for the following paintRegion() call.
//        playerPositionBeforeMove = board.playerPosition;
//        board.playerPosition     = board.getPosition(board.playerPosition, nextMovement.direction);
//        playerPositionAfterMove  = board.playerPosition;

//        // Increase the number of moves done since the player has moved.
//        movesCount++;

//        // Paint the new board and wait until the EDT has finished painting.
//        applicationGUI.mainBoardDisplay.paintRegion(true, boxPositionBeforePush, boxPositionAfterPush, playerPositionBeforeMove, playerPositionAfterMove );

//      }
//        }
//    while(!Thread.currentThread().isInterrupted() && movesHistory.hasSuccessorMovement() && redoAllMovements == true);

//    // No sound is played when the redo has been interrupted without considerung how many moves have been redone.
//    if(!Thread.currentThread().isInterrupted()) {
//      // Play a sound depending on the action that has been made.
//      if (boxTargetPosition > 0) {					// = a box has been pushed
//        if (board.isGoal(boxTargetPosition)) {
//          Sound.Effects.PUSH_TO_GOAL.play();
//        }
//        else {
//          Sound.Effects.PUSH.play();
//        }
//      } else {
//        Sound.Effects.MOVE.play();
//      }
//    }

//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();

//    // If a box has been pushed the new board position must be analyzed (deadlocks, level solved, ...)
//    if (boxStartPosition != 0) {
//      analyzeNewBoardPosition(boxStartPosition, boxTargetPosition);
//    }
//  }

//  /**
//   * Redoes the movements of the history from the current movement up to the
//   * passed movement number.
//   *
//   * @param endMovementNo  the number of the movement up to which the history is to be redone.
//   */
//  public void redoMovementsWithoutDisplay(int endMovementNo) {

//    // Number of the movement to do next.
//    int movementNo = movesHistory.getCurrentMovementNo();

//    // Redo all movements until movement with the passed number has been reached.
//    while (++movementNo <= endMovementNo && movesHistory.hasSuccessorMovement()) {

//      // Get the next movement from the history.
//      HistoryElement nextMovement = movesHistory.getSuccessorMovement();

//      // Push a box if necessary.
//      if (nextMovement.isPush()) {
//        int currentBoxPosition = board.boxData.getBoxPosition(nextMovement.pushedBoxNo);
//        int newBoxPosition = board.getPosition(currentBoxPosition, nextMovement.direction);
//        board.pushBoxUndo(currentBoxPosition, newBoxPosition);

//        // Increase number of pushes.
//        pushesCount++;
//      }

//      // Set the new view direction of the player and move the player.
//      applicationGUI.mainBoardDisplay.setViewDirection(nextMovement.direction);
//      board.playerPosition = board.getPosition(board.playerPosition, nextMovement.direction);

//      // Increase the number of moves.
//      movesCount++;
//    }

//    // Determine the new number of boxes on goals.
//    boxesOnGoalsCount = board.getBoxesOnGoalsCount();
//  }

//  /**
//   * Undoes the last movement in an own thread,
//   * in order to avoid blocking the event dispatcher thread (EDT).
//   */
//  private void undoMovementInOwnThread() {

//    movePlayerThread = new Thread() {
//      @Override
//      public void run() {
//        undoMovement();

//        // The "setUndoRedoFromHistory()" has happened already.

//        // This thread has finished its work. The thread isn't needed any more.
//        movePlayerThread = null;
//      }
//    };
//    movePlayerThread.start();
//  }

//  /**
//   * Undoes the last movement.
//   */
//  void undoMovement() {

//    // If the undo results in moving a box then the start and the target
//    // position of the moved box are saved. This information is needed
//    // to analyze the new board situation.
//    int boxStartPosition  = -1;
//    int boxTargetPosition = -1;

//    // Return immediately if there isn't any movement to undo.
//    if (movesHistory.hasPrecedingMovement() == false) {
//      return;
//    }

//    // Determine the number of moves we are going to do, in order to calculate meaningful delay values.
//    final int movesToUndoCount = Settings.singleStepUndoRedo? 1 : movesHistory.getPreviousCombinedMovementLength();

//    // Create a Delay object suitable for undo/redo and the above length.
//    Delays delay = Delays.makeDelayUndoRedo(movesToUndoCount);

//    // Undo the moves.
//    for (int undoneMovesCount = 0 ; !Thread.currentThread().isInterrupted() && undoneMovesCount < movesToUndoCount; undoneMovesCount++) {

//      // Record up to 4 positions for a selective call to paintRegion()
//      int boxPositionBeforePush = -1;
//      int boxPositionAfterPush  = -1;
//      final int playerPositionBeforeMove;
//      final int playerPositionAfterMove;

//      // Get the preceding movement.
//      HistoryElement lastMovement = movesHistory.getPrecedingMovement();

//      /** Now the history is advanced, but the board is not yet changed. */

//      // Wait in order not to undo the movements too quickly.
//      if (undoneMovesCount != 0) {
//        // The user may have changed the speed => set the current value.
//        delay.setStep(Settings.delayValueUndoRedo);

//        delay.sleep(true, lastMovement.isPush());
//      }

//      // If a box is to be pushed, do this now.
//      if (lastMovement.isPush()) {

//        // Move the box on the board.
//        int boxPosition = board.boxData.getBoxPosition(lastMovement.pushedBoxNo);
//        boxTargetPosition = board.getPositionAtOppositeDirection(boxPosition, lastMovement.direction);
//        board.pushBoxUndo(boxPosition, boxTargetPosition);

//        // Adjust the number of pushes due to the push
//        pushesCount--;

//        // Save the position when the box has been pushed the first time.
//        if(boxStartPosition == -1) {
//          boxStartPosition = boxPosition;
//        }

//        // Remember the box positions so only these areas have to be repainted in the GUI.
//        boxPositionBeforePush = boxPosition;
//        boxPositionAfterPush  = boxTargetPosition;
//      }

//      // Move the player, and record 2 positions for the following paintRegion() call.
//      playerPositionBeforeMove = board.playerPosition;
//      board.playerPosition = board.getPositionAtOppositeDirection(board.playerPosition, lastMovement.direction);
//      playerPositionAfterMove  = board.playerPosition;

//      // Set the correct view direction of the player.
//      applicationGUI.mainBoardDisplay.setViewDirection(lastMovement.direction);

//      // Decrease the number of moves since the player has moved.
//      movesCount--;

//      // Draw the new status (board, moves, pushes, ...) and wait until the drawing is finished.
//      applicationGUI.mainBoardDisplay.paintRegion(true, boxPositionBeforePush, boxPositionAfterPush, playerPositionBeforeMove, playerPositionAfterMove );
//    }

//    // Any interrupted are handled in this method. Hence, check and clear the interrupted status.
//    // No sound is played when the undo has been interrupted without considerung how many moves have been undone.
//    // According to YASC undo always results in the move sound to be played no matter if a push or a move has been undone.
//    if(!Thread.currentThread().isInterrupted()) {
//      Sound.Effects.MOVE.play();
//    }

//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();

//    // If a box has been pushed the new board position must be analyzed.
//    if (boxStartPosition != -1) {
//      analyzeNewBoardPosition(boxStartPosition, boxTargetPosition);
//    }
//  }

//  /**
//   * Undo of all movements without displaying the moves in the GUI.
//   */
//  public void undoAllMovementsWithoutDisplay() {

//    // Variable for holding the history element of the preceding movement.
//    HistoryElement precedingMovement;

//    // Current box position.
//    int currentBoxPosition = 0;

//    // Undo all movements.
//    while ((precedingMovement = movesHistory.getPrecedingMovement()) != null) {

//      // If a box is to be pushed do this now.
//      if (precedingMovement.isPush()) {
//        currentBoxPosition = board.boxData.getBoxPosition(precedingMovement.pushedBoxNo);
//        int boxPositionOppositeDirection = board.getPositionAtOppositeDirection(currentBoxPosition, precedingMovement.direction);
//        board.pushBoxUndo(currentBoxPosition, boxPositionOppositeDirection);

//        // Calculate the new number of pushes.
//        pushesCount--;
//      }

//      // Set the correct view direction of the player and move the player.
//      applicationGUI.mainBoardDisplay.setViewDirection(precedingMovement.direction);
//      board.playerPosition = board.getPositionAtOppositeDirection(board.playerPosition, precedingMovement.direction);

//      // Calculate the new number of moves.
//      movesCount--;
//    }

//    // Determine the new number of boxes on goals.
//    boxesOnGoalsCount =  board.getBoxesOnGoalsCount();
//  }

//  /**
//   * Returns whether the current level is valid.
//   * <P>
//   * If the level is invalid an info message is displayed.
//   *
//   * @return <code>true</code> if the level is valid, and<br>
//   *        <code>false</code> if the level is invalid
//   */
//  public boolean isLevelValid() {

//    StringBuilder validityMessage = new StringBuilder();

//    // Let the board check if it is valid.
//    boolean levelIsValid = board.isValid(validityMessage);

//    // If the level is invalid the editor mustn't be left and the
//    // reason for the invalidity is displayed.
//    if (levelIsValid == false) {
//      displayInfotext(validityMessage.toString());
//      applicationGUI.setEditorMenuItemEnabled(false);
//    } else {
//      displayInfotext("");
//      applicationGUI.setEditorMenuItemEnabled(true);
//    }

//    return levelIsValid;
//  }


//  /**
//   * The passed solution is set as history. This method calls the method of
//   * the history object and sets the new status of the undo / redo buttons.
//   *
//   * @param solution
//   *            the solution to be set.
//   */
//  public void takeSolutionForHistory(Solution solution) {

//    // Check whether the current status of the program allows action handling at the moment.
//    // This is important because this action can be called from the solutions GUI which doesn't
//    // calls the "actionEvent" method of this class.
//    if (preActionHandling(new ActionEvent(this, 0, ""), false) == true) {
//      return;
//    }

//    setHistoryFromLURDString(solution.lurd);

//    // Enable / disable the undo / redo button depending on the history status.
//    setUndoRedoFromHistory();

//    // Delete the info text because the board has been set back to the initial position.
//    displayInfotext("");

//    // Redraw the new board because it has been set to the start position.
//    redraw(false);
//  }

//  /**
//   * Sets a new collection for playing.
//   *
//   * @param levelCollection  the level collection to be set
//   */
//  public void setCollectionForPlaying(LevelCollection levelCollection) {

//    currentLevelCollection = levelCollection;

//    // Add the collection to the selectable collections.
//    levelCollectionsList.newLevelCollectionIsPlayed(levelCollection);

//    // Fire event that the currently selected collection has changed.
//    levelCollectionsList.setSelectedItem(currentLevelCollection);

//    // Refresh the list of collection levels in the GUI because a new collection has been loaded.
//    applicationGUI.updatedSelectableLevels();

//    // Display the new number of levels.
//    repaint();
//  }

//  /**
//   * Checks whether there is a newer version of JSoko available.
//   */
//  public void checkForUpdates() {

//    new SwingWorker<Void, Void>() {

//      String onlineVersion = null;

//      @Override
//      protected Void doInBackground() throws Exception {

//        // Inform the user that the program searches for a new version.
//        publish();

//        try {
//          Scanner scanner = new Scanner(new URL("http://sourceforge.net/projects/jsokoapplet/").openStream());
//          while(scanner.hasNextLine()){
//            String dataLine = scanner.nextLine();
//            int position = dataLine.indexOf("JSoko/Version ");
//            if(position != -1) {
//              // The next 4 characters contain the version number.
//              onlineVersion = dataLine.substring(position+14, position+18);
//              break;
//            }
//              }
//              scanner.close();
//        } catch (Exception e) {}

//        return null;
//      }

//      @Override
//      protected void process(List<Void> chunks) {
//        JButton infoButton = applicationGUI.getInfoButton();
//        infoButton.setText(  "<html><b>"
//                       + Texts.getText("menu.searchingUpdates")
//                       + "</b></html>" );
//        infoButton.setVisible(true);
//      }

//      @Override
//      protected void done() {

//        JButton infoButton = applicationGUI.getInfoButton();

//        if(onlineVersion != null && onlineVersion.compareTo(Settings.PROGRAM_VERSION) > 0) {
//          infoButton.setText(  "<html><b><font color=\"#FF0000\">"
//                         + Texts.getText("menu.getNewJSokoVersion", onlineVersion)
//                         + "</font></b></html>" );
//          infoButton.setCursor(Cursor.getPredefinedCursor(Cursor.HAND_CURSOR));
//          infoButton.addActionListener(new ActionListener() {
//            @Override
//            public void actionPerformed(ActionEvent e) {
//              try {
//                Desktop.getDesktop().browse(new URI("https://sourceforge.net/projects/jsokoapplet/"));
//              } catch (Exception e1) {
//                e1.printStackTrace();
//              }
//            }
//          });
//        }
//        else {
//          new SwingWorker<Void, Void>() {

//            @Override
//            protected Void doInBackground() throws Exception {

//              // Inform the user about the result of the search for a new version for 8 seconds.
//              publish();

//              Thread t = Thread.currentThread();
//              synchronized (t) {
//                      t.wait(8000);
//              }

//              return null;
//            }

//            @Override
//            protected void process(List<Void> chunks) {
//              JButton infoButton = applicationGUI.getInfoButton();
//              infoButton.setText(  "<html><b><font color=\"#04B404\">"
//                             + Texts.getText("menu.noNewerVersion")
//                             + "</font></b></html>" );
//            }

//            @Override
//            protected void done() {
//              // All information have been displayed. The info button can be hidden again.
//              JButton infoButton = applicationGUI.getInfoButton();
//              infoButton.setText("");
//              infoButton.setVisible(false);
//            }
//          }.execute();
//        }
//      }
//    }.execute();
//  }

//  /**
//   * Asks the user to select collections to be imported to the database.
//   */
//  public void importCollectionsOfFolderToDatabase() {

//    final char SUCCESS_ICON = '0';
//    final char FAILED_ICON  = '1';

//    // Ask the user which collections have to be imported.
//    final ArrayList<File> collectionFiles =
//        new FileSelector().getSelectedFiles(this,
//            Utilities.getFileFromClassPath("."),
//            ".*\\.sok$|.*\\.xsb$|.*\\.txt$");

//    // If no files have been selected just return.
//    if(collectionFiles.size() == 0) {
//      return;
//    }

//    // Create a panel showing the import status of every collection.
//    final JPanel importResults = new JPanel(new BorderLayout());
//    final JList<String> importedCollections = new JList<String>();
//    final DefaultListModel<String> model = new DefaultListModel<String>();
//    importedCollections.setModel(model);
//    importedCollections.setCellRenderer(new StyledListCellRenderer() {
//      @Override
//      protected void customizeStyledLabel(JList list, Object value, int index, boolean isSelected, boolean cellHasFocus) {
//        super.customizeStyledLabel(list, value, index, isSelected, cellHasFocus);

//        String text = getText();

//        if(text.length() > 0 && text.charAt(0) == SUCCESS_ICON) {
//          setIcon(Utilities.getIcon("apply (oxygen).png", ""));
//          setText(text.substring(1));
//        }
//        else if(text.length() > 0 && text.charAt(0) == FAILED_ICON) {
//          setIcon(Utilities.getIcon("process-stop.png", ""));
//          setText(text.substring(1));
//        } else {
//          setIcon(null);
//          addStyleRange(new StyleRange(Font.BOLD));
//        }

//        setBorder(BorderFactory.createEmptyBorder(0, 2, 0, 2));
//      }
//    });
//    importResults.add(new JScrollPane(importedCollections), BorderLayout.CENTER);

//    final JTextField infoText = new JTextField(Texts.getText("importedXOfYCollections", 0, collectionFiles.size()));
//    infoText.setEditable(false);
//    importResults.add(infoText, BorderLayout.SOUTH);
//    importResults.setPreferredSize(new Dimension(400, 600));

//    // OK button and cancel button of the dialog showing the results of the import.
//    // These buttons are already created here so the import SwingWorker can access them.
//    // The ok button is first enabled when the import has been completed.
//    final JButton okButton = new JButton();
//    okButton.setEnabled(false);
//    final JButton cancelButton = new JButton();

//    // Mutable boolean for cancelling the SwingWorker. the "cancel" method of
//    // Swingworker would cancel the whole thread which includes coding that
//    // may be interrupted (for instance SwingUtilities.invokeAndWait()).
//    // However, the coding should continue safely until the current collection
//    // is completely imported and then cancel the Swingworker before the next
//    // collection is processed. This also ensures that "done()" is first called
//    // AFTER "doInBackground()" has ended.
//    final boolean[] isImportCancelled = new boolean[1];

//    final SwingWorker<Void, String> importWorker = new SwingWorker<Void, String>() {

//      int importedCollectionsCount = 0;

//      @Override
//      protected Void doInBackground() throws Exception {

//        Collections.sort(collectionFiles, new Comparator<File>() {
//          @Override
//          public int compare(File o1, File o2) {
//            return o1.getName().compareToIgnoreCase(o2.getName());
//          }
//        });

//        for(File file : collectionFiles) {

//          // Break if the import has been cancelled by the user.
//          if(isImportCancelled[0] == true) {
//            break;
//          }

//          // Save the collection in the database.
//          try {
//            // Load the collection from the file.
//            LevelCollection collection = levelIO.getLevelCollectionFromFile(file.getAbsolutePath());

//            // Import all collections to the database.
//            if(collection.isEmpty()) {
//              publish(  FAILED_ICON
//                  + file.getName()
//                  + " (" + Texts.getText("noLevelsFound")
//                  + ")" );
//            } else if(levelIO.database.insertLevelCollection(collection) != null) {
//              publish(  SUCCESS_ICON
//                  + file.getName()
//                  + " (" + collection.getLevelsCount()
//                  + " "  + Texts.getText("general.levels")
//                  + ")" );
//            } else {
//              publish(  FAILED_ICON
//                  + file.getName()
//                  + " (" + collection.getLevelsCount()
//                  + " "  + Texts.getText("general.levels")
//                  + ")" );
//            }
//          } catch (IOException e) {
//            e.printStackTrace();
//          }
//        }

//        // 100% of the collections have been processed.
//        setProgress(100);

//        return null;
//      }

//      @Override
//      protected void process(List<String> data) {
//        for(String s : data) {
//          model.addElement(s);
//          infoText.setText(Texts.getText("importedXOfYCollections", ++importedCollectionsCount, collectionFiles.size()));
//        }

//        importedCollections.ensureIndexIsVisible(model.getSize() - 1);
//      }

//      @Override
//      protected void done() {

//        if(isImportCancelled[0] == true) {
//          model.addElement(FAILED_ICON+Texts.getText("furtherImportCancelled"));
//        }

//        // The import has been completed. The user may leave the dialog using
//        // the OK button but no abort the import anymore.
//        okButton.setEnabled(true);
//        cancelButton.getAction().setEnabled(false);

//        // Show a message that the import has been completed.
//        model.addElement(" ");
//        model.addElement(Texts.getText("importCompleted"));

//        // Show the last added texts.
//        importedCollections.ensureIndexIsVisible(model.getSize() - 1);
//      }
//    };
//    Utilities.executor.submit(importWorker);

//    // Create a dialog for showing the import status so the user can't use
//    // the main game GUI while the import is done.
//    StandardDialog dialog = new StandardDialog(this, Texts.getText("importResults"), true) {

//      @Override
//      public JComponent createBannerPanel() {
//        return null;
//      }

//      @Override
//      public JComponent createContentPanel() {
//        JPanel panel = new JPanel(new BorderLayout(2, 2));
//        panel.setBorder(BorderFactory.createCompoundBorder(
//                      new JideTitledBorder(
//                          new PartialEtchedBorder(PartialEtchedBorder.LOWERED,
//                                              PartialSide.NORTH),
//                          Texts.getText("importedCollections"),
//                          JideTitledBorder.LEADING,
//                          JideTitledBorder.ABOVE_TOP),
//                      BorderFactory.createEmptyBorder(6, 0, 0, 0)));
//        panel.add(importResults);
//        return panel;
//      }

//      @Override
//      public ButtonPanel createButtonPanel() {

//        ButtonPanel buttonPanel = new ButtonPanel(SwingConstants.CENTER);

//        // OK button
//        okButton.setName(OK);
//        okButton.setText(UIDefaultsLookup.getString("OptionPane.okButtonText"));
//        okButton.addActionListener(new ActionListener() {
//          @Override
//          public void actionPerformed(ActionEvent e) {
//            setDialogResult(RESULT_AFFIRMED);
//            setVisible(false);
//            dispose();
//          }
//        });

//        // Cancel button
//        cancelButton.setName(CANCEL);
//        cancelButton.setAction(new AbstractAction(UIDefaultsLookup.getString("OptionPane.cancelButtonText")) {
//          @Override
//          public void actionPerformed(ActionEvent e) {
//            if(importWorker.getProgress() < 100) {
//              // Cancel the import.
//              isImportCancelled[0] = true;
//            }
//          }
//        });

//        buttonPanel.addButton(okButton, ButtonPanel.AFFIRMATIVE_BUTTON);
//        buttonPanel.addButton(cancelButton, ButtonPanel.CANCEL_BUTTON);

//        setDefaultCancelAction(cancelButton.getAction());
//        getRootPane().setDefaultButton(okButton);
//        buttonPanel.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));
//        return buttonPanel;

//      }
//    };
//    dialog.pack();
//    dialog.setLocationRelativeTo(null);
//    dialog.setVisible(true);

//    return;
//  }

//  /**
//   * Sets the passed language, including side effects for
//   * Swing, the JRE and our text translation module.
//   *
//   * @param language the language to be set
//   */
//  public void setLanguage(String language) {

//    // Save the new language.
//    Settings.set("currentLanguage", language);

//    // Load the language texts.
//    Texts.loadAndSetTexts();
//  }

//  /**
//   * The movements passed as a LURD string are set as new movement history.
//   *
//   * @param movesAsLURDString  a <code>String</code> of LURD characters to be set as history
//   * @return number of found valid moves that have been added to the history
//   */
//  public int setHistoryFromLURDString(String movesAsLURDString) {

//    // Number of valid moves that have been added to the history.
//    int validMovesCount = 0;


//    // Set board to start position of level
//    undoAllMovementsWithoutDisplay();

//    // Backup current position
//    AbsoluteBoardPositionMoves boardPositionBackup = new AbsoluteBoardPositionMoves(board);

//    // The history is set to the start
//    movesHistory.setHistoryToStart();

//    // Go through the moves and add them to the history.
//    for (int movementNo = 0; movementNo < movesAsLURDString.length(); movementNo++) {

//      // Get the next character of the string.
//      char lurdChar = movesAsLURDString.charAt(movementNo);

//      // Jump over white spaces.
//      if(Character.isWhitespace(lurdChar)) {
//        continue;
//      }

//      int direction = -1;

//      switch (lurdChar) {
//      case 'u':
//      case 'U':
//        direction = UP;
//        break;

//      case 'd':
//      case 'D':
//        direction = DOWN;
//        break;

//      case 'l':
//      case 'L':
//        direction = LEFT;
//        break;

//      case 'r':
//      case 'R':
//        direction = RIGHT;
//        break;

//      default:
//        continue;
//      }

//      // Calculation of the new player and potential new box position.
//      int newPlayerPosition = board.getPosition(board.playerPosition, direction);
//      int newBoxPosition    = board.getPosition(newPlayerPosition, direction);

//      // Stop moving the player if the movement can't be done.
//      if(!(board.isAccessible(newPlayerPosition) || board.isBox(newPlayerPosition) && board.isAccessibleBox(newBoxPosition))) {
//        break;
//      }

//      // Count the number of valid moves.
//      validMovesCount++;

//      // Push a box if necessary.
//      if (board.isBox(newPlayerPosition)) {

//        // Do push and add it to the history
//        board.pushBox(newPlayerPosition, newBoxPosition);
//        board.setPlayerPosition(newPlayerPosition);
//        int pushedBoxNo = board.getBoxNo(newBoxPosition);
//        movesHistory.addMovement(direction, pushedBoxNo);
//      } else {
//        // Move the player and add this move to the history.
//        board.setPlayerPosition(newPlayerPosition);
//        movesHistory.addPlayerMove(direction);
//      }
//    }

//    // The history is set to the start
//    movesHistory.setHistoryToStart();

//    // Restore the board position at start of this method
//    board.setBoardPosition(boardPositionBackup);

//    return validMovesCount;
//  }

//  /**
//   * Sets the passed snapshot in the history and on the board.
//   *
//   * @param snapshot  the {@code Snapshot} to be set in the game
//   */
//  public void setSnapshot(Snapshot snapshot) {

//    if(snapshot == null || snapshot.getLURD().isEmpty()) {
//      return;
//    }

//    String lurdMoves = snapshot.getLURD();

//    // Set the moves in the history. This also sets the board back to the initial board position.
//    setHistoryFromLURDString(lurdMoves);

//    // Get the position in the moves to jump to. The user may have undone some moves,
//    // hence it might not be necessary to redo all moves. The current position in the
//    // movement history is marked with a "*".
//    int indexInHistory = lurdMoves.indexOf('*') - 1;

//    // Redo the history up to the marked position.
//    if(indexInHistory > 0) {
//      redoMovementsWithoutDisplay(indexInHistory);
//    }
//    else {
//      redoMovementsWithoutDisplay(lurdMoves.length()-1);
//    }
//  }

//  /**
//   * Returns a list of all collections that are selectable from the GUI.
//   * <p>
//   * The GUI will display this list to the user and allow the user to select
//   * a collection to be played.
//   *
//   * @return {@code List} of all {@code LevelCollection}s the user can select
//   */
//  public SelectableLevelCollectionComboBoxModel getSelectableLevelCollectionsModel() {
//    return levelCollectionsList;
//  }

//  /**
//   * Called when a solution event is fired for the currently loaded level.
//   */
//  @Override
//  public void solutionEventFired(SolutionEvent event) {

//    // Inform the solution combo box that solutions have been added / deleted.
//    // This is important for displaying the "level solved" mark in the list.
//    applicationGUI.repaint();
//  }
//}
  }
}
