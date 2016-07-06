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
package de.sokoban_online.jsoko.workInProgress;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.board.Directions;

/**
 * Static class for calculating paths for player and boxes.
 */
public final class PlayerPathCalculation implements DirectionConstants {

	/** Constant that is added for every move in the player path. */
	private final static int MOVES_VALUE = 1 << 16;

	/**
	 * A crossroads square is a square that can be entered by the player
	 * with the same number of moves and player lines from both axis.
	 */
	private final static int CROSSROADS_SQUARE = Integer.MAX_VALUE;

	/** Constant for marking a square not being reached by the player, yet. */
	private final static int NOT_REACHED = Integer.MAX_VALUE;

	/** This array holds the information about which square has already
	 *  been reached from which square.
	 */
	private int[] reachedByDirection;

	/** Queue for storing the squares to be further analyzed. */
	private int[] positionsQueue;

	// The number of moves and player lines needed to reach a specific square, stored this way:
	// number of moves * MOVES_VALUE + number of player lines
	private int[] pathValues;

	private PlayerPathCalculation() {
		reachedByDirection = new int[0];
	}

	/**
	 * Returns a player path from the start to the target square.
	 * The path is optimized regarding <br>
	 * 1. moves <br>
	 * 2. player lines <br>
	 * 
	 * FFS: this method is unused?
	 * FFS: the algorithm may be not completely correct (says heiner)
	 * 
	 * @param board  the board the path is to be found on
	 * @param startPosition the start position of the player
	 * @param directionStartSquare the direction the player has got to the start square
	 * @param targetPosition the target position for the player
	 * @param directionTargetSquare
	 * @return the path to the target square (including the start and the target square)
	 */
	public final int[] calculatePlayerPathWithLinesOptimization(
			Board board, int startPosition, int directionStartSquare,
			int targetPosition, int directionTargetSquare) {

		// Variables for the queue for storing the square positions to be analyzed.
		int topOfQueue = -1;
		int currentQueueIndex = 0;

		// Current position of the player and the new position of the player.
		int currentPosition = 0;
		int newPosition = 0;

		// Value of a path (value = moves * MOVES_VALUE + number of player lines).
		int newPathValue = 0;

		// In case the player is already located at the target square
		// we have a trivial path and return immediately.
		if (startPosition == targetPosition) {
			return new int[] { targetPosition };
		}

		// If the board is bigger than the current arrays, create new arrays.
		if (board.size > reachedByDirection.length) {
			reachedByDirection = new int[board.size];
			positionsQueue     = new int[board.size];
			pathValues         = new int[board.size];
		}

		// Initialize array with "not reached".
		for (int i = pathValues.length; --i != -1;) {
			pathValues[i] = NOT_REACHED;
		}

		// Add the start position to the open queue.
		positionsQueue[++topOfQueue] = startPosition;
		pathValues[startPosition] = 0;
		reachedByDirection[startPosition] = directionStartSquare;

		// Search for a path to the target position as long as there are un-expanded positions in the queue.
		while (currentQueueIndex <= topOfQueue) {

			// Get the next position out of the queue.
			currentPosition = positionsQueue[currentQueueIndex++];

			// Move the player to every direction.
			for (int direction = 0; direction < DIRS_COUNT; direction++) {

				// Calculate the new position.
				newPosition = currentPosition + board.offset[direction];

				// Immediately continue with the next direction if the new position isn't accessible for the player.
				if (board.isAccessible(newPosition) == false) {
					continue;
				}

				// Has the target square been reached?
				if (newPosition == targetPosition) {

					// Determine the best path of any of the neighbor squares to the target square.
					for (direction = 0; direction < DIRS_COUNT; direction++) {

						// Calculate the position of the neighbor square.
						newPosition = targetPosition - board.offset[direction];

						// Immediately continue with the next direction if the neighbor square hasn't been reached, yet.
						if (pathValues[newPosition] == NOT_REACHED) {
							continue;
						}

						// Calculate the path value for the neighbor square path.
						newPathValue = pathValues[newPosition] + MOVES_VALUE;
						if (direction != reachedByDirection[newPosition] && reachedByDirection[newPosition] != CROSSROADS_SQUARE){
							newPathValue++;
						}

						// Also take the direction of the target square into account.
						if (directionTargetSquare != CROSSROADS_SQUARE && direction != directionTargetSquare) {
							newPathValue++;
						}

						// Check whether a better path to the target square has been found.
						if (newPathValue < pathValues[targetPosition]) {
							pathValues[targetPosition] = newPathValue;
							reachedByDirection[targetPosition] = direction;
						}
					}

					// Leave the search, because the best path has been found.
					topOfQueue = -1;
					break;
				}

				// The new path value is at least by MOVES_VALUE higher than the one of the current position. 
				newPathValue = pathValues[currentPosition] + MOVES_VALUE;

				// Increase player lines if a turn of the player is necessary to reach the new position.
				if (direction != reachedByDirection[currentPosition] && reachedByDirection[currentPosition] != CROSSROADS_SQUARE) {
					newPathValue++;
				}

				// Check whether a better path to the new position has been found.
				if (newPathValue < pathValues[newPosition]) {

					if (pathValues[newPosition] == NOT_REACHED) {
						positionsQueue[++topOfQueue] = newPosition;
					}

					pathValues[newPosition] = newPathValue;
					reachedByDirection[newPosition] = direction;
					continue;
				}

				// Check whether the new position has already been visited via a better path.
				if (newPathValue > pathValues[newPosition])
					continue;

				// The square has been reached with exactly the same number of
				// moves and player lines. If it is reached from another axis
				// then a crossroads square has been found. This means
				// the player does always continue the player line no matter
				// which direction he will go to next!
				// (If this is already a crossroads point this doesn't matter
				//  for this coding)
				if (Directions.getAxisOfDirection(direction) != Directions.getAxisOfDirection(reachedByDirection[newPosition])) {
					reachedByDirection[newPosition] = CROSSROADS_SQUARE;
				}
			}
		}

		// Return null if no path has been found.
		if (pathValues[targetPosition] == NOT_REACHED) {
			return null;
		}

		// Calculate the number of moves.
		int movesCount = pathValues[targetPosition] >>> 16;

		int[] playerPath = new int[movesCount + 1];

		// The player path is saved from the end to the beginning.
		// Hence, the first position to save is the target position.
		playerPath[movesCount] = targetPosition;
		currentPosition = targetPosition;

		// Save all positions of the best player path. 
		int direction = directionTargetSquare == CROSSROADS_SQUARE ? 0 : directionTargetSquare;
		for (int index = movesCount; --index != -1;) {
			// On crossroads points just go straight in the same direction as before.
			if (reachedByDirection[currentPosition] != CROSSROADS_SQUARE) {
				direction = reachedByDirection[currentPosition];
			}
			currentPosition -= board.offset[direction];
			playerPath[index] = currentPosition;
		}

		return playerPath;
	}
}