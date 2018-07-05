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

import java.util.Arrays;
import java.util.Random;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.resourceHandling.Settings;



/**
 * This class saves a board position, that means every box position
 * and the player position.
 * <p>
 * This data is push oriented, and so the player position is saved using the
 * most top-left position the player can reach.  That way the exact player
 * position is replaced by a unique representative of the area reached
 * by the player.
 * <p>
 * This class is only to be used by class "PackingSequenceSearch".
 */
public class BoardPositionPackingSequence implements Comparable<BoardPositionPackingSequence>, Cloneable {

    /** Random ints for Zobrist hash value calculation. */
    private static int[] zobristValues = null;

	/** The hash value of this board position. */
	private int hashValue;

	/** Positions of the boxes and the player. */
	private int[] positions;

	/** [3] = true, means the goal with number 3 has been reached by a box. */
	private boolean[] reachedGoals;

	/**
	 * Number of the pulled box. The boxes are numbered when they are all
	 * located at a goal.
	 * Hence, the box numbers differ from the numbers in the "forward" game.
	 */
	private int pulledBoxNo;

	/** Start position of the pulled box. */
	private int startPosition;
	/** Target position of the pulled box. */
	private int targetPosition;

	/**
	 * true = the push has to be done from start to target position
	 * to ensure the packing sequence is correct.
	 * false = a box from any position can be pushed to the target position.
	 */
	private boolean isPushForced = false;

	/** The preceding board position of this board position. */
	private BoardPositionPackingSequence precedingBoardPosition;

	/** The relevance value of this board position for storing it in the
	 *  priority queue. */
	private int relevanceValue = 0;


	/**
	 * Constructor for cloning.
	 */
	private BoardPositionPackingSequence() {}

	/**
	 * Creates an object holding all box positions and the player position.
	 *
	 * @param board  the board containing the current board position
	 */
	public BoardPositionPackingSequence(Board board){
		this(board, -1, -1, -1, false, new boolean[board.boxCount], null);
	}


	/**
	 * Creates an object holding all box positions and the player position.
	 *
	 * @param board  the board containing the current board position
	 * @param pulledBoxNo  the number of the pulled box
	 * @param startPosition the position of the box before the pull
	 * @param targetPosition the position of the box after the pull
	 * @param isForced <code>true</code> means the pull must be done from start to target position, <code>false</code> pull from any position to target position
	 * @param reachedGoals  boolean array where all goals reached by a box are marked with <code>true</code>
	 * @param precedingBoardPosition  the preceding board position
	 */
	public BoardPositionPackingSequence(Board board, int pulledBoxNo, int startPosition, int targetPosition, boolean isForced, boolean[] reachedGoals, BoardPositionPackingSequence precedingBoardPosition){

		// Array for all box and positions and the player position.
		positions = new int[board.boxCount + 1];  // +1 for player position

		// Array for storing which box has reached which goal.
		this.reachedGoals = reachedGoals;

		// Save the number of the pulled box (either it has been pulled
		// one square or it has been pulled to a goal).
		this.pulledBoxNo = pulledBoxNo;

		this.isPushForced = isForced;

		// Save the start and target position of the pull.
		this.startPosition  = startPosition;
		this.targetPosition = targetPosition;

		// Save positions of all boxes.
        for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
            positions[boxNo] = board.boxData.getBoxPosition(boxNo);
        }

		// To be able to identify equal board positions the top-left player position is stored.
		positions[board.boxCount] = board.playersReachableSquares.getPlayerPositionTopLeft();

		// Set the board position that has been on the board before the current board position has been reached.
		this.precedingBoardPosition = precedingBoardPosition;

