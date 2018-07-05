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
package de.sokoban_online.jsoko.gui;

/**
 * This interface is used by implementations of {@code AbstractTableModel}.
 * 
 * @author Heiner Marxen
 */
public interface ColumnVisibility {
	
	/**
	 * Return whether column with the passed column number is visible.
	 * 
	 * @param column the number of the column (in the table model)
	 * @return <code>true</code> if the column is visible, and<br>
	 * 		  <code>false</code> if the column is invisible
	 */
	public boolean isColumnVisible(int column);
}
