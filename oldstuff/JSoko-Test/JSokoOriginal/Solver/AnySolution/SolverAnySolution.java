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
package de.sokoban_online.jsoko.solver.AnySolution;

import java.util.ArrayList;
import java.util.ListIterator;
import java.util.PriorityQueue;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.solverAnySolution.AbsoluteBoardPositionSolverAnySolution;
import de.sokoban_online.jsoko.boardpositions.solverAnySolution.IBoardPositionSolverAnySolution;
import de.sokoban_online.jsoko.boardpositions.solverAnySolution.RelativeBoardPositionSolverAnySolution;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.solver.Solver;
import de.sokoban_online.jsoko.solver.SolverGUI;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class implements a solver that tries to find a solution for a level - no matter what solution.
 * Hence, the solutions found needn't to be optimal regarding moves and pushes.
 */
public final class SolverAnySolution extends Solver
		implements DirectionConstants
{

	// PriorityQueue for the board positions to be analyzed.
	private PriorityQueue<IBoardPositionSolverAnySolution> boardPositionsToBeAnalyzedForward;

	// The last board position of the found solution.
	IBoardPositionSolverAnySolution solutionBoardPosition;

	// The found packing sequence for the current level.
	ArrayList<BoardPositionPackingSequence> packingSequence;

	// orderPushedBoxes[2] = 6 means: the box having box number 2 has been pushed the first time at
	// index 6 of the packing sequence.
	int[] orderPushedBoxes;


	/**
	 * Creates an instance of this class.
	 *
	 * @param application  Reference to the main object
	 * @param solverGUI reference to the GUI of this solver
	 */
	public SolverAnySolution(JSoko application, SolverGUI solverGUI) {
		super(application, solverGUI);

		// Create a priority queue for storing the board positions.
		boardPositionsToBeAnalyzedForward = new PriorityQueue<IBoardPositionSolverAnySolution>(100000);
	}

	/**
	 * This method controls the search for a solution of the current level.
	 */
	@Override
	public Solution searchSolution() {

		// Flag, indicating whether a solution has been found.
		boolean isSolutionFound = false;

		// Number of no-deadlock board positions that have been reached during the search.
		boardPositionsCount = 0;

		// isBoxAtPackingSequenceSquare[1] = true means the box having number 1 is controlled by the packing sequence routine.
		boolean[] isBoxControlledByPackingSequence = new boolean[board.boxCount];

		// Create an object of the current board position.
		IBoardPositionSolverAnySolution currentBoardPosition = new AbsoluteBoardPositionSolverAnySolution(board, isBoxControlledByPackingSequence);

		// During the search the board position is changed. Hence, the initial board position is backed up.
		AbsoluteBoardPositionMoves startBoardPosition = new AbsoluteBoardPositionMoves(board);

		// Just to be sure that there are no frozen boxes from previous searches anymore.
		board.boxData.setAllBoxesNotFrozen();

		// If the level is unsolvable display a message and exit.
		if(deadlockDetection.isDeadlock()) {
			publish(Texts.getText("levelunsolvable"));
			return null;
		}

		// Save the current board position in the hash table for detecting duplicate board positions.
		// There exist levels that start in a solved state. Those levels are a special kind of levels.
		// Hence -> search for a solution even if the level is already solved and don't mark the current
		// board position as already visited in that case.
		if(!board.boxData.isEveryBoxOnAGoal()) {
			currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);
			positionStorage.storeBoardPosition(currentBoardPosition);
		}

		// Depending on the level either a packing sequence search is done or the normal "any solution" search.
		if(isPackingSequenceLevel()) {

			// Display a text so the user is informed that a packing sequence is searched.
			publish(Texts.getText("solver.searchingPackingSequence"));

			// Search for a packing sequence.
			PackingSequenceSearch packingSequenceSearch = new PackingSequenceSearch(board, solverGUI, this);
			packingSequenceSearch.debugSearchPackingSequence();
			packingSequence  = packingSequenceSearch.getPackingSequence();
			orderPushedBoxes = packingSequenceSearch.getOrderPushedBoxes();
		}

		// Display an info because the search now starts.
		publish(Texts.getText("solver.searchingSolution"));

		// Take the current board position as start for the search.
		boardPositionsToBeAnalyzedForward.add(currentBoardPosition);

		// The initial board position is counted, too.
		boardPositionsCount++;

		// Remember the start time.
		long startTimeStamp = System.currentTimeMillis();

		// Search a solution doing a packing sequence search.
		if(packingSequence != null) {

		    // Display the packing order.
			if(Debug.isDebugModeActivated) {
				application.applicationGUI.mainBoardDisplay.repaint();
			}

			isSolutionFound = forwardSearchWithPackingSequence();
		}

		// If the packing sequence search couldn't find a solution then try a normal search.
		if(isSolutionFound == false && isCancelled() == false) {

			// If the packing sequence search has been performed the storage has to be cleared and the
			// start board position has to be added to the queue.
			if(packingSequence != null) {
				positionStorage.clear();
				boardPositionsToBeAnalyzedForward.add(currentBoardPosition);
			}

			// Search for a solution.
			isSolutionFound = forwardSearch();
		}

		// Display information about the result of the search.
		if(isSolutionFound == true) {
			publish(Texts.getText("solved") +
					Texts.getText("pushes") + ": " + solutionBoardPosition.getPushesCount());
		} else {
			publish(Texts.getText("solver.noSolutionFound"));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("===================================");
			System.out.println("Solution found: " + isSolutionFound);
			if(isSolutionFound == true) {
				System.out.println("Number of pushes: "+solutionBoardPosition.getPushesCount());
			}
			System.out.println("No Deadlockpositions: "+boardPositionsCount);
			System.out.println("Total positions: "+positionStorage.getNumberOfStoredBoardPositions());
			System.out.println("Time for search: "+(System.currentTimeMillis()-startTimeStamp));
			deadlockDetection.corralDeadlockDetection.debugShowStatistic();
		}

		// Clear the hash table.
		positionStorage.clear();

		// Restore the start board position.
		board.setBoardPosition(startBoardPosition);

		// If no solution has been found exit the solver.
		if(isSolutionFound == false) {
			return null;
		}

		// ArrayList holding all pushes of the solution.
		ArrayList<IBoardPosition> pushes = new ArrayList<IBoardPosition>(solutionBoardPosition.getPushesCount());

		// Add the board positions of the solution in the correct order.
		for(IBoardPosition boardPosition = solutionBoardPosition; boardPosition.getPrecedingBoardPosition() !=  null; boardPosition = boardPosition.getPrecedingBoardPosition()) {
			if(boardPosition.getBoxNo() != NO_BOX_PUSHED) {
				pushes.add(0, boardPosition);
			}
		}

		// Remember the current index of the history. All movements of the solution are added to the history and then the index
		// is set back to this value in order to have all solution movements "in the future" of the history.
		int currentIndex = application.movesHistory.getCurrentMovementNo();

		// Add all movements to the history.
		for(ListIterator<IBoardPosition> i = pushes.listIterator();i.hasNext();) {
			currentBoardPosition = (RelativeBoardPositionSolverAnySolution) i.next();

			int pushedBoxNo = currentBoardPosition.getBoxNo();
			int direction   = currentBoardPosition.getDirection();
			application.movesHistory.addMovement(direction, pushedBoxNo);
		}

		// Set the index of the history back to the remembered value in order to allow the user to use the redo functionality.
		application.movesHistory.setMovementNo(currentIndex);

		// Set back the initial board position.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			board.removeBox(position);
		}
		board.setBoardPosition(startBoardPosition);

		// Debug: Show a statistic about the hash table.
		if(Debug.debugShowHashTableStatistic) {
			positionStorage.printStatisticDebug();
		}

		// Optimize the solution a little bit. Thereby the player movements are added to the history, too.
		optimizeSolution();

		// Create the new solution.
		Solution newSolution = new Solution(application.movesHistory.getLURDFromHistoryTotal());
		newSolution.name = solutionByMeNow();

		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			board.removeBox(position);
		}
		board.setBoardPosition(startBoardPosition);

		// Exit the search.
		return newSolution;
	}


	/**
	 * Tries to solve the level by generating all possible no-deadlock board positions and
	 * returns the solution path via a global variable.
	 */
	protected final boolean forwardSearchWithPackingSequence() {

		// Hold a box position and the new box position.
		int boxPosition;
		int newBoxPosition = 0;

		// Object for the current board position
		IBoardPositionSolverAnySolution currentBoardPosition;

		// The board position to be analyzed further.
		IBoardPositionSolverAnySolution boardPositionToBeAnalyzed;

		// isBoxAtPackingSequenceSquare[1] = true means the box having number 1 is controlled by the packing sequence routine.
		boolean[] isBoxControlledByPackingSequence;

		// If a board position is reached that has been reached before this object holds
		// the board position that has been reached before.
		IBoardPosition oldBoardPosition;

		// Lowerbound of a board position.
		int currentBoardPositionLowerbound = 0;

		// Number of the pushed box.
		int pushedBoxNo = 0;

		// Index in the packing sequence. A index of 5 means that already 5 steps of the packing sequence have been played.
		int currentPackingSequenceIndex = 0;

		// Pushes of the current board position.
//		int currentBoardPositionPushesCount = 0;

		// A set bit means the box represented by this bit is relevant for the next push.
		boolean[] relevantBoxes = null;

		// Flag indicating whether the last pushed box is in a tunnel.
		boolean isLastPushedBoxInATunnel = false;


		// The board position with the highest relevance is taken as basis board position for generating successors.
		while((boardPositionToBeAnalyzed = boardPositionsToBeAnalyzedForward.poll()) != null && isCancelled() == false) {

			// Set the board position.
			board.setBoardPosition(boardPositionToBeAnalyzed);

			// Only for debugging: show board positions.
			if(solverGUI.isShowBoardPositionsActivated.isSelected()) {
				displayBoard();
			}

			// Calculate the number of pushes of the new board positions that are created.
//			currentBoardPositionPushesCount = boardPositionToBeAnalyzed.getPushesCount() + 1;

			// Get the information which boxes are under the control of the packing sequence. These boxes may only be moved
			// by the packing sequence method.
			isBoxControlledByPackingSequence = boardPositionToBeAnalyzed.getPackingSequenceControlledBoxInformation();

			// If there is a packing order then use it.
			if(packingSequence != null) {
				boolean isPackingSequencePushDone = pushBoxPackingSequence(boardPositionToBeAnalyzed, isBoxControlledByPackingSequence);
				if(solutionBoardPosition != null) {
					return true;
				}
				if(isPackingSequencePushDone == true) {
					//TODO: auskommentieren und weiter suchen. aber dafür muss sicher sein,
					// das die packing sequence pushes genau das board zurück geben, wie reingegeben wurde
					continue;
				}
			}

			// Set the board position.
			board.setBoardPosition(boardPositionToBeAnalyzed);

			// Determine the reachable squares of the player. These squares are used even after
			// the deadlock detection, hence they are calculated in an extra object.
			playersReachableSquares.update();

			// Get number of the last pushed box.
			pushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

			// Get the information how many steps of the packing sequence have already been played.
			currentPackingSequenceIndex = boardPositionToBeAnalyzed.getIndexPackingSequence();

			// Identify the relevant boxes for the next push.
			relevantBoxes = identifyRelevantBoxes();

			// If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
			if(pushedBoxNo != NO_BOX_PUSHED) {
				isLastPushedBoxInATunnel = isBoxInATunnel(pushedBoxNo, boardPositionToBeAnalyzed.getDirection());
			} else {
				isLastPushedBoxInATunnel = false;
			}

			// Loop over all boxes. The last pushed box is considered first.
			for(int boxNo=0; boxNo < board.boxCount; boxNo++) {

				// Don't consider boxes that are under the control of the packing sequence. Those boxes are handled in
				// the packing sequence method.
				if(isBoxControlledByPackingSequence[boxNo] == true) {
					continue;
				}

				// If the last pushed box is in a tunnel only pushes of this box have to be considered!
				if(isLastPushedBoxInATunnel == true && boxNo != pushedBoxNo) {
					continue;
				}

				// If there is no tunnel, check for an I-Corral. If it is an I-Corral only the relevant
				// boxes need to be considered for pushing.
				if(isLastPushedBoxInATunnel == false && relevantBoxes != null && relevantBoxes[boxNo] == false) {
					continue;
				}

				// Get the position of the box
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// Push the box to every direction possible.
				for(int direction = 0; direction < 4; direction++) {

					// Calculate the new box position.
					newBoxPosition = board.getPosition(boxPosition, direction);

					// Immediately continue with the next direction if the player can't reach the correct
					// position for pushing or the new box position isn't accessible.
					if(playersReachableSquares.isSquareReachable(boxPosition - offset[direction]) == false
							|| board.isAccessibleBox(newBoxPosition) == false) {
						continue;
					}

					// Do push.
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = boxPosition;

					// Create object of the current board position.
					currentBoardPosition = new RelativeBoardPositionSolverAnySolution(application, boxNo, direction, currentPackingSequenceIndex, isBoxControlledByPackingSequence, boardPositionToBeAnalyzed);

					// Try to read the current board position from the hash table.
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);


					//TODO: board position may have been reached with a better score = higher relevance this time?!
					// If the board position had already been saved in the hash table and has not been created during the corral
					// detection that this is a duplicate board position which can be discarded.
					if(oldBoardPosition != null) {
						board.pushBoxUndo(newBoxPosition, boxPosition);
						continue;
					}

					/*
					 * The board position hasn't already been in the hash table, hence it is a new one.
					 */
					// Calculate the lower bound of the current board position.
					currentBoardPositionLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

					// Undo push (the player is new positioned for the next board position anyway)
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Immediately continue with the next direction if the current board position is a deadlock.
					if(currentBoardPositionLowerbound == LowerBoundCalculation.DEADLOCK) {
						continue;
					}

					// Check if a solution has been found (no matter if it's an optimal one or not).
					if(currentBoardPositionLowerbound == 0) {
						solutionBoardPosition = currentBoardPosition;
						return true;
					}

					// Save the lower bound.
					currentBoardPosition.setPushesLowerBound(currentBoardPositionLowerbound);


					// Calculate the relevance of this board position for the search.
					int newRelevance = boardPositionToBeAnalyzed.getRelevance() - 400;

					// Bonus if the lower bound has decreased (the relevant lower bound is lower bound + pushesCount, therefore "-1").
					newRelevance += boardPositionToBeAnalyzed.getPushesLowerBound() - currentBoardPositionLowerbound - 1;

					// Penalty if a box has been pushed in the packing sequence (this can result in situations where boxes
					// needed later in the packing sequence aren't located where they are supposed to be, anymore):
					// (orderPushedBoxes[boxNo] = packingSequenceIndex at which the box has been pushed the first time in the packing sequence);
					//					newRelevance += 2000 * (currentPackingSequenceIndex - orderPushedBoxes[boxNo]);

					// Set the relevance value.
					currentBoardPosition.setRelevance(newRelevance);


					// Calculate the number of no deadlock board positions reached during the search.
					boardPositionsCount++;

					// Display info about the search (every 500 board positions)
					if((boardPositionsCount % 500) == 0) {
						publish(Texts.getText("numberofpositions")+boardPositionsCount);

						// Throw "out of memory" if less than 15MB RAM is free.
						if(Utilities.getMaxUsableRAMinMiB() <= 15) {
							isSolverStoppedDueToOutOfMemory = true;
							cancel(true);
						}
					}

					// Save the board position in the hash table and for further searching.
					positionStorage.storeBoardPosition(currentBoardPosition);
					boardPositionsToBeAnalyzedForward.add(currentBoardPosition);
				}
			}
		}
		return false;
	}

	/**
	 * Tries to solve the level by generating all possible no-deadlock board positions and
	 * returns the solution path via a global variable.
	 */
	protected final boolean forwardSearch() {

		// Hold a box position and the new box position.
		int boxPosition;
		int newBoxPosition = 0;

		// Object for the current board position
		IBoardPositionSolverAnySolution currentBoardPosition;

		// The board position to be analyzed further.
		IBoardPositionSolverAnySolution boardPositionToBeAnalyzed;

		// If a board position is reached that has been reached before this object holds
		// the board position that has been reached before.
		IBoardPosition oldBoardPosition;

		// Lowerbound of a board position.
		int currentBoardPositionLowerbound = 0;

		// Number of the pushed box.
		int pushedBoxNo = 0;

	    // A set bit means the box represented by this bit is relevant for the next push.
		boolean[] relevantBoxes = null;

		// Number of pushes of the board position.
		int currentBoardPositionPushesCount = 0;

		// The board position with the highest relevance value is taken as basis board position for generating successors.
		while((boardPositionToBeAnalyzed = boardPositionsToBeAnalyzedForward.poll()) != null && isCancelled() == false) {

			// Set the board position.
			board.setBoardPosition(boardPositionToBeAnalyzed);

			// Only for debugging: show board positions.
			if(solverGUI.isShowBoardPositionsActivated.isSelected()) {
				displayBoard();
			}

			// Determine the reachable squares of the player. These squares are used even after
			// the deadlock detection, hence they are calculated in an extra object.
			playersReachableSquares.update();

			// Get number of the last pushed box.
			pushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

			// Calculate the number of pushes of the new board positions that are created.
			currentBoardPositionPushesCount = boardPositionToBeAnalyzed.getPushesCount() + 1;

			// Identify the relevant boxes for the next push.
			relevantBoxes = identifyRelevantBoxes();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
				pushedBoxNo = -1;
			}

			// Loop over all boxes. The last pushed box is considered first.
			for(int boxCounter = -1, boxNo; boxCounter < board.boxCount; boxCounter++) {

				// The last pushed box has already been processed (-> boxCounter = -1)
				if(boxCounter == pushedBoxNo) {
					continue;
				}

				// The last pushed box is considered first. It is checked for being in a tunnel.
				if(boxCounter == -1) {
					boxNo = pushedBoxNo;

					// If the box is in a tunnel only pushes of this box have to be considered!
					if(isBoxInATunnel(pushedBoxNo, boardPositionToBeAnalyzed.getDirection())) {
						boxCounter = board.goalsCount;
					}
				} else {
					boxNo = boxCounter;
				}

				// If there is no tunnel, check for an I-Corral. If it is an I-Corral only the relevant
				// boxes need to be considered for pushing.
				if(boxCounter < board.boxCount && relevantBoxes != null && relevantBoxes[boxNo] == false) {
					continue;
				}

				// Get the position of the box
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// Push the box to every direction possible.
				for(int direction = 0; direction < 4; direction++) {

					// Calculate the new box position.
					newBoxPosition = board.getPosition(boxPosition, direction);

					// Immediately continue with the next direction if the player can't reach the correct
					// position for pushing or the new box position isn't accessible.
					if(playersReachableSquares.isSquareReachable(boxPosition - offset[direction]) == false
							|| board.isAccessibleBox(newBoxPosition) == false) {
						continue;
					}

					// Do push.
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = boxPosition;

					// Create object of the current board position.
					currentBoardPosition = new RelativeBoardPositionSolverAnySolution(application, boxNo, direction, -1, null, boardPositionToBeAnalyzed);

					// Try to read the current board position from the hash table.
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// If the board position had already been saved in the hash table and has not been created during the corral
					// detection that this is a duplicate board position which can be discarded.
					if(oldBoardPosition != null) {
						board.pushBoxUndo(newBoxPosition, boxPosition);
						continue;
					}

					/*
					 * The board position hasn't already been in the hash table, hence it is a new one.
					 */
					// Calculate the lower bound of the current board position.
					currentBoardPositionLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

					// Undo push (the player is new positioned for the next board position anyway)
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Immediately continue with the next direction if the current board position is a deadlock.
					if(currentBoardPositionLowerbound == LowerBoundCalculation.DEADLOCK) {
						continue;
					}

					// Check if a solution has been found (no matter if it's an optimal one or not).
					if(currentBoardPositionLowerbound == 0) {
						solutionBoardPosition = currentBoardPosition;
						return true;
					}

					// Set the relevance of this board position for the search.
					currentBoardPosition.setRelevance(-5000*currentBoardPositionLowerbound+5*currentBoardPositionPushesCount);

					// Calculate the number of no deadlock board positions reached during the search.
					boardPositionsCount++;

					// Display info about the search (every 500 board positions)
					if((boardPositionsCount%500) == 0) {
						publish(Texts.getText("numberofpositions")+boardPositionsCount+", "+Texts.getText("solutionDistance")+" = "+currentBoardPositionLowerbound);

						// Throw "out of memory" if less than 15MB RAM is free.
						if(Utilities.getMaxUsableRAMinMiB() <= 15) {
							isSolverStoppedDueToOutOfMemory = true;
							cancel(true);
						}
					}

					// Save the board position in the hashtable and for further searching.
					positionStorage.storeBoardPosition(currentBoardPosition);
					boardPositionsToBeAnalyzedForward.add(currentBoardPosition);
				}
			}
		}
		return false;
	}

	/**
	 * Returns whether the box corresponding to the passed box number is in a tunnel.
	 * If this is the case only pushes of this box are relevant for the current board position.
	 * <p>
	 * This is a copy of the method from the A*-Solver. This solver needn't to preserve push optimality,
	 * therefore it may use another version in the future.
	 *
	 * @param boxNo the number of the box
	 * @param pushDirection	the direction the box has been pushed to
	 * @return	<code>true</code>, if the box is in a tunnel, and
	 * 		   <code>false</code>  if the box is not in a tunnel
	 */
	private boolean isBoxInATunnel(int boxNo, int pushDirection){

		// New box position.
		int newBoxPosition;

		// Get current box position
		int boxPosition = board.boxData.getBoxPosition(boxNo);

		// Determine the opposite axis of the push direction
		int oppositeAxis = UP;
		if(pushDirection==UP || pushDirection == DOWN) {
			oppositeAxis = RIGHT;
		}

		/* First let's get the following tunnels (and rotated versions):
		 *
		 * #$    #$#    $#
		 * #@#   #@#   #@#
		 */
		if(board.isGoal(boxPosition) == false &&
				board.isWall(boxPosition-offset[pushDirection]+offset[oppositeAxis]) &&
				board.isWall(boxPosition-offset[pushDirection]-offset[oppositeAxis]) &&
				(board.isWall(boxPosition+offset[oppositeAxis]) || board.isWall(boxPosition-offset[oppositeAxis]))) {
			return true;
		}


		/*
		 * Prüfen, ob durch Verschieben der Kiste nur Sicherheitsstellungen erzeugt werden können
		 * oder ob die Kiste überhaupt nur in eine Richtung verschoben werden kann.
		 *
		 * Sicherheitsstellung =
		 *            #
		 *   #$# oder $
		 *            #
		 */
		// Identify the reachable squares of the player assumed only the relevant box is present on the board.
		board.setWall(boxPosition);
		board.playersReachableSquaresOnlyWalls.update();
		board.removeWall(boxPosition);

		// The number of directions the box is pushable to.
		byte pushableDirectionsCount = 0;

		// Number of safe positions.  Safe positions are:
		//            #
		//   #$# and  $
		//            #
		byte safeSituationsCount = 0;

		// Number of directions the box can be pushed to when all other boxes are on the board, too.
		byte reallyPushableDirectionsCount = 0;

		// Number of squares the box can be pushed to that weren't reachable for the player before the push (= squares in a corral).
		byte squaresInCorralCount = 0;

		// Number of free neighbor squares.
		byte notWallNeighborSquaresCount = 0;

		// Try to push the box to every direction.
		for(byte direction=0; direction<4; direction++) {

			// Calculate the new box position after the push.
			newBoxPosition = boxPosition + offset[direction];

			// Calculate the free neighbor squares of the box.
			if(board.isWall(newBoxPosition) == false) {
				notWallNeighborSquaresCount++;
			}

			// Check if the player can reach the square for pushing the box and the other side is empty (= no wall,
			// because the other boxes are still there!).
			// If this isn't the case jump to the next direction immediately.
			if(board.isWall(newBoxPosition) || board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition - offset[direction]) == false) {
				continue;
			}

			// The box can be pushed to the new position -> increase the counter.
			pushableDirectionsCount++;

			// Check if the box can be pushed even when the other boxes are assumed to be on the board and set the counter accordingly.
			if(playersReachableSquares.isSquareReachable(boxPosition - offset[direction])) {
				reallyPushableDirectionsCount++;
			}

			// If the player couldn't reach the new box position even if only the relevant box is on the board it's a corral square.
			if(board.playersReachableSquaresOnlyWalls.isSquareReachable(newBoxPosition) == false) {
				squaresInCorralCount++;
			}

			// Check if a safe position has been reached.
			// (Independent of the push direction always both axis can be checked)
			if(board.isWall(newBoxPosition+offset[UP]) && board.isWall(newBoxPosition+offset[DOWN])
					|| board.isWall(newBoxPosition-1) && board.isWall(newBoxPosition+1)) {
				safeSituationsCount++;
			}
		}

		if(
				// "Normal" tunnel: 2 pushable directions, both are safe situations,
				// only 2 free neighbor squares.  Example:
				//    # #
				//    #$#
				//    #@#
				pushableDirectionsCount == 2 && safeSituationsCount == 2 &&
				notWallNeighborSquaresCount == 2
			||

				// The box can only be pushed to safe positions and all of these pushes can be done at the moment -> tunnel.
				pushableDirectionsCount > 0 && pushableDirectionsCount == safeSituationsCount
				&& pushableDirectionsCount == reallyPushableDirectionsCount
			||

				// All possible pushes of the box end on corral squares. This is a tunnel if all of these pushes can be done
				// at the moment and there is at least on push can be done.
				pushableDirectionsCount == squaresInCorralCount &&
				pushableDirectionsCount == reallyPushableDirectionsCount &&
				reallyPushableDirectionsCount > 0) {

			// The box is in a tunnel. However, if the box is located on a goal the box is only in a tunnel if
			// all pushes end on a corral square and in at least on of the corrals there is a goal without a box.
			// Example tunnel:
			//   ####
			//  @$..#
			//   ####
			if(board.isGoal(boxPosition)) {
				if(pushableDirectionsCount == squaresInCorralCount) {
					for(int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
						if(board.playersReachableSquaresOnlyWalls.isSquareReachable(position) == false &&
								board.isGoal(position) && board.isBox(position) == false) {
							return true;
						}
					}
				}

				// Box is in a tunnel, but on a goal square. Therefore it isn't in a tunnel after all.
				return false;
			}

			// The box is in a tunnel.
			return true;
		}

		// The box isn't in a tunnel.
		return false;
	}


	/**
	 * Returns whether the level is a level for using the packing sequence algorithm.
	 * <p>.
	 * This method counts the "connected goals". The definition of "connected goal" is:
	 * <br>1. The goal has one or more neighboring goal squares
     * <br>2. When the neighboring goal square is located at one axis, there must be a neighboring square along the other axis, which either is a goal too, or a wall square
	 *
	 *  If the number of connected goals is higher than 8 the level is classified as packing sequence level.
	 *
	 * @return
	 */
	private boolean isPackingSequenceLevel() {

		int goalPosition = 0;
		int neighborSquareUp 	= 0;
		int neighborSquareDown  = 0;
		int neighborSquareLeft  = 0;
		int neighborSquareRight = 0;
		int relevantGoalsCount  = 0;

		// Count the goals having neighboring goals and/or neighboring walls.
		for(int goalNo=0; goalNo < board.goalsCount; goalNo++) {

			// Get the position of the goal.
			goalPosition = board.getGoalPosition(goalNo);

			// Get the positions of the squares above, under, left to and right to the goal.
			neighborSquareUp 	= board.getPosition(goalPosition, UP);
			neighborSquareDown  = board.getPosition(goalPosition, DOWN);
			neighborSquareLeft  = board.getPosition(goalPosition, LEFT);
			neighborSquareRight = board.getPosition(goalPosition, RIGHT);

			if((board.isGoal(neighborSquareUp) || board.isGoal(neighborSquareDown)) &&
			 ( board.isGoal(neighborSquareLeft) || board.isGoal(neighborSquareRight) || board.isWall(neighborSquareLeft) || board.isWall(neighborSquareRight))
			 ||
			 (board.isGoal(neighborSquareLeft) || board.isGoal(neighborSquareRight)) &&
			 (board.isGoal(neighborSquareUp) || board.isGoal(neighborSquareDown) || board.isWall(neighborSquareUp) || board.isWall(neighborSquareDown))) {
			 relevantGoalsCount++;
			}
		}

		return relevantGoalsCount > 8;
	}

	/**
	 * This method checks whether any of the boxes can be pushed to a square needed for the correct packing sequence.
	 * <p>
	 * The needed packing sequence has been found by a separate search before the solver has been started. This sequence
	 * is treated as to be the correct one (although this isn't sure).
	 *
	 * @param boardPositionToBeAnalyzed	current board position taken out of the open queue
	 * @param isBoxControlledByPackingSequence  boolean array indicating which box is under the control of the packing sequence
	 * @return <code>true</code> a packing sequence push has been done
	 *            <code>false</code> no packing sequence push could be done
	 */
	private boolean pushBoxPackingSequence(IBoardPositionSolverAnySolution boardPositionToBeAnalyzed, boolean[] isBoxControlledByPackingSequence) {

		// Index in the packing order.
		int currentPackingSequenceIndex = 0;

		// Start position of the next box to push in the packing sequence.
		int startBoxPosition = 0;

		// Target position of the next box to push in the packing sequence.
		int targetBoxPosition = 0;

		// Current board position.
		IBoardPositionSolverAnySolution currentBoardPosition = null;

		// Current box position.
		int currentBoxPosition = 0;

		// Flag indicating whether at least one "no deadlock" packing sequence push has been played.
		boolean isPackingSequenceBeenPlayed = false;

		// Backup of the player position and a box position.
		int playerPositionBackup = 0;
		int boxPositionBackup    = 0;

		// The "which box is under control of the packing sequence" information for the current board position.
		boolean[] isBoxControlledByPackingSequenceCurrentBoardPosition;

		// The box path from one square to another square.
		ArrayList<Integer> boxPath;


		// Get the information how many steps of the packing sequence have already been played.
		currentPackingSequenceIndex = boardPositionToBeAnalyzed.getIndexPackingSequence();

		// Get the next step of the packing sequence.
		BoardPositionPackingSequence packingSequenceStep = packingSequence.get(currentPackingSequenceIndex+1);

		// Get the start and target position of the next box to push.
		startBoxPosition  = packingSequenceStep.getStartBoxPosition();
		targetBoxPosition = packingSequenceStep.getTargetBoxPosition();



		playerPositionBackup = board.playerPosition;

		// Try to push every box to the needed target position except those boxes that are already under the control of the
		// packing sequence. The boxes under control are either already on their goals or they are parked and may only be
		// moved by the packing sequence.
		for(int boxNo=0; boxNo<board.boxCount; boxNo++) {

			// Get current box position.
			currentBoxPosition = board.boxData.getBoxPosition(boxNo);

			// If the push is "forced", that means only the box at the specific start position can be pushed to the target position.
			// If the push isn't forced any box from any position can be pushed to the target position.
			if(packingSequenceStep.isForcedPush() && currentBoxPosition != startBoxPosition) {
				continue;
			}

			// Boxes under the control of the packing sequence may only be pushed by "forced" pushes. This means: once a box has been pushed by using
			// the packing sequence it is only be pushed by the packing sequence from then on.
			if(isBoxControlledByPackingSequence[boxNo] == true && packingSequenceStep.isForcedPush() == false) {
				continue;
			}

			// Backup the box and the player position.
			playerPositionBackup = board.playerPosition;
			boxPositionBackup    = currentBoxPosition;

			// Get the box path to the target position . If there isn't one continue with the next box.
			boxPath = application.getBoxPath(currentBoxPosition, targetBoxPosition);
			if(boxPath.size() == 0) {
				continue;
			}

			// The initial board position is the passed one.
			currentBoardPosition = boardPositionToBeAnalyzed;

			// The current board position has the same boxes under control + the just pushed box.
			isBoxControlledByPackingSequenceCurrentBoardPosition = isBoxControlledByPackingSequence.clone();
			isBoxControlledByPackingSequenceCurrentBoardPosition[boxNo] = true;

			for(int newBoxPosition : boxPath) {

				// Do the push on the board.
				board.pushBox(currentBoxPosition, newBoxPosition);
				board.playerPosition = currentBoxPosition;

				// Calculate the direction of the push.
				int movementVector = newBoxPosition - currentBoxPosition;
				int pushDirection = 0;
				for(; pushDirection < 4; pushDirection++) {
					if(movementVector == board.offset[pushDirection]) {
						break;
					}
				}

				// Create a new board position from the board.
				currentBoardPosition = new RelativeBoardPositionSolverAnySolution(application, boxNo, pushDirection, currentPackingSequenceIndex+1, isBoxControlledByPackingSequenceCurrentBoardPosition, currentBoardPosition);

				// Set the current board position as new start box position.
				currentBoxPosition = newBoxPosition;
			}

			// Calculate the lower bound of the new board position. If deadlock -> discard board position.
			int pushesLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(targetBoxPosition);

			// If it is a deadlock then undo the push and continue with the next box.
			if(pushesLowerbound == LowerBoundCalculation.DEADLOCK) {
				// Undo the pushes.
				board.pushBox(currentBoxPosition, boxPositionBackup);
				board.setPlayerPosition(playerPositionBackup);
				continue;
			}

			// Save the lower bound.
			currentBoardPosition.setPushesLowerBound(pushesLowerbound);


			// The board position gets a higher relevance as the one take out of the open queue. Hence, it is taken out of the queue next.
			int newRelevance = boardPositionToBeAnalyzed.getRelevance()+800;

			// Set the relevance value for the board position.
			currentBoardPosition.setRelevance(newRelevance);

			// Add the board position to the open queue.
			boardPositionsToBeAnalyzedForward.add(currentBoardPosition);

			// Update the board positions counter.
			boardPositionsCount++;

			// Check whether the level has been solved.
			if(board.boxData.isEveryBoxOnAGoal()) {
				solutionBoardPosition = currentBoardPosition;
				return true;
			}

			// Save that the packing sequence has been used to do pushes.
			isPackingSequenceBeenPlayed = true;

			// Undo the pushes.
			board.pushBox(currentBoxPosition, boxPositionBackup);
			board.setPlayerPosition(playerPositionBackup);
		}

		// If at least one packing sequence push could be played, "true" must be returned so the added board positions can
		// be polled from the open queue depending on their relevance. If there couldn't be made any packing sequence
		// push false must be returned to ensure the solver uses the "normal" logic outside the packing sequence to search
		// for board positions where packing sequence pushes can be made from.
		return isPackingSequenceBeenPlayed;
	}
}
