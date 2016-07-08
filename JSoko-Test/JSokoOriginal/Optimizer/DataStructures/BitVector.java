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

/**
 * Bit vector for marking reached board positions as visited.
 * <p>
 * Note that this class doesn't check the method parameter for
 * being in a valid range. The bit vector must be instantiated 
 * with a proper size to ensure there no exceptions are thrown.<br>
 * The reason for this is that this method is often used in the
 * optimizer and therefore performance is important.
 */
public class BitVector {
	/**
	 * Array for storing the information which board position
	 * (box positions + player position) has already been reached.
	 * This array is a byte array because a boolean array would use 
	 * one byte for every bit. Hence the bits in the stored bytes 
	 * are addressed by extra methods in this class in order not to waste RAM. 
	 */
	final private byte[] visitedData;
	
	/**
	 * Creates a new set for marking board positions as visited.
	 * 
	 * @param bitCount  number of bits in this vector
	 */
	public BitVector(int bitCount) {
		visitedData = new byte[(bitCount >>> 3) + 1];
	}
	
	/**
	 * Marks the passed board position as visited.
	 * 
	 * @param boardPositionIndex
	 *            index of the board position in the visitedData array
	 */
	public void setVisited(int boardPositionIndex) {
		int bytePosition = (boardPositionIndex >>> 3), bitPosition = (boardPositionIndex & 7);
		visitedData[bytePosition] |= (1 << bitPosition);
	}

	/**
	 * Returns whether the board position (box + player positions) is marked as visited.
	 * 
	 * @param boardPositionIndex
	 *            index of the board position in the visitedData array
	 * @return <code>true</code>board position has already been visited;
	 *         <code>false</code>otherwise
	 */
	public boolean isVisited(int boardPositionIndex) {
		int i = (boardPositionIndex >>> 3), j = (boardPositionIndex & 7);
		return (visitedData[i] & (1 << j)) > 0;
	}	
}