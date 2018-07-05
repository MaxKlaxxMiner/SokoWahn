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
package de.sokoban_online.jsoko.utilities.listener;

/**
 * This class is used to generate multiple objects, e.g. to pass fresh
 * event objects to multiple listeners.
 * 
 * @author Heiner Marxen
 */
public class Generator<T> {
	/**
	 * The base object from which to generate objects by {@link #generate()}.
	 */
	public T base;
	
	/**
	 * Constructs a new generator from a base object.
	 * 
	 * @param base the base from which more objects are to be generated.
	 */
	public Generator(T base) {
		this.base = base;
	}
	
	/**
	 * This default generator implementation just returns the {@link #base}
	 * object reference.
	 * That probably works only for immutable types T.
	 * <p>
	 * Most implementations should override this method by something that
	 * clones the {@link #base} or uses it as basis for a constructor.
	 * E.g. ActionEvents are neither immutable nor do they support cloning.
	 * 
	 * @return the next generated object
	 */
	public T generate() {
		return base;
	}
}
