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
package de.sokoban_online.jsoko.board;

/**
 * This class offers static methods to operate on numeric direction values.
 * Inside this class we know the current encoding of the direction values.
 * Other classes should not depend on the exact encoding, but rather
 * depend on the abstract services of this class.
 * <p>
 * While this encoding encapsulation currently (2011-08-13) is not complete,
 * and we also do not have any plans to change our encoding, we still aim
 * at an encapsulation within this single class... since it is good style.
 * 
 * @author Heiner Marxen
 */
public final class Directions
		implements DirectionConstants
{

	/**
	 * Checks whether the passed value is a valid direction value. i.e. one of
	 * {@link DirectionConstants#UP},
	 * {@link DirectionConstants#DOWN},
	 * {@link DirectionConstants#RIGHT} and
	 * {@link DirectionConstants#LEFT}.
	 * 
	 * @param dir the direction value to be checked
	 * @return whether it is a valid direction value
	 */
	public static final boolean isDirection(int dir) {
		return (0 <= dir) && (dir < DIRS_COUNT);
	}
	
//	{ assert (LEFT^RIGHT) == (UP^DOWN); }
	
	/**
	 * Computes the opposite direction.
	 * Returns e.g. {@code LEFT} for {@code RIGHT}.
	 * If the argument is not a valid direction value,
	 * the result is undefined.
	 * 
	 * @param dir direction value to be reversed
	 * @return the opposite direction
	 */
	public static final int getOppositeDirection(int dir) {
		return dir ^ (LEFT ^ RIGHT);
	}
	
	/**
	 * Returns whether the two passed values are valid directions
	 * and also are opposite to each other.
	 * 
	 * @param dir1 first  value to check and compare
	 * @param dir2 second value to check and compare
	 * @return whether the values are directions and opposite to each other
	 * @see #areDirectionsOpposite(int, int)
	 */
	public static final boolean isValidDirectionsAndOpposite( int dir1, int dir2 ) {
		if (!isDirection(dir1) || !isDirection(dir2)) {
			return false;
		}
		return (dir1 ^ dir2) == (LEFT^RIGHT);
	}
	
	/**
	 * Returns whether the two passed directions are opposite to each other.
	 * 
	 * @param dir1 first  valid direction to compare
	 * @param dir2 second valid direction to compare
	 * @return whether the (valid) directions are opposite to each other
	 * @see #isValidDirectionsAndOpposite(int, int)
	 */
	public static final boolean areDirectionsOpposite( int dir1, int dir2 ) {
		return (dir1 ^ dir2) == (LEFT^RIGHT);
	}
	
	/**
	 * For a direction computes another direction such that it is orthogonal
	 * to the first direction, and when we repeat this computation with
	 * the resulting direction we yield the first direction, again.
	 * I.e. we switch between the 2 values of a pair of orthogonal directions.
	 * <p>
	 * If the passed value is not a valid direction value, the result is
	 * not specified, but it is a valid direction value.
	 * 
	 * @param dir the direction to be mapped
	 * @return the orthogonally paired direction
	 */
	public static final int getOrthogonalDirection( int dir ) {
		// We could use (dir+2) instead of (dir^2)
		return (dir ^ 02) & 03;
	}
	
	/**
	 * Returns, whether the passed value is a legal axis value, i.e. one of
	 * of {@link DirectionConstants#AXIS_VERTICAL}
	 * and {@link DirectionConstants#AXIS_HORIZONTAL}.
	 * 
	 * @param axis value to be checked for validity
	 * @return whether the value is a valid axis value
	 */
	public static final boolean isAxis( int axis ) {
		return (0 <= axis) && (axis < AXES);
	}
	
	/**
	 * Converts a direction into the corresponding axis,
	 * e.g. maps {@code RIGHT} to {@code AXIS_HORIZONTAL}.
	 * <p>
	 * If the passed value is not a valid direction,
	 * the result is not specified, but it is a valid axis value.
	 * 
	 * @param dir direction to convert
	 * @return the axis of the direction
	 */
	public static final int getAxisOfDirection( int dir ) {
		return (dir >>> 1) & 01;
	}
	
	/**
	 * Converts an axis value into one of the two corresponding direction
	 * values, the smaller of both direction candidates.
	 * <p>
	 * If the passed value is not a valid axis value,
	 * the result is not specified, but it is a valid direction value.
	 *  
	 * @param axis the axis value to be mapped to a base direction
	 * @return base direction of the passed axis
	 */
	public static final int getDirectionOfAxis( int axis ) {
		return (axis & 01) << 1;
	}
}
