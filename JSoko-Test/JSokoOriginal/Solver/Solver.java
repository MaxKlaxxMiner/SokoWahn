/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2012 by Matthias Meger, Germany
 *
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *
 *	This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
package de.sokoban_online.jsoko.solver;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.ListIterator;

import javax.swing.SwingUtilities;
import javax.swing.SwingWorker;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.PositionStorage;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.board.Directions;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.deadlockdetection.DeadlockDetection;
import de.sokoban_online.jsoko.leveldata.History;
import de.sokoban_online.jsoko.leveldata.HistoryElement;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.IntStack;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * All solver classes are derived from (extend) this class.
 */
public abstract class Solver extends SwingWorker<Solution, String>
		implements DirectionConstants
{

	/**
	 * Constant for "no box moved/pushed".  This is the largest value, which can be stored
	 * in a configuration as a box number.  It indicates the configuration as identical
	 * to the previous one, except that some boxes are inactive (really: not on the board).
	 */
	final protected short NO_BOX_PUSHED = 511;


	/**
	 * This class uses this own <code>PlayersReachableSquares</code> object because the
	 * global one is overwritten by other methods.
	 */
	public Board.PlayersReachableSquares playersReachableSquares;

	// Reference to the game board object
	protected Board board;

	// Reference to the main object
	protected JSoko application;

	// GUI for the solver.
	protected SolverGUI solverGUI;

	/** Object for storing board positions. Public for easier access. */
	protected PositionStorage positionStorage = new PositionStorage(1 << 22);

	// Direct reference to the offset array (improves readability)
	protected int[] offset;

	// Total count of non-deadlock-configurations reached during the search
	protected int boardPositionsCount = 0;

	/**
	 * Solution path for a possible goal room of the level.
	 * For levels where all goals are located in a room, which is separated from the boxes
	 * by a corral forcer, we solve the goal room separately, and store the solution here.
	 */
	protected int[] goalRoomSolutionPath;

	/**
	 * For a goal room level the goal room is indicated here.
	 */
	protected boolean[] goalRoomSquares = null;

	/**
	 * Position of the entrance to the goal room
	 */
	protected int goalRoomEntrancePosition = 0;


	// Variables for the methods "identifyRelevantBoxes" and "isICorral"
	// --------------------------------------------------------------------

	/** Indicates the corral squares on the board. */
	private int[] corralSquares;

	/**
	 * Reference value to indicate that a square belongs to the corral.
	 * <p>
	 * Instead of clearing a boolean array each time, we just increment this reference
	 * value for each new corral.
	 */
	private int corralIndicatorValue = 0;

	/** own object for reachable squares by the player with respect to just walls */
	Board.PlayersReachableSquaresOnlyWalls playersReachableSquaresOnlyWalls;

	/**
	 * Used for calculation the pushes lower bound for solving the board.
	 */
	protected final LowerBoundCalculation lowerBoundCalcuation;

	/**
	 * Used for identifying deadlock situations.
	 */
	protected final DeadlockDetection deadlockDetection;

	/**
	 * Inner class for storing information about the corral.
	 */
	final protected class CorralInfo {
		protected boolean isACombinedCorral        = false;
		protected boolean isAtLeastOneBoxNotOnGoal = false;
		protected CorralInfo() {}
	}

	/**
	 * All squares which are marked by this value, or a larger one,
	 * belong to a combined I-corral.
	 */
	private int lowestValueCombinedCorral = 0;

	// Runtime of this application.
	protected Runtime runtime;

	// Flag indicating whether the solver has been stopped due to an out of memory situation.
	protected boolean isSolverStoppedDueToOutOfMemory;


	/**
	 * Constructor for this abstract class.
	 *
	 * @param application the reference to the main object holding all references
	 * @param solverGUI reference to the GUI of this solver
	 */
	public Solver(JSoko application, SolverGUI solverGUI) {

		// Store references to the main object and the board object
		this.application = application;
		board            = application.board;

		// Save a reference to the GUI.
		this.solverGUI = solverGUI;

		deadlockDetection = new DeadlockDetection(board);

		// Create the own object for player reachable squares.
		playersReachableSquares = board.new PlayersReachableSquares();

		// Create own object for reachable squares with respect to just walls.
		// Used for the I-corral analysis
		playersReachableSquaresOnlyWalls = board.new PlayersReachableSquaresOnlyWalls();

		// Create the object that calculates a lower bound for the needed pushes to solve a specific board situation.
		lowerBoundCalcuation = new LowerBoundCalculation(board);

		// Save direct references for better readability.
		offset = board.offset;

		// Create array for method "identifyRelevantBoxes"
		corralSquares = new int[board.size];

		// Runtime of this application.
		runtime = Runtime.getRuntime();
	}


	/**
	 * Main method of the SwingWorker which tries to find a solution for the level
	 * in a background thread.
	 */
	@Override
	protected Solution doInBackground() throws Exception {

		// The "only push" solvers are assisted by the goal room solver.
		if(this instanceof SolverAStar || this instanceof SolverIDAStar) {
			setGoalRoomInformation();
		}

		// The solver uses all deadlock detections. The current settings are saved.
		boolean backupDetectSimpleDeadlocks    = Settings.detectSimpleDeadlocks;
		boolean backupDetectFreezeDeadlocks    = Settings.detectFreezeDeadlocks;
		boolean backupDetectCorralDeadlocks    = Settings.detectCorralDeadlocks;
		boolean backupDetectBipartiteDeadlocks = Settings.detectBipartiteDeadlocks;

		// The solver uses all deadlock detections.
		Settings.detectSimpleDeadlocks		= true;
		Settings.detectFreezeDeadlocks		= true;
		Settings.detectCorralDeadlocks		= true;
		Settings.detectBipartiteDeadlocks	= true;

		// Search a solution.
		Solution solution = searchSolution();

		// Set the original settings.
		Settings.detectSimpleDeadlocks	  = backupDetectSimpleDeadlocks;
		Settings.detectFreezeDeadlocks	  = backupDetectFreezeDeadlocks;
		Settings.detectCorralDeadlocks	  = backupDetectCorralDeadlocks;
		Settings.detectBipartiteDeadlocks = backupDetectBipartiteDeadlocks;

		// Inform the caller that 100% is done. This is important because:
		// if this SwingWorker is canceled, "isDone()" will return "done" before this
		// coding line is reached and therefore before this thread has finished.
		setProgress(100);

		return solution;
	}


	/**
	 * Process is called as a result of this worker thread's calling the
	 * publish method. This method runs on the event dispatch thread (EDT).
	 *
	 * All <code>Strings</code> to be published are passed to the GUI.
	 */
	@Override
	protected void process(List<String> textsToPublish) {
		for(String infoText: textsToPublish) {

			// Only publish the text if the solving isn't canceled by the user.
			if(isCancelled()) {
				break;
			}

			// If there has been provided a GUI for this solver the messages are shown.
			if(solverGUI != null) {
				solverGUI.setInfoText(infoText);
			}
		}
	}

	/**
	 * The algorithms A* and IDA* generate solutions, where consecutive pushes often
	 * push different boxes. That causes unnecessary moves between pushes.
	 * This method tries to improve the solution by performing the pushes of a box
	 * in a direct sequence, as far as possible.
	 */
	final protected void optimizeSolution() {

		int firstSolutionMovementNo = application.movesHistory.getCurrentMovementNo();
		int lastSolutionMovementNo  = 0;

		HistoryElement movement 	   = null;
		HistoryElement sameBoxPush	   = null;
		HistoryElement betweenMovement = null;

		// Sores all pushes from the history
		ArrayList<HistoryElement> historyClone;

		// Number of the push which has been done with the same box as the original push
		int sameBoxPushNo;

		// Direct reference to then history object
		History movementHistory = application.movesHistory;

		// Backup of the current configuration
		AbsoluteBoardPositionMoves currentBoardPositionBackup;


		// Create backup of the current configuration
		currentBoardPositionBackup = new AbsoluteBoardPositionMoves(board);

		// Fetch history
		historyClone = movementHistory.getMovementHistoryClone();

		// Delete all movements which were done before the solver started
		for(int i=0; i <= firstSolutionMovementNo; i++) {
			historyClone.remove(0);
		}

		lastSolutionMovementNo = historyClone.size() - 1;

		// Scan all pushes, and check whether following pushes of the same box can be
		// pulled out from behind.
		for(int pushNo = 0; pushNo < lastSolutionMovementNo; pushNo++) {

			// Fetch next movement from the history
			movement = historyClone.get(pushNo);

			// Perform the push
			int boxPosition = board.boxData.getBoxPosition(movement.pushedBoxNo);
			board.pushBox(boxPosition, boxPosition+offset[movement.direction]);
			board.setPlayerPosition(boxPosition);

			// Now search the history for more pushes of this box, and check whether
			// are legal already now.
			for(sameBoxPushNo = pushNo+1; sameBoxPushNo <= lastSolutionMovementNo; sameBoxPushNo++) {
				sameBoxPush = historyClone.get(sameBoxPushNo);

				// If the movement does not refer to the current box, jump to the next push
				if(sameBoxPush.pushedBoxNo != movement.pushedBoxNo) {
					continue;
				}

				// If the directly following push is with the same box anyhow, we can
				// continue with that push (without more checks).
				if(sameBoxPushNo == pushNo+1) {
					break;
				}

				/*
				 * Now we know that there is a further push of the proper box, and that
				 * this push is not an immediate successor (of the push we just did).
				 * Now we have to check whether that push can be pulled from behind
				 * to directly follow the first one.
				 */

				// If the push is not legal now (destination square blocked by another box,
				// or player cannot reach the square to perform the push), jump back now.
				boxPosition = board.boxData.getBoxPosition(sameBoxPush.pushedBoxNo);
				if(board.isBox(boxPosition+offset[sameBoxPush.direction])) {
					break;
				}
				board.playersReachableSquares.update();
				if(board.playersReachableSquares.isSquareReachable(boxPosition-offset[sameBoxPush.direction]) == false) {
					break;
				}

				// Perform the Push
				board.pushBox(boxPosition, boxPosition+offset[sameBoxPush.direction]);
				board.setPlayerPosition(boxPosition);

				/*
				 * The originally later push could be done now.
				 * But now we also have to check, whether all movements, which had been
				 * done in between, and one more, still can be done!
				 */

				// Check that all pushes originally between the both same-box-pushes are
				// still legal, and that the push after that also still is legal.
				int betweenPushes;
				for(betweenPushes = pushNo+1; betweenPushes <= sameBoxPushNo+1; betweenPushes++) {
					// If such a follow-up push does not exist, we can exit the loop
					if(betweenPushes > lastSolutionMovementNo) {
					    break;
					}

					// Alle "Zwischenpushes" nach und nach aus der History holen und setzen. Falls
					// ein Push nicht mehr möglich sein sollte (weil der vorgezogene Push dies
					// irgend wie verhindert), so kann die Schleife sofort verlassen werden.
					// Es wird dann der nächste same-box Push gesucht und durchgeführt, um zu prüfen,
					// ob danach die Zwischenzüge möglich sind. Dies ist notwendig, da die Kiste
					// eventuell erst mehrere Pushes hintereinander geschoben werden muss, bis
					// alle Zwischenpushes möglich sind.
					betweenMovement = historyClone.get(betweenPushes);

					// Es dürfen nur Pushes von anderen Kisten geprüft werden, nicht die der aktuellen
					// Kiste, denn die wurden ja bereits durchgeführt. Ausnahme ist der anschließende Push,
					// nach dem Vorziehen der SameBoxPushes noch durchführbar sein muss - egal, ob es eine
					// andere oder die aktuell relevante Kiste ist.
					if(betweenMovement.pushedBoxNo == sameBoxPush.pushedBoxNo &&
					   betweenPushes != sameBoxPushNo+1) {
						continue;
					}

					boxPosition = board.boxData.getBoxPosition(betweenMovement.pushedBoxNo);
					if(board.isBox(boxPosition+offset[betweenMovement.direction])) {
						break;
					}
					board.playersReachableSquares.update();
					if(board.playersReachableSquares.isSquareReachable(boxPosition-offset[betweenMovement.direction]) == false) {
						break;
					}

					// Perform push
					board.pushBox(boxPosition, boxPosition+offset[betweenMovement.direction]);
					board.setPlayerPosition(boxPosition);
				}

				// Die durchgeführten Zwischenzüge wieder rückgängig machen. Falls der letzte Push
				// der Zusatzpush war (mit dem geprüft wird, ob nach dem Vorziehen von SameBoxPushes
				// die darauffolgenden Pushes auch noch möglich sind), so muss er auf jeden Fall
				// rückgängig gemacht werden, denn er wurde in diesem Durchgang noch nicht vorgezogen.
				for(int pushNoUndo = betweenPushes-1; pushNoUndo > pushNo; pushNoUndo--) {
				    betweenMovement = historyClone.get(pushNoUndo);
					if(betweenMovement.pushedBoxNo == sameBoxPush.pushedBoxNo &&
					   pushNoUndo != sameBoxPushNo+1) {
						continue;
					}
					boxPosition = board.boxData.getBoxPosition(betweenMovement.pushedBoxNo);
					board.pushBox(boxPosition, boxPosition-offset[betweenMovement.direction]);
					board.setPlayerPosition(boxPosition-2*offset[betweenMovement.direction]);
				}

				// Falls alle Zwischenpushes + der nachfolgende Push durchgeführt werden
				// konnten, kann der Push tatsächlich vorgezogen werden.
				if(betweenPushes == sameBoxPushNo+2 || betweenPushes > lastSolutionMovementNo) {

					// Alle relevanten SameBoxPushes in der History vorziehen
					for(int bringForwardPushNo = pushNo+1; bringForwardPushNo <= sameBoxPushNo; bringForwardPushNo++) {
						betweenMovement = historyClone.get(bringForwardPushNo);
						if(betweenMovement.pushedBoxNo == sameBoxPush.pushedBoxNo) {
							historyClone.add(pushNo+++1, historyClone.remove(bringForwardPushNo));
						}
					}
				}
			}

			// Das Vorziehen der SameBoxPushes hat nicht funktioniert oder aber der nächste Push betrifft
			// sowieso die relevante Kiste. Es werden deswegen alle durchgeführten same-box-Pushes rückgängig gemacht.
			// (Vorgezoge Pushes wurden an dieser Stelle bereits vorgezogen, so dass "pushnr" entsprechend
			// erhöht wurde)
			for(int backPushNo = sameBoxPushNo-1; backPushNo > pushNo; backPushNo--) {
			    betweenMovement = historyClone.get(backPushNo);
			    if(betweenMovement.pushedBoxNo == movement.pushedBoxNo) {
					boxPosition = board.boxData.getBoxPosition(betweenMovement.pushedBoxNo);
					board.pushBox(boxPosition, boxPosition-offset[betweenMovement.direction]);
					board.setPlayerPosition(boxPosition-2*offset[betweenMovement.direction]);
				}
			}
		}

		// Restore original board
		for(int i = board.firstRelevantSquare; i < board.lastRelevantSquare; i++) {
			board.removeBox(i);
		}
		board.setBoardPosition(currentBoardPositionBackup);

		/*
		 * Insert the movements to the history. This includes creating the player
		 * moves between the pushes.
		 */
		for(int pushNo = 0; pushNo < historyClone.size(); pushNo++) {

		    movement = historyClone.get(pushNo);

			int boxPosition = board.boxData.getBoxPosition(movement.pushedBoxNo);

			// Get player path to the position next to the box.
			int[] playerPath = board.playerPath.getPathTo(boxPosition-offset[movement.direction]);

			// Add all moves of the player to the history.
			for(int moveNo = 1; moveNo < playerPath.length; moveNo++) {

				board.playerPosition = playerPath[moveNo];

				// Since the index has been set to the value which had been set before the solver has been started,
				// we can use the normal "addPlayerMove" method for inserting the player moves.
				application.movesHistory.addPlayerMove(board.getMoveDirectionNumber(playerPath[moveNo-1], playerPath[moveNo]));
			}

			// Perform push
			board.pushBox(boxPosition, boxPosition+offset[movement.direction]);
			board.playerPosition = boxPosition;

			// Store movement
			application.movesHistory.addMovement(movement.direction, board.getBoxNo(boxPosition));
		}

		// Set the board like it was before this method was called
		for(int i = board.firstRelevantSquare; i < board.lastRelevantSquare; i++) {
			board.removeBox(i);
		}
		board.setBoardPosition(currentBoardPositionBackup);

		// Set history index to begin of solution
		movementHistory.setMovementNo(firstSolutionMovementNo);
	}


	/**
	 * Individual method of each solver class to search for a solution
	 */
	abstract protected Solution searchSolution();


	/**
	 * Identifies the boxes which are relevant for the search.
	 * If a corral occured it is possible that not all boxes are releveant for the search.
	 * E.g. in the following situation:<pre>
	 * ####
	 * #  #
	 * #  #
	 * #$$#</pre>
	 * only these two boxes are relevant, since they must be pushed anyway, and cannot
	 * block other boxes in doing so.
	 * <p>
	 * Conditions:
	 * <ol>
	 * <li>There must be a corral</li>
	 * <li>all possible pushes of the corral boxes must go into the corral</li>
	 * <li>not all corral boxes are located on goals</li>
	 * <li>the player can perform already now all theoretically possible legal pushes
	 *     (not generating a deadlock) into the corral</li>
	 * </ol>
	 *
	 * If all conditions are satisfied, only the corral boxes are relevant for the next
	 * push.
	 * <p>
	 * Hint: This method is public only to call it from
	 * <code>JSoko.analyzeNewBoardPosition</code> for debug purposes.
	 *
	 * @return relevant boxes for the search, indexed by box numbers.
	 *         A returned <code>null</code> encodes "all boxes are relevant" for the search.
	 */
	public final boolean[] identifyRelevantBoxes() {

		// Position der Kiste, von der aus ein Corral gesucht wird.
		int corralCausingBoxPosition;

		// Nimmt die neue Position einer Kiste auf
		int newBoxPosition;

		// Hierdrin werden alle Kisten markiert, die für die weitere Suche des Solvers relevant sind.
		boolean[] relevantBoxes = new boolean[board.boxCount];

		// Corralindikatorwert bei Eintritt in diese Methode. Er wird gebraucht, um erkennen zu
		// können, ob ein Corral in diesem Aufruf ermittelt wurde oder noch vom einem vorigen Aufruf
		// existiert.
		int corralIndicatorAtBegin;


		// Corralindikator bei Methodenaufruf merken. Alle Corrals mit einem größeren Wert sind Corrals,
		// die im aktuellen Aufruf analysiert wurden.
		corralIndicatorAtBegin = corralIndicatorValue;

		// Alle Kisten auf NoBlocker setzen. Dies ist wichtig, da während der ICorralanalyse eine
		// normale Corralanalyse aufgerufen wird und eventuell noch Blockerkisten der letzten
		// Stellung vom Solver aktiv sind.
		board.boxData.setAllBoxesNotFrozen();

		// Von allen Kisten aus ein I-Corral suchen.
		for(int originBoxNo=0; originBoxNo<board.boxCount; originBoxNo++) {

			// Position der Kiste, von der aus ein I-Corral gesucht wird.
			corralCausingBoxPosition = board.boxData.getBoxPosition(originBoxNo);

			// Falls es ein Zielfeldraumlevel ist, werden alle Kisten im Zielfeldraum sowieso nicht durch
	        // die normale Suche behandelt. Von ihnen aus darf deshalb auch kein ICorral gesucht werden!
	        if(goalRoomSquares != null && goalRoomSquares[corralCausingBoxPosition] == true) {
	        	continue;
	        }

	        // Nur Corrals von Kisten aus suchen, die mindestens von einer Seite aus vom Spieler
	        // erreicht werden können.
	        int corralDirection = 0;
	        for(; corralDirection<DIRS_COUNT; corralDirection++) {
			    if(playersReachableSquares.isSquareReachable(corralCausingBoxPosition + offset[corralDirection]) == true) {
			    	break;
			    }
	        }
			if(corralDirection == DIRS_COUNT) {
				continue;
			}

			// Alle Corrals im Umfeld der Kiste bearbeiten.
			// (die erreichbaren Felder des Spielers wurden in der aufrufenden Methode bereits ermittelt!)
			for(corralDirection=0; corralDirection<DIRS_COUNT; corralDirection++) {

			    newBoxPosition = corralCausingBoxPosition + offset[corralDirection];

			    // Falls der Spieler die Seite erreichen kann oder das Feld gar nicht betretbar ist
			    // handelt es sich nicht um ein Corral und es kann gleich die nächste Seite geprüft werden.
			    // Dies kann ebenfalls getan werden, falls das Feld zu einem Corral gehört,
			    // welches bereits für diese Stellung untersucht wurde.
			    if(playersReachableSquares.isSquareReachable(newBoxPosition) ||
			       board.isAccessible(newBoxPosition) == false ||
			       corralSquares[newBoxPosition] > corralIndicatorAtBegin) {
					continue;
				}

			    // Alle Corrals, die bei der I-Corralsuche von diesem Feld aus analysiert werden werden
			    // mit einem Wert >= diesem Wert markiert.
			    lowestValueCombinedCorral = ++corralIndicatorValue;

			    // Falls das Feld Teil eines I-Corrals ist, so werden die Kisten des I-Corrals zurückgegeben.
			    if(isICorral(newBoxPosition, new CorralInfo()) == true) {

			        // Alle relevanten Kisten wurden mit einem Wert => g_indikatorUntergrenzeKombiniertesCorral markiert.
			        for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
			            if(corralSquares[board.boxData.getBoxPosition(boxNo)] >= lowestValueCombinedCorral) {
			            	relevantBoxes[boxNo] = true;
			            }
			        }

		            // Ausgaben zu Debugzwecken
					if(Debug.debugShowCorralsWhileSolving) {
			            for(int p=0; p<board.size; p++) {
			                if(corralSquares[p] >= lowestValueCombinedCorral) {
			                	board.setMarking(p);
			                }
			            }
			            application.redraw(true);
			            board.removeAllMarking();
					}
					if(Debug.debugShowICorrals) {
					    application.applicationGUI.mainBoardDisplay.displayInfotext("I-Corral found");
					}

					// Nur die Kisten des I-Corrals sind relevant.
			        return relevantBoxes;
			    }
			}
	    }

	    // Es konnte kein I-Corral gefunden werden.
	    return null;
	}

	/**
	 * This method searches for an I-corral, starting with the passed start square.
	 * Adjacent corrals integrated into the analysis as needed.  That way a found I-corral
	 * can consist of several corrals.
	 * <p>
	 * First, the corral containing the start square is checked for the I-corral attribute.
	 * When it needs another adjacent I-corral, the adjacent corral is checked for the
	 * I-corral attribute. When a corral is found that isn't an I-corral, the original
	 * corral isn't an I-corral.
	 * <p>
	 * Boxes are marked with the {@link #corralIndicatorValue} in the corral array.
	 * All boxes marked with a value >= {@link #lowestValueCombinedCorral}
	 * are part of the (combined) corral.
	 *
	 * @param startPosition	 square from which to start the search for an I-corral
	 * @param corralInformation  info object filled for the caller
	 *
	 * @return <code>true</code> if an I-corral has been found, and
	 *        <code>false</code> if no I-corral has been found
	 */
	final private boolean isICorral(int startPosition, CorralInfo corralInformation) {
		// FFS: (hm) this method is too large, and contains too deeply nested code.
		//      I find it hard to read .

		// Position einer Kiste
		int boxPosition;

		// Nimmt die neue Position einer Kiste auf
		int newBoxPosition;

		// Nachbarposition einer Kiste
		int neighborSquarePosition;

		// Nimmt die Position auf von der aus der Spieler eine Kiste verschieben möchte.
		int positionToPushFrom;

		// Nimmt eine Spielerposition auf
		int playerPosition;

		// Nimmt "die neue" Spielerposition auf, wenn der Spieler bewegt wird.
		int newPlayerPosition;

		// Array, in dem die zu analysierenden Positionen gespeichert werden. Dieses Array wird
		// bei der Ermittlung der Corralfelder verwendet.
		int[] positionsToBeAnalyzed = new int[board.size];

		// Stack, in den die Positionen aller Corralkisten aufgenommen werden + zugehöriger Index
		int[] positionsStack = new int[board.boxCount];
		int topOfStack = -1;

		// Anzahl relevanter Kisten in dem jeweiligen Corral
		int relevantBoxesCount = 0;

		// Corralindikatorwert für das Corral, welches aktuell untersucht wird.
		int currentCorralIndicatorValue;


		// Jedes Corral wird mit einem eigenen Wert gekennzeichnet, um es identifizieren zu können.
		currentCorralIndicatorValue = corralIndicatorValue++;

		/*
		 * Es werden nun alle Corralfelder als solche markiert. Dabei werden auch gleich die
		 * zum Corral gehörenden Kisten ermittelt.
		 */
		// Stack, in dem die Positionen der noch zu analysierenden Felder gespeichert werden
		// analysieren = ist Feld erreichbar durch den Spieler oder nicht
		positionsToBeAnalyzed[0] = startPosition;
		topOfStack = 0;
		corralSquares[startPosition] = currentCorralIndicatorValue;

		while(topOfStack != -1 ) {
			playerPosition = positionsToBeAnalyzed[topOfStack--];

			// Ein unbesetztes Zielfeld oder eine Kiste auf einem NichtZielfeld bedeuten, dass das
			// die Kiste auf jeden Fall noch verschoben werden müssen.
			if(corralInformation.isAtLeastOneBoxNotOnGoal == false &&
					((board.isGoal(playerPosition) || board.isBox(playerPosition)) &&
							board.isBoxOnGoal(playerPosition) == false))
			{
				corralInformation.isAtLeastOneBoxNotOnGoal = true;
			}

			// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
			// falls sie vorher nicht bereits erreicht wurden.
			for(int direction=0; direction<DIRS_COUNT; direction++) {

				newPlayerPosition = playerPosition + offset[direction];

				if(board.isAccessible(newPlayerPosition) && corralSquares[newPlayerPosition] != currentCorralIndicatorValue) {
					// Feld als neue Ausgangsbasis aufnehmen und sofort als schon erreicht markieren (nicht erst, wenn es aus dem Stack geholt wird!)
					positionsToBeAnalyzed[++topOfStack] = newPlayerPosition;
					corralSquares[newPlayerPosition] = currentCorralIndicatorValue;
				}
				else {
					// Prüfen, ob es eine Kiste ist, die noch nicht zum aktuellen Corral gehört.
					if(board.isBox(newPlayerPosition) && corralSquares[newPlayerPosition] != currentCorralIndicatorValue) {

						// Die Kiste hat ein Corralnachbarfeld. Falls es ein I-Corral ist, so ist diese Kiste
						// für die Suche relevant - deshalb gehört das Feld der Kiste nun auch zum Corral.
						positionsStack[relevantBoxesCount++] = newPlayerPosition;
						corralSquares[newPlayerPosition] 	 = currentCorralIndicatorValue;

						// Alle geblockten Nachbarkisten zum Corral hinzufügen.
						for(int neighborDirection=0; neighborDirection < DIRS_COUNT; neighborDirection++) {
							neighborSquarePosition = newPlayerPosition + offset[neighborDirection];

							// Falls auf dem Nachbarfeld keine Kiste steht oder sie schon zum Corral
							// gehört, kann gleich in der nächsten Richtung weitergesucht werden.
							if(board.isBox(neighborSquarePosition) == false ||
									corralSquares[neighborSquarePosition] == currentCorralIndicatorValue) {
								continue;
							}

							/*
							 * Nun wird geprüft, ob die Kiste auf der anderen Achse geblockt ist.
							 */
							// Hilfsvariablen, die die Positionen der Nachbarfelder auf einer Achse aufnehmen
							int neighborSquare1 = neighborSquarePosition + offset[(neighborDirection+2)%4];
							int neighborSquare2 = neighborSquarePosition - offset[(neighborDirection+2)%4];

							if(
									// Geblockt durch Mauer
									board.isWall(neighborSquare1) || board.isWall(neighborSquare2) ||

									// Prüfen auf geblockt durch Blockerkiste ist sinnlos, da der Blockerstatus
									// am Anfang gelöscht wird!

									// Geblockt durch Kiste des aktuellen Corrals
									board.isBox(neighborSquare1) && corralSquares[neighborSquare1] == currentCorralIndicatorValue ||
									board.isBox(neighborSquare2) && corralSquares[neighborSquare2] == currentCorralIndicatorValue ||

									// Prüfen, ob der Spieler die Kiste nur auf Deadlockfelder schieben könnte
									board.isSimpleDeadlockSquare(neighborSquare1) && board.isSimpleDeadlockSquare(neighborSquare2))
							{

								// Die Kiste ist nicht verschiebbar. Sie wird deshalb in das Corral mit aufgenommen.
								corralSquares[neighborSquarePosition] = currentCorralIndicatorValue;

								// Die Kiste ist für das aktuelle Corral relevant und verändert deshalb seines Status.
								if(board.isGoal(neighborSquarePosition) == false) {
									corralInformation.isAtLeastOneBoxNotOnGoal = true;
								}
							}
						}
					}
				}
			}
		}

		/*
		 * Die Positionen aller Corralkisten sind nun im Stack. Es muss nun
		 * geprüft werden, ob alle gültigen Verschiebungen der Kisten in das Corral führen.
		 * Falls ja, und der Spieler alle diese Verschiebungen auch durchführen kann,
		 * so sind für den nächsten Push des Solvers nur die Kisten des Corrals relevant.
		 * (Es wird versucht eine Kiste zu finden, die beweist, dass es kein I-Corral ist.
		 * Falls keine solche Kiste gefunden werden kann gilt das Corral als I-Corral)
		 */
        for(int corralBoxNo = 0; corralBoxNo < relevantBoxesCount; corralBoxNo++) {
		    boxPosition = positionsStack[corralBoxNo];

	        // Es wird ermittelt, ob mindestens eine der Kisten des aktuellen Corrals
	        // nicht auf einem Zielfeld steht.
	        if(corralInformation.isAtLeastOneBoxNotOnGoal == false &&
	           board.isGoal(boxPosition) == false) {
				corralInformation.isAtLeastOneBoxNotOnGoal = true;
			}

		    for(int pushDirection=0 ; pushDirection<DIRS_COUNT; pushDirection++) {
		        newBoxPosition     = boxPosition+offset[pushDirection];
		        positionToPushFrom = boxPosition-offset[pushDirection];

		        // Wenn das Zielfeld nicht von einer Kiste betreten werden kann oder das Feld zum
		        // Verschieben eine Mauer ist, dann ist der Push sowieso unmöglich und es kann
		        // gleich in der nächsten Richtung geprüft werden.
		        // Außerdem kann der Spieler nicht vom einem Feld verschieben, das im aktuellen Corral liegt.
		        // (Eine Kiste auf dem Zielfeld würde auf jeden Fall blockieren, da sie auf der einen Achse
		        // nur den Weg frei machen könnte, um die andere Kiste in das Corral zu schieben und
		        // auf der anderen Achse auf jeden Fall blockiert sein muss!)
		        if(        board.isAccessibleBox(newBoxPosition) == false
		        		|| board.isWall(positionToPushFrom)
		        		|| corralSquares[positionToPushFrom] == currentCorralIndicatorValue) {
		        	continue;
		        }

	            /*
	             * Es muss nun geprüft werden, ob alle gültigen Pushes in das Corral führen würden und
	             * diese Pushes auch alle derzeit durchführbar sind.
	             */

		        // Prüfen, ob das Zielfeld außerhalb des Corrals liegt.
		        if(corralSquares[newBoxPosition] < lowestValueCombinedCorral) {

		            // Aus Performancegründen nur einen einfachen Freezetest durchführen.
	                boolean isDeadlock = false;
	                board.removeBox(boxPosition);
	                // FFS/hm: axis logic by arithmetic
		            for(int direction=0; direction<DIRS_COUNT; direction+=2) {
	    	            int neighborSquare1 = newBoxPosition + offset[direction];
	    	            int neighborSquare2 = newBoxPosition - offset[direction];
	    	            if(board.isWall(neighborSquare1) || board.isWall(neighborSquare2) ||
	    	               board.isSimpleDeadlockSquare(neighborSquare1) && board.isSimpleDeadlockSquare(neighborSquare2)) {
		    	            neighborSquare1 = newBoxPosition + offset[(direction+2)%4];
		    	            if(board.isBox(neighborSquare1) && corralSquares[neighborSquare1] == currentCorralIndicatorValue &&
		    	               (board.isWall(neighborSquare1+offset[direction]) ||
		    	                board.isWall(neighborSquare1-offset[direction]) ||
		    	                board.isSimpleDeadlockSquare(neighborSquare1+offset[direction]) && board.isSimpleDeadlockSquare(neighborSquare1-offset[direction]))) {
		    	                // Die aktuelle Kiste und die Nachbarkiste sind geblockt.
		    	                // Falls mindestens eine von ihnen nicht auf einem Zielfeld steht -> Deadlock
		    	                if(board.isGoal(newBoxPosition) == false ||
		    	                   board.isGoal(neighborSquare1) == false) {
		    	                    isDeadlock = true;
		    	                    break;
		    	                }
		    	            }
		    	            neighborSquare2 = newBoxPosition - offset[(direction+2)%4];
		    	            if(board.isBox(neighborSquare2) && corralSquares[neighborSquare2] == currentCorralIndicatorValue &&
				    	      (board.isWall(neighborSquare2+offset[direction]) ||
				    	       board.isWall(neighborSquare2-offset[direction]) ||
				    	       board.isSimpleDeadlockSquare(neighborSquare2+offset[direction]) && board.isSimpleDeadlockSquare(neighborSquare2-offset[direction]))) {
		    	                // Die aktuelle Kiste und die Nachbarkiste sind geblockt.
		    	                // Falls mindestens eine von ihnen nicht auf einem Zielfeld steht -> Deadlock
		    	                if(board.isGoal(newBoxPosition) == false ||
		    	                   board.isGoal(neighborSquare2) == false) {
		    	                    isDeadlock = true;
		    	                    break;
		    	                }
				    	    }
	    	            }
    	            }
		            board.setBox(boxPosition);

		            // Falls der Push ein Deadlock wäre -> sofort nächste Richtung.
		            if(isDeadlock == true) {
		            	continue;
		            }


		            if(board.isBox(positionToPushFrom)) {

			            // Der Fall, dass eine Kiste des aktuellen Corrals das Feld blockiert wurde weiter
		                // oben schon abgefangen, so dass hier nur die Fälle behandelt werden, die Kisten
		                // in anderen Corrals betreffen.
		                // (zum aktuellen Corral wurden zu Beginn auch alle blockierten Nachbarkisten hinzugefügt)
		                if(corralSquares[positionToPushFrom] >= lowestValueCombinedCorral ||
		                   isBoxFrozen(positionToPushFrom, pushDirection)) {
		                    corralInformation.isACombinedCorral = true;

		                    // Falls die geblockte Kiste auf einem NichtZielfeld steht, so wird der entsprechende
		                    // Status gesetzt.
		                    if(board.isGoal(positionToPushFrom) == false) {
		                    	corralInformation.isAtLeastOneBoxNotOnGoal = true;
		                    }
		                    continue;
		                }

		                // Die Kiste ist nicht geblockt und gehört zu keinem bisher erkannten Corral.
		                // Das Corral wird deshalb um diese Kiste und alle Nachbarkisten erweitert.
	                    int[] stack = new int[board.boxCount];
	                    int top = -1;
	                    stack[++top] = positionToPushFrom;
	                    corralSquares[positionToPushFrom ] = currentCorralIndicatorValue;

	                    while(top >= 0) {
	                        int position = stack[top--];

	                        positionsStack[relevantBoxesCount++] = position;

	                        for(int i=0; i<DIRS_COUNT; i++) {
	                            if(board.isBox(position+offset[i]) &&
	                               corralSquares[position+offset[i]] < lowestValueCombinedCorral) {
	                                corralSquares[position+offset[i]] = currentCorralIndicatorValue;
	                                stack[++top] = position+offset[i];
	                            }
	                        }
	                    }
		                continue;
		            }

		            if(playersReachableSquares.isSquareReachable(positionToPushFrom) == true) {
	                    /*
	                     * Der Push führt auf ein Feld außerhalb des Corrals und der Spieler kann
	                     * diesen Push derzeit ausführen.
	                     */

		                // Falls es kein weiteres Corral hinter der Kiste gibt, so kann der Spieler
		                // die Kiste auf jeden Fall auf das NichtCorralfeld verschieben => Kein I-Corral.
		                if(playersReachableSquares.isSquareReachable(newBoxPosition)) {
		                	return false;
		                }

				        // Der Push führt in ein noch nicht analysiertes Corral. Dieses Corral muss
		                // deshalb ebenfalls analysiert werden.
					    CorralInfo corralInformationNeighborCorral = new CorralInfo();
				        if(isICorral(newBoxPosition, corralInformationNeighborCorral) == false) {
				            // Das Corral ist kein I-Corral => Das gesamte kombinierte Corral ist kein I-Corral.
				            return false;
				        }

				        // Das aktuelle Corral muss mit dem Nachbarcorral kombiniert werden.
				        if(corralInformationNeighborCorral.isACombinedCorral == true) {
				            corralInformation.isACombinedCorral = true;
				            corralInformation.isAtLeastOneBoxNotOnGoal |= corralInformationNeighborCorral.isAtLeastOneBoxNotOnGoal;

				            // Dieses Corral könnte weiterhin ein I-Corral sein, da der Push vom Spieler
				            // nicht durchgeführt werden kann, da das Feld zum Verschieben im kombinierten
				            // Corral liegt.
				            continue;
				        }

				        // Das Nachbarcorral ist ein Einzel-I-Corral (es benötigt kein anderes Corral).
				        // Somit wurde ein I-Corral gefunden und es kann die gesamte Rekursion durch
				        // dieser Status zurückgegeben werden.
				        // Alle Kisten, die nicht zu diesem Corral gehören, müssen als irrelevant ge-
				        // kennzeichnet werden. Dies geschieht, in dem die Untergrenze für den Indikator-
				        // wert auf den aktuellen Wert angehoben wird.
				        if(currentCorralIndicatorValue > lowestValueCombinedCorral) {
				        	lowestValueCombinedCorral = currentCorralIndicatorValue;
				        }
				        corralInformation.isACombinedCorral = false;
				        corralInformation.isAtLeastOneBoxNotOnGoal = true;
				        return true;
				    }

		            /*
		             * Der Push führt auf ein NichtCorralfeld und der Spieler kann die Position zum
		             * Verschieben derzeit nicht erreichen
		             */

	        	    // Nun wird geprüft, ob der Spieler theoretisch überhaupt auf die Position zum
	                // Verschieben gelangen kann. Falls er dies nicht kann, weil das Feld zum Verschieben
		            // in einem bereits analysierten Corral liegt, so ist der Push in jedem
	                // Fall unmöglich und es kann sofort in der nächsten Richtung weitergeprüft werden.
	                // Der Fall, dass das Feld im aktuellen Corral liegt wäre bereits weiter oben
		            // abgefangen worden => kombiniertes Corral.
	                if(corralSquares[positionToPushFrom] >= lowestValueCombinedCorral) {
	                    corralInformation.isACombinedCorral = true;
	                    continue;
	                }

	                // Der Spieler kann die Kiste von außerhalb des kombinierten Corrals auf ein
	                // Feld außerhalb des kombinierten Corrals verschieben.
	                // Es muss nun geprüft werden, ob die Position des Spielers vielleicht in einem
	                // Bereich liegt, auf den dieses Corral ausgedehnt werden kann (-> kombiniertes
	                // Corral)
	                // Falls ja, so könnte er von dort aus nicht schieben, da das Feld dann im
	                // kombinierten Corral liegen würde.
	                CorralInfo corralInformationNeighborCorral = new CorralInfo();
	                if(isICorral(positionToPushFrom, corralInformationNeighborCorral) == false) {
	                    // Es existiert kein Nachbar-I-Corral. Somit ist auch das aktuelle Corral kein I-Corral.
	                    return false;
	                }

                    // Das aktuelle Corral muss mit dem Nachbar-I-Corral kombiniert werden,
	                // falls das Nachbarcorral kein Einzel-I-Corral ist.
                    if(corralInformationNeighborCorral.isACombinedCorral == true) {
                        corralInformation.isACombinedCorral = true;
                        corralInformation.isAtLeastOneBoxNotOnGoal |= corralInformationNeighborCorral.isAtLeastOneBoxNotOnGoal;

                        // Dieses Corral könnte weiterhin ein I-Corral sein, da der Push vom Spieler
                        // nicht durchgeführt werden kann, da das Feld zum Verschieben im kombinierten
                        // Corral liegt.
                        continue;
                    }

                    // Das Nachbarcorral ist ein Einzel-I-Corral (es benötigt kein anderes Corral).
                    // Somit wurde ein I-Corral gefunden und es kann die gesamte Rekursion durch
                    // dieser Status zurückgegeben werden.
                    // Alle Kisten, die nicht zu diesem Corral gehören, müssen als irrelevant ge-
                    // kennzeichnet werden. Dies geschieht, in dem die Untergrenze für den Indikator-
                    // wert auf den aktuellen Wert angehoben wird.
                    if(currentCorralIndicatorValue > lowestValueCombinedCorral) {
                    	lowestValueCombinedCorral = currentCorralIndicatorValue;
                    }
                    corralInformation.isACombinedCorral = false;
                    corralInformation.isAtLeastOneBoxNotOnGoal = true;
                    return true;
	            }
	            else {
	                /*
	                 * Der Push der Kiste würde IN das kombinierte Corral führen und das Feld
	                 * der Kiste ist für eine Kiste betretbar (keine Mauer, keine Kiste, kein Simple-Deadlock)
	                 */

	                // Falls der Spieler die Position zum Verschieben erreichen kann, so
	                // kann er den Push in das Corral durchführen => es könnte weiterhin ein
	                // I-Corral sein.
	                // Falls der Push in ein anderes als das aktuelle Corral führt, so
	                // kann das aktuelle Corral kein Einzel-I-Corral sein, sondern muss ein
	                // kombiniertes Corral sein.
	                if(playersReachableSquares.isSquareReachable(positionToPushFrom) == true) {
	                    if(corralSquares[newBoxPosition] != currentCorralIndicatorValue) {
	                    	corralInformation.isACombinedCorral = true;
	                    }
	                    continue;
	                }

	                /*
	                 * Der Push führt in das Corral, kann aber derzeit nicht vom Spieler durchgeführt werden.
	                 */

	                // Es wird nun geprüft, ob der Push ein Deadlock verursachen würde.
	                // Denn dann wäre es kein gültiger Push und die Analyse könnte sofort mit der
	                // nächsten Richtung weitergehen.
					// Damit Einzel-I-Corrals korrekt erkannt werden, werden die Deadlockprüfungen
					// nur durchgeführt, falls der Push in das aktuelle Corral führt.
					if(corralSquares[newBoxPosition] == currentCorralIndicatorValue) {
						board.pushBox(boxPosition, newBoxPosition);

	                    // Freezedeadlocktest durchführen.
		                boolean isDeadlock = deadlockDetection.freezeDeadlockDetection.isDeadlock(newBoxPosition, false);

		                // Falls kein Deadlock gefunden wurde wird noch ein Corraldeadlocktest durchgeführt.
		                if(isDeadlock == false) {

		                    // Alle Nicht-Corralkisten deaktiv setzen und vom Feld nehmen
		                    // Achtung: Es werden nur die Kisten des aktuellen Corrals auf dem Feld gelassen!
		                    // Es wäre auch möglich alle Kisten des kombinierten Corrals auf dem Feld zu lassen,
		                    // dann müsste aber bei einem Deadlock auf jeden Fall dieses Deadlock als kombiniertes
		                    // Corral gekennzeichnet werden (oder aufwendig das Deadlock noch einmal nur mit den
		                    // Kisten dieses Corrals bestätigt werden)
		                    for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
		                        if(corralSquares[board.boxData.getBoxPosition(boxNo)] != currentCorralIndicatorValue) {
		                            board.boxData.setBoxInactive(boxNo);
		                            board.removeBox(board.boxData.getBoxPosition(boxNo));
		                        }
		                    }

		                    // Die Corralerkennung bekommt 150 Millisekunden Zeit zu beweisen, dass
		                    // das Corral ein Deadlock ist.
		                    isDeadlock = deadlockDetection.corralDeadlockDetection.isDeadlock(newBoxPosition, System.currentTimeMillis()+150);

		                    // Alle Nicht-Corralkisten wieder aktiv ins Spielfeld setzen
		                    for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
		                        if(corralSquares[board.boxData.getBoxPosition(boxNo)] != currentCorralIndicatorValue) {
		                            board.boxData.setBoxActive(boxNo);
		                            board.setBoxWithNo(boxNo, board.boxData.getBoxPosition(boxNo));
		                        }
		                    }
		                }

		                // Push rückgängig machen
		                board.pushBoxUndo(newBoxPosition, boxPosition);

		                // Falls der Push ein Deadlock verursachen würde kann es weiterhin ein I-Corral sein.
		                if(isDeadlock == true) {
		                	continue;
		                }
					}


	                /*
	                 * Jetzt steht folgendes fest:
	                 * Der Push würde in das Corral führen und kein Deadlock verursachen und der Spieler
	                 * kann theoretisch auf das Feld zum Verschieben kommen, kann dies aber in der
	                 * derzeitigen Stellung nicht.
	                 */

	        	    // Nun wird geprüft, ob der Spieler theoretisch überhaupt auf die Position zum
	                // Verschieben gelangen kann. Falls er dies nicht kann, weil das Feld zum Verschieben
		            // in einem bereits analysierten Corral liegt, so ist der Push in jedem
	                // Fall unmöglich und es kann sofort in der nächsten Richtung weitergeprüft werden.
	                // Der Fall, dass das Feld im aktuellen Corral liegt wäre bereits weiter oben
		            // abgefangen worden => kombiniertes Corral.
	                if(corralSquares[positionToPushFrom] >= lowestValueCombinedCorral) {
	                    corralInformation.isACombinedCorral = true;
	                    continue;
	                }

					if(board.isBox(positionToPushFrom)) {

					    // Falls die Kiste geblockt ist, so kann sie nur von einer Kiste des
					    // kombinierten Corrals geblockt sein.
						if(isBoxFrozen(positionToPushFrom, pushDirection)) {
						    corralInformation.isACombinedCorral = true;
		                    if(board.isGoal(positionToPushFrom) == false) {
		                    	corralInformation.isAtLeastOneBoxNotOnGoal = true;
		                    }
		                    continue;
						}

						// Die Kiste ist nicht geblockt und gehört zu keinem bisher erkannten Corral.
		                // Das Corral wird deshalb um diese Kiste und alle Nachbarkisten erweitert.
	                    int[] stack = new int[board.boxCount];
	                    int top = -1;
	                    stack[++top] = positionToPushFrom;
	                    corralSquares[positionToPushFrom] = currentCorralIndicatorValue;

	                    while(top >= 0) {
	                        int position = stack[top--];
	                        positionsStack[relevantBoxesCount++] = position;

	                        for(int i=0; i<DIRS_COUNT; i++) {
	                            if(board.isBox(position+offset[i]) &&
	                               corralSquares[position+offset[i]] < lowestValueCombinedCorral) {
	                                corralSquares[position+offset[i]] = currentCorralIndicatorValue;
	                                stack[++top] = position+offset[i];
	                            }
	                        }
	                    }
		                continue;
					}

	                // Es muss nun geprüft werden, ob die Position zum Verschieben vielleicht in einem
	                // Bereich liegt, auf den dieses Corral ausgedehnt werden kann (-> kombiniertes
	                // Corral)
					// Falls ja, so könnte er von dort aus nicht schieben, da das Feld dann im
	                // kombinierten Corral liegen würde.
	                CorralInfo corralInformationNeighborCorral = new CorralInfo();
	                if(isICorral(positionToPushFrom, corralInformationNeighborCorral) == false) {
	                    // Es existiert kein Nachbar-I-Corral. Somit ist auch das aktuelle Corral kein I-Corral.
	                    return false;
	                }

                    // Das aktuelle Corral muss mit dem Nachbar-I-Corral kombiniert werden.
                    if(corralInformationNeighborCorral.isACombinedCorral == true) {
                        corralInformation.isACombinedCorral = true;
                        corralInformation.isAtLeastOneBoxNotOnGoal |= corralInformationNeighborCorral.isAtLeastOneBoxNotOnGoal;

                        // Dieses Corral könnte weiterhin ein I-Corral sein, da der Push vom Spieler
                        // nicht durchgeführt werden kann, da das Feld zum Verschieben im kombinierten
                        // Corral liegt.
                        continue;
                    }

                    // Das Nachbarcorral ist ein Einzel-I-Corral (es benötigt kein anderes Corral).
                    // Somit wurde ein I-Corral gefunden und es kann die gesamte Rekursion durch
                    // dieser Status zurückgegeben werden.
                    // Alle Kisten, die nicht zu diesem Corral gehören, müssen als irrelevant ge-
                    // kennzeichnet werden. Dies geschieht, in dem die Untergrenze für den Indikator-
                    // wert auf den aktuellen Wert angehoben wird.
                    if(currentCorralIndicatorValue > lowestValueCombinedCorral) {
                        lowestValueCombinedCorral = currentCorralIndicatorValue;
                    }
                    corralInformation.isACombinedCorral = false;
                    corralInformation.isAtLeastOneBoxNotOnGoal = true;
                    return true;
	            }
		    }
        }

	    // Alle Kisten wurden untersucht. Alle Pushes führen in das Corral und alle gültigen
        // Pushes können derzeit auch vom Spieler durchgeführt werden.
	    // => es ist ein I-Corral.

		// Falls es ein I-Corral auf der ersten Rekursionsebene ist, so ist es kein I-Corral,
		// falls alle Kisten des (kombinierten) I-Corrals auf Zielfeldern stehen.
		if(currentCorralIndicatorValue == lowestValueCombinedCorral) {
		    if(corralInformation.isAtLeastOneBoxNotOnGoal == false) {
		        return false;
		    } else {
			    // Ein I-Corral, bei dem alle Kisten im Corral sind bringt keinen Vorteil. Deshalb wird
			    // dieses Corral nicht als I-Corral gewertet und der Methode die Chance gegeben ein anderes
			    // I-Corral, welches weniger Kisten besitzt zu finden.
			    // (Diese Prüfung wird nicht in tieferen Ebenen der Rekursion durchgeführt, da zwar
				// Zwischenzeitlich alle Kisten im Corral sein können, aber danach noch ein Einzel-I-Corral
				// gefunden werden könnte. Dieses Einzel-I-Corral könnte somit unter Umständen bei der
				// Suche übersehen werden)
			    int boxNo = 0;
			    for(; boxNo < board.boxCount; boxNo++) {
			        if(corralSquares[board.boxData.getBoxPosition(boxNo)] < lowestValueCombinedCorral) {
			        	break;
			        }
			    }
			    if(boxNo == board.boxCount) {
			    	return false;
			    }

				// I-Corral gefunden.
		        return true;
		    }
		}

		// Es wurde ein I-Corral in einer tieferen Ebene (Ebene tiefer als 1. Ebene) gefunden.
		// Selbst wenn alle Kisten bislang auf Zielfeldern stehen, so kann durch ein weiteres
		// Corral, was zu den bisherigen Corrals hinzugenommen wird, trotzdem noch eine
		// Kiste gefunden werden, die nicht auf einem Zielfeld steht. Deshalb wird auch in
		// diesem Fall das derzeitige Corral als I-Corral gewertet.
		// Es darf aber nicht als Einzel-I-Corral gewertet werden, da es alleine kein I-Corral ist!
		if(corralInformation.isAtLeastOneBoxNotOnGoal == false) {
		    corralInformation.isACombinedCorral = true;
		} else {
		    // Falls ein Einzel-I-Corral gefunden wurde, so wird die Untergrenze auf dieses
		    // Corral angehoben, damit nur dieses Corral als relevant gekennzeichnet ist.
		    if(corralInformation.isACombinedCorral == false) {
                if(currentCorralIndicatorValue > lowestValueCombinedCorral) {
                    lowestValueCombinedCorral = currentCorralIndicatorValue;
                }
		    }
		}
	    return true;
	}


	/**
	 * Checks whether a box at the passed position is blocked on the opposite axis
	 * with respect to the passed push direction.
	 * All boxes of the combined corral are considered.
	 *
	 * @param boxPosition	position of the box to analyze
	 * @param pushDirection	direction of axis the opposite of which we analyze
	 *
	 * @return	whether the box is blocked
	 */
	final private boolean isBoxFrozen(int boxPosition, int pushDirection) {

		// Determine the neighbor squares on the other axis
		final int ortodir = Directions.getOrthogonalDirection(pushDirection);
        int neighborSquare1 = boxPosition + offset[ortodir];
        int neighborSquare2 = boxPosition - offset[ortodir];

        if(
        		// blocked by wall
        		board.isWall(neighborSquare1)
        	 || board.isWall(neighborSquare2)

        	 	// checking blocked by blocker box does not work, since the blocker state
        	 	// has been cleared at the start.

        	 	// blocked by box from the combined corral
        	 || board.isBox(neighborSquare1) && corralSquares[neighborSquare1] >= lowestValueCombinedCorral
        	 || board.isBox(neighborSquare2) && corralSquares[neighborSquare2] >= lowestValueCombinedCorral

        	 	// Prüfen, ob der Spieler die Kiste nur auf Deadlockfelder schieben könnte
        	 || (    board.isSimpleDeadlockSquare(neighborSquare1)
        		  && board.isSimpleDeadlockSquare(neighborSquare2) )
          ) {
            return true;
        }

        return false;
	}


	/**
	 * Determines whether the current level is a goal room level.
	 * That is the case when all goals are located inside a room, which does not contain
	 * any box, and can be reached only via a single corral forcer.
	 */
	final private void setGoalRoomInformation() {

	    int goalPosition;

	    int playerPosition;
	    int newPlayerPosition;

	    // Anzahl Corralerzwingernachbarfelder, die nicht im Zielfeldraum liegen.
	    int corralForcerNeighborSquaresCount = 0;

	    // Flag, das angibt, ob eine Kiste im Zielfeldraum gefunden wurde.
	    boolean isBoxInGoalRoom = false;

	    // Anzahl Corralerzwinger
	    int corralForcerCount = 0;

	    boolean[] goalRoom = new boolean[board.size];

	    // Positionen aller Corralerzwinger im Zielfeldraum
	    int[] corralForcerPositionsStack = new int[board.size/2];

	    // Stack, in dem die zu analysierenden Positionen gespeichert werden. Dieser Stack wird
	    // bei der Ermittlung der Corralfelder verwendet.
	    IntStack positionsToBeAnalyzed = new IntStack(board.size);


        // Update the box distances so the corral forcer squares are identified. If the solver
        // has just been run before in this level they may have the state from the end board position of
        // the level. (Search direction doesn't matter; BACKWARD is chosen)
        board.distances.updateBoxDistances(SearchDirection.BACKWARD, true);

	    // Alle Zielfelder als Startposition eintragen
	    for(int goalNo=0; goalNo<board.goalsCount; goalNo++) {
	        goalPosition = board.getGoalPosition(goalNo);
	        positionsToBeAnalyzed.add(goalPosition);
		    goalRoom[goalPosition] = true;
	    }

	    // Von allen Zielfeldern aus alle Zielfeldraumfelder markieren
	    while(!positionsToBeAnalyzed.isEmpty() && isBoxInGoalRoom == false) {
	    	playerPosition = positionsToBeAnalyzed.remove();

	    	// Falls ein Corralerzwinger erreicht wurde ist dort der Zielraum erst einmal zu Ende.
	    	if(board.isCorralForcer(playerPosition) == true) {
	    	    corralForcerPositionsStack[corralForcerCount] = playerPosition;
	    	    corralForcerCount++;
	    	    goalRoom[playerPosition] = true;
	    	    continue;
	    	}

	    	// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
	    	// falls sie vorher nicht bereits erreicht wurden.
	    	for(int direction=0; direction<4; direction++) {
	    	    newPlayerPosition = playerPosition + offset[direction];

	    	    // Falls die Position bereits vorher erreicht wurde oder eine Mauer ist,
	    	    // so wird gleich in der nächsten Richtung weitergeprüft.
	    	    if(goalRoom[newPlayerPosition] == true ||
	    	       board.isWall(newPlayerPosition) == true) {
					continue;
				}

	    	    // Falls eine Kiste erreicht wurde, so wird die Suche abgebrochen -> Kein Zielfeldraum.
	    	    if(board.isBox(newPlayerPosition)) {
		    	    isBoxInGoalRoom = true;
	    	        break;
	    	    }

	    	    // Die neue Position wird in den Zielraum aufgenommen und von ihr aus weitergesucht.
    	        positionsToBeAnalyzed.add(newPlayerPosition);
    	    	goalRoom[newPlayerPosition] = true;
  	  		}
	    }

//	    for(int position=0; position<board.boardSize; position++)
//	    	if(goalRoom[position] == true)
//	    		board.setMarking(position);
//	    application.redraw(true);
//	    for(int position=0; position<board.boardSize; position++)
//	    	if(goalRoom[position] == true)
//	    		board.removeMarking(position);

	    // Falls eine Kiste innerhalb des Raums gefunden wurde gilt der Raum nicht als Zielfeldraum.
	    if(isBoxInGoalRoom == true) {
	        return;
	    }

	    // Es wird nun geprüft, wieviele Corralerzwinger eine NichtZielfeldraumnachbarposition haben.
	    // TODO: Zielfeldräume mit Kisten; Zielfeldraum solange um weitere Räume erweitern, wie diese Räume
	    // keine Kisten enthalten, ...
	    for(int corralForcerNo=0; corralForcerNo<corralForcerCount; corralForcerNo++) {
	        for(int direction=0; direction<4; direction++) {
	            newPlayerPosition = corralForcerPositionsStack[corralForcerNo]+offset[direction];
	            if(goalRoom[newPlayerPosition] == false && board.isOuterSquareOrWall(newPlayerPosition) == false) {
	                corralForcerNeighborSquaresCount++;

	                goalRoomEntrancePosition = corralForcerPositionsStack[corralForcerNo];

	        	    // Falls es mehr als 1 Nachbarfeld gibt, welches nicht im Zielfeldraum liegt,
	                // so ist es kein Zielfeldraum.
	                if(corralForcerNeighborSquaresCount > 1) {
	                    return;
	                }

	                // Es wird jeweils nur ein Außenfeld pro Corralerzwinger gezählt.
//	                break;
	                // Auskommentiert da: Wenn ein Corralerzwinger zwei Außenfelder hat ist es
	                // wichtig, dass eine Kiste auf ihm auch korrekt in den Zielraum geschoben werden
	                // kann. Ist eine Seite nicht erreichbar kann sie eventuell nicht auf die richtige
	                // Weise in den Zielraum geschoben werden. Dies muss beim automatischen
	                // Schieben im Solver geprüft werden (derzeit nicht eingebaut).
	            }
	        }
	    }

	    // Es wurde ein Zielfeldraum gefunden. Es wird nun ermittelt, wie dieser Raum gelöst werden kann.
		goalRoomSolutionPath = new SolverGoalRoom(application, this, solverGUI).searchSolution(goalRoomEntrancePosition, goalRoom);

		if(Debug.isDebugModeActivated) {
			if(goalRoomSolutionPath == null) {
			    System.out.println("No goalroom solution found");
			} else {
				for(int i=0; i<goalRoomSolutionPath.length; i+=2) {
				    System.out.println( "Push "   + i/2
				    				  + ": from " + goalRoomSolutionPath[i]
				    				  + " to "    + goalRoomSolutionPath[i+1] );
				}
			}
		}


		// Der Corralerzwinger selbst soll nicht zum Zielfeldraum zählen.
		// Innerhalb der Solver werden alle Kisten innerhalb des Zielfeldraumes ignoriert (weil sie
		// automatisch auf dem hier gefundenen Lösungspfad verschoben werden). Eine Kiste auf dem
		// Corralerzwinger darf aber nicht ignoriert werden, da sie nicht immer sofort auch tatsächlich
		// in den Zielfeldraum verschoben werden kann!
		goalRoom[goalRoomEntrancePosition] = false;

		// Falls der Raum eine Lösung hat wird auch der Raum selber global gespeichert.
		if(goalRoomSolutionPath != null) {
			goalRoomSquares = goalRoom;
		}
	}


	/**
	 * Debug method:
	 * Displays the path to the current board position.
	 *
	 * @param currentBoardPosition	the board position whose path is to be displayed.
	 */
	protected void debugShowPathToCurrentBoardPosition(IBoardPosition currentBoardPosition) {

		// Clear the flag for the next run.
		Debug.isDisplayPathToCurrentBoardPositionActivated = false;

		// Array holding all pushes of the current board position.
		ArrayList<IBoardPosition> pushes = new ArrayList<IBoardPosition>(currentBoardPosition.getPushesCount());

		// Add all board positions to an array.
		for(; currentBoardPosition.getPrecedingBoardPosition() !=  null; currentBoardPosition = currentBoardPosition.getPrecedingBoardPosition()) {
			pushes.add(0, currentBoardPosition);
		}

		// Save the initial board position of the level.
		IBoardPosition initialBoardPosition = currentBoardPosition;

		// Set the initial board position and display it.
		board.setBoardPosition(initialBoardPosition);
		application.redraw(true);

		// Loop over all board positions that lead to the current board position.
		for(ListIterator<IBoardPosition> i = pushes.listIterator();i.hasNext();) {
			currentBoardPosition = i.next();

			// Get the number of the pushed box.
			int pushedBoxNo = currentBoardPosition.getBoxNo();

			// If it's a dummy board position jump over it (see "linkPaths")
			if(pushedBoxNo == NO_BOX_PUSHED) {
				continue;
			}

			int boxPosition       = currentBoardPosition.getPositions()[pushedBoxNo];
			int direction         = currentBoardPosition.getDirection();
			int boxTargetPosition = 0;
			int boxStartPosition  = 0;

			// Determine box position before the push
			boxTargetPosition = boxPosition;
			boxStartPosition  = boxPosition - offset[direction];

			// Determine the player path to the square next to the box.
			int[] playerPath = board.playerPath.getPathTo(2*boxStartPosition-boxTargetPosition);

			// Display all player moves.
			for(int moveNo = 1; moveNo < playerPath.length; moveNo++) {

				board.setPlayerPosition(playerPath[moveNo]);

				// Determine and set view direction of the player
				application.applicationGUI.mainBoardDisplay.setViewDirection(
						board.getMoveDirection(playerPath[moveNo-1], playerPath[moveNo])
				);

				// Draw new game status
				application.redraw(false);

				if(moveNo < playerPath.length-1) {
					try {
						Thread.sleep( Math.max(0, Settings.delayValue - playerPath.length/10) );
					} catch(InterruptedException e) {}
				}
			}

			// Do the push.
			board.pushBox(boxStartPosition, boxTargetPosition);
			board.setPlayerPosition(boxStartPosition);
			application.redraw(true);
		}
	}

	/**
	 * Returns whether the solver has been stopped due to insufficient memory.
	 *
	 * @return <code>true</code> if the solver stopped due to insufficient memory, and
	 *        <code>false</code> otherwise
	 */
	public boolean isSolverStoppedDueToOutOfMemory() {
		return isSolverStoppedDueToOutOfMemory;
	}

	/**
	 * Draws the board to the GUI and waits until the board has been drawn.
	 * <p>
	 * This methods waits until the board is completely drawn because it might
	 * be called from a thread that would change the board while it is drawn.
	 */
	protected void displayBoard() {
		try {
			SwingUtilities.invokeAndWait(new Runnable() {
				@Override
				public void run() {
					application.applicationGUI.paintImmediately(0, 0, application.applicationGUI.getWidth(), application.applicationGUI.getHeight());
				}
			});
		} catch (Exception e) { /* nothing to be done */ }
	}

	/**
	 * Gives the name of this solver as used to tag new solutions.
	 * @return the name of this solver
	 */
	protected String creatorName() {
		return Texts.getText("solver");
	}

	/**
	 * Returns a string to be attached to a solution, which says that this
	 * solver did create the solution at the passed point in time.
	 *
	 * @param date the creation time point, or <code>null</code> for "now"
	 * @return string identifying the solver as solution creator
	 * @see Solution#name
	 */
	protected String solutionByMeAt(Date date) {
		return  Texts.getText("createdBy")
		      + " " + creatorName()
		      + " " + Utilities.dateString(date);
	}

	/**
	 * Returns a string to be attached to a solution, which says that this
	 * solver did create the solution "now".
	 *
	 * @return string identifying the solver as solution creator
	 * @see Solution#name
	 */
	protected String solutionByMeNow() {
		return solutionByMeAt(null);
	}
}