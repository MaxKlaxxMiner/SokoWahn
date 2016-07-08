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
 * Here we define numeric constants for the directions,
 * that make up the geometry of a de.sokoban_online.jsoko board.
 * These constants shall be used consistently throughout the program.
 * Any class (or interface) can import these definitions
 * by an {@code implements} clause.
 * <p>
 * Direction values are coded compactly {@code 0 <= dir < DIRS},
 * and can be used as array index.
 * All direction values together with {@code #NO_DIR} fit into a {@code byte}.
 * <p>
 * The currently implemented direction model is "UDLR":<pre>
 *    U            0
 *    |            |
 * L -+- R  ==  2 -+- 3
 *    |            |
 *    D            1</pre>
 * <p>
 * The numeric values must not be changed.
 * They are used implicitly, sometimes, e.g. as index into arrays.
 * 
 * @author Heiner Marxen
 * @see Directions
 */
public interface DirectionConstants {

	/** Constant for the direction "up". */
	public static final byte UP    = 0;

	/** Constant for the direction "down". */
	public static final byte DOWN  = 1;

	/** Constant for the direction "left". */
	public static final byte LEFT  = 2;

	/** Constant for the direction "right". */
	public static final byte RIGHT = 3;
	
	/**
	 * The number of directions, and also the first illegal direction value.
	 * Direction values start with {@code 0} and end just before {@code DIRS}.
	 * @see #DIR_BITS
	 */
	public static final byte DIRS_COUNT  = 4;
	
	/**
	 * The number of bits which can contain any of the {@link #DIRS_COUNT}
	 * (={@value #DIRS_COUNT}) direction values.
	 */
	public static final byte DIR_BITS = 2;
	
	/**
	 * Since {@code 0}, the default initial value of numeric variables,
	 * is a valid direction value, this value can be used to
	 * initialize variables to a non-valid direction value.
	 * 
	 * @see Directions#isDirection(int)
	 */
	public static final byte NO_DIR = -1;
	
	
	/**
	 * The vertical axis,
	 * representing directions {@link #UP} and {@link #DOWN}.
	 * This is <em>not</em> a direction!
	 */
	public static final byte AXIS_VERTICAL   = 0;
	
	/**
	 * The horizontal axis,
	 * representing directions {@link #LEFT} and {@link #RIGHT}.
	 * This is <em>not</em> a direction!
	 */
	public static final byte AXIS_HORIZONTAL = 1;
	
	/**
	 * The number of axis values, and also the first illegal axis value.
	 */
	public static final byte AXES = 2;
}
