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
package de.sokoban_online.jsoko.workInProgress.arrayWorks;

/**
 * This class collects several static helpers mostly useful for
 * the package "arrayWorks".
 * 
 * @author Heiner Marxen
 */
public final class ArrayHelp {

	/**
	 * Computes the hashCode of a {@code long} value.
	 * Equivalent to {@code (new Long(x).hashCode())}.
	 * <p>
	 * For efficiency we here inline the code for the official computation
	 * from the documentation of {@link Long#hashCode()}.
	 * 
	 * @param x value of which to compute the hash code
	 * @return the hash code of x
	 * @see Long#hashCode()
	 */
	public static int hashOfLong(long x) {
		return (int) ( x ^ (x >>> 32) );
	}

	//========================================================================
	//	Unsigned Matters

	/**
	 * The long value with exactly bit 63 set to 1 (the sign bit).
	 * This is also the most negative value.
	 */
	public static final long LB63 = (1L << 63);

	/**
	 * Compute a long value (a mask) with some low bits set to "one".
	 * 
	 * @param nbits  number of low bits to be set
	 * @return       mask value with {@code nbits} low 1s
	 */
	public static final long low1s(int nbits) {
		if (nbits <= 0) {
			return 0L;
		}
		if (nbits >= 64) {
			return ~0L;
		}
		return (1L << nbits) - 1L;
	}

	/**
	 * Compare the two {@code long} values, but interpreted as
	 * <em>unsigned</em> 64-bit numbers.
	 * 
	 * @param x  first  value to compare
	 * @param y  second value to compare
	 * @return {@code -1} if {@code x<y}, {@code 0} if {@code x==y}
	 *         and {@code +1} if {@code x>y} 
	 * @see Long#compareTo(Long)
	 */
	public static final int cmpU(long x, long y) {
//		if ((x ^ y) < 0) {
//			// x and y have different sign bit
//			return (x < 0) ? +1 : -1;
//		} else {
//			// x and y have the same sign bit
//			if (x != y) {
//				return (x < y) ? -1 : +1;
//			}
//			return 0;
//		}
		if (x != y) {
			return (((x^LB63) < (y^LB63)) ? -1 : +1);
		}
		return 0;
	}

	public static final boolean geU(long x, long y) {
		//return cmpU(x, y) >= 0;
		return (x^LB63) >= (y^LB63);
	}

	public static final boolean leU(long x, long y) {
		//return cmpU(x, y) <= 0;
		return (x^LB63) <= (y^LB63);
	}
}
