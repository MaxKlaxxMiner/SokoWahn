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

/**
 * This interface shall be implemented by collections of box configurations,
 * which have been recognized to be (forward) deadlocks.
 * 
 * @author Heiner Marxen
 */
public interface SubsetAskable {
	/**
	 * The implementing object searches its data for some (small) box
	 * configuration, which is a subset of the passed (big) box configuration.
	 * 
	 * A last moved and therefore involved box (position) can be specified
	 * to narrow the search.  Even when specified, the {@code bigboxconf}
	 * still must also contain that involved box.
	 * 
	 * @param bigboxconf box configuration from which we try to find a subset
	 * @param involvedbox position of the involved box, or -1
	 * @return whether it found some box configuration as a subset of the
	 *         argument
	 */
	public boolean hasSubsetIn(BoxConfByElem bigboxconf, int involvedbox);
}
