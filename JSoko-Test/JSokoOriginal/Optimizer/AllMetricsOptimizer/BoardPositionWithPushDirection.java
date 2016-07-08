/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2013 by Matthias Meger, Germany
 * 
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *	
 *a	This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
package de.sokoban_online.jsoko.optimizer.AllMetricsOptimizer;

import de.sokoban_online.jsoko.board.Directions;

/**
 * A board position used in the optimizer to save the data of one board position.<br>
 * This class is immutable.<br>
 * <p>
 * Note: two OptimizerBoardPositions are equal, when their following variables are equal:
 * boxConfigurationIndex, playerPosition AND pushDirection. 
 */
public class BoardPositionWithPushDirection extends BoardPosition {

	/** Direction the box has been pushed to. */
	public final byte pushDirection;
	
	/** Previous board position before the push. */
	public final BoardPositionWithPushDirection previous;

	
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
	 * @param pushDirection direction of the push
	 * @param previous previous {@code OptimizerBoardPosition}
	 */
	public BoardPositionWithPushDirection(int moves, int pushes, int boxLines, int boxChanges, int pushingSessions, int boxConfigurationIndex, int playerPosition, int pushDirection, BoardPositionWithPushDirection previous) {
		super(moves, pushes, boxLines, boxChanges, pushingSessions, boxConfigurationIndex , playerPosition);
		this.pushDirection = (byte) pushDirection;
		this.previous = previous;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + boxConfigurationIndex;
		result = prime * result + playerPosition;
		result += pushDirection == Directions.AXIS_HORIZONTAL ? 17 : 23;
		return result;
	}

	/**
	 * Compares this object to the specified object. 
	 * The result is true if and only if the argument is not null and is an 
	 * OptimizerBoardPosition object that has equal values comparing these fields:
	 * boxConfigurationIndex, playerPosition AND pushDirection.
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

		BoardPositionWithPushDirection other = (BoardPositionWithPushDirection) obj;

		// The board positions are equal, when the boxes are on the same
		// positions, the player is on the same position and the push
		// has been done to the same direction.
		if (boxConfigurationIndex != other.boxConfigurationIndex ||
				   playerPosition != other.playerPosition ||
				    pushDirection != other.pushDirection) {
			return false;
		}

		return true;
	}
}