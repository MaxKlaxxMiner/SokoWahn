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

namespace JSoko.Board_
{
  /// <summary>
  /// Here we define numeric constants for the directions, that make up the geometry of a de.sokoban_online.jsoko board.
  /// These constants shall be used consistently throughout the program. Any class (or interface) can import these definitions by an {@code implements} clause.
  /// 
  /// Direction values are coded compactly {@code 0 &lt;= dir &lt; DIRS}, and can be used as array index.
  /// All direction values together with {@code #NO_DIR} fit into a {@code byte}.
  /// 
  /// The currently implemented direction model is "UDLR": 
  /// 
  ///       U            0
  ///       |            |
  ///    L -+- R  ==  2 -+- 3
  ///       |            |
  ///       D            1
  /// 
  /// The numeric values must not be changed.
  /// They are used implicitly, sometimes, e.g. as index into arrays.
  /// 
  /// @author Heiner Marxen
  /// @see Directions
  /// </summary>
  public static class DirectionConstants
  {
    /// <summary>
    /// Constant for the direction "up".
    /// </summary>
    public const byte Up = 0;

    /// <summary>
    /// Constant for the direction "down".
    /// </summary>
    public const byte Down = 1;

    /// <summary>
    /// Constant for the direction "left".
    /// </summary>
    public const byte Left = 2;

    /// <summary>
    /// Constant for the direction "right".
    /// </summary>
    public const byte Right = 3;

    /// <summary>
    /// The number of directions, and also the first illegal direction value. 
    /// Direction values start with {@code 0} and end just before {@code DIRS}.
    /// @see #DIR_BITS
    /// </summary>
    public const byte DirsCount = 4;

    /// <summary>
    /// The number of bits which can contain any of the {@link #DIRS_COUNT}
    /// (={@value #DIRS_COUNT}) direction values.
    /// </summary>
    public const byte DirBits = 2;

    /// <summary>
    /// Since {@code 0}, the default initial value of numeric variables, is a valid direction value, this value can be used to initialize variables to a non-valid direction value.
    /// 
    /// @see Directions#isDirection(int)
    /// </summary>
    public const byte NoDir = unchecked((byte)-1);

    /// <summary>
    /// The vertical axis,
    /// representing directions {@link #UP} and {@link #DOWN}.
    /// This is NOT a direction!
    /// </summary>
    public const byte AxisVertical = 0;

    /// <summary>
    /// The horizontal axis,
    /// representing directions {@link #LEFT} and {@link #RIGHT}.
    /// This is NOT a direction!
    /// </summary>
    public const byte AxisHorizontal = 1;

    /// <summary>
    /// The number of axis values, and also the first illegal axis value.
    /// </summary>
    public const byte Axes = 2;
  }
}
