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

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPositionMoves;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * Solver which solves a level move optimally with minimal pushes.
 */
public class SolverAStarMovesPushes extends SolverIDAStarPushesMoves {

    /**
     * Constructor
     *
     * @param application  Reference to the main object
     * @param solverGUI reference to the GUI of this solver
     */
    public SolverAStarMovesPushes(JSoko application, SolverGUI solverGUI) {
        super(application, solverGUI);
    }

    /**
     * Tries to solve the level by generating all possible no-deadlock board positions and
     * returns the solution path via a global variable.
     */
    protected final void forwardSearch() {

        // Hold a box position and the new box position.
        int boxPosition;
        int newBoxPosition = 0;

        // Object for the current board position
        IBoardPositionMoves currentBoardPositionWithMoves;

        // The board position to be analyzed further.
        IBoardPositionMoves boardPositionToBeAnalyzed;

        // Object only used for avoiding too many casts.
        IBoardPositionMoves oldBoardPositionWithMoves;

        // If a board position is reached that has been reached before this object holds
        // the board position that has been reached before.
        IBoardPosition oldBoardPosition;

        // Pushes lower bound of a board position.
        int currentBoardPositionLowerBound = 0;

        // Number of moves the player has gone for reaching the current board position.
        short numberOfMovesSoFar = 0;

        // Number of moves and pushes of the current best known solution.
        int numberOfMovesBestSolution  = Integer.MAX_VALUE;
        int numberOfPushesBestSolution = Integer.MAX_VALUE;

        // Number of the pushed box.
        int pushedBoxNo = 0;

        // Number of pushes of the board positions.
        int numberOfPushesOldBoardPosition     = 0;
        int numberOfPushesCurrentBoardPosition = 0;

        // Number of moves of an old board position (board position from the hash table).
        int numberOfMovesOldBoardPosition = 0;

        // Lowest number of moves of a board position in the queue so far.
        int shortestSolutionPathLengthSoFar = 0;


        // The board position with the lowest estimated solution path length is analyzed further next.
        while((boardPositionToBeAnalyzed = getBestBoardPosition()) != null && isCancelled() == false) {

            // Set the board position.
            board.setBoardPosition(boardPositionToBeAnalyzed);

            // Determine the reachable squares of the player. These squares are used even after
            // the deadlock detection, hence they are calculated in an extra object.
            playersReachableSquaresMoves.update();

            // Get number of the last pushed box.
            pushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
				pushedBoxNo = -1;
			}

            // Calculate the number of pushes of the new board positions that are created.
            numberOfPushesCurrentBoardPosition = boardPositionToBeAnalyzed.getPushesCount() + 1;

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

                // Get the position of the box
                boxPosition = board.boxData.getBoxPosition(boxNo);

                // Push the box to every direction possible.
                for(int direction = 0; direction < 4; direction++) {

                    // Calculate the new box position.
                    newBoxPosition = boxPosition + offset[direction];

                    // Immediately continue with the next direction if the player can't reach the correct
                    // position for pushing or the new box position isn't accessible.
                    if(playersReachableSquaresMoves.isSquareReachable(boxPosition - offset[direction]) == false
                       || board.isAccessibleBox(newBoxPosition) == false) {
						continue;
					}

                    // Do push.
                    board.pushBox(boxPosition, newBoxPosition);
                    board.playerPosition = boxPosition;

                    // Calculate the number of moves so far.
                    numberOfMovesSoFar = (short) (boardPositionToBeAnalyzed.getTotalMovesCount() + playersReachableSquaresMoves.getDistance(boxPosition - offset[direction]) + 1);

                    // Immediately continue with the next direction if the the board position isn't
                    // reached better than the current best solution.
                    if(numberOfMovesSoFar > numberOfMovesBestSolution ||
                       numberOfMovesSoFar == numberOfMovesBestSolution &&
                       numberOfPushesCurrentBoardPosition >= numberOfPushesBestSolution) {
                        board.pushBoxUndo(newBoxPosition, boxPosition);
                        continue;
                    }

                    // Create object of the current board position.
                    currentBoardPositionWithMoves = new RelativeBoardPositionMoves(board, boxNo, direction, boardPositionToBeAnalyzed);

                    // Try to read the current board position of the hash table.
                    oldBoardPosition = positionStorage.getBoardPosition(currentBoardPositionWithMoves);

                    // If the board position had already been saved in the hash table it must be checked
                    // for being better than the one in the hash table (it may have already been reached
                    // by the corral detection hence there has to be a check for a SearchBoardPosition!)
                    if(oldBoardPosition instanceof IBoardPositionMoves) {

                        // For avoiding too many casts.
                        oldBoardPositionWithMoves = (IBoardPositionMoves) oldBoardPosition;

                        // Get the number of pushes and moves of the old board position.
                        numberOfPushesOldBoardPosition = oldBoardPositionWithMoves.getPushesCount();
                        numberOfMovesOldBoardPosition  = oldBoardPositionWithMoves.getTotalMovesCount();

                        // If the current board position has been reached better than the one in the
                        // hash table it has to be saved / used instead of the old one.
                        if(numberOfMovesSoFar < numberOfMovesOldBoardPosition ||
                           numberOfMovesSoFar == numberOfMovesOldBoardPosition &&
                           numberOfPushesCurrentBoardPosition < numberOfPushesOldBoardPosition) {

                            // Save the number of moves in the current board position.
                            currentBoardPositionWithMoves.setMovesCount(numberOfMovesSoFar);

                            // Replace the old board position by the new one in the hash table.
                            positionStorage.storeBoardPosition(currentBoardPositionWithMoves);

                            // The current board position is saved for further analyzing.
                            storeBoardPosition(currentBoardPositionWithMoves);
                        }

                        // Undo push and continue with next direction.
                        board.pushBoxUndo(newBoxPosition, boxPosition);
                        continue;
                    }


                    /*
                     * The board position hasn't already been in the hash table, hence it is a new one.
                     */
                    // Calculate the lower bound of the current board position.
					currentBoardPositionLowerBound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

                    // Undo push (the player is new positioned for the next board position anyway)
                    board.pushBoxUndo(newBoxPosition, boxPosition);

                    // Immediately continue with the next direction if the current board position is a deadlock
                    // or the estimated solution path length is higher/equal than the best known solution path length.
                    if(currentBoardPositionLowerBound == LowerBoundCalculation.DEADLOCK ||
                       numberOfMovesSoFar == numberOfMovesBestSolution &&
                       numberOfPushesCurrentBoardPosition >= numberOfPushesBestSolution) {
						continue;
					}

                    // Save the number of moves in the current board position.
                    currentBoardPositionWithMoves.setMovesCount(numberOfMovesSoFar);

                    // If a solution has been found the number of moves and the board position itself
                    // are saved. This solution is move optimal, but it needn't to be the one with
                    // best pushes, too!
                    if(currentBoardPositionLowerBound == 0) {
                        numberOfMovesBestSolution  = numberOfMovesSoFar;
                        numberOfPushesBestSolution = numberOfPushesCurrentBoardPosition;
                        solutionBoardPosition = currentBoardPositionWithMoves;
                        if(Debug.isDebugModeActivated) {
                        	System.out.println("Solution Found "+"Moves/Pushes: "+currentBoardPositionWithMoves.getTotalMovesCount()+"/"+currentBoardPositionWithMoves.getPushesCount());
                        }
                        continue;
                    }

                    // Calculate the number of no deadlock board positions reached during the search.
                    boardPositionsCount++;

                    // Display info about the search (every 5000 board positions and every time the search depths has been increased)
                    if(boardPositionsCount%5000 == 0 || shortestSolutionPathLengthSoFar != shortestSolutionPathLength) {
                        if(shortestSolutionPathLengthSoFar != shortestSolutionPathLength) {
							shortestSolutionPathLengthSoFar = shortestSolutionPathLength;
						}

                        publish(Texts.getText("numberofpositions")+boardPositionsCount+", "+
                        		Texts.getText("searchdepth")+shortestSolutionPathLength+" "+Texts.getText("moves"));

						// Throw "out of memory" if less than 15MB RAM is free.
						if(Utilities.getMaxUsableRAMinMiB() <= 15) {
							isSolverStoppedDueToOutOfMemory = true;
							cancel(true);
						}
                    }

                    // Save the board position in the hash table and for further searching.
                    positionStorage.storeBoardPosition(currentBoardPositionWithMoves);
                    storeBoardPosition(currentBoardPositionWithMoves);
                }
            }
        }
    }
}