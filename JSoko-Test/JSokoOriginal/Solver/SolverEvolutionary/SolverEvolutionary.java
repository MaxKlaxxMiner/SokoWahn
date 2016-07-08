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
package de.sokoban_online.jsoko.solver.solverEvolutionary;

import java.util.ArrayList;
import java.util.ListIterator;
import java.util.PriorityQueue;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.solver.Influence;
import de.sokoban_online.jsoko.solver.Solver;
import de.sokoban_online.jsoko.solver.SolverGUI;
import de.sokoban_online.jsoko.solver.solverEvolutionary.boardPositions.AbsoluteBoardPositionEvolutionarySolver;
import de.sokoban_online.jsoko.solver.solverEvolutionary.boardPositions.IBoardPositionEvolutionarySolver;
import de.sokoban_online.jsoko.solver.solverEvolutionary.boardPositions.RelativeBoardPositionEvolutionarySolver;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class implements a solver that tries to find a solution for a level - no matter what solution.
 * Hence, the solutions found needn't to be optimal regarding moves and pushes.
 */
public final class SolverEvolutionary extends Solver implements DirectionConstants {

	// PriorityQueue for the board positions to be analyzed.
	private PriorityQueue<IBoardPositionEvolutionarySolver> boardPositionsToBeAnalyzedForward;

	// The last board position of the found solution.
	private IBoardPositionEvolutionarySolver solutionBoardPosition;

	// Object for calculating the influence a push had on any square on the board.
	private Influence influence;

