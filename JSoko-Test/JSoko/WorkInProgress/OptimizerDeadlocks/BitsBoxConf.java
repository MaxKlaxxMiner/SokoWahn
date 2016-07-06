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

import java.util.Arrays;


/**
 * This implementation of a box configuration has one bit for each potential
 * box position.  It does not just enumerate those box positions which
 * actually are occupied by a box.
 * This is the preferred implementation where we are not clearly restricted
 * to some small number of boxes (set cardinality).
 * 
 * NB: Instances of this class shall be used as members in collections.
 * 
 * @author Heiner Marxen
 */
public class BitsBoxConf implements Cloneable, BoxConfByElem {

	/**
	 * Our main data: the byte array with one bit set for each box position
	 * occupied by a box.
	 */
	public byte[] bytarr;
	
	/**
	 * Low level constructor: the readily constructed main data is passed in.
	 * No further copying.
	 * @param srcarr box position bit-vector to be used
	 */
	public BitsBoxConf(byte[] srcarr) {
		this.bytarr = srcarr;
	}
	
	@Override
	public Object clone() {
		try {
			// Clone super class.
			BitsBoxConf clone = (BitsBoxConf) super.clone();
			
			// Clone variables of this class.
			if(bytarr != null) {
				clone.bytarr = bytarr.clone();
			}
			return clone;
        } catch (CloneNotSupportedException e) {
            throw new InternalError(); 
        }
	}
	
	@Override
	public boolean equals(Object o) {
		if (o == null) {
			return false;
		}
		if (o instanceof BitsBoxConf) {
			BitsBoxConf oth = (BitsBoxConf)o;
			return Arrays.equals(this.bytarr, oth.bytarr);
		}
		return false;
	}
	
	@Override
	public int hashCode() {
		return Arrays.hashCode(bytarr);
	}
	
	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.optimizer.deadlocks.BoxConfByElem#hasBox(int)
	 */
	@Override
	public boolean hasBox(int boxpos) {
		// FFS: for out of range "boxpos" return false? (instead of exception)
		// FFS info: the class BitSet returns "false" if boxpos is too large, but throws an exception if boxpos is negative?!
		if (bytarr != null) {
			return 0 != (bytarr[boxpos>>>3] & (1 << (boxpos & 07)));
		}
		return false;
	}

	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.optimizer.deadlocks.BoxConfByElem#getCard()
	 */
	@Override
	public short getCard() {
		if (bytarr == null) {
			return 0;
		}
		int bitcount = 0;
		
		for (byte b : bytarr) {
			bitcount += Integer.bitCount(b & 0x0ff);
		}
		return (short) bitcount;
	}
	
	// implement SubsetAskable
}
