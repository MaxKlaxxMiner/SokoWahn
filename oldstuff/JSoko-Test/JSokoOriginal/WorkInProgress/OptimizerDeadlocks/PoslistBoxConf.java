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
 * This implementation of a box configuration enumerates the box positions
 * one by one (instead of enumerating one bit for each potential box position),
 * and hence tends to be most useful for box deadlock configurations,
 * which normally contain a very limited number of boxes.
 * 
 * NB: Instances of this class shall be used as members in collections.
 * 
 * @author Heiner Marxen
 */
public class PoslistBoxConf
		implements Cloneable, BoxConfByElem, SubsetAskable
{

	/**
	 * Our main data: a sorted list of box positions.
	 * Sorting must be strictly increasing (without duplicates).
	 */
	public short[] posarr;
	
	/**
	 * Low level constructor: the readily constructed main data is passed in.
	 * No further sorting or copying.
	 * 
	 * @param srcarr sorted box position list to be used
	 */
	public PoslistBoxConf(short[] srcarr) {
		this.posarr = srcarr;
	}
	
	@Override
	public Object clone() {
		short[] newarr = ((posarr != null) ? posarr.clone() : null);
		return new PoslistBoxConf(newarr);
	}
	
	@Override
	public boolean equals(Object o) {
		if (o == null) {
			return false;
		}
		if (o instanceof PoslistBoxConf) {
			PoslistBoxConf oth = (PoslistBoxConf)o;
			return Arrays.equals(this.posarr, oth.posarr);
		}
		// FFS/hm BitsBoxConf?
		return false;
	}
	
	@Override
	public int hashCode() {
		return Arrays.hashCode(posarr);
	}
	
	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.optimizer.deadlocks.BoxConfByElem#hasBox(int)
	 */
	public boolean hasBox(int boxpos) {
		if (posarr != null) {
			for (short mypos : posarr) {
				if (mypos == boxpos) {
					return true;
				}
			}
		}
		return false;
	}

	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.optimizer.deadlocks.BoxConfByElem#getCard()
	 */
	public short getCard() {
		if (posarr != null) {
			return (short) posarr.length;
		}
		return 0;
	}
	
	public boolean hasSubsetIn(BoxConfByElem bigboxconf, int involvedbox) {
		if (posarr != null) {
			for (short mybox : posarr) {
				if ( ! bigboxconf.hasBox(mybox)) {
					return false;
				}
			}
			// All our boxes are found in "bigboxconf": yes, we are a subset
			return true;
		}
		// did not detect any subset ...
		return false;
	}
}