	/**
	 * Creates an instance of this class.
	 *
	 * @param application  Reference to the main object
	 * @param solverGUI reference to the GUI of this solver
	 */
	public SolverEvolutionary(JSoko application, SolverGUI solverGUI) {
		super(application, solverGUI);

		influence = new Influence(board);
		influence.calculateInfluenceValues();

		// Create a priority queue for storing the board positions.
		boardPositionsToBeAnalyzedForward = new PriorityQueue<IBoardPositionEvolutionarySolver>(100000);
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

		// During the search the board position is changed. Hence, the initial board position is backuped.
		AbsoluteBoardPositionMoves startBoardPosition;

		// Create an object of the current board position.
		IBoardPositionEvolutionarySolver currentBoardPosition = new AbsoluteBoardPositionEvolutionarySolver(board);

		// Backup the start board position.
		startBoardPosition = new AbsoluteBoardPositionMoves(board);

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


		// Display an info because the search now starts.
		publish(Texts.getText("solver.searchingSolution"));

		// Take the current board position as start for the search.
		boardPositionsToBeAnalyzedForward.add(currentBoardPosition);

		// The initial board position is counted, too.
		boardPositionsCount++;

		// Remember the start time.
		long startTimeStamp = System.currentTimeMillis();

		// Search for a solution by doing a forward search.
		isSolutionFound = forwardSearch();

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
			IBoardPosition boardPosition = i.next();

			int pushedBoxNo = boardPosition.getBoxNo();
			int direction   = boardPosition.getDirection();
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
		if(Debug.debugShowHashTableStatistic == true) {
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
	protected final boolean forwardSearch() {

		// Hold a box position and the new box position.
		int boxPosition;
		int newBoxPosition = 0;

		// Object for the current board position
		IBoardPositionEvolutionarySolver currentBoardPosition;

		// The board position to be analyzed further.
		IBoardPositionEvolutionarySolver boardPositionToBeAnalyzed;

		// If a board position is reached that has been reached before this object holds
		// the board position that has been reached before.
		IBoardPosition oldBoardPosition;

		// Lowerbound of a board position.
		int currentBoardPositionLowerbound = 0;

		// Number of the pushed box.
		int lastPushedBoxNo = 0;

	    // A set bit means the box represented by this bit is relevant for the next push.
		boolean[] relevantBoxes = null;

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
			lastPushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

			// Identify the relevant boxes for the next push.
			relevantBoxes = identifyRelevantBoxes();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(lastPushedBoxNo == NO_BOX_PUSHED) {
				lastPushedBoxNo = -1;
			}

			// Loop over all boxes. The last pushed box is considered first.
			for(int boxCounter = -1, boxNo; boxCounter < board.boxCount; boxCounter++) {

				// The last pushed box has already been processed (-> boxCounter = -1)
				if(boxCounter == lastPushedBoxNo) {
					continue;
				}

				// The last pushed box is considered first. It is checked for being in a tunnel.
				if(boxCounter == -1) {
					boxNo = lastPushedBoxNo;

					// If the box is in a tunnel only pushes of this box have to be considered!
					if(isBoxInATunnel(lastPushedBoxNo, boardPositionToBeAnalyzed.getDirection())) {
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
					currentBoardPosition = new RelativeBoardPositionEvolutionarySolver(application, boxNo, direction, boardPositionToBeAnalyzed);

					// Try to read the current board position from the hash table.
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// If the board position had already been saved in the hash table and has not been created during the corral
					// detection that this is a duplicate board position which can be discarded.
					// (The corral deadlock detection also saves board positions in the hash table in order not avoid having to create an own one).
					if(oldBoardPosition != null) {
						board.pushBoxUndo(newBoxPosition, boxPosition);
						continue;
					}

					/*
					 * The board position hasn't already been in the hash table, hence it is a new one.
					 */
					// Heuristic 1: estimated pushes to a goal state (= pushes lower bound)
					currentBoardPositionLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

					// Heuristic 2: pushes done for reaching the current board position.
					int currentBoardPositionPushesCount = boardPositionToBeAnalyzed.getPushesCount() + 1;

					// Heuristic 3: same box is pushed as before.
					boolean sameBoxPush = boxNo == lastPushedBoxNo;

					// Heuristic 4: distance to the nearest goal.
					int distanceToNearestGoal = getDistanceToNearestGoal(boxNo);

					// Heuristic 5: distance to nearest not occupied goal.
					int distanceToNearestNotOccupiedGoal = getDistanceToNearestNotOccupiedGoal(boxNo);

					// Heuristic 6: influence
					int influenceOnLastPushedBoxPosition = 0;
					if(lastPushedBoxNo != -1) {
						influenceOnLastPushedBoxPosition = getInfluenceValue(boxNo, lastPushedBoxNo);
					}

					// Heuristic 7: boxes on goals.
					int boxesOnGoalsCount = board.boxData.getBoxesOnGoalsCount();


					// Set the relevance of this board position for the search.
					currentBoardPosition.setRelevance(getRelevance(currentBoardPositionLowerbound, currentBoardPositionPushesCount, sameBoxPush,
							distanceToNearestGoal, distanceToNearestNotOccupiedGoal, influenceOnLastPushedBoxPosition, boxesOnGoalsCount));

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

					// Save the board position in the hash table and for further searching.
					positionStorage.storeBoardPosition(currentBoardPosition);
					boardPositionsToBeAnalyzedForward.add(currentBoardPosition);
				}
			}
		}
		return false;
	}

	/**
	 * !!! Currently this method is just a 1:1 copy from method from SolverAnySolution. !!!
	 *
	 * Returns whether the box corresponding to the passed box number is in a tunnel.
	 * If this is the case only pushes of this box are relevant for the current board position.
	 * <p>
	 * This is a copy of the method from the A*-Solver. This solver needn't to preserve push optimality,
	 * therefore it may use another version in the future.
	 *
	 * @param boxNo the number of the box
	 * @param pushDirection	the direction the box has been pushed to
	 * @return	<code>true</code> the box is in a tunnel
	 * 		   <code>false</code> the box is not in a tunnel
	 */
	private boolean isBoxInATunnel(int boxNo, int pushDirection){

		// New box position.
		int newBoxPosition;

		// Get current box position
		int boxPosition = board.boxData.getBoxPosition(boxNo);

		// Determine the opposite axis of the push direction
		int oppositeAxis = UP;
		if(pushDirection == UP || pushDirection == DOWN) {
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
		 * Check whether by pushing the box only "safe" configurations can be generated.
		 * or whether the box can be pushed into just one direction.
		 *
		 * A "safe" configuration is one of:
		 *            #
		 *   #$#  or  $
		 *            #
		 */
		// Identify the reachable squares of the player assumed only the relevant box is present on the board.
		board.setWall(boxPosition);
		board.playersReachableSquaresOnlyWalls.update();
		board.removeWall(boxPosition);

		// The number of directions the box is pushable to.
		byte pushableDirectionsCount = 0;

		// Number of safe positions.
		byte safeSituationsCount = 0;

		// Number of directions the box can be pushed to when all other boxes are on the board, too.
		byte reallyPushableDirectionsCount = 0;

		// Number of squares the box can be pushed to that weren't reachable for the player
		// before the push (= squares in a corral).
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
				// only 2 free neighbor squares. Example:
				//   # #
				//   #$#
				//   #@#
				pushableDirectionsCount == 2 && safeSituationsCount == 2 &&
				notWallNeighborSquaresCount == 2 ||

				// The box can only be pushed to safe positions and all of these pushes can be done at the moment -> tunnel.
				pushableDirectionsCount > 0 && pushableDirectionsCount == safeSituationsCount
				&& pushableDirectionsCount == reallyPushableDirectionsCount ||

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
						if(board.playersReachableSquaresOnlyWalls.isSquareReachable(position) == false
								&& board.isGoal(position) && board.isBox(position) == false) {
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
	 * Returns the push distance of the box corresponding to the passed box number to the nearest goal.
	 *
	 * @param boxNo  number of the box whose distance is returned
	 * @return distance of the box to the nearest goal
	 */
	public int getDistanceToNearestGoal(int boxNo) {

		// The lowest found distance of the box to a goal.
		int lowestDistance = Integer.MAX_VALUE;

		for(int goalNo = 0; goalNo < board.goalsCount; goalNo++) {

			// Get the box distance to the goal.
			int boxDistanceToGoal = board.distances.getBoxDistanceForwardsNo(boxNo, goalNo);

			// If the distance to that goal is lower than the current lowest found distance take it as new lowest distance.
			if(boxDistanceToGoal < lowestDistance) {
				lowestDistance = boxDistanceToGoal;
			}
		}

		// Return the lowest found distance.
		return lowestDistance;
	}

	/**
	 * Returns the push distance of the box corresponding to the passed box number
	 * to the nearest goal not occupied by another box.
	 *
	 * @param boxNo  number of the box whose distance is returned
	 * @return distance of the box to the nearest goal
	 */
	public int getDistanceToNearestNotOccupiedGoal(int boxNo) {

		// The lowest found distance of the box to a not occupied goal.
		int lowestDistance = Integer.MAX_VALUE;

		// If the box is located on a goal the distance is 0.
		if(board.isGoal(board.boxData.getBoxPosition(boxNo))) {
			return 0;
		}

		// Determine the lowest distance to any not occupied goal.
		for(int goalNo = 0; goalNo < board.goalsCount; goalNo++) {

			// Jump over goals occupied by another box.
			if(board.isBox(board.getGoalPosition(goalNo))) {
				continue;
			}

			// Get the box distance to the goal.
			int boxDistanceToGoal = board.distances.getBoxDistanceForwardsNo(boxNo, goalNo);

			// If the distance to that goal is lower than the current lowest found distance
			// take it as new lowest distance.
			if( lowestDistance > boxDistanceToGoal ) {
				lowestDistance = boxDistanceToGoal;
			}
		}

		// Return the lowest found distance.
		return lowestDistance;
	}

	/**
	 * Returns the influence value between the positions of the passed boxes.
	 * <p>
	 * The LOWER the influence value the more influence has the box on the other box.
	 *
	 * @param boxNo1  the position of the first  box whose position is considered
	 * @param boxNo2  the position of the second box whose position is considered
	 * @return the influence value
	 */
	public int getInfluenceValue(int boxNo1, int boxNo2) {
		return influence.getInfluenceDistance( board.boxData.getBoxPosition(boxNo1),
				                               board.boxData.getBoxPosition(boxNo2) );
	}

	/**
	 * Returns the relevance of a board position for the solver.
	 *
	 * @param currentBoardPositionLowerbound  estimated pushes distance to a solved board position
	 * @param currentBoardPositionPushesCount pushes done to reach the current board position
	 * @param sameBoxPush					  true if the same box has been pushed as in the push before
	 * @param distanceToNearestGoal   		  distance of the pushed box to the nearest goal
	 * @param distanceToNearestNotOccupiedGoal distance of the pushed box to the nearest goal not already occupied by another box
	 * @param influenceOnLastPushedBoxPosition influence of the pushed box on the last pushed box
	 * @param boxesOnGoalsCount				  Number of boxes on a goal
	 * @return relevance value (the higher the value the more important is the board position for the solver)
	 */
	public int getRelevance(int currentBoardPositionLowerbound, int currentBoardPositionPushesCount, boolean sameBoxPush,
			int distanceToNearestGoal, int distanceToNearestNotOccupiedGoal, int influenceOnLastPushedBoxPosition, int boxesOnGoalsCount) {

		/*
		 * All the passed values can be taken as heuristic.
		 */
//		System.out.printf("\nLowerbound: %d", currentBoardPositionLowerbound);
//		System.out.printf("\nPushes count: %d", currentBoardPositionPushesCount);
//		System.out.printf("\nSame box pushed again: "+sameBoxPush);
//		System.out.printf("\nDistance to nearest goal: %d", distanceToNearestGoal);
//		System.out.printf("\nDistance to nearest free goal: %d", distanceToNearestNotOccupiedGoal);
//		System.out.printf("\nInfluence on last pushed box: %d\n", influenceOnLastPushedBoxPosition);
//		application.redraw(true);

		// Return the relevance of the board position calculated from the heuristics.

		// This does not always generate push optimal solutions, since e.g.
		// the solution length of a configuration can be underestimated, and
		// the successor configuration is stored in the hash table.
		// When we later meet this configuration on the perfect solution path,
		// it is already contained in the hash table and not investigated, again.
		// Hence, such a configuration must be reached and investigated
		// multiple times.
		// (Hence, there is this "if" in the A* solver:
		//if(oldBoardPosition != null &&
		//   oldBoardPosition.getPushesCount() <= currentBoardPosition.getPushesCount())    // !important!
		return - (currentBoardPositionLowerbound + currentBoardPositionPushesCount) * 10000
		       + currentBoardPositionPushesCount * 100
		       - distanceToNearestNotOccupiedGoal;
	}
}