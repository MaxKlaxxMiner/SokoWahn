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

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.board.Directions;


/**
 * This class calculates influence distances. These distances can be used
 * in the optimizer and the solvers for guiding the search to promising
 * board positions.
 * Two squares that are far away from each other don't influence each other
 * much. This can be used to prune some of the pushes during the search
 * for a (better) solution. Hence, the search doesn't jump from one area
 * of the board to another and back all the time when doing pushes,
 * but rather keeps the search localized to specific areas.
 */
public final class Influence
		implements DirectionConstants
{

	/** Influence distances between the squares. */
	private int[][] influenceDistances;

	/** Reference to the board object.*/
	private Board board;

	/**
	 * Size of internal Q.
	 * Since below we use a copy-down heuristic, we may work successfully
	 * with substantially larger boards.
	 */
	private final int ENDPATH = 6000;
	// NB: this is a heuristic bound, not a guarantee.

	/**
	 * Creates an object for calculation of influence distances.
	 *
	 * @param board	 reference to the board object
	 */
	public Influence(Board board) {

		this.board = board;

	}

	/**
	 * This function sets the influence distances, capturing the idea of
	 * influence of the squares.
	 * Uses breadth first search to minimize re-traversals.
	 * <p>
	 * The smaller the influence distance, the more two squares influence
	 * each other.
	 *
	 * This methods calculates a influence distances from every square
	 * to every other square. These distances are an indicator how much
	 * influence a square has on another square. They are used to determine
	 * which box pushes get a higher relevance in the search.
	 */
	public void calculateInfluenceValues() {

		// The information that are stored in the Q
		// and used during the influence calculation.
		int fromDirection;
		int currentInfluenceDistance = 0;
		int currentPosition;

		// Variables for the stack.
		int next_in;
		int next_out;

		// Stacks for storing the position, the current influence distance
		// and the direction the push has been made from.
		int[] positionsQ     = new int[ENDPATH];
		int[] influenceQ     = new int[ENDPATH];
		int[] fromDirectionQ = new int[ENDPATH];

		// Backup of the player position. This is necessary, because the
		// player position is changed during execution of this method.
		// FFS/hm: isn't this dangerous?  it is the major board, also used by other threads
		int playerPositionBackup = board.playerPosition;

		// Create an array holding the influence distances and initialize it
		// with the maximum value.
		influenceDistances = new int[board.size][board.size];
		for (int position1 = 0; position1 < board.size; position1++) {
			for (int position2 = 0; position2 < board.size; position2++) {
				influenceDistances[position1][position2] = Integer.MAX_VALUE;
			}
		}

		// Remove all boxes from the board. The influence distances just take
		// one box into account.
		board.removeAllBoxes();

		// Calculate the influence distance from every square to every square.
		for (int startPosition = board.firstRelevantSquare; startPosition < board.lastRelevantSquare; startPosition++) {

			// Skip squares that are outside the board or a wall.
			if (board.isOuterSquareOrWall(startPosition)) {
				continue;
			}

			// Add the first square to the Q.
			positionsQ[0]     = startPosition;
			influenceQ[0]     = 0;
			fromDirectionQ[0] = NO_DIR;
			next_in  = 1;
			next_out = 0;

			// As long as there is something in the Q.
			while (next_out < next_in) {

				// Dequeue the information for the next square to be analyzed
				// (position, current influence value, direction the box comes from)
				currentPosition          = positionsQ[next_out];
				currentInfluenceDistance = influenceQ[next_out];
				fromDirection        	 = fromDirectionQ[next_out];
				next_out++;

				// Increase the distance considering the alternative paths
				// from the start position to the current position.
				currentInfluenceDistance += alternativesDistance(startPosition, currentPosition, fromDirection);

				// We search the lowest influence distance (the smaller the more influence).
				if (currentInfluenceDistance >= influenceDistances[startPosition][currentPosition]) {
					continue;
				}

				// Save the calculated influence distance from the Q.
				influenceDistances[startPosition][currentPosition] = currentInfluenceDistance;

				// Check whether this square is in a tunnel (= only two free neighbor squares)
				int freeNeighbors = 0;
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					if (!board.isWall( board.getPosition(currentPosition, direction) )) {
						freeNeighbors++;
					}
				}

				/*
				 * If the current position is a corral forcer with just two
				 * free neighbor squares, then the influence distance is
				 * increased by 1000 which means boxes on the neighbor
				 * squares are irrelevant for the search.
				 * This is independent from the next square and its direction,
				 * and hence added to the base value for all position steps.
				 */
				if (board.isCorralForcer(currentPosition) && freeNeighbors == 2) {
					currentInfluenceDistance += 1000;
				}

				/*
				 * Connection: The connection between consecutive squares along
				 * a path is used to modify the influence distance.
				 * If a box can be pushed in the direction of the destination
				 * square, then the 1 is added. If only the player can
				 * traverse the connection between the squares (moving towards
				 * the destination square), then 2 is added.
				 * (the higher the higher the distances gets = less influence)
				 * However, if the previous square on a path is in a tunnel
				 * (just two free neighbor squares), 0 is added,
				 * regardless of the above properties (all squares in a tunnel
				 * should get the same influence distance).
				 */

				// Check every direction for new squares for which to calculate
				// the distance, too.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {

					// Don't go back to the square we came from.
					if (fromDirection == direction) {
						continue;
					}

					// Consider the next position (a step in "direction")
					// as a new candidate for the Q.
					final int nextPos    = board.getPosition(currentPosition, direction);
					final int nextPosOpp = board.getPositionAtOppositeDirection(currentPosition, direction);

					int     newInfluenceDistance = currentInfluenceDistance;
					boolean takeit = false;		// not yet decided

					// Calculate new distance:
					//   Tunnel -> +0,
					//   box connectivity -> +1,
					//   only player can enter the square -> +2
					if (board.isAccessibleBox(nextPos) && board.isAccessible(nextPosOpp)) {
						newInfluenceDistance += (freeNeighbors == 2 ? 0 : 1);
						takeit = true;
					} else if (board.isWall(nextPos) == false) {
						newInfluenceDistance += (freeNeighbors == 2 ? 0 : 2);
						takeit = true;
					}
					if (takeit) {
						// Add the next position to the Q.
						if ((next_in >= ENDPATH) && (next_out > 0)) {
							// copy down arrays if next_in is too large
							System.arraycopy(positionsQ    , next_out, positionsQ    , 0, next_in-next_out);
							System.arraycopy(influenceQ    , next_out, influenceQ    , 0, next_in-next_out);
							System.arraycopy(fromDirectionQ, next_out, fromDirectionQ, 0, next_in-next_out);
							next_in  -= next_out;
							next_out  = 0;
						}
						positionsQ    [next_in] = nextPos;
						fromDirectionQ[next_in] = Directions.getOppositeDirection(direction);
						influenceQ    [next_in] = newInfluenceDistance;
						next_in++;
					}
				}
			}
		}

		// Reset the initial board position.
		board.setPlayerPosition(playerPositionBackup);
		for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			board.setBox(board.boxData.getBoxPosition(boxNo));
		}
	}

	/**
	 * This methods returns distance points for alternative paths
	 * from a square to another square.
	 *
	 * @param startPosition   The position the calculation started at
	 * @param currentPosition the current position influence of which on the
	 *                        start position is to be calculated
	 * @param fromDirection   the direction the push has been made from
	 *                        (may be {@code NO_DIR}
	 *
	 * @return the distance points for alternative paths
	 */
	private int alternativesDistance(int startPosition, int currentPosition, int fromDirection) {

		// Number of neighbor squares reachable for the player / for a box.
		int countForPlayer = 0;
		int countForBoxes  = 0;

		// Influence distance for a position, to be calculated here
		int influenceDistance = 0;

		// Get the other axis. The first square has "fromDirection" = -1.
		// However, the influence to himself isn't that important => in that
		// case just take the calculated direction of (-1+2)%4 = 1.
		int oppositeAxisDirection = Directions.getOrthogonalDirection(fromDirection);

		/*
		 * Alternatives: A square s on a path will have two neighboring squares
		 * that are not on the path. For each of the neighboring squares, n,
		 * the following points are added:
		 * 3 points if it is possible to push a box (if present) from s to n;
		 * 1 point if it is only possible to move the player from s to n; and
		 * 0 if n is a wall.
		 * Thus, the maximum number of points that one square can contribute
		 * for alternatives is 6.
		 */

		// Check whether it is possible to push a box to the neighbor squares
		// and whether it is possible to move the player to the neighbor squares.
		final int nextPos1 = board.getPosition(currentPosition, oppositeAxisDirection);
		final int nextPos2 = board.getPositionAtOppositeDirection(currentPosition, oppositeAxisDirection);
		if (board.isAccessibleBox(nextPos1) && board.isAccessible(nextPos2)) {
			countForBoxes++;
		} else if (board.isAccessible(nextPos1)) {
			countForPlayer++;
		}

		if (board.isAccessibleBox(nextPos2) && board.isAccessible(nextPos1)) {
			countForBoxes++;
		} else if (board.isAccessible(nextPos2)) {
			countForPlayer++;
		}

		// Calculate the influence distance for "alternatives".
		// 3 for every neighbor square reachable for a box,
		// and 1 for those reachable for the player.
		influenceDistance = 3 * countForBoxes + countForPlayer;

		// If the current position is on an optimal path from the start square
		// to any of the goals in the maze, then the alternatives points
		// are divided by two.
		if (isSquareOnOptimalPathToAnyGoal(startPosition, currentPosition)) {
			influenceDistance >>= 1;
		}

		return influenceDistance;
	}

	/**
	 * Returns whether the passed via square is on an optimal path
	 * from the passed box position to any of the goals.
	 *
	 * @param boxPosition  position the box is located
	 * @param via position the box is to be pushed via to be pushed to any goal
	 * @return <code>true</code> if via is on the optimal path to any of the
	 *                           goals, and<br>
	 * 		  <code>false</code> otherwise
	 */
	private boolean isSquareOnOptimalPathToAnyGoal(int boxPosition, int via) {

		int boxDistance  = 0;
		int boxDistance2 = 0;

		// Set the player on top of the assumed box. This ensures that the
		// minimum distance is calculated no matter where the player is.
		board.setPlayerPosition(boxPosition);

		// Check for all goals whether the square "via" is on the best path
		// from the box position to at least on of the goals.
		for (int goalNo = 0; goalNo < board.goalsCount; goalNo++) {

			// Get the minimum box-distance from the box position to the goal.
			boxDistance = board.distances.getBoxDistanceForwardsPosition(boxPosition, board.getGoalPosition(goalNo));
			if (boxDistance == Board.UNREACHABLE) {
				continue;
			}

			// Get the sum of the minimum box distance to the position "via"
			// and the minimum box distance from "via" to the goal.
			int distanceVia  = board.distances.getBoxDistanceForwardsPosition(boxPosition, via);
			int distanceGoal = board.distances.getBoxDistanceForwardsPosition(via, board.getGoalPosition(goalNo));
			if(distanceVia == Board.UNREACHABLE || distanceGoal == Board.UNREACHABLE) {
				continue;
			}
			boxDistance2 = distanceVia + distanceGoal;

			// If the distance via the square "via" is as long as the direct
			// path, then "via" is on the optimal path to the goal.
			if (boxDistance2 == boxDistance) {
				return true;
			}
		}

		// "via" isn't on an optimal path to any goal.
		return false;
	}

	/**
	 * Returns the influence distance between the start and the target square.
	 * The higher the distance the less influence the target square has
	 * on the start square.
	 *
	 * @param startSquare  position of the start square
	 * @param targetSquare position of the target square
	 *
	 * @return influence distance from the start square to the target square
	 */
	public int getInfluenceDistance(int startSquare, int targetSquare) {
		return influenceDistances[startSquare][targetSquare];
	}
}
