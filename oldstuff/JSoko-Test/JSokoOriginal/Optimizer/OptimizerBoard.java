/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2012 by Matthias Meger, Germany
 * 
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General  License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *	
 *	This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General  License for more details.
 *
 *  You should have received a copy of the GNU General  License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
package de.sokoban_online.jsoko.optimizer;

import java.util.Arrays;

import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.optimizer.dataStructures.BoxPositions;



/**
 * Stores the board information for the optimizer.
 */
class OptimizerBoard implements DirectionConstants {		

	// Constants for the internal board elements. This class doesn't use the
	// global "Board" class, but an own board.
	final static byte EMPTY    = 0;
	final static byte WALL     = 1;
	final static byte BOX      = 2;
	final static byte GOAL     = 4;
	final static byte DEADLOCK = 8;

	final int width;
	final int height;
	final int size;
	int playerPosition = 0;
	private final byte[] boardElements;
	public final int[] directionOffsets;
	public final PathCalculation playerPath;


	/**
	 * Board storing all level elements.
	 * 
	 * @param width  width of the board
	 * @param height  height of the board
	 * @param playerPosition  player position on the board
	 */
	OptimizerBoard(int width, int height, int playerPosition) {
		this.size   = width * height;
		this.width  = width;
		this.height = height;
		this.playerPosition  = playerPosition;
		boardElements	 = new byte[size];
		directionOffsets = new int[] {-width, width, -1, 1};
		playerPath		 = new PathCalculation();
	}

	/**
	 * Cloning constructor.
	 */
	private OptimizerBoard(OptimizerBoard board) {
		width 			 = board.width;
		height 		   	 = board.height;
		size 			 = board.size;
		playerPosition   = board.playerPosition;
		boardElements    = board.boardElements.clone();
		directionOffsets = board.directionOffsets.clone(); 
		playerPath		 = new PathCalculation();
	}

	/**
	 * Returns a clone of this board.
	 * 
	 * @return the clone of this board
	 */
	OptimizerBoard getClone() {
		return new OptimizerBoard(this);
	}	



	/**
	 * Sets a box at the passed position.
	 * 
	 * @param position the position a box is to be set.
	 */
	void setBox(int position) {
		boardElements[position] |= BOX;
	}

	/**
	 * Removes the box at the passed position.
	 * 
	 * @param position the position to remove a box from
	 */
	void removeBox(int position) {
		boardElements[position] &= (~BOX);
	}

	/**
	 * Returns whether there is a box at the passed position.
	 * 
	 * @param position the position of the square
	 * @return <code>true</code> if there is a box at the passed position;
	 * or<br> <code>false</code> otherwise
	 */
	boolean isBox(int position) {
		return (boardElements[position] & BOX) != 0;
	}

	/**
	 * Sets a a goal at the passed position.
	 * 
	 * @param position the position the goal is to be set
	 */
	void setGoal(int position) {
		boardElements[position] |= GOAL;
	}

	/**
	 * Removes the goal at the passed position.
	 * 
	 * @param position the position to remove a box from
	 */
	void removeGoal(int position) {
		boardElements[position] &= (~GOAL);
	}

	/**
	 * Returns whether there is a goal at the passed position.
	 * 
	 * @param position the position of the square
	 * @return <code>true</code> if there is a goal at the passed position,
	 * or<br> <code>false</code> if there isn't a goal at the passed position
	 */
	boolean isGoal(int position) {
		return (boardElements[position] & GOAL) != 0;
	}

	/**
	 * Sets a wall at the passed position.
	 * 
	 * @param position the position the wall is to be set
	 */
	void setWall(int position) {
		boardElements[position] |= WALL;
	}

	/**
	 * Removes the wall at the passed position.
	 * 
	 * @param position the position to remove a wall from
	 */
	void removeWall(int position) {
		boardElements[position] &= (~WALL);
	}

	/**
	 * Returns whether there is a wall at the passed position.
	 * 
	 * @param position the position
	 * @return <code>true</code> if there is a wall at the passed position,
	 * or<br> <code>false</code> if there isn't a wall at the passed position
	 */
	boolean isWall(int position) {
		return (boardElements[position] & WALL) != 0;
	}

	/**
	 * Marks the passed position to be a deadlock square.
	 * 
	 * @param position the position to be marked as deadlock
	 */
	void setDeadlock(int position) {
		boardElements[position] |= DEADLOCK;
	}

	/**
	 * Returns whether there the passed position is marked as deadlock square.
	 * 
	 * @param position the position
	 * @return <code>true</code> if there the passed position is marked as deadlock; 
	 * or<br> <code>false</code> otherwise
	 */
	boolean isDeadlock(int position) {
		return (boardElements[position] & DEADLOCK) != 0;
	}

