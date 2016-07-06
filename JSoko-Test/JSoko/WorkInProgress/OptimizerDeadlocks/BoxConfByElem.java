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
package de.sokoban_online.jsoko.workInProgress.optimizerDeadlocks;

/**
 * This interface shall help to abstract from different implementations
 * for box configurations.  Typical implementations are
 * <ol>
 *  <li> a byte[] with one bit for each potential box position
 *  <li> a short[] containing the sorted list of box positions
 * </ol>
 * Both should implement the service indicated here.
 * 
 * @author Heiner Marxen
 */
public interface BoxConfByElem {
	/**
	 * Tells whether the indicated square has a box (is an element).
	 * 
	 * @param boxpos the square to be inspected
	 * @return whether there is a box on that square
	 */
	boolean hasBox(int boxpos);
	
	/**
	 * Computes the number of boxes in this box configuration.
	 * This may be a trivial computation, or may be not,
	 * depending on the underlying implementation.
	 * 
	 * @return cardinality of the set of boxes
	 */
	short getCard();
}
