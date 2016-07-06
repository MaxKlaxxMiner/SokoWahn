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
package de.sokoban_online.jsoko.boardpositions.iterative;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPositionMoves;


/**
 * Instances of these class are used to store a specific board position (all box positions
 * and the player position).
 *
 * Instances of this class:<ul>
 * <li> are used in a search for a solution
 * <li> store the box and player positions relative to the previous board position
 * <li> are used in an iterative search
 * <li> can store the number of moves
 * </ul>
 */
public final class RelativeBoardPositionMovesIterative
		extends RelativeBoardPositionMoves implements IBoardPositionMovesIterative {

	// During a search for a solution a limit for pushes is set per iteration. This limit
	// limits the number of pushes that may be done for until the level is solved.
	// This limit is stored in this variable representing  the iteration depth this board
	// position is created in.
	private short maximumPushesCurrentIteration = 0;

	/**
	 * Creates the object for storing an relative board position.
	 *
	 * @param board  the board of the current level
	 * @param boxNo		 Number of the pushed box
	 * @param direction	 Direction the box was pushed in.
	 * @param precedingBoardPosition	the board position before the push was done
	 */
	public RelativeBoardPositionMovesIterative(Board board,
			int boxNo, int direction, IBoardPosition precedingBoardPosition) {

		super(board, boxNo, direction, precedingBoardPosition);
	}

	/**
	 * Sets the maximum solution length.
	 * This is a value representing the iteration depth during the search for a solution.
	 * (first all board positions are created that have a maximum solution length of x pushes.
	 * Then all board positions are created with a maximum solution length of x+1, ...
	 *
	 * @param maximumSolutionLength	the maximum solution length to be set
	 */
	@Override
	public void setMaximumSolutionLength(short maximumSolutionLength) {
		maximumPushesCurrentIteration = maximumSolutionLength;
	}

	/**
	 * Returns the maximum solution length (= iteration depth).
	 *
	 * @return	the maximum solution length stored in this board position
	 */
	@Override
	public short getMaximumSolutionLength() {
		return maximumPushesCurrentIteration;
	}
}