	/**
	 * Check whether every goal is occupied by a box.
	 * 
	 * @return <code>true</code> if all goals are occupied by a box;
	 * 		  <code>false</code> otherwise
	 */
	boolean isEveryBoxOnGoal() {

		// Check if all boxes are located on goals.
		for(int boardElement : boardElements) {
			if((boardElement & (BOX | GOAL)) != (BOX | GOAL)) {
				return false;
			}
		}
		return true;
	}

	/**
	 * Returns whether there the passed position is marked as deadlock square or
	 * there is a wall at the passed position.
	 * 
	 * @param position the position
	 * @return <code>true</code> if there the passed position is marked as deadlock or it is a wall;
	 * or<br> <code>false</code> otherwise
	 */
	boolean isWallOrDeadlock(int position) {
		return (boardElements[position] & (WALL | DEADLOCK)) != 0;
	}

	/**
	 * Returns whether there is a box or a wall at the passed position.
	 * 
	 * @param position the position
	 * @return <code>true</code> if there is a box or a wall at the passed position;
	 * or<br> <code>false</code> otherwise
	 */
	boolean isBoxOrWall(int position) {
		return (boardElements[position] & (BOX | WALL)) != 0;
	}


	/**
	 * Moves the player to the passed direction.
	 * <p>
	 * If the move results in a push then the push is also done.<br>
	 * This method does NOT check if the push is really valid! (push into
	 * a wall or another box is possible)
	 * 
	 * @param moveDirection
	 *            direction of the move
	 * @return <code>true</code> when a box has been pushed; <code>false</code>
	 *         when just a move has been made, no box has been pushed
	 */
	boolean doMovement(int moveDirection) {
		
		int dirOffset = directionOffsets[moveDirection & 3];
		
		int newPlayerPosition = playerPosition + dirOffset;
		
		if(isWall(playerPosition + dirOffset)) {
			return false;
		}
		
		// Move the player.
		playerPosition = newPlayerPosition;
				
		// Push a box if necessary.
		if (isBox(newPlayerPosition)) {
			removeBox(newPlayerPosition);
			setBox(newPlayerPosition + dirOffset);
			return true;
		}

		// No push done.
		return false;
	}


	/**
	 * Do the movement on the main board of the optimizer.
	 * 
	 * @param moveDirection
	 *            direction of the move
	 * @param boxPositions
	 *            in this object the old and new box position of the pushed box
	 *            are returned (if the movement is a push)
	 * @return <code>true</code>a box has been pushed <code>false</code>no box
	 *         has been pushed
	 */
	public boolean doMovementWithBoxPositions(byte moveDirection, BoxPositions boxPositions) {

		int dirOffset = directionOffsets[moveDirection & 3];
		
		int newPlayerPosition = playerPosition + dirOffset;
		
		if(isWall(playerPosition + dirOffset)) {
			return false;
		}
		
		// Move the player.
		playerPosition = newPlayerPosition;
		
		// Push a box if necessary.
		if (isBox(newPlayerPosition)) {
			removeBox(newPlayerPosition);
			setBox(newPlayerPosition + dirOffset);
			boxPositions.oldPosition = newPlayerPosition;
			boxPositions.newPosition = newPlayerPosition + dirOffset;
			return true;
		}

		// No push done.
		return false;
	}

	/**
	 * This class is used for calculating the path from one player position to another.
	 * <p>
	 * This class requires the boar to be surrounded by walls so the player
	 * can't leave the board. All passed positions to methods of this class 
	 * must be valid player positions inside the board.  
	 */
	class PathCalculation {

		// Own constants for offset for moving to a specific direction on the board.
		private final int BOARD_UP;
		private final int BOARD_DOWN;
		private final static int BOARD_LEFT  = -1;
		private final static int BOARD_RIGHT =  1;

		private final int[] DIRECTION_OFFSETS;

		private final static int START_POSITION_MARKER = Integer.MIN_VALUE;

		private final int[] queue;
		private final int[] moveDirectionBestPath;

		public PathCalculation() {

			queue     = new int[size];
			moveDirectionBestPath = new int[size];

			BOARD_UP   = -width;
			BOARD_DOWN =  width;

			DIRECTION_OFFSETS = new int[] {BOARD_UP, BOARD_DOWN, BOARD_LEFT, BOARD_RIGHT};
		}

		/**
		 * Returns the number of moves to be done by the player to get from the 
		 * current player position on the board to the passed target position.
		 * <p>
		 * This method doesn't change the board.

		 * @param targetPlayerPosition  target position of the player
		 * @return number of moves to be done to reach the target position or
		 * <code>null</code> if there is no path to the target position. 
		 */
		public int getDistanceTo(int targetPlayerPosition) {
			return getDistance(playerPosition, targetPlayerPosition);
		}

