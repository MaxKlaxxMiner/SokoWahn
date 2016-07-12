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

import java.util.Iterator;


/**
 * This is similar to an {@link Iterator}, but is meant for array like maps,
 * with a fixed key type {@code long}.
 * Especially for sparse arrays the main work would be duplicated
 * in the calls of {@code hasNext()} and {@code next()}.
 * That inefficiency is avoided here by contracting both
 * into the {@link #next()}, which returns {@code false} at the end
 * of the iteration instead of throwing an exception.
 * <p>
 * The typical (intended) usage looks like this:<pre>
 *   Stepper<V> s = ...;
 *   while (s.next()) {
 *       long index = s.curKey();
 *       V    value = s.curVal();
 *       ... body with index and value ...
 *   }</pre>
 * The order of they keys in which the entries are delivered is not specified
 * by this interface, but rather by the constructing class/method.
 * 
 * @author Heiner Marxen
 */
public interface Stepper<V> {
	
	/**
	 * Advances to the next entry (key/value pair).
	 * When called the first time "advances" to the first entry.
	 * 
	 * @return whether such a step could be done. If not, the iteration is done.
	 */
	boolean next();
	
	/**
	 * Retrieves the key of the current entry.
	 * If there is no current entry, some value will be returned,
	 * but which one is undefined.
	 * 
	 * @return the current key
	 */
	long curKey();
	
	/**
	 * Retrieves the value of the current entry.
	 * @return the current value
	 * @exception NoSuchElementException iteration has no more entries
	 */
	V curValue();
	
	/**
	 * Determines whether there is a current entry.
	 * Before the first call of {@link #next()} there is no current entry.
	 * Between {@link #remove()} and {@link #next()} there is no current entry.
	 * 
	 * @return whether there is a current entry
	 */
	boolean hasCurrent();
	
	/**
	 * Removes the current entry from the array behind this {@code Stepper}.
	 * This is an optional operation.
	 * 
	 * @exception UnsupportedOperationException if not implemented
	 * 
	 * @exception IllegalStateException if just now there is no current entry
	 * 
	 * @see Iterator#remove()
	 */
	void remove();
}
