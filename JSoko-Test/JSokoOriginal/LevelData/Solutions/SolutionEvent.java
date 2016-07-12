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

import java.util.EventObject;

/**
 * Defines an event that encapsulates changes to the solutions of a level.
 */
@SuppressWarnings("serial")
public class SolutionEvent extends EventObject {

	/**
	 * Actions that has been performed for an object.
	 */
	public enum EventAction {
		/** Event fired due to an insertion. */ INSERT,
		/** Event fired due to a deletion. 	 */ DELETE,
		/** Event fired due to a change. 	 */ CHANGE
	};


    /** Type of the object the event has been fired for. */
    private final Class<?> eventObjectClass;

    /** Type of the action that resulted in the event to be fired. */
    private final EventAction eventAction;

    /** The solution the event has been fired. */
    private final Solution solution;


    /**
     * Constructs a SolutionEvent.
     *
     * @param source  the source Object (typically <code>this</code>)
     * @param objectClass class of the object the event has been fired for
     * @param eventAction  type of the action that resulted in the event to be fired
     * @param solution the {@code Solution} the event has been fired for
     */
    public SolutionEvent(Object source, Class<?> objectClass, EventAction eventAction, Solution solution) {
		super(source);
		this.eventObjectClass  = objectClass;
		this.eventAction   	   = eventAction;
		this.solution		   = solution;
	}

    /**
     * Returns the class of the object the event has been fired for.
     *
     * @return the class of the object
     */
    public Class<?> getEventObjectClass() {
    	return eventObjectClass;
    }

    /**
     * Returns the type of action that resulted in the event to be fired.
     *
     * @return  type of event action
     */
    public EventAction getEventAction() {
    	return eventAction;
    }

    /**
     * Returns the solution the event has been fired for.
     *
     * @return {@code List} of database IDs
     */
    public Solution getSolution() {
    	return solution;
    }

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result
				+ ((eventAction == null) ? 0 : eventAction.hashCode());
		result = prime
				* result
				+ ((eventObjectClass == null) ? 0 : eventObjectClass.hashCode());
		result = prime * result
				+ ((solution == null) ? 0 : solution.hashCode());
		result = prime * result
				+ ((source == null) ? 0 : source.hashCode());
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj) {
			return true;
		}
		if (obj == null) {
			return false;
		}
		if (getClass() != obj.getClass()) {
			return false;
		}
		SolutionEvent other = (SolutionEvent) obj;
		if (eventAction != other.eventAction) {
			return false;
		}
		if (eventObjectClass == null) {
			if (other.eventObjectClass != null) {
				return false;
			}
		} else if (!eventObjectClass.equals(other.eventObjectClass)) {
			return false;
		}
		if (solution == null) {
			if (other.solution != null) {
				return false;
			}
		} else if (!solution.equals(other.solution)) {
			return false;
		}
		if (source == null) {
			if (other.source != null) {
				return false;
			}
		} else if (!source.equals(other.source)) {
			return false;
		}
		return true;
	}

	@Override
	public String toString() {
		return "SolutionEvent [eventObjectClass=" + eventObjectClass
				+ ", eventAction=" + eventAction + ", solution=" + solution
				+ ", source=" + source + "]";
	}

}