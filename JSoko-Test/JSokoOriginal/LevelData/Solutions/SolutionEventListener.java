/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2014 by Matthias Meger, Germany
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
package de.sokoban_online.jsoko.leveldata.solutions;

import java.util.EventListener;


/**
 * Interface for listeners of {@code SolutionEvent}s.
 */
public interface SolutionEventListener extends EventListener {

	/**
	 * Called when the solutions of a level have been changed.
	 *
	 * @param event <code>SolutionEvent</code> encapsulating the
     *    event information
	 */
	public void solutionEventFired(SolutionEvent event);

}
