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
package de.sokoban_online.jsoko.optimizer.dataStructures;

import de.sokoban_online.jsoko.optimizer.Optimizer;



/**
 * Queue for storing player positions.<br>
 * The queue only adds new player positions if they haven't been added before.<br>
 * The first player position must be added using the method 
 * {@link #addFirst(int, int)} to initialize the queue.
 * <p>
 * This queue must be used in a breadth first search, because it also counts 
 * the moves done by the player to reach specific positions.
 * 
 * This class is not thread safe.
 */
public class PlayerPositionsQueue {
	
	/** Marker for the end of a moves depth. */
	private final int END_OF_MOVES_DEPTH = -1;
	
	/** Squares reached by the player are marked as "visited". */
	private final int[] visited;
	
	/** Storage for storing all positions reached by the player. */
	private final int[] reachedPositions;
	
	private int visitedMarker = 0; // Marker for marking a square as visited
	private int indexToTake   = 0; // Index in the reached positions queue
	private int indexToAdd    = 0; // Index in the reached positions queue
	
	/** Moves count of the positions taken out of this queue using {@link #remove()}. */
	public int movesDepth = 0;
				
	/**
	 * Queue for storing player positions and calculating the
	 * moves depth of every position.
	 * <p>
	 * Note: the moves depth is only correctly calculated if this
	 * method is used in a breath first search!
	 * 
	 * @param reachablePlayerSquaresCount number of reachable squares of the player
	 */
	public PlayerPositionsQueue (int reachablePlayerSquaresCount) {
		visited 		 = new int[reachablePlayerSquaresCount];
		reachedPositions = new int[2*reachablePlayerSquaresCount];
	}
	
	
	/**
	 * Adds the first position reached by the player and the 
	 * corresponding moves depth.
	 * <p>
	 * This method initializes this queue and must be called when
	 * a new breadth first search for calculating the player reachable
	 * squares has been started.
	 * 
	 * @param playerPosition  position reached by the player
	 * @param startMovesDepth  moves depth in which the position has been reached
	 */
	public void addFirst(int playerPosition, int startMovesDepth) {
		
		// A new marker value is used for every new round.
		visitedMarker++;
		
		indexToTake = 0;
		indexToAdd  = 0;
		movesDepth  = startMovesDepth;
		
		reachedPositions[indexToAdd++] = playerPosition;  // Add the new player position
		visited[playerPosition] = visitedMarker; 		  // Mark player position as visited
		
		reachedPositions[indexToAdd++] = END_OF_MOVES_DEPTH;				
	}
	
	/**
	 * Adds the passed position to this queue if it hasn't been
	 * reached before.
	 * 
	 * @param playerPosition  position to be added
	 * @return <code>true</code> when the position has been added;
	 * <code>false</code> otherwise
	 */
	public boolean addIfNew(int playerPosition) {
		if(visited[playerPosition] != visitedMarker) {
			reachedPositions[indexToAdd++] = playerPosition;  // Add the new player position
			visited[playerPosition] = visitedMarker; 		  // Mark player position as visited
			return true;
		}
		
		return false;
	}
	
	/**
	 * Removes and returns the next player position from this queue.
	 * 
	 * @return  the player position or {@link Optimizer#NONE} if this queue is empty
	 */
	public int remove() {
		
		int playerPosition = reachedPositions[indexToTake++];	

		if(playerPosition == END_OF_MOVES_DEPTH) {			
			if(indexToTake == indexToAdd)
				return Optimizer.NONE;
			movesDepth++;					  // calculate the new moves depth
			reachedPositions[indexToAdd++] = END_OF_MOVES_DEPTH;
			return reachedPositions[indexToTake++];	
		}
		return playerPosition;
	}
}