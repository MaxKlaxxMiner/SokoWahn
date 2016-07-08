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
package de.sokoban_online.jsoko.optimizer.GUI;

import de.sokoban_online.jsoko.gui.BoardDisplay;
import de.sokoban_online.jsoko.leveldata.Level;


/**
 * This class is used to display a level graphically in the remodel solver GUI.
 * <p>
 * The level may be modified in the remodel solver. Hence, this class inherits the display
 * functionality from the BaordDisplay class and implements the additional modifying functionality.
 */
@SuppressWarnings("serial")
public class BoardDisplayRemodelSolver extends BoardDisplay {

	/**
	 * Creates a new object for displaying the board of the passed level.
	 *
	 * @param levelToBeDisplayed the <code>Level</code> to be displayed
	 */
	public BoardDisplayRemodelSolver(Level levelToBeDisplayed) {
		super(levelToBeDisplayed);
	}

	/**
	 * This method is called when the user pressed the mouse on board element in the GUI.
	 *
	 * @param position  Position in the board
	 */
	protected void mousePressedAt(int position) {
		if(board.isEmptySquare(position) && board.playerPosition != position) {
			board.setWall(position);
		} else
			if(board.isWall(position)) {
				board.removeWall(position);
			}

		recalculateGraphicSizes();
		repaint();
	}
}
