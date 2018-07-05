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
import java.util.Arrays;
import java.util.Hashtable;
import java.util.PriorityQueue;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.deadlockdetection.BipartiteMatchings;
import de.sokoban_online.jsoko.gui.MainBoardDisplay;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.solver.Solver;
import de.sokoban_online.jsoko.solver.SolverGUI;
import de.sokoban_online.jsoko.utilities.Debug;



/**
 * Class for searching a packing order sequence to fill goal areas in a level.
 * A packing sequence determines in which order the goals have to be filled
 * and where to park boxes.
 */
public class PackingSequenceSearch {

	/** PriorityQueue for the board positions to be analyzed. */
	private PriorityQueue<BoardPositionPackingSequence> boardPositionsToBeAnalyzed;

	/** Direct reference to the board of the current loaded level. */
	Board board;

	/** GUI for the solver. */
	SolverGUI solverGUI;

	/** The <code>SwingWorker</code> thread that uses this object. */
	Solver callingThread;

	/**
	 * Positions of the backwards goals. In the backwards search the goals
	 * are the box positions in the initial board position of the level.
	 */
	private int[] backwardsGoalsPositions;

	/** Object for identifying the reachable player squares. */
	private Board.PlayersReachableSquares reachablePlayerSquares;

	/**
	 * Last board position of the packing sequence
	 * (or null if no packing sequence has been found).
	 */
	private BoardPositionPackingSequence lastBoardPositionPackingSequence;

	/** Hash table for storing the board positions. */
	private Hashtable<BoardPositionPackingSequence, BoardPositionPackingSequence> boardPositionStorage;

	/** The found packing sequence. */
	private ArrayList<BoardPositionPackingSequence> packingSequence;

	/**
	 * Box order: the order in which the boxes are pushed the first time.
	 * orderPushesBoxes[3] = 7 means: the box with number 3 has been pushed
	 * the first time at push number 7 in the packing sequence.
	 */
	private int[] orderPushedBoxes;

	/**
	 * boxPositionReachableByOtherBoxesCount[2] = 3 means: the position box 2
	 * is located at can be reached by 3 other boxes.
	 * The "reached" status here means: presumed that the player is at his
	 * start position in the level and there is only one box on the board
	 * at the same time.
	 */
	private int[] boxPositionReachableByOtherBoxesCount;


	/**
	 * Creates an instance to search a packing order.
	 *
	 * @param board  the board of the current level
	 * @param solverGUI reference to the GUI of this solver
	 * @param callingThread the <code>SwingWorker</code> that calls this constructor
	 */
	public PackingSequenceSearch(Board board, SolverGUI solverGUI, Solver callingThread) {

		// Create a priority queue for storing the board positions.
		boardPositionsToBeAnalyzed = new PriorityQueue<BoardPositionPackingSequence>(100000);

		// Direct reference to the board for easier access.
		this.board = board;

		// Save reference for detecting cancellation of calling thread.
		this.callingThread = callingThread;

		// Save a reference to the GUI.
		this.solverGUI = solverGUI;

		// Create and fill the array holding the positions of the backwards goals.
		backwardsGoalsPositions = new int[board.boxCount];
		for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
			backwardsGoalsPositions[boxNo] = board.boxData.getBoxPosition(boxNo);
		}

		// Create an array for storing the information which box has been
		// pushed the first time when in the packing sequence.
		orderPushedBoxes = new int[board.boxCount];

		// How many of the other boxes can reach the position of box x.
		boxPositionReachableByOtherBoxesCount = new int[board.boxCount];