		// Calculate the hash value of this board position.
		// FFS/hm: incremental zobrist calculation?
		calculateHashValue();
	}

	/**
     * Calculates the hash value for this board position.
     */
    private void calculateHashValue() {

    	 // Fill the Zobrist values if they aren't filled yet.
        if(zobristValues == null) {
        	zobristValues = new int[Settings.MAXIMUM_BOARDSIZE*Settings.MAXIMUM_BOARDSIZE];
        	Random randomGenerator = new Random(42);
        	for(int i=zobristValues.length; --i != -1;) {
        		zobristValues[i] = randomGenerator.nextInt();
        	}

//        	for(int i=0; i<zobristValues.length; i++) {
//        		for(int j=i+1; j<zobristValues.length; j++) {
//        			if(zobristValues[i] == zobristValues[j]) {
//        				System.out.println("bad zobrist");
//        			}
//        		}
//        	}
        }

        // Calculate the hash value for this board position.
        for(int index=positions.length; --index != -1; ) {
        	hashValue ^= zobristValues[positions[index]];
        }
    }

	/**
	 * Returns the positions of the boxes and the player position in an array.
	 *
	 * @return array containing the board positions followed by the player position
	 */
	public int[] getPositions(){
		return positions;
	}

	/**
	 * Returns a boolean array indicating whether a specific goal has been reached by a box.
	 *
	 * @return a boolean array indicating whether a specific goal has been reached by a box
	 */
	public boolean[] getReachedGoalsStatus(){
		return reachedGoals;
	}


	/**
	 * Sets the box positions and the player position.
	 *
	 * @param positions positions of the boxes and the player
	 */
	public void setPositions(int[] positions){
		this.positions = positions;
	}


	/**
	 * Returns the player position of this board position.
	 *
	 * @return the player position
	 */
	public int getPlayerPosition() {
		return positions[positions.length-1];
	}

	/**
	 * Returns the number of the moved box.
	 *
	 * @return the number of the moved box
	 */
	public int getPulledBoxNumber() {
		return pulledBoxNo;
	}

	/**
	 * Returns the start position of the pulled box.
	 *
	 * @return the position of the box before it was pulled.
	 */
	public int getStartBoxPosition() {
		return startPosition;
	}

	/**
	 * Returns the target position of the pulled box.
	 *
	 * @return the position of the box after it was pulled.
	 */
	public int getTargetBoxPosition() {
		return targetPosition;
	}


    /**
     * Returns the preceding board position of this board position.
     *
     * @return the preceding board position of this board position
     */
    public BoardPositionPackingSequence getPrecedingBoardPosition() {
    	return precedingBoardPosition;
    }

	 /**
     * Returns the relevance of this board position for the search.
     *
     * @return the relevance
     */
    int getRelevance() {
    	return relevanceValue;
    }

	/**
	 * Sets the relevance of this board position for the search.
	 *
	 * @param relevanceValue the relevance to set (the higher the more relevant)
	 */
    void setRelevance(int relevanceValue) {
    	this.relevanceValue = relevanceValue;
    }

	/**
	 * Compares both board positions. Note: lower values means high priority
	 * in the <code>PriorityQueue</code>.
	 */
	@Override
	public int compareTo(BoardPositionPackingSequence boardPosition) {
		return boardPosition.getRelevance() - getRelevance();
	}


	/**
	 * Returns whether this board position is equal to the passed one.
	 *
	 * @param boardPositionToCompare  board position to be checked for being equal
	 * @return <code>true</code> if both board positions are equal,
	 * or<br> <code>false</code> otherwise
	 */
	@Override
	public boolean equals(Object boardPositionToCompare) {

		if(boardPositionToCompare == null || boardPositionToCompare.getClass() != this.getClass()) {
			return false;
		}

		BoardPositionPackingSequence boardPosition = (BoardPositionPackingSequence) boardPositionToCompare;

		// Different hash value means different board position.
		if(hashValue != boardPosition.hashValue) {
			return false;
		}

		  // Compare the player positions.
        if(getPlayerPosition() != boardPosition.getPlayerPosition()) {
            return false;
        }

        // Count the boxes having a 0 position (= removed).
        int null1 = 0;
        int null2 = 0;
        for(int boxNo=positions.length-1; --boxNo != -1; ){
        	if (positions[boxNo] == 0) {
				null1++;
			}
        	if (boardPosition.positions[boxNo] == 0) {
				null2++;
			}
        }
        // If the number of removed boxes isn't equal the board positions aren't equal either.
        if (null1 != null2) {
            return false;
        }

        // All boxes on the board must have the same positions.
        int counter2;
        for(int counter = positions.length-1; --counter != -1; ) {
            for(counter2 = positions.length-1; --counter2 != -1; ){
                if(positions[counter] == boardPosition.positions[counter2]) {
					break;
				}
            }
            if(counter2 == -1) {
                return false;
            }
        }

        // Now it only depends on the reached goals whether the board positions are equal or not.
		return Arrays.equals(reachedGoals, boardPosition.reachedGoals);
	}

	@Override
	public int hashCode() {
		return hashValue;
	}

	 /* (non-Javadoc)
     * @see de.sokoban_online.jsoko.boardpositions.BoardPosition#clone()
     */
    @Override
	final public Object clone() {
    	BoardPositionPackingSequence clone = new BoardPositionPackingSequence();
    	clone.setPositions(getPositions());

    	return clone;
    }


	/**
	 * Returns whether the push is a forced push, that means a box
	 * must be pushed from the start position to the target position.
	 *
	 * @return <code>true</code> if it is a forced push,
	 * 		   <code>false</code> otherwise
	 */
	public boolean isForcedPush() {
		return isPushForced;
	}
}
