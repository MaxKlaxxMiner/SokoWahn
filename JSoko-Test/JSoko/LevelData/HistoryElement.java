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
package de.sokoban_online.jsoko.leveldata;

/**
 * An element in a history storing in which direction the player has moved 
 * and - if any - which box has been pushed due to this move.
 */
final public class HistoryElement {

	/** Constants for "no box has been pushed". */
	public static final int NONE = -1;
	
	/** The direction of the movement */
	public byte direction;

	/** The number of the pushed box. */
	public short pushedBoxNo;


	/**
	 * Creates a HistoryElement from the given data.
	 * 
	 * @param direction			direction into which the player moved
	 * @param pushedBoxNo		number of the box pushed by the movement
	 */
	public HistoryElement(int direction, int pushedBoxNo) {
		this.direction = (byte) direction;
		this.pushedBoxNo = (short) pushedBoxNo;
	}
	
	/**
	 * Creates a HistoryElement for a move in the given direction
	 * and sets "no box pushed".
	 * 
	 * @param direction direction into which the player moved
	 */
	public HistoryElement(int direction) {
		this(direction, NONE);
	}

	@Override
	public boolean equals(Object compareObject) {
		if(this == compareObject)
			return true;

		if(compareObject == null || getClass() != compareObject.getClass())
			return false;

		// Cast the passed object to "HistoryElement".
		final HistoryElement movementElement = (HistoryElement) compareObject;

		return direction == movementElement.direction && pushedBoxNo == movementElement.pushedBoxNo;
	}

	@Override
	public int hashCode() {
		return (pushedBoxNo << 8) + direction; 
	}

	/**
	 * Returns if a box has been pushed.
	 * 
	 * @return true = A box has been pushed, false = no box has been pushed
	 */
	public boolean isPush() {
		return pushedBoxNo != NONE;
	}
}