		// Create an own object for identifying the reachable player squares.
		// (the global one in the object "board" is used in other method, too.
		//  Hence, the values may change in that object).
		reachablePlayerSquares = board.new PlayersReachableSquares();
	}

	/**
	 * DEBUG: this method controls the search for a packing sequence.
	 */
	public void debugSearchPackingSequence() {

		// During the search the board position is changed. Hence, the initial board position is saved.
		BoardPositionPackingSequence startBoardPosition = new BoardPositionPackingSequence(board);
		int startPlayerPosition = board.playerPosition;

		// Create a hash table for storing board positions.
		boardPositionStorage = new Hashtable<BoardPositionPackingSequence, BoardPositionPackingSequence>(100000);

		// Remove all boxes to create the start positions for the backwards search.
		board.removeAllBoxes();

		// Determine how many boxes can reach a specific box position.
		for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
			int boxPosition = board.boxData.getBoxPosition(boxNo);
			board.setBox(boxPosition);
			board.boxReachableSquares.markReachableSquares(boxPosition, false);
			board.removeBox(boxPosition);

			for(int boxNo2=0; boxNo2<board.boxCount; boxNo2++) {
				if(board.boxReachableSquares.isSquareReachable(board.boxData.getBoxPosition(boxNo2))) {
					boxPositionReachableByOtherBoxesCount[boxNo2]++;
				}
			}
		}

		// Create an array holding the packing order.
//	    application.applicationGUI.numbersToShow = new int[board.boardSize];
//	    for(int i2=0; i2<application.applicationGUI.numbersToShow.length; i2++)
//	    	application.applicationGUI.numbersToShow[i2] = -1;
//	    for(int boxNo2=0; boxNo2<board.boxCount; boxNo2++) {
//	    	application.applicationGUI.numbersToShow[board.boxData.getBoxPosition(boxNo2)] =boxPositionReachableByOtherBoxesCount[boxNo2];
//		}
//	    application.redraw(false);
//	    if(1==1) return;


		// Identify the reachable player squares.
		reachablePlayerSquares.update();

		// Set all boxes on a goal to create the start positions for the backwards search.
		for(int goalNo=0; goalNo < board.goalsCount; goalNo++) {

			// Get the position of the goal.
			int goalPosition = board.getGoalPosition(goalNo);

			// Set the corresponding box at this position (could also be order in another way, doesn't matter).
			board.boxData.setBoxPosition(board.getGoalNo(goalPosition), goalPosition);
			board.setBoxWithNo(board.getGoalNo(goalPosition), goalPosition);
		}

		// The level may be solved with the player in any of the reachable areas. Hence,
		// the player has to be placed to every area and the resulting board positions
		// have to be saved as start board positions for the backwards search.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare;position++) {

			// No box at that position and the position is "in the active level" area (player reachable).
			if(board.isAccessible(position) && reachablePlayerSquares.isSquareReachable(position)) {

				// Reduce the "reachable" marked area by the area reachable from the current position.
				reachablePlayerSquares.reduce(position);

				// Set the player to the position.
				board.playerPosition = position;

				// Create a board position from the current board.
				BoardPositionPackingSequence currentBoardPosition = new BoardPositionPackingSequence(board);

				// Store the board position for being able of detecting duplicates.
				boardPositionStorage.put(currentBoardPosition, currentBoardPosition);

				// Add the board position to the open queue.
				boardPositionsToBeAnalyzed.add(currentBoardPosition);
			}
		}

		// Do the search for a packing sequence.
		lastBoardPositionPackingSequence = doSearch();

		// If no packing sequence has been found display a message and exit.
		if(lastBoardPositionPackingSequence == null){

			// Clear the hash table.
			boardPositionStorage.clear();

			if(Debug.isDebugModeActivated) {
				System.out.println("no packing sequence found");	//TODO
			}

			// Restore the start board position and display it.
			board.setBoardPosition(startBoardPosition.getPositions());
			board.setPlayerPosition(startPlayerPosition);

			// Exit the solver.
			return;
		}

		// ArrayList holding all pushes of the packing sequence.
		packingSequence = new ArrayList<BoardPositionPackingSequence>();

		// Add the board positions in the correct order.
	    for(BoardPositionPackingSequence boardPosition = lastBoardPositionPackingSequence; boardPosition.getPrecedingBoardPosition() !=  null; boardPosition = boardPosition.getPrecedingBoardPosition()) {
	    	packingSequence.add(boardPosition);
	    }

	    // Create an array holding the packing order.
	    MainBoardDisplay.numbersToShow = new int[board.size];
	    // Initialize with "don't display"
	    Arrays.fill(MainBoardDisplay.numbersToShow, -1);
	    int i=1;
	    for(BoardPositionPackingSequence boardPosition : packingSequence) {
	    	MainBoardDisplay.numbersToShow[board.getGoalPosition(boardPosition.getPulledBoxNumber())] = i++;
	    }

		// Clear the position storage.
	    boardPositionStorage.clear();

	    // Restore the start board position.
	    board.setBoardPosition(startBoardPosition.getPositions());
	    board.setPlayerPosition(startPlayerPosition);

		// Now the start board position has been set back, that means
	    // the boxes have their "forward" box numbers.
		// It's possible now to get the order of the boxes to be pushed
	    // in this packing sequence.

	    // Start with -1 so the first used index in the sequence is 0
		int packingSequenceIndex = -1;
		for(BoardPositionPackingSequence boardPosition : packingSequence) {

			// Get the start position of the push.
			int startPosition = boardPosition.getStartBoxPosition();

			// We have reached a new step in the packing sequence.
			packingSequenceIndex++;

			// Check whether the push belongs to a box currently at the start position.
			if(board.isBox(startPosition) == false) {
				continue;
			}

			// Get the number of box.
			int boxNo = board.getBoxNo(startPosition);

			// Save the first time the box has been pushed in the packing sequence.
			if(orderPushedBoxes[boxNo] == 0) {
				orderPushedBoxes[boxNo] = packingSequenceIndex;
			}
		}


