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
package de.sokoban_online.jsoko.optimizer.AllMetricsOptimizer;

import java.util.Comparator;

import de.sokoban_online.jsoko.utilities.Utilities;

/**
 * A board position used in the optimizer to save the data of one board position.<br>
 * This class is immutable.<br>
 */
public class BoardPosition {

	public final int moves;
	public final int pushes;
	public final int boxLines;
	public final int boxChanges;
	public final int pushingSessions;
	public final int boxConfigurationIndex;
	public final int playerPosition;
		
	/**
	 * Creates a new immutable {@code OptimizerBoardPosition} used in the optimizer
	 * for storing the data of a reached board position.
	 * 
	 * @param moves	 number of moves 
	 * @param pushes number of pushes
	 * @param boxLines number of box lines
	 * @param boxChanges number of box changes
	 * @param pushingSessions number of pushing sessions
	 * @param boxConfigurationIndex index of the box configuration in the box configuration storage
	 * @param playerPosition player position
	 */
	public BoardPosition(int moves, int pushes, int boxLines, int boxChanges, int pushingSessions, int boxConfigurationIndex, int playerPosition) {
		this.moves  = moves;
		this.pushes = pushes;
		this.boxLines = boxLines;
		this.boxChanges = boxChanges;
		this.pushingSessions = pushingSessions;
		this.boxConfigurationIndex = boxConfigurationIndex;
		this.playerPosition = playerPosition;
	}
	

	/** 
	 * Comparator for comparing two {@code OptimizerBoardPosition}s regarding 
	 * their metrics in the following order:<br>
	 * 1. {@link #pushes}
	 * 2. {@link #moves}
	 * 3. {@link #boxLines}
	 * 4. {@link #boxChanges}
	 * 5. {@link #pushingSessions}
	 * <p>
	 *  If both board positions are {@code null} 0 is returned. If only one 
	 *  board position is {@code null} then the other is considered lower. 
	 * <p>
	 * Note: this comparator has a natural ordering that is inconsistent with equals.
	 */
	public static final Comparator<BoardPosition> PUSHES_MOVES_COMPARATOR =
		new Comparator<BoardPosition>() {
		
		/**
		 * Returns whether this board position has lower metrics (pushes, moves, ...),
		 * equal metrics or higher metrics than the passed one.<br>
		 * The comparison compares the metrics in the following order:
		 * 1. {@link #pushes}
		 * 2. {@link #moves}
		 * 3. {@link #boxLines}
		 * 4. {@link #boxChanges}
		 * 5. {@link #pushingSessions}
		 * <p>
		 * If both board positions are {@code null} 0 is returned. If only one 
		 * board position is {@code null} then the other is considered lower. 
		 * <p>
		 * Note: this class has a natural ordering that is inconsistent with equals.<br>
		 * If any of the board positions is {@code null} then 0 is returned.
		 * 
		 * @param boardPosition1  board position to be compared with the other passed board position
		 * @param boardPosition2  board position to be compared with the other passed board position
		 * @return <code>-1</code> if this board position has lower metrics that the other,<br>
		 *         <code>0</code> if this board position has equal metrics than the other,<br>
		 *         <code>1</code> if this board position has higher metrics that the other
		 */
		public int compare(BoardPosition boardPosition1, BoardPosition boardPosition2) {
			
			// If both of the board positions are null then they are equal.
			if(boardPosition1 == null && boardPosition2 == null) {
				return 0;
			}
			
			// If one of them is null then the other is "better/lower".
			if(boardPosition1 == null) return +1;
			if(boardPosition2 == null) return -1;
			
			// Return the compare result.
			return Utilities.intComparePairs(
					boardPosition1.pushes, 			boardPosition2.pushes,
					boardPosition1.moves, 			boardPosition2.moves,
					boardPosition1.boxLines, 		boardPosition2.boxLines,
					boardPosition1.boxChanges, 		boardPosition2.boxChanges,
					boardPosition1.pushingSessions, boardPosition2.pushingSessions);
		}
	};
		
	/** 
	 * Comparator for comparing two {@code OptimizerBoardPosition}s regarding 
	 * their metrics in the following order:<br>
	 * 1. {@link #moves}
	 * 2. {@link #pushes}
	 * 3. {@link #boxLines}
	 * 4. {@link #boxChanges}
	 * 5. {@link #pushingSessions}
	 * <p>
	 *  If both board positions are {@code null} 0 is returned. If only one 
	 *  board position is {@code null} then the other is considered lower. 
	 * <p>
	 * Note: this comparator has a natural ordering that is inconsistent with equals.
	 */
	public static final Comparator<BoardPosition> MOVES_PUSHES_COMPARATOR =
		new Comparator<BoardPosition>() {
		
		/**
		 * Returns whether this board position has lower metrics (moves, pushes, ...),
		 * equal metrics or higher metrics than the passed one.<br>
		 * The comparison compares the metrics in the following order:
		 * 1. {@link #moves}
		 * 2. {@link #pushes}
		 * 3. {@link #boxLines}
		 * 4. {@link #boxChanges}
		 * 5. {@link #pushingSessions}
		 * <p>
		 * If both board positions are {@code null} 0 is returned. If only one 
		 * board position is {@code null} then the other is considered lower. 
		 * <p>
		 * Note: this class has a natural ordering that is inconsistent with equals.<br>
		 * If any of the board positions is {@code null} then 0 is returned.
		 * 
		 * @param boardPosition1  board position to be compared with the other passed board position
		 * @param boardPosition2  board position to be compared with the other passed board position
		 * @return <code>-1</code> if this board position has lower metrics that the other,<br>
		 *         <code>0</code> if this board position has equal metrics than the other,<br>
		 *         <code>1</code> if this board position has higher metrics that the other
		 */
		public int compare(BoardPosition boardPosition1, BoardPosition boardPosition2) {
			
			// If both of the board positions are null then they are equal.
			if(boardPosition1 == null && boardPosition2 == null) {
				return 0;
			}
			
			// If one of them is null then the other is "better/lower".
			if(boardPosition1 == null) return +1;
			if(boardPosition2 == null) return -1;
			
			// Return the compare result.
			return Utilities.intComparePairs(
					boardPosition1.moves, 			boardPosition2.moves,
					boardPosition1.pushes, 			boardPosition2.pushes,
					boardPosition1.boxLines, 		boardPosition2.boxLines,
					boardPosition1.boxChanges, 		boardPosition2.boxChanges,
					boardPosition1.pushingSessions, boardPosition2.pushingSessions);
		}
	};
	
	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + boxConfigurationIndex;
		result = prime * result + playerPosition;
		return result;
	}

	/**
	 * Compares this object to the specified object. 
	 * The result is true if and only if the argument is not null and is an 
	 * OptimizerBoardPosition object that has equal values comparing these fields:
	 * boxConfigurationIndex and playerPosition.
	 * 
	 * @param obj  object to be compared
	 * @return <code>true</code> if the passed object is identical to this object
	 *   <code>false</code> otherwise
	 */
	public boolean equals(Object obj) {
		
		if (this == obj) {
			return true;
		}
		
		if (obj == null || getClass() != obj.getClass()) {
			return false;
		}
		
		BoardPosition other = (BoardPosition) obj;
		
		// The board positions are equal, when the boxes are on the same
		// positions, the player is on the same position and the push
		// has been done to the same direction (since in the push direction
		// there now is a box it's sufficient to check the axis of the push).
		if (boxConfigurationIndex != other.boxConfigurationIndex ||
		    playerPosition != other.playerPosition) {
			return false;
		}
		
		return true;
	}
	
}