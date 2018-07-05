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

import de.sokoban_online.jsoko.boardpositions.IBoardPosition;


/**
 * Interface for all search board positions that are used within an iterative search.
 */
public interface IBoardPositionIterative extends IBoardPosition {
	
    /**
     * Sets the maximum solution length. 
     * This is a value representing the iteration depth during the search for a solution.
     * (first all board positions are created that have a maximum solution length of x pushes.
     * Then all board positions are created with a maximum solution length of x+1, ...
     *  
     * @param maximumSolutionLength	the maximum solution length to be set
     */
	abstract void setMaximumSolutionLength(short maximumSolutionLength);
	
	/**
     * Returns the maximum solution length (= iteration depth).
     * 
     * @return	the maximum solution length stored in this board position
     */
	abstract short getMaximumSolutionLength();	
}