//		for(BoardPositionPackingSequence boardPosition : packingSequence) {
//
//			int reachableByBoxesCount = 0;
//
//			// Get the start and the target position of the push.
//			int startPosition = boardPosition.getStartBoxPosition();
//			int targetPosition = boardPosition.getTargetBoxPosition();
//
//			// Jump over forced pushes.
//			if(boardPosition.isForcedPush() == false) {
//
//
//				// Check how many boxes can be pushed to the target position.
//				for(int boxNo=board.boxCount; --boxNo != -1; ) {
//
//					// Get the position of the box
//					int boxPosition = board.boxData.getBoxPosition(boxNo);
////	alle Kisten, die nicht unter packing sequence Kontrolle sind vom feld nehmen für ermittlung?!
//					// Mark the backwards reachable squares of the box.
//					board.boxReachableSquares.markReachableSquares(boxPosition);
//
//					if(board.boxReachableSquares.isSquareReachable(targetPosition))
//						reachableByBoxesCount++;
//				}
//			}
//
//			application.redraw(false);
//
//
//			// Do push.
//			board.pushBox(startPosition, targetPosition);
//			board.setPlayerPosition(boardPosition.getPlayerPosition());
//		}

	    // Restore the start board position.
	    board.setBoardPosition(startBoardPosition.getPositions());
	    board.setPlayerPosition(startPlayerPosition);
	}


	/**
	 * Tries to find a packing sequence for filling the goals in the level.
	 */
	private BoardPositionPackingSequence doSearch() {

		// Hold a box position and the new box position.
		int boxPosition;
		int newBoxPosition = 0;

		// True for goals that have already been reached by a box.
		boolean[] reachedBackwardsGoals;

		// Flag, indicating whether a box could directly be pulled to a goal.
		boolean isBoxPulledToGoal = false;

		// The positions of all boxes and the player
		// (in that order: box positions and at last player position).
		int[] positions;

		// Object for the current board position.
		BoardPositionPackingSequence currentBoardPosition;

		// The board position to be analyzed further.
		BoardPositionPackingSequence boardPositionToBeAnalyzed;

		// If a board position is reached that has been reached before this
		// object holds the board position that has been reached before.
		BoardPositionPackingSequence oldBoardPosition;

		// Object for detecting bipartite deadlocks.
		BipartiteMatchings bipartiteDeadlockCheck = new BipartiteMatchings(board);

		// The board position with the highest relevance value is taken as
		// basis board position for generating successors.
		while((boardPositionToBeAnalyzed = boardPositionsToBeAnalyzed.poll()) != null && callingThread.isCancelled() == false) {

			// Get the information which of the backwards goals has already been reached.
			reachedBackwardsGoals = boardPositionToBeAnalyzed.getReachedGoalsStatus();

			// Get the positions of the boxes and the player in that board positions.
			positions = boardPositionToBeAnalyzed.getPositions();

			// Set the board position on the board.
			board.setBoardPosition(positions);

/**
 * Just for debugging
 */
//for(int goalNo=0; goalNo < backwardsGoalsPositions.length; goalNo++) {
//	int goalPosition = backwardsGoalsPositions[goalNo];
//	if(reachedBackwardsGoals[goalNo] == true)
//		board.removeMarking(goalPosition);
//	else
//		board.setMarking(goalPosition);
//}
//application.redraw(true);


			// Only for debugging: show board positions.
			if(solverGUI.isShowBoardPositionsActivated.isSelected()) {
				Debug.debugApplication.redraw(false);
			}

			// Determine the reachable squares of the player.
			reachablePlayerSquares.update();

			// Yet, for this board position no box has been pulled to a goal.
			isBoxPulledToGoal = false;

			// Check whether any of the boxes can be pulled to a backwards goal.
			for(int boxNo=0; boxNo < board.boxCount; boxNo++) {

				// Get the position of the box
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// A position of 0 means the box isn't on the board anymore.
				if(boxPosition == 0) {
					continue;
				}

				// Mark the backwards reachable squares of the box.
				board.boxReachableSquaresBackwards.markReachableSquares(boxPosition, false);

				// Check whether any of the backwards goals can be reached by the box.
				for(int goalNo=0; goalNo < backwardsGoalsPositions.length; goalNo++) {

					// Jump over goals that have already been reached by a box.
					if(reachedBackwardsGoals[goalNo] == true) {
						continue;
					}

					// Get the position of the goal.
					int goalPosition = backwardsGoalsPositions[goalNo];

					// Only goals that can be reached by the box are relevant. Note: if the
					// box is located directly at the goal position this is treated as NOT reachable.
					if(board.boxReachableSquaresBackwards.isSquareReachable(goalPosition) == false) {
						continue;
					}

					/*
					 * Ok, the box can reach a goal that hasn't been reached by another box before.
					 */

					// Remove the box by setting a position of 0.
					board.removeBox(boxPosition);
					board.boxData.setBoxPosition(boxNo, 0);

					// Create a new array of the reached goals and mark the goal as reached.
					boolean[] reachedBackwardsGoalsNew = reachedBackwardsGoals.clone();
					reachedBackwardsGoalsNew[goalNo] = true;

					// Check for a bipartite deadlock.
					if(bipartiteDeadlockCheck.isDeadlock(SearchDirection.BACKWARD, reachedBackwardsGoalsNew)) {
						// Undo the board changes.
						board.setBox(boxPosition);
						board.boxData.setBoxPosition(boxNo, boxPosition);

						continue;
					}

					// Create a board position of the current board. Start and target position are exchanged because this way
					// they are in the correct order for the forward search in the solver.
					currentBoardPosition = new BoardPositionPackingSequence(board, boxNo, goalPosition, boxPosition, false, reachedBackwardsGoalsNew, boardPositionToBeAnalyzed);

					// Undo the board changes.
					board.setBox(boxPosition);
					board.boxData.setBoxPosition(boxNo, boxPosition);

					// Save the board position in the storage.
					oldBoardPosition = boardPositionStorage.put(currentBoardPosition, currentBoardPosition);

					// If this board position had already been reached before discard it and continue with the next goal.
					if(oldBoardPosition != null) {
						continue;
					}

					// Check whether a packing sequence has been found that is: all boxes are on backwards goals.
					// If yes, return the board position which is the end of the packing sequence.
					for(goalNo=reachedBackwardsGoalsNew.length; --goalNo != -1; ) {
						if(reachedBackwardsGoalsNew[goalNo] == false) {
							break;
						}
					}
					if(goalNo == -1) {
						return currentBoardPosition;
					}

					// Set the relevance value so the search is guided and uses the board positions of higher relevance first.
					currentBoardPosition.setRelevance(boardPositionToBeAnalyzed.getRelevance()+1);

					// Add the board position to the open queue.
					boardPositionsToBeAnalyzed.add(currentBoardPosition);

					// It has been possible to pull a box directly to a goal. Hence, no other pushes have to be done.
					isBoxPulledToGoal = true;

					break;
				}
			}

			// If a box has been pulled directly to a goal no further pushes have to be done.
			if(isBoxPulledToGoal == true) {
				continue;
			}

			/*
			 * None of the boxes could directly be pulled to a goal.
			 * Now the boxes are pulled 1 square to create new board positions.
			 */

			// Loop over all boxes.
			for(int boxNo=0; boxNo < board.boxCount; boxNo++) {

				// Get the position of the box
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// A position of 0 means the box isn't on the board anymore.
				if(boxPosition == 0) {
					continue;
				}

				// Pull the box to every direction possible.
				for(int direction = 0; direction < 4; direction++) {

					// Calculate the new box position.
					newBoxPosition = board.getPosition(boxPosition, direction);

					// Immediately continue with the next direction if the player can't reach the correct
					// position for pulling or the new box position isn't accessible.
					if(!board.isAccessibleBox(newBoxPosition) ||
					   !reachablePlayerSquares.isSquareReachable( board.getPosition(newBoxPosition, direction) )) {
						continue;
					}

					// Do pull.
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = board.getPosition(newBoxPosition, direction);

					// Check for a bipartite deadlock.
					if(bipartiteDeadlockCheck.isDeadlock(SearchDirection.BACKWARD, reachedBackwardsGoals)) {
						// Undo the pull for the next pull in this loop (player is still in the right area).
						board.pushBox(newBoxPosition, boxPosition);
						continue;
					}

					// Create a board position of the current board (new box position and current box position are changed because it's a backwards search
					// and the packing sequence is later needed in a forwards search).
					currentBoardPosition = new BoardPositionPackingSequence(board, boxNo, newBoxPosition, boxPosition, true, reachedBackwardsGoals, boardPositionToBeAnalyzed);

					// Undo the pull for the next pull in this loop (player is still in the right area).
					board.pushBox(newBoxPosition, boxPosition);

					// Save the board position in the storage.
					oldBoardPosition = boardPositionStorage.put(currentBoardPosition, currentBoardPosition);

					// If the board position had already been saved this is a duplicate board position which can be discarded.
					if(oldBoardPosition != null) {
						continue;
					}

					// The board position gets a small penalty so other board positions get a chance, too.
					currentBoardPosition.setRelevance(boardPositionToBeAnalyzed.getRelevance()+1);

					// Save the board position in the open queue.
					boardPositionsToBeAnalyzed.add(currentBoardPosition);
				}
			}
		}

		// No packing sequence has been found.
		return null;
	}

	/**
	 * The found packing sequence, or <code>null</code> if no packing
	 * sequence has been found.
	 *
	 * @return the found packing sequence
	 */
	public ArrayList<BoardPositionPackingSequence> getPackingSequence() {
		return packingSequence;
	}


	/**
	 * Returns the order in which the boxes have been pushed the first time
	 * in the packing sequence.
	 * <p>
	 * orderPushedBoxes[2] = 6 means: the box with box number 2 has been
	 * pushed the first time at index 6 of the packing sequence.
	 *
	 * @return the order of the pushed boxes in the packing sequence
	 */
	public int[] getOrderPushedBoxes() {
		return orderPushedBoxes;
	}
}