		/**
		 * Returns the number of moves to be done by the player to get 
		 * from the passed position to the passed target position.
		 * <p>
		 * This method doesn't change the board.
 		 *
		 * @param startPlayerPosition   current position of the player
		 * @param targetPlayerPosition  target position of the player
		 * @return number of moves to be done to reach the target position or
		 * <code>null</code> if there is no path to the target position. 
		 */
		public int getDistance(int startPlayerPosition, int targetPlayerPosition) {

			int[] movesToTargetPosition = getMoves(startPlayerPosition, targetPlayerPosition);

			return movesToTargetPosition == null ? Optimizer.NONE : movesToTargetPosition.length;
		}

		/**
		 * Returns the moves to be done by the player to get from the current
		 * player position on the board to the passed target position.
		 * <p>
		 * This method doesn't change the board passed to this object!
		 * 
		 * @param targetPlayerPosition  target position of the player
		 * @return array of directions (see {@link DirectionConstants}) the player
		 *  must move to, to reach the target position.<br> 
		 * The array is empty if the current position is the target position.<br>
		 * <code>null</code> is returned when there is no path to the target position. 
		 */
		public int[] getMovesTo(int targetPlayerPosition) {
			return getMoves(playerPosition, targetPlayerPosition);
		}
		
		/**
		 * Returns the moves to be done by the player to get from the passed 
		 * position to the passed target position.
		 * <p>
		 * This method doesn't change the board.
		 * 
		 * @param startPlayerPosition   current position of the player
		 * @param targetPlayerPosition  target position of the player
		 * @return array of directions (see {@link DirectionConstants}) the player
		 *  must move to, to reach the target position.<br> 
		 * The array is empty if the current position is the target position.<br>
		 * <code>null</code> is returned when there is no path to the target position. 
		 */
		public int[] getMoves(int startPlayerPosition, final int targetPlayerPosition) {

			int currentPlayerPosition = startPlayerPosition;

			// If the player is already located at the target position there are no moves to be done.
			if (currentPlayerPosition == targetPlayerPosition) {
				return new int[] {};
			}

			// Initialize the array with invalid direction offset 0.
			Arrays.fill(moveDirectionBestPath, 0);
			
			// Initialization of the queue indices.
			int indexToWrite = 0;
			int indexToTake  = 0;

			// Mark the start position as reached by setting an invalid previous direction.
			moveDirectionBestPath[currentPlayerPosition] = START_POSITION_MARKER;

			// Do a breadth first search to find the shortest path to the target position.
			do {
				// Move the player to all four directions.
				for(int directionOffset : DIRECTION_OFFSETS) {

					// Calculate the new player position when moving to the direction.
					int newPlayerPosition = currentPlayerPosition + directionOffset;

					// Check whether the player can move to the new position and has never moved there before.
					if(!isBoxOrWall(newPlayerPosition) && moveDirectionBestPath[newPlayerPosition] == 0) {

						// Check whether the target position has been reached.
						if(newPlayerPosition == targetPlayerPosition) {

							// Copy the path from the start to the target position into an array.
							indexToWrite = queue.length;

							// Go backwards from the target position to the start position and save the path gone.
							do {
								if(directionOffset == BOARD_UP) {
									queue[--indexToWrite] = UP;

								}else if(directionOffset == BOARD_DOWN) {
									queue[--indexToWrite] = DOWN;

								}else if(directionOffset == BOARD_LEFT) {
									queue[--indexToWrite] = LEFT;

								}else if(directionOffset == BOARD_RIGHT) {
									queue[--indexToWrite] = RIGHT;
								}

								// Undo the move.
								newPlayerPosition -= directionOffset; 

								// Get the move direction this new position had been reached. 
								directionOffset = moveDirectionBestPath[newPlayerPosition];

							}while(directionOffset != START_POSITION_MARKER);

							// Copy the path into an array having just the right size and return it.
							return Arrays.copyOfRange(queue, indexToWrite, queue.length);
						}
						
						// Save by going to which direction this new position has been reached.
						moveDirectionBestPath[newPlayerPosition] = directionOffset;

						// Add to queue for further search.
						queue[indexToWrite++] = newPlayerPosition;
					}				
				}

				// Get the next position from the queue.				
				currentPlayerPosition = queue[indexToTake++];

			}while(indexToTake <= indexToWrite); // As long as there are positions in the queue
			
			// Return null since no path to the target position has been found.
			return null;
		}
	}
}