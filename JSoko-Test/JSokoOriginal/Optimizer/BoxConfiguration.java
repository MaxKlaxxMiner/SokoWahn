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
package de.sokoban_online.jsoko.optimizer;

import java.util.Arrays;

/**
 * Stores a specific box configuration. 
 * <p>
 * A box configuration represents the box positions on the board.
 * For every valid box position there is a bit stored representing
 * whether there is a box at that position or not.
 */
public class BoxConfiguration implements Cloneable {

	/** Every bit in this data indicates whether there is a box or not. */
	public final byte[] data;
	
	
	/**
	 * Creates a new object for storing the box positions.
	 *
	 * @param positionsCount  the number of valid positions a box can be located at
	 */
	public BoxConfiguration(int positionsCount) {
		data = new byte[(positionsCount - 1) / 8 + 1];
	}
	
	/**
	 * Creates a new <code>BoxConfiguration</code> by cloning the passed one.
	 *
	 * @param BoxConfiguration  <code>BoxConfiguration</code> to be cloned
	 */
	private BoxConfiguration(BoxConfiguration boxConfiguration) {
		data = boxConfiguration.data.clone();
	}
	
	/**
	 * Removes the box which is located at the passed position.
	 * 
	 * @param boxPosition  position to remove a box
	 */
	public void removeBox(int boxPosition) {
		int byteNo = (boxPosition >>> 3), bitPosition = (boxPosition & 7);
		data[byteNo] &= ~(1 << bitPosition);
	}
	
	/**
	 * Adds a box in at the passed position.
	 * 
	 * @param boxPosition  position to add at box at
	 */
	public void addBox(int boxPosition) {
		int byteNo = (boxPosition >>> 3), bitPosition = (boxPosition & 7);
		data[byteNo] |= (1 << bitPosition);
	}
	
	/**
	 * Removes the box at the old box position and adds it to the new box position.
	 * 
	 * @param oldBoxPosition  old box position
	 * @param newBoxPosition  new box position
	 */
	public void moveBox(int oldBoxPosition, int newBoxPosition) {
		removeBox(oldBoxPosition);
		addBox(newBoxPosition);
	}

	/**
	 * Returns whether there is a box at the passed position.
	 *  
	 * @param boxPosition  the position checked for a box
	 * @return <code>true</code> there is a box at the specified position
	 *   <code>false</code> otherwise
	 */
	public boolean isBoxAtPosition(int boxPosition) {
		int byteNo = (boxPosition >>> 3), bitPosition = (boxPosition & 7);
		return (data[byteNo] & (1 << bitPosition)) != 0;	
	}

	/**
	 * Returns whether the passed box configuration is a subset of this box configuration.
	 * <p>
	 * The passed box configuration must have the same size as this box configuration.
	 * 
	 * @param boxConfiguration  box configuration to be checked for being a subset
	 * 
	 * @return <code>true</code> if the passed box configuration is a subset of
	 *  this box configuration, and <code>false</code> otherwise
	 */
	public boolean hasSubset(BoxConfiguration boxConfiguration) {
		
		// It's a subset, if all its 1-bits survive the ANDing.
		for (int i = 0; i < data.length; i++) {
			if ((data[i] & boxConfiguration.data[i]) != boxConfiguration.data[i]) {
				return false;
			}
		}
		return true;
	}
	
	/**
	 * Returns the number of boxes stored in this box configuration.
	 * 
	 * @return number of boxes
	 */
	public int getBoxCount() {
		
		int boxCount = 0;
		
		// Calculate the number of set bits in all bytes.
		for(byte b : data) {
			while(b != 0) {
				if(b < 0) {
					boxCount++;
				}
				b <<= 1;
			}
		}
		
		return boxCount;
	}
	
	/**
	 * Fills the passed array with the box positions.<br>
	 * If the length of the array is lower than the number
	 * of boxes stored in this box configuration only 
	 * the first "array.length" box positions are stored.
	 * <p>
	 * The positions are filled from lowest position to
	 * highest position.
	 * 
	 * @param boxPositions the array to be filled with box positions
	 */
	public void fillBoxPositions(int[] boxPositions) {
		
		int index = 0;
		int boxPosition = 0;
		
		// Calculate the number of set bits in all bytes.
		for(int i=0; i< data.length; i++) {
			
			int value = data[i] & 0xFF;
			
			int boxPositionInByte = boxPosition;
			while(value != 0) {
				if((value & 1) == 1) {
					boxPositions[index++] = boxPositionInByte;
					if(index == boxPositions.length) {
						return;
					}
				}
				value >>= 1;
				boxPositionInByte++;
			}
			
			boxPosition += 8;
		}
	}
	
	/**
	 * Returns the positions of all boxes of this box configuration as array.
	 * @return array of all box positions
	 */
	public int[] getBoxPositions() {
		int[] boxPositions = new int[getBoxCount()];
		fillBoxPositions(boxPositions);
		return boxPositions;
	}
	
	/**
	 * Returns the hash code value for this box configuration.
	 */
	public int hashCode() {
		int hashValue = 1;
	    for(int indexTmp = data.length; --indexTmp != -1; ) {
	    	hashValue = 223 * (hashValue + data[indexTmp]);
	    }
	    return hashValue;
	}


	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (getClass() != obj.getClass())
			return false;
		BoxConfiguration other = (BoxConfiguration) obj;
		if (!Arrays.equals(data, other.data))
			return false;
		return true;
	}
	
	/**
	 * Returns a clone of this <code>BoxConfiguration</code>.
	 *
	 * @return clone of this <code>BoxConfiguration</code>
	 */
	public Object clone() {
		return new BoxConfiguration(this);
	}
}